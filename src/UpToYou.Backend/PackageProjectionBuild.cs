using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.Logging;
using UpToYou.Core;
using UniqueId = UpToYou.Core.UniqueId;

namespace UpToYou.Backend {

internal interface
IProjectionBuildObserver {
    void OnHostedFileBuilt(HostedFile hostedFile);
    void OnDeltaBuilt(PackageFileDelta delta);
    void OnProjectionBuilt(PackageProjection projection);
    void OnPackageFileNotFound(RelativePath path);
}

internal class 
ProjectionBuildContext {
    public string SourceDirectory { get; }
    public string OutputDirectory{ get; }
    public Package Package{ get; }
   // public PackageSpecs PackageSpecs{ get; }
    public PackageProjectionSpecs ProjectionSpecs{ get; }
    public PackageHostContext HostContext{ get; }
    public string HostRootUrl{ get; }
    public ILogger? Log { get; }

    public ProjectionBuildContext(string sourceDirectory, string outputDirectory, Package package, PackageProjectionSpecs projectionSpecs, PackageHostContext hostContext, string hostRootUrl, ILogger? log) {
        SourceDirectory = sourceDirectory;
        OutputDirectory = outputDirectory;
        Package = package;
        //PackageSpecs = packageSpecs;
        ProjectionSpecs = projectionSpecs;
        HostContext = hostContext;
        HostRootUrl = hostRootUrl;
        Log =log;
    }
}

internal static class 
PackageProjectionBuild {

    internal static (PackageProjection projection, string projectionFilesDir)
    BuildProjection(this ProjectionBuildContext ctx, List<Package>? allCachedPackages = null) =>
        (new PackageProjection(
                id: UniqueId.NewUniqueId(),
                packageId: ctx.Package.Id,
                rootUrl: ctx.HostRootUrl,
                hostedFiles: ctx.ProjectionSpecs
                    .RetrieveHostedFiles(ctx.Package)
                    .SelectMany(x => x.BuildHostedFiles(ctx, allCachedPackages).Where(y => y.RelevantItemsIds.Any())).ToList()).Log(ctx),
            ctx.OutputDirectory);

    //It is possible that some package files are not included into the projection specs by the user.
    //In this case we need to add one more hosted file containing all those package files.
    private static IEnumerable<PackageProjectionFileSpec>
    RetrieveHostedFiles(this PackageProjectionSpecs specs, Package package) {

        //This algorithm is inefficient and is required to be changed
        var remainingFiles = package.Files.Keys.ToList();

        void RemoveFromRemaining(RelativeGlob glob) =>
            remainingFiles.RemoveAll(x => x.Matches(glob));

        foreach (var projectionFileSpec in specs.HostedFiles) {
            foreach (var relativeGlob in projectionFileSpec.Content)
                RemoveFromRemaining(relativeGlob);

            yield return projectionFileSpec;
        }

        if (remainingFiles.Count > 0)
            yield return new PackageProjectionFileSpec(remainingFiles.Select(x => x.ToRelativeGlob()).ToList());
        
    }

    private static IEnumerable<HostedFile>
    BuildHostedFiles(this PackageProjectionFileSpec fileSpec, ProjectionBuildContext ctx, List<Package>? allCachedPackages = null) {
        yield return fileSpec.Content.Select(x => ctx.Package.FindFiles(x)).NotNull().SelectMany(x => x).Select(x => x.Id).ToList().PackItems(ctx);

        if (fileSpec.HostDeltas && fileSpec.MaxHostDeltas > 0)
            foreach (var buildDelta in BuildDeltas(fileSpec, ctx, allCachedPackages) )
                yield return buildDelta;
    }

    private static HostedFile
    PackItems(this List<string> packageItemsIds, ProjectionBuildContext ctx) {
        var resultFile = packageItemsIds
            .Select(x => ctx.Package.GetFileById(x).GetFile(ctx.SourceDirectory))
            .ArchiveFiles(ctx.SourceDirectory, ctx.OutputDirectory.CreateDirectoryIfAbsent().AppendPath(UniqueId.NewUniqueId()))
            .CompressFile()
            .ReplaceFileNameByHash();

        return new HostedFile(
            id: UniqueId.NewUniqueId(),
            subUrl:resultFile.GetPathRelativeTo(ctx.OutputDirectory).ToHostedFileSubUrlOnHost(),
            fileHash:resultFile.GetFileNameUntilDot(),
            fileSize:resultFile.GetFileSize(),
            content:new PackageItemsHostedFileContent(packageItemsIds)).Log(ctx);
    }

    private static IEnumerable<HostedFile>
    BuildDeltas(this PackageProjectionFileSpec fileSpec, ProjectionBuildContext ctx, List<Package>? allCachedPackages = null) {
        var packages = (allCachedPackages ?? ctx.HostContext.DownloadAllPackages())
            .Where(x => x.Metadata.Name == ctx.Package.Metadata.Name && x.Metadata.Version != ctx.Package.Metadata.Version).ToList();
        
        if (packages.Count > fileSpec.MaxHostDeltas)
            packages = packages.OrderByDescending(x => x.Metadata.Version).Take(fileSpec.MaxHostDeltas).ToList();

        return packages.Select(x => BuildDelta(fileSpec, x, ctx));
    }

    private static HostedFile
    BuildDelta(PackageProjectionFileSpec spec, Package oldPackage, ProjectionBuildContext ctx) {
        if (oldPackage.Id == ctx.Package.Id)
            throw new InvalidOperationException("Can't built delta for self package");

        var oldProjection = oldPackage.DownloadProjection(ctx.HostContext) 
            ?? throw new InvalidRemoteDataException($"Projection not found for package with id = {oldPackage.Id}");

        //Deltas for files absent in the oldPackage are simply not included into the resulting hosted file. 
        var oldFilesIds = spec.Content
            .SelectMany(x => ctx.Package.FindFiles(x))
            .Select(x => oldPackage.FindFileByPath(x.Path)?.Id).NotNull().ToList();
        
        //Downloading required oldPackage files from the host
        oldProjection
            .DownloadHostedFiles(ctx.HostContext, oldFilesIds, ctx.OutputDirectory)
            .ExtractAllHostedFiles(ctx.OutputDirectory.AppendPath(oldProjection.Id));

        (string packageFileId, string fromFile, string toFile) 
        BuildDeltaPars(string oldPackageFileId) {
            var file = oldPackage.GetFileById(oldPackageFileId);
            return (packageFileId: ctx.Package.GetFileByPath(file.Path).Id,
                fromFile: file.Path.ToAbsolute( ctx.OutputDirectory.AppendPath(oldProjection.Id)).VerifyFileExistence(),
                toFile: file.Path.ToAbsolute(ctx.SourceDirectory).VerifyFileExistence());
        }

        //Building deltas
        var deltas = oldFilesIds.Select(x => BuildDeltaPars(x).BuildFileDelta(ctx, oldPackage)).ToList();

        //Archiving deltas
        var deltasFilesArchive = deltas
            .Select(x => x.deltaFile)
            .ArchiveFiles(ctx.OutputDirectory, UniqueId.NewUniqueId().ToAbsoluteFilePath(ctx.OutputDirectory).CreateParentDirectoryIfAbsent())
            .ReplaceFileNameByHash();

        return new HostedFile(
            id: UniqueId.NewUniqueId(),
            subUrl: deltasFilesArchive.GetPathRelativeTo(ctx.OutputDirectory).ToDeltasSubUrlOnHost(),
            fileHash:deltasFilesArchive.GetFileNameUntilDot(),
            fileSize:deltasFilesArchive.GetFileSize(),
            content: new PackageFileDeltasHostedFileContent(deltas.MapToList(x => x.delta))).Log(ctx);
    }


    private static (PackageFileDelta delta, string deltaFile)
    BuildFileDelta(this (string packageFileId, string fromFile, string toFile) @in, ProjectionBuildContext ctx, Package? packageFrom = null) {

        var deltaBytes =BinaryDelta.GetDelta(@in.fromFile, @in.toFile);

        var oldHash = @in.fromFile.GetFileHash();
        var newHash = @in.toFile.GetFileHash();

        var deltaFile = deltaBytes
            .OverwriteToFile(PackageProjection.GetDeltaFileName(oldHash, newHash)
                .ToRelativePath().ToHostedFileSubUrlOnHost()
                .ToAbsolute(ctx.OutputDirectory)
                .CreateParentDirectoryIfAbsent());

        var delta = new PackageFileDelta(
            oldHash:oldHash,
            newHash:newHash,
            packageItemId:@in.packageFileId);

        if (ctx.Log != null && packageFrom != null) {
            var oldFile = packageFrom.FindFileById(@in.packageFileId);
            if (oldFile != null)
                ctx.Log?.LogInformation($"Delta has been built for {oldFile.Path.Value.Quoted()} of size {deltaBytes.Length.BytesToMegabytes()} mb has been built from version {packageFrom.Version}");
        }

        return (delta, deltaFile);
    }

    private static string
    GetFileNameUntilDot(this string file) {
        var name = file.GetFileName();
        return name.Contains(".") ? name.Substring(0, name.IndexOf(".", StringComparison.Ordinal)) : name;
    }

    private static string
    ReplaceFileNameByHash(this string file) => file.ReplaceInFileName(file.GetFileNameUntilDot(), file.GetFileHash());

    private static HostedFile Log(this HostedFile hostedFile, ProjectionBuildContext ctx) {
        if (ctx.Log != null) {
            if (hostedFile.Content is PackageFileDeltasHostedFileContent deltasContent) {
                ctx.Log.LogInformation($"Hosted file of size {hostedFile.FileSize.BytesToMegabytes()} mb has been built with {deltasContent.PackageFileDeltas.Count} deltas.");
                ctx.Log.LogDebug(deltasContent.PackageFileDeltas.Aggregate(string.Empty, (s,x) => s + ctx.Package.GetFileById(x.PackageItemId).Path.Value + "\n"));
            }
            else if (hostedFile.Content is PackageItemsHostedFileContent itemsContent)
                ctx.Log.LogInformation($"Hosted file of size {hostedFile.FileSize.BytesToMegabytes()} mb has been built with {itemsContent.PackageItems.Count} files.");
        }
        return hostedFile;
    }

    private static PackageProjection Log(this PackageProjection projection, ProjectionBuildContext ctx) {
        ctx.Log?.LogInformation($"Projection {projection.PackageId.PackageIdToProjectionFileOnHost()} has been built with {projection.HostedFiles.Count} hosted files.");
        return projection;
    }
}
}

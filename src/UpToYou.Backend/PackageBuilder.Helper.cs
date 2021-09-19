using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using UpToYou.Core;

namespace UpToYou.Backend {
internal static class BuilderHelper {
    
    public static PackageBuilder 
    ToPackageBuildContext(this PackageSpecs packageSpecs, string srcDir, string outDir) =>
        new PackageBuilder(srcDir, outDir, packageSpecs);

    public static (Package package, string packageFilesDir)
    BuildPackage(this PackageBuilder ctx){
        var files = ctx.Specs.GetFilesRelative(ctx.SourceDirectory)
            .Select<RelativePath, PackageFile>(x => x.ToPackageFile(ctx))
            .ToDictionary(x => x.Path, x => x);
        
        var versionProvider = files.TryGetValue(ctx.Specs.VersionProvider, out var res) ? res : throw new InvalidOperationException($"Version provider package file ({ctx.Specs.VersionProvider}) not found");
        return (new Package(
            metadata:new PackageMetadata(
                id:UniqueId.NewUniqueId(),
                name:ctx.Specs.PackageName ?? string.Empty,
                version: versionProvider.FileVersion ?? throw new InvalidOperationException("Version provider file should have a file version"),
                datePublished: DateTime.Now,
                versionProviderFile: versionProvider,
                customProperties: ctx.Specs.CustomProperties.AddCustomProperties(ctx.CustomProperties, @override:true)),
            files: files), ctx.OutputDirectory);;
    }

    private static PackageFile
    ToPackageFile(this RelativePath fileSpec, PackageBuilder context) {
        var outFile = 
            fileSpec.ToAbsolute(context.SourceDirectory)
                .CopyFile(context.OutputDirectory.AppendPath(fileSpec));

        return new PackageFile(
            id: UniqueId.NewUniqueId(),
            path:fileSpec,
            fileSize: outFile.GetFileSize(),
            fileHash:outFile.GetFileHash(),
            fileVersion:outFile.GetFileVersion()  );
    }
    
    internal static (PackageProjection projection, string projectionFilesDir)
    BuildProjection(this ProjectionBuilder builder, List<Package>? allCachedPackages = null) =>
        (new PackageProjection(
                id: UniqueId.NewUniqueId(),
                packageId: builder.Package.Id,
                rootUrl: builder.HostRootUrl,
                hostedFiles: builder.ProjectionSpecs
                    .RetrieveHostedFiles(builder.Package)
                    .SelectMany(x => x.BuildHostedFiles(builder, allCachedPackages).Where(y => y.RelevantItemsIds.Any())).ToList()).Log(builder),
            builder.OutputDirectory);

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
    BuildHostedFiles(this PackageProjectionFileSpec fileSpec, ProjectionBuilder ctx, List<Package>? allCachedPackages = null) {
        yield return fileSpec.Content.Select(x => ctx.Package.GetFiles(x)).NotNull().SelectMany(x => x).Select(x => x.Id).ToList().PackItems(ctx);

        if (fileSpec.HostDeltas && fileSpec.MaxHostDeltas > 0)
            foreach (var buildDelta in BuildDeltas(fileSpec, ctx, allCachedPackages) )
                yield return buildDelta;
    }

    private static HostedFile
    PackItems(this List<string> packageItemsIds, ProjectionBuilder builder) {
        var resultFile = packageItemsIds
            .Select(x => builder.Package.GetFileById(x).GetFile(builder.SourceDirectory))
            .ArchiveFiles(builder.SourceDirectory, builder.OutputDirectory.CreateDirectoryIfAbsent().AppendPath(UniqueId.NewUniqueId()))
            .CompressFile()
            .ReplaceFileNameByHash();

        return new HostedFile(
            id: UniqueId.NewUniqueId(),
            subUrl:resultFile.GetPathRelativeTo(builder.OutputDirectory).ToHostedFileSubUrlOnHost(),
            fileHash:resultFile.GetFileNameUntilDot(),
            fileSize:resultFile.GetFileSize(),
            content:new PackageItemsHostedFileContent(packageItemsIds)).Log(builder);
    }

    private static IEnumerable<HostedFile>
    BuildDeltas(this PackageProjectionFileSpec fileSpec, ProjectionBuilder builder, List<Package>? allCachedPackages = null) {
        var packages = (allCachedPackages ?? builder.Host.DownloadAllPackages())
            .Where(x => x.Metadata.Name == builder.Package.Metadata.Name && x.Metadata.Version != builder.Package.Metadata.Version).ToList();
        
        if (packages.Count > fileSpec.MaxHostDeltas)
            packages = packages.OrderByDescending(x => x.Metadata.Version).Take(fileSpec.MaxHostDeltas).ToList();

        return packages.Select(x => BuildDelta(fileSpec, x, builder));
    }

    private static HostedFile
    BuildDelta(PackageProjectionFileSpec spec, Package oldPackage, ProjectionBuilder builder) {
        if (oldPackage.Id == builder.Package.Id)
            throw new InvalidOperationException("Can't built delta for self package");

        var oldProjection = oldPackage.DownloadProjection(builder.Host) 
            ?? throw new InvalidOperationException($"Projection not found for package with id = {oldPackage.Id}");

        //Deltas for files absent in the oldPackage are simply not included into the resulting hosted file. 
        var oldFilesIds = spec.Content
            .SelectMany(x => builder.Package.GetFiles(x))
            .Select(x => oldPackage.TryGetFileByPath(x.Path)?.Id).NotNull().ToList();
        
        //Downloading required oldPackage files from the host
        oldProjection
            .DownloadHostedFiles(builder.Host, oldFilesIds, builder.OutputDirectory)
            .ExtractAllHostedFiles(builder.OutputDirectory.AppendPath(oldProjection.Id));

        (string packageFileId, string fromFile, string toFile) 
        BuildDeltaPars(string oldPackageFileId) {
            var file = oldPackage.GetFileById(oldPackageFileId);
            return (packageFileId: builder.Package.GetFileByPath(file.Path).Id,
                fromFile: file.Path.ToAbsolute( builder.OutputDirectory.AppendPath(oldProjection.Id)).VerifyFileExistence(),
                toFile: file.Path.ToAbsolute(builder.SourceDirectory).VerifyFileExistence());
        }

        //Building deltas
        var deltas = oldFilesIds.Select(x => BuildDeltaPars(x).BuildFileDelta(builder, oldPackage)).ToList();

        //Archiving deltas
        var deltasFilesArchive = deltas
            .Select(x => x.deltaFile)
            .ArchiveFiles(builder.OutputDirectory, UniqueId.NewUniqueId().ToAbsoluteFilePath(builder.OutputDirectory).CreateParentDirectoryIfAbsent())
            .ReplaceFileNameByHash();

        return new HostedFile(
            id: UniqueId.NewUniqueId(),
            subUrl: deltasFilesArchive.GetPathRelativeTo(builder.OutputDirectory).ToDeltasSubUrlOnHost(),
            fileHash:deltasFilesArchive.GetFileNameUntilDot(),
            fileSize:deltasFilesArchive.GetFileSize(),
            content: new PackageFileDeltasHostedFileContent(deltas.MapToList(x => x.delta))).Log(builder);
    }


    private static (PackageFileDelta delta, string deltaFile)
    BuildFileDelta(this (string packageFileId, string fromFile, string toFile) @in, ProjectionBuilder builder, Package? packageFrom = null) {

        var deltaBytes =BinaryDelta.GetDelta(@in.fromFile, @in.toFile);

        var oldHash = @in.fromFile.GetFileHash();
        var newHash = @in.toFile.GetFileHash();

        var deltaFile = deltaBytes
            .OverwriteToFile(PackageProjection.GetDeltaFileName(oldHash, newHash)
                .ToRelativePath().ToHostedFileSubUrlOnHost()
                .ToAbsolute(builder.OutputDirectory)
                .CreateParentDirectoryIfAbsent());

        var delta = new PackageFileDelta(
            oldHash:oldHash,
            newHash:newHash,
            packageItemId:@in.packageFileId);

        if ( packageFrom != null) {
            var oldFile = packageFrom.TryGetFileById(@in.packageFileId);
            if (oldFile != null)
                builder.Log?.LogInformation($"Delta has been built for {oldFile.Path.Value.Quoted()} of size {deltaBytes.Length.BytesToMegabytes()} mb has been built from version {packageFrom.Version}");
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

    private static HostedFile 
    Log(this HostedFile hostedFile, ProjectionBuilder builder) {
        if (hostedFile.Content is PackageFileDeltasHostedFileContent deltasContent) {
            builder.Log.LogInformation($"Hosted file of size {hostedFile.FileSize.BytesToMegabytes()} mb has been built with {deltasContent.PackageFileDeltas.Count} deltas.");
            builder.Log.LogDebug(deltasContent.PackageFileDeltas.Aggregate(string.Empty, (s,x) => s + builder.Package.GetFileById(x.PackageItemId).Path.Value + "\n"));
        }
        else if (hostedFile.Content is PackageItemsHostedFileContent itemsContent)
            builder.Log.LogInformation($"Hosted file of size {hostedFile.FileSize.BytesToMegabytes()} mb has been built with {itemsContent.PackageItems.Count} files.");
        return hostedFile;
    }

    private static PackageProjection 
    Log(this PackageProjection projection, ProjectionBuilder builder) {
        builder.Log?.LogInformation($"Projection {projection.PackageId.GetPackageProjectionFileOnHost()} has been built with {projection.HostedFiles.Count} hosted files.");
        return projection;
    }
}
}

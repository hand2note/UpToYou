using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using UpToYou.Core;

namespace UpToYou.Backend {
internal static class BuilderHelper {
    
    public static PackageBuilder 
    ToPackageBuildContext(this PackageSpecs packageSpecs, string sourceDirectory, string outputDirectory) =>
        new PackageBuilder(sourceDirectory, outputDirectory, packageSpecs);

    public static (Package package, string packageFilesDirectory)
    BuildPackage(this PackageBuilder builder){
        var files = builder.Specs.GetFilesRelative(builder.SourceDirectory)
            .Select<RelativePath, PackageFile>(x => x.ToPackageFile(builder))
            .ToImmutableDictionary(x => x.Path, x => x);
        
        var versionProvider = files.TryGetValue(builder.Specs.VersionProvider, out var res) ? res : throw new InvalidOperationException($"Version provider package file ({builder.Specs.VersionProvider}) not found");
        return (new Package(
            header:new PackageHeader(
                id:UniqueId.NewUniqueId(),
                name:builder.Specs.PackageName ?? string.Empty,
                version: versionProvider.FileVersion ?? throw new InvalidOperationException("Version provider file should have a file version"),
                datePublished: DateTime.Now,
                versionProviderFile: versionProvider,
                customProperties: builder.Specs.CustomProperties),
            files: files), builder.OutputDirectory);;
    }

    private static PackageFile
    ToPackageFile(this RelativePath fileSpec, PackageBuilder builder) {
        var outFile = 
            fileSpec.ToAbsolute(builder.SourceDirectory)
                .CopyFile(builder.OutputDirectory.AppendPath(fileSpec));

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
                files: builder.ProjectionSpecs
                    .RetrieveHostedFiles(builder.Package)
                    .SelectMany(x => x.BuildHostedFiles(builder, allCachedPackages).Where(y => y.RelevantItemsIds.Any())).ToImmutableList()).Log(builder),
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

    private static IEnumerable<PackageProjectionFile>
    BuildHostedFiles(this PackageProjectionFileSpec fileSpec, ProjectionBuilder builder, List<Package>? allCachedPackages = null) {
        yield return fileSpec.Content.Select(x => builder.Package.GetFiles(x)).NotNull().SelectMany(x => x).Select(x => x.Id).ToList().PackItems(builder);

        if (fileSpec.HostDeltas && fileSpec.MaxHostDeltas > 0)
            foreach (var buildDelta in BuildDeltas(fileSpec, builder, allCachedPackages) )
                yield return buildDelta;
    }

    private static PackageProjectionFile
    PackItems(this IList<string> packageItemsIds, ProjectionBuilder builder) {
        var resultFile = packageItemsIds
            .Select(x => builder.Package.GetFileById(x).GetFile(builder.SourceDirectory))
            .ArchiveFiles(builder.SourceDirectory, builder.OutputDirectory.CreateDirectoryIfAbsent().AppendPath(UniqueId.NewUniqueId()))
            .CompressFile()
            .ReplaceFileNameByHash();

        return new PackageProjectionFile(
            id: UniqueId.NewUniqueId(),
            subUrl:resultFile.GetPathRelativeTo(builder.OutputDirectory).ToHostedFileSubUrlOnHost(),
            fileHash:resultFile.GetFileNameUntilDot(),
            fileSize:resultFile.GetFileSize(),
            content:new PackageProjectionFileContent(packageItemsIds.ToImmutableList())).Log(builder);
    }

    private static IEnumerable<PackageProjectionFile>
    BuildDeltas(this PackageProjectionFileSpec fileSpec, ProjectionBuilder builder, List<Package>? allCachedPackages = null) {
        var packages = (allCachedPackages ?? builder.Host.DownloadAllPackages())
            .Where(x => x.Header.Name == builder.Package.Header.Name && x.Header.Version != builder.Package.Header.Version).ToList();
        
        if (packages.Count > fileSpec.MaxHostDeltas)
            packages = packages.OrderByDescending(x => x.Header.Version).Take(fileSpec.MaxHostDeltas).ToList();

        return packages.Select(x => BuildDelta(fileSpec, x, builder));
    }

    private static PackageProjectionFile
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
            .DownloadProjectionFiles(builder.Host, oldFilesIds, builder.OutputDirectory)
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

        return new PackageProjectionFile(
            id: UniqueId.NewUniqueId(),
            subUrl: deltasFilesArchive.GetPathRelativeTo(builder.OutputDirectory).ToDeltasSubUrlOnHost(),
            fileHash:deltasFilesArchive.GetFileNameUntilDot(),
            fileSize:deltasFilesArchive.GetFileSize(),
            content: new PackageProjectionFileDeltaContent(deltas.MapToImmutableList(x => x.delta))).Log(builder);
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
            packageFileId:@in.packageFileId);

        if ( packageFrom != null) {
            var oldFile = packageFrom.TryGetFileById(@in.packageFileId);
            if (oldFile != null)
                builder.Logger?.LogInformation($"Delta has been built for {oldFile.Path.Value.Quoted()} of size {deltaBytes.Length.BytesToMegabytes()} mb has been built from version {packageFrom.Version}");
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

    private static PackageProjectionFile 
    Log(this PackageProjectionFile packageProjectionFile, ProjectionBuilder builder) {
        if (packageProjectionFile.Content is PackageProjectionFileDeltaContent deltasContent) {
            builder.Logger.LogInformation($"Hosted file of size {packageProjectionFile.FileSize.BytesToMegabytes()} mb has been built with {deltasContent.PackageFileDeltas.Count} deltas.");
            builder.Logger.LogDebug(deltasContent.PackageFileDeltas.Aggregate(string.Empty, (s,x) => s + builder.Package.GetFileById(x.PackageFileId).Path.Value + "\n"));
        }
        else if (packageProjectionFile.Content is PackageProjectionFileContent itemsContent)
            builder.Logger.LogInformation($"Hosted file of size {packageProjectionFile.FileSize.BytesToMegabytes()} mb has been built with {itemsContent.PackageFileIds.Count} files.");
        return packageProjectionFile;
    }

    private static PackageProjection 
    Log(this PackageProjection projection, ProjectionBuilder builder) {
        builder.Logger?.LogInformation($"Projection {projection.PackageId.GetPackageProjectionFileOnHost()} has been built with {projection.Files.Count} hosted files.");
        return projection;
    }
}
}

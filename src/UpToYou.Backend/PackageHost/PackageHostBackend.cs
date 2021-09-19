using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.Logging;
using UpToYou.Core;
using static UpToYou.Core.PackageHost;

namespace UpToYou.Backend {

public class
PackageHostContext : PackageHostClientContext {
    public new IFilesHost FilesHost { get; }

    public PackageHostContext(IFilesHost filesHost, ILogger? log, ProgressContext? progressContext) 
        : base( filesHost, log, progressContext) => FilesHost = filesHost;
}

public static class PackageHostBackend {
    
    public static bool
    IsPackageFile(this string file) => file.FileContainsExtension(PackageExtension);

    public static bool
    IsPackageProjectionFile(this string file) => file.FileContainsExtension(ProjectionExtension);

    public static void
    Upload(this UpdatesManifest manifest, PackageHostContext ctx) =>
        ctx.FilesHost.UploadData(
            pCtx: ctx.ProgressContext,
            path: UpdateManifestPathOnHost,
            bytes:manifest.ProtoSerializeToBytes().Compress());

    public static UpdatesManifest? 
    DownloadUpdatesManifestIfExists(this PackageHostContext ctx) =>
        ctx.FilesHost.FileExists(UpdateManifestPathOnHost) ? ctx.DownloadUpdatesManifest() : null;

    internal static List<Package>
    DownloadAllPackages(this PackageHostContext ctx) =>
        ctx.FilesHost
           .GetAllFiles(AllPackagesGlobPattern)
           .AsParallel()
           .Select(x => x.DownloadPackage(ctx)).Where(x => x != null).ToList();

    internal static List<PackageProjection>
    DownloadAllProjections(this PackageHostContext ctx) =>
        ctx.FilesHost
           .GetAllFiles(AllProjectionsGlobalPattern)
           .AsParallel()
           .Select(x => x.DownloadProjection(ctx)).ToList();

    ///Downloads hosted files containing all items in filesIds. Doesn't download deltas.
    internal static List<string>
    DownloadHostedFiles(this PackageProjection projection, PackageHostContext ctx, List<string> filesIds, string outDir) {
        var relevantHostedFiles = projection.HostedFiles.Where(x => x.RelevantItemsIds.Intersect(filesIds).Any()).ToList();
        if (relevantHostedFiles.Count == 0)
            throw new InvalidOperationException("Given files to download not found in the hosted package");
        
        relevantHostedFiles = FilterHostedFiles(relevantHostedFiles).ToList();
        ctx.ProgressContext?.OnExtraTargetValue(relevantHostedFiles.Sum(x => x.FileSize));
        return relevantHostedFiles.AsParallel().Select(x => x.SubUrl.DownloadFile(ctx, outDir)).ToList();

        IEnumerable<HostedFile>
        FilterHostedFiles(IList<HostedFile> hostedFiles) {
            
            bool IsItemRelevant(string itemId) => filesIds.Contains(itemId);

            // ReSharper disable once VariableHidesOuterVariable
            bool ContainsAllFiles(HostedFile hostedFile,List<string> filesIds) => filesIds.All(x => hostedFile.RelevantItemsIds.Contains(x));

            bool Filter(HostedFile hostedFile) {
                var relevantFiles = GetRelevantFiles(hostedFile);
                return !hostedFiles.TakeUntil(x => x == hostedFile).NotEqual(hostedFile).Any(x => ContainsAllFiles(x, relevantFiles));
            }

            List<string> GetRelevantFiles(HostedFile hostedFile) => hostedFile.RelevantItemsIds.Where(IsItemRelevant).ToList();

            return hostedFiles.Where(Filter);
        }
    }

    public static RelativePath
    UploadPackageManifest(this Package package, PackageHostContext ctx) {
        var path = package.GetPathOnHost();
        ctx.FilesHost.UploadData(ctx.ProgressContext, path, package.ProtoSerializeToBytes().Compress());
        return path;
    }

    internal static RelativePath
    UploadProjectionManifest(this PackageProjection projection, PackageHostContext ctx) {
        var path = projection.GetPathOnHost();

        ctx.FilesHost.UploadData(ctx.ProgressContext, path, projection.ProtoSerializeToBytes().Compress());
        return path;
    }

    internal static void
    UploadAllProjectionFiles(this (PackageProjection projection, string sourceDir) @in, PackageHostContext ctx) {
        @in.projection.UploadProjectionManifest(ctx);

        var hostedFilesToUpload = @in.projection
            .HostedFiles
            .Where(x => !ctx.FilesHost.GetAllFiles().Contains(x.SubUrl)).ToList();

        ctx.ProgressContext?.OnExtraTargetValue(hostedFilesToUpload.Sum(x => x.FileSize));
        hostedFilesToUpload.ForEach(x => ctx.FilesHost.UploadFile(ctx.ProgressContext, x.SubUrl, x.SubUrl.Value.GetFileName().ToAbsoluteFilePath(@in.sourceDir).VerifyFileExistence())); 
    }

    internal static void
    RemovePackage(this PackageHostContext ctx, string packageId) {
        var package = packageId.DownloadPackageById(ctx);
        ctx.FilesHost.Remove(packageId.PackageIdToPackageFileOnHost());
        ctx.UpdateUpdatesManifest(x => x.Remove(package.Metadata));
        var projection = package.DownloadProjection(ctx);
        ctx.RemoveProjectionFiles(projection);
    }

    private static void
    RemoveProjectionFiles(this PackageHostContext ctx, PackageProjection projection) {
        ctx.FilesHost.Remove(projection.PackageId.PackageIdToProjectionFileOnHost());

        var allProjectionsFiles = ctx.DownloadAllProjections()
            .NotEqualById(projection)
            .SelectMany(x => x.HostedFiles)
            .Select(x => x.SubUrl.Value).Distinct().ToList();

        foreach (var hostedFile in projection.HostedFiles)
            if (!allProjectionsFiles.Contains(hostedFile.SubUrl.Value))
                ctx.FilesHost.RemoveFiles(hostedFile.SubUrl.Value);

    }

    internal static void
    UpdateUpdatesManifest(this PackageHostContext ctx, Action<UpdatesManifest> update) {
        var updatesManifest = ctx.DownloadUpdatesManifestIfExists();
        if (updatesManifest != null) {
            update(updatesManifest);
            updatesManifest.Upload(ctx);
        }
    }

    internal static void
    UploadUpdateNotesUtf8(this string updateNotesUtf8, string? packageName, string? locale, IFilesHost host) => 
        updateNotesUtf8.Utf8ToBytes().UploadUpdateNotesUtf8(packageName, locale, host);

    internal static void
    UploadUpdateNotesUtf8(this byte[] updateNotesUtf8, string? packageName, string? locale, IFilesHost host) => 
        host.UploadData(null, GetUpdateNotesFileOnHost(packageName, locale), updateNotesUtf8.Compress());
    


    //private static IEnumerable<RelativePath>
    //FindPackageProjectionFiles(this string packageId, PackageHostContext context) 
    //    => context.FilesHost.GetAllFiles($"**/*{packageId}*{PackageProjectionExt}, **/*{packageId}*{PackageProjectionExt}.*");
}
}

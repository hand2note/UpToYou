using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using UpToYou.Core;

namespace UpToYou.Backend {

public interface 
IHost: IHostClient {
    void RemoveFiles(string globPattern);
    bool FileExists(RelativePath path);
    List<RelativePath> GetAllFiles(string globPattern);
    void UploadFile(RelativePath path, Stream inStream);
}

public static class
HostHelper {
    
    public static bool 
    TryDownloadUpdateManifest(this IHost host, [NotNullWhen(true)] out UpdatesManifest? result) {
        result = host.DownloadUpdatesManifestIfExists();
        return result != null;
    }
    
    public static bool
    IsPackageFile(this string file) => file.FileContainsExtension(PackageHostHelper.PackageExtension);

    public static bool
    IsPackageProjectionFile(this string file) => file.FileContainsExtension(PackageHostHelper.ProjectionExtension);

    public static void
    Upload(this UpdatesManifest manifest, IHost host) =>
        host.UploadBytes(
            path: PackageHostHelper.UpdateManifestPathOnHost,
            bytes:manifest.ProtoSerializeToBytes().Compress());

    public static UpdatesManifest? 
    DownloadUpdatesManifestIfExists(this IHost host) =>
        host.FileExists(PackageHostHelper.UpdateManifestPathOnHost) ? host.DownloadUpdatesManifest() : null;

    internal static List<Package>
    DownloadAllPackages(this IHost host) =>
        host
           .GetAllFiles(PackageHostHelper.AllPackagesGlobPattern)
           .AsParallel()
           .Select(x => x.DownloadPackage(host)).Where(x => x != null).ToList();

    internal static List<PackageProjection>
    DownloadAllProjections(this IHost host) =>
        host
           .GetAllFiles(PackageHostHelper.AllProjectionsGlobalPattern)
           .AsParallel()
           .Select(x => x.DownloadProjection(host)).ToList();

    ///Downloads hosted files containing all items in filesIds. Doesn't download deltas.
    internal static List<string>
    DownloadHostedFiles(this PackageProjection projection, IHost host, List<string> filesIds, string outDirectory) {
        var relevantHostedFiles = projection.HostedFiles.Where(x => x.RelevantItemsIds.Intersect(filesIds).Any()).ToList();
        if (relevantHostedFiles.Count == 0)
            throw new InvalidOperationException("Given files to download not found in the hosted package");
        
        relevantHostedFiles = FilterHostedFiles(relevantHostedFiles).ToList();
        return relevantHostedFiles.AsParallel().Select(x => x.SubUrl.DownloadFile(host, outDirectory)).ToList();

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
    UploadPackageManifest(this Package package, IHost host) {
        var path = package.GetPathOnHost();
        host.UploadBytes(path, package.ProtoSerializeToBytes().Compress());
        return path;
    }

    internal static RelativePath
    UploadProjectionManifest(this PackageProjection projection, IHost host) {
        var path = projection.GetPathOnHost();

        host.UploadBytes(path, projection.ProtoSerializeToBytes().Compress());
        return path;
    }

    internal static void
    UploadAllProjectionFiles(this (PackageProjection projection, string sourceDir) @in, IHost host) {
        @in.projection.UploadProjectionManifest(host);

        var hostedFilesToUpload = @in.projection
            .HostedFiles
            .Where(x => !host.GetAllFiles().Contains(x.SubUrl)).ToList();

        hostedFilesToUpload.ForEach(x => host.UploadFile(x.SubUrl, x.SubUrl.Value.GetFileName().ToAbsoluteFilePath(@in.sourceDir).VerifyFileExistence())); 
    }

    internal static void
    RemovePackage(this IHost host, string packageId) {
        var package = packageId.DownloadPackageById(host);
        host.Remove(packageId.GetPackageFileOnHost());
        host.UpdateUpdatesManifest(x => x.Remove(package.Metadata));
        var projection = package.DownloadProjection(host);
        host.RemoveProjectionFiles(projection);
    }

    private static void
    RemoveProjectionFiles(this IHost host, PackageProjection projection) {
        host.Remove(projection.PackageId.GetPackageProjectionFileOnHost());

        var allProjectionsFiles = host.DownloadAllProjections()
            .NotEqualById(projection)
            .SelectMany(x => x.HostedFiles)
            .Select(x => x.SubUrl.Value).Distinct().ToList();

        foreach (var hostedFile in projection.HostedFiles)
            if (!allProjectionsFiles.Contains(hostedFile.SubUrl.Value))
                host.RemoveFiles(hostedFile.SubUrl.Value);

    }

    internal static void
    UpdateUpdatesManifest(this IHost host, Action<UpdatesManifest> update) {
        var updatesManifest = host.DownloadUpdatesManifestIfExists();
        if (updatesManifest != null) {
            update(updatesManifest);
            updatesManifest.Upload(host);
        }
    }

    internal static void
    UploadUpdateNotesUtf8(this string updateNotesUtf8, string packageName, string locale, IHost host) => 
        updateNotesUtf8.Utf8ToBytes().UploadUpdateNotesUtf8(packageName, locale, host);

    internal static void
    UploadUpdateNotesUtf8(this byte[] updateNotesUtf8, string packageName, string locale, IHost host) => 
        host.UploadBytes(PackageHostHelper.GetUpdateNotesFileOnHost(packageName, locale), updateNotesUtf8.Compress());
    
    public static List<RelativePath>
    GetAllFiles(this IHost host) => host.GetAllFiles("**/*");

    public static async Task 
    UploadFileAsync(this IHost host, RelativePath path, Stream inStream) =>
        await Task.Run(() => host.UploadFile(path, inStream));

    public static void 
    UploadFile(this IHost host,RelativePath path, string sourceFile) {
        using var fs =sourceFile.OpenFileForRead();
        host.UploadFile(path, fs);
    }

    public static void 
    UploadBytes(this IHost host, RelativePath path, byte[] bytes) =>
        host.UploadFile(path, new MemoryStream(bytes));

    public static void 
    Remove(this IHost host, RelativePath path) => host.RemoveFiles(path.Value);
}

}

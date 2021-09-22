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
HostServerHelper {
    
    public static bool 
    UpdateManifestFileExists(this IHost host) => 
        host.FileExists(HostHelper.UpdatesManifestPathOnHost);
    
    public static UpdatesManifest
    UploadUpdateManifest(this UpdatesManifest manifest, IHost host) {
        host.UploadBytes(
            path: HostHelper.UpdatesManifestPathOnHost,
            bytes: manifest.ProtoSerializeToBytes().Compress());
        return manifest;
    }

    public static UpdatesManifest? 
    DownloadUpdatesManifestIfExists(this IHost host) =>
        host.FileExists(Core.HostHelper.UpdatesManifestPathOnHost) ? host.DownloadUpdatesManifest() : null;

    internal static List<Package>
    DownloadAllPackages(this IHost host) =>
        host
           .GetAllFiles(Core.HostHelper.AllPackagesGlobPattern)
           .AsParallel()
           .Select(x => x.DownloadPackage(host)).ToList();

    internal static List<PackageProjection>
    DownloadAllProjections(this IHost host) =>
        host
           .GetAllFiles(HostHelper.AllProjectionsGlobalPattern)
           .AsParallel()
           .Select(x => x.DownloadProjection(host)).ToList();

    ///Downloads hosted files containing all items in filesIds. Doesn't download deltas.
    internal static List<string>
    DownloadProjectionFiles(this PackageProjection projection, IHost host, List<string> fileIds, string outDirectory) {
        var relevantHostedFiles = projection.Files.Where(x => x.RelevantItemsIds.Intersect(fileIds).Any()).ToList();
        if (relevantHostedFiles.Count == 0)
            throw new InvalidOperationException("Given files to download not found in the hosted package");
        
        relevantHostedFiles = FilterHostedFiles(relevantHostedFiles).ToList();
        return relevantHostedFiles.AsParallel().Select(x => x.SubUrl.DownloadFile(host, outDirectory)).ToList();

        IEnumerable<PackageProjectionFile>
        FilterHostedFiles(IList<PackageProjectionFile> hostedFiles) {
            return hostedFiles.Where(Filter);
            
            bool 
            Filter(PackageProjectionFile hostedFile) {
                var relevantFiles = GetRelevantFiles(hostedFile);
                return !hostedFiles.TakeUntil(x => x == hostedFile).NotEqual(hostedFile).Any(x => ContainsAllFiles(x, relevantFiles));
            }
            
            List<string> 
            GetRelevantFiles(PackageProjectionFile hostedFile) => hostedFile.RelevantItemsIds.Where(IsItemRelevant).ToList();
            
            bool 
            ContainsAllFiles(PackageProjectionFile hostedFile,List<string> filesIds) => filesIds.All(x => hostedFile.RelevantItemsIds.Contains(x));
            
            bool 
            IsItemRelevant(string itemId) => fileIds.Contains(itemId);
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
            .Files
            .Where(x => !host.GetAllFiles().Contains(x.SubUrl)).ToList();

        hostedFilesToUpload.ForEach(x => host.UploadFile(x.SubUrl, x.SubUrl.Value.GetFileName().ToAbsoluteFilePath(@in.sourceDir).VerifyFileExistence())); 
    }

    internal static void
    RemovePackage(this IHost host, string packageId) {
        var package = packageId.DownloadPackageById(host);
        host.Remove(packageId.GetPackageFileOnHost());
        host.UpdateUpdatesManifest(x => x.RemovePackage(packageId));
        var projection = package.DownloadProjection(host);
        host.RemoveProjectionFiles(projection);
    }

    private static void
    RemoveProjectionFiles(this IHost host, PackageProjection projection) {
        host.Remove(projection.PackageId.GetPackageProjectionFileOnHost());

        var allProjectionsFiles = host.DownloadAllProjections()
            .NotEqualById(projection)
            .SelectMany(x => x.Files)
            .Select(x => x.SubUrl.Value).Distinct().ToList();

        foreach (var hostedFile in projection.Files)
            if (!allProjectionsFiles.Contains(hostedFile.SubUrl.Value))
                host.RemoveFiles(hostedFile.SubUrl.Value);

    }

    internal static void
    UpdateUpdatesManifest(this IHost host, Action<UpdatesManifest> update) {
        var updatesManifest = host.DownloadUpdatesManifestIfExists();
        if (updatesManifest != null) {
            update(updatesManifest);
            updatesManifest.UploadUpdateManifest(host);
        }
    }

    internal static void
    UploadUpdateNotesUtf8(this byte[] updateNotesUtf8, string packageName, string locale, IHost host) => 
        host.UploadBytes(Core.HostHelper.GetUpdateNotesFileOnHost(packageName, locale), updateNotesUtf8.Compress());
    
    public static List<RelativePath>
    GetAllFiles(this IHost host) => host.GetAllFiles("**/*");

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

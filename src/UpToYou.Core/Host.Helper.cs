using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace UpToYou.Core {
public static class HostHelper {

    internal static RelativePath UpdatesManifestPathOnHost => ".updates.proto.xz".ToRelativePath();
    internal static RelativePath NewsPathOnHost => ".news.json.zip".ToRelativePath();
    internal static string AllPackagesGlobPattern => $"packages\\*.package*";
    internal static string AllProjectionsGlobalPattern => $"projections\\*.projection*";

    internal static RelativePath
    GetPackageFileOnHost(this string packageId) => $"packages/{packageId}.package.proto.xz".ToRelativePath();

    internal static RelativePath
    GetPackageProjectionFileOnHost(this string packageId) => $"projections/{packageId}.projection.proto.xz".ToRelativePath();

    internal static RelativePath
    ToHostedFileSubUrlOnHost(this RelativePath path) => "data".AppendPath(path.Value).ToRelativePath();

    internal static RelativePath
    ToDeltasSubUrlOnHost(this RelativePath path) => "data\\deltas".AppendPath(path.Value).ToRelativePath();

    internal static RelativePath
    GetPathOnHost(this Package package) => package.Id.GetPackageFileOnHost();

    internal static RelativePath
    GetPathOnHost(this PackageProjection projection) => projection.PackageId.GetPackageProjectionFileOnHost();

    public static List<UpdateNotes>
    DownloadUpdateNotes(this List<PackageHeader> packages, string locale, IHostClient host) =>
        packages.AsParallel().Select(x => x.Name).Distinct()
            .SelectMany(x => host.DownloadUpdateNotes(x, locale)
                .ParseUpdateNotes()
                .Select(y => new UpdateNotes(x, y.version, y.notes))).ToList();
    
    public static List<UpdateNotes>
    DownloadUpdateNotes(this IHostClient host, IEnumerable<string> packageNames, string locale) =>
        packageNames.AsParallel()
            .SelectMany(x => host.DownloadUpdateNotes(x, locale)
                .ParseUpdateNotes()
                .Select(y => new UpdateNotes(x, y.version, y.notes))).ToList();

    public static string
    DownloadUpdateNotes(this IHostClient client, string packageName, string locale) => 
        client.DownloadBytes(GetUpdateNotesFileOnHost(packageName, locale)).Decompress().ToUtf8String();

    internal static RelativePath
    GetUpdateNotesFileOnHost(string packageName, string locale) => $"notes/{UpdateNotesHelper.GetUpdateNotesFileName(packageName, locale)}.xz".ToRelativePath();

    public static Package
    DownloadPackageById(this string packageId, IHostClient client) =>
        packageId.GetPackageFileOnHost().DownloadPackage(client);

    public static Package
    DownloadPackage(this RelativePath path, IHostClient client) =>
        client
           .DownloadBytes(path)
           .DecompressBasedOnFilePath(path.Value)
           .DeserializeProto<Package>();

    internal static IEnumerable<string>
    DownloadAllHostedFiles(this IEnumerable<PackageProjectionFile> hostedFiles, string outDirectory, IHostClient client) => 
        hostedFiles.AsParallel().Select(x => x.DownloadHostedFile(outDirectory, client));

    internal static PackageProjection 
    DownloadProjection(this Package package, IHostClient client) =>
        package.Id.GetPackageProjectionFileOnHost().DownloadProjection(client);

    internal static PackageProjection
    DownloadProjection(this RelativePath path, IHostClient client) => 
        path.DownloadBytes(client).Decompress().DeserializeProto<PackageProjection>();

    internal static string
    DownloadHostedFile(this PackageProjectionFile packageProjectionFile, string outDirectory, IHostClient client) =>
        packageProjectionFile.SubUrl.DownloadFile(client, outDirectory);

    public static string
    DownloadFile(this RelativePath path,  IHostClient client, string outDirectory) {
        var outFile =  path.ToAbsolute(outDirectory).CreateParentDirectoryIfAbsent();
        using var fileStream = File.Create(outFile);
        client.DownloadFile(path, fileStream);
        return outFile;
    }

    internal static byte[] 
    DownloadBytes(this RelativePath path, IHostClient client) => client.DownloadBytes(path);
    
    internal static byte[]
    DownloadBytes(this IHostClient client, RelativePath path) {
        using var memoryStream = new MemoryStream();
        client.DownloadFile(path, memoryStream);
        return memoryStream.ToArray();
    }

    internal static void 
    DownloadFile(this IHostClient client, RelativePath path, Stream outStream) =>
        client.DownloadFile(path, progress: NullProgress.Instance, CancellationToken.None, outStream);
    
    public static UpdatesManifest
    DownloadUpdatesManifest(this IHostClient client) =>
        UpdatesManifestPathOnHost
            .DownloadBytes(client)
            .Decompress()
            .DeserializeProto<UpdatesManifest>();
    
    public static string
    DownloadNews(this IHostClient client) =>
        NewsPathOnHost
        .DownloadBytes(client)
        .Decompress().ToUtf8String();
}
}

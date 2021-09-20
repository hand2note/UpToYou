using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace UpToYou.Core {
public static class PackageHostHelper {
    
    public const string UpdatesManifestFileName = ".updates";
    public const string ProtoExtension = ".proto";
    public const string PackageExtension = ".package";
    public const string PackagesHostingDirectory = "packages";
    
    public const string ProjectionExtension = ".projection";
    public const string ProjectionsHostDir = "projections";
    public const string ProjectionFilesHostDir = "data";
    public const string DeltaFilesHostDir = "data\\deltas";
    public const string UpdateNotesHostDir = "notes";

    internal static string AllPackagesGlobPattern => $"{PackagesHostingDirectory}\\*{PackageExtension}*";
    internal static string AllProjectionsGlobalPattern => $"{ProjectionsHostDir}\\*{ProjectionExtension}*";

    internal static RelativePath
    GetPackageFileOnHost(this string packageId) => 
        PackagesHostingDirectory.AppendPath(packageId)
          .AppendFileExtensions(PackageExtension, ProtoExtension, Compressing.DefaultCompressMethodFileExtension)
          .ToRelativePath();

    internal static RelativePath
    GetPackageProjectionFileOnHost(this string packageId) => 
        ProjectionsHostDir.AppendPath(packageId)
          .AppendFileExtensions(ProjectionExtension, ProtoExtension, Compressing.DefaultCompressMethodFileExtension)
          .ToRelativePath();

    internal static RelativePath
    ToHostedFileSubUrlOnHost(this RelativePath path) => ProjectionFilesHostDir.AppendPath(path.Value).ToRelativePath();

    internal static RelativePath
    ToDeltasSubUrlOnHost(this RelativePath path) => DeltaFilesHostDir.AppendPath(path.Value).ToRelativePath();

    internal static RelativePath
    UpdateManifestPathOnHost => 
        UpdatesManifestFileName
            .AppendFileExtensions(ProtoExtension, Compressing.DefaultCompressMethodFileExtension).ToRelativePath();

    internal static RelativePath
    GetPathOnHost(this Package package) => package.Id.GetPackageFileOnHost();

    internal static RelativePath
    GetPathOnHost(this PackageProjection projection) => 
        ProjectionsHostDir
            .AppendPath(projection.PackageId)
            .AppendFileExtensions(ProjectionExtension, ProtoExtension, Compressing.DefaultCompressMethodFileExtension)
            .ToRelativePath();

    public static UpdateManifest
    DownloadUpdatesManifest(this IHostClient client) =>
        UpdateManifestPathOnHost
            .DownloadBytes(client)
            .Decompress()
            .DeserializeProto<UpdateManifest>();

    public static List<UpdateNotes>
    DownloadUpdateNotes(this List<Update> updates, string locale, IHostClient host) =>
        updates.AsParallel().Select(x => x.PackageMetadata.Name).Distinct()
            .SelectMany(x => host.DownloadUpdateNotes(x, locale)
                .ParseUpdateNotes()
                .Select(y => new UpdateNotes(x, y.version, y.notes))).ToList();

    public static string
    DownloadUpdateNotes(this IHostClient client, string packageName, string locale) => 
        client.DownloadBytes(GetUpdateNotesFileOnHost(packageName, locale)).Decompress().ToUtf8String();

    internal static RelativePath
    GetUpdateNotesFileOnHost(string packageName, string locale) => 
        UpdateNotesHostDir
        .AppendPath(UpdateNotesHelper.GetUpdateNotesFileName(packageName, locale))
        .AppendFileExtension(Compressing.DefaultCompressMethodFileExtension).ToRelativePath();

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
    DownloadAllHostedFiles(this IEnumerable<HostedFile> hostedFiles, string outDirectory, IHostClient client) => 
        hostedFiles.AsParallel().Select(x => x.DownloadHostedFile(outDirectory, client));

    internal static PackageProjection 
    DownloadProjection(this Package package, IHostClient client) =>
        package.Id.GetPackageProjectionFileOnHost().DownloadProjection(client);

    internal static PackageProjection
    DownloadProjection(this RelativePath path, IHostClient client) => 
        path.DownloadBytes(client).Decompress().DeserializeProto<PackageProjection>();

    internal static string
    DownloadHostedFile(this HostedFile hostedFile, string outDirectory, IHostClient client) =>
        hostedFile.SubUrl.DownloadFile(client, outDirectory);

    internal static string
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
}
}

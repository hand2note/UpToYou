using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using ProtoBuf;

namespace UpToYou.Core {

public interface 
IFilesHostClient {
    string DownloadFile(ProgressContext? progress, RelativePath path, string outFile);
    byte[] DownloadData(ProgressContext? pCtx, RelativePath path);
}

public class
PackageHostClientContext {
    public IFilesHostClient FilesHost { get; }
    public ILogger? Log { get; }
    public ProgressContext? ProgressContext { get; }

    public PackageHostClientContext(IFilesHostClient filesHost, ILogger? log, ProgressContext? progressContext) {
        FilesHost = filesHost;
        Log = log;
        ProgressContext = progressContext;
    }
}

public static class PackageHost {
    public const string UpdatesManifestFileName = ".updates";
    public const string ProtoExtension = ".proto";
    public const string PackageExtension = ".package";
    public const string PackagesHostingDir = "packages";
    
    public const string ProjectionExtension = ".projection";
    public const string ProjectionsHostDir = "projections";
    public const string ProjectionFilesHostDir = "data";
    public const string DeltaFilesHostDir = "data\\deltas";
    public const string UpdateNotesHostDir = "notes";

    internal static string AllPackagesGlobPattern => $"{PackagesHostingDir}\\*{PackageExtension}*";
    internal static string AllProjectionsGlobalPattern => $"{ProjectionsHostDir}\\*{ProjectionExtension}*";

    internal static RelativePath
    PackageIdToPackageFileOnHost(this string id) => 
        PackagesHostingDir.AppendPath(id)
          .AppendFileExtensions(PackageExtension, ProtoExtension, Compressing.DefaultCompressMethodFileExtension)
          .ToRelativePath();

    internal static RelativePath
    PackageIdToProjectionFileOnHost(this string id) => 
        ProjectionsHostDir.AppendPath(id)
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
    GetPathOnHost(this Package package) => package.Id.PackageIdToPackageFileOnHost();

    internal static RelativePath
    GetPathOnHost(this PackageProjection projection) => 
        ProjectionsHostDir
            .AppendPath(projection.PackageId)
            .AppendFileExtensions(ProjectionExtension, ProtoExtension, Compressing.DefaultCompressMethodFileExtension)
            .ToRelativePath();

    public static Task<UpdatesManifest>
    DownloadUpdatesManifestAsync(this PackageHostClientContext ctx) => Task.Run(ctx.DownloadUpdatesManifest);

    public static bool
    TryDownloadUpdatesManifest(this PackageHostClientContext ctx, out UpdatesManifest? result) {
        try {
            result = ctx.DownloadUpdatesManifest();
            return true;
        }
        catch (Exception ex) when(ex is InvalidRemoteDataException) {
            result = null;
            return false;
        }
    }

    public static UpdatesManifest
    DownloadUpdatesManifest(this PackageHostClientContext ctx) =>
        UpdateManifestPathOnHost
            .DownloadData(ctx)
            .Decompress()
            .DeserializeProto<UpdatesManifest>();

    public static List<UpdateNotes>
    DownloadUpdateNotes(this List<Update> updates, string locale, IFilesHostClient host) =>
        updates.AsParallel().Select(x => x.PackageMetadata.Name).Distinct()
            .SelectMany(x => host.DownloadUpdateNotes(x, locale)
                .ParseUpdateNotes()
                .Select(y => new UpdateNotes(x, y.version, y.notes))).ToList();

    public static string
    DownloadUpdateNotes(this IFilesHostClient host, string? packageName = null, string? locale = null) => 
        host.DownloadData(null, GetUpdateNotesFileOnHost(packageName, locale)).Decompress().ToUtf8String();

    internal static RelativePath
    GetUpdateNotesFileOnHost(string? packageName, string? locale) => 
        UpdateNotesHostDir
        .AppendPath(UpdateNotesModule.GetUpdateNotesFileName(packageName, locale))
        .AppendFileExtension(Compressing.DefaultCompressMethodFileExtension).ToRelativePath();

    public static async Task<Package>
    DownloadPackageByIdAsync(this string packageId, PackageHostClientContext ctx) => await Task.Run(() => packageId.DownloadPackageById(ctx));

    public static Package
    DownloadPackageById(this string packageId, PackageHostClientContext ctx) {
        try {
            return packageId.PackageIdToPackageFileOnHost().DownloadPackageOrThrow(ctx);
        }
        catch (FileNotFoundException ex) {
            throw new InvalidOperationException($"Package with id {packageId} not found on the host", ex);
        }
    }

    public static Package
    DownloadPackage(this Update update, PackageHostClientContext ctx) =>
        update.PackageMetadata.Id.PackageIdToPackageFileOnHost().DownloadPackage(ctx) 
        ?? throw new InvalidRemoteDataException($"Package {update.PackageMetadata.Version} with id = {update.PackageMetadata.Id} not found on the host");

    private static Package
    DownloadPackageOrThrow(this RelativePath path, PackageHostClientContext ctx) =>
        path.DownloadPackage(ctx) ?? throw new InvalidRemoteDataException($"Failed to download package {path}");

    public static Package?
    DownloadPackage(this RelativePath path, PackageHostClientContext ctx) =>
        ctx.FilesHost
           .DownloadData(ctx.ProgressContext, path)
           .DecompressBasedOnFilePath(path.Value)
           .TryDeserialize<Package>(path.Value, ctx.Log);

    internal static IEnumerable<string>
    DownloadAll(this IEnumerable<HostedFile> hostedFiles, PackageHostClientContext ctx, string outDir) => hostedFiles.AsParallel().Select(x => x.Download(ctx, outDir));

    internal static PackageProjection 
    DownloadProjection(this Package package, PackageHostClientContext ctx) =>
        package.Id .PackageIdToProjectionFileOnHost().DownloadProjection(ctx);

    internal static PackageProjection
    DownloadProjection(this RelativePath path, PackageHostClientContext ctx) => 
        path.DownloadData(ctx).Decompress().DeserializeProto<PackageProjection>();

    internal static string
    Download(this HostedFile hostedFile, PackageHostClientContext ctx, string outDir) =>
        hostedFile.SubUrl.DownloadFile(ctx, outDir);

    internal static string
    DownloadFile(this RelativePath path,  PackageHostClientContext ctx, string outDir) => ctx.FilesHost.DownloadFile(ctx.ProgressContext, path, path.ToAbsolute(outDir).CreateParentDirectoryIfAbsent());

    internal static byte[] 
    DownloadData(this RelativePath file, PackageHostClientContext ctx) =>
        ctx.FilesHost.DownloadData(ctx.ProgressContext, file);


    internal static T? 
    TryDeserialize<T>(this byte[] bytes, string? file = null, ILogger? log = null) where T: class {
        try {
            return bytes.DeserializeProto<T>();
        }
#pragma warning disable 168
        catch (ProtoException ex) {
#pragma warning restore 168
#if DEBUG
            throw;
            
#else
            log?.LogException($"Failed to deserialize {file}", ex);
            return null;
#endif
        }
    }
}
}

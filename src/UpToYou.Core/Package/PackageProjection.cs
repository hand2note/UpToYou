using System.Collections.Generic;
using System.IO;
using System.Linq;
using ProtoBuf;

namespace UpToYou.Core {

[ProtoContract]
internal class
PackageFileDelta {
    [ProtoMember(1)]
    public string OldHash { get; }

    [ProtoMember(2)]
    public string NewHash { get; }

    [ProtoMember(3)]
    public string PackageItemId { get; }

#pragma warning disable CS8618 // Non-nullable field is uninitialized.
    protected PackageFileDelta() { }
#pragma warning restore CS8618 // Non-nullable field is uninitialized.

    public PackageFileDelta(string oldHash, string newHash, string packageItemId) =>
        (OldHash,NewHash, PackageItemId) = (oldHash, newHash, packageItemId);
}

// ReSharper disable once ArrangeAttributes
[ProtoContract]
[ProtoInclude(100, typeof(PackageItemsHostedFileContent))]
[ProtoInclude(101, typeof(PackageFileDeltasHostedFileContent))]
internal interface
    IHostedFileContent {
    IEnumerable<string> RelevantItemsIds { get; }
}

[ProtoContract]
internal class
    PackageItemsHostedFileContent : IHostedFileContent {
    [ProtoMember(1)]
    public List<string> PackageItems { get; }

    protected PackageItemsHostedFileContent() => PackageItems =new List<string>();

    public PackageItemsHostedFileContent(List<string> packageItems) => PackageItems = packageItems;
    
    public IEnumerable<string> RelevantItemsIds => PackageItems;
}

[ProtoContract]
internal class
    PackageFileDeltasHostedFileContent : IHostedFileContent {
    [ProtoMember(1)]
    public List<PackageFileDelta> PackageFileDeltas {get;}

    protected PackageFileDeltasHostedFileContent() => PackageFileDeltas = new List<PackageFileDelta>();

    public PackageFileDeltasHostedFileContent(List<PackageFileDelta> packageFileDeltas) => PackageFileDeltas = packageFileDeltas;
    
    public IEnumerable<string>  
    RelevantItemsIds => PackageFileDeltas.Select(x => x.PackageItemId);
}

[ProtoContract]
internal class
    HostedFile {
    [ProtoMember(1)]
    public string Id { get; }

    [ProtoMember(2)]
    public RelativePath SubUrl { get; }

    [ProtoMember(3)]
    public string FileHash { get; }

    [ProtoMember(4)]
    public long FileSize { get; }

    [ProtoMember(5)]
    public IHostedFileContent Content { get; }

#pragma warning disable CS8618 // Non-nullable field is uninitialized.
    protected HostedFile() { }
#pragma warning restore CS8618 // Non-nullable field is uninitialized.

    public HostedFile(string id, RelativePath subUrl, string fileHash, long fileSize, IHostedFileContent content) =>
        (Id, SubUrl, FileHash,FileSize, Content) = (id, subUrl, fileHash, fileSize, content);

    public IEnumerable<string> 
        RelevantItemsIds => Content.RelevantItemsIds;
}

[ProtoContract]
internal class 
PackageProjection: IHasUniqueId {
    [ProtoMember(1)]
    public string Id { get; }

    [ProtoMember(2)]
    public string PackageId { get; }

    [ProtoMember(3)]
    public string RootUrl { get; }

    [ProtoMember(4)]
    public List<HostedFile> HostedFiles  { get; }

#pragma warning disable CS8618 // Non-nullable field is uninitialized.
    protected  PackageProjection() => HostedFiles = new List<HostedFile>();
#pragma warning restore CS8618 // Non-nullable field is uninitialized.
    public PackageProjection(string id, string packageId, string rootUrl, List<HostedFile> hostedFiles) =>
        (Id, PackageId, RootUrl, HostedFiles) = (id, packageId, rootUrl, hostedFiles);

    public const string DeltaExtension = ".delta";
    public static string GetDeltaFileName(string oldHash, string newHash) => $"{oldHash}.{newHash}{DeltaExtension}";
    //public const string PackageFileExtension = ".pfile";
}

internal static class PackageProjectionEx {
    public static bool 
    IsPackageDeltaFile(this string file) => file.FileContainsExtension(PackageProjection.DeltaExtension);

    public static bool
    IsUnpackedDeltaFile(this string file) => file.FileHasExtension(PackageProjection.DeltaExtension);

    public static void
    ExtractAllHostedFiles(this IEnumerable<string> hostedFiles, string outDir) {
        foreach (var hostedFile in hostedFiles) 
            hostedFile.ExtractHostedFile(outDir);
    }

    public static void 
    ExtractHostedFile(this string hostedFile, string outDir) {
        using var fs = hostedFile.OpenFileForRead();
        if (hostedFile.TryParseFileCompressMethod(out var method)) {
            var ms = new MemoryStream();
            fs.DecompressStream(ms, method);
            ms.Position = 0;
            ms.ExtractArchive(outDir);
        }
        else 
            fs.ExtractArchive(outDir);

    }
    
}

}
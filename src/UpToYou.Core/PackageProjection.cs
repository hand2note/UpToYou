using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using ProtoBuf;
#pragma warning disable 8618

namespace UpToYou.Core {

/// <summary>
/// Contains information about how a <see cref="Package"/> is stored on the host.
/// </summary>
[ProtoContract]
internal class 
PackageProjection: IHasUniqueId {
    [ProtoMember(1)] public string Id { get; }
    [ProtoMember(2)] public string PackageId { get; }
    [ProtoMember(3)] public ImmutableList<PackageProjectionFile> Files  { get; }
    public PackageProjection(string id, string packageId,  ImmutableList<PackageProjectionFile> files) {
        Id = id;
        PackageId = packageId;
        Files = files;
    }

    protected  PackageProjection() => Files = ImmutableList<PackageProjectionFile>.Empty;

    public const string DeltaExtension = ".delta";
    public static string GetDeltaFileName(string oldHash, string newHash) => $"{oldHash}.{newHash}{DeltaExtension}";
}

/// <summary>
/// Represents a file stored on the host. The file may be an archive of multiple <see cref="PackageFile"/> as well as
/// store a set of deltas.
/// </summary>
[ProtoContract(SkipConstructor = true)]
internal class
PackageProjectionFile {
    [ProtoMember(1)] public string Id { get; }
    [ProtoMember(2)] public RelativePath SubUrl { get; }
    [ProtoMember(3)] public string FileHash { get; }
    [ProtoMember(4)] public long FileSize { get; }
    [ProtoMember(5)] public IProjectionFileContent Content { get; }
    public PackageProjectionFile(string id, RelativePath subUrl, string fileHash, long fileSize, IProjectionFileContent content) {
        Id = id;
        SubUrl = subUrl;
        FileHash = fileHash;
        FileSize = fileSize;
        Content = content;
    }

    public IEnumerable<string>  RelevantItemsIds => Content.RelevantPackageFileIds;
}

[ProtoContract(SkipConstructor = true)]
internal class
PackageFileDelta {
    [ProtoMember(1)] public string OldHash { get; }
    [ProtoMember(2)] public string NewHash { get; }
    [ProtoMember(3)] public string PackageFileId { get; }
    public PackageFileDelta(string oldHash, string newHash, string packageFileId) {
        OldHash = oldHash;
        NewHash = newHash;
        PackageFileId = packageFileId;
    }
}

[ProtoContract]
[ProtoInclude(100, typeof(PackageProjectionFileContent))]
[ProtoInclude(101, typeof(PackageProjectionFileDeltaContent))]
internal interface
IProjectionFileContent {
    IEnumerable<string> RelevantPackageFileIds { get; }
}

[ProtoContract]
internal class
PackageProjectionFileContent : IProjectionFileContent {
    [ProtoMember(1)] 
    public ImmutableList<string> PackageFileIds { get; }
    public PackageProjectionFileContent(ImmutableList<string> packageFileIds) => PackageFileIds = packageFileIds;

    protected PackageProjectionFileContent() => PackageFileIds = ImmutableList<string>.Empty;
    
    public IEnumerable<string> RelevantPackageFileIds => PackageFileIds;
}

[ProtoContract]
internal class
PackageProjectionFileDeltaContent : IProjectionFileContent {
    [ProtoMember(1)] 
    public ImmutableList<PackageFileDelta> PackageFileDeltas {get;}
    public PackageProjectionFileDeltaContent(ImmutableList<PackageFileDelta> packageFileDeltas) => PackageFileDeltas = packageFileDeltas;
    
    public IEnumerable<string> RelevantPackageFileIds => PackageFileDeltas.Select(x => x.PackageFileId);
}
}
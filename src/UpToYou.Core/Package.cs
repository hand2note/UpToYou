using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics.Contracts;
using System.Linq;
using ProtoBuf;

namespace UpToYou.Core {
    
[ProtoContract]
public class Package {
    [ProtoMember(1)] public PackageMetadata Metadata { get; }
    [ProtoMember(2)] public Dictionary<RelativePath, PackageFile> Files { get; }
    public Package(PackageMetadata metadata, Dictionary<RelativePath, PackageFile> files) {
        Metadata = metadata;
        Files = files;
        _idToFile = new Lazy<Dictionary<string, PackageFile>>(() => Files.Values.ToDictionary(x => x.Id, x => x));
    }

    #pragma warning disable CS8625 // Cannot convert null literal to non-nullable reference type.
    protected Package():this(null, null) { }
    #pragma warning restore CS8625 // Cannot convert null literal to non-nullable reference type.

    public string Id => Metadata.Id;
    public Version Version => Metadata.Version;
    public string Name => Metadata.Name;

    private readonly Lazy<Dictionary<string, PackageFile>> _idToFile;
    public PackageFile GetFileById(string id) => _idToFile.Value.TryGetValue(id, out var res) ? res : throw new InvalidOperationException($"PackageFile with id = {id.Quoted()} not found");
    public PackageFile? TryGetFileById(string id) => _idToFile.Value.TryGetValue(id, out var res) ? res : null;
    public PackageFile? TryGetFileByPath(RelativePath path) => Files.TryGetValue(path, out var res) ? res : null;
    public PackageFile GetFileByPath(RelativePath path) => Files.TryGetValue(path, out var res) ? res : throw new InvalidOperationException($"File by path {path.Value.Quoted()} not found");

    internal IEnumerable<PackageFile> 
    GetFiles(RelativeGlob glob) => 
        Files.Values.Where(x => x.Path.Matches(glob));

    public override string ToString() => Metadata.ToString();
}

[ProtoContract]
public class
PackageFile {
    [ProtoMember(1)] public string Id {get;}
    [ProtoMember(2)] public RelativePath Path {get;}
    [ProtoMember(3)] public long FileSize {get;}
    [ProtoMember(4)] public string FileHash {get;}
    [ProtoMember(5)] public Version? FileVersion {get;}
    public PackageFile(string id, RelativePath path, long fileSize, string fileHash, Version? fileVersion) =>
        (Id, Path, FileSize, FileHash, FileVersion) = (id, path, fileSize, fileHash, fileVersion);
    
    protected PackageFile() => (Id, FileHash) = ("", "");
    
    public string 
    GetFile(string srcDir) => Path.ToAbsolute(srcDir);
    
    public void 
    Verify(string path) {
        Contract.Assert(path.GetFileHash() == FileHash, $"Hash of {Path.Value.Quoted()} is not equal expected");
        Contract.Assert(FileVersion == null || path.GetFileVersion() == FileVersion, $"Expected {FileVersion} of {Path.Value.Quoted()} but was {path.GetFileVersion()?.ToString().Quoted()}");
    }

    public override string ToString() => Path.Value;
}



[ProtoContract]
public class
PackageMetadata: IHasCustomProperties{
    [ProtoMember(1)] public string Id { get; }
    [ProtoMember(2)] public string Name { get; }
    [ProtoMember(3)] public Version Version { get; }
    [ProtoMember(4)] public DateTime DatePublished { get; }
    [ProtoMember(5)] public PackageFile VersionProviderFile { get; }
    [ProtoMember(6)] public ImmutableDictionary<string, string> CustomProperties { get; }
    public PackageMetadata(string id, string name, Version version, DateTime datePublished, PackageFile versionProviderFile, ImmutableDictionary<string, string> customProperties) {
        Id = id;
        Name = name;
        Version = version;
        DatePublished = datePublished;
        VersionProviderFile = versionProviderFile;
        CustomProperties = customProperties;
    }

    // ReSharper disable once UnusedMember.Global
    #pragma warning disable CS8618 // Non-nullable field is uninitialized.
    protected PackageMetadata() {
        Name = string.Empty;
        CustomProperties = ImmutableDictionary<string, string>.Empty;
    }
    #pragma warning restore CS8618 // Non-nullable field is uninitialized.

    public bool 
    IsSamePackage(PackageMetadata other) =>
        Version == other.Version &&
        Name == other.Name;

    public bool 
    IsSamePackage(Version version, string? name) =>
        Version == version && (string.IsNullOrWhiteSpace(Name) ? string.IsNullOrWhiteSpace(name) :  Name == name);

    public override string ToString() => $"{Name}, {Version}";
}



}

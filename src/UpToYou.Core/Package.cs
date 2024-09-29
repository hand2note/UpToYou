using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics.Contracts;
using System.Linq;
using ProtoBuf;
#pragma warning disable 8618

namespace UpToYou.Core {
    
[ProtoContract]
public class Package {
    [ProtoMember(1)] public PackageHeader Header { get; }
    [ProtoMember(2)] public ImmutableDictionary<RelativePath, PackageFile> Files { get; }
    public Package(PackageHeader header, ImmutableDictionary<RelativePath, PackageFile> files) {
        Header = header;
        Files = files;
        _idToFile = new Lazy<Dictionary<string, PackageFile>>(() => Files.Values.ToDictionary(x => x.Id, x => x));
    }

    protected Package() => Files = ImmutableDictionary<RelativePath, PackageFile>.Empty;

    public string Id => Header.Id;
    public Version Version => Header.Version;
    public string Name => Header.Name;

    private readonly Lazy<Dictionary<string, PackageFile>> _idToFile;
    public PackageFile GetFileById(string id) => _idToFile.Value.TryGetValue(id, out var res) ? res : throw new InvalidOperationException($"PackageFile with id = {id.Quoted()} not found");
    public PackageFile? TryGetFileById(string id) => _idToFile.Value.TryGetValue(id, out var res) ? res : null;
    public PackageFile? TryGetFileByPath(RelativePath path) => Files.TryGetValue(path, out var res) ? res : null;
    public PackageFile GetFileByPath(RelativePath path) => Files.TryGetValue(path, out var res) ? res : throw new InvalidOperationException($"File by path {path.Value.Quoted()} not found");

    internal IEnumerable<PackageFile> 
    GetFiles(RelativeGlob glob) => 
        Files.Values.Where(x => x.Path.Matches(glob));

    public override string ToString() => Header.ToString();
}

[ProtoContract(SkipConstructor = true)]
public class
PackageFile {
    [ProtoMember(1)] public string Id {get;}
    [ProtoMember(2)] public RelativePath Path {get;}
    [ProtoMember(3)] public long FileSize {get;}
    [ProtoMember(4)] public string FileHash {get;}
    [ProtoMember(5)] public Version? FileVersion {get;}
    public PackageFile(string id, RelativePath path, long fileSize, string fileHash, Version? fileVersion) {
        Id = id;
        Path = path;
        FileSize = fileSize;
        FileHash = fileHash;
        FileVersion = fileVersion;
    }
    
    public string GetFile(string srcDir) => Path.ToAbsolute(srcDir);
    public override string ToString() => Path.Value;
}

[ProtoContract]
public class
PackageHeader: IHasCustomProperties{
    [ProtoMember(1)] public string Id { get; }
    [ProtoMember(2)] public string Name { get; }
    [ProtoMember(3)] public Version Version { get; }
    [ProtoMember(4)] public DateTime DatePublished { get; }
    [ProtoMember(5)] public PackageFile VersionProviderFile { get; }
    [ProtoMember(6)] public ImmutableDictionary<string, string> CustomProperties { get; }
    public PackageHeader(string id, string name, Version version, DateTime datePublished, PackageFile versionProviderFile, ImmutableDictionary<string, string> customProperties) {
        Id = id;
        Name = name;
        Version = version;
        DatePublished = datePublished;
        VersionProviderFile = versionProviderFile;
        CustomProperties = customProperties;
    }

    protected PackageHeader() => CustomProperties = ImmutableDictionary<string, string>.Empty;

    public bool 
    IsSamePackage(PackageHeader other) =>
        Version == other.Version &&
        Name == other.Name;

    public bool 
    IsSamePackage(Version version, string? name) =>
        Version.VersionEquals(version) && (string.IsNullOrWhiteSpace(Name) ? string.IsNullOrWhiteSpace(name) :  Name == name);

    public override string ToString() => $"{Name}, {Version}";
}



}

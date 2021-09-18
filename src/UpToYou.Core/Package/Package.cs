using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;
using ProtoBuf;

namespace UpToYou.Core {

[ProtoContract]
public class
PackageFile {
    [ProtoMember(1)]
    public string Id {get;}
    [ProtoMember(2)]
    public RelativePath Path {get;}
    [ProtoMember(3)]
    public long FileSize {get;}
    [ProtoMember(4)]
    public string FileHash {get;}
    [ProtoMember(5)]
    public Version? FileVersion {get;}

    protected PackageFile() => (Id, FileHash) = ("", "");
    public PackageFile(string id, RelativePath path, long fileSize, string fileHash, Version? fileVersion) =>
        (Id, Path, FileSize, FileHash, FileVersion) = (id, path, fileSize, fileHash, fileVersion);
    
    public string GetFile(string srcDir) => Path.ToAbsolute(srcDir);
    public void Verify(string path) {
        Contract.Assert(path.GetFileHash() == FileHash, $"Hash of {Path.Value.Quoted()} is not equal expected");
        Contract.Assert(FileVersion == null || path.GetFileVersion() == FileVersion, $"Expected {FileVersion} of {Path.Value.Quoted()} but was {path.GetFileVersion()?.ToString().Quoted()}");
    }

    public override string ToString() => Path.Value;
}



[ProtoContract]
public class
PackageMetadata: IHasCustomProperties{

    [ProtoMember(1)]
    public string Id { get; }

    [ProtoMember(2)]
    public string Name { get; }

    [ProtoMember(3)]
    public Version Version { get; }

    [ProtoMember(4)] 
    public DateTime DateBuilt { get; }

    [ProtoMember(5)] 
    public PackageFile VersionProviderFile { get; }

    [ProtoMember(6)] 
    public Dictionary<string, string>? CustomProperties { get; }

    //[ProtoMember(7)] 
    //public List<PackageDependency> Dependencies { get; }

    // ReSharper disable once UnusedMember.Global
#pragma warning disable CS8618 // Non-nullable field is uninitialized.
    protected PackageMetadata() => Name = string.Empty;
#pragma warning restore CS8618 // Non-nullable field is uninitialized.

    public bool 
    IsSamePackage(PackageMetadata other) =>
        Version == other.Version &&
        Name == other.Name;

    public bool 
    IsSamePackage(Version version, string? name) =>
        Version == version && (string.IsNullOrWhiteSpace(Name) ? string.IsNullOrWhiteSpace(name) :  Name == name);

    public PackageMetadata(string id, string name,  Version version, DateTime dateBuilt, PackageFile versionProviderFile, Dictionary<string, string>? customProperties = null) {
        Id = id;
        Name = name;
        DateBuilt = dateBuilt;
        VersionProviderFile = versionProviderFile;
        CustomProperties = customProperties;
        Version = version;
    }

    public override string ToString() => $"{Name}, {Version}";
}

[ProtoContract]
public class Package {
    [ProtoMember(1)]
    public PackageMetadata Metadata { get; }
    [ProtoMember(2)]
    public Dictionary<RelativePath, PackageFile> Files { get; }

    //Constructor for protobuf
    // ReSharper disable once UnusedMember.Global
#pragma warning disable CS8618 // Non-nullable field is uninitialized.
#pragma warning disable CS8625 // Cannot convert null literal to non-nullable reference type.
    protected Package():this(null, null) { }
#pragma warning restore CS8625 // Cannot convert null literal to non-nullable reference type.
#pragma warning restore CS8618 // Non-nullable field is uninitialized.

    public Package(PackageMetadata metadata, Dictionary<RelativePath, PackageFile> files) {
        Metadata = metadata;
        Files = files;
        _filesById = new Lazy<Dictionary<string, PackageFile>>(() => Files.Values.ToDictionary(x => x.Id, x => x));
    }

    public string Id => Metadata.Id;
    public Version Version => Metadata.Version;
    public string Name => Metadata.Name;

    //public (RelativePath file, string hash) 
    //GetVersionProvider() => (Metadata.VersionProvider, FindFileByPath(Metadata.VersionProvider)?.FileHash 
    //    ?? throw new InvalidDataException($"Failed to find version provider file hash ({Metadata.VersionProvider}) "));

    private readonly Lazy<Dictionary<string, PackageFile>> _filesById;
    public PackageFile GetFileById(string id) => _filesById.Value.TryGetValue(id, out var res) ? res : throw new InvalidPackageDataException($"PackageFile with id = {id.Quoted()} not found");
    public PackageFile? FindFileById(string id) => _filesById.Value.TryGetValue(id, out var res) ? res : null;
    public PackageFile? FindFileByPath(RelativePath path) => Files.TryGetValue(path, out var res) ? res : null;
    public PackageFile GetFileByPath(RelativePath path) => Files.TryGetValue(path, out var res) ? res : throw new InvalidOperationException($"File by path {path.Value.Quoted()} not found");

    internal IEnumerable<PackageFile> FindFiles(RelativeGlob glob) => 
        Files.Values.Where(x => x.Path.Matches(glob));

    public List<PackageFile>? FindChildFiles(RelativePath path) {
        var res = new List<PackageFile>();
        var singleFile = FindFileByPath(path);
        if (singleFile != null)
            res.Add(singleFile);
        else {
            bool found = false;
            foreach (var packageFile in Files.Values)
                if (path.IsParentTo(packageFile.Path)) {
                    found = true;
                    res.Add(packageFile);
                }
            
            if (!found)
                return null;
        } 
        return res;
    }

    public string CopyFiles(string sourceDirectory, string outDirectory) {
        foreach (var filePath in Files.Keys)
            filePath.ToAbsolute(sourceDirectory).VerifyFileExistence().CopyFile(filePath.ToAbsolute(outDirectory));
        return outDirectory;
    }

    public bool IsDifferent(Package other) {
        if (other.Files.Count != Files.Count)
            return true;

        foreach (var file in Files.Values) {
            var otherFile = other.FindFileByPath(file.Path);
            if (otherFile == null || otherFile.FileHash != file.FileHash)
                return true;
        }
        return false;
    }

    public override string ToString() => Metadata.ToString();
}


}

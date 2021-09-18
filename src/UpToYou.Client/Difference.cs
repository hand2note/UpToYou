using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UpToYou.Core;

namespace UpToYou.Client
{

internal interface 
IPackageItemDifference {
    string PackageItemId { get; }
    bool IsDifferent { get; }
}

internal class 
ActualFileState {
    public string Path { get; }
    public bool Exists{ get; }
    public string Hash{ get; }
    public long Size{ get; }
    public Version? Version{ get; }
    public ActualFileState(string path, bool exists, string hash, long size, Version? version) =>
        (Path, Exists, Hash, Size, Version) = (path, exists, hash, size, version);
}

internal class 
PackageFileDifference: IPackageItemDifference {
    public ActualFileState ActualFileState { get; }
    public PackageFile PackageFile { get; }

    public PackageFileDifference(ActualFileState actualFileState, PackageFile packageFile) =>
        (ActualFileState, PackageFile) = (actualFileState, packageFile);

    public string 
    PackageItemId => PackageFile.Id;

    public bool 
    IsDifferent => 
        !ActualFileState.Exists ||
        ActualFileState.Version != null && PackageFile.FileVersion != null && !ActualFileState.Version.Equals(PackageFile.FileVersion)
        || ActualFileState.Hash != PackageFile.FileHash;
}

internal class
BuildDifferenceCache {
    private readonly Dictionary<string, ActualFileState> _fileStates = new Dictionary<string, ActualFileState>();

    public void Add(ActualFileState state) {
        var path = state.Path.ToLower();
        if (!_fileStates.ContainsKey(path))
            _fileStates.Add(path, state);
    }

    public ActualFileState? 
    FindFileState(string file) => _fileStates.TryGetValue(file.ToLower(), out var res) ? res : null;
}

internal class 
PackageDifference {
    public Package Package { get; }
    public List<PackageFileDifference> FileDifferences{ get; }

    public PackageDifference(Package package, List<PackageFileDifference> fileDifferences) =>
        (Package, FileDifferences) = (package, fileDifferences);

    public bool 
    IsDifferent() => FileDifferences.Any(x => x.IsDifferent);

    public IEnumerable<PackageFileDifference> 
    DifferentFiles => FileDifferences.Where(x => x.IsDifferent);

    public IEnumerable<string> 
    DifferentFilesIds => DifferentFiles.Select(x => x.PackageItemId);
}

internal static class PackageDifferenceModule{

    public static PackageDifference
    GetDifference(this Package package, string programDirectory, bool toCache) =>
        new BuildDifferenceContext(programDirectory, package, toCache ? new BuildDifferenceCache() : null).GetDifference();

    public static PackageDifference
    GetDifference(this Package package, string programDirectory, BuildDifferenceCache? cache = null) =>
        new BuildDifferenceContext(programDirectory, package, cache).GetDifference();
    
    private class 
    BuildDifferenceContext {
        public string RootDirectory { get; }
        public Package Package {get;}
        public BuildDifferenceCache? Cache { get; }

        public BuildDifferenceContext(string rootDirectory, Package package, BuildDifferenceCache? cache) => 
            (RootDirectory, Package, Cache) = (rootDirectory, package, cache);
    }

    private static PackageDifference
    GetDifference(this BuildDifferenceContext ctx) => 
        new PackageDifference(
            package:ctx.Package,
            fileDifferences:ctx.Package.Files.Values.Select(x => x.GetDifference(ctx)).ToList());

    private static PackageFileDifference
    GetDifference(this PackageFile packageFile, BuildDifferenceContext ctx) => 
        new PackageFileDifference(
            actualFileState:packageFile.Path.GetActualFileState(ctx),
            packageFile:packageFile);

    private static ActualFileState
    GetActualFileState(this RelativePath path, BuildDifferenceContext ctx) {
        var file = path.ToAbsolute(ctx.RootDirectory);  
        return ctx.Cache?.FindFileState(file) ?? file.GetActualFileState().Cache(ctx);
    }

    private static ActualFileState
    Cache(this ActualFileState state, BuildDifferenceContext ctx) {
        ctx.Cache?.Add(state);
        return state;
    }

    private static ActualFileState
    GetActualFileState(this string file) {
        bool exists = File.Exists(file);
        return new ActualFileState(
            path: file,
            exists:exists,
            hash:exists ? file.GetFileHash() : "",
            size:exists ? file.GetFileSize() : 0,
            version:exists ? file.GetFileVersion() : null);
    }
}

}

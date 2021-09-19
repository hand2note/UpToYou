using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UpToYou.Core;

namespace UpToYou.Client {
public class 
ActualFileState {
    public string Path { get; }
    public bool Exists{ get; }
    public string Hash{ get; }
    public long Size{ get; }
    public Version? Version{ get; }
    public ActualFileState(string path, bool exists, string hash, long size, Version? version) =>
        (Path, Exists, Hash, Size, Version) = (path, exists, hash, size, version);
}

public class 
PackageFileDifference {
    public ActualFileState ActualFileState { get; }
    public PackageFile PackageFile { get; }

    public PackageFileDifference(ActualFileState actualFileState, PackageFile packageFile) =>
        (ActualFileState, PackageFile) = (actualFileState, packageFile);

    public string PackageItemId => PackageFile.Id;

    public bool 
    IsDifferent => 
        !ActualFileState.Exists ||
        ActualFileState.Version != null && PackageFile.FileVersion != null && !ActualFileState.Version.Equals(PackageFile.FileVersion)
        || ActualFileState.Hash != PackageFile.FileHash;
}

internal class
BuildDifferenceCache {
    private readonly Dictionary<string, ActualFileState> _fileStates = new();

    public void Add(ActualFileState state) {
        var path = state.Path.ToLower();
        if (!_fileStates.ContainsKey(path))
            _fileStates.Add(path, state);
    }

    public ActualFileState? 
    FindFileState(string file) => _fileStates.TryGetValue(file.ToLower(), out var res) ? res : null;
}

public class 
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



}

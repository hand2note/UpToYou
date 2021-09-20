using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UpToYou.Core;

namespace UpToYou.Client {
internal static class 
DifferenceHelper{

    public static PackageDifference
    GetDifference(this Package package, string programDirectory, bool cache) =>
        new PackageDifferenceBuilder(programDirectory, package, cache ? new BuildDifferenceCache() : null).GetDifference();

    public static PackageDifference
    GetDifference(this Package package, string programDirectory, BuildDifferenceCache? cache = null) =>
        new PackageDifferenceBuilder(programDirectory, package, cache).GetDifference();
    
    private class 
    PackageDifferenceBuilder {
        public string RootDirectory { get; }
        public Package Package {get;}
        public BuildDifferenceCache? Cache { get; }

        public PackageDifferenceBuilder(string rootDirectory, Package package, BuildDifferenceCache? cache) => 
            (RootDirectory, Package, Cache) = (rootDirectory, package, cache);
    }

    private static PackageDifference
    GetDifference(this PackageDifferenceBuilder builder) => 
        new PackageDifference(
            package:builder.Package,
            fileDifferences:builder.Package.Files.Values.Select(x => x.GetDifference(builder)).ToList());

    private static PackageFileDifference
    GetDifference(this PackageFile packageFile, PackageDifferenceBuilder builder) => 
        new PackageFileDifference(
            actualFileState:packageFile.Path.GetActualFileState(builder),
            packageFile:packageFile);

    private static ActualFileState
    GetActualFileState(this RelativePath path, PackageDifferenceBuilder builder) {
        var file = path.ToAbsolute(builder.RootDirectory);  
        return builder.Cache?.FindFileState(file) ?? file.GetActualFileState().Cache(builder);
    }

    private static ActualFileState
    Cache(this ActualFileState state, PackageDifferenceBuilder builder) {
        builder.Cache?.Add(state);
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

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
        new BuildDifferenceContext(programDirectory, package, cache ? new BuildDifferenceCache() : null).GetDifference();

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

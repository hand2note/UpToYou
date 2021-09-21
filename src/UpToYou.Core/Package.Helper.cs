using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Diagnostics.Contracts;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UpToYou.Core {
public static class PackageHelper {
    
    public static IEnumerable<(string packageName, ImmutableList<PackageMetadata> updateByVersion)> 
    GroupByPackageName(this IEnumerable<PackageMetadata> packages) =>
        packages.GroupBy(x => x.Name).Select(x => (packageName: x.Key, updateByVersion: x.OrderByDescending(x => x.Version).ToImmutableList()));
    
    public static bool 
    TryGetLatestUpdate(this IEnumerable<PackageMetadata> updates, [NotNullWhen(true)] out PackageMetadata? result) {
        result = updates.OrderByDescending(x => x.Version).FirstOrDefault();
        return result != null;
    }
    
    public static void 
    VerifyOrderedByVersion(this IList<PackageMetadata> packages) =>
        packages.VerifyOrdered(x => x.Version);
    
    public static void 
    VerifyOrderedByDate(this IList<PackageMetadata> packages) => packages.VerifyOrdered(x => x.DatePublished);
    
    public static void 
    Verify(this PackageFile packageFile, string path) {
        if (path.GetFileHash() != packageFile.FileHash)
            throw new InvalidOperationException($"Hash of {packageFile.Path.Value.Quoted()} is not equal expected");
        if (packageFile.FileVersion == null || path.GetFileVersion() != packageFile.FileVersion)
            throw new InvalidOperationException($"Expected {packageFile.FileVersion} of {packageFile.Path.Value.Quoted()} but was {path.GetFileVersion()?.ToString().Quoted()}");
    }
    
    public static IEnumerable<PackageMetadata>
    OrderByVersion(this IEnumerable<PackageMetadata> packages) => packages.OrderByDescending(x => x.Version);
}
}

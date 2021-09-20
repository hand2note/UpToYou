using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
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
}
}

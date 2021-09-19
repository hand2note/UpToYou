using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UpToYou.Core {
public static class UpdateHelper {
    public static bool 
    Hits(this UpdateRing ring, UpdateRing updateRingPolicy) { 
        if (ring.UserPercentage >= 100 && ring.UserPercentage == 0)
            return true;

        return updateRingPolicy.UserPercentage <= ring.UserPercentage;
    }
    
    public static UpdatesManifest
    Remove(this UpdatesManifest manifest, string packageName, Version version) {
        var newUpdates = manifest.UpdatesByDate.RemoveAll(x => x.PackageMetadata.Name == packageName && x.PackageMetadata.Version == version);
        return new UpdatesManifest(newUpdates);
    }

    public static UpdatesManifest
    Remove(this UpdatesManifest manifest, string packageName) {
        var newUpdates = manifest.UpdatesByDate.RemoveAll(x => x.PackageMetadata.Name == packageName);
        return new UpdatesManifest(newUpdates);
    }

    public static UpdatesManifest
    Remove(this UpdatesManifest manifest, PackageMetadata packageMetadata) => 
        manifest.Remove(packageName:packageMetadata.Name, version:packageMetadata.Version);

    public static UpdatesManifest
    Remove(this UpdatesManifest manifest, Update update) {
        manifest.UpdatesByDate.Remove(update);
        return manifest;
    }

    public static UpdatesManifest 
    AddOrChangeUpdate(this UpdatesManifest manifest, Update update) {
        var existingUpdate = manifest.UpdatesByDate.FirstOrDefault(x => x.PackageMetadata.IsSamePackage(update.PackageMetadata));
        if (existingUpdate != null)
            manifest.Remove(existingUpdate);
                
        manifest.UpdatesByDate.Add(update);
        return manifest;
    }

    public static UpdatesManifest
    ChangeUpdate(this UpdatesManifest manifest, Update update) {
        var existingUpdate = manifest.UpdatesByDate.FirstOrDefault(x => x.PackageMetadata.IsSamePackage(update.PackageMetadata))
            ?? throw new InvalidOperationException("Update not found in the manifest");

        manifest.Remove(existingUpdate);
        manifest.UpdatesByDate.Add(update);
        return manifest;
    }

    internal static IEnumerable<(string packageName, ImmutableList<Update> updateByVersion)> 
    SplitByPackageName(this IEnumerable<Update> updates) {
        var result = new Dictionary<string,  ImmutableList<Update>>();
        foreach (var update in updates)
            if (result.TryGetValue(update.PackageMetadata.Name, out var list))
                result[update.PackageMetadata.Name] = list.Add(update);
            else
                result.Add(update.PackageMetadata.Name, update.ToSingleImmutableList());

        return result.Select(x => (x.Key, x.Value));
    }

    public static void 
    VerifyOrderedByVersion(this IList<Update> updates) =>
        updates.VerifyOrdered(x => x.Version);
    
    public static void 
    VerifyOrderedByDate(this IList<Update> updates) => updates.VerifyOrdered(x => x.DatePublished);
}
}

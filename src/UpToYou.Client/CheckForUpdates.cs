using System;
using System.Collections.Generic;
using System.Linq;
using UpToYou.Core;

namespace UpToYou.Client {

public class UpdateContext {
    public PackageHostClientContext Host { get; }
    public IUpdaterLogger? Log { get; }
    public string ProgramDirectory { get; }
    public string UpdateFilesDirectory { get; }
    public IUpdateRingPolicy UpdateRingPolicy { get; }

    public UpdateContext(PackageHostClientContext host, IUpdaterLogger? log, string programDirectory, string updateFilesDirectory, IUpdateRingPolicy updateRingPolicy) =>
        (Host, Log, ProgramDirectory,UpdateFilesDirectory , UpdateRingPolicy) = (host, log, programDirectory, updateFilesDirectory, updateRingPolicy);
}

//public class CheckUpdateResult {
//    public string? PackageName;
//    public List<Update> FreshUpdates = new List<Update>();
//    public List<Update> AllUpdates;
//    public Update RequiredUpdate;
//    public Update AutoUpdate;
//}
public static class CheckForUpdatesModule {

    public static bool IsUpdateRunnerQueued() => "updaterequested".ToAbsoluteFilePath(Environment.CurrentDirectory).FileExists();

    private static IEnumerable<(string packageName, UpdatesByVersion updates)> 
    NotifyObserver(this IEnumerable<(string packageName, UpdatesByVersion updates)> entries, UpdateContext ctx) {
        foreach (var @in in entries) {
            ctx.Log?.LogInfo($"Fetched {@in.updates.Count} updates of package {@in.packageName}");
            yield return @in;
        }
    }

    public static Update?
    FindRequiredUpdate(this UpdatesManifest updatesManifest, UpdateContext ctx) => updatesManifest.UpdatesByVersion.Value.FindRequiredUpdate(ctx);

    private static Update?
    FindRequiredUpdate(this UpdatesByVersion updates, UpdateContext ctx) =>
        updates.SplitByPackageName().Select(x => x.updates.FindRequiredUpdateInPackageSet(ctx)).NotNull().FirstOrDefault();

    private static Update? 
    FindRequiredUpdateInPackageSet(this UpdatesByVersion packages, UpdateContext ctx) {
        foreach (var freshPackage in packages.GetFreshUpdates(ctx).Where(x => x.UpdatePolicy.UpdateRing.Hits(ctx.UpdateRingPolicy)))
            if (freshPackage.UpdatePolicy.IsRequired)
                return freshPackage;
        return null;
    }

    public static Update 
    FindAutoUpdate(this UpdatesByVersion updates, UpdateContext ctx) => updates.GetRelevantFreshUpdates(ctx).FirstOrDefault(x => x.UpdatePolicy.IsAuto);

    private static IEnumerable<Update>
    GetRelevantFreshUpdates(this UpdatesByVersion updates, UpdateContext ctx) => 
        updates.GetFreshUpdates(ctx).Where(x => x.UpdatePolicy.UpdateRing.Hits(ctx.UpdateRingPolicy));

    //packageUpdates should be ordered by version descending
    private static IEnumerable<Update>
    GetFreshUpdates(this UpdatesByVersion updates, UpdateContext ctx) {
        bool isInstalledPackageFound = false;
        foreach (var packageUpdate in updates)
            if (isInstalledPackageFound)
                yield return packageUpdate;
            else if (packageUpdate.PackageMetadata.IsInstalled(ctx.ProgramDirectory))
                isInstalledPackageFound = true;
    }

    internal static IEnumerable<(string packageName, UpdatesByVersion updates)> 
    SplitByPackageName(this IEnumerable<Update> updates) {
        var res = new Dictionary<string, UpdatesByVersion>();
        foreach (var update in updates)
            if (res.TryGetValue(update.PackageMetadata.Name, out var list))
                list.Add(update);
            else
                res.Add(update.PackageMetadata.Name, new UpdatesByVersion(new List<Update>() {update}));

        return res.Select(x => (x.Key, x.Value));
    }

}
}

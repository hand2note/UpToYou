using System;
using System.Linq;
using CommandLine;
using UpToYou.Core;

namespace UpToYou.Backend.Runner {
[Verb("RemovePackage")]
public class RemovePackageOptions {
    public RemovePackageOptions(string packageVersion, string packageName) {
        PackageVersion = packageVersion;
        PackageName = packageName;
    }

    [Value(0), Option(Required = true)]
    public string PackageVersion { get; }

    [Value(1), Option]
    public string? PackageName { get; }
}

public static class RemovePackageModule {
    public static void RemovePackage(this RemovePackageOptions options) {

        var azureEnvironment = EnvironmentModule.GetAzureEnvironment();
        var log = new Logger();
        var host = new PackageHostContext(
            filesHost:new AzureBlobStorage(azureEnvironment.ToAzureBlobStorageProperties()),
            log:new Logger(),
            progressContext:null);

        var packages = host.DownloadAllPackages().ToList();
        log.LogInfo($"Downloaded {packages.Count} packages");

        var packageToRemove = packages.FirstOrDefault(x => x.Metadata.IsSamePackage(options.PackageVersion.ParseVersion(), options.PackageName));
        if (packageToRemove== null)
            throw new InvalidOperationException($"Package {options.PackageName} {options.PackageVersion} not found on the host.");

        log.LogInfo($"Package to remove found: id={packageToRemove.Id}, name={packageToRemove.Metadata.Name}, version={packageToRemove.Version}, dateBuilt={packageToRemove.Metadata.DateBuilt}");

        host.RemovePackage(packageToRemove.Id);
        log.LogInfo($"Package {packageToRemove.Metadata.Name} {packageToRemove.Version} built on {packageToRemove.Metadata.DateBuilt} has been removed from the host");

        var updateManifest  = host.DownloadUpdatesManifestIfExists();
        if (updateManifest == null) {
            log.LogWarning("Update manifest doesn't exists");
            return;
        }

        var update = updateManifest.FindUpdate(options.PackageVersion.ParseVersion(), options.PackageName);
        if (update == null) {
            log.LogWarning($"Update with version {options.PackageVersion.ParseVersion()} and name {(options.PackageName??string.Empty).Quoted()} not found in the update manifest");
            return;
        }
        updateManifest.Remove(update);

        updateManifest.Upload(host);
        log.LogInfo("Update manifest has been successfully updated");

    }
}

}

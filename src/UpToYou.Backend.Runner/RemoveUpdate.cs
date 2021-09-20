using System;
using System.Linq;
using CommandLine;
using Microsoft.Extensions.Logging;
using UpToYou.Core;

namespace UpToYou.Backend.Runner {
[Verb("RemovePackage")]
public class RemovePackageOptions {

    [Value(0), Option(Required = true)]
    public string PackageVersion { get; }

    [Value(1), Option(Required = true)]
    public string PackageName { get; }
    public RemovePackageOptions(string packageVersion, string packageName) {
        PackageVersion = packageVersion;
        PackageName = packageName;
    }
}

public static class 
RemovePackageModule {
    public static void 
    RemovePackage(this RemovePackageOptions options) {

        var azureEnvironment = EnvironmentModule.GetAzureEnvironment();
        var logger = new ConsoleLogger();
        var host = new AzureBlobStorageHost(azureEnvironment.ToAzureBlobStorageProperties());

        var packages = host.DownloadAllPackages().ToList();
        logger.LogInformation($"Downloaded {packages.Count} packages");

        var packageToRemove = packages.FirstOrDefault(x => x.Metadata.IsSamePackage(options.PackageVersion.ParseVersion(), options.PackageName));
        if (packageToRemove== null)
            throw new InvalidOperationException($"Package {options.PackageName} {options.PackageVersion} not found on the host.");

        logger.LogInformation($"Package to remove found: id={packageToRemove.Id}, name={packageToRemove.Metadata.Name}, version={packageToRemove.Version}, dateBuilt={packageToRemove.Metadata.DatePublished}");

        host.RemovePackage(packageToRemove.Id);
        logger.LogInformation($"Package {packageToRemove.Metadata.Name} {packageToRemove.Version} built on {packageToRemove.Metadata.DatePublished} has been removed from the host");

        var updateManifest  = host.DownloadUpdatesManifestIfExists();
        if (updateManifest == null) {
            logger.LogWarning("Update manifest doesn't exists");
            return;
        }

        if (!updateManifest.TryGetPackage(options.PackageName, options.PackageVersion.ParseVersion(), out var package)) {
            logger.LogWarning($"Update with version {options.PackageVersion.ParseVersion()} and name {(options.PackageName??string.Empty).Quoted()} not found in the update manifest");
            return;
        }
        
        updateManifest.RemovePackage(package.Id);

        updateManifest.UploadUpdateManifest(host);
        logger.LogInformation("Update manifest has been successfully updated");

    }
}

}

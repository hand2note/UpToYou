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
    
    [Option(Required = true)]
    public string AzureRootContainer {get;}
    
    [Option(Required = true)]
    public string AzureConnectionString {get;}
    public RemovePackageOptions(string packageVersion, string packageName, string azureRootContainer, string azureConnectionString) {
        PackageVersion = packageVersion;
        PackageName = packageName;
        AzureRootContainer = azureRootContainer;
        AzureConnectionString = azureConnectionString;
    }
}

public static class 
RemovePackageModule {
    public static void 
    RemovePackage(this RemovePackageOptions options) {

        var logger = new ConsoleLogger();
        var host = new AzureBlobStorage(new AzureBlobStorageOptions(rootContainer: options.AzureRootContainer, connectionString: options.AzureConnectionString));

        var packages = host.DownloadAllPackages().ToList();
        logger.LogInformation($"Downloaded {packages.Count} packages");

        var packageToRemove = packages.FirstOrDefault(x => x.Header.IsSamePackage(options.PackageVersion.ParseVersion(), options.PackageName));
        if (packageToRemove== null)
            throw new InvalidOperationException($"Package {options.PackageName} {options.PackageVersion} not found on the host.");

        logger.LogInformation($"Package to remove found: id={packageToRemove.Id}, name={packageToRemove.Header.Name}, version={packageToRemove.Version}, dateBuilt={packageToRemove.Header.DatePublished}");

        host.RemovePackage(packageToRemove.Id);
        logger.LogInformation($"Package {packageToRemove.Header.Name} {packageToRemove.Version} built on {packageToRemove.Header.DatePublished} has been removed from the host");

        var updateManifest  = host.DownloadUpdatesManifestIfExists();
        if (updateManifest == null) {
            logger.LogWarning("Update manifest doesn't exists");
            return;
        }

        if (!updateManifest.TryGetPackage(options.PackageName, options.PackageVersion.ParseVersion(), out var package)) {
            logger.LogWarning($"Update with version {options.PackageVersion.ParseVersion()} and name {(options.PackageName??string.Empty).Quoted()} not found in the update manifest");
            return;
        }
        
        updateManifest = updateManifest.RemovePackage(package.Id);

        updateManifest.UploadUpdateManifest(host);
        logger.LogInformation("Update manifest has been successfully updated");

    }
}

}

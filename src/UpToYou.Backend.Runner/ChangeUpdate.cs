using System;
using UpToYou.Core;

namespace UpToYou.Backend.Runner
{
    public static class ChangeUpdateHelper
    {
        public static void ChangeUpdate(Version packageVersion, string? packageName, Func<Update, Update> change) {
            var azureEnvironment = EnvironmentModule.GetAzureEnvironment();
            var host = new PackageHostContext(
                filesHost:new AzureBlobStorage(azureEnvironment.ToAzureBlobStorageProperties()),
                log:new ConsoleLogger(),
                progressContext:null);

            var updateManifest  = host.DownloadUpdatesManifestIfExists();
            if (updateManifest == null)
                throw new InvalidOperationException("Update manifest doesn't exists");
        
            var update = updateManifest.FindUpdate(packageVersion, packageName);
            if (update == null)
                throw new InvalidOperationException($"Update with version {packageVersion} and name {(packageName??string.Empty).Quoted()} not found");

            var newUpdate = change(update);
            updateManifest.Remove(update);
            updateManifest.AddOrChangeUpdate(newUpdate);

            updateManifest.Upload(host);
            Console.WriteLine("Update manifest has been successfully updated");
        }
    }

}

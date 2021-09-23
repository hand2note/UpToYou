# UpToYou
UpToYou is an easy to use updater for .NET desktop applications.

## How to publish an update?

The simplest scenario: 

1. Create package specs file `MyApp.package.yml`: 
```yaml
PackageName: MyApp
Files: **/*
VersionProvider: MyApp.exe
```

`VersionProvider` is the file that determines a version of your app.

2. Create update notes file `MyApp.UpdateNotes.en`:

```
[//] #1.0.0.1
Added: 
- Some **important** feature was added
- Another **important** feature was added
```

2. Pack and publish the update to Azure Blob Storage:

```powershell
> dotnet tool install uptoyou
> uptoyou PushUpdate `
--SourceDirectory /path/to/bin `
--PackageSpecsFile MyApp.package.yml `
--UpdateNotesFiles MyApp.UpdateNotes.en `
--FilesHostType AzureBlobStorage `
--AzureRootContainer my-container `
--AzureConnectionString $MyAzureConnectionString
```

3. On you client side 
```powershell
> dotnet add package UpToYou.Client
```

```cs 
using UpToYou.Core;
using UpToYou.Client;

var host = new HttpHostClient("https://myapp.blob.core.windows.net/my-container");
var updatesManifest = host.DownloadUpdatesManifest();
var latestPackage = updater.Packages.OrderByVersion().First();
if (!latestPackage.IsInstalled()){
    var updateNotes = host.DownloadUpdateNotes(packageName: latestPackage.Name, locale: "en");
    //ask the user to intall an updates
    var updater = new Updater(host, logger: NullLogger.Instance, options: UpdaterOptions.Default);
    var installResult = latestPackge.DownloadAndInstall(updater);
    if (installResult.IsRestartRequired){
        //Ask user to reinstall
        updater.ExecuteRunner();
        Environment.Exit(0);
    }
} 
```

Basically this code:
- Downloaded updates manifest
- Checked wether the new update is available
- Downloaded update notes to show them to user
- Downloaded and installed the new update files
- Restart could be required because some files can't be replace while the app is running. 
- `updater.ExecuteRunner` runs the `UpToYou.Client.Runner.exe` that replaces all files that haven't been replaced yet and starts the `MyApp.exe`

## Features
- Download a subset of files rather than the whole package
- Download and apply deltas, i.e. download much less data
- Plugins support, i.e. multiple packages per app
- Update notes localiczation
- Attach custom properties to package like `IsBeta`, `IsRequired` etc. YOu can describe them in `*.package.yml` and then retrieve them on the client.
- Remove package with `uptoyou RemovePackage --PackageName MyApp --PacakgeVersion 0.1.0`

## todo: 
- AWS, Google Cloud and other file hosts support
- More CLI commands like `ListPackages`, `GetUpdateNotes` etc.

## Glossary
- `Package` is simply a set of files that can be updated
- `Package Header` contains all information about the package except its files.
- `Version Provider` is the file that determines the version of the package. `package.IsIntalled()` check the version of this file.
- `Updates Manifest` is a set of `Package Header` available to download
- `Custom Properties` is a key/value dictionary of custom properties attached to a package like `IsBeta`, `IsRequired`, `UpdateRing` etc.
- `Host` is a remote file storage that hosts an `Update Manifest`, all packages and its files.
- `Package Projection` describes a set of files that are stored in a separate archive on the `Host`. Host stores an archive per projection. You can't download a specific file of the package, you can download only files contained in a single projection.
- `Delta` is a binary difference between one file and another.

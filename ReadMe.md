# UpToYou
UpToYou is an easy to use updater for .NET desktop applications.

## Installation
`todo: add nuget package`

## How to use?
1. Describe a package and its files in `*.package` YAML file: 
`MyApp.package`
```yaml
PackageName: MyApp
Files: **/*
VersionProvider: MyApp.exe
```

2. In CI/CD pipeline run the following command:
```cmd
UpToYou.exe
--SourceDirectory $BinariesDirectory \
--PackageSpecsFile ./MyApp.package \
--FilesHostType AzureBlobStorage \
--AzureRootContainer my-app-updates \
--AzureConnectionString $AzureConnectionString
--Force
```

3. On your app, i.e. in a client app:
```csharp
var upToYou = new UpToYouClient("connection/string");
upToYou.InstallLatestIfAny();
```

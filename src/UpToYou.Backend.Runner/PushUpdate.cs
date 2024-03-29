﻿using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Security;
using CommandLine;
using Microsoft.Extensions.Logging;
using UpToYou.Core;

namespace UpToYou.Backend.Runner {
[Verb("PushUpdate", HelpText = "Builds a package and uploads it to the files host")]
public class PushUpdateOptions: IFilesHostOptions {
    [Option(Required = true, HelpText = "Directory of the files to be packed.")]
    public string SourceDirectory { get; set; }

    [Option(HelpText = "Relative path to the file which should be used to determine the version of the package " +
       "and the version of a client installed on a user's machine." +
       "May not be specified and will be taken from the package specs in this case.")]
    public string? VersionProvider { get; set; }

    [Option(HelpText = "Path to the package specifications file.", Required = true)]
    public string PackageSpecsFile { get;set;  }

    [Option(HelpText = "Path to the package projection specifications file.")]
    public string? ProjectionSpecsFile { get;set; }

    [Option(HelpText = "Path to the package updates specifications file.")]
    public string? UpdatesSpecsFile { get; set; }

    [Option(HelpText = "Files containing package update notes. Supposed that each file contains a localized version of the package notes.")]
    public IEnumerable<string>? UpdateNotesFiles { get; set; }

    [Option(Hidden = true, HelpText = "Custom properties to be attached to the package in the format: " +
    "\"PropertyName1:PropertyValue2, PropertyName2:PropertyValue2\"")]
    public IEnumerable<string>? PackageCustomProperties { get;set;  }

    [Option(HelpText = "Path to the working directory to build a package.")]
    public string? WorkingDirectory { get; set; }

    [Option(HelpText = "True if push the package even in case when the same package is already present on the host. " +
                       "The package entry in the update manifest will be updated.")]
    public bool Force { get; set; }

    [Option(HelpText = "True if push the package if update notes file is specified but no update notes have been found for this version of the package.")]
    public bool ForceIfEmptyNotes { get; set;}

    [Option(Hidden =  true)]
    public string? FilesHostType { get; set; }

    [Option]
    public string? AzureConnectionString { get; set; }

    [Option]
    public string? AzureRootContainer {get;set; }

    [Option(Hidden = true)]
    public string? LocalHostRootPath {get;set; }


    #pragma warning disable CS8618 // Non-nullable field is uninitialized.
    public PushUpdateOptions(){ }
    #pragma warning restore CS8618 // Non-nullable field is uninitialized.

    public PushUpdateOptions(string sourceDirectory, string? updatesSpecsFile = null, bool forceIfEmptyNotes = false, string? localHostRootPath = null, bool force = false,
        string? filesHostType = null, string? packageSpecsFile = null, string? projectionSpecsFile = null, IEnumerable<string>? updateNotesFiles = null, bool isRequired = false, bool isAuto =false, bool isBeta = false, IEnumerable<string>? packageCustomProperties= null, string? workingDirectory= null,  string? azureConnectionString = null,  string? azureRootContainer= null, int updateRing = 0, IEnumerable<string>? updateCustomProperties = null, string? versionProvider =null) {
        SourceDirectory = sourceDirectory;
        UpdatesSpecsFile = updatesSpecsFile;
        ForceIfEmptyNotes = forceIfEmptyNotes;
        LocalHostRootPath = localHostRootPath;
        PackageSpecsFile = packageSpecsFile;
        ProjectionSpecsFile = projectionSpecsFile;
        UpdateNotesFiles = updateNotesFiles;
        PackageCustomProperties = packageCustomProperties;
        WorkingDirectory = workingDirectory;
        Force = force;
        FilesHostType = filesHostType;
        AzureConnectionString = azureConnectionString;
        VersionProvider = versionProvider;
        AzureRootContainer = azureRootContainer;
    }
}

public static class 
PushUpdateHelper {
    
    public static void 
    PushUpdate(this PushUpdateOptions options) =>
        PushUpdate(
            workingDirectory:options.WorkingDirectory ?? Environment.CurrentDirectory.AppendPath(UniqueId.NewUniqueId()),
            sourceDirectory: options.SourceDirectory,
            packageSpecs:options.PackageSpecsFile.ParsePackageSpecsFromFile(),
            projectionSpecs:  options.ProjectionSpecsFile?.ParseProjectionFromFile(),
            updateNotesFiles: options.UpdateNotesFiles?.ToList() ?? new List<string>(),
            allowEmptyNotes: options.ForceIfEmptyNotes,
            host: options.GetFilesHost()
        );

    /// <returns>Package Id</returns>
    internal static string 
    PushUpdate(string workingDirectory, 
        string sourceDirectory,
        PackageSpecs packageSpecs,
        PackageProjectionSpecs? projectionSpecs,
        IList<string> updateNotesFiles,
        bool allowEmptyNotes,
        IHost host)
    {
        var packageDirectory = workingDirectory.AppendPath("package").CreateDirectory();
        var projectionDirectory= workingDirectory.AppendPath("projection").CreateDirectory();
        var logger = new ConsoleLogger();
        logger.LogInformation($"Pushing update (UpToYou.Backend.Runner {Process.GetCurrentProcess().MainModule?.FileVersionInfo.FileVersion})");
        projectionSpecs ??= sourceDirectory.EnumerateDirectoryRelativeFiles().ToList().ToSingleProjectionFileSpec().ToProjectionSpecs();
        
        //Build package
        logger.LogDebug("Building package...");
        var packageBuilder = new PackageBuilder(
            sourceDirectory:sourceDirectory,
            outputDirectory:packageDirectory,
            specs:packageSpecs);

        var (package, _) = packageBuilder.BuildPackage();
        logger.LogInformation($"Package {package.Header.Name} #{package.Version} has been successfully built!");

        //Check existing packages
        var isSamePackageAlreadyExists = false;
        var allPackages = host.DownloadAllPackages();
        if (allPackages.TryGet(x => x.Header.IsSamePackage(package.Header), out var samePackageOnHost)) {
            isSamePackageAlreadyExists = true;
            logger.LogWarning("The same package is already present on the host and will be skipped. However, the update notes will be overriden.");
        }

        if (!isSamePackageAlreadyExists) {
            //Build projection
            var projectionBuilder = new ProjectionBuilder(
                sourceDirectory: packageDirectory,
                outputDirectory: projectionDirectory,
                package: package,
                projectionSpecs: projectionSpecs,
                host: host,
                logger: new ConsoleLogger());

            var projectionBuildResult = projectionBuilder.BuildProjection(allCachedPackages: allPackages);
            logger.LogInformation($"Package {package.Header.Name} #{package.Version} has been successfully published!");

            logger.LogInformation("Package projection has been successfully built!");

            //Upload
            logger.LogDebug("Uploading projection files to the host...");

            projectionBuildResult.UploadAllProjectionFiles(host);
            package.UploadPackageManifest(host);

            logger.LogInformation("Projection files have been successfully uploaded!");

            //Removing existing same packages
            allPackages.Where(x => x.Id != package.Id && x.Header.IsSamePackage(package.Header))
                .ForEach(x => host.RemovePackage(x.Id));

            //Update manifest
            var updateManifest = host.UpdateManifestFileExists() 
                ? host.DownloadUpdatesManifest().AddOrChangeUpdate(package.Header)
                : new UpdatesManifest(package.Header.ToSingleImmutableList());
                    
            updateManifest.UploadUpdateManifest(host);
        }

        //Upload update notes
        if (updateNotesFiles.Count > 0) {
            logger.LogDebug($"Uploading {updateNotesFiles.Count} update notes files");
            foreach (var updateNotesFile in updateNotesFiles) {
                updateNotesFile.VerifyFileExistence();
                //Note! Here we also check if update notes file is successfully parseable. Keep in mind this if you want remove the code
                if (!updateNotesFile.ReadAllFileText().Trim().ParseUpdateNotes().Contains(package.Version)) {
                    var msg = $"Update notes file {updateNotesFile.Quoted()} doesn't contain notes for the update {package.Version}";
                    if (allowEmptyNotes || package.Header.CustomProperties.ContainsKey("ForceIfEmptyNotes"))
                        logger.LogWarning(msg);
                    else 
                        throw new InvalidOperationException(msg);
                }
                var result = updateNotesFile.ParseUpdateNotesParsFromFile();
                updateNotesFile.ReadAllFileBytes().UploadUpdateNotesUtf8(package.Header.Name, result.locale, host);
                logger.LogInformation($"Uploaded update notes file {updateNotesFile.GetFileName()} for packageName={result.fileName??"any"}, locale={result.locale??"any"}");
            }
        }
        else 
            logger.LogWarning($"No update notes files have been specified. Consider creating an update notes file with name {UpdateNotesHelper.GetUpdateNotesFileName(package.Header.Name, null).Quoted()}.");

        logger.LogInformation("Updates manifest has been updated");
        return package.Id;
    }
    
    public static ImmutableDictionary<string,string> 
    ToCustomProperties(this IEnumerable<string> option) =>
        option.Select(x => x.Split(':')).ToImmutableDictionary(x => x[0].Trim(),x => x[1].Trim());
}

}

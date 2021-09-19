using System;
using System.Collections.Generic;
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

    [Option(HelpText = "Path to the package specifications file.")]
    public string? PackageSpecsFile { get;set;  }

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
    PushUpdate(this PushUpdateOptions options){
        var workingDirectory = options.WorkingDirectory ?? Environment.CurrentDirectory.AppendPath(UniqueId.NewUniqueId());
        var packageDirectory = workingDirectory.AppendPath("package").CreateDirectory();
        var projectionDirectory= workingDirectory.AppendPath("projection").CreateDirectory();
        var logger = new ConsoleLogger();

        //Build package
        logger.LogDebug("Building package...");
        var buildPackageCtx = new PackageBuilder(
            sourceDirectory:options.SourceDirectory,
            outputDirectory:packageDirectory,
            specs:options.PackageSpecsFile?.ReadAllFileText().Trim().ParsePackageSpecsFromYaml() 
                  ?? options.SourceDirectory
                        .EnumerateDirectoryRelativeFiles()
                        .FilesToPackageSpecs(options.VersionProvider?.ToRelativePath() 
                            ?? throw new InvalidOperationException($"VersionProvider should be specified in case of package specs are not specified.")),
            customProperties:options.PackageCustomProperties?.ToCustomProperties());

        var (package, _) = buildPackageCtx.BuildPackage();
        logger.LogInformation($"Package {package.Metadata.Name} #{package.Version} has been successfully built!");

        //Retrieve host
        var host = options.GetFilesHost();
        
        //Check existing packages
        bool isSamePackageAlreadyExists = false;
        var allPackages = host.DownloadAllPackages();
        if (allPackages.TryGet(x => x.Metadata.IsSamePackage(package.Metadata), out var samePackageOnHost)) {
            isSamePackageAlreadyExists = true;
            logger.LogWarning("The same package is already present on the host and will be skipped. However, the update notes will be overriden.");
        }

        var allUpdatesSpecs = options.UpdatesSpecsFile?.VerifyFileExistence().ReadAllFileText().ParseUpdatesSpecsFromYaml();
        if (!isSamePackageAlreadyExists) {
            //Build projection
            var buildProjectionCtx = new ProjectionBuilder(
                sourceDirectory: packageDirectory,
                outputDirectory: projectionDirectory,
                package: package,
                projectionSpecs: options.ProjectionSpecsFile?.ReadAllFileText().Trim().ParseProjectionFromYaml()
                                 ?? options.SourceDirectory.EnumerateDirectoryRelativeFiles().ToList()
                                           .ToSingleProjectionFileSpec().ToProjectionSpecs(),
                host: host,
                options.LocalHostRootPath ?? options.AzureBlobStorageProperties().GetRootUrl(),
                new ConsoleLogger());

            var projectionBuildResult = buildProjectionCtx.BuildProjection(allCachedPackages: allPackages);
            logger.LogInformation($"Package {package.Metadata.Name} #{package.Version} has been successfully published!");

            logger.LogInformation("Package projection has been successfully built!");

            //Upload
            logger.LogDebug("Uploading projection files to the host...");

            projectionBuildResult.UploadAllProjectionFiles(host);
            package.UploadPackageManifest(host);

            logger.LogInformation("Projection files have been successfully uploaded!");

            //Removing existing same packages
            allPackages.Where(x => x.Id != package.Id && x.Metadata.IsSamePackage(package.Metadata))
                .ForEach(x => host.RemovePackage(x.Id));

            //Update manifest
            var updateSpec = allUpdatesSpecs?.FindUpdateSpec(package.Version);
            var update = updateSpec.ToUpdate(package.Metadata);

            if (!host.TryDownloadUpdateManifest(out var updateManifest))
                updateManifest = new UpdatesManifest(new List<Update>() {update});
            else
                updateManifest = updateManifest!.AddOrChangeUpdate(update);
            
            updateManifest.Upload(host);
        }

        //Updating updates manifest based on package updates specs
        if (allUpdatesSpecs != null && !allUpdatesSpecs.IsEmpty)
            if (host.TryDownloadUpdateManifest(out var updateManifest)) {
                foreach (var existingUpdate in updateManifest!.GetUpdates(package.Metadata.Name).ToList()) {
                    var updateSpec = allUpdatesSpecs.FindUpdateSpec(existingUpdate.PackageMetadata.Version);
                    if (updateSpec != null) {
                        updateManifest.ChangeUpdate(updateSpec.ToUpdate(existingUpdate.PackageMetadata));
                    }
                    else {
                        updateManifest.ChangeUpdate(new Update(
                            packageMetadata:existingUpdate.PackageMetadata,
                            updatePolicy:UpdatePolicy.Default,
                            customProperties:null));
                    }

                }

                updateManifest!.Upload(host);
            }

        //Upload update notes
        if (options.UpdateNotesFiles != null) {
            var updateNotesFiles = options.UpdateNotesFiles.ToList();
            logger.LogDebug($"Uploading {updateNotesFiles.Count} update notes files");
            foreach (var updateNotesFile in updateNotesFiles) {
                updateNotesFile.VerifyFileExistence();
                //Note! Here we also check if update notes file is successfully parseable. Keep in mind this if you want remove the code
                if (!updateNotesFile.ReadAllFileText().Trim().ParseUpdateNotes().Contains(package.Version)) {
                    var msg = $"Update notes file {updateNotesFile.Quoted()} doesn't contain notes for the update {package.Version}";
                    if (options.ForceIfEmptyNotes || package.Metadata.CustomProperties.ContainsKey("ForceIfEmptyNotes"))
                        logger.LogWarning(msg);
                    else 
                        throw new InvalidOperationException(msg);
                }
                var result = updateNotesFile.ParseUpdateNotesParsFromFile();
                updateNotesFile.ReadAllFileBytes().UploadUpdateNotesUtf8(package.Metadata.Name, result.locale, host);
                logger.LogInformation($"Uploaded update notes file {updateNotesFile.GetFileName()} for packageName={result.fileName??"any"}, locale={result.locale??"any"}");
            }
        }
        else 
            logger.LogWarning($"No update notes files have been specified. Consider creating an update notes file with name {UpdateNotesHelper.GetUpdateNotesFileName(package.Metadata.Name, null).Quoted()}.");

        logger.LogInformation("Updates manifest has been updated");
    }
    
    public static Dictionary<string, string> 
    ToCustomProperties(this IEnumerable<string> option) =>
        option.Select(x => x.Split(':')).ToDictionary(x => x[0].Trim(),x => x[1].Trim());
}

}

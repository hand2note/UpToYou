using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Diagnostics.Contracts;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using UpToYou.Core;

namespace UpToYou.Client {
public static class UpdaterHelper {
    
public static TimeSpan RelevantDownloadSpeedTimeSpan = TimeSpan.FromSeconds(15);

    public static void 
    RunUpdater(string? updateFilesDirectory = null, string? backupDirectory = null) {
        if (updateFilesDirectory == null)
            updateFilesDirectory = Updater.DefaultUpdateFilesSubDirectory.ToAbsoluteFilePath(Environment.CurrentDirectory);
        if (backupDirectory == null)
            backupDirectory = Updater.DefaultBackupSubDirectory.ToAbsoluteFilePath(Environment.CurrentDirectory);
        if (!updateFilesDirectory.EndsWith(UpdaterHelper.RunnerSourcesSubDirectory))
            updateFilesDirectory = updateFilesDirectory.AppendPath(UpdaterHelper.RunnerSourcesSubDirectory);
        var applicationStartupFile = Process.GetCurrentProcess().MainModule?.FileName;
        var processesIdsToWaitExit = new int[]{ Process.GetCurrentProcess().Id};
        Process.Start("UpToYou.Client.Runner.exe", $"{updateFilesDirectory.Quoted()} {backupDirectory.Quoted()} {string.Join(",", processesIdsToWaitExit.MapToList(x => x.ToString()))}  {applicationStartupFile.Quoted()}");
    }

    internal static string
    DownloadUpdateFiles(this PackageDifference difference, Updater updater) {
        if (!difference.IsDifferent()) throw new InvalidOperationException("ActualState doesn't differ from the package");

        var projection = difference.Package.DownloadProjection(updater.HostClient);
        var hostedFileToDownload = difference.GetFilesToDownload(projection).ToList().GetSmallestHostedFilesSet().ToList();
        
        //updater.ProgressContext?.OnExtraTargetValue(hostedFileToDownload.Sum(x =>x.FileSize));
        var downloadedFiles = hostedFileToDownload.DownloadAllHostedFiles(updater.UpdateFilesDirectory, updater.HostClient).ToList();
        downloadedFiles.ExtractAllHostedFiles(updater.UpdateFilesDirectory);
        return updater.UpdateFilesDirectory; 
    }

    private static HashSet<HostedFile>
    GetSmallestHostedFilesSet(this List<List<HostedFile>> itemsFiles) {
        //Ideally here should be a recursive algorithm to find the smallest possible set of hosted files. But it is too complicated for now.
        //So here is the simple one
        var result = new HashSet<HostedFile>(itemsFiles.Where(x => x.Count == 1).Select(x => x[0]).Distinct());

        foreach (var itemFiles in itemsFiles.Where(x => x.Count > 1)) {
            if (result.ContainsAny(itemFiles))
                continue;
            result.Add(itemFiles.SmallestFile()); }
        return result; 
    }

    private static HostedFile
    SmallestFile(this IEnumerable<HostedFile> hostedFiles) => hostedFiles.MinBy(x => x.FileSize);

    private static IEnumerable<List<HostedFile>>
    GetFilesToDownload(this PackageDifference difference, PackageProjection projection) {
        //Probably will be faster to cache relevant hosted files in advance
        var relevantHostedFiles = projection.HostedFiles.Where(x => x.RelevantItemsIds.ContainsAny(difference.DifferentFilesIds)).ToList();

        foreach (var fileDifference in difference.DifferentFiles) {
            var hostedFiles = !fileDifference.ActualFileState.Exists
                ? FindFullHostedFile(fileDifference.PackageItemId, relevantHostedFiles).ToList()
                : FindDeltaHostedFile(fileDifference, relevantHostedFiles) .Union(FindFullHostedFile(fileDifference.PackageItemId, relevantHostedFiles)).ToList();
            if (hostedFiles.Count == 0)
                throw new InvalidOperationException($"Failed to find hosted file containing an updated version of {fileDifference.PackageFile.Path}");
            yield return hostedFiles; } 
    }

    private static IEnumerable<HostedFile>
    FindFullHostedFile(string packageItemId, IEnumerable<HostedFile> relevantHostedFiles) {
        foreach (var hostedFile in relevantHostedFiles)
            if (hostedFile.Content is PackageItemsHostedFileContent content)
                if (content.PackageItems.Contains(packageItemId))
                    yield return hostedFile; 
    }

    private static IEnumerable<HostedFile>
    FindDeltaHostedFile(PackageFileDifference fileDifference, IEnumerable<HostedFile> relevantHostedFiles) {
        foreach (var hostedFile in relevantHostedFiles)
            if (hostedFile.Content is PackageFileDeltasHostedFileContent deltaContent) {
                var delta = deltaContent.PackageFileDeltas.FirstOrDefault(x =>
                    x.PackageItemId == fileDifference.PackageItemId &&
                    x.OldHash == fileDifference.ActualFileState.Hash &&
                    x.NewHash == fileDifference.PackageFile.FileHash);

                if (delta != null)
                    yield return hostedFile;
            }
    }
    
    public static string RunnerSourcesSubDirectory = ".uptoyou.runner";

    public static PackageDifference
    DownloadPackageDifference(this PackageDifference difference, Updater updater) {
        if (!difference.IsDifferent()) throw new InvalidOperationException("ActualState doesn't differ from the package");
        updater.Logger.LogDebug("Clearing update directory...");
        updater.UpdateFilesDirectory.ClearDirectoryIfExists();

        updater.Logger.LogInformation($"Downloading update files for package {difference.Package}");
        difference.DownloadUpdateFiles(updater);
        return difference;
    }
    
    public static InstallUpdateResult 
    InstallPackageDifference(this PackageDifference difference, Updater updater) {
        if (!difference.IsDifferent())
            throw new InvalidOperationException($"Package {difference.Package.Metadata.Version} is already installed");
        updater.Logger.LogInformation($"Installing package update {difference.Package}...");
        var remainingDifference = difference.InstallAccessibleFiles(updater);
        return remainingDifference != null && remainingDifference.IsDifferent()
            ? new InstallUpdateResult(isCompleted: false, isRunnerExecutionRequired: true)
            : new InstallUpdateResult(isCompleted: true, isRunnerExecutionRequired: false);
    }

    internal static void
    UpdateUpdaterExe(this PackageFileDifference updaterExeDifference, Updater updater) {
        if (!updaterExeDifference.PackageFile.Path.IsInstallExecutable())
            throw new ArgumentException(nameof(updaterExeDifference),$"Expecting a package difference for Updater.exe");

        if (!updater.UpdateFilesDirectory.GetAllDirectoryFiles().TryGet(x => x.MatchGlob($"**/{SelfBinaries.InstallExecutable}"), out var updateFile))
            throw new InvalidOperationException("Update file for Updater.exe not found");

        if (updater.IsBackupEnabled)
            updater.BackupDirectory.CreateDirectoryIfAbsent();

        updaterExeDifference.UpdateFile(updater, new Dictionary<string, string>(){{updaterExeDifference.PackageFile.FileHash, updateFile}});
    }

    public static void 
    VerifyInstallation(this Package package, string programDirectory) => package.Files.Values.ForEach(x => x.Verify( x.Path.ToAbsolute(programDirectory)));

    internal static PackageDifference? 
    InstallAccessibleFiles(this PackageDifference difference, Updater updater) {
        updater.InitBackupDirectory();
        var updateFilesCache = difference.GetUpdateFilesHashes(updater).DistinctBy(x => x.hash).ToDictionary(x => x.hash, x => x.file);
        
        var remainingDifferences = new List<PackageFileDifference>();
        foreach (var fileDifference in  difference.DifferentFiles)
            try {
                fileDifference.UpdateFile(updater, updateFilesCache);
            }
            catch (Exception ex) when (ex is AccessViolationException || ex is UnauthorizedAccessException) {
                updater.Logger.LogInformation($"File {fileDifference.PackageFile.Path.Value.Quoted()} is not accessible.");
                remainingDifferences.Add(fileDifference);
                fileDifference.PrepareForRunner(updater, updateFilesCache);
            }
        if (remainingDifferences.Count > 0)
            return new PackageDifference(package:difference.Package, fileDifferences:remainingDifferences);
        return null;
    }

    private static void 
    Install(this PackageDifference difference, Updater updater) {
        updater.InitBackupDirectory();

        var updateFilesCache = difference.GetUpdateFilesHashes(updater).DistinctBy(x => x.hash).ToDictionary(x => x.hash, x => x.file);
        difference.DifferentFiles.ForEach(x => x.UpdateFile(updater, updateFilesCache));
        difference.Package.VerifyInstallation(updater.ProgramDirectory);
    }

    private static void
    InitBackupDirectory(this Updater updater) {
        if (updater.IsBackupEnabled) {
            if (updater.BackupDirectory.DirectoryExists())
                updater.BackupDirectory.RemoveDirectory();
            updater.BackupDirectory.CreateDirectoryIfAbsent();
        }
    }

    private static IEnumerable<(string hash, string file)>
    GetUpdateFilesHashes(this PackageDifference packageDifference, Updater updater) {
        foreach (var fileDifference in packageDifference.DifferentFiles) {
            var packageFile = fileDifference.PackageFile;
            var file = packageFile.Path.ToAbsolute(updater.UpdateFilesDirectory);
            if (file.FileExists()) {
                #if DEBUG
                Contract.Assert(file.GetFileHash() == packageFile.FileHash,
                    $"Update file hash is not equal expected in its packageFile ({file.Quoted()})");
                #endif
                yield return (packageFile.FileHash, file);
            }
            else
                yield return (packageFile.FileHash, fileDifference.GetDeltaFile(updater.UpdateFilesDirectory));
        }
    }

    private static string
    GetDeltaFile(this PackageFileDifference difference, string updateFilesDirectory) =>
        Directory.EnumerateFiles(updateFilesDirectory, 
            $"*{PackageProjection.GetDeltaFileName(difference.ActualFileState.Hash, difference.PackageFile.FileHash)}",SearchOption.AllDirectories).FirstOrDefault()
            ?? throw new InvalidOperationException($"No file found in the update files directory for different package file {difference.PackageFile.Path}");

    internal static void
    PrepareForRunner(this PackageFileDifference fileDifference, Updater ctx, Dictionary<string, string> fileHashToPath) {
        var runnerDirectory = ctx.UpdateFilesDirectory.AppendPath(RunnerSourcesSubDirectory).CreateDirectoryIfAbsent();
        
        if (!fileHashToPath.TryGetValue(fileDifference.PackageFile.FileHash, out var updateFile))
            throw new InvalidOperationException(
                $"Update file with hash = {fileDifference.PackageFile.FileHash.Quoted()} not found for packageFile {fileDifference.PackageFile.Path.Value.Quoted()}");

        var fileForRunner = fileDifference.PackageFile.Path.ToAbsolute(runnerDirectory);
        if (updateFile.IsPackageDeltaFile()) {
            if (!updateFile.IsUnpackedDeltaFile())
                throw new InvalidOperationException($"Expecting all delta files to be unpacked, but {updateFile.Quoted()} is packed");
            
            fileDifference.ActualFileState.Path.CopyFile(fileForRunner);
            fileForRunner.ApplyDelta(updateFile);
            ctx.Logger.LogDebug($"{fileDifference.PackageFile.Path} copied to the runner's source directory and applied delta to.");
        }
        else {
            updateFile.CopyFile(fileForRunner);
            ctx.Logger.LogDebug($"File copied to {fileForRunner}");
        }
    }

    private static void
    UpdateFile(this PackageFileDifference fileDifference, Updater updater, Dictionary<string, string> fileHashToPath) {
        if (!fileHashToPath.TryGetValue(fileDifference.PackageFile.FileHash, out var updateFile))
            throw new InvalidOperationException(
                $"Update file with hash = {fileDifference.PackageFile.FileHash.Quoted()} not found for packageFile {fileDifference.PackageFile.Path.Value.Quoted()}");

        if (updater.IsBackupEnabled)
            fileDifference.Backup(updater);

        if (updateFile.IsPackageDeltaFile()) {
            if (!updateFile.IsUnpackedDeltaFile())
                throw new InvalidOperationException($"Expecting all delta files to be unpacked, but {updateFile.Quoted()} is packed");

            fileDifference.ActualFileState.Path.ApplyDelta(updateFile);
            updater.Logger.LogDebug($"Applied delta to {fileDifference.PackageFile.Path}");
        }
        else {
            updateFile.CopyFile(fileDifference.ActualFileState.Path);
            updater.Logger.LogDebug($"File copied to {fileDifference.PackageFile.Path}");
        }
    }

    private static void
    Backup(this PackageFileDifference fileDifference, Updater updater) {
        if (fileDifference.ActualFileState.Exists)
            fileDifference.ActualFileState.Path.CopyFile(
                fileDifference.ActualFileState.Path
                    .GetPathRelativeTo(updater.ProgramDirectory).Value
                    .ToAbsoluteFilePath(updater.BackupDirectory));
    }

    private static void
    ApplyDelta(this string inFile, string deltaFile) {
        var tempFile = inFile + "." + UniqueId.NewUniqueId();
        try {
            using (var fs = tempFile.OpenFileForWrite())
                BinaryDelta.ApplyDelta(inFile, deltaFile, fs);
            tempFile.MoveFile(inFile);
        }
        finally {
            tempFile.RemoveFileIfExists();
        }
        
    }
    
    public static bool 
    IsUpdateRunnerQueued() => "updaterequested".ToAbsoluteFilePath(Environment.CurrentDirectory).FileExists();

    private static IEnumerable<Update>
    GetFreshUpdates(this IList<Update> updateByVersion, Updater updater) {
        #if DEBUG
        updateByVersion.VerifyOrderedByVersion();
        #endif
        bool isInstalledPackageFound = false;
        foreach (var packageUpdate in updateByVersion)
            if (isInstalledPackageFound)
                yield return packageUpdate;
            else if (packageUpdate.PackageMetadata.IsInstalled(updater.ProgramDirectory))
                isInstalledPackageFound = true;
    }
    
    public static string 
    GetUpdateBackupDirectory(this Update update, Updater updater) =>
        updater.BackupDirectory.AppendPath(update.PackageMetadata.Name).CreateDirectoryIfAbsent();
    
}
}

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
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
    
    public static bool
    TryGetInstalledVersion(this UpdatesManifest manifest, string packageName, string programDirectory, [NotNullWhen(true)] out Version? result) {
        if (manifest.GetPackageHeaders(packageName).OrderByVersion().TryGet(x => x.IsInstalled(programDirectory), out var resultPackage)) {
            result = resultPackage.Version;
            return true;
        }
        if (!manifest.GetPackageHeaders(packageName).OrderByVersion().TryGet(x => x.VersionProviderFile.GetFile(programDirectory).FileExists(), out var package)) {
            result = default;
            return false;
        }
        result = package.VersionProviderFile.GetFile(programDirectory).GetFileVersion();
        return result != null;
    }

    public static IEnumerable<PackageHeader>
    GetNewUpdates(this IEnumerable<PackageHeader> packages, Updater updater) {
        foreach(var (_, packageUpdates) in packages.GroupByPackageName()) {
            if (!packageUpdates.TryGetLatestUpdate(out var latestUpdate))
                continue;
            if (!latestUpdate.IsHigherVersionInstalled(updater.ProgramDirectory))
                yield return latestUpdate;
        }
    }
    
    public static InstallUpdateResult
    DownloadAndInstall(this IEnumerable<PackageHeader> updates, Updater updater) {
        InstallUpdateResult? result = null;
        foreach(var update in updates) {
            var updateResult = update.DownloadAndInstall(updater);
            if (result == null)
                result = updateResult;
            else 
                result = result.Combine(updateResult);
        }
        if (result == null) throw new InvalidOperationException("Collection of updates is empty");
        return result;
    }
    
    public static InstallUpdateResult 
    Combine(this InstallUpdateResult result, InstallUpdateResult other) =>
        new InstallUpdateResult(isRestartRequired: result.IsRestartRequired || other.IsRestartRequired);

    public static InstallUpdateResult 
    DownloadAndInstall(this PackageHeader packageHeader, Updater updater) {
        if (packageHeader.IsInstalled(updater))
            throw new InvalidOperationException($"Update {packageHeader} is already installed");
        var package = packageHeader.Id.DownloadPackageById(updater.HostClient);
        var difference = package.GetDifference(programDirectory: updater.ProgramDirectory);
        difference.DownloadPackageDifference(updater);
        return difference.InstallPackageDifference(updater);
    }
    
    public static void 
    ExecuteRunner(this Updater updater) {
        var applicationStartupFile = Process.GetCurrentProcess().MainModule?.FileName ?? throw new InvalidOperationException("Main module of the current process not found");
        var processesIdsToWaitExit = new int[]{ Process.GetCurrentProcess().Id};
        Process.Start("Updater.exe", $"{updater.UpdateFilesDirectory.AppendPath(".uptoyou.runner").Quoted()} {updater.BackupDirectory.Quoted()} {applicationStartupFile.Quoted()} {processesIdsToWaitExit.AggregateToString(",").Quoted()}");
    }
    
    public static bool 
    IsInstalled(this PackageHeader packageHeader, Updater updater) =>
       packageHeader.IsInstalled(programDirectory: updater.ProgramDirectory);

    internal static string
    DownloadUpdateFiles(this PackageDifference difference, Updater Updater) {
        if (!difference.IsDifferent()) throw new InvalidOperationException("ActualState doesn't differ from the package");

        var projection = difference.Package.DownloadProjection(Updater.HostClient);
        var hostedFileToDownload = difference.GetFilesToDownload(projection).ToList().GetSmallestHostedFilesSet().ToList();
        
        //updater.ProgressContext?.OnExtraTargetValue(hostedFileToDownload.Sum(x =>x.FileSize));
        var downloadedFiles = hostedFileToDownload.DownloadAllHostedFiles(Updater.UpdateFilesDirectory, Updater.HostClient).ToList();
        downloadedFiles.ExtractAllHostedFiles(Updater.UpdateFilesDirectory);
        return Updater.UpdateFilesDirectory; 
    }

    private static HashSet<PackageProjectionFile>
    GetSmallestHostedFilesSet(this List<List<PackageProjectionFile>> itemsFiles) {
        //Ideally here should be a recursive algorithm to find the smallest possible set of hosted files. But it is too complicated for now.
        //So here is the simple one
        var result = new HashSet<PackageProjectionFile>(itemsFiles.Where(x => x.Count == 1).Select(x => x[0]).Distinct());

        foreach (var itemFiles in itemsFiles.Where(x => x.Count > 1)) {
            if (result.ContainsAny(itemFiles))
                continue;
            result.Add(itemFiles.SmallestFile()); }
        return result; 
    }

    private static PackageProjectionFile
    SmallestFile(this IEnumerable<PackageProjectionFile> hostedFiles) => Collections.MinBy(hostedFiles, x => x.FileSize);

    private static IEnumerable<List<PackageProjectionFile>>
    GetFilesToDownload(this PackageDifference difference, PackageProjection projection) {
        //Probably will be faster to cache relevant hosted files in advance
        var relevantHostedFiles = projection.Files.Where(x => x.RelevantItemsIds.ContainsAny(difference.DifferentFilesIds)).ToList();

        foreach (var fileDifference in difference.DifferentFiles) {
            var hostedFiles = !fileDifference.ActualFileState.Exists
                ? FindFullHostedFile(fileDifference.PackageItemId, relevantHostedFiles).ToList()
                : FindDeltaHostedFile(fileDifference, relevantHostedFiles) .Union(FindFullHostedFile(fileDifference.PackageItemId, relevantHostedFiles)).ToList();
            if (hostedFiles.Count == 0)
                throw new InvalidOperationException($"Failed to find hosted file containing an updated version of {fileDifference.PackageFile.Path}");
            yield return hostedFiles; } 
    }

    private static IEnumerable<PackageProjectionFile>
    FindFullHostedFile(string packageItemId, IEnumerable<PackageProjectionFile> relevantHostedFiles) {
        foreach (var hostedFile in relevantHostedFiles)
            if (hostedFile.Content is PackageProjectionFileContent content)
                if (content.PackageFileIds.Contains(packageItemId))
                    yield return hostedFile; 
    }

    private static IEnumerable<PackageProjectionFile>
    FindDeltaHostedFile(PackageFileDifference fileDifference, IEnumerable<PackageProjectionFile> relevantHostedFiles) {
        foreach (var hostedFile in relevantHostedFiles)
            if (hostedFile.Content is PackageProjectionFileDeltaContent deltaContent) {
                var delta = deltaContent.PackageFileDeltas.FirstOrDefault(x =>
                    x.PackageFileId == fileDifference.PackageItemId &&
                    x.OldHash == fileDifference.ActualFileState.Hash &&
                    x.NewHash == fileDifference.PackageFile.FileHash);

                if (delta != null)
                    yield return hostedFile;
            }
    }
    
    public static PackageDifference
    DownloadPackageDifference(this PackageDifference difference, Updater Updater) {
        if (!difference.IsDifferent()) throw new InvalidOperationException("ActualState doesn't differ from the package");
        Updater.Logger.LogDebug("Clearing update directory...");
        Updater.UpdateFilesDirectory.ClearDirectoryIfExists();

        Updater.Logger.LogInformation($"Downloading update files for package {difference.Package}");
        difference.DownloadUpdateFiles(Updater);
        return difference;
    }
    
    public static InstallUpdateResult 
    InstallPackageDifference(this PackageDifference difference, Updater Updater) {
        if (!difference.IsDifferent())
            throw new InvalidOperationException($"Package {difference.Package.Header.Version} is already installed");
        Updater.Logger.LogInformation($"Installing package update {difference.Package}...");
        var remainingDifference = difference.InstallAccessibleFiles(Updater);
        return remainingDifference != null && remainingDifference.IsDifferent()
            ? new InstallUpdateResult(isRestartRequired: true)
            : new InstallUpdateResult(isRestartRequired: false);
    }

    internal static void
    UpdateUpdaterExe(this PackageFileDifference updaterExeDifference, Updater Updater) {
        if (!updaterExeDifference.PackageFile.Path.IsInstallExecutable())
            throw new ArgumentException(nameof(updaterExeDifference),$"Expecting a package difference for Updater.exe");

        if (!Updater.UpdateFilesDirectory.GetAllDirectoryFiles().TryGet(x => x.MatchGlob($"**/{SelfBinaries.InstallExecutable}"), out var updateFile))
            throw new InvalidOperationException("Update file for Updater.exe not found");

        if (Updater.IsBackupEnabled)
            Updater.BackupDirectory.CreateDirectoryIfAbsent();

        updaterExeDifference.UpdateFile(Updater, new Dictionary<string, string>(){{updaterExeDifference.PackageFile.FileHash, updateFile}});
    }

    public static void 
    VerifyInstallation(this Package package, string programDirectory) => package.Files.Values.ForEach(x => x.Verify( x.Path.ToAbsolute(programDirectory)));

    internal static PackageDifference? 
    InstallAccessibleFiles(this PackageDifference difference, Updater Updater) {
        Updater.InitBackupDirectory();
        var updateFilesHashes = difference.GetUpdateFilesHashes(Updater);
        var updateFilesCache = Collections.DistinctBy(updateFilesHashes, x => x.hash).ToDictionary(x => x.hash, x => x.file);
        
        var remainingDifferences = new List<PackageFileDifference>();
        foreach (var fileDifference in  difference.DifferentFiles)
            try {
                fileDifference.UpdateFile(Updater, updateFilesCache);
            }
            catch (Exception ex) when (ex is AccessViolationException or UnauthorizedAccessException or IOException) {
                Updater.Logger.LogInformation($"File {fileDifference.PackageFile.Path.Value.Quoted()} is not accessible. Restart app and running the update runner will be required.");
                remainingDifferences.Add(fileDifference);
                fileDifference.PrepareForRunner(Updater, updateFilesCache);
            }
        if (remainingDifferences.Count > 0)
            return new PackageDifference(package:difference.Package, fileDifferences:remainingDifferences);
        return null;
    }

    private static void 
    Install(this PackageDifference difference, Updater Updater) {
        Updater.InitBackupDirectory();

        var updateFilesHashes = difference.GetUpdateFilesHashes(Updater);
        var updateFilesCache = Collections.DistinctBy(updateFilesHashes, x => x.hash).ToDictionary(x => x.hash, x => x.file);
        difference.DifferentFiles.ForEach(x => x.UpdateFile(Updater, updateFilesCache));
        difference.Package.VerifyInstallation(Updater.ProgramDirectory);
    }

    private static void
    InitBackupDirectory(this Updater Updater) {
        if (Updater.IsBackupEnabled) {
            if (Updater.BackupDirectory.DirectoryExists())
                Updater.BackupDirectory.RemoveDirectory();
            Updater.BackupDirectory.CreateDirectoryIfAbsent();
        }
    }

    private static IEnumerable<(string hash, string file)>
    GetUpdateFilesHashes(this PackageDifference packageDifference, Updater Updater) {
        foreach (var fileDifference in packageDifference.DifferentFiles) {
            var packageFile = fileDifference.PackageFile;
            var file = packageFile.Path.ToAbsolute(Updater.UpdateFilesDirectory);
            if (file.FileExists()) {
                #if DEBUG
                Contract.Assert(file.GetFileHash() == packageFile.FileHash,
                    $"Update file hash is not equal expected in its packageFile ({file.Quoted()})");
                #endif
                yield return (packageFile.FileHash, file);
            }
            else
                yield return (packageFile.FileHash, fileDifference.GetDeltaFile(Updater.UpdateFilesDirectory));
        }
    }

    private static string
    GetDeltaFile(this PackageFileDifference difference, string updateFilesDirectory) =>
        Directory.EnumerateFiles(updateFilesDirectory, 
            $"*{PackageProjection.GetDeltaFileName(difference.ActualFileState.Hash, difference.PackageFile.FileHash)}",SearchOption.AllDirectories).FirstOrDefault()
            ?? throw new InvalidOperationException($"No file found in the update files directory for different package file {difference.PackageFile.Path}");

    internal static void
    PrepareForRunner(this PackageFileDifference fileDifference, Updater updater, Dictionary<string, string> fileHashToPath) {
        var runnerDirectory = updater.UpdateFilesDirectory.AppendPath(".uptoyou.runner").CreateDirectoryIfAbsent();
        
        if (!fileHashToPath.TryGetValue(fileDifference.PackageFile.FileHash, out var updateFile))
            throw new InvalidOperationException(
                $"Update file with hash = {fileDifference.PackageFile.FileHash.Quoted()} not found for packageFile {fileDifference.PackageFile.Path.Value.Quoted()}");

        var fileForRunner = fileDifference.PackageFile.Path.ToAbsolute(runnerDirectory);
        if (updateFile.IsPackageDeltaFile()) {
            if (!updateFile.IsUnpackedDeltaFile())
                throw new InvalidOperationException($"Expecting all delta files to be unpacked, but {updateFile.Quoted()} is packed");
            
            fileDifference.ActualFileState.Path.CopyFile(fileForRunner);
            fileForRunner.ApplyDelta(updateFile);
            updater.Logger.LogDebug($"{fileDifference.PackageFile.Path} copied to the runner's source directory and applied delta to.");
        }
        else {
            updateFile.CopyFile(fileForRunner);
            updater.Logger.LogDebug($"File copied to {fileForRunner}");
        }
    }

    private static void
    UpdateFile(this PackageFileDifference fileDifference, Updater Updater, Dictionary<string, string> fileHashToPath) {
        if (!fileHashToPath.TryGetValue(fileDifference.PackageFile.FileHash, out var updateFile))
            throw new InvalidOperationException(
                $"Update file with hash = {fileDifference.PackageFile.FileHash.Quoted()} not found for packageFile {fileDifference.PackageFile.Path.Value.Quoted()}");

        if (Updater.IsBackupEnabled)
            fileDifference.Backup(Updater);

        if (updateFile.IsPackageDeltaFile()) {
            if (!updateFile.IsUnpackedDeltaFile())
                throw new InvalidOperationException($"Expecting all delta files to be unpacked, but {updateFile.Quoted()} is packed");

            fileDifference.ActualFileState.Path.ApplyDelta(updateFile);
            Updater.Logger.LogDebug($"Applied delta to {fileDifference.PackageFile.Path}");
        }
        else {
            updateFile.CopyFile(fileDifference.ActualFileState.Path);
            Updater.Logger.LogDebug($"File copied to {fileDifference.PackageFile.Path}");
        }
    }

    private static void
    Backup(this PackageFileDifference fileDifference, Updater Updater) {
        if (fileDifference.ActualFileState.Exists)
            fileDifference.ActualFileState.Path.CopyFile(
                fileDifference.ActualFileState.Path
                    .GetPathRelativeTo(Updater.ProgramDirectory).Value
                    .ToAbsoluteFilePath(Updater.BackupDirectory));
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

    private static IEnumerable<PackageHeader>
    GetFreshUpdates(this IList<PackageHeader> packagesByVersion, Updater Updater) {
        #if DEBUG
        packagesByVersion.VerifyOrderedByVersion();
        #endif
        bool isInstalledPackageFound = false;
        foreach (var packageUpdate in packagesByVersion)
            if (isInstalledPackageFound)
                yield return packageUpdate;
            else if (packageUpdate.IsInstalled(Updater.ProgramDirectory))
                isInstalledPackageFound = true;
    }
    
    public static string 
    GetUpdateBackupDirectory(this PackageHeader update, Updater Updater) =>
        Updater.BackupDirectory.AppendPath(update.Name).CreateDirectoryIfAbsent();
    
}
}

using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using UpToYou.Core;

namespace UpToYou.Client {

public class 
InstallUpdateContext {
    public string UpdateFilesDirectory { get; }
    public string ProgramDirectory { get; }
    public bool Backup => !string.IsNullOrWhiteSpace(BackupDirectory);
    public string BackupDirectory { get; }
    public CancellationToken? CancellationToken { get; }
    public IUpdaterLogger? Log { get; }

    public InstallUpdateContext(string updateFilesDirectory, string programDirectory, string? backupDirectory, IUpdaterLogger? log, CancellationToken? cancellationToken = null) {
        UpdateFilesDirectory = updateFilesDirectory;
        ProgramDirectory = programDirectory;
        Log = log;
        CancellationToken = cancellationToken;
        BackupDirectory = string.IsNullOrWhiteSpace(backupDirectory) ? string.Empty : backupDirectory.CreateDirectoryIfAbsent();
    }
}

public class
DownloadAndInstallContext {
    public string UpdateFilesDirectory { get; }
    public string ProgramDirectory { get; }
    public string BackupDirectory { get; }
    public CancellationToken? CancellationToken { get; }
    public PackageHostClientContext Host { get; }
    public IProgressOperationObserver? ProgressObserver { get; }
    public IUpdaterLogger? Log { get; }

    public DownloadAndInstallContext(string updateFilesDirectory, string programDirectory, string backupDirectory, CancellationToken? cancellationToken, IUpdaterLogger? log, PackageHostClientContext host, IProgressOperationObserver? progressObserver) {
        UpdateFilesDirectory = updateFilesDirectory;
        ProgramDirectory = programDirectory;
        BackupDirectory = backupDirectory;
        CancellationToken = cancellationToken;
        Host = host;
        ProgressObserver = progressObserver;
        Log = log;
    }
}

public class InstallUpdateResult {
    public InstallUpdateResult(bool isCompleted, bool isRunnerExecutionRequired, Exception? exception = null) {
        IsCompleted = isCompleted;
        IsRunnerExecutionRequired = isRunnerExecutionRequired;
        Exception = exception;
    }
    public bool IsCompleted { get; }
    public bool IsRunnerExecutionRequired { get; }
    public bool IsApplicationShutDownRequired => IsRunnerExecutionRequired;
    public Exception? Exception { get; }
    public bool IsError => Exception != null;
    public string? ErrorMessage => Exception?.Message;
}

public static class InstallModule {

    public static string RunnerSourcesSubDirectory = ".uptoyou.runner";

    public static async Task<InstallUpdateResult>
    DownloadAndInstallAsync(this Package package,  DownloadAndInstallContext ctx) => 
        await Task.Run(() => DownloadAndInstall(package, ctx));

    public static InstallUpdateResult
    DownloadAndInstall(this Package package, DownloadAndInstallContext ctx) {
        try {
            ctx.ProgressObserver?.OnOperationChanged(Resources.AnaylyzingFilesDifferenceDotted);

            var difference = package.GetDifference(ctx.ProgramDirectory, true);
            if (!difference.IsDifferent())
                throw new InvalidOperationException($"Package {package.Metadata.Version} is already installed");

            if (ctx.Log != null)
                foreach (var differentFile in difference.DifferentFiles)
                    ctx.Log.LogDebug($"{differentFile.PackageFile.Path} differs");

            ctx.ProgressObserver?.OnOperationChanged(Resources.ClearingUpdateDirectoryDotted);
            ctx.UpdateFilesDirectory.ClearDirectoryIfExists();

            ctx.ProgressObserver?.OnOperationChanged(Resources.DownloadingUpdateFilesDotted, new Progress());
            ctx.Log?.LogInfo($"Downloading update files for package {package}");
            difference.DownloadUpdateFiles(new DownloadUpdateContext(
                outputDirectory: ctx.UpdateFilesDirectory,
                progressContext: ctx.Host.ProgressContext,
                host: ctx.Host,
                progressOperationObserver: ctx.ProgressObserver));

            ctx.ProgressObserver?.OnOperationChanged(Resources.InstallingUpdateDotted, null);
            ctx.Log?.LogInfo("Installing update...");
            var remainingDifference = difference.InstallAccessibleFiles(new InstallUpdateContext(
                updateFilesDirectory: ctx.UpdateFilesDirectory,
                ctx.ProgramDirectory,
                ctx.BackupDirectory,
                ctx.Log));

            return remainingDifference != null && remainingDifference.IsDifferent()
                ? new InstallUpdateResult(isCompleted: false, isRunnerExecutionRequired: true)
                : new InstallUpdateResult(isCompleted: true, isRunnerExecutionRequired: false);
        }
        catch (Exception ex) {
            ctx.Log?.LogException(UpdaterLogLevels.Error, "Failed to download and install update", ex);
            return new InstallUpdateResult(false, false, ex);
        }
    } 

    internal static void
    UpdateUpdaterExe(this PackageDifference packageDifference, string updateFilesDir, string programDir) =>
        packageDifference.DifferentFiles.FirstOrDefault(x => x.PackageFile.Path.IsInstallExecutable())?
            .UpdateUpdaterExe(updateFilesDir, programDir);

    internal static void
    UpdateUpdaterExe(this PackageFileDifference updaterExeDifference, string updateFilesDir, string programDir) {
        if (!updaterExeDifference.PackageFile.Path.IsInstallExecutable())
            throw new ArgumentException(nameof(updaterExeDifference),$"Expecting a package difference for Updater.exe");

        var updateFile = updateFilesDir.EnumerateAllDirectoryFiles().FirstOrDefault(x => x.MatchGlob($"**/{SelfBinaries.InstallExecutable}"))
            ?? throw new InvalidRemoteDataException("Update file for Updater.exe not found");

        var ctx = new InstallUpdateContext(updateFilesDir, programDir, null, null, null);
        if (ctx.Backup)
            ctx.BackupDirectory.CreateDirectoryIfAbsent();

        updaterExeDifference.UpdateFile(ctx, new Dictionary<string, string>(){{updaterExeDifference.PackageFile.FileHash, updateFile}});
    }

    public static void VerifyInstallation(this Package package, string programDirectory) {
        package.Files.Values.ForEach(x => x.Verify( x.Path.ToAbsolute(programDirectory)));
       // package.Folders.ForEach(x => x.Verify(x.Path.ToAbsolute(programDirectory)));
    }


    internal static PackageDifference? 
    InstallAccessibleFiles(this PackageDifference difference, InstallUpdateContext ctx) {
        ctx.InitBackupDirectory();
        var updateFilesCache = difference.GetUpdateFilesHashes(ctx).DistinctBy(x => x.hash).ToDictionary(x => x.hash, x => x.file);
        
        var remainingDifferences = new List<PackageFileDifference>();
        foreach (var fileDifference in  difference.DifferentFiles)
            try {
                fileDifference.UpdateFile(ctx, updateFilesCache);
            }
            catch (Exception ex) when (ex is AccessViolationException || ex is UnauthorizedAccessException) {
                ctx.Log?.LogInfo($"File {fileDifference.PackageFile.Path.Value.Quoted()} is not accessible.");
                remainingDifferences.Add(fileDifference);
                fileDifference.PrepareForRunner(ctx, updateFilesCache);
            }
        if (remainingDifferences.Count > 0)
            return new PackageDifference(package:difference.Package, fileDifferences:remainingDifferences);
        return null;
    }

    private static void 
    Install(this PackageDifference difference, InstallUpdateContext ctx) {
        ctx.InitBackupDirectory();

        var updateFilesCache = difference.GetUpdateFilesHashes(ctx).DistinctBy(x => x.hash).ToDictionary(x => x.hash, x => x.file);
        difference.DifferentFiles.ForEach(x => x.UpdateFile(ctx, updateFilesCache));
        difference.Package.VerifyInstallation(ctx.ProgramDirectory);
    }

    private static void
    InitBackupDirectory(this InstallUpdateContext ctx) {
        if (ctx.Backup) {
            if (ctx.BackupDirectory.DirectoryExists())
                ctx.BackupDirectory.RemoveDirectory();
            ctx.BackupDirectory.CreateDirectoryIfAbsent();
        }
    }

    private static IEnumerable<(string hash, string file)>
    GetUpdateFilesHashes(this PackageDifference packageDifference, InstallUpdateContext ctx)
    {
        foreach (var fileDifference in packageDifference.DifferentFiles) {
            var packageFile = fileDifference.PackageFile;
            var file = packageFile.Path.ToAbsolute(ctx.UpdateFilesDirectory);
            if (file.FileExists()) {
#if DEBUG
                Contract.Assert(file.GetFileHash() == packageFile.FileHash,
                    $"Update file hash is not equal expected in its packageFile ({file.Quoted()})");
#endif
                yield return (packageFile.FileHash, file);
            }
            else
                yield return (packageFile.FileHash, fileDifference.GetDeltaFile(ctx.UpdateFilesDirectory));

        }
    }

    private static string
    GetDeltaFile(this PackageFileDifference difference, string updateFilesDirectory) =>
        Directory.EnumerateFiles(updateFilesDirectory, 
            $"*{PackageProjection.GetDeltaFileName(difference.ActualFileState.Hash, difference.PackageFile.FileHash)}",SearchOption.AllDirectories).FirstOrDefault()
            ?? throw new InvalidRemoteDataException($"No file found in the update files directory for different package file {difference.PackageFile.Path}");

    internal static void
    PrepareForRunner(this PackageFileDifference fileDifference, InstallUpdateContext ctx, Dictionary<string, string> updateFilesCache) {
        var runnerDirectory = ctx.UpdateFilesDirectory.AppendPath(RunnerSourcesSubDirectory).CreateDirectoryIfAbsent();
        
        if (!updateFilesCache.TryGetValue(fileDifference.PackageFile.FileHash, out var updateFile))
            throw new InvalidRemoteDataException(
                $"Update file with hash = {fileDifference.PackageFile.FileHash.Quoted()} not found for packageFile {fileDifference.PackageFile.Path.Value.Quoted()}");

        var fileForRunner = fileDifference.PackageFile.Path.ToAbsolute(runnerDirectory);
        if (updateFile.IsPackageDeltaFile()) {
            if (!updateFile.IsUnpackedDeltaFile())
                throw new InvalidDataException($"Expecting all delta files to be unpacked, but {updateFile.Quoted()} is packed");
            
            fileDifference.ActualFileState.Path.CopyFile(fileForRunner);
            fileForRunner.ApplyDelta(updateFile);
            ctx.Log?.LogDebug($"{fileDifference.PackageFile.Path} copied to the runner's source directory and applied delta to.");
        }
        else {
            updateFile.CopyFile(fileForRunner);
            ctx.Log?.LogDebug($"File copied to {fileForRunner}");
        }
    }

    private static void
    UpdateFile(this PackageFileDifference fileDifference, InstallUpdateContext ctx, Dictionary<string, string> updateFilesCache) {

        //if (fileDifference.PackageFile.Path.IsSelfBinaryFile())
        //    throw new InvalidOperationException($"Can't update loaded libraries or executables: {fileDifference.PackageFile.Path.Value.Quoted()}");

        if (!updateFilesCache.TryGetValue(fileDifference.PackageFile.FileHash, out var updateFile))
            throw new InvalidRemoteDataException(
                $"Update file with hash = {fileDifference.PackageFile.FileHash.Quoted()} not found for packageFile {fileDifference.PackageFile.Path.Value.Quoted()}");

        if (ctx.Backup)
            fileDifference.Backup(ctx);

        if (updateFile.IsPackageDeltaFile()) {
            if (!updateFile.IsUnpackedDeltaFile())
                throw new InvalidDataException($"Expecting all delta files to be unpacked, but {updateFile.Quoted()} is packed");

            fileDifference.ActualFileState.Path.ApplyDelta(updateFile);
            ctx.Log?.LogDebug($"Applied delta to {fileDifference.PackageFile.Path}");
        }
        else {
            updateFile.CopyFile(fileDifference.ActualFileState.Path);
            ctx.Log?.LogDebug($"File copied to {fileDifference.PackageFile.Path}");
        }
    }

    private static void
    Backup(this PackageFileDifference fileDifference, InstallUpdateContext ctx) {
        if (fileDifference.ActualFileState.Exists)
            fileDifference.ActualFileState.Path.CopyFile(
                fileDifference.ActualFileState.Path
                    .GetPathRelativeTo(ctx.ProgramDirectory).Value
                    .ToAbsoluteFilePath(ctx.BackupDirectory));
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



    }
}

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using UpToYou.Core;

namespace UpToYou.Client {

public interface 
IApplicationShutDown {
    void ShutDown();
}

public class 
ApplicationUpdateContext {
    //public List<Update> InstalledUpdates { get; private  set;}
    //public List<Update> NewUpdates{ get; }
    public string ProgramDirectory { get; }
    public string  UpdateDirectory { get; }
    public string BackupDirectory { get; }
    public UpdatesManifest UpdatesManifest { get; }
    public IFilesHostClient FilesHost { get; }
    public IUpdaterLogger? Log { get; }
    public IUpdateRingPolicy UpdateRingPolicy { get; }
    private IApplicationShutDown _applicationShutDown { get; }
    private Func<Update, bool> _updatesFilter { get; }

    public InstallingUpdateState? InstallingUpdateState { get; private set; }

    public event Action<string> PackageInstallStarted;
    public event Action<string> PackageInstallCompleted;

    internal const string DefaultUpdateFilesSubDirectory = "_updates";
    internal const string DefaultBackupSubDirectory = "_backup";

    internal List<(string packageName, UpdatesByVersion updates, Version? installedUpdateVersion)> UpdatesByPackageName;
    
    //Update notes per locale
    private readonly Dictionary<string, List<UpdateNotes>> _updateNotes = new Dictionary<string, List<UpdateNotes>>();
    private readonly object _lockObject= new object();

    public ApplicationUpdateContext(
        UpdatesManifest updatesManifest, 
        IFilesHostClient filesHost, 
        IApplicationShutDown applicationShutDown,
        IUpdateRingPolicy updateRingPolicy, 
        string? programDirectory = null, 
        string? updateDirectory = null, 
        string? backupDirectory = null, 
        IUpdaterLogger? log = null, 
        Func<Update, bool>? updatesFilter = null) 
    {
        UpdatesManifest = updatesManifest;
        FilesHost = filesHost;
        UpdateRingPolicy = updateRingPolicy;
        _updatesFilter = updatesFilter ?? new Func<Update, bool>( x => true);
        _applicationShutDown = applicationShutDown;
        ProgramDirectory  = programDirectory ?? Environment.CurrentDirectory;
        UpdateDirectory =updateDirectory ?? DefaultUpdateFilesSubDirectory.ToAbsoluteFilePath(ProgramDirectory);
        BackupDirectory =backupDirectory?? DefaultBackupSubDirectory.ToAbsoluteFilePath(ProgramDirectory);
        Log = log;
        UpdatesByPackageName = updatesManifest.Updates.Where(_updatesFilter).SplitByPackageName().MapToList(
            x => (x.packageName, x.updates, x.updates.FirstOrDefault(y => y.PackageMetadata.IsInstalled(ProgramDirectory))?.PackageMetadata.Version));
    }

    public async Task<List<UpdateNotes>> GetUpdateNotesAsync(string locale) {
        lock (_lockObject) {
            if (_updateNotes.TryGetValue(locale, out var res))
                return res;
        }

        return await Task.Run(() => GetUpdateNotes(locale));
    }

    private List<UpdateNotes> GetUpdateNotes(string locale) {
        lock (_lockObject) {
            if (_updateNotes.TryGetValue(locale, out var res))
                return res;
            var notes = UpdatesManifest.Updates.Where(_updatesFilter).ToList().DownloadUpdateNotes(locale, FilesHost);
            _updateNotes.Add(locale, notes);
            return notes;
        }
    }

    public static ApplicationUpdateContext 
    Create(IFilesHostClient filesHost, 
        IApplicationShutDown applicationShutDown,
        IUpdateRingPolicy updateRingPolicy, 
        string? programDirectory = null, 
        string? updateDirectory = null, 
        string? backupDirectory = null, 
        IUpdaterLogger? log = null, 
        Func<Update, bool>? updatesFilter = null) {

        new PackageHostClientContext(filesHost, log, null).TryDownloadUpdatesManifest(out var updateManifest);
        return new ApplicationUpdateContext(updateManifest ?? new UpdatesManifest(new List<Update>()), filesHost, applicationShutDown, updateRingPolicy, programDirectory, updateDirectory, backupDirectory, log, updatesFilter);
    }

    public string GetUpdateBackupDirectory(Update update) {
        if (string.IsNullOrWhiteSpace(BackupDirectory))
            return string.Empty;

        //Removed backup of each version into a separate directory to avoid the uncontrolled growth of _backup folder size
        return string.IsNullOrWhiteSpace(update.PackageMetadata.Name) ? BackupDirectory : BackupDirectory.AppendPath(update.PackageMetadata.Name).CreateDirectoryIfAbsent();
        //var installedVersion =update.GetInstalledVersion(ProgramDirectory);
        //if (installedVersion == null)
        //    return string.IsNullOrWhiteSpace(update.PackageMetadata.Name) ? BackupDirectory : BackupDirectory.AppendPath(update.PackageMetadata.Name).CreateDirectoryIfAbsent();

        //return string.IsNullOrWhiteSpace( update.PackageMetadata.Name) 
        //    ? installedVersion.ToString().ToAbsoluteFilePath(BackupDirectory).CreateDirectoryIfAbsent()
        //    : BackupDirectory.AppendPath(update.PackageMetadata.Name).AppendPath(installedVersion.ToString()).CreateDirectoryIfAbsent();
    }

    public bool IsInstalled(Update update) {
        var version = UpdatesByPackageName.FirstOrDefault(x => x.packageName == update.PackageMetadata.Name).installedUpdateVersion;
        if (version == null)
            return false;
        return version >= update.PackageMetadata.Version;
    }

    public PackageHostClientContext 
    Host(ProgressContext? pCtx = null) => 
        new PackageHostClientContext(FilesHost, Log, pCtx);

    internal UpdateContext 
    UpdateContext(ProgressContext? pCtx = null) => 
        new UpdateContext(Host(pCtx), Log, ProgramDirectory, UpdateDirectory, UpdateRingPolicy);

    internal DownloadAndInstallContext 
    DownloadAndInstallContext(Update update, ProgressContext? pCtx = null) => 
        new DownloadAndInstallContext(
            UpdateDirectory,
            ProgramDirectory,
            GetUpdateBackupDirectory(update),
            InstallingUpdateState?.CancellationTokenSource.Token,
            Log,
            Host(pCtx),
            InstallingUpdateState);

    public bool 
    IsInstalling => InstallingUpdateState != null;

    public IEnumerable<Update> 
    AllUpdates => UpdatesManifest.Updates.Where(_updatesFilter);

    public IEnumerable<Update>
    NewUpdates => UpdatesByPackageName.SelectMany(x => x.updates.GetNewUpdates(this));

    public Update 
    FindPackage(string packageId) => AllUpdates.FirstOrDefault(x => x.PackageMetadata.Id == packageId);

    public Update
    FindUpdate(string packageName, Version version) => AllUpdates.FirstOrDefault(x => x.PackageMetadata.IsSamePackage(version, packageName));

    public void 
    OnPackageInstallStarted(string packageId) {
        InstallingUpdateState = new InstallingUpdateState(packageId);
        PackageInstallStarted?.Invoke(packageId);
    }

    public void OnPackageInstallCompleted(PackageMetadata packageMetadata, InstallUpdateResult result) {
        if (result.IsCompleted)
            OnPackageInstalled(packageMetadata);
        else if (result.IsRunnerExecutionRequired)
            InstallingUpdateState?.OnOperationChanged(Resources.WaitingForApplicatoinRestartDotted, null);
    }

    public void
    OnPackageInstalled(PackageMetadata packageMetadata) {
        InstallingUpdateState = null;
        var updates = UpdatesByPackageName.FirstOrDefault(x => x.packageName == packageMetadata.Name);
        if (updates != default)
            updates.installedUpdateVersion = packageMetadata.Version;
        else 
            throw new InvalidOperationException("Installed update which was absent in the updates manifest");
        PackageInstallCompleted?.Invoke(packageMetadata.Id);
    }

    public void
    ShutDown() {
        if (_applicationShutDown != null)
            _applicationShutDown.ShutDown();
        else 
            throw new InvalidOperationException("IApplicationShutDown should be defined");
    }
}

public interface IHasProgressOperation {
    ProgressOperation? ProgressOperation { get; }
} 

public class 
InstallingUpdateState: INotifyPropertyChanged, IProgressOperationObserver, IHasProgressOperation, IDisposable {
    public string PackageId { get; }
    public ProgressOperation? ProgressOperation{ get;private set;}
    public CancellationTokenSource CancellationTokenSource { get; } = new CancellationTokenSource();

    public InstallingUpdateState(string packageId, ProgressOperation? progress = null) =>
        (PackageId, ProgressOperation) = (packageId, progress);

    public void 
    OnProgressChanged(Progress progress) => 
        ProgressOperation = new ProgressOperation(ProgressOperation?.Operation, progress);

    public void 
    OnOperationChanged(string operation, Progress? progress) => 
        ProgressOperation = new ProgressOperation(operation, progress);

    public void 
    Dispose() => CancellationTokenSource?.Dispose();

    //INotifyPropertyChanged
    public event PropertyChangedEventHandler PropertyChanged;
    public void RaisePropertyChanged(string propertyName) => 
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
}

public static class  ApplicationContextEx {

    public static bool
    IsInstalled(this PackageDependency dependency, ApplicationUpdateContext ctx) {
        var update = ctx.FindUpdate(dependency.PackageName, dependency.MinVersion);
        return update.IsInstalled(ctx);
    }

    public static IEnumerable<Update>
    FindRequiredStartupUpdates(this ApplicationUpdateContext ctx) {
        foreach (var entry in ctx.UpdatesByPackageName) {
            var lastRequiredUpdate = entry.updates.FirstOrDefault(x => !x.UpdatePolicy.IsLazy && x.UpdatePolicy.IsRequired);
            if (lastRequiredUpdate != null && !lastRequiredUpdate.IsInstalled(ctx))
                yield return lastRequiredUpdate;
        }
    }

    public static IEnumerable<Update>
    FindAutoStartupUpdates(this ApplicationUpdateContext ctx) {
        foreach (var entry in ctx.UpdatesByPackageName) {
            var lastAutoUpdate = entry.updates.FirstOrDefault(x => !x.UpdatePolicy.IsLazy && x.UpdatePolicy.IsAutoUpdate(x.GetInstalledVersion(ctx)));
            if (lastAutoUpdate != null && !lastAutoUpdate.IsInstalled(ctx))
                yield return lastAutoUpdate;
        }
    }

    public static async Task< InstallUpdateResult>
    DownloadAndInstallAsync(this Update update, ApplicationUpdateContext appCtx) =>
        await Task.Run(() => update.DownloadAndInstall(appCtx));

    public static InstallUpdateResult
    DownloadAndInstall(this Update update, ApplicationUpdateContext appCtx) {
        appCtx.OnPackageInstallStarted(update.PackageMetadata.Id);
        var ctx = appCtx.DownloadAndInstallContext(update, new ProgressContext(appCtx.InstallingUpdateState,
            Download.RelevantDownloadSpeedTimeSpan));
        
        ctx.ProgressObserver?.OnOperationChanged(Resources.DownloadingPackageManifestDotted);
        var result= update.DownloadPackage(ctx.Host).DownloadAndInstall(ctx);
        appCtx.OnPackageInstallCompleted(update.PackageMetadata,result);
        return result;
    }

    public static bool 
    IsInstalled(this Update package, ApplicationUpdateContext ctx) => ctx.IsInstalled(package);

    public static Version?
    GetInstalledVersion(this Update update, ApplicationUpdateContext ctx) {
        var file = update.PackageMetadata.VersionProviderFile.Path.ToAbsolute(ctx.ProgramDirectory);
        if (file.FileExists())
            return file.GetFileVersion();
        return null;
    }

    public static IEnumerable<Update>
    GetNewUpdates(this UpdatesByVersion updates, ApplicationUpdateContext ctx) => updates.Where(x => {
        var installedVersion = x.GetInstalledVersion(ctx);
        return installedVersion == null || x.PackageMetadata.Version > installedVersion;
    });
}

}
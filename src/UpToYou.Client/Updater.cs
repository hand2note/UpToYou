using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.ComponentModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using UpToYou.Core;

namespace UpToYou.Client {

public class 
Updater {
    public IHostClient HostClient { get; }
    public ILogger Logger { get; }
    public UpdaterOptions Options {get;}

    public Updater(IHostClient hostClient, ILogger logger, UpdaterOptions options) {
        HostClient = hostClient;
        Logger = logger;
        Options = options;
    }
    
    public static Updater 
    Default(IHostClient host) => new (host, NullLogger.Instance, UpdaterOptions.Default);

    public string ProgramDirectory => Options.ProgramDirectory;
    public string UpdateFilesDirectory => Options.UpdateFilesDirectory;
    public string BackupDirectory => Options.BackupDirectory;
    public bool IsBackupEnabled => Options.EnableBackup;
}

public class 
UpdaterOptions {
    public string ProgramDirectory { get; }
    public string UpdateFilesDirectory { get; }
    public string BackupDirectory { get; }
    public bool EnableBackup {get;}
    public UpdaterOptions(string programDirectory, string updateFilesDirectory, string backupDirectory, bool enableBackup) {
        ProgramDirectory = programDirectory;
        UpdateFilesDirectory = updateFilesDirectory;
        BackupDirectory = backupDirectory;
        EnableBackup = enableBackup;
    }
    public static UpdaterOptions
    Default = new(
        programDirectory: Environment.CurrentDirectory,
        updateFilesDirectory: Environment.CurrentDirectory.AppendPath("_updates"),
        backupDirectory: Environment.CurrentDirectory.AppendPath("_backup"),
        enableBackup: true);
}

public class 
InstallUpdateResult {
   public bool IsRestartRequired {get;}
   public InstallUpdateResult(bool isRestartRequired) => IsRestartRequired = isRestartRequired;
}
}
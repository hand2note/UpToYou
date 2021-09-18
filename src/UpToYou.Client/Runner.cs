using System;
using System.Diagnostics;
using System.Linq;
using UpToYou.Core;

namespace UpToYou.Client
{
    public static class Runner
    {
        public static void RunUpdater(string? updateFilesDirectory = null, string? backupDirectory = null) {
            if (updateFilesDirectory == null)
                updateFilesDirectory = ApplicationUpdateContext.DefaultUpdateFilesSubDirectory.ToAbsoluteFilePath(Environment.CurrentDirectory);
            if (backupDirectory == null)
                backupDirectory = ApplicationUpdateContext.DefaultBackupSubDirectory.ToAbsoluteFilePath(Environment.CurrentDirectory);
            if (!updateFilesDirectory.EndsWith(InstallModule.RunnerSourcesSubDirectory))
                updateFilesDirectory = updateFilesDirectory.AppendPath(InstallModule.RunnerSourcesSubDirectory);
            var applicationStartupFile = Process.GetCurrentProcess().MainModule?.FileName;
            var processesIdsToWaitExit = new int[]{ Process.GetCurrentProcess().Id};
            Process.Start("UpToYou.Client.Runner.exe", $"{updateFilesDirectory.Quoted()} {backupDirectory.Quoted()} {string.Join(",", processesIdsToWaitExit.MapToList(x => x.ToString()))}  {applicationStartupFile.Quoted()}");

        }
    }
}
 
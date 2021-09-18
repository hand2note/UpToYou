using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Resources;
using System.Security.Cryptography;
using System.Threading.Tasks;

namespace Updater {
    class Options {
        public string UpdateFilesDirectory;
        public string BackupDirectory;
        public List<int> ProcessesIdsToWait;
        public string ApplicationStartupFile;

        public static Options FromArgs(string[] args) =>
            new Options() {
                UpdateFilesDirectory = args[0],
                BackupDirectory = args[1],
                ProcessesIdsToWait = args[2].Split(',').Select(x => int.Parse(x.Trim())).ToList(),
                ApplicationStartupFile = args[3]
            };
    }

    class Program {

        static void Main(string[] args) {
                Console.WriteLine("Begin the installation of the update");
                Console.WriteLine("Args:");
                foreach (var arg in args) Console.WriteLine(arg);
                Task.Delay(1000).Wait();
                try {
                    Install(Options.FromArgs(args));
                }
                catch (Exception) {
                    Task.Delay(3000).Wait();
                    try {
                        Install(Options.FromArgs(args));
                    }
                    catch (Exception ex) {
                        Console.WriteLine(ex);
                        Console.WriteLine("Installation failed. Sorry for inconvenience.");
                        Console.ReadKey();
                    }
                }
               
        }

        static void Install(Options options) {
            if (File.Exists("updaterequested"))
                File.Delete("updaterequested");

            WaitForProcessesToExit(options);
            foreach (var updateFile in Directory.GetFiles(options.UpdateFilesDirectory, "*", SearchOption.AllDirectories)) {
                var actualFile = GetActualFilePath(updateFile, options);
                actualFile.BackupFile(options);
                if (!File.Exists(actualFile)) {
                    File.Copy(updateFile, actualFile);
                    Console.WriteLine($"Added new file {actualFile}");
                }
                else if (!AreSameFiles(updateFile, actualFile)) {
                    File.Copy(updateFile, actualFile, overwrite: true);
                    Console.WriteLine($"Updated file {actualFile}");
                }
                else
                    Console.WriteLine("Skipped file because its updated version is the same as actual one: " + actualFile);
            }

            Console.WriteLine("The update has been successfully installed");
            RunApplication(options);
        }

        private static void RunApplication(Options option) {
            if (File.Exists(option.ApplicationStartupFile))
                Process.Start(option.ApplicationStartupFile);
            else {
                Console.WriteLine("Update completed! Please, start the application.");
                Console.ReadKey();
            }
        }

        private static void WaitForProcessesToExit(Options options) {
            foreach (var process in options.ProcessesIdsToWait.Select(FindProcessById).Where(x => x!=null)){
                //if (process.StartInfo.FileName.StartsWith(Environment.CurrentDirectory)) {
                Console.WriteLine($"Waiting for process \"{process.ProcessName}\" to exit");
                try {
                    process.WaitForExit();
                    Task.Delay(1000).Wait();
                }
                catch { }
            }
        }

     
        private static Process? FindProcessById(int processId) {
            try {
                return Process.GetProcessById(processId);
            }
            catch (ArgumentException) {
                return null;
            }
        }

        private static bool AreSameFiles(string file1, string file2) =>
            new FileInfo(file1).Length == new FileInfo(file2).Length &&
            file1.GetFileHash() == file2.GetFileHash();

        private static string GetActualFilePath(string updateFilePath, Options options) =>
            Path.Combine(Environment.CurrentDirectory, updateFilePath.GetRelativePath(options.UpdateFilesDirectory));
    }

    internal static class Helpers {

        public static void BackupFile(this string actualFile, Options options) {
            if (!string.IsNullOrWhiteSpace(options.BackupDirectory))
                File.Copy(actualFile, Path.Combine(options.BackupDirectory, actualFile.GetRelativePath(Environment.CurrentDirectory)).CreateParentDirectoryIfAbsent(), overwrite:true);
        }

        public static string
        GetFileHash(this string file) {
            using var md5 = MD5.Create();
            using var fs = File.OpenRead(file);
            var hash = md5.ComputeHash(fs);
            return BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant();
        }

        public static string GetRelativePath(this string filespec, string folder) {
            Uri pathUri = new Uri(filespec);
            // Folders must end in a slash
            if (!folder.EndsWith(Path.DirectorySeparatorChar.ToString())) {
                folder += Path.DirectorySeparatorChar;
            }
            Uri folderUri = new Uri(folder);
            return Uri.UnescapeDataString(folderUri.MakeRelativeUri(pathUri).ToString().Replace('/', Path.DirectorySeparatorChar));
        }
    }

public static class FileSystemEx {

    public static string
        CreateDirectoryIfAbsent(this string path) {
        if (!Directory.Exists(path)) 
            Directory.CreateDirectory(path);
        return path; }
       
    public static string
    CreateParentDirectoryIfAbsent(this string path) {
        path.GetParentDirectory().CreateDirectoryIfAbsent();
        return path; } 

    public static string
    GetParentDirectory(this string path) => Directory.GetParent(path)?.FullName ?? throw new InvalidOperationException($"Parent directory doesn't exist for path {path}");



}
}

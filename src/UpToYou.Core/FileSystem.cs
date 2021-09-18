using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;

namespace UpToYou.Core {

internal static class
FileSystem {

    public static string
    AppendPath(this string path1, string path2) {
        if (Path.IsPathRooted(path2)) throw new ArgumentException("Can't append rooted path", nameof(path2));

        if (Path.IsPathRooted(path1))
            return Path.Combine(path1, path2);
        return (path1 + "\\" + path2).Replace("\\\\","\\");
    }

    public static bool
    PathExists(this string path) => Directory.Exists(path) || File.Exists(path);

    public static bool
    FileExists(this string path) => File.Exists(path);

    public static string
    VerifyFileExistence(this string path) => path.FileExists() ? path : throw new FileNotFoundException($"File {path} doesn't exists");

    public static FileStream
    OpenFileForRead(this string file) => File.OpenRead(file);

    public static FileStream
    OpenFileForWrite(this string file) => File.OpenWrite(file);

    public static long
    GetFileSize(this string file) => new FileInfo(file).Length;

    public static Version?
    GetFileVersion(this string file) {
        var fi = FileVersionInfo.GetVersionInfo(file);
        if ((fi.FileMajorPart, fi.FileMinorPart,  fi.FileBuildPart,  fi.FilePrivatePart) == (0,0,0,0))
            return null;
        return new Version(fi.FileMajorPart, fi.FileMinorPart, fi.FileBuildPart, fi.FilePrivatePart);
    }

    public static string
    GetFileName(this string file) => new FileInfo(file).Name;

    public static string
    GetFileNameWithoutExtension(this string file) {
        var fileName = file.GetFileName();
        int dotIndex = fileName.IndexOf(".", StringComparison.Ordinal);
        return dotIndex == -1 ? fileName : file.Substring(0, dotIndex);
    }

    public static string
    RemoveFile(this string file) { File.Delete(file); return file;}

    public static string
    RemoveFileIfExists(this string file) { if (File.Exists(file)) File.Delete(file); return file; }

    public static FileStream
    CreateFile(this string path) => File.Create(path);

    public static string
    GetParentDirectory(this string path) => Directory.GetParent(path)?.FullName ?? throw new InvalidOperationException($"Parent directory doesn't exist for path {path.Quoted()}");

    public static string
    CreateDirectory(this string path) { Directory.CreateDirectory(path); return path; }

    public static string
    CreateSubDirectory(this string dir, string subPath) => dir.AppendPath(subPath).CreateDirectory();

    public static string
    VerifyDirectoryAbsent(this string dir) => !dir.DirectoryExists() ? dir : throw new InvalidOperationException($"Directory {dir.Quoted()} already exists");

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
    CopyFile(this string src, string dest) {
        src.VerifyFileExistence();
        File.Copy(src.VerifyFileExistence(), dest.RemoveFileIfExists().CreateParentDirectoryIfAbsent());
        return dest; }

    public static string
    MoveFile(this string src, string dest) {
        File.Move(src.VerifyFileExistence(), dest.RemoveFileIfExists().CreateParentDirectoryIfAbsent());
        return dest; }

    public static string
    ReplaceInFileName(this string file, string from, string to) => MoveFile(file, file.Replace(from, to));

    public static bool
    DirectoryExists(this string path) => Directory.Exists(path);

    public static bool
    IsDirectory(this string path) => File.GetAttributes(path).HasFlag(FileAttributes.Directory);

    public static string
    VerifyDirectoryExistence(this string path) => path.DirectoryExists() ? path : throw new DirectoryNotFoundException(path);

    public static string
    RemoveDirectory(this string path) { Directory.Delete(path, true); return path; }

    public static void
    ClearDirectoryIfExists(this string directory) {
        if (Directory.Exists(directory))
            directory.ClearDirectory();
    }

    public static string
    ClearDirectory(this string directory) {
        foreach (var file in directory.EnumerateAllDirectoryFiles()) 
            File.Delete(file);

        foreach(var childDirectory in new DirectoryInfo(directory).EnumerateDirectories())
            childDirectory.Delete(true);

        return directory; 
    }

    public static string
    MoveDirectory(this string src, string dest) { Directory.Move(src, dest); return dest;}

    public static IEnumerable<string>
    EnumerateAllDirectoryFiles(this string dir, bool recursive = true) => Directory.EnumerateFiles(dir.VerifyDirectoryExistence(), "*", recursive ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly);

    public static IEnumerable<string>
    EnumerateChildDirectories(this string dir, bool recursive = false) => Directory.GetDirectories(dir, "*", recursive? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly);

    public static int
    GetDirectoryFilesCount(this string dir) => dir.EnumerateAllDirectoryFiles().Count();

    public static string? 
    FindFile(this string dir, Func<string, bool> predicate) => dir.EnumerateAllDirectoryFiles().Where(predicate).FirstOrDefault();

    public static string
    AppendFileExtension(this string file, string extension) => file + extension;

    public static string 
    AppendFileExtensions(this string file, params string[] extensions) => extensions.Aggregate(file, (s, x) => s.AppendFileExtension(x));

    public static bool
    FileHasExtension(this string file, string extension) => file.EndsWith(extension);

    public static bool
    FileContainsExtension(this string file, string extension) => file.FileHasExtension(extension) || file.GetFileName().Contains(extension + ".");

    public static string
    AppendFileExtensionIfAbsent(this string file, string extension) => file.FileContainsExtension(extension) ? file : file.AppendFileExtension(extension);

    public static string
    OverwriteToFile(this byte[] bytes, string file) {
        using var fs = file.CreateFile();
        fs.Write(bytes, 0, bytes.Length);
        return file; }

    public static FileStream
    OpenFileOverwrite(this string file) => File.Open(file, FileMode.Create);

    public static string
    ToAbsoluteFilePath(this string file, string root) => root.AppendPath(file);

    public static string
    ReadAllFileText(this string file) => File.ReadAllText(file);

    public static string? 
    ReadAllFileTextIfExists(this string file) => File.Exists(file) ? File.ReadAllText(file) : null;

    public static byte[]
    ReadAllFileBytes(this string file) => File.ReadAllBytes(file);

    public static string
    CopyDirectory(this string src, string dest) {
        src.VerifyDirectoryExistence();
        dest.CreateDirectoryIfAbsent();

        var srcDir = new DirectoryInfo(src);

        foreach (var file in srcDir.GetFiles()) 
            file.CopyTo(dest.AppendPath(file.Name));

        foreach (var subDir in srcDir.GetDirectories()) {
            CopyDirectory(subDir.FullName, dest.AppendPath(subDir.Name));
        }
        return dest;
    }
}
}

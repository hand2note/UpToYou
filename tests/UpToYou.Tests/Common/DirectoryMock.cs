using System;
using System.Collections.Generic;
using UpToYou.Core;

namespace UpToYou.Tests {

public class
DirectoryMock: IDisposable {
    public string Root { get; }

    public DirectoryMock(string root) => Root = root;
    public void Dispose() => Root.RemoveDirectory();
}

public static class 
DirectoryMockHelper{

    public const int RandomNameLength = 4;
    public static DirectoryMock Create(string? prefix = null) => 
        new DirectoryMock(Environment.CurrentDirectory.AppendPath(prefix+ UniqueId.NewUniqueId(RandomNameLength)).CreateDirectory());

    public static string 
    CreateRandomSubDirectory(this DirectoryMock updater) => 
        UniqueId.NewUniqueId(RandomNameLength).ToAbsoluteMockPath(updater).VerifyDirectoryAbsent().CreateDirectory();

    public static string
    CreateMockedSubDirectory(this string dir, DirectoryMock updater) {
        var newDir = dir.ToAbsoluteFilePath(updater.Root);
        if (newDir.DirectoryExists()) 
            throw new InvalidOperationException($"Mocked sub directory {newDir.Quoted()} already exists");
        return newDir.CreateDirectory();
    }

    public static DirectoryMock
    CreateSubDirectoryMockIfAbsent(this string dir, DirectoryMock ctx) {
        var newDir = ctx.Root.AppendPath(dir);
        return new DirectoryMock(newDir.CreateDirectoryIfAbsent());
    }

    public static string
    CreateSubDirectory(this DirectoryMock ctx, string subDir) => subDir.CreateSubDirectoryMock(ctx).Root;

    public static DirectoryMock
    CreateSubDirectoryMock(this string dir, DirectoryMock ctx) {
        var newDir = ctx.Root.AppendPath(dir);
        if (newDir.DirectoryExists())
            throw new InvalidOperationException($"Mocked sub directory {newDir.Quoted()} already exists");
        return new DirectoryMock(newDir.CreateDirectory());
    } 

    public static string
    ToAbsoluteMockPath(this string subPath, DirectoryMock ctx) => subPath.ToAbsoluteFilePath(ctx.Root);

    public static string
    AbsolutePathTo(this DirectoryMock ctx, string path) => ctx.Root.AppendPath(path);

    public static DirectoryMock
    MockThisDirectory(this string dir) {
        var ctx = Create();
        dir.GetAllDirectoryFiles().ForEach(x => x.CopyFile(x.GetPathRelativeTo(dir).ToAbsolute(ctx.Root)));
        return ctx;
    }

    public static DirectoryMock
    MockDirectory(this string dir, DirectoryMock ctx) {
        dir.GetAllDirectoryFiles().ForEach(x => x.CopyFile(x.GetPathRelativeTo(dir).ToAbsolute(ctx.Root)));
        return ctx;
    }

    public static void
    MockFiles(this IEnumerable<string> files, string baseSourceDirectory, DirectoryMock ctx) => 
        files.ForEach(x => x.CopyFile(x.GetPathRelativeTo(baseSourceDirectory).ToAbsolute(ctx.Root)));

}
}
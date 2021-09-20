using System;
using System.Collections.Generic;
using UpToYou.Core;

namespace UpToYou.Tests {

public class
DirectoryMockContext: IDisposable {
    public string Root { get; }

    public DirectoryMockContext(string root) => Root = root;
    public void Dispose() => Root.RemoveDirectory();
}

public static class 
DirectoryMock{

    public const int RandomNameLength = 4;
    public static DirectoryMockContext Create(string? prefix = null) => 
        new DirectoryMockContext(Environment.CurrentDirectory.AppendPath(prefix+ UniqueId.NewUniqueId(RandomNameLength)).CreateDirectory());

    public static string 
    CreateRandomSubDirectory(this DirectoryMockContext updater) => 
        UniqueId.NewUniqueId(RandomNameLength).ToAbsoluteMockPath(updater).VerifyDirectoryAbsent().CreateDirectory();

    public static string
    CreateMockedSubDirectory(this string dir, DirectoryMockContext updater) {
        var newDir = dir.ToAbsoluteFilePath(updater.Root);
        if (newDir.DirectoryExists()) 
            throw new InvalidOperationException($"Mocked sub directory {newDir.Quoted()} already exists");
        return newDir.CreateDirectory();
    }

    public static DirectoryMockContext
    CreateSubDirectoryMockIfAbsent(this string dir, DirectoryMockContext ctx) {
        var newDir = ctx.Root.AppendPath(dir);
        return new DirectoryMockContext(newDir.CreateDirectoryIfAbsent());
    }

    public static string
    CreateSubDirectory(this DirectoryMockContext ctx, string subDir) => subDir.CreateSubDirectoryMock(ctx).Root;

    public static DirectoryMockContext
    CreateSubDirectoryMock(this string dir, DirectoryMockContext ctx) {
        var newDir = ctx.Root.AppendPath(dir);
        if (newDir.DirectoryExists())
            throw new InvalidOperationException($"Mocked sub directory {newDir.Quoted()} already exists");
        return new DirectoryMockContext(newDir.CreateDirectory());
    } 

    public static string
    ToAbsoluteMockPath(this string subPath, DirectoryMockContext ctx) => subPath.ToAbsoluteFilePath(ctx.Root);

    public static string
    AbsolutePathTo(this DirectoryMockContext ctx, string path) => ctx.Root.AppendPath(path);

    public static DirectoryMockContext
    MockThisDirectory(this string dir) {
        var ctx = Create();
        dir.GetAllDirectoryFiles().ForEach(x => x.CopyFile(x.GetPathRelativeTo(dir).ToAbsolute(ctx.Root)));
        return ctx;
    }

    public static DirectoryMockContext
    MockDirectory(this string dir, DirectoryMockContext ctx) {
        dir.GetAllDirectoryFiles().ForEach(x => x.CopyFile(x.GetPathRelativeTo(dir).ToAbsolute(ctx.Root)));
        return ctx;
    }

    public static void
    MockFiles(this IEnumerable<string> files, string baseSourceDirectory, DirectoryMockContext ctx) => 
        files.ForEach(x => x.CopyFile(x.GetPathRelativeTo(baseSourceDirectory).ToAbsolute(ctx.Root)));

}
}
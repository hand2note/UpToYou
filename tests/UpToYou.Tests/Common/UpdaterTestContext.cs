using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using UpToYou.Backend;
using UpToYou.Core;

namespace UpToYou.Tests {
[DebuggerDisplay("{DirMock.Root}")]
public class UpdaterTestContext: IDisposable
{
    public DirectoryMock DirMock { get; }
    public IHost Host { get; }

    public UpdaterTestContext(string? prefix = null) {

        DirMock = DirectoryMockHelper.Create(prefix);
        SourcesDirMock = "sources".CreateSubDirectoryMock(DirMock);
        //sourceFiles.MockFiles(baseDir, SourcesDirMock);
        PackagesDirMock = "packages".CreateSubDirectoryMock(DirMock);
        ProjectionsDirMock = "projections".CreateSubDirectoryMock(DirMock);
        ClientDirMock = "client".CreateSubDirectoryMock(DirMock);
        Host = new LocalHost(DirMock.Root);
    }

    public DirectoryMock SourcesDirMock { get; }
    public DirectoryMock PackagesDirMock { get; }
    public DirectoryMock ProjectionsDirMock { get; }
    public DirectoryMock ClientDirMock { get; }

    public List<Version> BuiltProjections = new List<Version>();
    public bool IsPackageProjectionAlreadyBuilt(Version version) => BuiltProjections.Contains(version);

    public string SourcesDirectory(Version version) => SourcesDirMock.Root.AppendPath(version.ToString());
    public string PackageFilesDirectory => PackagesDirMock.Root;
    public string ProjectionFilesDirectory => ProjectionsDirMock.Root;
    public string ClientDirectory => ClientDirMock.Root;
    public string UpdateFilesDirectory => ClientDirectory.AppendPath("_updates");
    public string BackupDirectory => ClientDirectory.AppendPath("_backup");

    public string HostRootUrl => DirMock.Root.AppendPath("host");


    public void Dispose() {
        DirMock.Dispose();
    }
}

internal static class UpdaterTestContextEx {

    public static string 
    MockClientFiles(this IEnumerable<RelativePath> files, string baseSourceDirectory, UpdaterTestContext ctx) => 
        files.Select(x => x.ToAbsolute(baseSourceDirectory).VerifyFileExistence()).MockClientFiles(baseSourceDirectory, ctx);

    public static string 
    MockPackageFiles(this IEnumerable<RelativePath> files, string baseSourceDirectory, UpdaterTestContext ctx) => 
        files.Select(x => x.ToAbsolute(baseSourceDirectory).VerifyFileExistence()).MocPackageFiles(baseSourceDirectory, ctx);

    public static string
    MocPackageFiles(this IEnumerable<string> sourceFiles, string baseSourceDirectory, UpdaterTestContext ctx) {
        sourceFiles.MockFiles(baseSourceDirectory, ctx.PackagesDirMock);
        return ctx.PackagesDirMock.Root;
    }

    public static string
    MockClientFiles(this IEnumerable<string> sourceFiles, string baseSourceDirectory, UpdaterTestContext ctx) {
        sourceFiles.MockFiles(baseSourceDirectory, ctx.ClientDirMock);
        return ctx.ClientDirMock.Root;
    }

    public static string
    MockClientDirectory(this string directory, UpdaterTestContext ctx) =>
        directory.GetAllDirectoryFiles().MockClientFiles(directory, ctx);

    internal static (Package package, string packageFilesDirectory)
    BuildTestPackage(this (PackageSpecs specs, Version version, string sourceDirectory) @in, UpdaterTestContext ctx) =>
        @in.specs.BuildTestPackage(@in.version, @in.sourceDirectory, ctx);

    internal static (Package package, string packageFilesDirectory)
    BuildTestPackage(this PackageSpecs specs, Version version, string packageFilesSrcDir, UpdaterTestContext ctx) {
        
        specs.GetFiles(packageFilesSrcDir)
             .MockFiles(packageFilesSrcDir, $"{version}".CreateSubDirectoryMockIfAbsent(ctx.SourcesDirMock));

        string outDir = $"{version}".CreateMockedSubDirectory(ctx.PackagesDirMock);

        return specs.ToPackageBuildContext(
            sourceDirectory: ctx.SourcesDirMock.AbsolutePathTo($"{version}"), 
            outputDirectory: outDir).BuildPackage();
    }

    internal static (PackageProjection projection, string outDir)
    BuildTestProjection(this (Package package, string srcDir) @in, PackageProjectionSpecs specs, UpdaterTestContext ctx){
        
        string outDir = @in.srcDir
            .GetPathRelativeTo(ctx.PackagesDirMock.Root)
            .ToAbsolute(ctx.ProjectionsDirMock.Root)
            .CreateDirectoryIfAbsent();

        var projCtx = new ProjectionBuilder(
            sourceDirectory:@in.srcDir,
            outputDirectory:outDir,
            package:@in.package,
            projectionSpecs:specs,
            host:ctx.Host,
           logger:null);

        ctx.BuiltProjections.Add(@in.package.Header.Version);

        return projCtx.BuildProjection();
    }
    
    internal static (PackageProjection projection, string sourceDir)
    UploadToHost(this (PackageProjection projection, string sourceDir) @in, UpdaterTestContext ctx) {
        @in.UploadAllProjectionFiles(ctx.Host);
        @in.projection.UploadProjectionManifest(ctx.Host);
        return @in;
    }

    public static (Package package, string srcDir)
    UploadToHost(this (Package package, string srcDir) @in, UpdaterTestContext ctx) {
        @in.package.UploadPackageManifest(ctx.Host);
        return @in;
    }


}
}

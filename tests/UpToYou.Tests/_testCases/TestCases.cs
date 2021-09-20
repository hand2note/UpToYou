using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.Logging.Abstractions;
using UpToYou.Backend;
using UpToYou.Backend.Runner;
using UpToYou.Core;
using UpToYou.Tests.UpdateTestCases;

namespace UpToYou.Tests {

public interface IPackageMetadataTestCase {
    string PackageName { get; }
    DateTime? DateBuilt { get; }
    Dictionary<string, string>? CustomProperties { get; }
    RelativePath? VersionProvider { get; }
}

internal interface IPackageTestCase {
    string SourceDirectory { get; }
    PackageSpecs? PackageSpecs { get; }
    IPackageMetadataTestCase PackageMetadata { get; }
}

internal interface IProjectionTestCase {
    IPackageTestCase PackageTestCase { get; }
    PackageProjectionSpecs? ProjectionSpecs { get; }
    IFilesHostTestState? HostState { get; }
}

internal interface IFilesHostTestState {
    List<IProjectionTestCase> UploadedProjections { get; }
}

internal interface IUpdateTestCase {
    TestFilesSet ClientFiles { get; }
    IFilesHostTestState HostState { get; }
    Version VersionTo { get; }
}


internal class FilesHostTestCase:IFilesHostTestState {
    public FilesHostTestCase(params IProjectionTestCase[] projections) => UploadedProjections = projections.ToList();
    public FilesHostTestCase(IEnumerable<IProjectionTestCase> projections) => UploadedProjections = projections.ToList();
    public List<IProjectionTestCase> UploadedProjections { get; }
}

internal static class TestCasesHelper {

    internal static UpdaterTestContext
    BuildTestCase(this IUpdateTestCase test, UpdaterTestContext ctx) {
        test.HostState.Build(ctx);
        test.ClientFiles.AbsoluteFiles.MockClientFiles(test.ClientFiles.Root, ctx);
        return ctx;
    }

    internal static UpdaterTestContext
    Build(this IFilesHostTestState test, UpdaterTestContext ctx) {
       // using var buildCtx = new UpdaterTestContext(prefix:"temp");
        foreach (var projectionTestCase in test.UploadedProjections) {
            projectionTestCase.BuildProjection(ctx).UploadToHost(ctx);
        }

        return ctx;
    }

    internal static (PackageProjection projection, string projectionFilesDirectory)
    BuildProjection(this IProjectionTestCase test, UpdaterTestContext ctx) {
        test.HostState?.Build(ctx);
        var package = test.PackageTestCase.BuildPackage(ctx).UploadToHost(ctx);
        var projectionBuildCtx = new ProjectionBuilder(
            sourceDirectory:package.srcDir,
            outputDirectory:ctx.ProjectionFilesDirectory,
            package:package.package,
            projectionSpecs:test.GetProjectionSpecs(),
            host:ctx.Host,
            hostRootUrl:ctx.HostRootUrl, 
            logger: NullLogger.Instance);

        return projectionBuildCtx.BuildProjection();
    }

    internal static PackageProjectionSpecs
    GetProjectionSpecs(this IProjectionTestCase test) => 
        test.ProjectionSpecs ?? test.PackageTestCase.SourceDirectory.EnumerateDirectoryRelativeFiles().ToSingleFileProjectionSpecs();

    internal static (Package package, string packageFilesDirectory) 
    BuildPackage(this IPackageTestCase test, UpdaterTestContext ctx) 
        => test.GetPackageSpecs()
               .ToPackageBuildContext(
                   sourceDirectory: test.SourceDirectory, 
                   outputDirectory: ctx.PackageFilesDirectory).BuildPackage();

    internal static PackageSpecs GetPackageSpecs(this IPackageTestCase test) =>
        test.PackageSpecs 
        ?? test.SourceDirectory.EnumerateDirectoryRelativeFiles().FilesToPackageSpecs(test.PackageMetadata.VersionProvider 
            ?? throw new InvalidTestDataException("Version provider can't be null if PackageSpecs are not specified"));
}


}

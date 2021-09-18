using System;
using System.Linq;
using UpToYou.Backend;
using UpToYou.Client;
using UpToYou.Core;
using UpToYou.Tests.UpdateTestCases;

namespace UpToYou.Tests
{

internal class UpdateTC_h2n: IUpdateTestCase {
    public TestFilesSet ClientFiles { get; }
    public IFilesHostTestState HostState { get; }
    public Version VersionTo { get; }

    public UpdateTC_h2n(string versionFrom,string versionTo, IFilesHostTestState hostState) {
        HostState = hostState;
        VersionTo = versionTo.ParseVersion();
        ClientFiles = new TestFilesSet(
            root:versionFrom.ParseVersion().GetH2nRootDirectory(),
            files:versionFrom.ParseVersion().GetH2nRootDirectory().EnumerateDirectoryRelativeFiles().Select(x => x.Value));
            
    }
}

internal static class IUpdateTestCaseEx {

    public static void DownloadAndInstall(this IUpdateTestCase test, UpdaterTestContext ctx) {
        var newPackage = test.DownloadPackage(ctx);
        newPackage.DownloadAndInstall(new DownloadAndInstallContext(
            updateFilesDirectory:ctx.UpdateFilesDirectory,
            programDirectory:ctx.ClientDirectory,
            backupDirectory:ctx.BackupDirectory, 
            cancellationToken:null,
            log:new UpToYou.Backend.Runner.Logger(),
            host:ctx.Host,
            progressObserver:null));
    }

    public static string DownloadUpdateFiles(this IUpdateTestCase testCase, UpdaterTestContext ctx) {
        var newPackage = ctx.Host.DownloadAllPackages().FirstOrDefault(x => x.Version == testCase.VersionTo) 
            ?? throw new InvalidTestDataException($"Package {testCase.VersionTo} not found on the host");
        var difference = newPackage.GetDifference(ctx.ClientDirectory, new BuildDifferenceCache());
        difference.DownloadUpdateFiles(new DownloadUpdateContext(
            outputDirectory:ctx.UpdateFilesDirectory,
            progressContext:null,
            host:ctx.Host));
        return ctx.UpdateFilesDirectory;
    }

    public static Package DownloadPackage(this IUpdateTestCase test, UpdaterTestContext ctx) {
        return ctx.Host.DownloadAllPackages().FirstOrDefault(x => x.Version == test.VersionTo) 
           ?? throw new InvalidTestDataException($"Package {test.VersionTo} not found on the host");
    }
}
}

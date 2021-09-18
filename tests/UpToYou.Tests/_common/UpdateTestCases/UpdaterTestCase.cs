using System;
using System.Collections.Generic;
using System.Linq;
using UpToYou.Client;
using UpToYou.Core;

namespace UpToYou.Tests.UpdateTestCases
{

internal interface 
IUpdateTestCase : IProjectionTestCase {
    TestFilesSet ClientFiles { get; }
}

internal interface IPushUpdateTestCase: IUpdateTestCase {
   string PackageSpecsFile { get; }
   string ProjectionSpecsFile { get; }
   //string UpdateNotesFile { get; }
}

internal static class UpdateTestCaseEx {

    public static IUpdateTestCase 
    ToUpdateTestCase(this Type type) => (IUpdateTestCase) Activator.CreateInstance(type);

    public static string
    MockPackageFiles(this IUpdateTestCase test, UpdaterTestContext ctx) =>
        test.PackageFiles.RelativeFiles.MockPackageFiles(test.PackageFiles.Root, ctx);

    public static string
    MockClientFiles(this IUpdateTestCase test, UpdaterTestContext ctx) {
        test.ClientFiles.RelativeFiles.MockClientFiles(test.ClientFiles.Root, ctx);
        return ctx.ClientDirectory;
    }

    public static (Package package, PackageProjection projection, string outDir)
    Build(this IUpdateTestCase test, UpdaterTestContext ctx) {
        test.PreviousProjections?.Build(ctx);
        var root= test.ClientFiles.Root;
        test.ClientFiles.RelativeFiles.MockClientFiles(root, ctx);
        return Build((IProjectionTestCase) test, ctx);
    }

    public static void
    Build(this IEnumerable<IProjectionTestCase> tests,  UpdaterTestContext ctx) =>
        tests.Where(x => !ctx.IsPackageProjectionAlreadyBuilt(x.Version)).ForEach(x  => x.Build(ctx));

    public static (Package package, PackageProjection projection, string outDir)
    Build(this IProjectionTestCase test, UpdaterTestContext ctx) {
        test.PreviousProjections?.Build(ctx);
        var (package, packageFilesDir) = test.PackageSpecs.BuildTestPackage(test.Version, test.PackageFiles.Root, ctx).UploadToHost(ctx);
        var (projection, outDir) = (package, packageFilesDir).BuildTestProjection(test.ProjectionSpecs, ctx).UploadToHost(ctx);

        //projection.UploadProjectionManifest(ctx.Host);
        return (package, projection, outDir);
    }

    public static string
    BuildAndInstall(this IUpdateTestCase test, UpdaterTestContext ctx) {
        var (package, projection, _) = test.Build(ctx);
        var clientDirectory = ctx.ClientDirectory;
        var difference = package.GetDifference(clientDirectory, toCache:true);
        var updateFilesDir =  difference.DownloadUpdateFiles( new DownloadUpdateContext(ctx.ClientDirectory.CreateSubDirectory("_update"), null, ctx.Host));
        difference.InstallAccessibleFiles(new InstallUpdateContext(updateFilesDir, clientDirectory, ctx.ClientDirectory.CreateSubDirectory("_backup"), null));
        return clientDirectory;
    }
}

}

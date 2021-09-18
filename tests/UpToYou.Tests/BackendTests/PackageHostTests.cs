using System;
using System.Diagnostics;
using System.Threading.Tasks;
using NUnit.Framework;
using UpToYou.Backend;
using UpToYou.Core;

namespace UpToYou.Tests.BackendTests
{

[UpdaterTestFixture]
public class PackageHostTests
{
    public PackageHostContext 
    CreateContext(DirectoryMockContext dirMock) => new PackageHostContext(new LocalFilesHost(dirMock.Root), null, null);

//    [Test(Description = "Upload / DownloadAllPackages "), MyAutoData]
//    public void DownloadAllPackages(List<Package> packages)
//    {
//#if DEBUG
//        Console.WriteLine(packages.PrettyPrint());
//#endif
//        using var dirMock = DirectoryMock.Create();
//        var context = CreateContext(dirMock);
//        foreach (var package in packages)
//            package.UploadPackageManifest(context);

//        var downloaded = context.DownloadAllPackages().ToList();

//        downloaded.OrderBy(x => x.Id).ShouldBeSame(packages.OrderBy(x => x.Id));
//    }

    [Test]
    public void RemovePackage() {
        //Arrange
        using var ctx = new UpdaterTestContext();
        var testHostState =new Fhtc_last_two_versions_with_deltas();
        testHostState.Build(ctx);

        var hostFiles = ctx.HostRootUrl.EnumerateAllDirectoryFiles().ToFilesHashesMap();
        var (newProjection, _) = new Pjtc_h2n_with_deltas("3.2.6.24").BuildProjection(ctx).UploadToHost(ctx);
        var package = newProjection.PackageId.DownloadPackageById(ctx.Host);
        
        //Act
        ctx.Host.RemovePackage(package.Id);

        //Assert
        var actualHostFiles = ctx.HostRootUrl.EnumerateAllDirectoryFiles().ToFilesHashesMap();
        actualHostFiles.ShouldBeSame(hostFiles);
    }

    [Test]
    public void RemovePackage_should_not_remove_its_hosted_files_included_into_other_projections() {
        //Arrange
        using var ctx = new UpdaterTestContext();
        var projectionSpecs = $@"HostedFiles:
            - Content: 
                - {Ptc_h2n.NeverChangingFile}".ParseProjectionFromYaml();

        var testHostState = new FilesHostTestCase(new Pjtc_h2n("3.2.6.14", projectionSpecs)); testHostState.Build(ctx);

        var hostFiles = ctx.HostRootUrl.EnumerateAllDirectoryFiles().ToFilesHashesMap();
        var (newProjection, _) = new Pjtc_h2n_with_deltas("3.2.6.22").BuildProjection(ctx).UploadToHost(ctx);
        
        //Act
        ctx.Host.RemovePackage(newProjection.PackageId);

        //Assert
        var actualHostFiles = ctx.HostRootUrl.EnumerateAllDirectoryFiles().ToFilesHashesMap();
        actualHostFiles.ShouldBeSame(hostFiles);
    }



}
}

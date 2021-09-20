using System;
using System.Diagnostics;
using System.Threading.Tasks;
using NUnit.Framework;
using UpToYou.Backend;
using UpToYou.Core;

namespace UpToYou.Tests.BackendTests
{

[UpdaterTestFixture]
public class PackageHostTests {

    [Test]
    public void RemovePackage() {
        //Arrange
        using var updater = new UpdaterTestContext();
        var testHostState =new Fhtc_last_two_versions_with_deltas();
        testHostState.Build(updater);

        var hostFiles = updater.HostRootUrl.GetAllDirectoryFiles().ToFilesHashesMap();
        var (newProjection, _) = new Pjtc_h2n_with_deltas("3.2.6.24").BuildProjection(updater).UploadToHost(updater);
        var package = newProjection.PackageId.DownloadPackageById(updater.Host);
        
        //Act
        updater.Host.RemovePackage(package.Id);

        //Assert
        var actualHostFiles = updater.HostRootUrl.GetAllDirectoryFiles().ToFilesHashesMap();
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

        var hostFiles = ctx.HostRootUrl.GetAllDirectoryFiles().ToFilesHashesMap();
        var (newProjection, _) = new Pjtc_h2n_with_deltas("3.2.6.22").BuildProjection(ctx).UploadToHost(ctx);
        
        //Act
        ctx.Host.RemovePackage(newProjection.PackageId);

        //Assert
        var actualHostFiles = ctx.HostRootUrl.GetAllDirectoryFiles().ToFilesHashesMap();
        actualHostFiles.ShouldBeSame(hostFiles);
    }



}
}

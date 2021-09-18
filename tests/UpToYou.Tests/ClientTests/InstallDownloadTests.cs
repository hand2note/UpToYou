using System.Linq;
using NUnit.Framework;
using UpToYou.Backend;
using UpToYou.Client;
using UpToYou.Core;

namespace UpToYou.Tests.ClientTests {
[TestFixture]
public class InstallDownloadTests {
    
    [Test]
    public void Should_download_only_deltas() {
        //Arrange
        var test = new UpdateTC_h2n("3.2.6.14", "3.2.6.22", new Fhtc_last_two_versions_with_deltas());
        var ctx = new UpdaterTestContext();
        test.BuildTestCase(ctx);

        //Act
        var updatesFilesDir = test.DownloadUpdateFiles(ctx);

        //Assert
        foreach (var file in updatesFilesDir.EnumerateAllDirectoryFiles()) {
            if (!file.Contains(Archive.ArchiveExtension))
                Assert.IsTrue(file.EndsWith(PackageProjection.DeltaExtension), file.GetPathRelativeTo(ctx.UpdateFilesDirectory).Value);
        }
    }

    [Test]
    public void Install_with_deltas() {
        //Arrange
        var test = new UpdateTC_h2n("3.2.6.14", "3.2.6.22", new Fhtc_last_two_versions_with_deltas());
        var ctx = new UpdaterTestContext();
        test.BuildTestCase(ctx);

        var oldPackage = ctx.Host.DownloadAllPackages().FirstOrDefault(x => x.Version == "3.2.6.14".ParseVersion());
        Assert.IsFalse(oldPackage.GetDifference(ctx.ClientDirectory).IsDifferent());

        //Act
        test.DownloadAndInstall(ctx);

        //Assert
        var package = test.DownloadPackage(ctx);

        foreach (var packageFile in package.Files.Values) {
            var actualFile = packageFile.Path.ToAbsolute(ctx.ClientDirectory);
            Assert.AreEqual(packageFile.FileHash, actualFile.GetFileHash());
        }
    }
}
}

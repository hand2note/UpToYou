using NUnit.Framework;
using UpToYou.Client;

namespace UpToYou.Tests.ClientTests {

[TestFixture]
public class DifferencesTest {

    [Test]
    public void Package_built_on_the_same_files_should_not_be_different() {
    
        //Arrange
        using var updater = new UpdaterTestContext();
        var clientVersion = TestData.AnyH2nNonRootVersion;
        var clientSrc = clientVersion.GetH2nUpdateDirectory();
        TestData.H2nTestPackageSpecs.GetFiles(clientSrc).MockClientFiles(clientSrc, updater);
        var package = TestData.H2nTestPackageSpecs.BuildTestPackage(clientVersion, clientSrc, updater).package;

        //Act
        var actual = package.GetDifference(updater.ClientDirectory);

        //Assert 
        Assert.IsFalse(actual.IsDifferent());
    }

    [Test]
    public void Different_packages_should_be_different() {
        //Arrange
        using var ctx = new UpdaterTestContext();
        var clientVersion = TestData.AnyH2nNonRootVersion;
        var clientSrc = clientVersion.GetH2nUpdateDirectory();
        TestData.H2nTestPackageSpecs.GetFiles(clientSrc).MockClientFiles(clientSrc, ctx);
        var package = TestData.H2nTestPackageSpecs.BuildTestPackage(TestData.H2nRootLastVersion, TestData.H2nRootLastVersion.GetH2nUpdateDirectory(), ctx).package;

        //Act
        var actual = package.GetDifference(ctx.ClientDirectory);

        //Assert
        Assert.IsTrue(actual.IsDifferent());
    }

}
}

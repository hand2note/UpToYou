using System.IO;
using System.Linq;
using NUnit.Framework;
using UpToYou.Backend;
using UpToYou.Core;

namespace UpToYou.Tests.BackendTests {

[UpdaterTestFixture]
public class ProjectionBuildTests {

    //private static PackageProjectionSpecs 
    //GetSingleFileProjectionSpecs(PackageSpecs packageSpecs) =>
    //    new PackageProjectionSpecs(new PackageProjectionFileSpec(
    //            packageSpecs.Files.Select(x => x.Path).Union(packageSpecs.Folders.Select(x => x.Path)).ToList()).ToSingleEnumerable().ToList());

    [Test]
    public void BuildPackageProjectionDeltas() {
        //Arrange
        using var updater = new UpdaterTestContext();

        var test = new Pjtc_h2n_with_deltas("3.2.6.24",new Fhtc_last_two_versions_with_deltas());

        //Act
        var (baseProjection, _) = test.BuildProjection(updater);
        var package = baseProjection.PackageId.DownloadPackageById(updater.Host);

        //Assert
        var deltas = baseProjection.Files.Select(x => x.Content as PackageProjectionFileDeltaContent).NotNull().ToList();

        Assert.AreEqual(3, baseProjection.Files.Count);
        Assert.AreEqual(2, deltas.Count);
        foreach (var delta in deltas) {
            delta.PackageFileDeltas.ForEach(x => Assert.AreEqual(package.GetFileById(x.PackageFileId).FileHash, x.NewHash));
        }
        //AssertHostedFiles(ctx);
    }

    [Test]
    public void BuildProjectionTest() {
        //Arrange
        using var ctx= new  UpdaterTestContext();
        var test = new Pjtc_h2n("3.2.6.14");

        //Act
        var (projection, _) = test.BuildProjection( ctx);
        var package = projection.PackageId.DownloadPackageById(ctx.Host);

        //Assert
        foreach (var specItemPath in test.PackageTestCase.PackageSpecs!.GetFilesRelative(ctx.PackageFilesDirectory)) 
            Assert.IsTrue(projection.Files.Any(x => x.Content.RelevantPackageFileIds.Any(id => package.GetFileById(id).Path.Equals(specItemPath))), specItemPath.Value);

        AssertProjection(package, TestData.H2nTestProjectionSpecs, projection);

        //AssertHostedFiles(ctx);
    }

    private void AssertProjection(Package package, PackageProjectionSpecs specs, PackageProjection projection) {
        Assert.AreEqual(package.Id, projection.PackageId);
        Assert.IsNotEmpty(projection.Files);
        Assert.IsTrue(projection.Files.SelectMany(x => x.RelevantItemsIds).All(x => package.Files.Values.Select(y => y.Id).Contains(x)));
    }

    private void AssertHostedFile(PackageProjectionFile packageProjectionFile) {
        Assert.Greater(packageProjectionFile.FileSize, 0);
        Assert.IsTrue(packageProjectionFile.SubUrl.Value.Contains(packageProjectionFile.FileHash));
        Assert.IsNotEmpty(packageProjectionFile.RelevantItemsIds);
    }

    /// <summary>
    /// Asserts that every hosted file on the ctx.Host tar archive has name equals to the total hash of the files in the archive
    /// </summary>
    //public static void
    //AssertHostedFiles(UpdaterTestContext ctx) {

    //    //HostedFiles names should be a hashes of containing files
    //    var globPattern = EnumHelper.GetValues<CompressMethods>().Select(x => ".tar" + x.FileExtension()).Aggregate((s, x) => s.AppendGlobPattern(GlobPattern.FromExtensionRecursive(x)));
    //    var hostedFiles = ctx.HostRootUrl.EnumerateAllDirectoryFiles().Where(x => x.MatchGlob(globPattern)).ToList();

    //    string outCommonDir = ctx.DirMock.CreateRandomSubDirectory();
    //    foreach (var hostedFile in hostedFiles) {
    //        var decompressedBytes  = hostedFile.ReadAllFileBytes() .DecompressBasedOnFilePath(hostedFile);
    //        var outDir = outCommonDir.AppendPath(UniqueId.NewUniqueId()).CreateDirectory();
    //        new MemoryStream(decompressedBytes).ExtractArchive(outDir);
    //        var totalHash = outDir.EnumerateAllDirectoryFiles().GetTotalFilesHash();
    //        Assert.AreEqual(totalHash, hostedFile.GetFileName(), hostedFile);
    //    }
    //}

}
}

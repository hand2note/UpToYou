//using System;
//using System.Linq;
//using NUnit.Framework;
//using UpToYou.Core;
//using UpToYou.Tests.UpdateTestCases;
//using UpToYou.Client;
//using Download = UpToYou.Client.Download;

//namespace UpToYou.Tests.ClientTests {

//[TestFixture]
//public class InstallTests {

//[TestCase(typeof(UTC_simple))]
//[TestCase(typeof(UTC_with_deleted_file))]
//[TestCase(typeof(UTC_when_projection_specs_missing_package_file))]
////[TestCase(typeof(UTC_with_updater_own_dlls))]
////[TestCase(typeof(UTC_whole_h2n))]
//public void Install_update_test(Type testType) { 
//    //Arrange
//    var test= testType.ToUpdateTestCase();
//    using var ctx = new UpdaterTestContext();
//    //Act
//    var clientDirectory = test.BuildAndInstall(ctx);

//    //Assert
//    AssertTestCase(clientDirectory, test);
//}

//void AssertTestCase(string clientDirectory, IUpdateTestCase test) {
//    var actualFiles = clientDirectory.EnumerateAllDirectoryFiles().Where(x => !x.MatchGlob("**/_update/**"));
//    foreach (var actualFile in actualFiles) {
//        var relativePath = actualFile.GetPathRelativeTo(clientDirectory);
//        var expectedFile = test.PackageFiles.Root.AppendPath(relativePath);
//        if (expectedFile.FileExists() && test.PackageSpecs.Includes(relativePath))
//            Assert.AreEqual(expectedFile.GetFileHash(), actualFile.GetFileHash(), relativePath.Value);
//    }
//}

//[Test]
//public void Should_download_only_delta_files() {
//    //Arrange
//    var test= new UTC_with_deltas();
//    using var ctx = new UpdaterTestContext();

//    //Act
//    var clientDirectory = test.BuildAndInstall(ctx);

//    //Assert
//    AssertTestCase(clientDirectory, test);

//    //Downloaded files total size should be less than initial
//    var downloadedFilesSize = ctx.ClientDirectory.AppendPath("_update").EnumerateAllDirectoryFiles()
//       .Where(x => x.FileContainsExtension(PackageProjection.DeltaExtension) || 
//                   !x.FileContainsExtension(Compressing.DefaultCompressMethodFileExtension) && 
//                   !x.FileContainsExtension(Archive.ArchiveExtension))
//       .Sum(x => x.GetFileSize());

//    var initialFilesSize=  ctx.SourcesDirectory(test.Version).EnumerateAllDirectoryFiles().Sum(x => x.GetFileSize());
//    Assert.Less(downloadedFilesSize, initialFilesSize);
//}

//[Test]
//public void Install_after_failed_installation() {
//    //Arrange
//    using var ctx = new UpdaterTestContext();
//    var test = new UTC_simple();
//    var (package, projection, _) = test.Build(ctx);
//    var clientDirectory = ctx.ClientDirectory;

//    var difference = package.GetDifference(clientDirectory, toCache:true);
//    var updateFilesDir = clientDirectory.CreateSubDirectory("_update");
//    var backupDirectory = clientDirectory.CreateSubDirectory("_backup");
//    var downloadCtx = new DownloadUpdateContext(updateFilesDir, null, ctx.Host);
//    difference.DownloadUpdateFiles(downloadCtx);

//    var differentFiles = difference.FileDifferences.Where(x => x.IsDifferent).ToList();
//    //Remove one different file simulating update failed
//    difference.FileDifferences.Remove(differentFiles.First());
//    var updateCtx = new InstallUpdateContext(updateFilesDir, clientDirectory,backupDirectory, null, null);
//    difference.InstallAccessibleFiles(updateCtx);
    
//    //Act
//    difference = package.GetDifference(clientDirectory, toCache:true);
//    updateFilesDir =  difference.DownloadUpdateFiles(downloadCtx);
//    difference.InstallAccessibleFiles(updateCtx);

//    //Assert
//    AssertTestCase(clientDirectory, test);
//}

////[TestCase(typeof(UTC_simple), typeof(UTC_new_folder)) ]
//public void Rollback(Type fromTestType, Type toTestType) {
//    //Arrange
//    using var ctx = new UpdaterTestContext();
//    var fromTest= fromTestType.ToUpdateTestCase();
//    fromTest.BuildAndInstall(ctx);
//    toTestType.ToUpdateTestCase().BuildAndInstall(ctx);

//    //Act
//    var clientDir = fromTest.BuildAndInstall(ctx);

//    //Assert
//    AssertTestCase(clientDir, fromTest);
//}

//[TestCase(typeof(UTC_simple))]
//public void RunUpdaterExe(Type testType) {
//    var test = testType.ToUpdateTestCase();
//    using var ctx = new UpdaterTestContext();

//    var (package, projection, _) = test.Build(ctx);
//    var clientDirectory = ctx.ClientDirectory;

//    var difference = package.GetDifference(clientDirectory, toCache:true);
//    var updateFilesDir =  difference.DownloadUpdateFiles( new DownloadUpdateContext(clientDirectory.CreateSubDirectory("_update"),null,  ctx.Host));

//    Environment.CurrentDirectory.AppendPath("updater.exe").CopyFile(clientDirectory);
//    difference.RunUpdaterAndWait(updateFilesDir);

//    AssertTestCase(clientDirectory, test);
//}



//}
//}

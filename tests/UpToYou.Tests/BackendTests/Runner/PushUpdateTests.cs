//using System;
//using System.Collections.Generic;
//using System.IO;
//using System.Linq;
//using System.Text;
//using Microsoft.Azure.Storage;
//using Microsoft.Azure.Storage.Blob;
//using NUnit.Framework;
//using UpToYou.Backend;
//using UpToYou.Backend.Runner;
//using UpToYou.Core;
//using UpToYou.Tests.UpdateTestCases;

//namespace UpToYou.Tests.BackendTests.Runner {
//    [TestFixture]
//    public class PushUpdateTests {

//        [OneTimeSetUp]
//        public void OneTimeSetup() {
//            var storageAccount = CloudStorageAccount.Parse(AzureTestStorage.ConnectionString);
//            storageAccount.CreateCloudBlobClient().ListContainers().Where(x => !x.Uri.LocalPath.Contains("uptoyou", StringComparison.OrdinalIgnoreCase)) .ForEach(x => x.Delete());
//        }

//        [SetUp]
//        public void SetUp() => _azureContainer = UniqueId.NewUniqueId(3);
//        [SetUp]
//        public void Cleanup() => AzureTestStorage.GetHost(_azureContainer).RemoveRootContainer();

//#pragma warning disable CS8618 // Non-nullable field is uninitialized.
//        private string _azureContainer;
//#pragma warning restore CS8618 // Non-nullable field is uninitialized.

//        [TestCase(typeof(UTC_simple))]
//        public void Push_update_with_empty_specs(Type testType) {
//            //Arrange
//            var ctx = new UpdaterTestContext();
//            var test= testType.ToUpdateTestCase();
//            //test.MockPackageFiles(ctx);
//            var workingDirectory = ctx.DirMock.CreateRandomSubDirectory();
//            var options = new PushUpdateOptions(
//                sourceDirectory:test.PackageFiles.Root,
//                versionProvider:test.VersionProvider,
//                workingDirectory:workingDirectory,
//                filesHostType:nameof(LocalFilesHost),
//                localHostRootPath:ctx.HostRootUrl);

//            //Act
//            options.PushUpdate();

//            //Assert
//            AssertPushedPackage(test, ctx.ClientDirectory, GetHost(options));
//        }

//        [TestCase("3.2.6.14", "x64")]
//        [TestCase("3.2.6.22", "x64")]
//        [TestCase("3.2.6.24", "x64")]
        
//        [TestCase("3.2.6.14", "x86")]
//        [TestCase("3.2.6.22", "x86")]
//        [TestCase("3.2.6.24", "x86")]
//        public void Push_hand2note_package_on_azure(string version, string bitness) {
//            var ctx = new UpdaterTestContext();
//            var test =new Hand2NoteBaseTestCase(version, bitness);

//            //test.MockPackageFiles(ctx);
//            var options = GetAzurePushOptions(test, ctx);

//            //Act
//            options.PushUpdate();
            
//            //Assert
//            AssertPushedPackage(test, ctx.ClientDirectory, GetHost(options));
//        }

//        [TestCase("PokerStars", "1.0.2.8", "x64")]
//        [TestCase("PokerStars", "1.0.3.8", "x64")]
//        [TestCase("PokerStars", "1.0.3.9", "x64")]
//        [TestCase("PokerStars", "1.0.2.8", "x86")]
//        [TestCase("PokerStars", "1.0.3.8", "x86")]
//        [TestCase("PokerStars", "1.0.3.9", "x86")]
        
//        [TestCase("Asia", "1.0.5.5", "x64")]
//        [TestCase("Asia", "1.0.6.2", "x64")]
//        [TestCase("Asia", "1.0.6.7", "x64")]
//        [TestCase("Asia", "1.0.5.5", "x86")]
//        [TestCase("Asia", "1.0.6.2", "x86")]
//        [TestCase("Asia", "1.0.6.7", "x86")]
//        public void Push_h2n_external_package_on_azure(string package, string version, string bitness) {
//            var ctx = new UpdaterTestContext();
//            var test =new H2nExternalPushUpdateTestCase(package ,version, bitness);

//            //test.MockPackageFiles(ctx);
//            var options = GetAzurePushOptions(test, ctx);

//            //Act
//            options.PushUpdate();
            
//            //Assert
//            AssertPushedPackage(test, ctx.ClientDirectory,GetHost(options));
//        }

//        [Test]
//        public void Push_package_twice_should_create_upload_only_once() {
//            var ctx = new UpdaterTestContext();
//            var test =new H2nExternalPushUpdateTestCase("PokerStars", "1.0.2.8", "x64");

//            //test.MockPackageFiles(ctx);
//            var options = GetAzurePushOptions(test, ctx, force:true);
//            var host = new PackageHostContext(filesHost:options.GetFilesHost(), new Logger(), null);

//            //Act
//            options.PushUpdate();
//            options.PushUpdate();

//            var packages = host.DownloadAllPackages().ToList();

//            Assert.AreEqual(1, packages.Count);
//        }

//        private PackageHostContext
//        GetHost(PushUpdateOptions options) => new PackageHostContext(options.GetFilesHost(), new Logger(), null);

//        private PushUpdateOptions
//        GetAzurePushOptions(IPushUpdateTestCase test, UpdaterTestContext ctx, bool force = false) =>
//            new PushUpdateOptions(
//                sourceDirectory:test.PackageFiles.Root,
//                filesHostType:nameof(AzureBlobStorage),
//                force:force,
//                packageSpecsFile:test.PackageSpecsFile,
//                projectionSpecsFile:test.ProjectionSpecsFile,
//                workingDirectory:ctx.DirMock.CreateSubDirectory("working"),
//                azureRootContainer:_azureContainer,
//                azureConnectionString:AzureTestStorage.ConnectionString);

//        private void AssertPushedPackage(IUpdateTestCase test, string clientDirectory, PackageHostContext host) {
//            var package = host.DownloadAllPackages().First();
//            //Assert.AreEqual(test.PackageFiles.RelativeFiles.Count, package.Files.Count);
//            foreach (var expectedFile in test.PackageFiles.RelativeFiles)
//                Assert.IsTrue(test.PackageFiles.RelativeFiles.Contains(expectedFile), expectedFile.Value);

//            var updatesDirectory = clientDirectory.AppendPath("_updates");

//            package.DownloadProjection(host).HostedFiles
//                   .DownloadAll(host, updatesDirectory)
//                   .ExtractAllHostedFiles(updatesDirectory);

//            foreach (var packageFile in package.Files.Values) 
//                packageFile.Path.ToAbsolute(updatesDirectory).VerifyFileExistence();
//        }


//    }
//}

using AutoFixture.NUnit3;
using Azure;
using Azure.Storage.Blobs;
using NUnit.Framework;
using System;
using System.IO;
using System.Threading.Tasks;
using UpToYou.Backend;
using UpToYou.Core;

namespace UpToYou.Tests.BackendTests {

[TestFixture]
public class AzureBlobStorageTests {

    /*
     * NOTE! This test requires local azure blob storage devstoreaccount1
     * And you must run Azure Storage Emulator (win application searchable in the start menu)
     */
    public static string 
    TestAzureConnectionsString => 
        @"AccountName=devstoreaccount1;AccountKey=Eby8vdM02xNOcqFlqUwJPLlmEtlCDXJ1OUzFT50uSRZ6IFsuFq2UVErCz4I6tq/K1SZFPTOtr/KBHBeksoGMGw==;DefaultEndpointsProtocol=http;BlobEndpoint=http://127.0.0.1:10000/devstoreaccount1;QueueEndpoint=http://127.0.0.1:10001/devstoreaccount1;TableEndpoint=http://127.0.0.1:10002/devstoreaccount1;";

    public static AzureBlobStorageProperties
    AzureStorageTestProperties => new AzureBlobStorageProperties(
        rootContainer: "uptoyou",
        connectionString: TestAzureConnectionsString);

    public static AzureBlobStorage
    AzureBlobStorageTest => new AzureBlobStorage(AzureStorageTestProperties);

    [OneTimeSetUp]
    public static void
    SetUp() {
        CreateLocalContainer();
    }

    public static void
    CreateLocalContainer() {
        var blobService = new BlobServiceClient(TestAzureConnectionsString);
        var container = blobService.GetBlobContainerClient("uptoyou");
        if (!container.Exists())
            container.Create();
    }

    [Test]
    public static void
    UploadFileTest() {
        var data = new byte[] {1,2,3,4,5 };
        var file = "test" + Guid.NewGuid();
        AzureBlobStorageTest.UploadData(null, file.ToRelativePath(), data);
        var downloaded = AzureBlobStorageTest.DownloadData(null, file.ToRelativePath());
        CollectionAssert.AreEquivalent(data, downloaded);
    }

    [Test, AutoData]
    public static void
    DownloadFile(byte[] data) {
        var file = ("test" + Guid.NewGuid()).ToRelativePath();
        AzureBlobStorageTest.UploadData(null, file, data);
        AzureBlobStorageTest.DownloadFile(null, file, file);
        CollectionAssert.AreEquivalent(data, File.ReadAllBytes(file));
    }


    [Test, AutoData]
    public static void
    RemoveFile(byte[] data) {
        var file = ("test" + Guid.NewGuid()).ToRelativePath();
        AzureBlobStorageTest.UploadData(null, file, data);
        AzureBlobStorageTest.Remove(file);
        Assert.Throws<RequestFailedException>(() => AzureBlobStorageTest.DownloadData(null, file));
    }


}
}

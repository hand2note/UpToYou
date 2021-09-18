using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Authentication;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using ICSharpCode.SharpZipLib;
using UpToYou.Core;

namespace UpToYou.Backend {

public class
AzureBlobStorageProperties {
    public string? RootContainer { get; }
    public string? ConnectionString { get; }
    
    public AzureBlobStorageProperties(string? rootContainer, string? connectionString) {
        //RootUrl = rootUrl;
        RootContainer = rootContainer;
        ConnectionString = connectionString;
    }

    public string RootContainerOrThrow => RootContainer ?? throw new InvalidOperationException($"{nameof(RootContainer)} can't be null");
    public string ConnectionStringOrThrow => ConnectionString ?? throw new InvalidOperationException($"{nameof(ConnectionString)} can't be null");
}

public class 
AzureBlobStorage: IFilesHost {
    public Lazy<string> RootUrl { get; }

    public AzureBlobStorage(AzureBlobStorageProperties props) {
        RootUrl = new Lazy<string>(props.GetRootUrl);
        _rootContainer = new Lazy<BlobContainerClient>(props.GetRootContainer);
    } 

    private readonly Lazy<BlobContainerClient> _rootContainer;
    public BlobContainerClient 
    RootContainer => _rootContainer.Value;

    public void 
    UploadFile(ProgressContext? progress, RelativePath path, Stream inStream) {
        progress?.OnProgressStarted(DateTime.Now);
        RootContainer.GetBlobClient(path.Value).Upload(inStream, overwrite:true);
        progress?.OnIncrement(DateTime.Now, inStream.Length);
    }

    public string 
    DownloadFile(ProgressContext? progress, RelativePath path, string outFile) {
        using var outFileStream = outFile.OpenFileOverwrite();
        RootContainer.GetBlobClient(path).DownloadTo(outFileStream);
        progress?.OnIncrement(DateTime.Now, outFileStream.Length);
        return outFile;
    }

    public byte[] 
    DownloadData(ProgressContext? progress, RelativePath path) {
        var memoryStream = new MemoryStream();
        RootContainer.GetBlobClient(path).DownloadTo(memoryStream);
        return memoryStream.ToArray();
    }

    public void 
    RemoveFiles(string globPattern) {
        var blobsToDelete = _rootContainer.Value.GetBlobItems(globPattern).ToList();
        foreach (var blob in blobsToDelete)
            RootContainer.DeleteBlob(blob.Name);
    }

    public List<RelativePath> 
    GetAllFiles(string? globPattern) =>
        _rootContainer.Value.GetBlobItems(globPattern).MapToList(x => x.Name.ToRelativePath());

    public void RemoveRootContainer() => _rootContainer.Value.DeleteIfExists();

    public bool 
    FileExists(RelativePath path) => RootContainer.GetBlobClient(path.Value).Exists();
}

internal static class 
AzureBlobStorageHelper {

    public static BlobContainerClient 
    GetRootContainer(this AzureBlobStorageProperties props) {
        var blobService = new BlobServiceClient(props.ConnectionStringOrThrow);
        var container = blobService.GetBlobContainerClient(props.RootContainerOrThrow);
        if (!container.Exists()) {
            container.Create();
            container.SetAccessPolicy(accessType:PublicAccessType.Blob);
        }
        return container;
    }

    public static string 
    GetRootUrl(this AzureBlobStorageProperties props) => props.GetRootContainer().Uri.AbsoluteUri;

    internal static IEnumerable<BlobItem>
    GetBlobItems(this BlobContainerClient container, string? globPattern) {
        var blobs = container.GetBlobs(prefix:GetPrefix(globPattern)).ToList();
        return blobs.Where(x => globPattern == null || x.Name.MatchGlob(globPattern));
    }

    private static string? 
    GetPrefix(string? globPattern) {
        if (globPattern == null)
            return null;
        int slashIndex = globPattern.LastIndexOf('\\');
        var starsIndex = globPattern.LastIndexOf("**", StringComparison.Ordinal);
        if (slashIndex > 0 && !(starsIndex > 0 && starsIndex <slashIndex))
            return globPattern.Substring(0, slashIndex +1).Replace('\\', '/');
        return null;
    }

}

}
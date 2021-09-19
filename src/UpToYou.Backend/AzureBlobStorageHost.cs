using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Authentication;
using System.Threading;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using ICSharpCode.SharpZipLib;
using UpToYou.Core;

namespace UpToYou.Backend {

public class
AzureBlobStorageOptions {
    public string? RootContainer { get; }
    public string? ConnectionString { get; }
    
    public AzureBlobStorageOptions(string? rootContainer, string? connectionString) {
        RootContainer = rootContainer;
        ConnectionString = connectionString;
    }

    public string RootContainerOrThrow => RootContainer ?? throw new InvalidOperationException($"{nameof(RootContainer)} can't be null");
    public string ConnectionStringOrThrow => ConnectionString ?? throw new InvalidOperationException($"{nameof(ConnectionString)} can't be null");
}

public class 
AzureBlobStorageHost: IHost {
    public Lazy<string> RootUrl { get; }

    public AzureBlobStorageHost(AzureBlobStorageOptions options) {
        RootUrl = new Lazy<string>(options.GetRootUrl);
        _rootContainer = new Lazy<BlobContainerClient>(options.GetRootContainer);
    } 

    private readonly Lazy<BlobContainerClient> _rootContainer;
    public BlobContainerClient 
    RootContainer => _rootContainer.Value;

    public void 
    DownloadFile(RelativePath path, IProgress<long> progress, CancellationToken cancellationToken, Stream outStream) => 
        RootContainer.GetBlobClient(path).DownloadTo(outStream);
    
    public void 
    UploadFile(RelativePath path, Stream inStream) =>
        RootContainer.GetBlobClient(path.Value).Upload(inStream, overwrite:true);
    public void 
    RemoveFiles(string globPattern) {
        var blobsToDelete = _rootContainer.Value.GetBlobItems(globPattern).ToList();
        foreach (var blob in blobsToDelete)
            RootContainer.DeleteBlob(blob.Name);
    }

    public List<RelativePath> 
    GetAllFiles(string? globPattern) =>
        _rootContainer.Value.GetBlobItems(globPattern).MapToList(x => x.Name.ToRelativePath());

    public bool 
    FileExists(RelativePath path) => RootContainer.GetBlobClient(path.Value).Exists();
}

internal static class 
AzureBlobStorageHelper {

    public static BlobContainerClient 
    GetRootContainer(this AzureBlobStorageOptions props) {
        var blobService = new BlobServiceClient(props.ConnectionStringOrThrow);
        var container = blobService.GetBlobContainerClient(props.RootContainerOrThrow);
        if (!container.Exists()) {
            container.Create();
            container.SetAccessPolicy(accessType:PublicAccessType.Blob);
        }
        return container;
    }

    public static string 
    GetRootUrl(this AzureBlobStorageOptions props) => props.GetRootContainer().Uri.AbsoluteUri;

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
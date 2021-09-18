using System;
using System.Collections.Generic;
using System.Text;
using CommandLine;

namespace UpToYou.Backend.Runner {

public interface 
IFilesHostOptions {
    string? AzureConnectionString { get;  }
    string? AzureRootContainer {get; }
    string? FilesHostType { get;  }
    string? LocalHostRootPath {get; }
}

public static class FileHostOptionsEx {

    public static AzureBlobStorageProperties 
    AzureBlobStorageProperties(this IFilesHostOptions options) =>
        new AzureBlobStorageProperties(options.AzureRootContainer, options.AzureConnectionString);

    public static IFilesHost 
    GetFilesHost(this IFilesHostOptions options) {
        if (options.FilesHostType == null || options.FilesHostType == nameof(AzureBlobStorage))
            return new AzureBlobStorage(new AzureBlobStorageProperties(options.AzureRootContainer,  options.AzureConnectionString));
            
        if (options.FilesHostType == nameof(LocalFilesHost))
            return new LocalFilesHost(options.LocalHostRootPath ?? throw new InvalidOperationException($"{nameof(PushUpdateOptions.LocalHostRootPath)} should be specified."));

        throw new InvalidOperationException($"{options.FilesHostType} is an undefined type of host");
    }
}
}

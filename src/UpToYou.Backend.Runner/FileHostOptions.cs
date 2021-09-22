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

    public static AzureBlobStorageOptions 
    AzureBlobStorageProperties(this IFilesHostOptions options) =>
        new AzureBlobStorageOptions(options.AzureRootContainer, options.AzureConnectionString);

    public static IHost 
    GetFilesHost(this IFilesHostOptions options) {
        if (options.FilesHostType == null || options.FilesHostType == nameof(AzureBlobStorage))
            return new AzureBlobStorage(new AzureBlobStorageOptions(options.AzureRootContainer,  options.AzureConnectionString));
            
        if (options.FilesHostType == nameof(LocalHost))
            return new LocalHost(options.LocalHostRootPath ?? throw new InvalidOperationException($"{nameof(PushUpdateOptions.LocalHostRootPath)} should be specified."));

        throw new InvalidOperationException($"{options.FilesHostType} is an undefined type of host");
    }
}
}

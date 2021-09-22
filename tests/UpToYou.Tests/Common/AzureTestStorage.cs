using UpToYou.Backend;

namespace UpToYou.Tests {

public class AzureTestStorage {

    public const string RootUrl = "http://127.0.0.1:10000/devstoreaccount1";
    public const string AccountName = "devstoreaccount1";
    public const string AccountKey = "Eby8vdM02xNOcqFlqUwJPLlmEtlCDXJ1OUzFT50uSRZ6IFsuFq2UVErCz4I6tq/K1SZFPTOtr/KBHBeksoGMGw==";

    public const string RootContainer = "uptoyou";

    public static string DownloadRootUrl => $"{RootUrl}\\{RootContainer}";
    public const string ConnectionString = 
        @"UseDevelopmentStorage=true";

    public static AzureBlobStorageOptions GetProps(string rootContainer) => new AzureBlobStorageOptions(
        rootContainer:rootContainer,
        connectionString:ConnectionString);

    public static AzureBlobStorage GetHost(string rootContainer) => new AzureBlobStorage(GetProps(rootContainer));

}
}

using System;

namespace UpToYou.Backend.Runner
{
public class AzureEnvironment {
    public AzureEnvironment( string? azureRootContainer, string? azureConnectionString) {
        //AzureRootUrl = azureRootUrl;
        AzureRootContainer = azureRootContainer;
        AzureConnectionString = azureConnectionString;
    }
   // public string? AzureRootUrl { get; }
    public string? AzureRootContainer { get; }
    public string? AzureConnectionString { get; }

    public AzureBlobStorageOptions ToAzureBlobStorageProperties() =>
        new AzureBlobStorageOptions(AzureRootContainer, AzureConnectionString);
}

public static class EnvironmentModule {

    public static void Save(this AzureEnvironment azure) {
       // Environment.SetEnvironmentVariable(nameof(AzureEnvironment.AzureRootUrl).FullVariableName(), azure.AzureRootUrl);
        Environment.SetEnvironmentVariable(nameof(AzureEnvironment.AzureRootContainer).FullVariableName(), azure.AzureRootContainer);
        Environment.SetEnvironmentVariable(nameof(AzureEnvironment.AzureConnectionString).FullVariableName(), azure.AzureConnectionString);
    }

    private static string FullVariableName(this string name) => "UpToYou." + name;

    public static AzureEnvironment GetAzureEnvironment() {
        var container =Environment.GetEnvironmentVariable(nameof(AzureEnvironment.AzureRootContainer).FullVariableName() , EnvironmentVariableTarget.User);
        var azureConnectionString  =  Environment.GetEnvironmentVariable(nameof(AzureEnvironment.AzureConnectionString).FullVariableName(), EnvironmentVariableTarget.User);
        Console.WriteLine($"AzureRootContainer={container}");
        Console.WriteLine($"AzureConnectionString={azureConnectionString}");
        return new AzureEnvironment(container, azureConnectionString);
    }
}
}

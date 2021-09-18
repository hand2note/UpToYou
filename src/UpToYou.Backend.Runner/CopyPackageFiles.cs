using System.IO;
using CommandLine;
using UpToYou.Core;

namespace UpToYou.Backend.Runner {

[Verb("CopyPackageFiles")]
public class CopyPackageFilesOptions {

    public CopyPackageFilesOptions() { }

    public CopyPackageFilesOptions(string sourceDirectory, string outputDirectory, string packageSpecsFile) {
        SourceDirectory = sourceDirectory;
        OutputDirectory = outputDirectory;
        PackageSpecsFile = packageSpecsFile;
    }

    [Option(Required = true)]
    public string SourceDirectory { get; set; }
    [Option(Required = true)]
    public string OutputDirectory{ get; set; }
    
    [Option]
    public string PackageSpecsFile { get; set; }

    [Option] 
    public string PackageSpecsDirectory { get; set; }
}

public static class CopyPackageFilesModule {
    public static void CopyPackageFiles(this CopyPackageFilesOptions options) {

        void CopyPackage(string packageSpecsFile) 
            => packageSpecsFile.ReadAllFileText().ParsePackageSpecsFromYaml().CopyFiles(options.SourceDirectory, options.OutputDirectory);

        if (!string.IsNullOrWhiteSpace( options.PackageSpecsFile))
            CopyPackage(options.PackageSpecsFile);

        if (!string.IsNullOrWhiteSpace(options.PackageSpecsDirectory))
            Directory.GetFiles(options.PackageSpecsDirectory, "*.package.specs", SearchOption.AllDirectories).ForEach(CopyPackage);
    }
}

}
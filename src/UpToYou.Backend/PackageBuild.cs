using System;
using System.Collections.Generic;
using System.Linq;
using UpToYou.Core;

namespace UpToYou.Backend {

internal class
PackageBuildContext {
    public string SourceDirectory { get; }
    public string OutputDirectory{ get; }
    public PackageSpecs Specs { get; }
    public Dictionary<string, string>? CustomProperties { get; }

    public PackageBuildContext(string sourceDirectory, string outputDirectory, PackageSpecs specs,   Dictionary<string, string>? customProperties= null) =>
        (SourceDirectory, OutputDirectory, Specs, CustomProperties) = (sourceDirectory, outputDirectory, specs, customProperties);
}

internal static class PackageBuild {

    public static PackageBuildContext 
    ToPackageBuildContext(this PackageSpecs packageSpecs, string srcDir, string outDir) =>
        new PackageBuildContext(srcDir, outDir, packageSpecs);

    public static (Package package, string packageFilesDir)
    BuildPackage(this PackageBuildContext ctx){
        var files = ctx.Specs.GetFilesRelative(ctx.SourceDirectory)
            .Select(x => x.ToPackageFile(ctx))
            .ToDictionary(x => x.Path, x => x);
        
        var versionProvider = files.TryGetValue(ctx.Specs.VersionProvider, out var res) ? res : throw new InvalidOperationException($"Version provider package file ({ctx.Specs.VersionProvider}) not found");
        return (new Package(
            metadata:new PackageMetadata(
                id:UniqueId.NewUniqueId(),
                name:ctx.Specs.PackageName ?? string.Empty,
                version: versionProvider.FileVersion ?? throw new InvalidPackageDataException("Version provider file should have a file version"),
                dateBuilt: DateTime.Now,
                versionProviderFile: versionProvider,
                customProperties: ctx.Specs.CustomProperties.AddCustomProperties(ctx.CustomProperties, @override:true)),
            files: files), ctx.OutputDirectory);;
        }

    //private static IEnumerable<PackageFile>
    //BuildFolder(this PackageFolderSpec folderSpec, PackageBuildContext ctx) =>
    //    folderSpec.Path.ToAbsolute(ctx.SourceDirectory).EnumerateAllDirectoryFiles()
    //        .Select(x => new PackageFileSpec(x.GetPathRelativeTo(ctx.SourceDirectory), false, folderSpec.Actions).ToPackageFile(ctx));

    private static PackageFile
    ToPackageFile(this RelativePath fileSpec, PackageBuildContext context) {
        var outFile = 
            fileSpec.ToAbsolute(context.SourceDirectory)
            .CopyFile(context.OutputDirectory.AppendPath(fileSpec));

        return new PackageFile(
            id: UniqueId.NewUniqueId(),
            path:fileSpec,
            fileSize: outFile.GetFileSize(),
            fileHash:outFile.GetFileHash(),
            fileVersion:outFile.GetFileVersion()  );
    }

}
}

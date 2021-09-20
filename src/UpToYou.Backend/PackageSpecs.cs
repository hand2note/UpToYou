using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using UpToYou.Core;

namespace UpToYou.Backend {
    
internal class 
PackageSpecs: IHasCustomProperties {
    public string? PackageName { get; }
    public ImmutableList<RelativeGlob> Files { get; }
    public ImmutableList<RelativeGlob> ExcludedFiles { get; }
    public RelativePath VersionProvider { get; }
    public ImmutableDictionary<string, string> CustomProperties { get; }
    public PackageSpecs(string? packageName, ImmutableList<RelativeGlob> files, ImmutableList<RelativeGlob> excludedFiles, RelativePath versionProvider, ImmutableDictionary<string, string> customProperties) {
        PackageName = packageName;
        Files = files;
        ExcludedFiles = excludedFiles;
        VersionProvider = versionProvider;
        CustomProperties = customProperties;
    }

    public IEnumerable<string> 
    GetFiles(string directory) => 
        directory.GetMatchingFilesRelative(Files, ExcludedFiles).Select(x => x.ToAbsolute(directory)).Distinct();

    public IEnumerable<RelativePath> 
    GetFilesRelative(string directory) {
        var result = directory.GetMatchingFilesRelative(Files, ExcludedFiles).Distinct().ToList();
        //At least one file must match a glob. Otherwise, we risk to build a package with a missing file.
        foreach(var fileGlob in Files)
            if (!result.Any(x => x.Matches(fileGlob)))
                throw new InvalidOperationException($"No file matches package ({PackageName}) glob {fileGlob.Value.Quoted()}");
        return result;
    } 

    public string 
    CopyFiles(string sourceDirectory, string outputDirectory) {
        foreach (var file in GetFilesRelative(sourceDirectory)) 
            file.Copy(sourceDirectory, outputDirectory);
        return outputDirectory;
    }

    public bool Includes(RelativePath file) => ExcludedFiles.All(x => !file.Matches(x)) && Files.Any(x => file.Matches(x));
}

internal class
PackageProjectionSpecs {
    public List<PackageProjectionFileSpec> HostedFiles { get; }
    public PackageProjectionSpecs(List<PackageProjectionFileSpec> hostedFiles) => HostedFiles = hostedFiles;
}

internal class 
PackageProjectionFileSpec {
    public List<RelativeGlob> Content { get; }
    public int MaxHostDeltas{ get; }
    public PackageProjectionFileSpec(List<RelativeGlob> content, int maxHostDeltas = 0) => 
        (Content, MaxHostDeltas) = (content, maxHostDeltas);

    public bool HostDeltas => MaxHostDeltas >0;
}

}

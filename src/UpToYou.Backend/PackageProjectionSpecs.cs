using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.FSharp.Collections;
using Microsoft.FSharp.Core;
using UpToYou.Backend.FSharp;
using UpToYou.Core;

namespace UpToYou.Backend {

internal class 
PackageProjectionFileSpec {
    public List<RelativeGlob> Content { get; }
    public int MaxHostDeltas{ get; }
    public PackageProjectionFileSpec(List<RelativeGlob> content, int maxHostDeltas = 0) => 
        (Content, MaxHostDeltas) = (content, maxHostDeltas);

    public bool HostDeltas => MaxHostDeltas >0;
}

internal class PackageProjectionSpecs {
    public List<PackageProjectionFileSpec> HostedFiles { get; }
    public PackageProjectionSpecs(List<PackageProjectionFileSpec> hostedFiles) => HostedFiles = hostedFiles;
}

internal static class
PackageProjectionSpecsEx {

    public static PackageProjectionSpecs 
    ToProjectionSpecs(this PackageProjectionFileSpec fileSpec) => new PackageProjectionSpecs(new List<PackageProjectionFileSpec>(){fileSpec});

    public static PackageProjectionFileSpec 
    ToSingleProjectionFileSpec(this IEnumerable<RelativePath> paths) => new PackageProjectionFileSpec(paths.ToRelativeGlobs().ToList());

    public static PackageProjectionSpecs
    ToSingleFileProjectionSpecs(this IEnumerable<RelativePath> files) => files.ToSingleProjectionFileSpec().ToProjectionSpecs();

    internal static PackageProjectionSpecs
    ParseProjectionFromYaml(this string yaml) {
        if (string.IsNullOrWhiteSpace(yaml)) throw new ArgumentException("Value cannot be null or whitespace.", nameof(yaml));

        var yObject = YamlParser.parseYObjectResult(yaml);
        return new PackageProjectionSpecs(
            YamlMapper.findListOfYObjects(nameof(PackageProjectionSpecs.HostedFiles), yObject).Select(MapYamlToFile).ToList())
            .Verify();
    }

    public static PackageProjectionSpecs
    Verify(this PackageProjectionSpecs specs) {
        if (specs.HostedFiles.Count ==0)
            throw new InvalidDataException("HostedFiles.Count can't be zero");
        foreach (var hostedFile in specs.HostedFiles) {
            if (hostedFile.Content.Distinct().Count() != hostedFile.Content.Count)
                throw new InvalidDataException("Duplicate entries in HostedFile.Content");
        }
        return specs;
    }

    private static PackageProjectionFileSpec
    MapYamlToFile(FSharpMap<FSharpOption<string>, Yaml> yObject) =>
        new PackageProjectionFileSpec(
            content: YamlMapper.findListOfYString(nameof(PackageProjectionFileSpec.Content), yObject).Select(x => new RelativeGlob(x.Replace("/", "\\"))).ToList(),
            maxHostDeltas: (int?)YamlMapper.tryFindYNumber(nameof(PackageProjectionFileSpec.MaxHostDeltas), yObject)?.Value ?? 0); 


}
}



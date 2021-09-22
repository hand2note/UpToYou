using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.FSharp.Collections;
using Microsoft.FSharp.Core;
using UpToYou.Backend.FSharp;
using UpToYou.Core;

namespace UpToYou.Backend {
internal static class PackageSpecsHelper {
    
    public static PackageSpecs
    FilesToPackageSpecs(this IEnumerable<RelativePath> files, RelativePath versionProvider) => 
        new PackageSpecs(
            packageName:null,
            files:files.MapToImmutableList(x => new RelativeGlob(x.Value)),
            excludedFiles: ImmutableList<RelativeGlob>.Empty, 
            versionProvider:versionProvider,
            customProperties: ImmutableDictionary<string, string>.Empty);

    public static PackageSpecs
    ParsePackageSpecsFromFile(this string file) => file.FileHasExtension(".json") ? file.ReadAllFileText().ParseJson<PackageSpecs>() : file.ReadAllFileText().Trim().ParsePackageSpecsFromYaml();
    
    public static PackageSpecs
    ParsePackageSpecsFromYaml(this string yaml) {

        if (string.IsNullOrWhiteSpace(yaml)) throw new ArgumentException("Value cannot be null or whitespace.", nameof(yaml));

        var yObject = YamlParser.parseYObjectResult(yaml);
        return new PackageSpecs(
            packageName: YamlMapper.tryFindYString(nameof(PackageSpecs.PackageName), yObject)?.Value,
            files: YamlMapper.tryFindYList(nameof(PackageSpecs.Files), yObject)?.Value.MapYListToRelativeGlobs().ToImmutableList() ?? ImmutableList<RelativeGlob>.Empty,
            excludedFiles:YamlMapper.tryFindYList(nameof(PackageSpecs.ExcludedFiles), yObject)?.Value.MapYListToRelativeGlobs().ToImmutableList() ?? ImmutableList<RelativeGlob>.Empty,
            versionProvider:new RelativePath( YamlMapper.findYString(nameof(PackageSpecs.VersionProvider), yObject).Replace("/", "\\")),
            customProperties: YamlMapper.tryFindYList(nameof(PackageSpecs.CustomProperties), yObject)?.Value.MapToCustomProperties() ?? ImmutableDictionary<string, string>.Empty) ;
    }

    internal static ImmutableDictionary<string, string>
    MapToCustomProperties(this  FSharpList<Yaml> yList) {
        var result = new Dictionary<string, string>();
        foreach (var yItem in yList)
            if (yItem is Yaml.YObject yObject) {
                var kv = yObject.Item.ToList()[0];
                result.Add(kv.Key.Value, (kv.Value as Yaml.YString)?.Item
                      ?? (kv.Value as Yaml.YBool)?.Item.ToString()
                      ?? (kv.Value as Yaml.YNumber)?.Item.ToString(CultureInfo.InvariantCulture)
                      ?? "Expecting a string value of custom property");
            }       

        return result.ToImmutableDictionary();
    }

    private static IEnumerable<RelativeGlob> 
    MapYListToRelativeGlobs(this FSharpList<Yaml> yList) {
        foreach (var yItem in yList)
            if (yItem is Yaml.YString yString)
                yield return new RelativeGlob(yString.Item);
            else
                throw new InvalidOperationException("Expecting string yaml value for a Files item");
    }

    public static PackageProjectionSpecs 
    ToProjectionSpecs(this PackageProjectionFileSpec fileSpec) => new PackageProjectionSpecs(new List<PackageProjectionFileSpec>() { fileSpec });

    public static PackageProjectionFileSpec 
    ToSingleProjectionFileSpec(this IEnumerable<RelativePath> paths) => new PackageProjectionFileSpec(paths.ToRelativeGlobs().ToList());

    public static PackageProjectionSpecs
    ToSingleFileProjectionSpecs(this IEnumerable<RelativePath> files) => files.ToSingleProjectionFileSpec().ToProjectionSpecs();

    public static PackageProjectionSpecs
    ParseProjectionFromFile(this string file) => 
        file.FileHasExtension(".json") ? file.ReadAllFileText().ParseJson<PackageProjectionSpecs>() : file.ReadAllFileText().Trim().ParseProjectionFromYaml();
    
    internal static PackageProjectionSpecs
    ParseProjectionFromYaml(this string yaml) {
        if (string.IsNullOrWhiteSpace(yaml)) throw new ArgumentException("Value cannot be null or whitespace.", nameof(yaml));

        var yObject = YamlParser.parseYObjectResult(yaml);
        return new PackageProjectionSpecs(
                YamlMapper.findListOfYObjects(nameof(PackageProjectionSpecs.HostedFiles), yObject).Select(MapYamlToFile).ToList<PackageProjectionFileSpec>())
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

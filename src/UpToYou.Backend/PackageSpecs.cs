using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using Microsoft.FSharp.Collections;
using UpToYou.Backend.FSharp;
using UpToYou.Core;

namespace UpToYou.Backend {
internal class PackageSpecs: IHasCustomProperties {
    public string? PackageName { get; }
    public List<RelativeGlob> Files { get; }
    public List<RelativeGlob> ExcludedFiles { get; }
    public RelativePath VersionProvider { get; }
    public Dictionary<string, string>? CustomProperties { get; }

    public PackageSpecs(string? packageName, List<RelativeGlob>? files, List<RelativeGlob>? excludedFiles, RelativePath versionProvider, Dictionary<string, string>? customProperties = null) {
        PackageName = packageName;
        Files = files??new List<RelativeGlob>();
        ExcludedFiles = excludedFiles ?? new List<RelativeGlob>();
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

internal static class PackageSpecsEx {
    
    public static PackageSpecs
    FilesToPackageSpecs(this IEnumerable<RelativePath> files, RelativePath versionProvider) => 
        new PackageSpecs(
            packageName:null,
            files:files.MapToList(x => new RelativeGlob(x.Value)),
            excludedFiles:null,
            versionProvider:versionProvider);

    public static PackageSpecs
    ParsePackageSpecsFromYaml(this string yaml) {

        if (string.IsNullOrWhiteSpace(yaml)) throw new ArgumentException("Value cannot be null or whitespace.", nameof(yaml));

        var yObject = YamlParser.parseYObjectResult(yaml);
        return new PackageSpecs(
                packageName: YamlMapper.tryFindYString(nameof(PackageSpecs.PackageName), yObject)?.Value,
                files: YamlMapper.tryFindYList(nameof(PackageSpecs.Files), yObject)?.Value.MapYListToRelativeGlobs().ToList() ?? new List<RelativeGlob>(),
                excludedFiles:YamlMapper.tryFindYList(nameof(PackageSpecs.ExcludedFiles), yObject)?.Value.MapYListToRelativeGlobs().ToList() ?? new List<RelativeGlob>(),
                versionProvider:new RelativePath( YamlMapper.findYString(nameof(PackageSpecs.VersionProvider), yObject).Replace("/", "\\")),
                customProperties: YamlMapper.tryFindYList(nameof(PackageSpecs.CustomProperties), yObject)?.Value.MapToCustomProperties()) ;
    }

    internal static Dictionary<string, string>
    MapToCustomProperties(this  FSharpList<Yaml> yList) {
        var res = new Dictionary<string, string>();
        foreach (var yItem in yList) {
            if (yItem is Yaml.YObject yObject) {
                var kv = yObject.Item.ToList()[0];
                res.Add(kv.Key.Value, (kv.Value as Yaml.YString)?.Item
                                      ?? (kv.Value as Yaml.YBool)?.Item.ToString()
                                      ?? (kv.Value as Yaml.YNumber)?.Item.ToString(CultureInfo.InvariantCulture)
                                      ?? "Expecting a string value of custom property");
            }
        }
        return res;
    }

    private static IEnumerable<RelativeGlob>
    MapYListToRelativeGlobs(this FSharpList<Yaml> yList)
    {
        foreach (var yItem in yList)
            if (yItem is Yaml.YString yString)
                yield return new RelativeGlob(yString.Item);
            else
                throw new ParsingException("Expecting string yaml value for a Files item");
    }
    
}
}

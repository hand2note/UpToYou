using System;
using System.Collections.Generic;
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
internal static class SpecsHelper {
    
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
        foreach (var yItem in yList)
            if (yItem is Yaml.YObject yObject) {
                var kv = yObject.Item.ToList()[0];
                res.Add(kv.Key.Value, (kv.Value as Yaml.YString)?.Item
                                      ?? (kv.Value as Yaml.YBool)?.Item.ToString()
                                      ?? (kv.Value as Yaml.YNumber)?.Item.ToString(CultureInfo.InvariantCulture)
                                      ?? "Expecting a string value of custom property");
            }

        return res;
    }

    private static IEnumerable<RelativeGlob> 
    MapYListToRelativeGlobs(this FSharpList<Yaml> yList) {
        foreach (var yItem in yList)
            if (yItem is Yaml.YString yString)
                yield return new RelativeGlob(yString.Item);
            else
                throw new InvalidOperationException("Expecting string yaml value for a Files item");
    }

    public static Update 
    ToUpdate(this UpdateSpec? updateSpec, PackageMetadata packageMetadata) =>
        new Update(
            packageMetadata: packageMetadata,
            updatePolicy: new UpdatePolicy(
                isAuto:updateSpec?.IsAuto??false,
                isRequired:updateSpec?.IsRequired??false,
                updateRing:new UpdateRing(updateSpec?.UpdateRing??0),
                isLazy:updateSpec?.IsLazy??false,
                autoUpdateFrom:updateSpec?.AutoUpdateFrom ),
            customProperties: updateSpec?.CustomProperties);

    public static PackageUpdatesSpecs
    ParseUpdatesSpecsFromYaml(this string yaml) {
        var yObject = YamlParser.parseYObjectResult(yaml.Trim());
        return new PackageUpdatesSpecs(
           updateSpecs:YamlMapper.tryFindListOfYObjects(nameof(PackageUpdatesSpecs.UpdatesSpecs), yObject)?.Value.Select(MapYamlToUpdateSpec).ToList(),
           defaultSpec:(YamlMapper.tryFindYObject(nameof(PackageUpdatesSpecs.DefaultSpec), yObject)?? 
                YamlMapper.tryFindYObject("Default", yObject))?.Value.MapYamlToDefaultUpdateSpec());
    }

    private static UpdateSpec 
    MapYamlToDefaultUpdateSpec(this FSharpMap<FSharpOption<string>, Yaml> yObject) => 
        new UpdateSpec(
            version:new Version(),
            isAuto:YamlMapper.tryFindYBool(nameof(UpdateSpec.IsAuto), yObject)?.Value,
            isRequired:YamlMapper.tryFindYBool(nameof(UpdateSpec.IsRequired), yObject)?.Value,
            isBeta:null,
            isLazy:YamlMapper.tryFindYBool(nameof(UpdateSpec.IsLazy), yObject)?.Value,
            updateRing:0,
            autoUpdateFrom:null,
            customProperties:YamlMapper.tryFindYList(nameof(UpdateSpec.CustomProperties), yObject)?.Value.MapToCustomProperties());

    private static UpdateSpec 
    MapYamlToUpdateSpec(this FSharpMap<FSharpOption<string>, Yaml> yObject) => 
        new UpdateSpec(
            version:YamlMapper.findOptionalYString(nameof(UpdateSpec.Version), yObject).ParseVersion(),
            isAuto:YamlMapper.tryFindYBool(nameof(UpdateSpec.IsAuto), yObject)?.Value,
            isRequired:YamlMapper.tryFindYBool(nameof(UpdateSpec.IsRequired), yObject)?.Value,
            updateRing:((int?)YamlMapper.tryFindYNumber(nameof(UpdateSpec.UpdateRing), yObject)?.Value) ?? 0,
            isBeta:YamlMapper.tryFindYBool(nameof(UpdateSpec.IsBeta), yObject)?.Value,
            isLazy:YamlMapper.tryFindYBool(nameof(UpdateSpec.IsLazy), yObject)?.Value,
            autoUpdateFrom:YamlMapper.tryFindListOfYString(nameof(UpdateSpec.AutoUpdateFrom), yObject)?.Value.Select(x => x.ParseVersion()).ToList(),
            customProperties:YamlMapper.tryFindYList(nameof(UpdateSpec.CustomProperties), yObject)?.Value.MapToCustomProperties());

    public static PackageProjectionSpecs 
        ToProjectionSpecs(this PackageProjectionFileSpec fileSpec) => new PackageProjectionSpecs(new List<PackageProjectionFileSpec>() { fileSpec });

    public static PackageProjectionFileSpec 
        ToSingleProjectionFileSpec(this IEnumerable<RelativePath> paths) => new PackageProjectionFileSpec(paths.ToRelativeGlobs().ToList());

    public static PackageProjectionSpecs
        ToSingleFileProjectionSpecs(this IEnumerable<RelativePath> files) => files.ToSingleProjectionFileSpec().ToProjectionSpecs();

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

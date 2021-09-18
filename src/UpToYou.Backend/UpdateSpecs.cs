using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using Microsoft.FSharp.Collections;
using Microsoft.FSharp.Control;
using Microsoft.FSharp.Core;
using UpToYou.Backend.FSharp;
using UpToYou.Core;

namespace UpToYou.Backend {

public class 
UpdateSpec:IHasCustomProperties, IHasPackageDependencies {
    public Version Version { get; }
    public bool? IsAuto { get; }
    public bool? IsRequired { get; }
    public bool? IsBeta { get; }
    public int UpdateRing { get; }
    public bool? IsLazy { get; }
    public List<Version> AutoUpdateFrom { get; }
    public List<PackageDependency> Dependencies { get; }
    public Dictionary<string, string>? CustomProperties { get; }

    public UpdateSpec(Version version, bool? isAuto, bool? isRequired, int updateRing, bool? isBeta, bool? isLazy, List<Version>? autoUpdateFrom, List<PackageDependency>? dependencies, Dictionary<string, string>? customProperties) {
        Version = version;
        IsAuto = isAuto;
        IsRequired = isRequired;
        UpdateRing = updateRing;
        IsBeta = isBeta;
        IsLazy = isLazy;
        AutoUpdateFrom = autoUpdateFrom ?? new List<Version>();
        Dependencies = dependencies ?? new List<PackageDependency>();
        CustomProperties = customProperties;
    }

    public bool HasDependencies => Dependencies.Count > 0;
}

public class 
PackageUpdatesSpecs {
    public List<UpdateSpec> UpdatesSpecs { get; }
    public UpdateSpec? DefaultSpec { get; }

    public bool IsEmpty => DefaultSpec == null && UpdatesSpecs.Count == 0;

    public PackageUpdatesSpecs(List<UpdateSpec>? updateSpecs, UpdateSpec? defaultSpec = null) {
        UpdatesSpecs = updateSpecs ?? new List<UpdateSpec>();
        DefaultSpec = defaultSpec;
    }

    public UpdateSpec? FindUpdateSpec(Version version) {
        var spec = UpdatesSpecs.FirstOrDefault(x => x.Version == version);
        if (spec == null && DefaultSpec == null)
            return null;

        if (DefaultSpec != null && DefaultSpec.Dependencies.Count != 0)
            throw new InvalidOperationException($"Default {nameof(UpdateSpec.Dependencies)} are not supported");

        if (DefaultSpec != null && DefaultSpec.AutoUpdateFrom.Count != 0)
            throw new InvalidOperationException($"Default {nameof(UpdateSpec.AutoUpdateFrom)} are not supported");

        return new UpdateSpec(
            version:version,
            isAuto: spec?.IsAuto ?? DefaultSpec?.IsAuto,
            isRequired: spec?.IsRequired ?? DefaultSpec?.IsRequired,
            updateRing:spec?.UpdateRing??0,
            isBeta:spec?.IsBeta ?? DefaultSpec?.IsBeta,
            isLazy:spec?.IsLazy ?? DefaultSpec?.IsLazy,
            autoUpdateFrom:spec?.AutoUpdateFrom,
            dependencies:spec?.Dependencies,
            customProperties:this.DefaultSpec?.CustomProperties == null && spec?.CustomProperties == null 
                ? null: 
                this.DefaultSpec?.CustomProperties.AddCustomProperties(spec?.CustomProperties, true));
    }
}


public static class PackageUpdateSpecsEx {

    public static Update ToUpdate(this UpdateSpec? updateSpec, PackageMetadata packageMetadata) =>
        new Update(
            packageMetadata: packageMetadata,
            updatePolicy: new UpdatePolicy(
                isAuto:updateSpec?.IsAuto??false,
                isRequired:updateSpec?.IsRequired??false,
                updateRing:new UpdateRing(updateSpec?.UpdateRing??0),
                isBeta:updateSpec?.IsBeta??false,
                isLazy:updateSpec?.IsLazy??false,
                autoUpdateFrom:updateSpec?.AutoUpdateFrom ),
            dependencies:updateSpec?.Dependencies,
            customProperties: updateSpec?.CustomProperties);

    public static PackageUpdatesSpecs
    ParseUpdatesSpecsFromYaml(this string yaml) {
        var yObject = YamlParser.parseYObjectResult(yaml.Trim());
        return new PackageUpdatesSpecs(
           updateSpecs:YamlMapper.tryFindListOfYObjects(nameof(PackageUpdatesSpecs.UpdatesSpecs), yObject)?.Value.Select(MapYamlToUpdateSpec).ToList(),
           defaultSpec:(YamlMapper.tryFindYObject(nameof(PackageUpdatesSpecs.DefaultSpec), yObject)?? 
                YamlMapper.tryFindYObject("Default", yObject))?.Value.MapYamlToDefaultUpdateSpec());
    }

    private static PackageDependency
    MapYamlToPackageDependency(FSharpMap<FSharpOption<string>, Yaml> yObject) =>
        new PackageDependency(
            packageName:YamlMapper.findOptionalYString(nameof(PackageDependency.PackageName), yObject),
            minVersion:YamlMapper.findYString(nameof(PackageDependency.MinVersion), yObject).ParseVersion());

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
            dependencies:YamlMapper.tryFindListOfYObjects(nameof(UpdateSpec.Dependencies), yObject)?.Value.Select(MapYamlToPackageDependency).ToList(),
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
            dependencies:YamlMapper.tryFindListOfYObjects(nameof(UpdateSpec.Dependencies), yObject)?.Value.Select(MapYamlToPackageDependency).ToList(),
            customProperties:YamlMapper.tryFindYList(nameof(UpdateSpec.CustomProperties), yObject)?.Value.MapToCustomProperties());
            
        

}



}

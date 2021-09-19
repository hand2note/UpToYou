using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UpToYou.Core;

namespace UpToYou.Backend {
    
internal class 
PackageSpecs: IHasCustomProperties {
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

public class 
UpdateSpec:IHasCustomProperties {
    public Version Version { get; }
    public bool? IsAuto { get; }
    public bool? IsRequired { get; }
    public bool? IsBeta { get; }
    public int UpdateRing { get; }
    public bool? IsLazy { get; }
    public List<Version> AutoUpdateFrom { get; }
    public Dictionary<string, string>? CustomProperties { get; }
    public UpdateSpec(Version version, bool? isAuto, bool? isRequired, int updateRing, bool? isBeta, bool? isLazy, List<Version>? autoUpdateFrom, Dictionary<string, string>? customProperties) {
        Version = version;
        IsAuto = isAuto;
        IsRequired = isRequired;
        UpdateRing = updateRing;
        IsBeta = isBeta;
        IsLazy = isLazy;
        AutoUpdateFrom = autoUpdateFrom ?? new List<Version>();
        CustomProperties = customProperties;
    }
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

    public UpdateSpec? 
    FindUpdateSpec(Version version) {
        var spec = UpdatesSpecs.FirstOrDefault(x => x.Version == version);
        if (spec == null && DefaultSpec == null)
            return null;

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
            customProperties:this.DefaultSpec?.CustomProperties == null && spec?.CustomProperties == null 
                ? null: 
                this.DefaultSpec?.CustomProperties.AddCustomProperties(spec?.CustomProperties, true));
    }
}

internal class 
PackageProjectionFileSpec {
    public List<RelativeGlob> Content { get; }
    public int MaxHostDeltas{ get; }
    public PackageProjectionFileSpec(List<RelativeGlob> content, int maxHostDeltas = 0) => 
        (Content, MaxHostDeltas) = (content, maxHostDeltas);

    public bool HostDeltas => MaxHostDeltas >0;
}

internal class
PackageProjectionSpecs {
    public List<PackageProjectionFileSpec> HostedFiles { get; }
    public PackageProjectionSpecs(List<PackageProjectionFileSpec> hostedFiles) => HostedFiles = hostedFiles;
}
}

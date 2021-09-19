using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using ProtoBuf;

namespace UpToYou.Core {
    
[ProtoContract]
public class 
Update: IHasCustomProperties, IComparable<Update> {
    [ProtoMember(1)] public PackageMetadata PackageMetadata { get; }
    [ProtoMember(2)] public UpdatePolicy UpdatePolicy{ get; }
    [ProtoMember(4)] public Dictionary<string, string>? CustomProperties { get; }
    public Update(PackageMetadata packageMetadata, UpdatePolicy updatePolicy, Dictionary<string, string>? customProperties) {
        PackageMetadata = packageMetadata;
        UpdatePolicy = updatePolicy;
        CustomProperties = customProperties;
    }

    #pragma warning disable CS8618 // Non-nullable field is uninitialized.
    protected Update() {
        UpdatePolicy = UpdatePolicy.Default;
    } 
    #pragma warning restore CS8618 // Non-nullable field is uninitialized.

    public DateTime DatePublished => PackageMetadata.DatePublished;
    public Version Version => PackageMetadata.Version;
    
    public int 
    CompareTo(Update other) => this.PackageMetadata.DatePublished.CompareTo(other.PackageMetadata.DatePublished);

    public override string ToString() => PackageMetadata.ToString();
}

[ProtoContract]
public readonly struct 
UpdateRing {
    [ProtoMember(1)] public int UserPercentage { get; }
    public UpdateRing(int userPercentage) => UserPercentage = userPercentage;
}

[ProtoContract]
public class 
UpdatePolicy {
    [ProtoMember(1)] public bool IsAuto { get; }
    [ProtoMember(2)] public bool IsRequired { get; }
    [ProtoMember(3)] public UpdateRing UpdateRing { get; }

    /// <summary>
    /// Update is lazy if it is not installed on application startup. Installation should be initiated by the client code.
    /// The "new" marker is not displayed in updates notes for lazy and auto ((IsAuto==true || IsRequired) && IsLazy==true) updates.
    /// </summary>
    [ProtoMember(5)] public bool IsLazy { get; }

    /// <summary>
    /// If client's version is contained in the given list then the client should be automatically updated. 
    /// </summary>
    [ProtoMember(6)] public List<Version> AutoUpdateFrom { get; }
    public UpdatePolicy(bool isAuto, bool isRequired, UpdateRing updateRing, bool isLazy, List<Version> autoUpdateFrom) {
        IsAuto = isAuto;
        IsRequired = isRequired;
        UpdateRing = updateRing;
        IsLazy = isLazy;
        AutoUpdateFrom = autoUpdateFrom;
    }

    public bool 
    IsAutoUpdate(Version? currentVersion) => 
        IsAuto && 
        (AutoUpdateFrom.Count == 0 ||
        currentVersion ==null || 
        AutoUpdateFrom.Any(x => x == currentVersion));

    #pragma warning disable CS8618 // Non-nullable field is uninitialized.
    protected UpdatePolicy() {
        AutoUpdateFrom = new List<Version>();
        UpdateRing = new UpdateRing(0);
    }
    #pragma warning restore CS8618 // Non-nullable field is uninitialized.

    public static UpdatePolicy 
    Default => new UpdatePolicy(false, false, new UpdateRing(0), false, new ());
}

[ProtoContract]
public class 
UpdatesManifest {
    [ProtoMember(1)] public ImmutableList<Update> UpdatesByDate { get; }
    public UpdatesManifest(IList<Update> updates) {
        UpdatesByDate = updates.OrderByDescending(x => x.PackageMetadata.DatePublished).ToImmutableList();
        UpdatesByVersion = updates.OrderByDescending(x => x.PackageMetadata.Version).ToImmutableList();
    }

    public ImmutableList<Update> UpdatesByVersion { get; }
    
    public IEnumerable<Update> Updates => UpdatesByDate;

    public bool
    TryGetUpdate(Version version, string packageName, out Update result) => 
        UpdatesByDate.TryGet(x => x.PackageMetadata.Version == version && x.PackageMetadata.Name == packageName, out result);

    public IEnumerable<Update> 
    GetUpdates(string packageName) => UpdatesByDate.Where(x =>  x.PackageMetadata.Name == packageName);

    public IEnumerable<Update> 
    GetUpdatesOfHigherVersion(Version version, string packageName) =>
        UpdatesByDate.Where(x => x.PackageMetadata.Version > version && x.PackageMetadata.Name == packageName);

    protected UpdatesManifest():this(new List<Update>()) { }
    
}

}

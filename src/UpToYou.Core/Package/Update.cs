using System;
using System.Collections.Generic;
using System.Linq;
using ProtoBuf;

namespace UpToYou.Core {

[ProtoContract]
public struct 
UpdateRing {
    [ProtoMember(1)]
    public int UserPercentage { get; }

    public UpdateRing(int userPercentage) => UserPercentage = userPercentage;
}

public interface 
IUpdateRingPolicy {
    int GetPercentage();
}

public static class 
UpdateRingEx {
    public static bool 
    Hits(this UpdateRing ring, IUpdateRingPolicy updateRingPolicy) { 
        if (ring.UserPercentage >= 100 && ring.UserPercentage == 0)
            return true;

        return updateRingPolicy.GetPercentage()<= ring.UserPercentage;
    }
}

[ProtoContract]
public class 
UpdatePolicy {
    [ProtoMember(1)]
    public bool IsAuto { get; }

    [ProtoMember(2)]
    public bool IsRequired { get; }

    [ProtoMember(3)]
    public UpdateRing UpdateRing { get; }

    [ProtoMember(4)] 
    public bool IsBeta { get; }

    /// <summary>
    /// Update is lazy if it is not installed on application startup. Installation should be initiated by the client code.
    /// The "new" marker is not displayed in updates notes for lazy and auto ((IsAuto==true || IsRequired) && IsLazy==true) updates.
    /// </summary>
    [ProtoMember(5)] 
    public bool IsLazy { get; }

    /// <summary>
    /// If client's version is contained in the given list then the client should be automatically updated. 
    /// </summary>
    [ProtoMember(6)]
    public List<Version> AutoUpdateFrom { get; }

    public bool IsAutoUpdate(Version? currentVersion) => 
        IsAuto && (AutoUpdateFrom.Count == 0 || currentVersion ==null || AutoUpdateFrom.Any(x => x == currentVersion));

#pragma warning disable CS8618 // Non-nullable field is uninitialized.
    protected UpdatePolicy() {
        AutoUpdateFrom = new List<Version>();
        UpdateRing = new UpdateRing(0);
    }
#pragma warning restore CS8618 // Non-nullable field is uninitialized.

    public UpdatePolicy(bool isAuto, bool isRequired, UpdateRing updateRing, bool isBeta, bool isLazy, List<Version>? autoUpdateFrom) {
        IsBeta = isBeta;
        IsLazy = isLazy;
        AutoUpdateFrom = autoUpdateFrom ?? new List<Version>();
        (IsAuto, IsRequired, UpdateRing) = (isAuto, isRequired, updateRing);
    }

    public static UpdatePolicy Default => new UpdatePolicy(false, false, new UpdateRing(0), false, false, null);
}

[ProtoContract]
public class PackageDependency {
    [ProtoMember(1)]
    public string PackageName { get; }
    [ProtoMember(2)]
    public Version MinVersion { get; }

#pragma warning disable CS8618 // Non-nullable field is uninitialized.
#pragma warning disable CS8618 // Non-nullable field is uninitialized.
    public PackageDependency() { }
#pragma warning restore CS8618 // Non-nullable field is uninitialized.
#pragma warning restore CS8618 // Non-nullable field is uninitialized.

    public PackageDependency(string packageName, Version minVersion) => (PackageName, MinVersion) = (packageName, minVersion);
}

public interface IHasPackageDependencies {
    List<PackageDependency> Dependencies { get; }
}

[ProtoContract]
public class 
Update: IHasCustomProperties, IHasPackageDependencies, IComparable<Update> {
    [ProtoMember(1)]
    public PackageMetadata PackageMetadata { get; }
    [ProtoMember(2)]
    public UpdatePolicy UpdatePolicy{ get; }
    //[ProtoMember(3)]
    //public string? UpdateNotes { get; set;}
    [ProtoMember(4)]
    public Dictionary<string, string>? CustomProperties { get; }

    [ProtoMember(5)] 
    public List<PackageDependency> Dependencies { get; set;}


#pragma warning disable CS8618 // Non-nullable field is uninitialized.
    protected Update() {
        Dependencies = new List<PackageDependency>();
        UpdatePolicy = UpdatePolicy.Default;
    } 
#pragma warning restore CS8618 // Non-nullable field is uninitialized.

    public Update(PackageMetadata packageMetadata, UpdatePolicy updatePolicy, List<PackageDependency>? dependencies = null, Dictionary<string, string>? customProperties = null) =>
        (PackageMetadata, UpdatePolicy, Dependencies, CustomProperties) = (packageMetadata, updatePolicy, dependencies??new List<PackageDependency>(), customProperties);

    public bool HasDependencies => Dependencies.Count > 0;


    public int CompareTo(Update other) => this.PackageMetadata.DateBuilt.CompareTo(other.PackageMetadata.DateBuilt);

    public override string ToString() => PackageMetadata.ToString();
}

[ProtoContract]
public class 
UpdatesManifest {
    [ProtoMember(1)]
    public UpdatesByDate UpdatesByDate { get; }

    public Lazy<UpdatesByVersion> UpdatesByVersion { get; }

    public IEnumerable<Update> Updates => UpdatesByDate;

    public Update? FindUpdate(Version version, string? packageName) => 
        UpdatesByDate.FirstOrDefault(x => x.PackageMetadata.Version == version && x.PackageMetadata.Name == (packageName??string.Empty));

    public IEnumerable<Update> FindUpdates(string? packageName) => UpdatesByDate.Where(x =>  x.PackageMetadata.Name == (packageName??string.Empty));

    public IEnumerable<Update> FindUpdatesOfHigherVersion(Version version, string? packageName) =>
        UpdatesByDate.Where(x => x.PackageMetadata.Version > version && x.PackageMetadata.Name == (packageName??string.Empty));

    protected UpdatesManifest():this(new List<Update>()) { }

    public UpdatesManifest(List<Update> updates) {
        UpdatesByDate = new UpdatesByDate(updates);
        UpdatesByVersion = new Lazy<UpdatesByVersion>(() => new UpdatesByVersion(UpdatesByDate));
    }
    
}

[ProtoContract]
public class 
UpdatesByDate : SortedSet<Update> {
    public UpdatesByDate() : base(new UpdateByDateComparer()) { }
    public UpdatesByDate(ICollection<Update> updates) : base(updates, new UpdateByDateComparer()) { }
}

public class UpdatesByVersion : SortedSet<Update> {
    public UpdatesByVersion() : base(new UpdateByDateComparer()) { }
    public UpdatesByVersion(ICollection<Update> updates) : base(updates, new UpdateByVersionComparer()) { }
}

public class 
UpdateByDateComparer: IComparer<Update> {
    public int 
    Compare(Update x, Update y) => -x?.PackageMetadata.DateBuilt.CompareTo(y?.PackageMetadata.DateBuilt) ?? -1;
}

public class 
UpdateByVersionComparer: IComparer<Update> {
    public int 
    Compare(Update x, Update y) => -x?.PackageMetadata.Version.CompareTo(y?.PackageMetadata.Version) ?? -1;
}

public static  class 
UpdatesModule {

    public static List<PackageDependency> 
    AddDependencies(this List<PackageDependency> dependencies, List<PackageDependency> other, bool @overwrite = false) {
        var result = dependencies.ToList();
        foreach (var otherDep in other) {
            var existingDep = result.FirstOrDefault(x => x.PackageName == otherDep.PackageName);
            if (existingDep == null)
                result.Add(otherDep);
            else if (@overwrite && existingDep.MinVersion < otherDep.MinVersion) {
                result.Remove(existingDep);
                result.Add(otherDep);
            }
        }
        return result;
    }

    public static UpdatesManifest
    Remove(this UpdatesManifest manifest, string packageName, Version version) {
        manifest.UpdatesByDate.RemoveWhere(x => x.PackageMetadata.Name == packageName && x.PackageMetadata.Version == version);
        return manifest;
    }

    public static UpdatesManifest
    Remove(this UpdatesManifest manifest, string packageName) {
        manifest.UpdatesByDate.RemoveWhere(x => x.PackageMetadata.Name == packageName);
        return manifest;
    }

    public static UpdatesManifest
    Remove(this UpdatesManifest manifest, PackageMetadata packageMetadata) => 
        manifest.Remove(packageName:packageMetadata.Name, version:packageMetadata.Version);

    public static UpdatesManifest
    Remove(this UpdatesManifest manifest, Update update) {
        manifest.UpdatesByDate.Remove(update);
        return manifest;
    }

    public static UpdatesManifest 
    AddOrChangeUpdate(this UpdatesManifest manifest, Update update) {
        var existingUpdate = manifest.UpdatesByDate.FirstOrDefault(x => x.PackageMetadata.IsSamePackage(update.PackageMetadata));
        if (existingUpdate != null)
            manifest.Remove(existingUpdate);
                
        manifest.UpdatesByDate.Add(update);
        return manifest;
    }

    public static UpdatesManifest
    ChangeUpdate(this UpdatesManifest manifest, Update update) {
        var existingUpdate = manifest.UpdatesByDate.FirstOrDefault(x => x.PackageMetadata.IsSamePackage(update.PackageMetadata))
            ?? throw new InvalidOperationException("Update not found in the manifest");

        manifest.Remove(existingUpdate);
        manifest.UpdatesByDate.Add(update);
        return manifest;
    }
}

}

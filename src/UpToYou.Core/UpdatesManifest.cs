using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ProtoBuf;

namespace UpToYou.Core {
[ProtoContract]
public class 
UpdatesManifest {
    
    [ProtoMember(1)] public ImmutableList<PackageHeader> PackagesByDate { get; }
    public UpdatesManifest(IList<PackageHeader> packages) {
        PackagesByDate = packages.OrderByDescending(x => x.DatePublished).ToImmutableList();
    }

    public IEnumerable<PackageHeader> Packages => PackagesByDate;
    protected UpdatesManifest() => PackagesByDate = ImmutableList<PackageHeader>.Empty;
    
    public PackageHeader
    GetPackageHeader(string packageId) => 
        Packages.TryGet(x => x.Id == packageId, out var result) ? result : throw new InvalidOperationException($"Package with id = {packageId} not found in the manifest");
    
    public bool 
    TryGetPackage(string packageName, Version version, out PackageHeader result) => 
        Packages.TryGet(package => package.Name.Equals(packageName, StringComparison.OrdinalIgnoreCase) && version.Equals(package.Version), out result);
}

public static class 
UpdatesManifestHelper {
    
    public static UpdatesManifest
    RemovePackage(this UpdatesManifest manifest, string packageId) => new (manifest.PackagesByDate.RemoveAll(x => x.Id == packageId));
    
    public static UpdatesManifest
    AddPackage(this UpdatesManifest manifest, PackageHeader package) => new(manifest.PackagesByDate.Add(package));
    
    public static UpdatesManifest 
    AddOrChangeUpdate(this UpdatesManifest manifest, PackageHeader package) {
        if (manifest.Packages.TryGet(x => x.IsSamePackage(package), out var existingPackage))
            manifest = manifest.RemovePackage(existingPackage.Id);
                
        return manifest.AddPackage(package);
    }
    
    public static IEnumerable<PackageHeader>
    GetPackageHeaders(this UpdatesManifest manifest, string packageName) =>
        manifest.Packages.Where(x => x.Name == packageName);
}
}

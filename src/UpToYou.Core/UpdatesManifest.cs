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
    
    [ProtoMember(1)] public ImmutableList<PackageMetadata> PackagesByDate { get; }
    public UpdatesManifest(IList<PackageMetadata> packages) {
        PackagesByDate = packages.OrderByDescending(x => x.DatePublished).ToImmutableList();
    }

    public IEnumerable<PackageMetadata> Packages => PackagesByDate;
    protected UpdatesManifest() => PackagesByDate = ImmutableList<PackageMetadata>.Empty;
    
    public bool 
    TryGetPackage(string packageName, Version version, out PackageMetadata result) => 
        Packages.TryGet(package => package.Name.Equals(packageName, StringComparison.OrdinalIgnoreCase) && version.Equals(package.Version), out result);
}

public static class 
UpdatesManifestHelper {
    
    public static UpdatesManifest
    RemovePackage(this UpdatesManifest manifest, string packageId) => new (manifest.PackagesByDate.RemoveAll(x => x.Id == packageId));
    
    public static UpdatesManifest
    AddPackage(this UpdatesManifest manifest, PackageMetadata package) => new(manifest.PackagesByDate.Add(package));
    
    public static UpdatesManifest 
    AddOrChangeUpdate(this UpdatesManifest manifest, PackageMetadata package) {
        if (manifest.Packages.TryGet(x => x.IsSamePackage(package), out var existingPackage))
            manifest.RemovePackage(existingPackage.Id);
                
        manifest.AddPackage(package);
        return manifest;
    }
    

}
}

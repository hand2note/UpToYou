using System;
using System.Collections.Generic;
using System.Linq;
using UpToYou.Backend;
using UpToYou.Core;

namespace UpToYou.Tests
{
    public class Pmtc_h2n: IPackageHeaderTestCase
    {
        public Pmtc_h2n( string packageName, DateTime? dateBuilt = null, Dictionary<string, string>? customProperties = null, RelativePath? versionProvider = null) {
            DateBuilt = dateBuilt;
            CustomProperties = customProperties;
            VersionProvider = versionProvider;
            PackageName = packageName;
        }

        public string PackageName { get; }

        public DateTime? DateBuilt { get; } 
        public Dictionary<string, string>? CustomProperties { get; }
        public RelativePath? VersionProvider { get; }
    }

internal class Ptc_h2n : IPackageTestCase {
    public const string NeverChangingFile = "System.Threading.Tasks.Extensions.dll";
    public Ptc_h2n(string version) {
        SourceDirectory = version.ParseVersion().GetH2nRootDirectory();
        PackageHeader = new Pmtc_h2n("Hand2Note");
        PackageSpecs = new [] {
            "Hand2Note.exe", 
            "Hand2NoteCore.dll",
            NeverChangingFile
        }.ToRelativePaths().FilesToPackageSpecs("Hand2Note.exe".ToRelativePath());
    }

    public string SourceDirectory { get; }
    public PackageSpecs? PackageSpecs { get; }
    public IPackageHeaderTestCase PackageHeader { get; }
}

internal class Pjtc_h2n: IProjectionTestCase {
    public IPackageTestCase PackageTestCase { get; }
    public PackageProjectionSpecs? ProjectionSpecs { get; }
    public IFilesHostTestState? HostState { get; }

    public Pjtc_h2n(string version, PackageProjectionSpecs? projectionSpecs = null, IFilesHostTestState? hostState = null) {
        ProjectionSpecs = projectionSpecs;
        HostState = hostState;
        PackageTestCase = new Ptc_h2n(version);
    }
}

internal class Pjtc_h2n_with_deltas: IProjectionTestCase {
    
    public Pjtc_h2n_with_deltas(string version,  IFilesHostTestState? hostState = null) {
        PackageTestCase = new Ptc_h2n(version);
        ProjectionSpecs =new PackageProjectionSpecs(new List<PackageProjectionFileSpec>() {
            new PackageProjectionFileSpec( PackageTestCase.PackageSpecs!.GetFilesRelative(PackageTestCase.SourceDirectory).ToRelativeGlobs().ToList(), 3)
        });
        HostState = hostState;
    }

    public IPackageTestCase PackageTestCase { get; }
    public PackageProjectionSpecs? ProjectionSpecs { get; }
    public IFilesHostTestState? HostState { get; }
}

internal class Fhtc_last_two_versions_with_deltas: FilesHostTestCase {

    public Fhtc_last_two_versions_with_deltas() : base(
        new Pjtc_h2n("3.2.6.14"),
        new Pjtc_h2n_with_deltas("3.2.6.22")) { }
}


}

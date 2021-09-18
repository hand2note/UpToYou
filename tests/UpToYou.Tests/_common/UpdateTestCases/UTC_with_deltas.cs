//using System.Collections.Generic;
//using System.Linq;
//using UpToYou.Backend;
//using UpToYou.Core;

//namespace UpToYou.Tests.UpdateTestCases {

//internal class 
//    UTC_with_deltas : UTC_simple {
//    public UTC_with_deltas() : this("3.2.6.19", "3.2.6.22") { }
//    public UTC_with_deltas(string from, string to) : base(from, to) { }

//    protected override string SourceDirectoryFrom => TestData.TestDataDirectory.AppendPath($"h2nUpdates/{_versionFrom}");
//    protected override string SourceDirectoryTo => TestData.TestDataDirectory.AppendPath($"h2nUpdates/{_versionTo}");

//    public override List<IProjectionTestCase>? PreviousProjections => (new List<IProjectionTestCase>() {
//        new UTC_simple("3.2.6.21"),
//        new UTC_with_deltas("3.2.6.19", "3.2.6.20"),
//        new UTC_with_deltas("3.2.6.18", "3.2.6.19"),
//    }).Where(x => x.Version < this.Version).ToList() ;

//    public override PackageProjectionSpecs ProjectionSpecs =>
//        new PackageProjectionFileSpec(PackageFiles.RelativeFiles,2).ToProjectionSpecs();
//}
//}


//using System.Collections.Generic;
//using System.Linq;
//using UpToYou.Backend;
//using UpToYou.Core;

//namespace UpToYou.Tests.UpdateTestCases {

//internal class UTC_with_deltas_and_new_files : UTC_with_deltas {

//    public override TestFilesSet 
//    PackageFiles => new TestFilesSet(
//        root: base.PackageFiles.Root,
//        files: base.PackageFiles.Files.Union("Hand2Note.exe".ToSingleEnumerable()));

//    //Adding new file to the separate hosted file
//    public override PackageProjectionSpecs 
//    ProjectionSpecs => new PackageProjectionSpecs(
//        hostedFiles:new List<PackageProjectionFileSpec>() {
//            new PackageProjectionFileSpec(PackageFiles.RelativeFiles.Except(x => x.Value == "Hand2Note.exe").ToList(),  3),
//            new PackageProjectionFileSpec("Hand2Note.exe".ToRelativePath().ToSingleItemList())
//        });
//}

//}
//using System.Collections.Generic;
//using System.Linq;
//using UpToYou.Backend;
//using UpToYou.Core;

//namespace UpToYou.Tests.UpdateTestCases {

//internal class
//UTC_whole_h2n : UTC_simple {
//    string _clientFileSourceDir => TestData.TestDataDirectory.AppendPath($"h2nRoot/{_versionFrom}");
//    string _packageFileSourceDir => TestData.TestDataDirectory.AppendPath($"h2nRoot/{_versionTo}");

//    public override TestFilesSet ClientFiles => new TestFilesSet(
//        _clientFileSourceDir, new List<string>() {
//              "x64/h2npoker.dll",
//              "external/x64/h2nwpn.exe",
//              "external/x64/h2nps.exe",
//              "external/x64/h2nemi.dll",
//              "external/x86/h2nwpn.exe",
//              "external/x86/h2nps.exe",
//              "external/x86/h2nemi.dll"
//          }
//          .Union("config".ToAbsoluteFilePath(_clientFileSourceDir).EnumerateAllDirectoryFiles().Select(x => x.GetPathRelativeTo(_clientFileSourceDir).Value))
//          .Union("pgsql".ToAbsoluteFilePath(_clientFileSourceDir).EnumerateAllDirectoryFiles().Select(x => x.GetPathRelativeTo(_clientFileSourceDir).Value))
//          .Union("ru".ToAbsoluteFilePath(_clientFileSourceDir).EnumerateAllDirectoryFiles().Select(x => x.GetPathRelativeTo(_clientFileSourceDir).Value))
//          .Union(_clientFileSourceDir.EnumerateDirectoryRelativeFiles(false).Select(x => x.Value))
//    );

//    public override TestFilesSet PackageFiles => new TestFilesSet(_packageFileSourceDir, ClientFiles.Files.Where(x => x.ToAbsoluteFilePath(_packageFileSourceDir).FileExists()) );

//    public override PackageSpecs PackageSpecs => new PackageSpecs(null,null,
//        folders: new List<PackageFolderSpec>() {
//            new PackageFolderSpec("config".ToRelativePath()),
//            new PackageFolderSpec("ru".ToRelativePath()),
//            new PackageFolderSpec("pgsql".ToRelativePath()),
//        },
//        files: new[] {
//            "x64/h2npoker.dll",
//            "external/x64/h2nwpn.exe",
//            "external/x64/h2nps.exe",
//            "external/x64/h2nemi.dll",
//            "external/x86/h2nwpn.exe",
//            "external/x86/h2nps.exe",
//            "external/x86/h2nemi.dll"
//            }.MapToList(x => x.ToRelativePath().ToPackageFileSpec("Hand2NoteCore.dll".ToRelativePath()))
//             .Union(_packageFileSourceDir.EnumerateDirectoryRelativeFiles(false).MapToList(x => x.ToPackageFileSpec("Hand2NoteCore.dll".ToRelativePath())))
//             .ToList());

//    public override PackageProjectionSpecs ProjectionSpecs => @"
//HostedFiles:
//    - Content:
//        - config
//    - Content:
//        - pgsql
//    - Content:
//        -ru
//        -external/x64/h2nwpn.exe 
//        -external/x64/h2nps.exe
//        -external/x64/h2nemi.dll    
//        -external/x86/h2nwpn.exe
//        -external/x86/h2nps.exe
//        -external/x86/h2nemi.dll
//    - Content:
//        - Hand2Note.exe
//        - Hand2NoteCore.dll
//".Trim().ParseProjectionFromYaml();
//}

//}
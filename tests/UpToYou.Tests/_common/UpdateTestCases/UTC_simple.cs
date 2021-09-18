//using System;
//using System.Collections.Generic;
//using System.Linq;
//using UpToYou.Backend;
//using UpToYou.Core;

//namespace UpToYou.Tests.UpdateTestCases
//{

//internal class 
//UTC_simple : IUpdateTestCase {
//    protected readonly string _versionFrom;
//    protected readonly string _versionTo;

//    public UTC_simple() : this("3.2.6.14", "3.2.6.22") { }
//    public UTC_simple(string versionFrom, string versionTo ="3.2.6.22" ) {
//        _versionFrom = versionFrom;
//        _versionTo = versionTo;
//    }

//    protected virtual string 
//    SourceDirectoryFrom => TestData.TestDataDirectory.AppendPath($"h2nRoot/{_versionFrom}");

//    protected virtual string 
//    SourceDirectoryTo => TestData.TestDataDirectory.AppendPath($"h2nRoot/{_versionTo}");

//    public virtual TestFilesSet 
//    ClientFiles => new TestFilesSet(
//        SourceDirectoryFrom,
//        "Hand2Note.exe",
//        "Hand2NoteCore.dll",
//        //"Common.dll",
//        "npgsql.dll"
//    );

//    public string VersionProvider { get; } = "Hand2Note.exe";
//    public Version Version  =>_versionTo.ParseVersion();
//    public virtual TestFilesSet PackageFiles => new TestFilesSet(SourceDirectoryTo, ClientFiles);
//    public virtual PackageSpecs PackageSpecs => PackageFiles.RelativeFiles.FilesToPackageSpecs("Hand2Note.exe".ToRelativePath());
//    public virtual PackageProjectionSpecs ProjectionSpecs => new PackageProjectionFileSpec( PackageFiles.RelativeFiles).ToProjectionSpecs();
//    public virtual List<IProjectionTestCase>? PreviousProjections { get; }
//}


//internal class 
//UTC_with_deleted_file: UTC_simple {
//    public override TestFilesSet PackageFiles => new TestFilesSet(
//        root:base.PackageFiles.Root,
//        files:base.PackageFiles.Files.Except("Hand2NoteCore.dll".ToSingleEnumerable()));
//}

//internal class
//UTC_when_projection_specs_missing_package_file: UTC_simple {
//    public override PackageProjectionSpecs ProjectionSpecs => new PackageProjectionFileSpec(
//        base.ProjectionSpecs.HostedFiles[0].Content
//            .Except(x => x.Value == "Hand2NoteCore.dll").ToList()).ToProjectionSpecs();
//}

//internal class
//UTC_with_updater_own_dlls : UTC_simple {
//    private static string _clientDirectory =  "WithUpdaterDlls/client".ToAbsoluteFilePath(TestData.TestDataDirectory);

//    public override TestFilesSet ClientFiles { get; } = new TestFilesSet(
//        root:_clientDirectory,
//        files: _clientDirectory.EnumerateAllDirectoryFiles().Select(x => x.GetPathRelativeTo(_clientDirectory).Value));

//    public override TestFilesSet PackageFiles => new TestFilesSet("WithUpdaterDlls/update".ToAbsoluteFilePath(TestData.TestDataDirectory), ClientFiles);
//}

//}





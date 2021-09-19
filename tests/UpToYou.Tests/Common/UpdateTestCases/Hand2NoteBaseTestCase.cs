using System;
using System.Collections.Generic;
using System.Linq;
using UpToYou.Backend;
using UpToYou.Core;

namespace UpToYou.Tests.UpdateTestCases {

internal class Hand2NoteBaseTestCase:IPushUpdateTestCase {
    public Version Version { get; }
    public TestFilesSet PackageFiles { get; }
    public PackageSpecs PackageSpecs { get; }
    public PackageProjectionSpecs ProjectionSpecs { get; }
    public List<IProjectionTestCase>? PreviousProjections { get; }
    public string VersionProvider { get; }
    public TestFilesSet ClientFiles => throw new NotImplementedException();

    public string PackageSpecsFile { get; }
    public string ProjectionSpecsFile { get; }
    public string UpdateNotesFile { get; }

    public Hand2NoteBaseTestCase(string version, string architecture) {
    
        Version = version.ParseVersion();
        var src =  Version.GetH2nRootDirectory();
        PackageFiles = new TestFilesSet(
            root:src,
            files:src.EnumerateDirectoryRelativeFiles().MapToList(x => x.Value));

        PackageSpecsFile = $"Hand2Note.{architecture}.pspec".ToAbsoluteFilePath(TestData.TestDataDirectory);
        PackageSpecs = PackageSpecsFile.ReadAllFileText().Trim().ParsePackageSpecsFromYaml();

        ProjectionSpecsFile = $"Hand2Note.{architecture}.pjspec" .ToAbsoluteFilePath(TestData.TestDataDirectory);
        ProjectionSpecs = ProjectionSpecsFile.ReadAllFileText().Trim().ParseProjectionFromYaml();

        PreviousProjections = null;
        VersionProvider = PackageSpecs.VersionProvider.Value;

        UpdateNotesFile = $"Hand2Note.UpdateNotes.en.md";
}
}

internal class H2nExternalPushUpdateTestCase: IPushUpdateTestCase {
    public Version Version { get; }
    public TestFilesSet PackageFiles { get; }
    public PackageSpecs PackageSpecs { get; }
    public PackageProjectionSpecs ProjectionSpecs { get; }
    public List<IProjectionTestCase>? PreviousProjections { get; }
    public string VersionProvider { get; }
    public TestFilesSet ClientFiles { get; }
    public string PackageSpecsFile { get; }
    public string ProjectionSpecsFile { get; }

    public H2nExternalPushUpdateTestCase(string package, string version, string architecture) {
        var src = $"{TestData.H2nExternalDir}\\{package}\\{version}";
    
        Version = version.ParseVersion();
        PackageFiles = new TestFilesSet(
            root:src,
            files:src.EnumerateDirectoryRelativeFiles().Where(x => x.Value.Contains($"\\{architecture}\\")).Select(x => $"external\\{architecture}\\{x.Value.GetFileName()}").ToList());

        PackageSpecsFile = $"{package}.{architecture}.pspec".ToAbsoluteFilePath(TestData.TestDataDirectory);
        PackageSpecs = PackageSpecsFile.ReadAllFileText().ParsePackageSpecsFromYaml();

        ProjectionSpecsFile = $"{package}.{architecture}.pjspec" .ToAbsoluteFilePath(TestData.TestDataDirectory);
        ProjectionSpecs = ProjectionSpecsFile.ReadAllFileText().ParseProjectionFromYaml();

        PreviousProjections = null;
        VersionProvider = PackageSpecs.VersionProvider.Value;

    }
}

}
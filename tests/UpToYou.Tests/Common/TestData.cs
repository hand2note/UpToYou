using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UpToYou.Backend;
using UpToYou.Core;

namespace UpToYou.Tests
{
internal static class TestData {

    public static string
        SourcesDirectory {
        get {
            string startupPath = Environment.CurrentDirectory;
            var pathItems = startupPath.Split(Path.DirectorySeparatorChar);
            var pos = pathItems.Reverse().ToList().FindIndex(x => string.Equals("bin", x));
            return String.Join(Path.DirectorySeparatorChar.ToString(), pathItems.Take(pathItems.Length - pos - 1));
        }
    }

    private const string H2nLastVersionDir = "h2nRoot 3.2.6.24";
    private const string H2nUpdatesDir = "h2nUpdates";
    private const string H2nRootDir = "h2nRoot";
    public static string H2nExternalDir => "h2nExternal".ToAbsoluteFilePath(TestDataDirectory); 

    public static readonly List<Version> H2nRootVersions = new List<Version>() {
        Version.Parse("3.2.6.14"),
        Version.Parse("3.2.6.22"),
        Version.Parse("3.2.6.24"),
    };
    
    public static readonly Version H2nRootLastVersion = H2nRootVersions.Last();
    public static readonly Version H2nRootPreviousVersion = H2nRootVersions.NotEqual(H2nRootLastVersion).First();

    public static readonly List<Version> H2nUpdateVersions = new  List<Version>() {
        Version.Parse("3.2.6.18"),
        Version.Parse("3.2.6.19"),
        Version.Parse("3.2.6.20"),
        Version.Parse("3.2.6.21"),
        Version.Parse("3.2.6.22"),
    };

    public static readonly  Version H2nUpdateLastVersion  = H2nUpdateVersions.Last();

    public static string 
    GetH2nExternalPackageDir(string package, string version, string architecture) => 
        $"{H2nExternalDir}\\{package}\\{version}\\{architecture}";

    public static Version
    AnyH2nNonRootVersion => H2nUpdateVersions.NotEqual(H2nRootLastVersion).First();

    public static string 
    TestDataDirectory => SourcesDirectory.AppendPath("..\\UpToYou.Tests\\_testdata");

    public static string 
    H2nLastVersionTestDir => TestDataDirectory.AppendPath(H2nLastVersionDir);

    public static PackageSpecs 
    H2nTestPackageSpecs => 
        TestDataDirectory.AppendPath("TestPackage.pspec").ReadAllFileText().ParsePackageSpecsFromYaml();

    public static PackageProjectionSpecs
    H2nTestProjectionSpecs =>  
        TestDataDirectory.AppendPath("TestPackageProjection.pjspec").ReadAllFileText().ParseProjectionFromYaml();

    public static string
    GetH2nRootDirectory(this Version version) => 
        H2nRootDir
            .ToAbsoluteFilePath(TestDataDirectory)
            .EnumerateChildDirectories(recursive:false)
            .FirstOrDefault(x => x.EndsWith(version.ToString()))
        ?? throw new InvalidOperationException($"H2nRoot source directory for version {version.ToString().Quoted()} not found");

    public static string
    GetH2nUpdateDirectory(this Version version) =>
        H2nUpdatesDir
            .ToAbsoluteFilePath(TestDataDirectory)
            .EnumerateChildDirectories(recursive:false)
            .FirstOrDefault(x => x.EndsWith(version.ToString()))
        ?? throw new InvalidOperationException($"h2nUpdate source directory for version {version.ToString().Quoted()} not found");

    public static (PackageSpecs specs, Version version, string srcDir)
    H2nLastVersionPackageInput => (H2nTestPackageSpecs, H2nUpdateLastVersion, H2nUpdateLastVersion.GetH2nRootDirectory());

}
}

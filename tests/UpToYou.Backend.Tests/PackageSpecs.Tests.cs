using System.Collections.Generic;
using System.Collections.Immutable;
using NUnit.Framework;
using UpToYou.Core;
using UpToYou.Core.Tests;

namespace UpToYou.Backend.Tests {
public class PackageSpecsTests {
    
    [Test] public static void
    ParsePackageSpecs_Yaml01() {
        var actual = @"PackageName: TestPackage
Files:
    - TestFile.dll
    - Test.exe

VersionProvider: Test.exe

CustomProperties:
    - TestBoolProperty: true
    - TestIntProperty: 1".ParsePackageSpecsFromYaml();
        actual.DeepAssert(new PackageSpecs(
            packageName: "TestPackage",
            files: new[] {
                "TestFile.dll".ToRelativeGlob(),
                "Test.exe".ToRelativeGlob()
            }.ToImmutableList(),
            excludedFiles:ImmutableList<RelativeGlob>.Empty, 
            versionProvider: "Test.exe".ToRelativePath(),
            customProperties: new Dictionary<string, string>() {
                {"TestBoolProperty", "True"},
                {"TestIntProperty","1"}
            }.ToImmutableDictionary()));
    }
}
}
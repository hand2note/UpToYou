using System;
using System.Linq;
using NUnit.Framework;
using UpToYou.Backend;
using UpToYou.Core;
using UpToYou.Tests.Minimods;

namespace UpToYou.Tests.BackendTests {

[TestFixture]
public class PackageSpecsTests {

[Test]
public void ParsePackageSpecsYaml() {
    var yaml = "Hand2Note.x64.pspec".ToAbsoluteFilePath(TestData.TestDataDirectory).ReadAllFileText().Trim();

    var actual = yaml.ParsePackageSpecsFromYaml();
    
    #if DEBUG
    Console.WriteLine(actual.PrettyPrint());
    #endif

    Assert.AreEqual("Hand2Note", actual.PackageName);
    Assert.AreEqual("Hand2Note.exe", actual.VersionProvider.Value);
    Assert.AreEqual("x64", actual.FindCustomProperty("Architecture"));
    Assert.IsTrue(actual.Files.Count > 2);
    Assert.IsTrue(actual.ExcludedFiles.Count > 0);
}

[Test]
public void ParseProjectionSpecsYaml() {
    var yaml = "Hand2Note.x64.pjspec".ToAbsoluteFilePath(TestData.TestDataDirectory).ReadAllFileText().Trim();
    var actual = yaml.ParseProjectionFromYaml();

    #if DEBUG
    Console.WriteLine(actual.PrettyPrint());
    #endif

    Assert.IsTrue(actual.HostedFiles.All(x => x.Content.Count > 0));
    Assert.IsTrue(actual.HostedFiles.Count > 0);
}

[Test]
public void Test() {
    var yaml = "Microgaming.x64.projection.specs".ToAbsoluteFilePath(TestData.TestDataDirectory).ReadAllFileText();
    var actual = yaml.ParseProjectionFromYaml();
    Assert.AreEqual(3, actual.HostedFiles[0].MaxHostDeltas);

    actual = yaml.Trim().ParseProjectionFromYaml();
    Assert.AreEqual(3, actual.HostedFiles[0].MaxHostDeltas);
}


}
}

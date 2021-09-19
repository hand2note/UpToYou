#nullable disable

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NUnit.Framework;
using UpToYou.Backend;
using UpToYou.Backend.Runner;
using UpToYou.Core;
using AssemblyInfo = UpToYou.Backend.AssemblyInfo;

namespace UpToYou.Tests.BackendTests {

[TestFixture]
public class UpdateSpecsTest {

    [Test]
    public void ParseYaml() {
        var actual = "PokerStars.updates.specs".ToAbsoluteFilePath(TestData.TestDataDirectory).ReadAllFileText().ParseUpdatesSpecsFromYaml();
        
        Assert.IsTrue(actual.DefaultSpec.IsLazy);
        Assert.IsTrue(actual.DefaultSpec.IsAuto);
        
        var spec = actual.FindUpdateSpec("1.0.4.6".ParseVersion());
        Assert.AreEqual("1.0.4.6".ParseVersion(), spec.Version);

        Assert.IsTrue(spec.IsBeta);
        Assert.IsTrue(spec.IsLazy);
        Assert.IsFalse(spec.IsAuto);
        Assert.IsNotNull(spec.AutoUpdateFrom.FirstOrDefault(x => x == "1.0.4.5".ParseVersion()));
        Assert.IsTrue(spec.FindCustomBoolProperty("InstallOnDemand"));

        spec = actual.FindUpdateSpec("1.0.4.5".ParseVersion());
        Assert.AreEqual(50, spec.UpdateRing);
        Assert.IsTrue(spec.IsAuto);
        Assert.IsTrue(spec.IsRequired);
        Assert.AreEqual("myvalue", spec.FindCustomProperty("MyProperty"));
        Assert.AreEqual("myvalue2", spec.FindCustomProperty("MyProperty2"));
    }

    [Test]
    public void ParseSimple() {
        var actual = ".updates.specs".ToAbsoluteFilePath(TestData.TestDataDirectory).ReadAllFileText().ParseUpdatesSpecsFromYaml();

        Assert.IsTrue(actual.DefaultSpec.IsAuto);
        Assert.IsTrue(actual.DefaultSpec.IsLazy);
        Assert.IsNull(actual.DefaultSpec.IsRequired);

    }

}
}

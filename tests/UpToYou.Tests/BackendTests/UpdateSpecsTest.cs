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

        var dependency = spec.Dependencies.First();
        Assert.AreEqual("Hand2Note", dependency.PackageName);
        Assert.AreEqual("3.2.6.25".ParseVersion(), dependency.MinVersion);

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

    [Test]
    public void UpdatesManifestDeserializationBug() {
        var manifest = new UpdatesManifest(new List<Update>() {
            new Update(new PackageMetadata(null, "Android", "1.0.0.0".ParseVersion(), DateTime.Now, null), new UpdatePolicy(
                isAuto:true, 
                isRequired:false,
                updateRing:new UpdateRing(0),
                isBeta:false,
                isLazy:true,
                null)),
            new Update(new PackageMetadata(null, "Hand2Note", "1.0.0.0".ParseVersion(), DateTime.Now.AddDays(-1), null), new UpdatePolicy(
                isAuto:false, 
                isRequired:true,
                updateRing:new UpdateRing(0),
                isBeta:false,
                isLazy:false,
                null))
        });

        var deserialized = manifest.ProtoSerializeToBytes().DeserializeProto<UpdatesManifest>();

        Assert.IsFalse(deserialized.Updates.FirstOrDefault(x => x.PackageMetadata.Name == "Android").UpdatePolicy.IsRequired);
    }

    //[Test]
    //public void Test() {

    //    var updatesManifest = "c:/_junk/.updates.proto.xz".ReadAllFileBytes().Decompress().DeserializeProto<UpdatesManifest>();

    //    var options = new PushUpdateOptions(
    //        sourceDirectory:"C:/h2nexternal.binaries",
    //        updatesSpecsFile:"C:/h2nExternal.Binaries/deploy/AndroidEmulator/.updates.specs",
    //        forceIfEmptyNotes:true,
    //        packageSpecsFile:"C:/h2nExternal.Binaries/deploy/AndroidEmulator/AndroidEmulator.x64.package.specs",
    //        azureRootContainer:"uptoyou",
    //        azureConnectionString:"DefaultEndpointsProtocol=https;AccountName=h2n;AccountKey=jzzncAXQsNVh7A7rHngOefRCKgiyIoGhizo8/rZy58cshUAnuT7fygVLNWToNycFTLT3EiMTBKHoXYo4dXrV2A==;EndpointSuffix=core.windows.net");

        
    //        options.PushUpdate();
        
    //}

}
}

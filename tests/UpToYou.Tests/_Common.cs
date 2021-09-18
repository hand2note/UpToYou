using KellermanSoftware.CompareNetObjects;
using NUnit.Framework;
using UpToYou.Core;

namespace UpToYou.Tests
{

public static class
AssertEx {
    public static void 
    ShouldBeSame(this object actual, object expected) {
        var res = new CompareLogic().Compare(actual, expected);
        if (!res.AreEqual)
            Assert.Fail($"\n{res.DifferencesString}\n");
    }
}

public class UpdaterTestFixture : TestFixtureAttribute {
    public UpdaterTestFixture() {
        ProtoRuntimeModel.Initialize();
    }
}

}

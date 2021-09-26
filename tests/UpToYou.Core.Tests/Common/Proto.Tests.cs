using System;
using NUnit.Framework;
using UpToYou.Core;
namespace UpToYou.Core.Tests {
public static class ProtoTests {
    
    [Test]
    public static void 
    SerializeVersion() =>
        new Version("1.0.0.0").ProtoSerializeToBytes().DeserializeProto<Version>().DeepAssert(new Version("1.0.0.0"));
}
}

using System;
using System.IO;
using ProtoBuf;
using ProtoBuf.Meta;

namespace UpToYou.Core {

internal static class
ProtoRuntimeModel {

    static bool _isInitialed = false;
    static readonly object LockObject = new object();

    public static void 
    Initialize() {
        lock (LockObject) {
            if (_isInitialed) return;
            RuntimeTypeModel.Default.Add(typeof(Version), true).SetSurrogate(typeof(VersionSurrogate));
            _isInitialed = true;
        }
    }
}

internal static class
Proto {

    static Proto() => ProtoRuntimeModel.Initialize();

    public static byte[] 
    ProtoSerializeToBytes(this object obj) { 
        var ms = new MemoryStream();
        Serializer.Serialize( ms, obj );
        return ms.ToArray();
    } 

    public static T 
    DeserializeProto<T>(this byte[] bytes) => ProtoBuf.Serializer.Deserialize<T>(new MemoryStream(bytes));

    public static T?
    TryDeserializeProto<T>(this byte[] bytes) where T:class {
        try {
            return bytes.DeserializeProto<T>();
        }
        catch (ProtoException) {
            return default;
        }
    }
}

[ProtoContract(ImplicitFields = ImplicitFields.AllPublic)]
internal class VersionSurrogate {
    public int X, Y, Z, W;
    public VersionSurrogate(int x, int y, int z, int w) {
        X = x;
        Y = y;
        Z = z;
        W = w;
    }

    public  static  implicit operator Version(VersionSurrogate version) => version != null? new Version(version.X, version.Y, version.Z, version.W) : new Version(0,0,0, 0);
    public  static  implicit operator VersionSurrogate(Version version) => version != null ? new VersionSurrogate(version.Major, version.Minor, version.Build, version.Revision) : new VersionSurrogate(0,0,0, 0);
}

}
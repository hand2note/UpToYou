using System.IO;
using deltaq.BsDiff;

namespace UpToYou.Core {

internal static class 
BinaryDelta
{
    public static byte[]
    GetDelta(byte[] fromBytes, byte[] toBytes) {
        var ms = new MemoryStream();
        BsDiff.Create(fromBytes, toBytes, ms);
        return ms.ToArray();
    }

    public static void
    GetDelta(byte[] fromBytes, byte[] toBytes, Stream outStream) => BsDiff.Create(fromBytes, toBytes, outStream);

    public static byte[]
    GetDelta(string fromFile, string toFile) =>
        GetDelta(fromFile.VerifyFileExistence().ReadAllFileBytes(), toFile.VerifyFileExistence().ReadAllFileBytes());

    public static void
    ApplyDelta(byte[] input, byte[] delta, Stream outStream) =>
        BsPatch.Apply(input, delta, outStream);

    public static void
    ApplyDelta(string inputFile, string deltaFile, Stream outStream) =>
        ApplyDelta(inputFile.VerifyFileExistence().ReadAllFileBytes(), 
            deltaFile.VerifyFileExistence().ReadAllFileBytes(), 
            outStream);

    public static byte[]
    GetAppliedDeltaBytes(byte[] input, byte[] delta) {
        var ms = new MemoryStream();
        BsPatch.Apply(input, delta, ms);
        return ms.ToArray();
    }

}
}

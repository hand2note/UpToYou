using System;
using System.IO;
using System.IO.Compression;
using System.Linq;
#if OS_WINDOWS
using XZ.NET;
#endif

namespace UpToYou.Core {

internal enum
CompressMethods {
    [FileExtension(".gz")] Gzip = 1,
    [FileExtension(".xz")] Xz = 2
}

internal class
CompressMethodAttribute : Attribute {
    public CompressMethods Value;
    public CompressMethodAttribute(CompressMethods value) => Value = value;
}

internal interface
ICompress {
    void Compress(Stream inStream, Stream outStream);
    void Decompress(Stream inStream, Stream outStream);
}

[CompressMethod(CompressMethods.Gzip)]
internal class 
GZipCompress: ICompress {
    public void 
    Compress(Stream inStream, Stream outStream) {
        using var gs = new GZipStream(outStream, CompressionMode.Compress, leaveOpen:true);
        inStream.CopyTo(gs);
    }

    public void Decompress(Stream inStream, Stream outStream) {
        using var gs = new GZipStream(inStream, CompressionMode.Decompress, leaveOpen:true);
        gs.CopyTo(outStream);
    }
}

#if OS_WINDOWS
[CompressMethod(CompressMethods.Xz)]
internal class 
XzCompress:ICompress {
    public void 
    Compress(Stream inStream, Stream outStream) {
        using var xz = new XZOutputStream(outStream, 1, 9u, true);
        inStream.CopyTo(xz);
    }

    public void 
    Decompress(Stream inStream, Stream outStream) {
        using var xz = new XZInputStream(inStream, leaveOpen:true);
        xz.CopyTo(outStream);
    }
}
#endif

internal static class   
Compressing {
    #if OS_WINDOWS
    public const CompressMethods DefaultCompressMethod = CompressMethods.Xz;
    #else
    public const CompressMethods DefaultCompressMethod = CompressMethods.Gzip;
    #endif
    public static readonly string DefaultCompressMethodFileExtension = DefaultCompressMethod.GetEnumAttribute<FileExtensionAttribute>().Value;

    public static byte[]
    Compress(this ICompress compress, byte[] bytes) {
        var ms =new MemoryStream();
        compress.Compress(new MemoryStream(bytes), ms);
        return ms.ToArray();
    }

    public static byte[]
    Decompress(this ICompress compress, byte[] bytes) {
        var ms =new MemoryStream();
        compress.Decompress(new MemoryStream(bytes), ms);
        return ms.ToArray();
    }

    public static CompressMethods
    CompressMethod(this ICompress compress) => compress.GetAttribute<CompressMethodAttribute>().Value;

    public static string
    FileExtension(this ICompress compress) => compress.CompressMethod().GetEnumAttribute<FileExtensionAttribute>().Value;

    public static string
    FileExtension(this CompressMethods method) => method.GetEnumAttribute<FileExtensionAttribute>().Value;

    public static string
    CompressFile(this ICompress compress, string file) {
        using var fs =file.VerifyFileExistence().OpenFileForRead();
        var outFile = file.AppendFileExtension(compress.FileExtension());
        using var outFs = outFile.OpenFileOverwrite();
        compress.Compress(fs, outFs);
        return outFile;
    }

    private static ICompress
    ResolveCompressor(this CompressMethods m) {
        return m switch {
            CompressMethods.Gzip => new GZipCompress(),
            #if OS_WINDOWS
            CompressMethods.Xz => new XzCompress(),
            #endif
            _ => throw new NotImplementedException()
        };
    }

    public static byte[]
    Compress(this byte[] bytes, CompressMethods method = DefaultCompressMethod) => 
        method.ResolveCompressor().Compress(bytes);
    
    public static byte[]
    Decompress(this byte[] bytes, CompressMethods method = DefaultCompressMethod) => 
        method.ResolveCompressor().Decompress(bytes);

    public static void
    CompressStream(this Stream inStream, Stream outStream, CompressMethods method = DefaultCompressMethod) => 
        method.ResolveCompressor().Compress(inStream, outStream);

    public static Stream
    DecompressStream(this Stream inStream, Stream outStream, CompressMethods method = DefaultCompressMethod) {
        method.ResolveCompressor().Decompress(inStream, outStream);
        return outStream;
    }

    public static string
    CompressFile(this string file, CompressMethods method = DefaultCompressMethod) {
        using var @in = file.OpenFileForRead();
        var outFile = file.AppendFileExtension(DefaultCompressMethodFileExtension);
        using var @out = outFile.OpenFileOverwrite();
        CompressStream(@in, @out, method);
        return outFile;
    }

    public static byte[]
    DecompressBasedOnFilePath(this byte[] bytes, string filePath) {
        if (filePath.TryParseFileCompressMethod(out var method))
            return bytes.Decompress(method);
        return bytes;
    }

    public static bool
    TryParseFileCompressMethod(this string file, out CompressMethods method) {
        method = EnumHelper.GetValues<CompressMethods>()
            .FirstOrDefault(x => file.FileContainsExtension(x.GetEnumAttribute<FileExtensionAttribute>().Value));

        return method !=0;
    }

}
}

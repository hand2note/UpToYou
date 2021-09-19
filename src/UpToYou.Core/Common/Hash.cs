using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;

namespace UpToYou.Core {

internal static class Hash {

    public static string
    GetFileHash(this string file) {
        using var md5 = MD5.Create();
        using var fs = file.OpenFileForRead();
        var hash = md5.ComputeHash(fs);
        return BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant();
    }

    public static string
    GetHash(this Stream @in) {
        using var md5 = MD5.Create();
        var hash = md5.ComputeHash(@in);
        return BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant();
    }

    public static string
    GetHash(this byte[] bytes) => new MemoryStream(bytes).GetHash();

    public static string
    GetTotalFilesHash(this IEnumerable<string> files) {
        var ms = new MemoryStream();
        foreach (var file in files) {
            file.VerifyFileExistence();
            using var fs = file.OpenFileForRead();
            fs.CopyTo(ms);
        }
        ms.Position = 0;
        return ms.GetHash();
    }

    public static Dictionary<string, string>
    ToFilesHashesMap(this IEnumerable<string> files) => files.ToDictionary(x => x,x => x.GetFileHash());

}
}

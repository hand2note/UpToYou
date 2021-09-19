using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UpToYou.Core{
internal static class PackageProjectionHelper {
    public static bool 
    IsPackageDeltaFile(this string file) => file.FileContainsExtension(PackageProjection.DeltaExtension);

    public static bool
    IsUnpackedDeltaFile(this string file) => file.FileHasExtension(PackageProjection.DeltaExtension);

    public static void
    ExtractAllHostedFiles(this IEnumerable<string> hostedFiles, string outDir) {
        foreach (var hostedFile in hostedFiles) 
            hostedFile.ExtractHostedFile(outDir);
    }

    public static void 
    ExtractHostedFile(this string hostedFile, string outDir) {
        using var fs = hostedFile.OpenFileForRead();
        if (hostedFile.TryParseFileCompressMethod(out var method)) {
            var ms = new MemoryStream();
            fs.DecompressStream(ms, method);
            ms.Position = 0;
            ms.ExtractArchive(outDir);
        }
        else 
            fs.ExtractArchive(outDir);
    }
}
}

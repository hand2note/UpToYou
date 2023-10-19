using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using ICSharpCode.SharpZipLib.Tar;

namespace UpToYou.Core;

internal static class Archive {

    public const string ArchiveExtension = ".tar";

    public static void 
    ArchiveFilesIntoStream(this IEnumerable<string> files, string rootDirectory, Stream outStream) {
        using var tar = TarArchive.CreateOutputTarArchive(outStream);
        tar.RootPath = rootDirectory.Replace('\\', '/');

        TarEntry CreateTarEntry(string file) {
            var entry = TarEntry.CreateEntryFromFile(file);
            entry.Name = file.GetPathRelativeTo(rootDirectory).Value;
            return entry;
        }

        foreach (var tarEntry in  files.Select(x => x.VerifyFileExistence()).Select(CreateTarEntry)) 
            tar.WriteEntry(tarEntry, true);

        tar.Close();
    }

    public static void
    ArchiveFilesIntoStream(this IEnumerable<(string file, string archivedEntryName)> inputs, Stream outStream) {
        using var tar = TarArchive.CreateOutputTarArchive(outStream);

        foreach (var input in inputs) {
            var entry = TarEntry.CreateEntryFromFile(input.file);
            entry.Name= input.archivedEntryName;
            tar.WriteEntry(entry, true);
        }

        tar.Close();
    }

    public static string
    ArchiveFiles(this IEnumerable<(string file, string archivedEntryName)> inputs, string outFile) {
        outFile = outFile.AppendFileExtensionIfAbsent(ArchiveExtension);
        using var fs = outFile.CreateFile();
        inputs.ArchiveFilesIntoStream(fs);
        return outFile;
    }   

    public static string
    ArchiveFiles(this IEnumerable<string> files, string baseDirectory, string outFile) {
        outFile = outFile.AppendFileExtensionIfAbsent(ArchiveExtension);
        using var fs = outFile.CreateFile();
        files.ArchiveFilesIntoStream(baseDirectory, fs);
        
        return outFile;
    }

    public static string
    ExtractArchive(this Stream inStream, string outDir) {
        using var tar = TarArchive.CreateInputTarArchive(inputStream: inStream, nameEncoding: Encoding.UTF8);
        tar.ExtractContents(outDir.CreateDirectoryIfAbsent());
        return outDir;
    }
}
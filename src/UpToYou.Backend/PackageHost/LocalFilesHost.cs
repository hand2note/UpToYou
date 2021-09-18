using System.Collections.Generic;
using System.IO;
using System.Linq;
using UpToYou.Core;

namespace UpToYou.Backend {

internal class LocalFilesHost: IFilesHost {
    private readonly string _rootDir;

    public LocalFilesHost(string rootDir) => _rootDir = rootDir;

    public void 
    UploadFile(ProgressContext? progress, RelativePath path, Stream inStream) {
        using var fs =path.ToAbsolute(_rootDir).CreateParentDirectoryIfAbsent().OpenFileOverwrite();
        inStream.CopyTo(fs);
    }

    public string 
    DownloadFile(ProgressContext? progress, RelativePath path, string outFile) => 
        path.ToAbsolute(_rootDir).CopyFile(outFile);

    public byte[] 
    DownloadData(ProgressContext? progress, RelativePath path) => 
        path.ToAbsolute(_rootDir).ReadAllFileBytes();

    public void RemoveFiles(string globPattern) {
        _rootDir.EnumerateDirectoryRelativeFiles().Where(x => x.Value.MatchGlob(globPattern)).ForEach(x => x.ToAbsolute(_rootDir).RemoveFile());
    }

    public List<RelativePath>
    GetAllFiles(string? globPattern) {
        var files = _rootDir.EnumerateAllDirectoryFiles().Select(x => x.GetPathRelativeTo(_rootDir));
        return !string.IsNullOrWhiteSpace(globPattern) 
            ? files.Where(x => x.Value.MatchGlob(globPattern)).ToList() 
            : files.ToList();
    }

    public bool 
    FileExists(RelativePath path) => File.Exists(path.ToAbsolute(_rootDir));
}
}

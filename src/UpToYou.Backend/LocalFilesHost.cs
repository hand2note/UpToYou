using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using UpToYou.Core;

namespace UpToYou.Backend {

internal class LocalHost: IHost {
    private readonly string _rootDir;

    public LocalHost(string rootDir) => _rootDir = rootDir;

    public void 
    UploadFile(RelativePath path, Stream inStream) {
        using var fs =path.ToAbsolute(_rootDir).CreateParentDirectoryIfAbsent().OpenFileOverwrite();
        inStream.CopyTo(fs);
    }

    public void 
    RemoveFiles(string globPattern) =>
        _rootDir.EnumerateDirectoryRelativeFiles()
            .Where(x => x.Value.MatchGlob(globPattern))
            .ForEach(x => x.ToAbsolute(_rootDir).RemoveFile());

    public List<RelativePath>
    GetAllFiles(string globPattern) =>
        _rootDir.GetAllDirectoryFiles().Select(x => x.GetPathRelativeTo(_rootDir)).Where(x => x.Value.MatchGlob(globPattern)).ToList();

    public bool 
    FileExists(RelativePath path) => File.Exists(path.ToAbsolute(_rootDir));

    public void 
    DownloadFile(RelativePath path, IProgress<long> progress, CancellationToken cancellationToken, Stream outStream) {
        using var stream = path.ToAbsolute(_rootDir).OpenFileForRead();
        stream.CopyTo(outStream);
    }
}
}

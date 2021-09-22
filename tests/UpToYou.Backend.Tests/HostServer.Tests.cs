using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using UpToYou.Core;
using UpToYou.Core.Tests;

namespace UpToYou.Backend.Tests {
    
public class 
InMemoryHost:  IHost {
    public Dictionary<RelativePath, byte[]> Files {get;} = new();
    public void 
    DownloadFile(RelativePath path, IProgress<long> progress, CancellationToken cancellationToken, Stream outStream) => new MemoryStream(GetFileBytes(path)).CopyTo(outStream);

    public void 
    RemoveFiles(string globPattern) {
        var filesToRemove = Files.Keys.Where(x => x.Matches(globPattern.ToRelativeGlob()));
        foreach(var file in filesToRemove)
            Files.Remove(file);
    }

    public bool 
    FileExists(RelativePath path) => Files.ContainsKey(path);

    public List<RelativePath> 
    GetAllFiles(string globPattern) => Files.Keys.Where(x => x.Matches(globPattern.ToRelativeGlob())).ToList();

    public void 
    UploadFile(RelativePath path, Stream inStream) {
        var memoryStream = new MemoryStream();
        inStream.CopyTo(memoryStream);
        Files[path] = memoryStream.ToArray();
    }
    
    public byte[]
    GetFileBytes(RelativePath path) => Files.TryGetValue(path, out var result) ? result : throw new InvalidOperationException($"File {path.Value.Quoted()} not found");
}
    
public static class HostServerTests {
    
    public static void 
    AssertFileExists(this IHost host, string relativePath) => 
        host.FileExists(relativePath.ToRelativePath()).AssertTrue($"File {relativePath.Quoted()} is expected to be present on host but wasn't\nFiles on Host:\n{host.GetAllFiles().AggregateToString("\n")}");

}
}

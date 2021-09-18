using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using UpToYou.Core;

namespace UpToYou.Backend {

public interface 
IFilesHost: IFilesHostClient {
    void UploadFile(ProgressContext? progress, RelativePath path, Stream inStream);
    void RemoveFiles(string globPattern);
    List<RelativePath> GetAllFiles(string? globPattern = null);
    bool FileExists(RelativePath path);
}

public static class
FileHostEx {
    public static async Task UploadFileAsync(this IFilesHost host, ProgressContext? pCtx, RelativePath path, Stream inStream) =>
        await Task.Run(() => host.UploadFile(pCtx, path, inStream));

    public static void UploadFile(this IFilesHost host, ProgressContext? pCtx, RelativePath path, string sourceFile) {
        using var fs =sourceFile.OpenFileForRead();
        host.UploadFile(pCtx, path, fs);
    }

    public static void UploadData(this IFilesHost host, ProgressContext? pCtx, RelativePath path, byte[] bytes) {
        host.UploadFile(pCtx, path, new MemoryStream(bytes));
    }

    public static void Remove(this IFilesHost host, RelativePath path) => host.RemoveFiles(path.Value);
}

}

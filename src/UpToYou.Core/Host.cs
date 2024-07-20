using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using ProtoBuf;

namespace UpToYou.Core {

public interface 
IHostClient {
    void DownloadFile(RelativePath path, IProgress<long> progress, CancellationToken cancellationToken, Stream outStream);
}

public class 
NullProgress : IProgress<long> {
    public static NullProgress Instance = new();
    public void Report(long value) {  }
}

public class 
DownloadProgressObserver: IProgress<long> {
    public Action<long> OnDownloadProgress { get; }
    public DownloadProgressObserver(Action<long> onDownloadProgress) => OnDownloadProgress = onDownloadProgress;

    public void Report(long value) => OnDownloadProgress(value);
}

public class 
HttpHostClient: IHostClient, IDisposable {
    public string BaseUri {get;}
    public HttpClient HttpClient {get;}
    public HttpHostClient(string baseUri) {
        BaseUri = baseUri;   
        HttpClient = new HttpClient();
    }

    public void 
    DownloadFile(RelativePath path, IProgress<long> progress, CancellationToken cancellationToken, Stream outStream) =>
        HttpClient.Download(uri: path.ToAbsolute(BaseUri), progress, cancellationToken, outStream);

    public void Dispose() => HttpClient.Dispose();
}

}

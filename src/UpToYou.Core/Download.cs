using System;
using System.Net;
using System.Threading;
using System.Threading.Tasks;

namespace UpToYou.Core
{
    internal static class Download
    {
        public static byte[]
        DownloadData(this string url, ProgressContext? progressContext) {
            Console.WriteLine(url);
            using var webClient = new WebClient();
            
            if (progressContext != null) {
                progressContext.OnProgressStarted(DateTime.Now);
                var progressValueIncrement = new ProgressValueIncrement();
                webClient.DownloadProgressChanged += (s, e) => {
                    progressContext.OnIncrement(DateTime.Now, progressValueIncrement.OnProgressValueUpdate( e.BytesReceived));
                };
            }

            using var downloaded = new ManualResetEvent(false);
            DownloadDataCompletedEventArgs? completedEventArgs = null;
            webClient.DownloadDataCompleted += (s, e) => {
                completedEventArgs = e;
                // ReSharper disable once AccessToDisposedClosure
                downloaded.Set();
            };

            try {
                // ReSharper disable once AccessToDisposedClosure
                Task.Run( () => webClient.DownloadDataAsync(new Uri(url)));
                downloaded.WaitOne();
                return completedEventArgs!.Result;
            }
            catch (Exception ex) {
                throw new InvalidRemoteDataException($"Failed to download data from {url.Quoted()}", ex);
            }
        }

        public static bool 
        TryDownloadData(this string url, ProgressContext? progressContext, out byte[]? result) {
            try {
                result = url.DownloadData(progressContext);
                return true;
            }
            catch (WebException) {
                result = null;
                return false;
            }
        }

        public static string
        DownloadFile(this string url, ProgressContext? progressContext, string outFile) {
            using var webClient = new WebClient();
            if (progressContext != null) {
                progressContext.OnProgressStarted(DateTime.Now);
                var progressValueIncrement = new ProgressValueIncrement();
                webClient.DownloadProgressChanged += (s,e) => progressContext.OnIncrement(DateTime.Now, progressValueIncrement.OnProgressValueUpdate( e.BytesReceived));
            }

            using var downloaded = new ManualResetEvent(false);
            // ReSharper disable once AccessToDisposedClosure
            webClient.DownloadFileCompleted += (s, e) => downloaded.Set();

            try {
                // ReSharper disable once AccessToDisposedClosure
                Task.Run( () => webClient.DownloadFileAsync(new Uri(url), outFile));
                downloaded.WaitOne();
                return outFile;
            }
            catch (Exception ex) {
                throw new InvalidRemoteDataException($"Failed to download data from {url.Quoted()}", ex);
            }
        }   

        public static async Task<string>
        DownloadFileAsync(this string url, ProgressContext? progressContext, string outFile) {
            using var webClient = new WebClient();
            if (progressContext != null) {
                progressContext.OnProgressStarted(DateTime.Now);
                var progressValueIncrement = new ProgressValueIncrement();
                webClient.DownloadProgressChanged += (s,e) => progressContext.OnIncrement(DateTime.Now, progressValueIncrement.OnProgressValueUpdate( e.BytesReceived));
            }
            await webClient.DownloadFileTaskAsync(url, outFile);
            return outFile;
        }
    }

public class DownloadFileHostClient: IFilesHostClient {
    private readonly string _rootUrl;

    public DownloadFileHostClient(string rootUrl) => _rootUrl = rootUrl;

    public string DownloadFile(ProgressContext? progress, RelativePath path, string outFile) => 
        path.ToAbsolute(_rootUrl).DownloadFile(progress, outFile);

    public byte[] DownloadData(ProgressContext? progress, RelativePath path) => 
        path.ToAbsolute(_rootUrl).DownloadData(progress);
}

}

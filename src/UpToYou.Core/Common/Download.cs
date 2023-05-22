using System;
using System.IO;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace UpToYou.Core {
    

public static class 
DownloadHelper {
    
    public static string 
    DownloadFile(this string url, HttpClient client, string outputFile) {
        using var fileStream = File.Create(outputFile);
        client.DownloadAsync(url, progress: NullProgress.Instance, cancellationToken: CancellationToken.None, outStream: fileStream).Wait();
        return outputFile;
    }
    
    public static void 
    Download(this HttpClient client, string uri, IProgress<long> progress, CancellationToken cancellationToken, Stream outStream) =>
        client.DownloadAsync(uri, progress, cancellationToken, outStream).Wait(cancellationToken);

    public static async Task
    DownloadAsync(this HttpClient client, string uri, IProgress<long> progress, CancellationToken cancellationToken, Stream outStream) {
        var response = await client.GetAsyncWithAttempts(uri, cancellationToken).ConfigureAwait(false);
        using var stream =  await response.Content.ReadAsStreamAsync(cancellationToken);
        await stream.CopyToAsync(
            destination: outStream,
            progress, 
            cancellationToken,
            bufferSize: 8192);
    }

    public static Task<HttpResponseMessage>
    GetAsyncWithAttempts(this HttpClient httpClient, string requestUri, CancellationToken cancellationToken) {
        var attempts = 100;
        var attempt = 0;
        while (attempt < attempts) {
            try {
                return httpClient.GetAsync(requestUri: requestUri, cancellationToken: cancellationToken);
            }
            catch {
                Task.Delay(200).Wait();
                attempt++;
            }
        }
        throw new Exception("Please check your internet connection and try again");
    }
    
    public static async Task 
    CopyToAsync(this Stream source, Stream destination, IProgress<long> progress, CancellationToken cancellationToken, int bufferSize) {
        if (source == null)
            throw new ArgumentNullException(nameof(source));
        if (!source.CanRead)
            throw new ArgumentException("Has to be readable", nameof(source));
        if (destination == null)
            throw new ArgumentNullException(nameof(destination));
        if (!destination.CanWrite)
            throw new ArgumentException("Has to be writable", nameof(destination));
        if (bufferSize < 0)
            throw new ArgumentOutOfRangeException(nameof(bufferSize));

        var buffer = new byte[bufferSize];
        long totalBytesRead = 0;
        int bytesRead;
        while ((bytesRead = await source.ReadAsync(buffer, 0, buffer.Length, cancellationToken).ConfigureAwait(false)) != 0) {
            await destination.WriteAsync(buffer, 0, bytesRead, cancellationToken).ConfigureAwait(false);
            totalBytesRead += bytesRead;
            progress?.Report(totalBytesRead);
        }
    }
    
}
}

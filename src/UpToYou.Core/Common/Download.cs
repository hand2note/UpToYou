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
        using var response = await client.GetAsyncWithAttempts(
            requestUri: uri,
            cancellationToken: cancellationToken).ConfigureAwait(continueOnCapturedContext: false);
        response.EnsureSuccessStatusCode();
        using var stream = await response.Content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(continueOnCapturedContext: false);
        await stream.CopyToAsync(
            destination: outStream,
            progress: progress,
            cancellationToken: cancellationToken,
            bufferSize: 8192).ConfigureAwait(continueOnCapturedContext: false);
    }

    public static async Task<HttpResponseMessage>
    GetAsyncWithAttempts(
        this HttpClient httpClient,
        string requestUri,
        CancellationToken cancellationToken) {
        //Edited by Gemini 4 Neon (https://github.com/hand2note/Hand2Note4.1.Issues/issues/3101): Await GetAsync so transient transport errors are retried without blocking or swallowing the final exception.
        const int maxAttempts = 5;
        const int retryDelayMs = 200;
        var attempt = 0;
        while (true) {
            try {
                return await httpClient.GetAsync(
                    requestUri: requestUri,
                    completionOption: HttpCompletionOption.ResponseHeadersRead,
                    cancellationToken: cancellationToken).ConfigureAwait(continueOnCapturedContext: false);
            }
            catch (Exception exception) when (
                exception is HttpRequestException or IOException or TaskCanceledException &&
                !cancellationToken.IsCancellationRequested &&
                attempt + 1 < maxAttempts) {
                await Task.Delay(
                    millisecondsDelay: retryDelayMs,
                    cancellationToken: cancellationToken).ConfigureAwait(continueOnCapturedContext: false);
                attempt++;
            }
        }
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
            progress?.Report(bytesRead);
        }
    }
    
}
}

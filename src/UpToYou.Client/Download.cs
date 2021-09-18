using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using UpToYou.Core;

namespace UpToYou.Client {

public class
DownloadUpdateContext {
    public string OutputDirectory { get; }
    public ProgressContext? ProgressContext { get; }
    public IProgressOperationObserver? ProgressOperationObserver { get; }
    public PackageHostClientContext Host { get; }
    public CancellationToken? CancellationToken { get; }
    public bool IsCancelled => CancellationToken?.IsCancellationRequested ?? false;

    public DownloadUpdateContext(string outputDirectory, ProgressContext? progressContext, PackageHostClientContext host, CancellationToken? cancellationToken = null, IProgressOperationObserver? progressOperationObserver = null) {
        OutputDirectory = outputDirectory;
        ProgressContext = progressContext;
        Host = host;
        ProgressOperationObserver = progressOperationObserver;
        CancellationToken =cancellationToken;
    }
}

public static class Download {

//public static string
//DownloadUpdateFiles(this PackageDifference difference, string outputDirectory, PackageHostContext host, ProgressContext? progress = null) =>
//    difference.DownloadUpdateFiles(new DownloadUpdateContext(outputDirectory, progress, host));

public static TimeSpan RelevantDownloadSpeedTimeSpan = TimeSpan.FromSeconds(15);

internal static string
DownloadUpdateFiles(this PackageDifference difference, DownloadUpdateContext ctx) {
    if (!difference.IsDifferent()) throw new InvalidOperationException("ActualState doesn't differ from the package");

    var projection = difference.Package.DownloadProjection(ctx.Host) ?? 
        throw new InvalidRemoteDataException($"Failed to download PackageProjection for package {difference.Package.Metadata.Version}");

    var hostedFileToDownload = difference.GetFilesToDownload(projection).ToList().GetSmallestHostedFilesSet().ToList();
    ctx.ProgressContext?.OnExtraTargetValue(hostedFileToDownload.Sum(x =>x.FileSize));
    var downloadedFiles = hostedFileToDownload.DownloadAll(ctx.Host,ctx.OutputDirectory).ToList();
    ctx.ProgressOperationObserver?.OnOperationChanged(Resources.ExtractngUpdateFilesDotted);
    downloadedFiles.ExtractAllHostedFiles(ctx.OutputDirectory);
    return ctx.OutputDirectory; }

private static HashSet<HostedFile>
GetSmallestHostedFilesSet(this List<List<HostedFile>> itemsFiles) {
    //Ideally here should be a recursive algorithm to find the smallest possible set of hosted files. But it is too complicated for now.
    //So here is the simple one
    var result = new HashSet<HostedFile>(itemsFiles.Where(x => x.Count == 1).Select(x => x[0]).Distinct());

    foreach (var itemFiles in itemsFiles.Where(x => x.Count > 1)) {
        if (result.ContainsAny(itemFiles))
            continue;
        result.Add(itemFiles.SmallestFile()); }
    return result; }

private static HostedFile
SmallestFile(this IEnumerable<HostedFile> hostedFiles) => hostedFiles.MinBy(x => x.FileSize);

private static IEnumerable<List<HostedFile>>
GetFilesToDownload(this PackageDifference difference, PackageProjection projection) {
    //Probably will be faster if cache relevant hosted files in advance
    var relevantHostedFiles = projection.HostedFiles.Where(x => x.RelevantItemsIds.ContainsAny(difference.DifferentFilesIds)).ToList();

    foreach (var fileDifference in difference.DifferentFiles) {
        var hostedFiles = !fileDifference.ActualFileState.Exists
            ? FindFullHostedFile(fileDifference.PackageItemId, relevantHostedFiles).ToList()
            : FindDeltaHostedFile(fileDifference, relevantHostedFiles) .Union(FindFullHostedFile(fileDifference.PackageItemId, relevantHostedFiles)).ToList();
        if (hostedFiles.Count == 0)
            throw new InvalidRemoteDataException($"Failed to find hosted file containing an updated version of {fileDifference.PackageFile.Path}");
        yield return hostedFiles; } }

private static IEnumerable<HostedFile>
FindFullHostedFile(string packageItemId, IEnumerable<HostedFile> relevantHostedFiles) {
    foreach (var hostedFile in relevantHostedFiles)
        if (hostedFile.Content is PackageItemsHostedFileContent content)
            if (content.PackageItems.Contains(packageItemId))
                yield return hostedFile; }

private static IEnumerable<HostedFile>
FindDeltaHostedFile(PackageFileDifference fileDifference, IEnumerable<HostedFile> relevantHostedFiles) {
    foreach (var hostedFile in relevantHostedFiles) {
        if (hostedFile.Content is PackageFileDeltasHostedFileContent deltaContent) {
            var delta = deltaContent.PackageFileDeltas.FirstOrDefault(x => 
                x.PackageItemId == fileDifference.PackageItemId && 
                x.OldHash == fileDifference.ActualFileState.Hash && 
                x.NewHash == fileDifference.PackageFile.FileHash);

            if (delta != null)
                yield return hostedFile; } } }


}
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using CommandLine;
using CommandLine.Text;
using UpToYou.Core;

namespace UpToYou.Backend.Runner {

[Verb("GetUpdateNotes", HelpText = "Retrieves notes for the specific update and locale.")]
public class GetUpdateNotesOptions: IFilesHostOptions {
#pragma warning disable CS8618 // Non-nullable field is uninitialized.
    //public GetUpdateNotesOptions() { }
#pragma warning restore CS8618 // Non-nullable field is uninitialized.

    public GetUpdateNotesOptions(string version, string? packageName, string locale, string? azureConnectionString, string? azureRootContainer, string? filesHostType, string? localHostRootPath) {
       
        Version = version;
        PackageName = packageName;
        Locale = locale;
        AzureConnectionString = azureConnectionString;
        AzureRootContainer = azureRootContainer;
        FilesHostType = filesHostType;
        LocalHostRootPath = localHostRootPath;
    }

    //[Option(HelpText = "Path to the update notes file", Required = true)]
    //[Value(0)]
    //public string UpdatesFile { get; set; }

    [Option(HelpText = "Version of the desired update", Required = true)]
    public string Version { get; set;}

    [Option(HelpText = "Name of the package of the desired update.", Required = false)]
    public string? PackageName { get; set;}

    [Option(HelpText = "Locale of the desired update notes.")]
    public string? Locale { get; set; }

    [Option]
    public string? AzureConnectionString { get; set; }
    [Option]
    public string? AzureRootContainer {get;set;}

    [Option]
    public string? FilesHostType { get;set;}

    [Option]
    public string? LocalHostRootPath {get;set;}
}

public static class GetUpdateNotesModule {

    public static string? 
    GetUpdateNotes(this GetUpdateNotesOptions options) {
        var filesHost = options.GetFilesHost();
        var version = options.Version.ParseVersion();
        var updateNotes = filesHost.DownloadUpdateNotes(options.PackageName, options.Locale).ParseUpdateNotes();
        return updateNotes.TryGet(x => x.version == version, out var res) ? res.notes : null;
    } 

}
}

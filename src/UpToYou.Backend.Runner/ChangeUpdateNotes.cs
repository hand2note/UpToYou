using System;
using CommandLine;
using UpToYou.Core;

namespace UpToYou.Backend.Runner {
public class ChangeUpdateNotesOptions {
    public ChangeUpdateNotesOptions(string packageVersion, string packageName, string notes) {
        PackageVersion = packageVersion;
        PackageName = packageName;
        Notes = notes;
    }

    [Value(0), Option(Required = true)]
    public string PackageVersion {get;}

    [Option]
    public string? PackageName {get;}

    [Option(Required = true)]
    public string Notes {get;}
}

public static class ChangeUpdateNotesModule {
    public static void 
    ChangeUpdateNotes(this ChangeUpdateNotesOptions options) {
        throw new NotImplementedException();
        //ChangeUpdateHelper.ChangeUpdate(options.PackageVersion.ParseVersion(), options.PackageName, x => {
        //    //x.UpdateNotes = options.Notes;
        //    return x;
        //});
    }
}
}

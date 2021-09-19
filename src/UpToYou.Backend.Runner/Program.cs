using System;
using System.IO;
using CommandLine;
using Microsoft.Extensions.Logging;

namespace UpToYou.Backend.Runner
{

class Program {

    static void Main(string[] args) {

        try {
            var parser = new Parser(x => {
                x.CaseSensitive = false;
                x.HelpWriter = Console.Error;
            });
            var result = parser.ParseArguments<PushUpdateOptions, RemovePackageOptions, ChangeUpdateNotesOptions, GetUpdateNotesOptions, CopyPackageFilesOptions>(args)
                .WithParsed<PushUpdateOptions>(x => x.PushUpdate())
                .WithParsed<CopyPackageFilesOptions>(x => x.CopyPackageFiles())
                .WithParsed<GetUpdateNotesOptions>(x => x.GetUpdateNotes())
                .WithParsed<ChangeUpdateNotesOptions>(x => x.ChangeUpdateNotes())
                .WithParsed<RemovePackageOptions>(x => x.RemovePackage());

            if (result.Tag == ParserResultType.NotParsed)
                throw new InvalidOperationException($"Failed to parse command arguments");
        } catch(Exception exception) {
            new ConsoleLogger().LogException(LogLevel.Critical, "Failed to execute", exception);
        }
    }
}
}

using System;
using Newtonsoft.Json;
using UpToYou.Core;

namespace UpToYou.Backend.Runner {

internal class Logger: IUpdaterLogger {
    public void Log(UpdaterLogLevels level, string msg) {
        switch(level) {
            case UpdaterLogLevels.Debug:
                Console.ForegroundColor = ConsoleColor.Gray;
                break;

            case UpdaterLogLevels.Trace:
                Console.ForegroundColor = ConsoleColor.DarkGray;
                break;

            case UpdaterLogLevels.Info:
                Console.ForegroundColor = ConsoleColor.White;
                break;

            case UpdaterLogLevels.Warning:
                Console.ForegroundColor = ConsoleColor.Yellow;
                break;

            case UpdaterLogLevels.Error:
            case UpdaterLogLevels.Fatal:
                Console.ForegroundColor = ConsoleColor.Red;
                break;

            default:
                Console.ForegroundColor = ConsoleColor.White;
                break;
        }

        Console.WriteLine(msg);
        Console.ForegroundColor = ConsoleColor.White;

    }

    public void LogException(UpdaterLogLevels level, string msg, Exception ex) {
        Console.ForegroundColor = ConsoleColor.Red;
        Console.WriteLine(msg);
        Console.WriteLine(ex.ToString());
        Console.Error.WriteLine(msg);
        Console.Error.WriteLine(ex.ToString());
        Console.ForegroundColor = ConsoleColor.White;
    }

    public void LogObject(UpdaterLogLevels level, string name, object obj) {
        Console.WriteLine(name + ":");
        Console.ForegroundColor = ConsoleColor.DarkGray;
        Console.WriteLine(JsonConvert.SerializeObject(obj, Formatting.Indented));
        Console.ForegroundColor = ConsoleColor.White;
    }
}

}
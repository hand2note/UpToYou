using System;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using UpToYou.Core;

namespace UpToYou.Backend.Runner {

internal class ConsoleLogger: ILogger {
    public void Log(LogLevel level, string msg) {
        switch(level) {
            case LogLevel.Debug:
                Console.ForegroundColor = ConsoleColor.Gray;
                break;

            case LogLevel.Trace:
                Console.ForegroundColor = ConsoleColor.DarkGray;
                break;

            case LogLevel.Information:
                Console.ForegroundColor = ConsoleColor.White;
                break;

            case LogLevel.Warning:
                Console.ForegroundColor = ConsoleColor.Yellow;
                break;

            case LogLevel.Error:
            case LogLevel.Critical:
                Console.ForegroundColor = ConsoleColor.Red;
                break;

            default:
                Console.ForegroundColor = ConsoleColor.White;
                break;
        }

        Console.WriteLine(msg);
        Console.ForegroundColor = ConsoleColor.White;

    }

    public void LogException(LogLevel level, string msg, Exception ex) {
        Console.ForegroundColor = ConsoleColor.Red;
        Console.WriteLine(msg);
        Console.WriteLine(ex.ToString());
        Console.Error.WriteLine(msg);
        Console.Error.WriteLine(ex.ToString());
        Console.ForegroundColor = ConsoleColor.White;
    }

    public void LogObject(LogLevel level, string name, object obj) {
        Console.WriteLine(name + ":");
        Console.ForegroundColor = ConsoleColor.DarkGray;
        Console.WriteLine(JsonConvert.SerializeObject(obj, Formatting.Indented));
        Console.ForegroundColor = ConsoleColor.White;
    }

    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception, string> formatter) {
        if (exception != null) 
            LogException(logLevel, state as string, exception);
        else 
            Log(logLevel, state?.ToString()!);
    }

    public bool IsEnabled(LogLevel logLevel) => true;

    public IDisposable BeginScope<TState>(TState state) {
        throw new NotImplementedException();
    }
}

}
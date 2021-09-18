using System;

namespace UpToYou.Core
{

public enum UpdaterLogLevels {
    Trace = 0,
    Debug = 1,
    Info = 2,
    Warning = 3,
    Error = 4,
    Fatal = 5,
    None = 6
}

    public interface IUpdaterLogger
    {
        void Log(UpdaterLogLevels level, string msg);
        void LogException(UpdaterLogLevels level, string msg, Exception ex);
        void LogObject(UpdaterLogLevels level, string name, Object obj);
    }

internal static class LoggerModule {

    public static void LogDebug(this IUpdaterLogger logger, string msg) => 
        logger.Log(UpdaterLogLevels.Debug, msg);

    public static void LogInfo(this IUpdaterLogger logger, string msg) =>
        logger.Log(UpdaterLogLevels.Info, msg);

    public static void LogWarning(this IUpdaterLogger logger, string msg) =>
        logger.Log(UpdaterLogLevels.Warning, msg);

    public static void LogError(this IUpdaterLogger logger, string msg) =>
        logger.Log(UpdaterLogLevels.Error, msg);

    public static void LogException(this IUpdaterLogger logger, string msg, Exception ex) =>
        logger.LogException(UpdaterLogLevels.Error, msg, ex);

}

}

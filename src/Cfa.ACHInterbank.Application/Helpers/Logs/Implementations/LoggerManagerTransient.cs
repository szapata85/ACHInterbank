using Cfa.ACHInterbank.Application.Helpers.Logs.Interfaces;
using NLog;

namespace Cfa.ACHInterbank.Application.Helpers.Logs.Implementations;

public class LoggerManagerTransient : ILoggerManagerTransient
{
    Logger log = LogManager.GetLogger("LogFile");

    public void LogError(string ErrorMessage)
    {
        log.Error($"Error: {ErrorMessage}");
    }

    public void LogFatal(string ErrorMessage)
    {
        log.Fatal($"Fatal: {ErrorMessage}");
    }

    public void LogInfo(string InfoMessage)
    {
        log.Info($"Info: {InfoMessage}");
    }

    public void LogTrace(string ErrorData)
    {
        log.Trace($"Trace: {ErrorData}");
    }

    public void LogWarn(string ErrorMessage)
    {
        log.Warn($"Warn: {ErrorMessage}");
    }
}

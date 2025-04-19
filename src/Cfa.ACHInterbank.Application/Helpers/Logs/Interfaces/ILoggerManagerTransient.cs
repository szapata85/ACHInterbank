namespace Cfa.ACHInterbank.Application.Helpers.Logs.Interfaces;

public interface ILoggerManagerTransient
{
    void LogError(string ErrorMessage);
    void LogInfo(string ErrorMessage);
    void LogTrace(string ErrorMessage);
    void LogWarn(string ErrorMessage);
    void LogFatal(string ErrorMessage);
}

namespace Cfa.ACHInterbank.Application.Helpers.Logs.Interfaces;

public interface ILoggerManager
{
    void LogError(string ErrorMessage);
    void LogInfo(string ErrorMessage);
    void LogTrace(string ErrorMessage);
    void LogWarn(string ErrorMessage);
    void LogFatal(string ErrorMessage);
}

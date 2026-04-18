namespace Cfa.ACHInterbank.Application.ACH.Models;

public sealed class NachaConfigException : Exception
{
    public NachaConfigException(string errorCode, string message, int httpStatusCode, string? currentRowVersion = null)
        : base(message)
    {
        ErrorCode = errorCode;
        HttpStatusCode = httpStatusCode;
        CurrentRowVersion = currentRowVersion;
    }

    public string ErrorCode { get; }
    public int HttpStatusCode { get; }
    public string? CurrentRowVersion { get; }
}

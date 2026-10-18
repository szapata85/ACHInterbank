namespace Cfa.ACHInterbank.Application.Security.Exceptions;

public sealed class SoapIntegrationSettingsRevisionConflictException : Exception
{
    public SoapIntegrationSettingsRevisionConflictException(string expectedRevision, string currentRevision)
        : base("SOAP_SETTINGS_REVISION_CONFLICT")
    {
        ExpectedRevision = expectedRevision;
        CurrentRevision = currentRevision;
    }

    public string ExpectedRevision { get; }
    public string CurrentRevision { get; }
}

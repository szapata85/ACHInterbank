namespace Cfa.ACHInterbank.External.Connections;

public static class ExternalHttpClientNames
{
    public const string HealthProbe = "ach-health-probe-idempotent";
    public const string GenericNoRetry = "ach-generic-http-no-retry";
    public const string WscfaachSoapNoRetry = "ach-wscfaach-soap-no-retry";
    public const string WsAxonSoapNoRetry = "ach-wsaxon-soap-no-retry";
}

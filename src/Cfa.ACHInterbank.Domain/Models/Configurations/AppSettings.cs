using Cfa.ACHInterbank.Domain.Entities.Token;

namespace Cfa.ACHInterbank.Domain.Models.Configurations;

public class AppSettings
{
    private static AppSettings? settings;
    public static AppSettings Settings { get => settings!; set => settings = value; }
    public string? prueba { get; set; }
    public string? address { get; set; }
    public string? key { get; set; }
    public SecurityHeadersOptions? HeadersSecurity { get; set; }
    public Token? TokenManager { get; set; }
    public ServesSettings? Servers { set; get; }
    public TokenGeneric? TokenGeneric { set; get; }
    public Integrations? Integrations { set; get; }
}

public class Token
{
    public string? secretKetJwt { get; set; }
    public string? issuerJwt { get; set; }
    public string? audienceJwt { get; set; }
    public string? expirationType { get; set; }
    public int accessExpiration { get; set; }
    public string? clientId { get; set; }
    public string? clientSecret { get; set; }
    public string? x_api_key { get; set; }
}


public class SecurityHeadersOptions
{
    public string? XFrameOptions { get; set; }
    public string? ContentSecurityPolicy { get; set; }
    public string? ReferrerPolicy { get; set; }
}

public class ServesSettings
{
    public List<ServicesIntegration>? services { set; get; }
    public List<ServicesIntegration>? servicesWCF { set; get; }
}

public class ServicesIntegration
{
    public string? Name { set; get; }
    public string? Url { set; get; }
    public bool State { get; set; }
    public bool IsHealthy { get; set; } = true;
    public DateTime LastHealthCheck { get; set; } = DateTime.MinValue;
    public int FailedChecks { get; set; } = 0;
    public int ActiveConnections { get; set; } = 0;
    public int Timeout { get; set; } = 0;
}


public class TokenGeneric
{
    public string? clientId { get; set; }

    public string? clientSecret { get; set; }
    public string? x_api_key { get; set; }
    public string? scope { get; set; }
    public string? grant_type { get; set; }
    public string? client_assertion_type { get; set; }
}

public class Integrations
{
    public string? UrlAch { get; set; }
    public List<Methods>? Methods { get; set; }
}

public class Methods
{
    public string? Name { get; set; }
    public string? Value { get; set; }
}

public enum TypeBody
{ Body, Query }

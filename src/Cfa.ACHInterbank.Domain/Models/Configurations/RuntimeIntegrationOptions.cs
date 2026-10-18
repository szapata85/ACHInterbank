namespace Cfa.ACHInterbank.Domain.Models.Configurations;

/// <summary>Legacy application encryption key projected from appSettings:key.</summary>
public sealed class ApplicationEncryptionOptions
{
    public string? Key { get; set; }
}

/// <summary>Authentication UI settings projected from appSettings:address.</summary>
public sealed class AuthenticationUiOptions
{
    public string ResetPasswordBaseUrl { get; set; } = "https://localhost:4200";
}

/// <summary>Legacy WCF authorization timeout projected from appSettings:prueba.</summary>
public sealed class WcfAuthorizationOptions
{
    public string? TimeoutMinutes { get; set; }
}

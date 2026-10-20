namespace Cfa.ACHInterbank.Application.Security.Interfaces;

/// <summary>
/// Exposes the current caller authorization context without coupling Application to HTTP.
/// </summary>
public interface ICurrentUserContext
{
    IReadOnlyCollection<string> Roles { get; }
    IReadOnlyCollection<string> Permissions { get; }
}

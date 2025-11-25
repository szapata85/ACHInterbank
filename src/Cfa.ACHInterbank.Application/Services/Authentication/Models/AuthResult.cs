namespace Cfa.ACHInterbank.Application.Services.Authentication.Models;

public class AuthResult
{
    public bool Success { get; set; }
    public string? Message { get; set; }
    public string? Token { get; set; }
    public DateTimeOffset ExpiresAt { get; set; }
    public string? Username { get; set; }
    public string? FullName { get; set; }
    public IEnumerable<string> Roles { get; set; } = Enumerable.Empty<string>();
    public IEnumerable<string> Permissions { get; set; } = Enumerable.Empty<string>();
}

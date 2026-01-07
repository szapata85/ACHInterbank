namespace Cfa.ACHInterbank.Application.Services.Authentication.Models;

public class LoginRequest
{
    public string? Username { get; set; }
    public string? Password { get; set; }
    public int? MaxFailedAttempts { get; set; }
    public int? LockoutMinutes { get; set; }
}

namespace Cfa.ACHInterbank.Application.Services.ClientAssertion.Model;

public class TokenClientAssertion
{
    public string? access_token { get; set; }
    public string? token_type { get; set; }
    public double? expires_in { get; set; }
    public string? refresh_token { get; set; }
    public string? scope { get; set; }
}

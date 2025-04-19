namespace Cfa.ACHInterbank.Application.Services.ClientAssertion.Model;

public class SendClientAssertion
{
    public string? grant_type { get; set; }
    public string? client_assertion_type { get; set; }
    public string? client_assertion { get; set; }
    public string? scope { get; set; }
}

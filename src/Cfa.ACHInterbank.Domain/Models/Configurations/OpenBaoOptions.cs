namespace Cfa.ACHInterbank.Domain.Models.Configurations;

public class OpenBaoOptions
{
    public const string SectionName = "DigitalEnvelope:OpenBao";

    public bool Enabled { get; set; } = false;
    public string BaseUrl { get; set; } = "http://openbao:8200";
    public string KvMount { get; set; } = "secret";
    public string CertificatesPrefix { get; set; } = "certificates";
    public string ApiToken { get; set; } = string.Empty;
    public int TimeoutSeconds { get; set; } = 10;
}

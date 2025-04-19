using System.Text.Json.Serialization;

namespace Cfa.ACHInterbank.Domain.Entities.Token;

public class TokenCache
{
    public double? expires { get; set; }
    public string? access_token { get; set; }
    public string? token_type { get; set; }
    public string? client_id { get; set; }
    [JsonIgnore]
    public DateTime? expires_in { get; set; }
}

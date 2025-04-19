using System.Text.Json.Serialization;

namespace Cfa.ACHInterbank.Application.Services.TokenClient.Model;

public class GetTokenResponseModel
{
    public double? expires_in { get; set; }
    public string? access_token { get; set; }
    public string? token_type { get; set; }
    [JsonIgnore]
    public DateTime? expires { get; set; }

}

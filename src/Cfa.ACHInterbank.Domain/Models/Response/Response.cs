using System.Text.Json.Serialization;

namespace Cfa.OpenData.Domain.Models.Response;

public class Response<T>
{
    public T? Result { set; get; }
    public MessageErrors? Errors { get; set; }
    public bool Success { get; set; }
}

public class Error
{
    public string? Code { get; set; }
    public string? Message { get; set; }
}

public class Errors
{
    [JsonPropertyName("error")]
    public MessageErrors? Error { get; set; }
}
public class MessageErrors
{
    [JsonPropertyName("error")]
    public string? Code { get; set; }

    [JsonPropertyName("error_description")]
    public string? Message { get; set; }
}


namespace Cfa.ACHInterbank.Domain.Models.Configurations;

public class BaseResponseModel
{
    public int StatusCode { get; set; }
    public bool Sucess { get; set; }
    public string? Message { get; set; }
    public dynamic? Data { get; set; }
}

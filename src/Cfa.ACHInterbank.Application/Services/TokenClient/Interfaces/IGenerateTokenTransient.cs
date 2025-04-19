using Cfa.ACHInterbank.Application.Services.TokenClient.Model;

namespace Cfa.ACHInterbank.Application.Services.TokenClient.Interfaces;

public interface IGenerateTokenTransient
{
    Task<GetTokenResponseModel> GenerateTokenAsync(TokenModelClient model);
    Task<GetTokenResponseModel> GenerateTokenAsync(string Assertion);
}

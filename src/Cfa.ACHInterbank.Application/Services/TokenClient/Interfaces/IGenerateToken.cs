using Cfa.ACHInterbank.Application.Services.TokenClient.Model;

namespace Cfa.ACHInterbank.Application.Services.TokenClient.Interfaces;

public interface IGenerateToken
{
    Task<GetTokenResponseModel> GenerateTokenAsync(TokenModelClient model);
    Task<GetTokenResponseModel> GenerateTokenAsync(string Assertion);
}

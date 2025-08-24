using Cfa.ACHInterbank.Application.Services.TokenClient.Model;

namespace Cfa.ACHInterbank.Application.External.TokenClient;

public interface IGenerateTokenExternal
{
    GetTokenResponseModel GenerateToken(TokenModelClient model);
}

using Cfa.ACHInterbank.Domain.Entities.Token;

namespace Cfa.ACHInterbank.Application.CacheMemory.Tokens.Interfaces;

public interface ITokenRepositorySingleton
{
    void AddToken(TokenCache user);
    TokenCache GetToken(string client_id);
    List<TokenCache> ListToken();
}

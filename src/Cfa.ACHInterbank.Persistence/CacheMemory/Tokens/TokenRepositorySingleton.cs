using Cfa.ACHInterbank.Application.CacheMemory.Tokens.Interfaces;
using Cfa.ACHInterbank.Domain.Entities.Token;

namespace Cfa.ACHInterbank.Persistence.CacheMemory.Tokens;

public class TokenRepositorySingleton : ITokenRepositorySingleton
{
    private readonly Dictionary<string, TokenCache> _tokens = new();
    public void AddToken(TokenCache token)
    {
        if (!_tokens.ContainsKey(token.client_id!))
            _tokens[token.client_id!] = token;
    }

    public TokenCache GetToken(string client_id)
    {
        if (_tokens.TryGetValue(client_id, out var token))
        {
            if (token!.expires_in < DateTime.UtcNow)
            {
                _tokens.Remove(client_id);
                return null!;

            }
            return token!;
        }

        return null!;
    }

    public List<TokenCache> ListToken()
    {
        return _tokens.Values/*Where(c => c.expires_in < DateTime.Now)*/.ToList();
    }
}

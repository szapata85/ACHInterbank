using Cfa.ACHInterbank.Application.CacheMemory.Keys.Interfaces;
using Cfa.ACHInterbank.Domain.Entities.JwksService;
using static Cfa.ACHInterbank.Domain.Entities.JwksService.JwksService;

namespace Cfa.ACHInterbank.Persistence.CacheMemory.Keys;

public class KeysRepositorySingleton : IKeysRepositorySingleton
{
    private readonly Dictionary<string, JwksService.Key> _keys = new();
    public void AddKey(JwksService.Key key)
    {
        if (!_keys.ContainsKey(key.kid!))
            _keys[key.kid!] = key;
    }

    public JwksService.Key GetKey(string IdPublicKey)
    {
        if (_keys.TryGetValue(IdPublicKey, out var key))
        {
            if (key!.Expire < DateTime.Now)
            {
                _keys.Remove(key.kid!);
                return null!;

            }
            return key!;
        }

        return null!;
    }

    public List<Key> GetKeyList()
    {
        var keys = new List<Key>();
        foreach (var key in _keys.Values.ToList())
        {
            if (RemoveKey(key))
                keys.Add(key);
        }

        return keys;
    }

    private bool RemoveKey(Key key)
    {
        if (key!.Expire < DateTime.Now)
        {
            _keys.Remove(key.kid!);
            return false;
        }
        return true;
    }
}

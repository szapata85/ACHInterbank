using static Cfa.ACHInterbank.Domain.Entities.JwksService.JwksService;

namespace Cfa.ACHInterbank.Application.CacheMemory.Keys.Interfaces;

public interface IKeysRepository
{
    void AddKey(Key key);
    Key GetKey(string client_id);
    List<Key> GetKeyList();
}

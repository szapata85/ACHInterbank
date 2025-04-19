using Cfa.ACHInterbank.Application.CacheMemory.Users.Interfaces;
using Cfa.ACHInterbank.Application.External.Connections;
using Cfa.ACHInterbank.Application.Helpers.ConvertDataSend;
using Cfa.ACHInterbank.Application.Services.EncryptionService.Interfaces;
using Cfa.ACHInterbank.Application.Services.Transaction.Interfaces;
using Cfa.ACHInterbank.Domain.Entities.User;
using Cfa.ACHInterbank.Domain.Models.Configurations;
using Microsoft.AspNetCore.Http;

namespace Cfa.ACHInterbank.Application.Services.Transaction;

public class TestTransient : IDisposable, ITestTransient
{

    #region Diposable

    private bool disposed = false;

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    private void Dispose(bool disposing)
    {
        if (!disposed)
        {
            if (disposing)
            {

            }
            disposed = true;
        }
    }

    // Destructor
    ~TestTransient()
    {
        Dispose(false);
    }

    #endregion Diposable

    private readonly AppSettings _appSettings = AppSettings.Settings;
    private readonly IConnectionAuthScoped _connection;
    private readonly IEncryptionServiceScoped _encryption;
    private readonly IUserRepositorySingleton _userRepository;

    public TestTransient(IConnectionAuthScoped connection, IEncryptionServiceScoped encryption, IUserRepositorySingleton userRepository)
    {
        _connection = connection;
        _encryption = encryption;
        _userRepository = userRepository;
    }

    public void Get(HttpRequest request, string Prueba)
    {
        var ap = _encryption.Encrypt(Prueba);
        var users = new User { Id = "AA1", Name = "Prueba Usuario En Memoria 1" };
        var users2 = new User { Id = "AA2", Name = "Prueba Usuario En Memoria 2" };
        var users3 = new User { Id = "AA3", Name = "Prueba Usuario En Memoria 3" };
        var users4 = new User { Id = "AA4", Name = "Prueba Usuario En Memoria 4" };
        _userRepository.AddUser(users);
        _userRepository.AddUser(users2);
        _userRepository.AddUser(users3);
        _userRepository.AddUser(users4);
        var a = _appSettings.prueba;
        _connection.StartWCF(a);
        var data = ConvertDataSend.DataSend<Token>(request);
        var u = _userRepository.GetUser(users.Id).Name;
        var ulst = _userRepository.ListUsers();
        ap = _encryption.Decrypt(ap);
        throw new NotImplementedException();
    }
}

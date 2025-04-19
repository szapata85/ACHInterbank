using Cfa.ACHInterbank.Application.CacheMemory.Keys.Interfaces;
using Cfa.ACHInterbank.Application.External.JwksService;
using Cfa.ACHInterbank.Application.Features;
using Cfa.OpenData.Domain.Models.Response;
using static Cfa.ACHInterbank.Domain.Entities.JwksService.JwksService;

namespace Cfa.ACHInterbank.External.JwksService;

public class JwksServiceScoped : IDisposable, IJwksServiceScoped
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
    ~JwksServiceScoped()
    {
        Dispose(false);
    }

    #endregion Diposable

    private readonly IKeysRepositorySingleton _keysRepository;

    public JwksServiceScoped(IKeysRepositorySingleton keysRepository)
    {
        _keysRepository = keysRepository;
    }

    public Response<Keys> GetPublicJwks()
    {
        var response = new Response<Keys>();
        try
        {
            var key = new Keys { keys = _keysRepository.GetKeyList() };
            if (key is null)
                throw new Exception("No se encontraron llaves públicas.");
            response = ExtensionMethods.SuccessResponse<Keys>(key);
        }
        catch (Exception ex)
        {
            response = ExtensionMethods.ErrorResponse<Keys>(ex.Message);
        }
        return response;
    }
}


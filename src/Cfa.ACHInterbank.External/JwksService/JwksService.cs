using Cfa.ACHInterbank.Application.CacheMemory.Keys.Interfaces;
using Cfa.ACHInterbank.Application.External.JwksService;
using Cfa.ACHInterbank.Application.Features;
using Cfa.ACHInterbank.Application.Helpers.Logs.Interfaces;
using Cfa.ACHInterbank.Domain.Models.Configurations;
using Cfa.OpenData.Domain.Models.Response;
using static Cfa.ACHInterbank.Domain.Entities.JwksService.JwksService;

namespace Cfa.ACHInterbank.External.JwksService;

[Scoped]
public class JwksService : IDisposable, IJwksService
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
    ~JwksService()
    {
        Dispose(false);
    }

    #endregion Diposable

    private readonly IKeysRepository _keysRepository;
    private readonly ILoggerManager _loggerManager;

    public JwksService(IKeysRepository keysRepository, ILoggerManager loggerManager)
    {
        _keysRepository = keysRepository;
        _loggerManager = loggerManager;
    }

    public Response<Keys> GetPublicJwks()
    {
        var response = new Response<Keys>();
        try
        {
            var key = new Keys { keys = _keysRepository.GetKeyList() };
            if (key is null)
                throw new Exception("No se encontraron llaves públicas.");
            response = ExtensionMethods.SuccessResponse<Keys>(key, _loggerManager);
        }
        catch (Exception ex)
        {
            response = ExtensionMethods.ErrorResponse<Keys>(ex.Message, _loggerManager);
        }
        return response;
    }
}


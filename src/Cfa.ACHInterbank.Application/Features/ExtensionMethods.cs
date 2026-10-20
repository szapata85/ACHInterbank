using System.Text.Json;
using Cfa.ACHInterbank.Application.Helpers.Logs.Interfaces;
using Cfa.OpenData.Domain.Models.Response;

namespace Cfa.ACHInterbank.Application.Features;

public static class ExtensionMethods
{
    public static Response<T> SuccessResponse<T>(T data, ILoggerManager? loggerManager = null)
    {
        loggerManager?.LogInfo($"Respuesta en el consumo del servicio \n Detalle -> {JsonSerializer.Serialize(data)}");
        return new Response<T> { Success = true, Result = data };
    }

    public static Response<T> ErrorResponse<T>(string error, ILoggerManager? loggerManager = null)
    {
        loggerManager?.LogError($"Respuesta en el consumo del servicio \n Detalle -> {error}");
        var errors = JsonSerializer.Deserialize<MessageErrors>(error)!;
        return new Response<T> { Success = false, Errors = new MessageErrors { Code = errors!.Code!, Message = errors.Message } };
    }
}

using System.Text.Json;
using Cfa.ACHInterbank.Application.Helpers.Logs.Implementations;
using Cfa.ACHInterbank.Domain.Models.Configurations;
using Cfa.OpenData.Domain.Models.Response;

namespace Cfa.ACHInterbank.Application.Features;

public static class ExtensionMethods
{
    static LoggerManager loggerManager = new();
    public static AppSettings GetMethodExtensions(this AppSettings appSettings)
    {
        appSettings.Servers!.services = appSettings.Servers.services!.Where(c => c.State == true).ToList();
        appSettings.Servers!.servicesWCF = appSettings.Servers.servicesWCF!.Where(c => c.State == true).ToList();
        return appSettings;
    }

    public static Response<T> SuccessResponse<T>(T data)
    {
        LoggerManager loggerManager = new();
        loggerManager.LogInfo($"Respuesta en el consumo del servicio \n Detalle -> {JsonSerializer.Serialize(data)}");
        return new Response<T> { Success = true, Result = data };
    }

    public static Response<T> ErrorResponse<T>(string error)
    {
        loggerManager.LogError($"Respuesta en el consumo del servicio \n Detalle -> {error}");
        var errors = JsonSerializer.Deserialize<MessageErrors>(error)!;
        return new Response<T> { Success = false, Errors = new MessageErrors { Code = errors!.Code!, Message = errors.Message } };
    }
}

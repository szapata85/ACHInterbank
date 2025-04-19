using Cfa.ACHInterbank.Application.Helpers.Logs.Implementations;
using Cfa.ACHInterbank.Application.Helpers.Logs.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Newtonsoft.Json;

namespace Cfa.ACHInterbank.Application.Helpers.Filters;

public class PostFilter : ActionFilterAttribute
{
    private readonly ILoggerManagerTransient _loggerManager = new LoggerManagerTransient();

    public override void OnActionExecuting(ActionExecutingContext context)
    {
        RegisterLogRequest(context);
    }

    public override void OnActionExecuted(ActionExecutedContext context)
    {
        RegisterLogResponse(context);
    }

    private void RegisterLogRequest(ActionExecutingContext context)
    {
        var filterContext = ((Microsoft.AspNetCore.Mvc.Controllers.ControllerActionDescriptor)context.ActionDescriptor);
        var body = JsonConvert.SerializeObject(context.ActionArguments.FirstOrDefault().Value);
        var httpInfo = context.HttpContext.Request;
        var Url = $"{httpInfo.Protocol}:{httpInfo.Host}{httpInfo.Path}";
        var cabeceras = JsonConvert.SerializeObject(httpInfo.Headers);
        try
        {
            var LogTransactional = string.Empty;
            LogTransactional = $"Solicitud \n Method -> {httpInfo.Method}";
            LogTransactional += $"\n Request Uri -> {Url}";
            LogTransactional += $"\n Controlador -> {filterContext.ControllerName}";
            LogTransactional += $"\n Id Transaccion -> {filterContext.Id}";
            LogTransactional += $"\n Datos -> {body}";
            LogTransactional += $"\n Cabeceras -> {cabeceras}";

            _loggerManager.LogInfo(LogTransactional);
            //_loggerManager.LogInfo($"Inicio de la transacción en el método: tipo -> {httpInfo.Method} Url -> {Url} nombre -> {filterContext.ActionName} controlador -> {filterContext.ControllerName} con Id de transacción -> {filterContext.Id} con el json es -> {body}");
        }
        catch (Exception ex)
        {

            _loggerManager.LogError($"logRequest {ex.Message}");
        }
    }

    private void RegisterLogResponse(ActionExecutedContext context)
    {
        var filterContext = ((Microsoft.AspNetCore.Mvc.Controllers.ControllerActionDescriptor)context.ActionDescriptor);
        var httpInfo = context.HttpContext.Request;
        dynamic result = context.Result is OkObjectResult okResult ? (Microsoft.AspNetCore.Mvc.OkObjectResult)context.Result : (Microsoft.AspNetCore.Mvc.NoContentResult)context.Result;
        var Url = $"{httpInfo.Protocol}:{httpInfo.Host}{httpInfo.Path}";
        var cabeceras = JsonConvert.SerializeObject(httpInfo.Headers);
        var resultValue = result.StatusCode != StatusCodes.Status204NoContent ? JsonConvert.SerializeObject(result.Value) : string.Empty;

        try
        {
            var LogTransactional = string.Empty;
            LogTransactional = $"Respuesta \n Method -> {httpInfo.Method}";
            LogTransactional += $"\n Request Uri -> {Url}";
            LogTransactional += $"\n Controlador -> {filterContext.ControllerName}";
            LogTransactional += $"\n Id Transaccion -> {filterContext.Id}";
            LogTransactional += $"\n Response -> {resultValue}";
            LogTransactional += $"\n Cabeceras -> {cabeceras}";
            _loggerManager.LogInfo(LogTransactional);
        }
        catch (Exception ex)
        {
            _loggerManager.LogError($"logRequest {ex.Message}");
        }

    }

}

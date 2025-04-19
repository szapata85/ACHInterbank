using Cfa.ACHInterbank.Application.Features;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace Cfa.ACHInterbank.Application.Exceptions;

public class ExceptionManager : IExceptionFilter
{
    public void OnException(ExceptionContext context)
    {
        if (context.Exception.Message.Contains("En el momento no podemos completar la operación, por favor intente mas tarde."))
        {
            context.Result = new ObjectResult(ResponseApiService.Response(
             StatusCodes.Status403Forbidden, "Forbidden", context.Exception.Message));
            context.HttpContext.Response.StatusCode = StatusCodes.Status403Forbidden;
        }
        else
        {
            context.Result = new ObjectResult(ResponseApiService.Response(
             StatusCodes.Status500InternalServerError, null!, context.Exception.Message));
            context.HttpContext.Response.StatusCode = StatusCodes.Status500InternalServerError;
        }
    }
}

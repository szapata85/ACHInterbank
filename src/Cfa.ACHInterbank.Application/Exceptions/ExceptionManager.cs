using Cfa.ACHInterbank.Application.Common;
using Cfa.ACHInterbank.Application.Features;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace Cfa.ACHInterbank.Application.Exceptions;

public class ExceptionManager : IExceptionFilter
{
    public void OnException(ExceptionContext context)
    {
        var (statusCode, error) = MapException(context.Exception);

        var result = Result.Failure(error);

        context.Result = new ObjectResult(ResponseApiService.Response(statusCode, result));
        context.HttpContext.Response.StatusCode = statusCode;
        context.ExceptionHandled = true;
    }

    private static (int StatusCode, ErrorDetail Error) MapException(Exception exception)
    {
        if (exception.Message.Contains("En el momento no podemos completar la operación, por favor intente mas tarde.", StringComparison.OrdinalIgnoreCase))
        {
            return (
                StatusCodes.Status403Forbidden,
                new ErrorDetail("FORBIDDEN", exception.Message, ErrorType.Forbidden));
        }

        return (
            StatusCodes.Status500InternalServerError,
            new ErrorDetail("UNEXPECTED_ERROR", exception.Message, ErrorType.Unexpected));
    }
}

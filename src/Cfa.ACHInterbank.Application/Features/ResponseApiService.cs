using System.Linq;
using Cfa.ACHInterbank.Domain.Models.Configurations;
using Cfa.ACHInterbank.Application.Common;

namespace Cfa.ACHInterbank.Application.Features;

public static class ResponseApiService
{
    public static BaseResponseModel Response(int statusCode, object? data = null, string? message = null)
    {
        bool sucess = false;

        if (statusCode >= 200 && statusCode < 300)
            sucess = true;

        var result = new BaseResponseModel
        {
            StatusCode = statusCode,
            Sucess = sucess,
            Message = message,
            Data = data
        };

        return result;
    }

    public static BaseResponseModel Response(int statusCode, Result result)
    {
        var message = result.Errors.FirstOrDefault()?.Message;
        var data = result.IsSuccess
            ? null
            : result.Errors.Select(x => new { x.Code, x.Message, Type = x.Type.ToString() });

        return Response(statusCode, data, message);
    }

    public static BaseResponseModel Response<T>(int statusCode, Result<T> result)
    {
        var message = result.Errors.FirstOrDefault()?.Message;
        object? data = result.IsSuccess
            ? result.Value
            : result.Errors.Select(x => new { x.Code, x.Message, Type = x.Type.ToString() });

        return Response(statusCode, data, message);
    }
}

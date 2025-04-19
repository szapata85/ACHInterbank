using Cfa.ACHInterbank.Domain.Models.Configurations;

namespace Cfa.ACHInterbank.Application.Features;

public static class ResponseApiService
{
    public static BaseResponseModel Response(int statusCode, object Data = null, string message = null)
    {
        bool sucess = false;

        if (statusCode >= 200 && statusCode < 300)
            sucess = true;

        var result = new BaseResponseModel
        {
            StatusCode = statusCode,
            Sucess = sucess,
            Message = message,
            Data = Data
        };

        return result;
    }
}

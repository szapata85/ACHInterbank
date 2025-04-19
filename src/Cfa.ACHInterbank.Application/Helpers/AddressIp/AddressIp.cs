using Microsoft.AspNetCore.Http;

namespace Cfa.ACHInterbank.Application.Helpers.AddressIp;

public static class AddressIp
{
    public static string GetAddressIp(HttpRequest request)
    {
        return (!string.IsNullOrEmpty(request.Headers["X-Forwarded-For"])) ? request.Headers["X-Forwarded-For"].ToString() : request.HttpContext.Connection.RemoteIpAddress.ToString();
    }
}

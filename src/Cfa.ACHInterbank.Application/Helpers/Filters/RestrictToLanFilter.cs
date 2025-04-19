using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace Cfa.ACHInterbank.Application.Helpers.Filters;

public class RestrictToLanFilter : ActionFilterAttribute
{
    private static readonly Regex lanIpRegex = new(@"^(10\.\d{1,3}\.\d{1,3}\.\d{1,3}|172\.(1[6-9]|2[0-9]|3[0-1])\.\d{1,3}\.\d{1,3}|192\.168\.\d{1,3}\.\d{1,3}|127\.0\.0\.1|::1)$");

    public override void OnActionExecuting(ActionExecutingContext context)
    {
        var remoteIp = context.HttpContext.Connection.RemoteIpAddress?.ToString();

        if (remoteIp == null || !lanIpRegex.IsMatch(remoteIp))
        {
            context.Result = new StatusCodeResult(StatusCodes.Status403Forbidden);
        }

        base.OnActionExecuting(context);
    }
}

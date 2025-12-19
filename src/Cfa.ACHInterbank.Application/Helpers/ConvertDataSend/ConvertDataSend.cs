using Cfa.ACHInterbank.Domain.Models.ConvertDataSend;
using Microsoft.AspNetCore.Http;
using Newtonsoft.Json;

namespace Cfa.ACHInterbank.Application.Helpers.ConvertDataSend;

public static class ConvertDataSend
{
    public static string DataSend<T>(HttpRequest? request = null, string parametros = "", string controller = "", string entidad = "", string documento = "", string clientId = "")
    {
        ObjectSendingModel model = new();
        string headerClientId = string.Empty;
        string XChannel = string.Empty;
        string Ip = string.Empty;


        if (request != null)
        {
            XChannel = request.Headers["Canal"];
            Ip = AddressIp.AddressIp.GetAddressIp(request);
            headerClientId = request.Headers["IdSesion"];

            if (!string.IsNullOrEmpty(headerClientId))
                model.Controller = new ControllerModel { Controlador = controller, Entidad = entidad, IdSession = clientId, Ip = Ip, Documento = documento, Canal = XChannel };
            else if (!string.IsNullOrEmpty(documento))
                model.Controller = new ControllerModel { Controlador = controller, Entidad = entidad, Ip = Ip, Documento = documento, Canal = XChannel };
            else if (string.IsNullOrEmpty(documento) && string.IsNullOrEmpty(clientId))
                model.Controller = new ControllerModel { Controlador = controller, Entidad = entidad, Ip = Ip, Documento = documento, Canal = XChannel };

        }
        else
            model.Controller = new ControllerModel { Controlador = controller, Entidad = entidad };

        if (!string.IsNullOrEmpty(parametros) && IsJson(parametros))
            model.Datos = JsonConvert.DeserializeObject<T>(parametros);

        return JsonConvert.SerializeObject(model, Formatting.Indented).ToString();

    }

    private static bool IsJson(string input)
    {
        input = input.Trim();
        return input.StartsWith("{") && input.EndsWith("}")
               || input.StartsWith("[") && input.EndsWith("]");
    }
}

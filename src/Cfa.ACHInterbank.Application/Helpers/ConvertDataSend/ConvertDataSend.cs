using Cfa.ACHInterbank.Domain.Models.ConvertDataSend;
using Microsoft.AspNetCore.Http;
using Newtonsoft.Json;

namespace Cfa.ACHInterbank.Application.Helpers.ConvertDataSend;

public static class ConvertDataSend
{
    public static string DataSend<T>(HttpRequest? request = null, string parametros = "", string controller = "", string entidad = "", string documento = "", string clientId = "")
    {
        ObjectSendingModel model = new();
        string idSesion = string.Empty;
        string channel = string.Empty;
        string ip = string.Empty;


        if (request != null)
        {
            channel = request.Headers["Canal"];
            ip = AddressIp.AddressIp.GetAddressIp(request);
            idSesion = request.Headers["IdSesion"];

            if (!string.IsNullOrEmpty(idSesion))
                model.Controller = new ControllerModel { Controlador = controller, Entidad = entidad, IdSession = idSesion, Ip = ip, Documento = documento, Canal = channel };
            else if (!string.IsNullOrEmpty(documento))
                model.Controller = new ControllerModel { Controlador = controller, Entidad = entidad, Ip = ip, Documento = documento, Canal = channel };
            else if (string.IsNullOrEmpty(documento) && string.IsNullOrEmpty(clientId))
                model.Controller = new ControllerModel { Controlador = controller, Entidad = entidad, Ip = ip, Documento = documento, Canal = channel };

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

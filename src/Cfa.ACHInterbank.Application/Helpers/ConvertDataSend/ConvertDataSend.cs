using Cfa.ACHInterbank.Domain.Models.ConvertDataSend;
using Microsoft.AspNetCore.Http;
using Newtonsoft.Json;

namespace Cfa.ACHInterbank.Application.Helpers.ConvertDataSend;

public static class ConvertDataSend
{
    public static string DataSend<T>(HttpRequest Request = null, string parametros = "", string controller = "", string entidad = "", string Documento = "", string Clientid = "")
    {
        ObjectSendingModel model = new();
        string ClientId = string.Empty;
        string XChannel = string.Empty;
        string Ip = string.Empty;


        if (Request != null)
        {
            XChannel = Request.Headers["Canal"];
            Ip = AddressIp.AddressIp.GetAddressIp(Request);
            ClientId = Request.Headers["IdSesion"];

            if (!string.IsNullOrEmpty(ClientId))
                model.Controller = new ControllerModel { Controlador = controller, Entidad = entidad, IdSession = Clientid, Ip = Ip, Documento = Documento, Canal = XChannel };
            else if (!string.IsNullOrEmpty(Documento))
                model.Controller = new ControllerModel { Controlador = controller, Entidad = entidad, Ip = Ip, Documento = Documento, Canal = XChannel };
            else if (string.IsNullOrEmpty(Documento) && string.IsNullOrEmpty(Clientid))
                model.Controller = new ControllerModel { Controlador = controller, Entidad = entidad, Ip = Ip, Documento = Documento, Canal = XChannel };

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

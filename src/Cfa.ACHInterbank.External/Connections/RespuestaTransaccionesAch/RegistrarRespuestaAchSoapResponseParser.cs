using System.Xml.Linq;
using Cfa.ACHInterbank.Application.ACH.Responses.Models;

namespace Cfa.ACHInterbank.External.Connections.RespuestaTransaccionesAch;

internal sealed class RegistrarRespuestaAchSoapResponseParser
{
    public ResultadoRegistroRespuestaAch Parse(string soapResponseXml)
    {
        if (string.IsNullOrWhiteSpace(soapResponseXml))
        {
            throw new InvalidOperationException("SOAP response is empty for RegistrarRespuestaTransaccion.");
        }

        XDocument document;
        try
        {
            document = XDocument.Parse(soapResponseXml, LoadOptions.None);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException("Malformed SOAP response for RegistrarRespuestaTransaccion.", ex);
        }

        var result = document
            .Descendants()
            .FirstOrDefault(x => x.Name.LocalName == "RegistrarRespuestaTransaccionResult");

        if (result is null)
        {
            throw new InvalidOperationException("SOAP response does not contain RegistrarRespuestaTransaccionResult.");
        }

        var existeErrorText = result.Elements().FirstOrDefault(x => x.Name.LocalName == "existeError")?.Value;
        if (!bool.TryParse(existeErrorText, out var existeError))
        {
            throw new InvalidOperationException("SOAP response field existeError is missing or invalid.");
        }

        var codigoError = result.Elements().FirstOrDefault(x => x.Name.LocalName == "codigoError")?.Value;
        var descripcionError = result.Elements().FirstOrDefault(x => x.Name.LocalName == "descripcionError")?.Value;

        return new ResultadoRegistroRespuestaAch(existeError, codigoError, descripcionError);
    }
}

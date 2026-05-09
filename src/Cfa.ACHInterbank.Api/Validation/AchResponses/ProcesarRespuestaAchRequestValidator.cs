using Cfa.ACHInterbank.Api.Contracts.AchResponses;

namespace Cfa.ACHInterbank.Api.Validation.AchResponses;

public sealed class ProcesarRespuestaAchRequestValidator
{
    public IReadOnlyList<string> Validate(ProcesarRespuestaAchRequest request)
    {
        var errors = new List<string>();
        if (string.IsNullOrWhiteSpace(request.TipoRespuesta)) errors.Add("TipoRespuesta es requerido.");
        else if (!string.Equals(request.TipoRespuesta, "Prenota", StringComparison.OrdinalIgnoreCase)
              && !string.Equals(request.TipoRespuesta, "Transaccion", StringComparison.OrdinalIgnoreCase))
            errors.Add("TipoRespuesta debe ser Prenota o Transaccion.");

        if (string.IsNullOrWhiteSpace(request.IdTransaccion)) errors.Add("IdTransaccion es requerido.");
        if (string.IsNullOrWhiteSpace(request.CodigoCamaraCompensacion)) errors.Add("CodigoCamaraCompensacion es requerido.");
        if (string.IsNullOrWhiteSpace(request.CodigoEstadoExterno)) errors.Add("CodigoEstadoExterno es requerido.");
        if (request.IdCanal <= 0) errors.Add("IdCanal debe ser mayor a cero.");
        if (string.IsNullOrWhiteSpace(request.NombreCanal)) errors.Add("NombreCanal es requerido.");
        if (request.IdTransaccionServicioExterno <= 0) errors.Add("IdTransaccionServicioExterno debe ser mayor a cero.");
        if (request.FechaRecepcion.HasValue && request.FechaRecepcion.Value == DateTime.MinValue) errors.Add("FechaRecepcion no puede ser DateTime.MinValue.");
        return errors;
    }
}

using Cfa.ACHInterbank.Application.ACH.Responses.Processing.Models;

namespace Cfa.ACHInterbank.Application.ACH.Responses.Processing.Validation;

public sealed class ProcesarRespuestaAchCommandValidator
{
    public IReadOnlyList<string> Validate(ProcesarRespuestaAchCommand command)
    {
        var errors = new List<string>();
        if (string.IsNullOrWhiteSpace(command.IdTransaccion)) errors.Add(nameof(command.IdTransaccion));
        if (string.IsNullOrWhiteSpace(command.CodigoCamaraCompensacion)) errors.Add(nameof(command.CodigoCamaraCompensacion));
        if (string.IsNullOrWhiteSpace(command.CodigoEstadoExterno)) errors.Add(nameof(command.CodigoEstadoExterno));
        if (command.IdCanal <= 0) errors.Add(nameof(command.IdCanal));
        if (string.IsNullOrWhiteSpace(command.NombreCanal)) errors.Add(nameof(command.NombreCanal));
        if (command.IdTransaccionServicioExterno <= 0) errors.Add(nameof(command.IdTransaccionServicioExterno));
        if (!Enum.IsDefined(command.TipoRespuesta)) errors.Add(nameof(command.TipoRespuesta));
        if (command.DescripcionCausalExterna is not null && string.IsNullOrWhiteSpace(command.DescripcionCausalExterna)) errors.Add(nameof(command.DescripcionCausalExterna));
        if (command.FechaRecepcion.HasValue && command.FechaRecepcion.Value == DateTime.MinValue) errors.Add(nameof(command.FechaRecepcion));
        return errors;
    }
}

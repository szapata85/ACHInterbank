using Cfa.ACHInterbank.Application.ACH.Responses.Models;

namespace Cfa.ACHInterbank.Application.ACH.Responses.Validation;

public sealed class RegistrarRespuestaAchCommandValidator
{
    public IReadOnlyList<string> Validate(RegistrarRespuestaAchCommand command)
    {
        var errors = new List<string>();

        if (command.IdCanal <= 0)
            errors.Add($"{nameof(RegistrarRespuestaAchCommand.IdCanal)} debe ser mayor que cero.");

        if (string.IsNullOrWhiteSpace(command.NombreCanal))
            errors.Add($"{nameof(RegistrarRespuestaAchCommand.NombreCanal)} es requerido.");

        if (string.IsNullOrWhiteSpace(command.IdTransaccion))
            errors.Add($"{nameof(RegistrarRespuestaAchCommand.IdTransaccion)} es requerido.");

        if (command.IdEstado <= 0)
            errors.Add($"{nameof(RegistrarRespuestaAchCommand.IdEstado)} debe ser mayor que cero.");

        if (command.IdTransaccionServicioExterno <= 0)
            errors.Add($"{nameof(RegistrarRespuestaAchCommand.IdTransaccionServicioExterno)} debe ser mayor que cero.");

        if (!Enum.IsDefined(command.TipoRespuesta))
            errors.Add($"{nameof(RegistrarRespuestaAchCommand.TipoRespuesta)} no es válido.");

        if (command.DescripcionCausal is not null && string.IsNullOrWhiteSpace(command.DescripcionCausal))
            errors.Add($"{nameof(RegistrarRespuestaAchCommand.DescripcionCausal)} no puede contener solo espacios.");

        return errors;
    }
}

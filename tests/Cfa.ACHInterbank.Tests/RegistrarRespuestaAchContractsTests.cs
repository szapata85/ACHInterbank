using Cfa.ACHInterbank.Application.ACH.Responses.Models;
using Cfa.ACHInterbank.Application.ACH.Responses.Validation;
using Xunit;

namespace Cfa.ACHInterbank.Tests;

public class RegistrarRespuestaAchContractsTests
{
    [Fact]
    public void ResultadoRegistroRespuestaAch_Exitoso_ShouldBeTrue_WhenExisteErrorFalse()
    {
        var result = new ResultadoRegistroRespuestaAch(false, null, null);
        Assert.True(result.Exitoso);
    }

    [Fact]
    public void ResultadoRegistroRespuestaAch_Exitoso_ShouldBeFalse_WhenExisteErrorTrue()
    {
        var result = new ResultadoRegistroRespuestaAch(true, "E01", "Error");
        Assert.False(result.Exitoso);
    }

    [Fact]
    public void RegistrarRespuestaAchCommandValidator_ShouldAcceptValidPrenotaCommand()
    {
        var validator = new RegistrarRespuestaAchCommandValidator();
        var command = BuildValidCommand(TipoRespuestaAch.Prenota);

        var errors = validator.Validate(command);

        Assert.Empty(errors);
    }

    [Fact]
    public void RegistrarRespuestaAchCommandValidator_ShouldAcceptValidTransaccionCommand()
    {
        var validator = new RegistrarRespuestaAchCommandValidator();
        var command = BuildValidCommand(TipoRespuestaAch.Transaccion);

        var errors = validator.Validate(command);

        Assert.Empty(errors);
    }

    [Fact]
    public void RegistrarRespuestaAchCommandValidator_ShouldRejectInvalidIdCanal()
    {
        var validator = new RegistrarRespuestaAchCommandValidator();
        var command = BuildValidCommand(TipoRespuestaAch.Transaccion) with { IdCanal = 0 };

        var errors = validator.Validate(command);

        Assert.Contains(errors, e => e.Contains(nameof(RegistrarRespuestaAchCommand.IdCanal)));
    }

    [Fact]
    public void RegistrarRespuestaAchCommandValidator_ShouldRejectEmptyNombreCanal()
    {
        var validator = new RegistrarRespuestaAchCommandValidator();
        var command = BuildValidCommand(TipoRespuestaAch.Transaccion) with { NombreCanal = "   " };

        var errors = validator.Validate(command);

        Assert.Contains(errors, e => e.Contains(nameof(RegistrarRespuestaAchCommand.NombreCanal)));
    }

    [Fact]
    public void RegistrarRespuestaAchCommandValidator_ShouldRejectEmptyIdTransaccion()
    {
        var validator = new RegistrarRespuestaAchCommandValidator();
        var command = BuildValidCommand(TipoRespuestaAch.Transaccion) with { IdTransaccion = "" };

        var errors = validator.Validate(command);

        Assert.Contains(errors, e => e.Contains(nameof(RegistrarRespuestaAchCommand.IdTransaccion)));
    }

    [Fact]
    public void RegistrarRespuestaAchCommandValidator_ShouldRejectInvalidIdEstado()
    {
        var validator = new RegistrarRespuestaAchCommandValidator();
        var command = BuildValidCommand(TipoRespuestaAch.Transaccion) with { IdEstado = -1 };

        var errors = validator.Validate(command);

        Assert.Contains(errors, e => e.Contains(nameof(RegistrarRespuestaAchCommand.IdEstado)));
    }

    [Fact]
    public void RegistrarRespuestaAchCommandValidator_ShouldRejectInvalidIdTransaccionServicioExterno()
    {
        var validator = new RegistrarRespuestaAchCommandValidator();
        var command = BuildValidCommand(TipoRespuestaAch.Transaccion) with { IdTransaccionServicioExterno = 0 };

        var errors = validator.Validate(command);

        Assert.Contains(errors, e => e.Contains(nameof(RegistrarRespuestaAchCommand.IdTransaccionServicioExterno)));
    }

    [Fact]
    public void RegistrarRespuestaAchCommand_ShouldNotExposePhysicalSoapFieldNames()
    {
        var propertyNames = typeof(RegistrarRespuestaAchCommand).GetProperties().Select(p => p.Name).ToList();
        var forbidden = new[] { "idTransaccionAxon", "IdTransaccionAxon", "SoapAction", "SoapEnvelope", "Xml", "Wsdl", "Axon" };

        foreach (var token in forbidden)
        {
            Assert.DoesNotContain(propertyNames, p => p.Contains(token, StringComparison.OrdinalIgnoreCase));
        }
    }

    private static RegistrarRespuestaAchCommand BuildValidCommand(TipoRespuestaAch tipo)
        => new(
            TipoRespuesta: tipo,
            IdTransaccion: "TX-001",
            IdCanal: 1,
            NombreCanal: "ACH",
            IdEstado: 2,
            Causal: "R01",
            IdTransaccionServicioExterno: 999,
            DescripcionCausal: "Cuenta cerrada",
            CodigoCamaraCompensacion: "ACH",
            CodigoEntidadOrigen: "001",
            CodigoEntidadDestino: "002",
            CorrelationId: "corr-1");
}

using Cfa.ACHInterbank.Api.Contracts.AchResponses;
using Cfa.ACHInterbank.Api.Mappers.AchResponses;
using Cfa.ACHInterbank.Api.Validation.AchResponses;
using Cfa.ACHInterbank.Application.ACH.Responses.Notification.Models;
using Cfa.ACHInterbank.Application.ACH.Responses.Processing.Models;
using Cfa.ACHInterbank.Domain.Models.ACH.Enums;

namespace Cfa.ACHInterbank.Tests;

public class AchResponseApiContractsTests
{
    private static readonly string[] ForbiddenNames = ["Axon", "Soap", "Xml", "Wsdl", "Envelope", "SOAPAction", "idTransaccionAxon", "IdTransaccionAxon"];

    [Fact]
    public void ProcesarRespuestaAchRequest_ShouldNotExposeSoapOrProviderFields()
    {
        var props = typeof(ProcesarRespuestaAchRequest).GetProperties().Select(x => x.Name);
        props.Should().NotContain(p => ForbiddenNames.Any(f => p.Contains(f, StringComparison.OrdinalIgnoreCase)));
    }

    [Fact]
    public void AchResponseApiResponses_ShouldNotExposeSoapOrProviderFields()
    {
        var responseTypes = new[]
        {
            typeof(ProcesarRespuestaAchResponse), typeof(NotificarRespuestaAchResponse), typeof(AchResponseListItemResponse),
            typeof(AchResponseDetailResponse), typeof(AchResponseNotificationAttemptResponse), typeof(AchResponseStatusMappingResponse)
        };

        foreach (var t in responseTypes)
        {
            var props = t.GetProperties().Select(x => x.Name);
            props.Should().NotContain(p => ForbiddenNames.Any(f => p.Contains(f, StringComparison.OrdinalIgnoreCase)));
        }
    }

    [Fact]
    public void ProcesarRespuestaAchApiMapper_ShouldMapRequestToCommand()
    {
        var mapper = new ProcesarRespuestaAchApiMapper();
        var request = new ProcesarRespuestaAchRequest("Transaccion", "TX-1", "ACH", "001", "002", "E1", "C1", "Desc", 1, "Canal", 99, DateTime.UtcNow, "corr");

        var command = mapper.MapRequest(request);

        command.TipoRespuesta.Should().Be(TipoRespuestaAch.Transaccion);
        command.IdTransaccionServicioExterno.Should().Be(99);
        command.CodigoEstadoExterno.Should().Be("E1");
    }

    [Fact]
    public void ProcesarRespuestaAchApiMapper_ShouldMapResultToResponse()
    {
        var mapper = new ProcesarRespuestaAchApiMapper();
        var id = Guid.NewGuid();
        var result = new ProcesarRespuestaAchResult(id, true, false, true, true, true, AchResponseProcessingStatus.Notificada, null, "hash");

        var response = mapper.MapResponse(result);

        response.AchResponseId.Should().Be(id);
        response.Procesada.Should().BeTrue();
        response.HashIdempotencia.Should().Be("hash");
    }

    [Fact]
    public void NotificarRespuestaAchApiMapper_ShouldMapRequestToCommand()
    {
        var mapper = new NotificarRespuestaAchApiMapper();
        var request = new NotificarRespuestaAchRequest(10, "corr");

        var command = mapper.MapRequest(request);

        command.NotificationAttemptId.Should().Be(10);
        command.CorrelationId.Should().Be("corr");
    }

    [Fact]
    public void NotificarRespuestaAchApiMapper_ShouldMapResultToResponse()
    {
        var mapper = new NotificarRespuestaAchApiMapper();
        var result = new NotificarRespuestaAchResult(true, true, false, true, false, AchResponseNotificationStatus.ErrorFuncional, AchResponseProcessingStatus.ErrorFuncional, "E01", "Error funcional", null, "motivo");

        var response = mapper.MapResponse(result);

        response.Procesada.Should().BeTrue();
        response.EstadoNotificacion.Should().Be("ErrorFuncional");
        response.CodigoError.Should().Be("E01");
    }

    [Fact]
    public void ProcesarRespuestaAchRequestValidator_ShouldRejectInvalidTipoRespuesta()
    {
        var validator = new ProcesarRespuestaAchRequestValidator();
        var request = new ProcesarRespuestaAchRequest("Otro", "TX", "ACH", null, null, "E1", null, null, 1, "Canal", 1, DateTime.UtcNow, null);

        var errors = validator.Validate(request);

        errors.Should().Contain(x => x.Contains("Prenota o Transaccion"));
    }

    [Fact]
    public void ProcesarRespuestaAchRequestValidator_ShouldAcceptValidRequest()
    {
        var validator = new ProcesarRespuestaAchRequestValidator();
        var request = new ProcesarRespuestaAchRequest("Prenota", "TX", "ACH", null, null, "E1", null, null, 1, "Canal", 1, DateTime.UtcNow, null);

        var errors = validator.Validate(request);

        errors.Should().BeEmpty();
    }

    [Fact]
    public void NotificarRespuestaAchRequestValidator_ShouldRejectInvalidAttemptId()
    {
        var validator = new NotificarRespuestaAchRequestValidator();
        var errors = validator.Validate(new NotificarRespuestaAchRequest(0, null));
        errors.Should().Contain(x => x.Contains("mayor a cero"));
    }

    [Fact]
    public void PagedResponse_ShouldCalculateTotalPages()
    {
        var paged = new PagedResponse<int>([1,2], 1, 2, 5);
        paged.TotalPages.Should().Be(3);
    }
}

using Cfa.ACHInterbank.Application.ACH.Responses.Homologation.Interfaces;
using Cfa.ACHInterbank.Application.ACH.Responses.Homologation.Models;
using Cfa.ACHInterbank.Application.ACH.Responses.Homologation.Services;
using Cfa.ACHInterbank.Application.ACH.Responses.Models;
using Cfa.ACHInterbank.Domain.Models.ACH.Enums;
using Moq;
using Xunit;

namespace Cfa.ACHInterbank.Tests;

public class RespuestaAchStatusMappingServiceTests
{

    [Fact]
    public async Task HomologarAsync_ShouldUseStateOnlyMapping_WhenCausalMissing_AndOtherMappingsRequireCausal()
    {
        var causalRequired = BuildMapping(id: 1, causal: "R01", requiereCausal: true);
        var stateOnly = BuildMapping(id: 2, causal: null, requiereCausal: false);
        var service = BuildService(causalRequired, stateOnly);

        var result = await service.HomologarAsync(new HomologarRespuestaAchRequest("ACH", TipoRespuestaAch.Transaccion, "00", null, DateTime.UtcNow));

        Assert.True(result.ExisteHomologacion);
        Assert.Equal(2, result.IdEstadoInterno);
    }

    [Fact]
    public async Task HomologarAsync_ShouldReturnNotFound_WhenCausalMissing_AndOnlyCausalRequiredMappingsExist()
    {
        var mappingA = BuildMapping(id: 1, causal: "R01", requiereCausal: true);
        var mappingB = BuildMapping(id: 2, causal: "R02", requiereCausal: true);
        var service = BuildService(mappingA, mappingB);

        var result = await service.HomologarAsync(new HomologarRespuestaAchRequest("ACH", TipoRespuestaAch.Transaccion, "00", null, DateTime.UtcNow));

        Assert.False(result.ExisteHomologacion);
        Assert.Contains("causal", result.MotivoNoHomologacion ?? string.Empty, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task HomologarAsync_ShouldTreatBlankMappingCausalAsStateOnly()
    {
        var mapping = BuildMapping(id: 6, causal: "   ", requiereCausal: false);
        var service = BuildService(mapping);

        var result = await service.HomologarAsync(new HomologarRespuestaAchRequest("ACH", TipoRespuestaAch.Transaccion, "00", null, DateTime.UtcNow));

        Assert.True(result.ExisteHomologacion);
        Assert.Equal(6, result.IdEstadoInterno);
    }

    [Fact]
    public async Task HomologarAsync_ShouldNormalizeMappingCausal_WhenMatchingExactCausal()
    {
        var mapping = BuildMapping(id: 7, causal: " r01 ", requiereCausal: true);
        var service = BuildService(mapping);

        var result = await service.HomologarAsync(new HomologarRespuestaAchRequest("ACH", TipoRespuestaAch.Transaccion, "00", "R01", DateTime.UtcNow));

        Assert.True(result.ExisteHomologacion);
        Assert.Equal(7, result.IdEstadoInterno);
    }

    [Fact]
    public async Task HomologarAsync_ShouldReturnSuccess_WhenExactStateAndCausalMatch()
    {
        var mapping = BuildMapping(causal: "R01", requiereCausal: true);
        var service = BuildService(mapping);

        var result = await service.HomologarAsync(new HomologarRespuestaAchRequest("ACH", TipoRespuestaAch.Transaccion, "00", "R01", DateTime.UtcNow));

        Assert.True(result.ExisteHomologacion);
        Assert.True(result.PermiteNotificacion);
        Assert.Equal(mapping.IdEstadoInterno, result.IdEstadoInterno);
    }

    [Fact]
    public async Task HomologarAsync_ShouldReturnSuccess_WhenStateMatchesWithoutCausal()
    {
        var mapping = BuildMapping(causal: null, requiereCausal: false);
        var service = BuildService(mapping);

        var result = await service.HomologarAsync(new HomologarRespuestaAchRequest("ACH", TipoRespuestaAch.Transaccion, "00", null, DateTime.UtcNow));

        Assert.True(result.ExisteHomologacion);
    }

    [Fact]
    public async Task HomologarAsync_ShouldPreferExactCausalOverStateOnlyFallback()
    {
        var fallback = BuildMapping(id: 1, causal: null, requiereCausal: false);
        var exact = BuildMapping(id: 2, causal: "R02", requiereCausal: true);
        var service = BuildService(fallback, exact);

        var result = await service.HomologarAsync(new HomologarRespuestaAchRequest("ACH", TipoRespuestaAch.Transaccion, "00", "R02", DateTime.UtcNow));

        Assert.Equal(2, result.IdEstadoInterno);
        Assert.Equal("R02", result.CausalNormalizada);
    }

    [Fact]
    public async Task HomologarAsync_ShouldFallbackToStateOnly_WhenCausalProvidedButMappingDoesNotRequireCausal()
    {
        var fallback = BuildMapping(id: 3, causal: null, requiereCausal: false);
        var service = BuildService(fallback);

        var result = await service.HomologarAsync(new HomologarRespuestaAchRequest("ACH", TipoRespuestaAch.Transaccion, "00", "R77", DateTime.UtcNow));

        Assert.True(result.ExisteHomologacion);
        Assert.Equal(3, result.IdEstadoInterno);
    }

    [Fact]
    public async Task HomologarAsync_ShouldReturnNotFound_WhenCausalRequiredButMissing()
    {
        var mapping = BuildMapping(causal: null, requiereCausal: true);
        var service = BuildService(mapping);

        var result = await service.HomologarAsync(new HomologarRespuestaAchRequest("ACH", TipoRespuestaAch.Transaccion, "00", null, DateTime.UtcNow));

        Assert.False(result.ExisteHomologacion);
        Assert.Contains("causal", result.MotivoNoHomologacion ?? string.Empty, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task HomologarAsync_ShouldReturnNotFound_WhenNoActiveMappingExists()
    {
        var inactive = BuildMapping(activo: false);
        var service = BuildService(inactive);

        var result = await service.HomologarAsync(new HomologarRespuestaAchRequest("ACH", TipoRespuestaAch.Transaccion, "00", null, DateTime.UtcNow));

        Assert.False(result.ExisteHomologacion);
    }

    [Fact]
    public async Task HomologarAsync_ShouldIgnoreInactiveMappings()
    {
        var inactive = BuildMapping(id: 1, activo: false);
        var active = BuildMapping(id: 2, activo: true);
        var service = BuildService(inactive, active);

        var result = await service.HomologarAsync(new HomologarRespuestaAchRequest("ACH", TipoRespuestaAch.Transaccion, "00", null, DateTime.UtcNow));

        Assert.True(result.ExisteHomologacion);
        Assert.Equal(2, result.IdEstadoInterno);
    }

    [Fact]
    public async Task HomologarAsync_ShouldIgnoreMappingsOutsideVigency()
    {
        var expired = BuildMapping(id: 1, fechaInicio: DateTime.UtcNow.AddDays(-10), fechaFin: DateTime.UtcNow.AddDays(-1));
        var future = BuildMapping(id: 2, fechaInicio: DateTime.UtcNow.AddDays(1));
        var service = BuildService(expired, future);

        var result = await service.HomologarAsync(new HomologarRespuestaAchRequest("ACH", TipoRespuestaAch.Transaccion, "00", null, DateTime.UtcNow));

        Assert.False(result.ExisteHomologacion);
    }

    [Fact]
    public async Task HomologarAsync_ShouldReturnNotAllowed_WhenMappingExistsButPermiteNotificacionFalse()
    {
        var mapping = BuildMapping(permiteNotificacion: false);
        var service = BuildService(mapping);

        var result = await service.HomologarAsync(new HomologarRespuestaAchRequest("ACH", TipoRespuestaAch.Transaccion, "00", null, DateTime.UtcNow));

        Assert.True(result.ExisteHomologacion);
        Assert.False(result.PermiteNotificacion);
    }

    [Fact]
    public async Task HomologarAsync_ShouldNormalizeInputValues()
    {
        var repo = new Mock<IAchResponseStatusMappingRepository>();
        repo.Setup(r => r.FindCandidatesAsync("ACH", TipoRespuestaAch.Transaccion, "00", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<AchResponseStatusMappingModel> { BuildMapping() });
        var service = new RespuestaAchStatusMappingService(repo.Object);

        var result = await service.HomologarAsync(new HomologarRespuestaAchRequest(" ach ", TipoRespuestaAch.Transaccion, " 00 ", " r01 ", DateTime.UtcNow));

        Assert.True(result.ExisteHomologacion);
        repo.VerifyAll();
    }

    [Fact]
    public async Task HomologarAsync_ShouldChooseMostRecentEffectiveMapping_WhenMultipleEquivalentMappingsExist()
    {
        var old = BuildMapping(id: 1, fechaInicio: DateTime.UtcNow.AddDays(-20));
        var recent = BuildMapping(id: 2, fechaInicio: DateTime.UtcNow.AddDays(-1));
        var service = BuildService(old, recent);

        var result = await service.HomologarAsync(new HomologarRespuestaAchRequest("ACH", TipoRespuestaAch.Transaccion, "00", null, DateTime.UtcNow));

        Assert.Equal(2, result.IdEstadoInterno);
    }

    [Fact]
    public async Task HomologarAsync_ShouldNotThrow_WhenRepositoryReturnsEmptyList()
    {
        var repo = new Mock<IAchResponseStatusMappingRepository>();
        repo.Setup(r => r.FindCandidatesAsync(It.IsAny<string>(), It.IsAny<TipoRespuestaAch>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<AchResponseStatusMappingModel>());
        var service = new RespuestaAchStatusMappingService(repo.Object);

        var result = await service.HomologarAsync(new HomologarRespuestaAchRequest("ACH", TipoRespuestaAch.Transaccion, "00", null, DateTime.UtcNow));

        Assert.False(result.ExisteHomologacion);
    }

    private static RespuestaAchStatusMappingService BuildService(params AchResponseStatusMappingModel[] mappings)
    {
        var repo = new Mock<IAchResponseStatusMappingRepository>();
        repo.Setup(r => r.FindCandidatesAsync(It.IsAny<string>(), It.IsAny<TipoRespuestaAch>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(mappings.ToList());
        return new RespuestaAchStatusMappingService(repo.Object);
    }

    private static AchResponseStatusMappingModel BuildMapping(
        int id = 10,
        string? causal = null,
        bool requiereCausal = false,
        bool activo = true,
        bool permiteNotificacion = true,
        DateTime? fechaInicio = null,
        DateTime? fechaFin = null)
        => new()
        {
            Id = id,
            CodigoCamaraCompensacion = "ACH",
            TipoRespuesta = TipoRespuestaAch.Transaccion,
            CodigoEstadoExterno = "00",
            CodigoCausalExterna = causal,
            IdEstadoInterno = id,
            IdEstadoServicioExterno = 200 + id,
            EstadoInternoNombre = $"EST_{id}",
            CausalNormalizada = causal,
            DescripcionCausalNormalizada = causal is null ? null : "Desc",
            RequiereCausal = requiereCausal,
            PermiteNotificacion = permiteNotificacion,
            Activo = activo,
            FechaInicioVigencia = fechaInicio ?? DateTime.UtcNow.AddDays(-5),
            FechaFinVigencia = fechaFin
        };
}

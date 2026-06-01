using Cfa.ACHInterbank.Api.Controllers;
using Cfa.ACHInterbank.Application.ACH.Interfaces;
using Cfa.ACHInterbank.Application.ACH.Models;
using Cfa.ACHInterbank.Domain.Entities.Ach.Dtos;
using Cfa.ACHInterbank.Persistence.ACH.Services.Implementation;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Xunit;

namespace Cfa.ACHInterbank.Tests;

public class NachaSoapUatConsoleReadModelTests
{
    [Fact]
    public async Task SoapUatConsole_ShouldReturnNoGoAndNoRealInvocation()
    {
        var service = new NachaSoapUatConsoleReadModelService(ReadStore().Object);

        var dashboard = await service.GetDashboardAsync();

        dashboard.ProductiveStatus.Should().Be("NO-GO");
        dashboard.ProductiveExecution.Should().BeFalse();
        dashboard.WouldInvokeRealSoap.Should().BeFalse();
    }

    [Fact]
    public async Task SoapUatConsole_ShouldReturnCandidatesFromPersistedOrDerivedSources()
    {
        var candidates = await new NachaSoapUatConsoleReadModelService(ReadStore().Object).GetCandidatesAsync();

        candidates.Should().Contain(x => x.OperationCandidate == "ProcTransacciones" && x.IsDerived && x.IsPersisted);
        candidates.Should().Contain(x => x.OperationCandidate == "RegistrarRespuestaTransaccion");
    }

    [Fact]
    public async Task SoapUatConsole_ShouldReturnPartialWarningsWhenSourcesMissing()
    {
        var readStore = ReadStore(decisions: [Decision("corr-partial", "ProcTransacciones")], readiness: [], audit: []);

        var dashboard = await new NachaSoapUatConsoleReadModelService(readStore.Object).GetDashboardAsync();

        dashboard.IsPartialData.Should().BeTrue();
        dashboard.Warnings.Should().Contain(x => x.Contains("parcial", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task SoapUatConsole_ShouldSanitizeAuditDetails()
    {
        var audit = await new NachaSoapUatConsoleReadModelService(ReadStore().Object).GetAuditAsync();
        var serialized = Serialize(audit);

        serialized.Should().Contain("sanitized");
        serialized.Should().NotContain("<soap");
        serialized.Should().NotContain("secret-value");
    }

    [Fact]
    public async Task SoapUatConsole_ShouldNotExposeSecretsEndpointsOrPayloads()
    {
        var audit = await new NachaSoapUatConsoleReadModelService(ReadStore().Object).GetAuditAsync();
        var serialized = Serialize(audit);

        serialized.Should().NotContain("https://real-bank-soap");
        serialized.Should().NotContain("certificate-private");
        serialized.Should().NotContain("password");
        serialized.Should().NotContain("token");
    }

    [Fact]
    public async Task SoapUatConsole_ShouldNotCallSaveChanges()
    {
        var readStore = ReadStore();
        var service = new NachaSoapUatConsoleReadModelService(readStore.Object);

        await service.GetDashboardAsync();

        readStore.Verify(x => x.GetOperationalDecisionsAsync(It.IsAny<CancellationToken>()), Times.Once);
        readStore.Verify(x => x.GetSoapReadinessAsync(It.IsAny<CancellationToken>()), Times.Once);
        readStore.Verify(x => x.GetOperationalAuditAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public void SoapUatConsole_ShouldNotExecuteSoapOrGateway()
    {
        typeof(NachaSoapUatConsoleReadModelService).GetConstructors().Single().GetParameters()
            .Select(x => x.ParameterType.Name)
            .Should()
            .NotContain(x => x.Contains("Orchestrator", StringComparison.OrdinalIgnoreCase)
                || x.Contains("Executor", StringComparison.OrdinalIgnoreCase)
                || x.Contains("Gateway", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task SoapUatConsole_ShouldKeepRegistrarRespuestaNonMonetary()
    {
        var candidates = await new NachaSoapUatConsoleReadModelService(ReadStore().Object).GetCandidatesAsync();

        candidates.Single(x => x.OperationCandidate == "RegistrarRespuestaTransaccion").RequiresMonetaryMovement.Should().BeFalse();
    }

    [Fact]
    public async Task SoapUatConsole_ShouldClassifyProcTransaccionesAndContrapartidasAsCandidatesOnly()
    {
        var candidates = await new NachaSoapUatConsoleReadModelService(ReadStore().Object).GetCandidatesAsync();

        candidates.Where(x => x.OperationCandidate is "ProcTransacciones" or "ProcContrapartidas")
            .Should()
            .OnlyContain(x => x.RequiresMonetaryMovement && x.IsBlocked && !x.WouldInvokeRealSoap && !x.ProductiveExecution);
    }

    [Fact]
    public void SoapUatConsoleEndpoints_ShouldBeGetOnly()
    {
        typeof(NachaSoapUatConsoleController).GetMethods()
            .Where(x => x.DeclaringType == typeof(NachaSoapUatConsoleController) && x.IsPublic && x.Name.StartsWith("Get", StringComparison.Ordinal))
            .Should()
            .OnlyContain(x => x.GetCustomAttributes(typeof(HttpGetAttribute), true).Any());
    }

    [Fact]
    public void NachaExport_ShouldStillUseCycleIdNotHash()
    {
        var action = typeof(NachaExportController).GetMethod(nameof(NachaExportController.Export));

        action!.GetParameters().Should().ContainSingle(x => x.Name == "cycleId");
        action.GetParameters().Should().NotContain(x => x.Name!.Contains("hash", StringComparison.OrdinalIgnoreCase));
        typeof(AchCycleExportDto).GetProperty(nameof(AchCycleExportDto.ExportIdentifier)).Should().NotBeNull();
    }

    [Fact]
    public void DashboardAndConsole_ShouldNotUseLegacyEndpoints()
    {
        var constructorTypes = typeof(NachaSoapUatConsoleController).GetConstructors().Single().GetParameters()
            .Concat(typeof(NachaOperationalReadinessController).GetConstructors().Single().GetParameters())
            .Select(x => x.ParameterType.Name);

        constructorTypes.Should().NotContain(x => x.Contains("Layout", StringComparison.OrdinalIgnoreCase) || x.Contains("Definition", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task GetCandidate_ShouldReturnNotFoundForMissingCorrelation()
    {
        var controller = new NachaSoapUatConsoleController(new NachaSoapUatConsoleReadModelService(ReadStore().Object));

        var result = await controller.GetCandidate("missing", default);

        result.Should().BeOfType<NotFoundObjectResult>();
    }

    private static Mock<INachaOperationalReadStore> ReadStore(
        IReadOnlyList<NachaOperationalDecisionReadModel>? decisions = null,
        IReadOnlyList<NachaSoapReadinessReadModel>? readiness = null,
        IReadOnlyList<NachaOperationalAuditReadModel>? audit = null)
    {
        var readStore = new Mock<INachaOperationalReadStore>(MockBehavior.Strict);
        readStore.Setup(x => x.GetOperationalFilesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync([new NachaOperationalFileReadModel
            {
                FileId = "nacha-N1",
                FileName = "entrada.ach",
                ClearingHouseCode = "ACH",
                ProfileCode = "nacha-config profiles",
                FlowType = "IncomingPersisted",
                ProcessingStatus = "Processed",
                CorrelationId = "corr-proc",
                CreatedAt = DateTimeOffset.UtcNow
            }]);
        readStore.Setup(x => x.GetOperationalDecisionsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(decisions ?? [Decision("corr-proc", "ProcTransacciones"), Decision("corr-reg", "RegistrarRespuestaTransaccion"), Decision("corr-manual", "None", manual: true)]);
        readStore.Setup(x => x.GetSoapReadinessAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(readiness ?? [Readiness("corr-proc", "ProcTransacciones", success: true), Readiness("corr-contra", "ProcContrapartidas", success: false)]);
        readStore.Setup(x => x.GetOperationalAuditAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(audit ?? [new NachaOperationalAuditReadModel
            {
                CorrelationId = "corr-proc",
                Phase = "6B.5",
                EventType = "IncomingNachaIntegrationExecution",
                Severity = "Information",
                Message = "SOAP/UAT audit sanitizado",
                Timestamp = DateTimeOffset.UtcNow,
                DataSource = "backend read-only",
                IsPersisted = true,
                SanitizedDetails = new Dictionary<string, string>
                {
                    ["RequestPayloadXml"] = "<soap>secret-value</soap>",
                    ["EndpointUrl"] = "https://real-bank-soap.example",
                    ["CertificatePrivateMaterial"] = "certificate-private",
                    ["WouldInvokeRealSoap"] = "false"
                }
            }]);
        return readStore;
    }

    private static NachaOperationalDecisionReadModel Decision(string correlationId, string operation, bool manual = false)
        => new()
        {
            CorrelationId = correlationId,
            FileName = "entrada.ach",
            EntryTraceNumber = "***0001",
            DecisionType = manual ? "ManualReviewRequired" : "CreditoEntrante",
            SoapOperationCandidate = operation,
            RequiresMonetaryMovement = operation is "ProcTransacciones" or "ProcContrapartidas",
            ReasonCode = "00",
            ReasonDescription = "Decision sanitizada",
            NewInternalStatus = manual ? "RevisionManual" : "Accepted",
            ManualReviewRequired = manual,
            IsBlocked = manual,
            DataSource = "backend read-only",
            IsPersisted = true,
            IsDerived = true,
            CreatedAt = DateTimeOffset.UtcNow
        };

    private static NachaSoapReadinessReadModel Readiness(string correlationId, string operation, bool success)
        => new()
        {
            CorrelationId = correlationId,
            OperationCandidate = operation,
            IsReadyForUat = success,
            IsBlocked = !success,
            BlockReasons = success ? [] : ["NO-GO"],
            SimulationPassed = success,
            ResiliencePassed = success,
            PayloadMappingPassed = true,
            RequestMappingPassed = true,
            OperationalGatePassed = success,
            ReadinessCheckPassed = success,
            ProductiveExecution = false,
            WouldInvokeRealSoap = false,
            RequiresMonetaryMovement = operation is "ProcTransacciones" or "ProcContrapartidas",
            Phase = "6B.5",
            DataSource = "backend read-only",
            IsPersisted = true,
            IsDerived = true,
            LastCheckedAt = DateTimeOffset.UtcNow
        };

    private static string Serialize(object value)
        => System.Text.Json.JsonSerializer.Serialize(value).ToLowerInvariant();
}

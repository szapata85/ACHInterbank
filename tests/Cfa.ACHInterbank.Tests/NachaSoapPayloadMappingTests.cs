using Cfa.ACHInterbank.Application.ACH.Models;
using Cfa.ACHInterbank.Persistence.ACH.Services.Implementation;
using Xunit;

namespace Cfa.ACHInterbank.Tests;

public class NachaSoapPayloadMappingTests
{
    [Fact]
    public void MapProcContrapartidas_ShouldCreatePayloadForCfaDebit()
    {
        var result = Map(Decision(NachaSoapOperationCandidate.ProcContrapartidas, NachaIncomingDecisionType.ApplyDebitMovement, true));

        Assert.True(result.IsMapped);
        Assert.True(result.Payload is NachaSoapProcContrapartidasPayload);
        Assert.True(result.RequiresMonetaryMovement);
    }

    [Fact]
    public void MapProcContrapartidas_ShouldRequireMonetaryMovement()
    {
        var result = Map(Decision(NachaSoapOperationCandidate.ProcContrapartidas, NachaIncomingDecisionType.ApplyDebitMovement, true));

        Assert.True(result.RequiresMonetaryMovement);
        Assert.True(result.IsExecutable);
    }

    [Fact]
    public void MapProcContrapartidas_ShouldRejectNonMonetaryDecision()
    {
        var result = Map(Decision(NachaSoapOperationCandidate.ProcContrapartidas, NachaIncomingDecisionType.ApplyDebitMovement, false));

        Assert.False(result.IsMapped);
        Assert.Contains(result.Errors, x => x.Contains("movimiento monetario", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void MapProcContrapartidas_ShouldRejectManualReview()
    {
        var result = Map(Decision(NachaSoapOperationCandidate.ProcContrapartidas, NachaIncomingDecisionType.ManualReviewRequired, true));

        Assert.False(result.IsExecutable);
        Assert.Contains(result.Errors, x => x.Contains("ManualReviewRequired", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void MapProcContrapartidas_ShouldIncludePhase6B5()
    {
        var result = Map(Decision(NachaSoapOperationCandidate.ProcContrapartidas, NachaIncomingDecisionType.ApplyDebitMovement, true));

        Assert.Equal("6B.5", result.Phase);
        Assert.Equal("6B.5", result.Payload!.Phase);
    }

    [Fact]
    public void MapProcContrapartidas_ShouldCreateSanitizedSummary()
    {
        var result = Map(Decision(NachaSoapOperationCandidate.ProcContrapartidas, NachaIncomingDecisionType.ApplyDebitMovement, true));

        Assert.Equal("ProcContrapartidas", result.SanitizedSummary["OperationCandidate"]);
        Assert.DoesNotContain("1234567890123456", string.Join("|", result.SanitizedSummary.Values));
    }

    [Fact]
    public void MapProcTransacciones_ShouldCreatePayloadForExternalCredit()
    {
        var result = Map(Decision(NachaSoapOperationCandidate.ProcTransacciones, NachaIncomingDecisionType.ApplyCreditMovement, true));

        Assert.True(result.IsMapped);
        var payload = Assert.IsType<NachaSoapProcTransaccionesPayload>(result.Payload);
        Assert.Equal("76543210", payload.ExternalOriginatorInstitutionCode);
        Assert.Equal("12345678", payload.CfaReceiverInstitutionCode);
    }

    [Fact]
    public void MapProcTransacciones_ShouldRequireMonetaryMovement()
    {
        var result = Map(Decision(NachaSoapOperationCandidate.ProcTransacciones, NachaIncomingDecisionType.ApplyCreditMovement, true));

        Assert.True(result.RequiresMonetaryMovement);
    }

    [Fact]
    public void MapProcTransacciones_ShouldRejectNonMonetaryDecision()
    {
        var result = Map(Decision(NachaSoapOperationCandidate.ProcTransacciones, NachaIncomingDecisionType.ApplyCreditMovement, false));

        Assert.False(result.IsMapped);
        Assert.Contains(result.Errors, x => x.Contains("movimiento monetario", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void MapProcTransacciones_ShouldRejectManualReview()
    {
        var result = Map(Decision(NachaSoapOperationCandidate.ProcTransacciones, NachaIncomingDecisionType.ManualReviewRequired, true));

        Assert.False(result.IsExecutable);
        Assert.Contains(result.Errors, x => x.Contains("ManualReviewRequired", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void MapProcTransacciones_ShouldIncludePhase6B5()
    {
        var result = Map(Decision(NachaSoapOperationCandidate.ProcTransacciones, NachaIncomingDecisionType.ApplyCreditMovement, true));

        Assert.Equal("6B.5", result.Payload!.Phase);
    }

    [Fact]
    public void MapProcTransacciones_ShouldCreateSanitizedSummary()
    {
        var result = Map(Decision(NachaSoapOperationCandidate.ProcTransacciones, NachaIncomingDecisionType.ApplyCreditMovement, true));

        Assert.Equal("COP", result.SanitizedSummary["Currency"]);
        Assert.DoesNotContain("1234567890123456", string.Join("|", result.SanitizedSummary.Values));
    }

    [Fact]
    public void MapRegistrarRespuesta_ShouldCreatePayloadWithoutMonetaryMovement()
    {
        var result = Map(Decision(NachaSoapOperationCandidate.RegistrarRespuestaTransaccion, NachaIncomingDecisionType.RegisterDifferentialResponse, false));

        Assert.True(result.IsMapped);
        Assert.False(result.RequiresMonetaryMovement);
        Assert.IsType<NachaSoapRegistrarRespuestaTransaccionPayload>(result.Payload);
    }

    [Fact]
    public void MapRegistrarRespuesta_ShouldRejectMonetaryDecision()
    {
        var result = Map(Decision(NachaSoapOperationCandidate.RegistrarRespuestaTransaccion, NachaIncomingDecisionType.RegisterDifferentialResponse, true));

        Assert.False(result.IsMapped);
        Assert.Contains(result.Errors, x => x.Contains("no puede mapear", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void MapRegistrarRespuesta_ShouldSupportReturnFileDecision()
    {
        var result = Map(Decision(NachaSoapOperationCandidate.RegistrarRespuestaTransaccion, NachaIncomingDecisionType.RegisterDifferentialResponse, false));

        var payload = Assert.IsType<NachaSoapRegistrarRespuestaTransaccionPayload>(result.Payload);
        Assert.Equal("DifferentialResponse", payload.ResponseType);
    }

    [Fact]
    public void MapRegistrarRespuesta_ShouldSupportPrenotificationApproved()
    {
        var result = Map(Decision(NachaSoapOperationCandidate.RegistrarRespuestaTransaccion, NachaIncomingDecisionType.ApprovePrenotification, false));

        var payload = Assert.IsType<NachaSoapRegistrarRespuestaTransaccionPayload>(result.Payload);
        Assert.Equal("PrenotificationApproved", payload.ResponseType);
    }

    [Fact]
    public void MapRegistrarRespuesta_ShouldSupportPrenotificationRejected()
    {
        var result = Map(Decision(NachaSoapOperationCandidate.RegistrarRespuestaTransaccion, NachaIncomingDecisionType.RejectPrenotification, false));

        var payload = Assert.IsType<NachaSoapRegistrarRespuestaTransaccionPayload>(result.Payload);
        Assert.Equal("PrenotificationRejected", payload.ResponseType);
    }

    [Fact]
    public void MapRegistrarRespuesta_ShouldIncludeReasonCodeAndDescription()
    {
        var result = Map(Decision(NachaSoapOperationCandidate.RegistrarRespuestaTransaccion, NachaIncomingDecisionType.RegisterDifferentialResponse, false));

        Assert.Equal("R01", result.Payload!.ReasonCode);
        Assert.Equal("Cuenta insuficiente UAT", result.Payload.ReasonDescription);
    }

    [Fact]
    public void MapRegistrarRespuesta_ShouldIncludePhase6B5()
    {
        var result = Map(Decision(NachaSoapOperationCandidate.RegistrarRespuestaTransaccion, NachaIncomingDecisionType.RegisterDifferentialResponse, false));

        Assert.Equal("6B.5", result.Payload!.Phase);
    }

    [Fact]
    public void MapRegistrarRespuesta_ShouldCreateSanitizedSummary()
    {
        var result = Map(Decision(NachaSoapOperationCandidate.RegistrarRespuestaTransaccion, NachaIncomingDecisionType.RegisterDifferentialResponse, false));

        Assert.Equal("R01", result.SanitizedSummary["ReasonCode"]);
        Assert.Equal("False", result.SanitizedSummary["RequiresMonetaryMovement"]);
    }

    [Fact]
    public void MapNone_ShouldReturnNotMappedAndNotExecutable()
    {
        var result = Map(Decision(NachaSoapOperationCandidate.None, NachaIncomingDecisionType.ManualReviewRequired, false));

        Assert.False(result.IsMapped);
        Assert.False(result.IsExecutable);
        Assert.Contains(result.Warnings, x => x.Contains("None", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void MapManualReview_ShouldReturnNotMappedAndNotExecutable()
    {
        var result = Map(Decision(NachaSoapOperationCandidate.ProcTransacciones, NachaIncomingDecisionType.ManualReviewRequired, true));

        Assert.False(result.IsMapped);
        Assert.False(result.IsExecutable);
    }

    [Fact]
    public void MapManualReview_ShouldNotCreateMonetaryPayload()
    {
        var result = Map(Decision(NachaSoapOperationCandidate.ProcContrapartidas, NachaIncomingDecisionType.ManualReviewRequired, true));

        Assert.Null(result.Payload);
        Assert.False(result.RequiresMonetaryMovement);
    }

    [Fact]
    public void PayloadSummary_ShouldNotExposeCredentials()
    {
        var result = Map(Decision(NachaSoapOperationCandidate.ProcTransacciones, NachaIncomingDecisionType.ApplyCreditMovement, true));

        Assert.DoesNotContain(result.SanitizedSummary.Keys, x => x.Contains("password", StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(result.Metadata.Keys, x => x.Contains("token", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void PayloadSummary_ShouldNotExposeFullAccountNumbers()
    {
        var result = Map(Decision(NachaSoapOperationCandidate.ProcTransacciones, NachaIncomingDecisionType.ApplyCreditMovement, true));

        var payload = Assert.IsType<NachaSoapProcTransaccionesPayload>(result.Payload);
        Assert.Equal("***3456", payload.SourceAccountReference);
        Assert.Equal("***4321", payload.DestinationAccountReference);
        Assert.DoesNotContain("1234567890123456", string.Join("|", result.SanitizedSummary.Values));
    }

    [Fact]
    public void PayloadSummary_ShouldNotSerializeFullPayload()
    {
        var result = Map(Decision(NachaSoapOperationCandidate.ProcTransacciones, NachaIncomingDecisionType.ApplyCreditMovement, true));

        Assert.DoesNotContain("SourceAccountReference", result.SanitizedSummary.Keys);
        Assert.DoesNotContain("DestinationAccountReference", result.SanitizedSummary.Keys);
    }

    [Fact]
    public void PayloadSummary_ShouldIncludeSafeOperationalFields()
    {
        var result = Map(Decision(NachaSoapOperationCandidate.ProcTransacciones, NachaIncomingDecisionType.ApplyCreditMovement, true));

        Assert.Equal("ProcTransacciones", result.SanitizedSummary["OperationCandidate"]);
        Assert.Equal("phase-6b5-payload", result.SanitizedSummary["CorrelationId"]);
        Assert.Equal("150000", result.SanitizedSummary["AmountInCents"]);
        Assert.Equal("6B.5", result.SanitizedSummary["Phase"]);
    }

    [Fact]
    public void SoapRequestMapper_ShouldAttachPayloadMappingMetadata()
    {
        var request = ExecutionRequest(NachaSoapOperationCandidate.ProcTransacciones, NachaIncomingDecisionType.ApplyCreditMovement, true);
        var mapped = new NachaSoapRequestMapper(new NachaSoapPayloadMapper()).Map(request);

        Assert.NotNull(mapped.PayloadMapping);
        Assert.Equal("NachaSoapProcTransaccionesPayload", mapped.Payload["PayloadType"]);
        Assert.Equal("True", mapped.Payload["PayloadIsMapped"]);
    }

    [Fact]
    public async Task DryRunExecutor_ShouldKeepPayloadSanitized()
    {
        var request = ExecutionRequest(NachaSoapOperationCandidate.ProcTransacciones, NachaIncomingDecisionType.ApplyCreditMovement, true);
        var result = await new NachaSoapDryRunOperationExecutor(new NachaSoapRequestMapper(new NachaSoapPayloadMapper())).ExecuteAsync(request);

        Assert.False(result.SoapWasInvoked);
        Assert.NotNull(result.MappedRequest!.PayloadMapping);
        Assert.DoesNotContain("1234567890123456", string.Join("|", result.MappedRequest.PayloadMapping!.SanitizedSummary.Values));
    }

    [Fact]
    public async Task PayloadMapping_ShouldFeedDryRunExecutionWithoutRealSoap()
    {
        var request = ExecutionRequest(NachaSoapOperationCandidate.RegistrarRespuestaTransaccion, NachaIncomingDecisionType.RegisterDifferentialResponse, false);
        var result = await new NachaSoapDryRunOperationExecutor(new NachaSoapRequestMapper(new NachaSoapPayloadMapper())).ExecuteAsync(request);

        Assert.Equal(NachaSoapExecutionStatus.DryRunCompleted, result.Status);
        Assert.False(result.SoapWasInvoked);
        Assert.True(result.MappedRequest!.PayloadMapping!.IsMapped);
    }

    [Fact]
    public async Task IncomingProcessorProcTransaccionesDecision_ShouldMapToSoapPayloadAndDryRun()
    {
        var decision = Decision(NachaSoapOperationCandidate.ProcTransacciones, NachaIncomingDecisionType.ApplyCreditMovement, true);
        var request = ExecutionRequest(decision);

        var result = await new NachaSoapDryRunOperationExecutor(new NachaSoapRequestMapper(new NachaSoapPayloadMapper())).ExecuteAsync(request);

        Assert.Equal(NachaSoapExecutionStatus.DryRunCompleted, result.Status);
        Assert.Equal("NachaSoapProcTransaccionesPayload", result.MappedRequest!.PayloadMapping!.PayloadType);
    }

    [Fact]
    public async Task IncomingProcessorRegistrarRespuestaDecision_ShouldMapToSoapPayloadAndDryRun()
    {
        var decision = Decision(NachaSoapOperationCandidate.RegistrarRespuestaTransaccion, NachaIncomingDecisionType.RegisterDifferentialResponse, false);
        var request = ExecutionRequest(decision);

        var result = await new NachaSoapDryRunOperationExecutor(new NachaSoapRequestMapper(new NachaSoapPayloadMapper())).ExecuteAsync(request);

        Assert.Equal(NachaSoapExecutionStatus.DryRunCompleted, result.Status);
        Assert.Equal("NachaSoapRegistrarRespuestaTransaccionPayload", result.MappedRequest!.PayloadMapping!.PayloadType);
    }

    private static NachaSoapPayloadMappingResult Map(NachaIncomingDecision decision)
        => new NachaSoapPayloadMapper().Map(decision, Context());

    private static NachaSoapExecutionRequest ExecutionRequest(
        NachaSoapOperationCandidate operation,
        NachaIncomingDecisionType decisionType,
        bool requiresMonetaryMovement)
        => ExecutionRequest(Decision(operation, decisionType, requiresMonetaryMovement));

    private static NachaSoapExecutionRequest ExecutionRequest(NachaIncomingDecision decision)
        => new()
        {
            Decision = decision,
            CorrelationId = "phase-6b5-payload",
            ClearingHouseCode = "ACH",
            ProfileCode = "OFFICIAL_ACH_ENTRADA_ORIGINAL_V1_0",
            RequestedBy = "test",
            IsEnabled = true,
            DryRun = true,
            PayloadContext = Context(),
            Metadata = new Dictionary<string, string>
            {
                ["case"] = "payload-test",
                ["token"] = "secret-token"
            }
        };

    private static NachaIncomingDecision Decision(
        NachaSoapOperationCandidate operation,
        NachaIncomingDecisionType decisionType,
        bool requiresMonetaryMovement)
        => new()
        {
            EntryTraceNumber = "123456780000001",
            OriginalTraceNumber = "123456780000000",
            TransactionId = decisionType == NachaIncomingDecisionType.ApprovePrenotification ? null : 1234,
            PrenotificationId = decisionType is NachaIncomingDecisionType.ApprovePrenotification or NachaIncomingDecisionType.RejectPrenotification ? 44 : null,
            DecisionType = decisionType,
            RequiresMonetaryMovement = requiresMonetaryMovement,
            SoapOperation = operation,
            ReasonCode = "R01",
            ReasonDescription = "Cuenta insuficiente UAT",
            NewInternalStatus = "Accepted",
            AuditMessage = "UAT 6B.5.2"
        };

    private static NachaSoapExecutionContext Context()
        => new()
        {
            CorrelationId = "phase-6b5-payload",
            SourceFileName = "ACH_COL_IN_001.ach",
            ClearingHouseCode = "ACH",
            ProfileCode = "OFFICIAL_ACH_ENTRADA_ORIGINAL_V1_0",
            AmountInCents = 150000,
            Currency = "COP",
            SourceAccountReference = "1234567890123456",
            DestinationAccountReference = "6543210009874321",
            SourceFinancialInstitutionCode = "76543210",
            DestinationFinancialInstitutionCode = "12345678",
            ExternalOriginatorInstitutionCode = "76543210",
            CfaReceiverInstitutionCode = "12345678",
            CreatedAt = new DateTime(2026, 5, 24, 12, 0, 0, DateTimeKind.Utc),
            Metadata = new Dictionary<string, string>
            {
                ["scenario"] = "uat",
                ["password"] = "no-debe-salir",
                ["accountHint"] = "1234567890123456"
            }
        };
}

using Cfa.ACHInterbank.Application.ACH.Models;
using Cfa.ACHInterbank.Persistence.ACH.Services.Implementation;
using Xunit;

namespace Cfa.ACHInterbank.Tests;

public class NachaSoapUatOperationalControlTests
{
    [Fact]
    public void UatReadinessChecker_ShouldReturnReadyForSafeUatConfiguration()
    {
        var result = Readiness().CheckReadiness("uat-ready", Options(), [Endpoint()]);

        Assert.True(result.IsReady);
        Assert.False(result.IsBlocked);
    }

    [Fact]
    public void UatReadinessChecker_ShouldBlockProductiveExecution()
    {
        var result = Readiness().CheckReadiness("uat-prod", Options(productive: true), [Endpoint()]);

        Assert.True(result.IsBlocked);
        Assert.Contains("ProductiveExecution", result.BlockReason);
    }

    [Fact]
    public void UatReadinessChecker_ShouldBlockRealInvocationWhenNoGo()
    {
        var result = Readiness().CheckReadiness("uat-real", Options(allowReal: true), [Endpoint()]);

        Assert.True(result.IsBlocked);
        Assert.Contains("AllowRealSoapInvocation", result.BlockReason);
    }

    [Fact]
    public void UatReadinessChecker_ShouldBlockProductionEndpoint()
    {
        var result = Readiness().CheckReadiness("uat-prod-endpoint", Options(), [Endpoint(production: true, url: "https://ach-prod.example.invalid/soap")]);

        Assert.True(result.IsBlocked);
        Assert.Contains("endpoints", result.BlockReason, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void UatReadinessChecker_ShouldIncludePhase6B5()
    {
        var result = Readiness().CheckReadiness("uat-phase", Options(), [Endpoint()]);

        Assert.Equal("6B.5", result.Phase);
        Assert.Equal("true", result.FeatureFlagChecks["AchSoap:BlockByNoGo"]);
    }

    [Fact]
    public void UatReadinessChecker_ShouldSanitizeEndpoint()
    {
        var result = Readiness().CheckReadiness("uat-sanitize", Options(), [Endpoint(url: "https://uat-bank-gateway.example.invalid/soap/private")]);

        Assert.DoesNotContain("/soap/private", result.EndpointChecks[0].SanitizedEndpoint);
        Assert.Contains("***", result.EndpointChecks[0].SanitizedEndpoint);
    }

    [Fact]
    public void EndpointSafetyValidator_ShouldAllowUatEndpoint()
    {
        var result = new NachaSoapEndpointSafetyValidator().Validate(Endpoint(), Options());

        Assert.True(result.IsSafeForUat);
        Assert.False(result.IsBlocked);
    }

    [Fact]
    public void EndpointSafetyValidator_ShouldBlockProductionEndpoint()
    {
        var result = new NachaSoapEndpointSafetyValidator().Validate(Endpoint(production: true, url: "https://production.example.invalid/soap"), Options());

        Assert.True(result.IsBlocked);
        Assert.True(result.IsProductionEndpoint);
    }

    [Fact]
    public void EndpointSafetyValidator_ShouldBlockMissingEndpointWhenRequired()
    {
        var result = new NachaSoapEndpointSafetyValidator().Validate(Endpoint(url: ""), Options());

        Assert.True(result.IsBlocked);
        Assert.Contains("requerido", result.BlockReason);
    }

    [Fact]
    public void EndpointSafetyValidator_ShouldSanitizeEndpointUrl()
    {
        var result = new NachaSoapEndpointSafetyValidator().Validate(Endpoint(url: "https://uat-secret-host.example.invalid/path/query"), Options());

        Assert.StartsWith("https://", result.SanitizedEndpoint);
        Assert.EndsWith("/***", result.SanitizedEndpoint);
    }

    [Fact]
    public void EndpointSafetyValidator_ShouldRejectUnknownEnvironment()
    {
        var result = new NachaSoapEndpointSafetyValidator().Validate(Endpoint(uat: false, production: false), Options());

        Assert.True(result.IsBlocked);
        Assert.Contains("desconocido", result.BlockReason);
    }

    [Fact]
    public void CertificateReadiness_ShouldPassWhenCertificateMetadataIsPresent()
    {
        var result = new NachaSoapCertificateReadinessValidator().Validate(Endpoint(), Options());

        Assert.False(result.IsBlocked);
        Assert.True(result.CertificateAvailable);
    }

    [Fact]
    public void CertificateReadiness_ShouldWarnWhenCertificateMissingForUat()
    {
        var result = Readiness().CheckReadiness("uat-cert-warning", Options(requireCertificate: false), [Endpoint(cert: false)]);

        Assert.True(result.IsReady);
        Assert.Contains(result.Warnings, x => x.Contains("certificado", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void CertificateReadiness_ShouldBlockWhenCertificateRequiredAndMissing()
    {
        var result = new NachaSoapCertificateReadinessValidator().Validate(Endpoint(cert: false), Options(requireCertificate: true));

        Assert.True(result.IsBlocked);
        Assert.Contains("certificado", result.BlockReason);
    }

    [Fact]
    public void CertificateReadiness_ShouldSanitizeThumbprint()
    {
        var result = new NachaSoapCertificateReadinessValidator().Validate(Endpoint(), Options());

        Assert.Contains("***", result.SanitizedThumbprint);
        Assert.DoesNotContain("AABBCCDDEEFF00112233445566778899", result.SanitizedThumbprint);
    }

    [Fact]
    public void CertificateReadiness_ShouldNotAccessRealCertificateStoreByDefault()
    {
        var result = new NachaSoapCertificateReadinessValidator().Validate(Endpoint(), Options());

        Assert.Equal("false", result.Metadata["RealCertificateStoreAccess"]);
        Assert.False(result.PrivateKeyAccessible);
    }

    [Fact]
    public void OperationalGate_ShouldAllowDryRun()
    {
        var result = Gate().Evaluate(Request(), Options(mode: NachaSoapOperationalMode.DryRun), [Endpoint()]);

        Assert.True(result.IsAllowed);
    }

    [Fact]
    public void OperationalGate_ShouldAllowSimulated()
    {
        var result = Gate().Evaluate(Request(), Options(mode: NachaSoapOperationalMode.Simulated), [Endpoint()]);

        Assert.True(result.IsAllowed);
    }

    [Fact]
    public void OperationalGate_ShouldAllowUatReadiness()
    {
        var result = Gate().Evaluate(Request(), Options(mode: NachaSoapOperationalMode.UatReadiness), [Endpoint()]);

        Assert.True(result.IsAllowed);
    }

    [Fact]
    public void OperationalGate_ShouldBlockRealSoapInvocation()
    {
        var result = Gate().Evaluate(Request(), Options(allowReal: true), [Endpoint()]);

        Assert.True(result.IsBlocked);
        Assert.Contains("SOAP real", result.BlockReason);
    }

    [Fact]
    public void OperationalGate_ShouldBlockProductiveExecution()
    {
        var result = Gate().Evaluate(Request(), Options(productive: true), [Endpoint()]);

        Assert.True(result.IsBlocked);
        Assert.Contains("ProductiveExecution", result.BlockReason);
    }

    [Fact]
    public void OperationalGate_ShouldBlockProductionEndpoint()
    {
        var result = Gate().Evaluate(Request(), Options(), [Endpoint(production: true, url: "https://prod.example.invalid/soap")]);

        Assert.True(result.IsBlocked);
        Assert.Contains("Endpoint productivo", result.BlockReason);
    }

    [Fact]
    public void OperationalGate_ShouldBlockMonetaryOperationWhenProductive()
    {
        var result = Gate().Evaluate(Request(monetary: true), Options(productive: true), [Endpoint()]);

        Assert.True(result.IsBlocked);
        Assert.Contains("monetario", result.BlockReason, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void OperationalGate_ShouldRequireManualApprovalWhenConfigured()
    {
        var result = Gate().Evaluate(Request(), Options(requireApproval: true, approval: false), [Endpoint()]);

        Assert.True(result.IsBlocked);
        Assert.Contains("Aprobacion manual", result.BlockReason);
    }

    [Fact]
    public void OperationalGate_ShouldReturnAuditableBlockReason()
    {
        var result = Gate().Evaluate(Request(), Options(allowReal: true), [Endpoint()]);

        Assert.Equal("6B.5", result.Audit["Phase"]);
        Assert.False(string.IsNullOrWhiteSpace(result.Audit["BlockReason"]));
    }

    [Fact]
    public async Task BlockedRealSoapClientAdapter_ShouldNotInvokeSoap()
    {
        var result = await new NachaBlockedRealSoapClientAdapter().ExecuteProcTransaccionesAsync(ProcTransaccionesPayload(), Options());

        Assert.False(result.SoapWasInvoked);
        Assert.False(result.WasExecuted);
    }

    [Fact]
    public async Task BlockedRealSoapClientAdapter_ShouldReturnBlockedByNoGo()
    {
        var result = await new NachaBlockedRealSoapClientAdapter().ExecuteProcTransaccionesAsync(ProcTransaccionesPayload(), Options());

        Assert.Equal(NachaSoapExecutionStatus.BlockedByNoGo, result.Status);
        Assert.Contains("NO-GO", result.Message);
    }

    [Fact]
    public async Task BlockedRealSoapClientAdapter_ShouldIncludePhase6B5()
    {
        var result = await new NachaBlockedRealSoapClientAdapter().ExecuteProcContrapartidasAsync(ProcContrapartidasPayload(), Options());

        Assert.Equal("6B.5", result.Phase);
        Assert.Equal("6B.5", result.Trace["Phase"]);
    }

    [Fact]
    public async Task BlockedRealSoapClientAdapter_ShouldNotRequireCredentials()
    {
        var result = await new NachaBlockedRealSoapClientAdapter().ExecuteRegistrarRespuestaTransaccionAsync(RegistrarPayload(), Options(metadata: new Dictionary<string, string>()));

        Assert.Equal(NachaSoapExecutionStatus.BlockedByNoGo, result.Status);
        Assert.DoesNotContain("credential", string.Join("|", result.RequestSummary.Values), StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task BlockedRealSoapClientAdapter_ShouldNotExposeSensitiveData()
    {
        var result = await new NachaBlockedRealSoapClientAdapter().ExecuteProcTransaccionesAsync(ProcTransaccionesPayload(), Options(metadata: new Dictionary<string, string> { ["token"] = "secret" }));

        var joined = string.Join("|", result.RequestSummary.Values.Concat(result.ResponseSummary.Values));
        Assert.DoesNotContain("secret", joined, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("token", joined, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task ResilientExecutor_ShouldRemainCompatibleWithOperationalGate()
    {
        var gate = Gate().Evaluate(Request(), Options(mode: NachaSoapOperationalMode.Simulated), [Endpoint()]);
        var executor = new NachaSoapResilientExecutor(
            Gateway(),
            new NachaSoapInMemoryIdempotencyStore(),
            new NachaSoapInMemoryAttemptAuditor(),
            new NachaSoapResiliencePolicyEvaluator());

        var result = await executor.ExecuteAsync(Request(), Context(), new NachaSoapRetryPolicy());

        Assert.True(gate.IsAllowed);
        Assert.True(result.IsSuccess);
    }

    [Fact]
    public async Task SimulatedGateway_ShouldStillWorkWhenUatControlExists()
    {
        var result = await Gateway().ExecuteAsync(Request(), Context());

        Assert.Equal(NachaSoapExecutionStatus.SimulatedSuccess, result.Status);
        Assert.False(result.SoapWasInvoked);
    }

    [Fact]
    public void ExistingSoapTests_ShouldContinuePassing()
    {
        var readiness = Readiness().CheckReadiness("compat", Options(), [Endpoint()]);

        Assert.True(readiness.IsReady);
        Assert.Equal("NO-GO", readiness.SecurityChecks["Productivo"]);
    }

    private static NachaSoapUatReadinessChecker Readiness()
        => new(new NachaSoapEndpointSafetyValidator(), new NachaSoapCertificateReadinessValidator());

    private static NachaSoapOperationalGate Gate()
        => new(new NachaSoapEndpointSafetyValidator());

    private static NachaSoapSimulatedGateway Gateway()
        => new([new NachaSoapMockOperationAdapter()], new NachaSoapRequestMapper(new NachaSoapPayloadMapper()));

    private static NachaSoapUatControlOptions Options(
        bool productive = false,
        bool allowReal = false,
        bool allowProduction = false,
        bool requireCertificate = true,
        bool requireApproval = false,
        bool approval = false,
        NachaSoapOperationalMode mode = NachaSoapOperationalMode.UatReadiness,
        IReadOnlyDictionary<string, string>? metadata = null)
        => new()
        {
            Enabled = true,
            EnvironmentName = "UAT",
            ProductiveExecution = productive,
            AllowRealSoapInvocation = allowReal,
            AllowMonetaryOperations = false,
            AllowUatEndpoints = true,
            AllowProductionEndpoints = allowProduction,
            RequireCertificateValidation = requireCertificate,
            RequireManualApproval = requireApproval,
            ManualApprovalGranted = approval,
            Mode = mode,
            Metadata = metadata ?? new Dictionary<string, string> { ["case"] = "uat-control" }
        };

    private static NachaSoapEndpointDescriptor Endpoint(
        bool production = false,
        bool uat = true,
        bool cert = true,
        string url = "https://uat-ach-gateway.example.invalid/soap")
        => new()
        {
            OperationCandidate = NachaSoapOperationCandidate.ProcTransacciones,
            EnvironmentName = production ? "Production" : "UAT",
            EndpointName = "ACH-UAT-SOAP",
            EndpointUrl = url,
            IsProduction = production,
            IsUat = uat,
            IsEnabled = true,
            RequiresClientCertificate = cert,
            CertificateThumbprint = cert ? "AABBCCDDEEFF00112233445566778899" : string.Empty,
            CertificateStoreName = cert ? "My" : string.Empty,
            CertificateStoreLocation = cert ? "CurrentUser" : string.Empty,
            Metadata = new Dictionary<string, string> { ["owner"] = "uat" }
        };

    private static NachaSoapExecutionRequest Request(
        bool monetary = true,
        NachaSoapOperationCandidate operation = NachaSoapOperationCandidate.ProcTransacciones)
        => new()
        {
            Decision = new NachaIncomingDecision
            {
                EntryTraceNumber = "123456780000001",
                OriginalTraceNumber = "123456780000000",
                TransactionId = 123,
                DecisionType = NachaIncomingDecisionType.ApplyCreditMovement,
                RequiresMonetaryMovement = monetary,
                SoapOperation = operation,
                ReasonCode = "R01",
                ReasonDescription = "UAT"
            },
            CorrelationId = "phase-6b5-uat",
            ClearingHouseCode = "ACH",
            ProfileCode = "OFFICIAL_ACH_ENTRADA_ORIGINAL_V1_0",
            IsEnabled = true,
            DryRun = true,
            PayloadContext = Context(),
            SimulationScenario = new NachaSoapSimulationScenario { SimulatedExternalReference = "SIM-UAT" },
            SimulationOptions = new NachaSoapSimulationOptions()
        };

    private static NachaSoapExecutionContext Context()
        => new()
        {
            CorrelationId = "phase-6b5-uat",
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
            CreatedAt = new DateTime(2026, 5, 24, 12, 0, 0, DateTimeKind.Utc)
        };

    private static NachaSoapProcTransaccionesPayload ProcTransaccionesPayload()
        => new()
        {
            CorrelationId = "phase-6b5-uat",
            OperationCandidate = NachaSoapOperationCandidate.ProcTransacciones,
            TransactionId = 123,
            AmountInCents = 150000,
            SourceFileName = "ACH_COL_IN_001.ach"
        };

    private static NachaSoapProcContrapartidasPayload ProcContrapartidasPayload()
        => new()
        {
            CorrelationId = "phase-6b5-uat",
            OperationCandidate = NachaSoapOperationCandidate.ProcContrapartidas,
            TransactionId = 123,
            AmountInCents = 150000,
            SourceFileName = "ACH_COL_IN_001.ach"
        };

    private static NachaSoapRegistrarRespuestaTransaccionPayload RegistrarPayload()
        => new()
        {
            CorrelationId = "phase-6b5-uat",
            OperationCandidate = NachaSoapOperationCandidate.RegistrarRespuestaTransaccion,
            TransactionId = 123,
            ReasonCode = "R01",
            ReasonDescription = "UAT"
        };
}

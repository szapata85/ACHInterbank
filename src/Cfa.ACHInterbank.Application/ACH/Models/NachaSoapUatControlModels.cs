namespace Cfa.ACHInterbank.Application.ACH.Models;

public sealed class NachaSoapUatControlOptions
{
    public bool Enabled { get; init; } = true;
    public string EnvironmentName { get; init; } = "UAT";
    public bool ProductiveExecution { get; init; }
    public bool AllowRealSoapInvocation { get; init; }
    public bool AllowMonetaryOperations { get; init; }
    public bool AllowUatEndpoints { get; init; } = true;
    public bool AllowProductionEndpoints { get; init; }
    public bool RequireCertificateValidation { get; init; } = true;
    public bool RequireExplicitNoGoOverride { get; init; } = true;
    public bool RequireManualApproval { get; init; }
    public bool ManualApprovalGranted { get; init; }
    public bool RequireSafeEndpoint { get; init; } = true;
    public bool RequireDryRunBeforeRealInvocation { get; init; } = true;
    public int MaxAllowedAttempts { get; init; } = 3;
    public NachaSoapOperationalMode Mode { get; init; } = NachaSoapOperationalMode.UatReadiness;
    public IReadOnlyDictionary<string, string> Metadata { get; init; } = new Dictionary<string, string>();
}

public sealed class NachaSoapEndpointDescriptor
{
    public NachaSoapOperationCandidate OperationCandidate { get; init; } = NachaSoapOperationCandidate.None;
    public string EnvironmentName { get; init; } = "UAT";
    public string EndpointName { get; init; } = string.Empty;
    public string EndpointUrl { get; init; } = string.Empty;
    public bool IsProduction { get; init; }
    public bool IsUat { get; init; } = true;
    public bool IsEnabled { get; init; } = true;
    public bool RequiresClientCertificate { get; init; }
    public string CertificateThumbprint { get; init; } = string.Empty;
    public string CertificateStoreName { get; init; } = string.Empty;
    public string CertificateStoreLocation { get; init; } = string.Empty;
    public int TimeoutMs { get; init; } = 30_000;
    public IReadOnlyDictionary<string, string> Metadata { get; init; } = new Dictionary<string, string>();
}

public sealed class NachaSoapReadinessCheckResult
{
    public string CorrelationId { get; init; } = string.Empty;
    public string EnvironmentName { get; init; } = string.Empty;
    public bool IsReady { get; init; }
    public bool IsBlocked { get; init; }
    public string BlockReason { get; init; } = string.Empty;
    public bool ProductiveExecution { get; init; }
    public bool AllowRealSoapInvocation { get; init; }
    public bool AllowMonetaryOperations { get; init; }
    public IReadOnlyList<NachaSoapEndpointCheckResult> EndpointChecks { get; init; } = [];
    public IReadOnlyList<NachaSoapCertificateCheckResult> CertificateChecks { get; init; } = [];
    public IReadOnlyDictionary<string, string> FeatureFlagChecks { get; init; } = new Dictionary<string, string>();
    public IReadOnlyDictionary<string, string> SecurityChecks { get; init; } = new Dictionary<string, string>();
    public IReadOnlyList<string> Warnings { get; init; } = [];
    public IReadOnlyList<string> Errors { get; init; } = [];
    public string Phase { get; init; } = "6B.5";
    public IReadOnlyDictionary<string, string> Metadata { get; init; } = new Dictionary<string, string>();
}

public sealed class NachaSoapEndpointCheckResult
{
    public NachaSoapOperationCandidate OperationCandidate { get; init; } = NachaSoapOperationCandidate.None;
    public string EndpointName { get; init; } = string.Empty;
    public bool IsConfigured { get; init; }
    public bool IsEnabled { get; init; }
    public bool IsSafeForUat { get; init; }
    public bool IsProductionEndpoint { get; init; }
    public bool IsBlocked { get; init; }
    public string BlockReason { get; init; } = string.Empty;
    public string SanitizedEndpoint { get; init; } = string.Empty;
    public IReadOnlyDictionary<string, string> Metadata { get; init; } = new Dictionary<string, string>();
}

public sealed class NachaSoapCertificateCheckResult
{
    public bool RequiresClientCertificate { get; init; }
    public bool HasThumbprint { get; init; }
    public bool HasStoreLocation { get; init; }
    public bool CertificateAvailable { get; init; }
    public bool PrivateKeyAccessible { get; init; }
    public bool IsBlocked { get; init; }
    public string BlockReason { get; init; } = string.Empty;
    public string SanitizedThumbprint { get; init; } = string.Empty;
    public IReadOnlyDictionary<string, string> Metadata { get; init; } = new Dictionary<string, string>();
}

public sealed class NachaSoapOperationalGateResult
{
    public string CorrelationId { get; init; } = string.Empty;
    public NachaSoapOperationCandidate OperationCandidate { get; init; } = NachaSoapOperationCandidate.None;
    public NachaSoapOperationalMode Mode { get; init; } = NachaSoapOperationalMode.Disabled;
    public bool IsAllowed { get; init; }
    public bool IsBlocked { get; init; }
    public string BlockReason { get; init; } = string.Empty;
    public bool ProductiveExecution { get; init; }
    public bool WouldInvokeRealSoap { get; init; }
    public string Phase { get; init; } = "6B.5";
    public IReadOnlyDictionary<string, string> Audit { get; init; } = new Dictionary<string, string>();
}

public sealed class NachaSoapReadinessAudit
{
    public string CorrelationId { get; init; } = string.Empty;
    public string EnvironmentName { get; init; } = string.Empty;
    public string ReadinessStatus { get; init; } = string.Empty;
    public IReadOnlyList<string> BlockReasons { get; init; } = [];
    public IReadOnlyDictionary<string, string> FeatureFlags { get; init; } = new Dictionary<string, string>();
    public IReadOnlyList<NachaSoapEndpointCheckResult> EndpointSafetySummary { get; init; } = [];
    public IReadOnlyList<NachaSoapCertificateCheckResult> CertificateReadinessSummary { get; init; } = [];
    public bool ProductiveExecution { get; init; }
    public string Phase { get; init; } = "6B.5";
    public DateTime Timestamp { get; init; }
    public IReadOnlyDictionary<string, string> Metadata { get; init; } = new Dictionary<string, string>();
}

public enum NachaSoapOperationalMode
{
    Disabled = 0,
    DryRun = 1,
    Simulated = 2,
    UatReadiness = 3,
    UatControlled = 4
}

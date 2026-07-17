using Cfa.ACHInterbank.Domain.Models.ACH;
using Cfa.ACHInterbank.Domain.Entities.Integrations;

namespace Cfa.ACHInterbank.Application.ACH.Models;

public sealed record ContrapartidaDispatchBatchCreateRequest(
    string AchCycleId,
    int ClearingHouseId,
    int? AchBatchId,
    ContrapartidaDispatchBatchTriggerTypeEnum TriggerType,
    string RequestedBy,
    string? JobId = null,
    string? RequestPayloadXml = null);

public sealed record ContrapartidaDispatchAttemptCreateRequest(
    long DispatchItemId,
    Guid? DispatchBatchId,
    DateTime StartedAtUtc,
    DateTime? FinishedAtUtc,
    ContrapartidaDispatchAttemptResultEnum Result,
    string CorrelationId,
    string TriggeredBy,
    bool RetryEligible,
    string? ExternalResponseCode = null,
    string? ExternalResponseMessage = null,
    string? ErrorCode = null,
    string? ErrorMessage = null,
    string? RequestPayloadXml = null,
    string? ResponsePayloadXml = null,
    string? SoapMethodName = null,
    string? SoapEndpoint = null,
    string? ExecutionMode = null,
    long DurationMs = 0,
    string? SoapResponseCode = null,
    string? SoapResponseDescription = null,
    string? SoapTechnicalStatus = null,
    long? ResponseCatalogId = null,
    IntegrationTransportStatus TransportStatus = IntegrationTransportStatus.NotExecuted,
    IntegrationResponseBusinessStatus BusinessStatus = IntegrationResponseBusinessStatus.Unknown,
    bool RetryAllowed = false,
    bool RequiresManualReview = false,
    DateTime? ProcessedAtUtc = null,
    bool IsSuccessful = false,
    bool IsFunctionalRejection = false,
    bool IsTechnicalFailure = false,
    string? TechnicalException = null);

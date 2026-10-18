using Cfa.ACHInterbank.Domain.Models.ACH;

namespace Cfa.ACHInterbank.Application.ACH.Models;

public sealed record AchColombiaCycleOperationalScheduleAdministrationRowDto(
    int ClearingHouseId,
    int ClearingHouseCycleConfigId,
    string PolicyVersion,
    string CycleName,
    TimeSpan RegulatoryStartTime,
    TimeSpan RegulatoryCutoffTime,
    bool RegulatoryCycleActive,
    int? ScheduleId,
    bool AutomaticOutboundEnabled,
    TimeSpan? AutomaticOutboundStartTime,
    TimeSpan? AutomaticOutboundLatestStartTime,
    AchColombiaOutboundExecutionStrategy OutboundExecutionStrategy,
    string EffectiveTimeZoneId,
    bool IsActive,
    string? RowVersion);

public sealed record AchColombiaCycleOperationalScheduleFilter(
    int? ClearingHouseCycleConfigId = null,
    bool? IsActive = null);

public sealed record AchColombiaCycleOperationalScheduleDto(
    int Id,
    int ClearingHouseId,
    string ClearingHouseCode,
    int ClearingHouseCycleConfigId,
    string PolicyVersion,
    string CycleName,
    bool AutomaticOutboundEnabled,
    TimeSpan? AutomaticOutboundStartTime,
    TimeSpan? AutomaticOutboundLatestStartTime,
    AchColombiaOutboundExecutionStrategy OutboundExecutionStrategy,
    string EffectiveTimeZoneId,
    bool IsActive,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt,
    string RowVersion);

public sealed record CreateAchColombiaCycleOperationalScheduleRequest(
    int ClearingHouseId,
    int ClearingHouseCycleConfigId,
    bool AutomaticOutboundEnabled,
    TimeSpan? AutomaticOutboundStartTime,
    TimeSpan? AutomaticOutboundLatestStartTime,
    AchColombiaOutboundExecutionStrategy OutboundExecutionStrategy,
    string EffectiveTimeZoneId,
    bool IsActive = true);

public sealed record UpdateAchColombiaCycleOperationalScheduleRequest(
    int ClearingHouseId,
    int ClearingHouseCycleConfigId,
    bool AutomaticOutboundEnabled,
    TimeSpan? AutomaticOutboundStartTime,
    TimeSpan? AutomaticOutboundLatestStartTime,
    AchColombiaOutboundExecutionStrategy OutboundExecutionStrategy,
    string EffectiveTimeZoneId,
    bool IsActive,
    string ExpectedRowVersion);

public sealed record DeactivateAchColombiaCycleOperationalScheduleRequest(string ExpectedRowVersion);

public sealed record AchColombiaCycleOperationalScheduleApiError(
    string Code,
    string Message,
    string? CurrentRowVersion = null);

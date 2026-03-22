using Cfa.ACHInterbank.Domain.Entities.Ach.Dtos;

namespace Cfa.ACHInterbank.Application.ACH.Interfaces;

public sealed record NachaValidationContext(
    AchCycleDto Cycle,
    ClearingHouseDto ClearingHouse);

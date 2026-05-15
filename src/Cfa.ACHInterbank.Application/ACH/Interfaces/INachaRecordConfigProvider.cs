using Cfa.ACHInterbank.Application.ACH.Models;

namespace Cfa.ACHInterbank.Application.ACH.Interfaces;

public interface INachaRecordConfigProvider
{
    NachaRailRecordConfig Resolve(int? clearingHouseId, string? clearingHouseCode, NachaRecordFlow flow, NachaRecordDirection direction);
}

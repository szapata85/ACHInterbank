namespace Cfa.ACHInterbank.Application.ACH.Models;

public enum NachaRecordFlow
{
    ReturnOut = 1,
    ReturnOfReturnOut = 2
}

public enum NachaRecordDirection
{
    Outbound = 1
}

public sealed record NachaRailRecordConfig(
    string RailCode,
    int? ClearingHouseId,
    NachaRecordFlow Flow,
    NachaRecordDirection Direction,
    bool IsCurrentLayout,
    bool IsProductiveApproved,
    NachaRecord1Config Record1,
    NachaRecord5Config Record5,
    NachaRecord7Config Record7,
    NachaRecord89Config Record89);

public sealed record NachaRecord1Config(
    string ImmediateDestination,
    string ImmediateOrigin,
    string ImmediateDestinationName,
    string ImmediateOriginName,
    string FileIdModifier,
    string ReferenceCode,
    int RecordSize,
    int BlockingFactor,
    int FormatCode);

public sealed record NachaRecord5Config(
    int? ServiceClassCodeOverride,
    string CompanyName,
    string CompanyIdentification,
    string StandardEntryClassCode,
    string CompanyEntryDescription,
    string OriginatorStatusCode,
    string OriginatingDfi,
    string BatchNumberDefault);

public sealed record NachaRecord7Config(
    string AddendaTypeCode,
    string ReturnReasonCodeSourceStrategy,
    string OriginalTraceSourceStrategy);

public sealed record NachaRecord89Config(
    string CompanyIdentification,
    string OriginatingDfi,
    string BatchNumber,
    string PaddingStrategy);

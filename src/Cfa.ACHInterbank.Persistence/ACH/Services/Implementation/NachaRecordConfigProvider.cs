using Cfa.ACHInterbank.Application.ACH.Interfaces;
using Cfa.ACHInterbank.Application.ACH.Models;

namespace Cfa.ACHInterbank.Persistence.ACH.Services.Implementation;

[Scoped]
public class NachaRecordConfigProvider : INachaRecordConfigProvider
{
    private const string RailAch = "ACH";
    private const string RailCenit = "CENIT";

    public NachaRailRecordConfig Resolve(int? clearingHouseId, string? clearingHouseCode, NachaRecordFlow flow, NachaRecordDirection direction)
    {
        var normalizedRail = NormalizeRail(clearingHouseCode);
        return BuildCurrentLayoutConfig(normalizedRail, clearingHouseId, flow, direction);
    }

    private static string NormalizeRail(string? rail)
    {
        if (string.Equals(rail, RailAch, StringComparison.OrdinalIgnoreCase)) return RailAch;
        if (string.Equals(rail, RailCenit, StringComparison.OrdinalIgnoreCase)) return RailCenit;
        return "UNKNOWN";
    }

    private static NachaRailRecordConfig BuildCurrentLayoutConfig(string rail, int? clearingHouseId, NachaRecordFlow flow, NachaRecordDirection direction)
        => new(
            RailCode: rail,
            ClearingHouseId: clearingHouseId,
            Flow: flow,
            Direction: direction,
            IsCurrentLayout: true,
            IsProductiveApproved: false,
            Record1: new NachaRecord1Config(
                ImmediateDestination: "000101006",
                ImmediateOrigin: "000101006",
                ImmediateDestinationName: "ACH COLOMBIA",
                ImmediateOriginName: flow == NachaRecordFlow.ReturnOfReturnOut ? "ACHINTERBANK ROR" : "DEVOLUCIONES",
                FileIdModifier: "A",
                ReferenceCode: "",
                RecordSize: 106,
                BlockingFactor: 10,
                FormatCode: 1),
            Record5: new NachaRecord5Config(
                ServiceClassCodeOverride: null,
                CompanyName: flow == NachaRecordFlow.ReturnOfReturnOut ? "DEV. DEV." : "DEVOLUCIONES",
                CompanyIdentification: flow == NachaRecordFlow.ReturnOfReturnOut ? "BANCROR" : "BANCORET",
                StandardEntryClassCode: "PPD",
                CompanyEntryDescription: flow == NachaRecordFlow.ReturnOfReturnOut ? "RETORNO" : "DEVOLUCIONES",
                OriginatorStatusCode: "1",
                OriginatingDfi: "00010100",
                BatchNumberDefault: "0000001"),
            Record7: new NachaRecord7Config(
                AddendaTypeCode: "99",
                ReturnReasonCodeSourceStrategy: "CurrentLayout/TransactionReasonCode",
                OriginalTraceSourceStrategy: "CurrentLayout/OriginalTrace15"),
            Record89: new NachaRecord89Config(
                CompanyIdentification: flow == NachaRecordFlow.ReturnOfReturnOut ? "BANCROR" : "BANCORET",
                OriginatingDfi: "00010100",
                BatchNumber: "0000001",
                PaddingStrategy: "CurrentLayout/PadWithRecord9"));
}

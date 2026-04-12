namespace Cfa.ACHInterbank.Application.ACH.Interfaces;

public interface IContrapartidaSoapResponseParser
{
    ContrapartidaSoapResponseParseResult Parse(string responseXml);
}

public sealed record ContrapartidaSoapResponseParseResult(
    string ResponseCode,
    bool IsSuccess,
    bool IsPartial,
    IReadOnlyDictionary<int, ContrapartidaSoapItemResult> ItemResults,
    string? Message = null);

public sealed record ContrapartidaSoapItemResult(
    int TransactionId,
    string ResponseCode,
    bool IsSuccess,
    string? Message = null);

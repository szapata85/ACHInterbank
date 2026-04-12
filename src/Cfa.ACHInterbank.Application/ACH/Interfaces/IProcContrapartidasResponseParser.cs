namespace Cfa.ACHInterbank.Application.ACH.Interfaces;

public interface IProcContrapartidasResponseParser
{
    ProcContrapartidasParsedResponse Parse(string responseXml);
}

public sealed record ProcContrapartidasParsedResponse(
    bool IsSuccess,
    bool IsSoapFault,
    bool IsRetryable,
    bool IsFunctionalRejection,
    string ErrorCode,
    string ErrorMessage,
    string RawResponse,
    string ResponseCode,
    IReadOnlyDictionary<int, ProcContrapartidasParsedItemResponse> ItemResults,
    string? FaultCode = null,
    string? FaultDetail = null);

public sealed record ProcContrapartidasParsedItemResponse(
    int TransactionId,
    bool IsSuccess,
    bool IsRetryable,
    string ResponseCode,
    string Message);

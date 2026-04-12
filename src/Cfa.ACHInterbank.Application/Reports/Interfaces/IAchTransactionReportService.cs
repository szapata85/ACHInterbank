using Cfa.ACHInterbank.Application.Reports.Models;

namespace Cfa.ACHInterbank.Application.Reports.Interfaces;

public interface IAchTransactionReportService
{
    Task<AchTransactionReportResponseDto> GetSentTransactionsAsync(AchTransactionReportFilter filter, CancellationToken ct = default);
    Task<AchTransactionReportResponseDto> GetReceivedTransactionsAsync(AchTransactionReportFilter filter, CancellationToken ct = default);
}


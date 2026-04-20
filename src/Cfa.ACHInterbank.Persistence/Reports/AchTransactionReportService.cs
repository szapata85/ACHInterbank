using Cfa.ACHInterbank.Application.Reports.Interfaces;
using Cfa.ACHInterbank.Application.Reports.Models;
using Cfa.ACHInterbank.Domain.Entities.Transactions.Enums;
using Cfa.ACHInterbank.Domain.Models.Configurations;
using Cfa.ACHInterbank.Persistence.DataBase;
using Microsoft.EntityFrameworkCore;

namespace Cfa.ACHInterbank.Persistence.Reports;

[Scoped]
public sealed class AchTransactionReportService : IAchTransactionReportService
{
    private readonly AchDbContext _context;

    public AchTransactionReportService(AchDbContext context)
    {
        _context = context;
    }

    public Task<AchTransactionReportResponseDto> GetSentTransactionsAsync(AchTransactionReportFilter filter, CancellationToken ct = default)
        => QueryAsync(filter, isSent: true, ct);

    public Task<AchTransactionReportResponseDto> GetReceivedTransactionsAsync(AchTransactionReportFilter filter, CancellationToken ct = default)
        => QueryAsync(filter, isSent: false, ct);

    private async Task<AchTransactionReportResponseDto> QueryAsync(AchTransactionReportFilter filter, bool isSent, CancellationToken ct)
    {
        var page = filter.Page <= 0 ? 1 : filter.Page;
        var pageSize = filter.PageSize <= 0 ? 25 : Math.Min(filter.PageSize, 200);

        var query = _context.AchTransactions
            .AsNoTracking()
            .Include(t => t.AchCycle)
            .ThenInclude(c => c.ClearingHouse)
            .Include(t => t.SourceInstitution)
            .Include(t => t.DestinationInstitution)
            .Include(t => t.AchBatch)
            .AsQueryable();

        if (filter.Date.HasValue)
        {
            var targetDate = filter.Date.Value.Date;
            query = query.Where(t => t.EffectiveEntryDate.Date == targetDate);
        }

        if (filter.ClearingHouseId.HasValue)
        {
            query = query.Where(t => t.AchCycle.ClearingHouseId == filter.ClearingHouseId.Value);
        }

        if (!string.IsNullOrWhiteSpace(filter.AchCycleId))
        {
            var achCycleId = filter.AchCycleId.Trim();
            query = query.Where(t => t.AchCycleId == achCycleId);
        }

        if (filter.State.HasValue)
        {
            query = query.Where(t => t.State == filter.State.Value);
        }

        if (!string.IsNullOrWhiteSpace(filter.Reference))
        {
            var reference = filter.Reference.Trim();
            query = query.Where(t => t.Reference.Contains(reference) || t.TransactionExternalId.Contains(reference));
        }

        if (filter.BankId.HasValue)
        {
            query = isSent
                ? query.Where(t => t.SourceInstitutionId == filter.BankId.Value)
                : query.Where(t => t.DestinationInstitutionId == filter.BankId.Value);
        }

        if (filter.TransactionType.HasValue)
        {
            query = query.Where(t => t.Type == filter.TransactionType.Value);
        }

        if (isSent)
        {
            query = query.Where(t =>
                t.State != AchTransferStateEnum.ReturnedByOperator &&
                t.State != AchTransferStateEnum.ReturnedByEpr &&
                string.IsNullOrWhiteSpace(t.ReturnReasonCode));
        }

        var total = await query.CountAsync(ct);
        var totalCreditAmount = await query
            .Where(t => t.Type == TransactionTypeEnum.Credit)
            .Select(t => (decimal?)t.Amount)
            .SumAsync(ct) ?? 0m;

        var totalDebitAmount = await query
            .Where(t => t.Type == TransactionTypeEnum.Debit)
            .Select(t => (decimal?)t.Amount)
            .SumAsync(ct) ?? 0m;

        var projectedQuery = query.Select(t => new
        {
            t.CreatedAt,
            Row = new AchTransactionReportRowDto
            {
                TransactionId = t.Id,
                EffectiveEntryDate = t.EffectiveEntryDate,
                TransactionExternalId = t.TransactionExternalId,
                Reference = t.Reference,
                Amount = t.Amount,
                TransactionType = t.Type,
                State = t.State,
                ClearingHouseName = t.AchCycle.ClearingHouse!.Name,
                AchCycleId = t.AchCycleId,
                AchCycleName = t.AchCycle.CycleName,
                BatchId = t.AchBatchId,
                BatchSequenceNumber = t.AchBatch.BatchSequenceNumber,
                SourceBankName = t.SourceInstitution.Name,
                DestinationBankName = t.DestinationInstitution.Name,
                NachaFileName = _context.AchFileExports
                    .Where(f => f.AchCycleId == t.AchCycleId && f.ClearingHouseId == t.AchCycle.ClearingHouseId)
                    .OrderByDescending(f => f.GeneratedAtUtc)
                    .Select(f => f.FileName)
                    .FirstOrDefault() ?? "-"
            }
        });

        List<AchTransactionReportRowDto> items;
        if (string.Equals(_context.Database.ProviderName, "Microsoft.EntityFrameworkCore.Sqlite", StringComparison.Ordinal))
        {
            var bufferedRows = await projectedQuery.ToListAsync(ct);
            items = bufferedRows
                .OrderByDescending(x => x.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(x => x.Row)
                .ToList();
        }
        else
        {
            items = await projectedQuery
                .OrderByDescending(x => x.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(x => x.Row)
                .ToListAsync(ct);
        }

        return new AchTransactionReportResponseDto
        {
            Items = items,
            Totals = new AchTransactionReportTotalsDto
            {
                TotalRecords = total,
                TotalCreditAmount = totalCreditAmount,
                TotalDebitAmount = totalDebitAmount
            },
            Total = total,
            Page = page,
            PageSize = pageSize
        };
    }
}

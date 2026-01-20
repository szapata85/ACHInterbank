using Cfa.ACHInterbank.Application.ACH.Interfaces;
using Cfa.ACHInterbank.Application.ACH.Models;
using Cfa.ACHInterbank.Domain.Models.ACH;
using Cfa.ACHInterbank.Persistence.DataBase;
using Microsoft.EntityFrameworkCore;

namespace Cfa.ACHInterbank.Persistence.ACH.Services.Implementation;

[Scoped]
public class NachaRecordDataProvider(AchDbContext context) : INachaRecordDataProvider
{
    private readonly AchDbContext _context = context;

    public async Task<IReadOnlyList<object>> GetRecordsAsync(
        NachaRecordDefinition definition,
        NachaBuildContext context,
        CancellationToken ct = default)
    {
        return definition.SourceType switch
        {
            NachaRecordSourceType.Entity => await GetEntityRecordsAsync(definition, context, ct),
            NachaRecordSourceType.View => await GetSqlRecordsAsync(definition, context, isProcedure: false, ct),
            NachaRecordSourceType.Procedure => await GetSqlRecordsAsync(definition, context, isProcedure: true, ct),
            _ => []
        };
    }

    private Task<IReadOnlyList<object>> GetEntityRecordsAsync(
        NachaRecordDefinition definition,
        NachaBuildContext context,
        CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(definition.SourceName))
        {
            return Task.FromResult<IReadOnlyList<object>>([]);
        }

        return definition.SourceName switch
        {
            nameof(AchBatch) => Task.FromResult<IReadOnlyList<object>>(context.Batches.Cast<object>().ToList()),
            nameof(AchTransaction) => Task.FromResult<IReadOnlyList<object>>(context.Transactions.Cast<object>().ToList()),
            nameof(AchTransactionAddenda) => Task.FromResult<IReadOnlyList<object>>(
                context.Transactions.SelectMany(t => t.Addendas ?? []).Cast<object>().ToList()),
            _ => Task.FromResult<IReadOnlyList<object>>([])
        };
    }

    private async Task<IReadOnlyList<object>> GetSqlRecordsAsync(
        NachaRecordDefinition definition,
        NachaBuildContext context,
        bool isProcedure,
        CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(definition.SourceName))
        {
            return [];
        }

        var sql = isProcedure
            ? definition.SourceName
            : $"SELECT * FROM {definition.SourceName}";

        Dictionary<string, object?> parameters = [];
        if (string.Equals(definition.FilterKey, "CycleId", StringComparison.OrdinalIgnoreCase))
        {
            parameters["CycleId"] = context.Cycle.Id;
        }
        else if (string.Equals(definition.FilterKey, "BatchId", StringComparison.OrdinalIgnoreCase))
        {
            parameters["BatchIds"] = context.Batches.Select(b => b.Id).ToArray();
        }

        return await _context.ExecuteDynamicSqlAsync(sql, parameters, ct);
    }
}

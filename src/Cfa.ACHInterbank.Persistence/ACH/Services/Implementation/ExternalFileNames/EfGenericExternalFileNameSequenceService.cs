using Cfa.ACHInterbank.Application.ACH.Models.ExternalFileNames;
using Cfa.ACHInterbank.Domain.Models.ACH.ExternalFileNames;
using Cfa.ACHInterbank.Domain.Models.Configurations;
using Cfa.ACHInterbank.Persistence.DataBase;
using Microsoft.EntityFrameworkCore;

namespace Cfa.ACHInterbank.Persistence.ACH.Services.Implementation.ExternalFileNames;

[Scoped]
public class EfGenericExternalFileNameSequenceService : IExternalFileNameSequenceProvider
{
    private readonly AchDbContext _context;

    public EfGenericExternalFileNameSequenceService(AchDbContext context)
    {
        _context = context;
    }

    public bool CanHandle(string? providerName) => true;

    public async Task<int> ReserveNextSequenceAsync(ExternalFileNameContext context, CancellationToken ct = default)
    {
        var scopeCode = ExternalFileNameSupport.GetSequenceScopeCode(context);
        var date = context.OperationalTimeSnapshot?.OperationalDate
            ?? DateOnly.FromDateTime(context.ProcessingDate.Date);
        var row = await _context.ExternalFileSequences
            .SingleOrDefaultAsync(x => x.ClearingHouseId == context.ClearingHouseId
                                    && x.ScopeCode == scopeCode
                                    && x.SequenceDate == date, ct);

        var maxValue = ExternalFileNameSupport.IsAchColombiaNachaOut(context) || ExternalFileNameSupport.IsReturnOut(context)
            ? 36
            : int.MaxValue;
        if (row is not null && row.LastValue >= maxValue)
        {
            throw new InvalidOperationException("Regla ACH HARD BLOCK: máximo 36 archivos diarios por participante.");
        }

        var now = context.OperationalTimeSnapshot?.CapturedAtUtc ?? DateTime.UtcNow;
        if (row is null)
        {
            row = new ExternalFileSequence
            {
                ClearingHouseId = context.ClearingHouseId,
                ScopeCode = scopeCode,
                SequenceDate = date,
                LastValue = 0,
                UpdatedAtUtc = now,
                RowVersion = [1]
            };
            _context.ExternalFileSequences.Add(row);
        }

        row.LastValue += 1;
        row.UpdatedAtUtc = now;
        row.RowVersion = BitConverter.GetBytes(row.LastValue);
        await _context.SaveChangesAsync(ct);
        return row.LastValue;
    }
}

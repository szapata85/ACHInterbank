using Cfa.ACHInterbank.Application.ACH.Models.ExternalFileNames;
using Cfa.ACHInterbank.Domain.Models.ACH.ExternalFileNames;
using Cfa.ACHInterbank.Domain.Models.Configurations;
using Cfa.ACHInterbank.Persistence.DataBase;
using Microsoft.EntityFrameworkCore;

namespace Cfa.ACHInterbank.Persistence.ACH.Services.Implementation.ExternalFileNames;

[Scoped]
public class EfGenericExternalFileNameSequenceService : IExternalFileNameSequenceProvider
{
    private const string ScopeCode = "ACH_EXTERNAL_NAME";
    private readonly AchDbContext _context;

    public EfGenericExternalFileNameSequenceService(AchDbContext context)
    {
        _context = context;
    }

    public bool CanHandle(string? providerName) => true;

    public async Task<int> ReserveNextSequenceAsync(ExternalFileNameContext context, CancellationToken ct = default)
    {
        var date = DateOnly.FromDateTime(context.ProcessingDate.Date);
        var row = await _context.ExternalFileSequences
            .SingleOrDefaultAsync(x => x.ClearingHouseId == context.ClearingHouseId
                                    && x.ScopeCode == ScopeCode
                                    && x.SequenceDate == date, ct);

        if (row is null)
        {
            row = new ExternalFileSequence
            {
                ClearingHouseId = context.ClearingHouseId,
                ScopeCode = ScopeCode,
                SequenceDate = date,
                LastValue = 0,
                UpdatedAtUtc = DateTime.UtcNow,
                RowVersion = [1]
            };
            _context.ExternalFileSequences.Add(row);
        }

        row.LastValue += 1;
        row.UpdatedAtUtc = DateTime.UtcNow;
        row.RowVersion = [(byte)(row.LastValue % 255 == 0 ? 1 : row.LastValue % 255)];

        if (ExternalFileNameSupport.IsAch(context) && row.LastValue > 36)
        {
            throw new InvalidOperationException("Regla ACH HARD BLOCK: máximo 36 archivos diarios por participante.");
        }

        await _context.SaveChangesAsync(ct);
        return row.LastValue;
    }
}

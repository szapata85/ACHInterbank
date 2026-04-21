using Cfa.ACHInterbank.Application.ACH.Interfaces.ExternalFileNames;
using Cfa.ACHInterbank.Application.ACH.Models.ExternalFileNames;
using Cfa.ACHInterbank.Domain.Models.Configurations;
using Cfa.ACHInterbank.Persistence.DataBase;
using Microsoft.EntityFrameworkCore;

namespace Cfa.ACHInterbank.Persistence.ACH.Services.Implementation.ExternalFileNames;

[Scoped]
public class ExternalFileDuplicateGuard : IExternalFileDuplicateGuard
{
    private readonly AchDbContext _context;

    public ExternalFileDuplicateGuard(AchDbContext context)
    {
        _context = context;
    }

    public async Task<bool> IsDuplicateAsync(ExternalFileNameContext context, string externalFileName, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(externalFileName))
        {
            return false;
        }

        var day = context.ProcessingDate.Date;
        var exists = await _context.ExternalFileNameRegistry
            .AsNoTracking()
            .AnyAsync(x => x.ClearingHouseId == context.ClearingHouseId
                           && x.ExternalFileName == externalFileName
                           && x.ProcessingDate.Date == day, ct);

        return exists;
    }
}

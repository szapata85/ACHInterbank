using Cfa.ACHInterbank.Application.ACH.Interfaces;
using Cfa.ACHInterbank.Persistence.DataBase;
using Microsoft.EntityFrameworkCore;

namespace Cfa.ACHInterbank.Persistence.ACH.Services.Implementation;

[Scoped]
public class NachaFileIdentifierMapService : INachaFileIdentifierMapService
{
    private readonly AchDbContext _context;

    public NachaFileIdentifierMapService(AchDbContext context)
    {
        _context = context;
    }

    public async Task<char> ResolveIdentifierAsync(int sequence, CancellationToken ct = default)
    {
        if (sequence is < 1 or > 36)
        {
            throw new InvalidOperationException($"Consecutivo diario fuera de rango SLA (001-036): {sequence:D3}.");
        }

        string? identifier = await _context.NachaFileIdentifierMaps
            .AsNoTracking()
            .Where(map => map.Sequence == sequence)
            .Select(map => map.Identifier)
            .FirstOrDefaultAsync(ct);

        if (string.IsNullOrWhiteSpace(identifier) || identifier.Length != 1)
        {
            throw new InvalidOperationException($"No existe mapeo de identificador NACHA para secuencia {sequence:D3} en tabla NachaFileIdentifierMap.");
        }

        return identifier[0];
    }
}

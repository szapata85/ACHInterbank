using Cfa.ACHInterbank.Application.ACH.Models;
using Cfa.ACHInterbank.Persistence.DataBase;

namespace Cfa.ACHInterbank.Persistence.ACH.Services.Implementation;

/// <summary>
/// Owns the EF write of an already parsed and validated inbound NACHA graph.
/// Transaction ownership intentionally remains with the caller/ambient ingestion flow.
/// </summary>
internal sealed class NachaInboundPersistenceCoordinator
{
    private readonly AchDbContext _context;

    public NachaInboundPersistenceCoordinator(AchDbContext context)
    {
        _context = context;
    }

    public async Task PersistAsync(IReadOnlyList<ParsedNachaHeader> headers, CancellationToken ct)
    {
        var persistenceGraph = ParsedNachaEntityMapper.Map(headers);
        _context.NachaHeaders.AddRange(persistenceGraph);
        await _context.SaveChangesAsync(ct);
    }
}

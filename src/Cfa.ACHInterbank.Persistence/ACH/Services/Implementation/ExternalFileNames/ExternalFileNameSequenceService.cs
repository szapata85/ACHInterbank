using Cfa.ACHInterbank.Application.ACH.Interfaces.ExternalFileNames;
using Cfa.ACHInterbank.Application.ACH.Models.ExternalFileNames;
using Cfa.ACHInterbank.Domain.Models.Configurations;
using Cfa.ACHInterbank.Persistence.DataBase;

namespace Cfa.ACHInterbank.Persistence.ACH.Services.Implementation.ExternalFileNames;

[Scoped]
public class ExternalFileNameSequenceService : IExternalFileNameSequenceService
{
    private readonly AchDbContext _context;
    private readonly IExternalFileNameSequenceProviderResolver _resolver;

    public ExternalFileNameSequenceService(
        AchDbContext context,
        IExternalFileNameSequenceProviderResolver resolver)
    {
        _context = context;
        _resolver = resolver;
    }

    public Task<int> ReserveNextSequenceAsync(ExternalFileNameContext context, CancellationToken ct = default)
    {
        var provider = _resolver.Resolve(_context.Database.ProviderName);
        return provider.ReserveNextSequenceAsync(context, ct);
    }
}

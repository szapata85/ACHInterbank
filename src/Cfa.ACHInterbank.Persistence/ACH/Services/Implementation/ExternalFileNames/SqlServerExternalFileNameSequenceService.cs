using Cfa.ACHInterbank.Application.ACH.Models.ExternalFileNames;
using Cfa.ACHInterbank.Domain.Models.Configurations;

namespace Cfa.ACHInterbank.Persistence.ACH.Services.Implementation.ExternalFileNames;

[Scoped]
public class SqlServerExternalFileNameSequenceService : IExternalFileNameSequenceProvider
{
    public bool CanHandle(string? providerName)
        => providerName?.Contains("SqlServer", StringComparison.OrdinalIgnoreCase) == true;

    public Task<int> ReserveNextSequenceAsync(ExternalFileNameContext context, CancellationToken ct = default)
    {
        throw new NotSupportedException(
            "External filename sequence reservation optimized for SQL Server is not implemented yet. Configure provider adapter or enable EF generic fallback explicitly.");
    }
}

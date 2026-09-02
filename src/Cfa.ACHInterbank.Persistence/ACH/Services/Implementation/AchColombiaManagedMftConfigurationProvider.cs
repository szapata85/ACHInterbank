using Cfa.ACHInterbank.Application.ACH.Configuration;
using Cfa.ACHInterbank.Application.ACH.Interfaces;
using Cfa.ACHInterbank.Application.ACH.Models;
using Cfa.ACHInterbank.Domain.Models.Configurations;
using Cfa.ACHInterbank.Persistence.DataBase;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Cfa.ACHInterbank.Persistence.ACH.Services.Implementation;

[Scoped]
public sealed class AchColombiaManagedMftConfigurationProvider(
    AchDbContext context, IOptions<AchColombiaManagedMftOptions> options) : IAchColombiaManagedMftConfigurationProvider
{
    public async Task<AchManagedMftEffectiveConfiguration> GetEffectiveAsync(CancellationToken ct = default)
    {
        var configured = await context.AchManagedFileTransferConfigurations.AsNoTracking()
            .Where(x => x.ClearingHouse.Code == "ACHCOL").SingleOrDefaultAsync(ct);
        var fallback = options.Value;
        if (configured is null)
            return new(fallback.Enabled, fallback.OutboundPath, fallback.InboundPath, fallback.ProcessingPath, fallback.ArchivePath, fallback.MaximumFileBytes);
        return new(configured.ProfileEnabled,
            Required(configured.OutboundLocation), Required(configured.InboundLocation),
            Required(fallback.ProcessingPath), Required(configured.ArchiveLocation), fallback.MaximumFileBytes);
    }

    private static string Required(string value) => string.IsNullOrWhiteSpace(value)
        ? throw new InvalidOperationException("ACHCOL_MFT_PATH_NOT_CONFIGURED") : value;
}

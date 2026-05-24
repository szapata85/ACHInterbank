using Cfa.ACHInterbank.Application.ACH.Interfaces;
using Cfa.ACHInterbank.Application.ACH.Models;
using Cfa.ACHInterbank.Domain.Entities.Transactions.Enums;
using Cfa.ACHInterbank.Domain.Models.ACH.Enums;
using Cfa.ACHInterbank.Domain.Models.Configurations;
using Cfa.ACHInterbank.Persistence.DataBase;
using Microsoft.EntityFrameworkCore;

namespace Cfa.ACHInterbank.Persistence.ACH.Services.Implementation;

[Scoped]
public class NachaFileNamingRuleService : INachaFileNamingRuleService
{
    private readonly AchDbContext _context;

    public NachaFileNamingRuleService(AchDbContext context)
    {
        _context = context;
    }

    public async Task<NachaFileNamingRulePolicy?> GetActiveOutboundRuleAsync(int clearingHouseId, DateTime processingDate, CancellationToken ct = default)
    {
        var rule = await _context.NachaFileNamingRules
            .AsNoTracking()
            .Where(x => x.ClearingHouseId == clearingHouseId
                        && x.FileDirection == NachaFileDirection.Outbound
                        && x.IsActive
                        && x.EffectiveFrom.Date <= processingDate.Date
                        && (!x.EffectiveTo.HasValue || x.EffectiveTo.Value.Date >= processingDate.Date))
            .OrderByDescending(x => x.EffectiveFrom)
            .FirstOrDefaultAsync(ct);

        if (rule is null)
        {
            return null;
        }

        var source = rule.SourceFinancialInstitutionId.HasValue
            ? await _context.FinancialInstitutions
                .AsNoTracking()
                .SingleOrDefaultAsync(x => x.Id == rule.SourceFinancialInstitutionId.Value, ct)
            : await ResolveDefaultSourceAsync(ct);

        if (source is null)
        {
            throw new InvalidOperationException("No existe institucion financiera originadora para aplicar nomenclatura NACHA-M.");
        }

        if (source.Status != FinancialInstitutionStatus.Active)
        {
            throw new InvalidOperationException($"La institucion originadora '{source.Name}' no esta activa para nomenclatura NACHA-M.");
        }

        var originCode = BuildExternalOriginEntityCode(source.RoutingNumber, source.TransitCode);

        return new NachaFileNamingRulePolicy(
            rule.Id,
            rule.ClearingHouseId,
            source.Id,
            source.Name,
            originCode,
            rule.NamePattern,
            rule.DailySequenceMin,
            rule.DailySequenceMax,
            rule.InternalFileIdMappingMode.ToString(),
            rule.RequiresNameHeaderEntityMatch,
            rule.NormativeSource,
            rule.NormativeReference);
    }

    private async Task<Cfa.ACHInterbank.Domain.Models.ACH.FinancialInstitution> ResolveDefaultSourceAsync(CancellationToken ct)
    {
        var defaults = await _context.FinancialInstitutions
            .AsNoTracking()
            .Where(x => x.IsDefaultSource)
            .ToListAsync(ct);

        if (defaults.Count != 1)
        {
            throw new InvalidOperationException($"Debe existir exactamente una institucion FinancialInstitution.IsDefaultSource=true. Encontradas: {defaults.Count}.");
        }

        return defaults[0];
    }

    private static string BuildExternalOriginEntityCode(string routingNumber, string transitCode)
    {
        var routing = DigitsOnly(routingNumber);
        var transit = DigitsOnly(transitCode);
        if (routing.Length < 4)
        {
            throw new InvalidOperationException("La ruta de la institucion originadora no permite construir RRRRTTT.");
        }

        if (transit.Length != 3)
        {
            throw new InvalidOperationException("El codigo de transito de la institucion originadora debe tener 3 digitos para construir RRRRTTT.");
        }

        return $"{routing[^4..]}{transit}";
    }

    private static string DigitsOnly(string value)
        => new(value.Where(char.IsDigit).ToArray());
}

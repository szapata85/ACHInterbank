using Cfa.ACHInterbank.Application.DataBase;
using Cfa.ACHInterbank.Domain.Entities.Transactions.Enums;
using Cfa.ACHInterbank.Domain.Models.ACH;
using Cfa.ACHInterbank.Domain.Models.ACH.Enums;
using Cfa.ACHInterbank.Domain.Models.Configurations;
using Cfa.ACHInterbank.Persistence.DataBase;
using Microsoft.EntityFrameworkCore;

namespace Cfa.ACHInterbank.Persistence.ACH.Services.Implementation.Seeders;

[Scoped]
public sealed class NachaFileNamingRuleSeeder : IDbSeeder
{
    private readonly AchDbContext _context;

    public NachaFileNamingRuleSeeder(AchDbContext context)
    {
        _context = context;
    }

    public int Order => 10;

    public async Task SeedAsync()
    {
        var source = await ResolveDefaultSourceAsync();
        ValidateSource(source);

        var achClearingHouse = await ResolveClearingHouseAsync("ACH Colombia", "ACHCOL", "ACH");
        var cenitClearingHouse = await ResolveClearingHouseAsync("CENIT", "CENIT");

        var now = DateTimeOffset.UtcNow;
        var effectiveFrom = new DateTime(2026, 01, 01, 0, 0, 0, DateTimeKind.Utc);

        await UpsertRuleAsync(
            achClearingHouse.Id,
            source.Id,
            "ACH Colombia",
            "MAN-004 ACH Colombia V32",
            "ACH-Colombia-V32",
            "6.1.10.1 / 6.1.10.3",
            "Regla outbound oficial ACH Colombia. ReturnOut reutiliza esta regla con scope separado de secuencia.",
            now,
            effectiveFrom);

        await UpsertRuleAsync(
            cenitClearingHouse.Id,
            source.Id,
            "CENIT",
            "CENIT-DSP-152-Anexo-2",
            "CENIT-DSP-152-Anexo-2",
            "Homologacion operativa actual",
            "Regla outbound homologada para CENIT mientras no exista naming distinto documentado. ReturnOut reutiliza esta regla con scope separado de secuencia.",
            now,
            effectiveFrom);

        await _context.SaveChangesAsync();
    }

    private async Task<FinancialInstitution> ResolveDefaultSourceAsync()
    {
        var defaults = await _context.FinancialInstitutions
            .AsNoTracking()
            .Where(x => x.IsDefaultSource)
            .ToListAsync();

        if (defaults.Count != 1)
        {
            throw new InvalidOperationException($"Debe existir exactamente una institucion FinancialInstitution.IsDefaultSource=true. Encontradas: {defaults.Count}.");
        }

        return defaults[0];
    }

    private static void ValidateSource(FinancialInstitution source)
    {
        if (string.IsNullOrWhiteSpace(source.RoutingNumber))
        {
            throw new InvalidOperationException($"La institucion financiera origen '{source.Name}' no tiene RoutingNumber configurado.");
        }

        if (string.IsNullOrWhiteSpace(source.TransitCode))
        {
            throw new InvalidOperationException($"La institucion financiera origen '{source.Name}' no tiene TransitCode configurado.");
        }

        if (source.Status != FinancialInstitutionStatus.Active)
        {
            throw new InvalidOperationException($"La institucion financiera origen '{source.Name}' no esta activa.");
        }
    }

    private async Task<ClearingHouse> ResolveClearingHouseAsync(string displayName, params string[] codes)
    {
        var houses = await _context.ClearingHouses
            .AsNoTracking()
            .Where(x => codes.Contains(x.Code))
            .OrderBy(x => x.Id)
            .ToListAsync();

        if (houses.Count == 0)
        {
            throw new InvalidOperationException($"Falta la camara requerida '{displayName}' para sembrar reglas de naming.");
        }

        if (houses.Count > 1)
        {
            throw new InvalidOperationException($"La camara '{displayName}' esta duplicada por codigo y no se puede sembrar de forma segura.");
        }

        return houses[0];
    }

    private async Task UpsertRuleAsync(
        int clearingHouseId,
        int sourceFinancialInstitutionId,
        string clearingHouseName,
        string normativeSource,
        string normativeReference,
        string notesHeading,
        string notes,
        DateTimeOffset now,
        DateTime effectiveFrom)
    {
        var existing = await _context.NachaFileNamingRules
            .SingleOrDefaultAsync(x =>
                x.ClearingHouseId == clearingHouseId &&
                x.FileDirection == NachaFileDirection.Outbound &&
                x.NamePattern == "RRRRTTT.ZZZ.N");

        if (existing is null)
        {
            _context.NachaFileNamingRules.Add(new NachaFileNamingRule
            {
                ClearingHouseId = clearingHouseId,
                SourceFinancialInstitutionId = sourceFinancialInstitutionId,
                FileDirection = NachaFileDirection.Outbound,
                NamePattern = "RRRRTTT.ZZZ.N",
                Extension = ".ach",
                DailySequenceMin = 1,
                DailySequenceMax = 36,
                InternalFileIdMappingMode = InternalFileIdMappingMode.Alphanumeric36,
                RequiresNameHeaderEntityMatch = true,
                IsActive = true,
                EffectiveFrom = effectiveFrom,
                EffectiveTo = null,
                NormativeSource = normativeSource,
                NormativeReference = normativeReference,
                Notes = $"{notesHeading}. {notes}",
                CreatedAt = now,
                UpdatedAt = now
            });
            return;
        }

        existing.SourceFinancialInstitutionId = sourceFinancialInstitutionId;
        existing.FileDirection = NachaFileDirection.Outbound;
        existing.NamePattern = "RRRRTTT.ZZZ.N";
        existing.Extension = ".ach";
        existing.DailySequenceMin = 1;
        existing.DailySequenceMax = 36;
        existing.InternalFileIdMappingMode = InternalFileIdMappingMode.Alphanumeric36;
        existing.RequiresNameHeaderEntityMatch = true;
        existing.IsActive = true;
        existing.EffectiveFrom = effectiveFrom;
        existing.EffectiveTo = null;
        existing.NormativeSource = normativeSource;
        existing.NormativeReference = normativeReference;
        existing.Notes = $"{notesHeading}. {notes}";
        existing.UpdatedAt = now;
    }
}

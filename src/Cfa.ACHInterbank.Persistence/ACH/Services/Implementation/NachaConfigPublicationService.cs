using System.Text.Json;
using Cfa.ACHInterbank.Application.ACH.Interfaces;
using Cfa.ACHInterbank.Application.ACH.Models;
using Cfa.ACHInterbank.Domain.Models.ACH.Config;
using Cfa.ACHInterbank.Persistence.DataBase;
using Microsoft.EntityFrameworkCore;

namespace Cfa.ACHInterbank.Persistence.ACH.Services.Implementation;

[Scoped]
public sealed class NachaConfigPublicationService : INachaConfigPublicationService
{
    private readonly AchDbContext _context;
    private readonly INachaConfigValidationService _validation;

    public NachaConfigPublicationService(AchDbContext context, INachaConfigValidationService validation)
    {
        _context = context;
        _validation = validation;
    }

    public async Task<NachaConfigPublicationResultDto> PublishAsync(int profileId, string actor, CancellationToken ct = default)
    {
        var validation = await _validation.ValidateBeforePublishAsync(profileId, ct);
        if (!validation.IsValid)
        {
            return new NachaConfigPublicationResultDto
            {
                ProfileId = profileId,
                Publicado = false,
                Mensaje = validation.Resumen
            };
        }

        var profile = await _context.CfgProfiles.FirstOrDefaultAsync(x => x.Id == profileId, ct)
                     ?? throw new InvalidOperationException("Perfil no encontrado.");

        profile.StatusId = await ResolveStatusIdAsync("PUBLICADO", ct);
        profile.PublishedAt = DateTime.UtcNow;
        profile.PublishedBy = string.IsNullOrWhiteSpace(actor) ? "system" : actor;
        profile.VersionMinor += 1;
        await _context.SaveChangesAsync(ct);

        var snapshot = new HistConfigSnapshot
        {
            ProfileId = profile.Id,
            VersionMajor = profile.VersionMajor,
            VersionMinor = profile.VersionMinor,
            SnapshotType = "PUBLISH",
            SnapshotJson = JsonSerializer.Serialize(new
            {
                profile.ProfileCode,
                profile.VersionMajor,
                profile.VersionMinor,
                profile.EffectiveFrom,
                profile.EffectiveTo,
                profile.StatusId
            }),
            CreatedAtUtc = DateTime.UtcNow,
            CreatedBy = profile.PublishedBy ?? "system"
        };

        _context.HistConfigSnapshots.Add(snapshot);
        _context.HistConfigChanges.Add(new HistConfigChange
        {
            ProfileId = profile.Id,
            EntityName = nameof(CfgProfile),
            EntityId = profile.Id.ToString(),
            ChangeType = "PUBLISH",
            BeforeJson = null,
            AfterJson = JsonSerializer.Serialize(new { profile.StatusId, profile.VersionMajor, profile.VersionMinor }),
            ChangedAtUtc = DateTime.UtcNow,
            ChangedBy = profile.PublishedBy ?? "system",
            CorrelationId = $"NACHA-PUBLISH-{profile.Id}-{DateTime.UtcNow:yyyyMMddHHmmssfff}"
        });

        await _context.SaveChangesAsync(ct);

        return new NachaConfigPublicationResultDto
        {
            ProfileId = profileId,
            Publicado = true,
            Mensaje = "Perfil publicado correctamente.",
            VersionMajor = profile.VersionMajor,
            VersionMinor = profile.VersionMinor
        };
    }

    private async Task<int> ResolveStatusIdAsync(string code, CancellationToken ct)
    {
        var status = await _context.CatConfigStatuses.AsNoTracking().FirstOrDefaultAsync(x => x.Code == code, ct)
                     ?? throw new InvalidOperationException($"Estado {code} no existe.");
        return status.Id;
    }
}

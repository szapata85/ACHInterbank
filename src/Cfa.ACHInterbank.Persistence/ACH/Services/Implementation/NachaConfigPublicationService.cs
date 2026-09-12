using System.Text.Json;
using Cfa.ACHInterbank.Application.ACH.Interfaces;
using Cfa.ACHInterbank.Application.ACH.Models;
using Cfa.ACHInterbank.Domain.Models.ACH.Config;
using Cfa.ACHInterbank.Domain.Models.Configurations;
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

    public async Task<NachaConfigPublicationResultDto> PublishAsync(int profileId, string actor, string expectedRowVersion, CancellationToken ct = default)
    {
        var currentProfile = await _context.CfgProfiles
            .Include(x => x.Status)
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == profileId, ct)
            ?? throw new InvalidOperationException("Perfil no encontrado.");
        EnsureProfileIsBorrador(currentProfile);

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

        return await ExecuteInTransactionAsync(async () =>
        {
            var profile = await NachaCompleteProfileQuery.Create(_context)
                              .FirstOrDefaultAsync(profile => profile.Id == profileId, ct)
                         ?? throw new InvalidOperationException("Perfil no encontrado.");

            EnsureExpectedRowVersion(profile, expectedRowVersion);
            EnsureProfileIsBorrador(profile);

            var currentValidation = await _validation.ValidateBeforePublishAsync(profileId, ct);
            if (!currentValidation.IsValid)
            {
                return new NachaConfigPublicationResultDto
                {
                    ProfileId = profileId,
                    Publicado = false,
                    Mensaje = currentValidation.Resumen
                };
            }

            var publishedAtUtc = DateTime.UtcNow;
            var publishedBy = string.IsNullOrWhiteSpace(actor) ? "system" : actor;
            var targetVersionMinor = profile.VersionMinor + 1;
            var snapshotContract = NachaPublicationSnapshotSerializer.Build(
                profile,
                profile.VersionMajor,
                targetVersionMinor,
                publishedAtUtc,
                publishedBy);
            var snapshotJson = NachaPublicationSnapshotSerializer.Serialize(snapshotContract);
            var snapshotReadResult = NachaPublicationSnapshotSerializer.ReadForTraceLineage(snapshotJson);
            if (!snapshotReadResult.IsSupported)
            {
                throw new InvalidOperationException(
                    $"SNAPSHOT_SERIALIZATION_INVALID: {snapshotReadResult.Status}: {snapshotReadResult.Error}");
            }

            var snapshot = new HistConfigSnapshot
            {
                ProfileId = profile.Id,
                VersionMajor = profile.VersionMajor,
                VersionMinor = targetVersionMinor,
                SnapshotType = "PUBLISH",
                SnapshotJson = snapshotJson,
                CreatedAtUtc = publishedAtUtc,
                CreatedBy = publishedBy
            };

            _context.HistConfigSnapshots.Add(snapshot);

            profile.StatusId = await ResolveStatusIdAsync("PUBLICADO", ct);
            profile.PublishedAt = publishedAtUtc;
            profile.PublishedBy = publishedBy;
            profile.UpdatedAt = DateTimeOffset.UtcNow;
            profile.VersionMinor = targetVersionMinor;
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
                VersionMinor = profile.VersionMinor,
                RowVersion = Convert.ToBase64String(profile.RowVersion)
            };
        }, ct);
    }

    private static void EnsureExpectedRowVersion(CfgProfile profile, string expectedRowVersion)
    {
        if (string.IsNullOrWhiteSpace(expectedRowVersion))
        {
            throw new NachaConfigException("CONCURRENCY_TOKEN_REQUIRED", "Debe enviar la versión de concurrencia del perfil.", 409, Convert.ToBase64String(profile.RowVersion));
        }

        byte[] expectedBytes;
        try
        {
            expectedBytes = Convert.FromBase64String(expectedRowVersion);
        }
        catch (FormatException)
        {
            throw new NachaConfigException("INVALID_CONCURRENCY_TOKEN", "La versión de concurrencia no tiene formato Base64 válido.", 400, Convert.ToBase64String(profile.RowVersion));
        }

        if (!profile.RowVersion.SequenceEqual(expectedBytes))
        {
            throw new NachaConfigException("CONCURRENCY_CONFLICT", "El perfil fue modificado por otro usuario.", 409, Convert.ToBase64String(profile.RowVersion));
        }
    }

    private static void EnsureProfileIsBorrador(CfgProfile profile)
    {
        if (!string.Equals(profile.Status.Code, "BORRADOR", StringComparison.OrdinalIgnoreCase))
        {
            throw new NachaConfigException(
                "INVALID_PROFILE_STATE",
                "Solo se puede publicar un perfil en estado BORRADOR. Se requiere una nueva version para cambiar una publicacion existente.",
                409,
                Convert.ToBase64String(profile.RowVersion));
        }
    }

    private async Task<T> ExecuteInTransactionAsync<T>(Func<Task<T>> operation, CancellationToken ct)
    {
        var strategy = _context.Database.CreateExecutionStrategy();
        return await strategy.ExecuteAsync(async () =>
        {
            await using var tx = await _context.Database.BeginTransactionAsync(ct);
            try
            {
                var result = await operation();
                await tx.CommitAsync(ct);
                return result;
            }
            catch (DbUpdateConcurrencyException)
            {
                await tx.RollbackAsync(ct);
                throw new NachaConfigException("CONCURRENCY_CONFLICT", "El perfil fue modificado por otro usuario.", 409);
            }
            catch
            {
                await tx.RollbackAsync(ct);
                throw;
            }
        });
    }

    private async Task<int> ResolveStatusIdAsync(string code, CancellationToken ct)
    {
        var status = await _context.CatConfigStatuses.AsNoTracking().FirstOrDefaultAsync(x => x.Code == code, ct)
                     ?? throw new InvalidOperationException($"Estado {code} no existe.");
        return status.Id;
    }
}

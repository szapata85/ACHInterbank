using Cfa.ACHInterbank.Application.ACH.Interfaces;
using Cfa.ACHInterbank.Application.ACH.Models;
using Cfa.ACHInterbank.Domain.Models.ACH.Config;
using Cfa.ACHInterbank.Domain.Models.Configurations;
using Cfa.ACHInterbank.Persistence.DataBase;
using Microsoft.EntityFrameworkCore;

namespace Cfa.ACHInterbank.Persistence.ACH.Services.Implementation;

[Scoped]
public sealed class NachaConfigProfileQueryService : INachaConfigProfileQueryService
{
    private readonly AchDbContext _context;

    public NachaConfigProfileQueryService(AchDbContext context)
    {
        _context = context;
    }

    public async Task<IReadOnlyList<NachaConfigProfileListItemDto>> GetProfilesAsync(CancellationToken ct = default)
    {
        var profiles = await _context.CfgProfiles
            .AsNoTracking()
            .Include(x => x.Status)
            .Include(x => x.ClearingHouse)
            .Include(x => x.FlowType)
            .Include(x => x.Direction)
            .Include(x => x.ServiceClass)
            .OrderByDescending(x => x.UpdatedAt)
            .ToListAsync(ct);

        return profiles.Select(x => new NachaConfigProfileListItemDto
        {
            Id = x.Id,
            ProfileCode = x.ProfileCode,
            NombreEs = x.NameEs,
            Estado = x.Status.Code,
            Camara = x.ClearingHouse.Code,
            Flujo = x.FlowType.Code,
            Direccion = x.Direction.Code,
            Servicio = x.ServiceClass?.Code,
            VersionMajor = x.VersionMajor,
            VersionMinor = x.VersionMinor,
            EffectiveFrom = x.EffectiveFrom,
            EffectiveTo = x.EffectiveTo
        }).ToList();
    }

    public async Task<NachaConfigProfileDetailDto?> GetProfileDetailAsync(int profileId, CancellationToken ct = default)
    {
        var profile = await _context.CfgProfiles
            .AsNoTracking()
            .Include(x => x.Status)
            .Include(x => x.Records)
                .ThenInclude(x => x.RecordCode)
            .Include(x => x.LayoutVariants)
                .ThenInclude(x => x.RecordCode)
            .Include(x => x.LayoutVariants)
                .ThenInclude(x => x.Fields)
                    .ThenInclude(x => x.SourceDefinition)
                        .ThenInclude(x => x.DataSourceType)
            .FirstOrDefaultAsync(x => x.Id == profileId, ct);

        if (profile is null)
        {
            return null;
        }

        return new NachaConfigProfileDetailDto
        {
            Id = profile.Id,
            ProfileCode = profile.ProfileCode,
            NombreEs = profile.NameEs,
            Descripcion = profile.Description,
            Estado = profile.Status.Code,
            VersionMajor = profile.VersionMajor,
            VersionMinor = profile.VersionMinor,
            ContextPriority = profile.ContextPriority,
            EffectiveFrom = profile.EffectiveFrom,
            EffectiveTo = profile.EffectiveTo,
            Records = profile.Records.OrderBy(x => x.Sequence).Select(x => new NachaConfigProfileRecordDto
            {
                Id = x.Id,
                RecordCode = x.RecordCode.Code,
                Sequence = x.Sequence,
                IsEnabled = x.IsEnabled,
                MinOccurs = x.MinOccurs,
                MaxOccurs = x.MaxOccurs,
                SourceStrategy = x.SourceStrategy
            }).ToList(),
            Variantes = profile.LayoutVariants.OrderBy(x => x.RecordCode.Code).ThenBy(x => x.Priority).Select(v => new NachaConfigLayoutVariantDto
            {
                Id = v.Id,
                RecordCode = v.RecordCode.Code,
                VariantCode = v.VariantCode,
                NombreEs = v.NameEs,
                Priority = v.Priority,
                IsDefaultForRecord = v.IsDefaultForRecord,
                TotalLength = v.TotalLength,
                Fields = v.Fields.OrderBy(f => f.StartPosition).Select(f => new NachaConfigLayoutFieldDto
                {
                    Id = f.Id,
                    FieldCode = f.FieldCode,
                    FieldNameEs = f.FieldNameEs,
                    StartPosition = f.StartPosition,
                    Length = f.Length,
                    PropertyPath = f.SourceDefinition.PropertyPath,
                    SourceType = f.SourceDefinition.DataSourceType.Code,
                    IsEnabled = f.IsEnabled
                }).ToList()
            }).ToList()
        };
    }
}

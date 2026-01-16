using Cfa.ACHInterbank.Application.ACH.Interfaces;
using Cfa.ACHInterbank.Domain.Entities.Ach.Dtos;
using Cfa.ACHInterbank.Domain.Models.ACH;
using Cfa.ACHInterbank.Domain.Models.Configurations;
using Cfa.ACHInterbank.Persistence.DataBase;
using Microsoft.EntityFrameworkCore;

namespace Cfa.ACHInterbank.Persistence.ACH.Services.Implementation;

[Scoped]
public class NachaRecordDefinitionAppService : INachaRecordDefinitionAppService
{
    private readonly AchDbContext _context;

    public NachaRecordDefinitionAppService(AchDbContext context)
    {
        _context = context;
    }

    public async Task<IReadOnlyList<NachaRecordDefinitionDto>> GetAllAsync(CancellationToken ct = default)
    {
        var items = await _context.NachaRecordDefinitions
            .AsNoTracking()
            .OrderBy(d => d.Sequence)
            .ToListAsync(ct);

        return items.Select(ToDto).ToList();
    }

    public async Task<NachaRecordDefinitionDto?> GetByIdAsync(int id, CancellationToken ct = default)
    {
        var entity = await _context.NachaRecordDefinitions
            .AsNoTracking()
            .FirstOrDefaultAsync(d => d.Id == id, ct);

        return entity is null ? null : ToDto(entity);
    }

    public async Task<NachaRecordDefinitionDto> CreateAsync(NachaRecordDefinitionDto request, CancellationToken ct = default)
    {
        var entity = new NachaRecordDefinition();
        ApplyChanges(entity, request);

        _context.NachaRecordDefinitions.Add(entity);
        await _context.SaveChangesAsync(ct);

        var created = await GetByIdAsync(entity.Id, ct);
        return created ?? ToDto(entity);
    }

    public async Task<NachaRecordDefinitionDto?> UpdateAsync(int id, NachaRecordDefinitionDto request, CancellationToken ct = default)
    {
        var entity = await _context.NachaRecordDefinitions
            .FirstOrDefaultAsync(d => d.Id == id, ct);

        if (entity is null)
        {
            return null;
        }

        ApplyChanges(entity, request);
        await _context.SaveChangesAsync(ct);

        return await GetByIdAsync(entity.Id, ct);
    }

    public async Task<bool> DeleteAsync(int id, CancellationToken ct = default)
    {
        var entity = await _context.NachaRecordDefinitions.FindAsync([id], ct);
        if (entity is null)
        {
            return false;
        }

        _context.NachaRecordDefinitions.Remove(entity);
        await _context.SaveChangesAsync(ct);
        return true;
    }

    private static NachaRecordDefinitionDto ToDto(NachaRecordDefinition entity)
    {
        return new NachaRecordDefinitionDto
        {
            Id = entity.Id,
            RecordCode = entity.RecordCode,
            Sequence = entity.Sequence,
            SourceType = entity.SourceType,
            SourceName = entity.SourceName,
            FilterKey = entity.FilterKey,
            IsEnabled = entity.IsEnabled
        };
    }

    private static void ApplyChanges(NachaRecordDefinition entity, NachaRecordDefinitionDto request)
    {
        entity.RecordCode = request.RecordCode.Trim();
        entity.Sequence = request.Sequence;
        entity.SourceType = request.SourceType;
        entity.SourceName = string.IsNullOrWhiteSpace(request.SourceName) ? null : request.SourceName.Trim();
        entity.FilterKey = string.IsNullOrWhiteSpace(request.FilterKey) ? null : request.FilterKey.Trim();
        entity.IsEnabled = request.IsEnabled;
    }
}

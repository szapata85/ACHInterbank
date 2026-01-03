using Cfa.ACHInterbank.Application.ACH.Interfaces;
using Cfa.ACHInterbank.Domain.Entities.Ach.Dtos;
using Cfa.ACHInterbank.Domain.Models.ACH;
using Cfa.ACHInterbank.Persistence.DataBase;
using Microsoft.EntityFrameworkCore;

namespace Cfa.ACHInterbank.Persistence.ACH.Services.Implementation;

[Scoped]
public class NachaRecordLayoutAppService : INachaRecordLayoutAppService
{
    private readonly AchDbContext _context;

    public NachaRecordLayoutAppService(AchDbContext context)
    {
        _context = context;
    }

    public async Task<IReadOnlyList<NachaRecordLayoutDto>> GetAllAsync(CancellationToken ct = default)
    {
        var layouts = await _context.NachaRecordLayouts
            .AsNoTracking()
            .Include(l => l.Fields)
            .OrderBy(l => l.RecordCode)
            .ToListAsync(ct);

        return layouts.Select(ToDto).ToList();
    }

    public async Task<NachaRecordLayoutDto?> GetByIdAsync(int id, CancellationToken ct = default)
    {
        var layout = await _context.NachaRecordLayouts
            .AsNoTracking()
            .Include(l => l.Fields)
            .FirstOrDefaultAsync(l => l.Id == id, ct);

        return layout is null ? null : ToDto(layout);
    }

    public async Task<NachaRecordLayoutDto> CreateAsync(NachaRecordLayoutDto request, CancellationToken ct = default)
    {
        var entity = new NachaRecordLayout();
        ApplyChanges(entity, request);

        _context.NachaRecordLayouts.Add(entity);
        await _context.SaveChangesAsync(ct);

        var created = await GetByIdAsync(entity.Id, ct);
        return created ?? ToDto(entity);
    }

    public async Task<NachaRecordLayoutDto?> UpdateAsync(int id, NachaRecordLayoutDto request, CancellationToken ct = default)
    {
        var entity = await _context.NachaRecordLayouts
            .Include(l => l.Fields)
            .FirstOrDefaultAsync(l => l.Id == id, ct);

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
        var entity = await _context.NachaRecordLayouts.FindAsync([id], ct);
        if (entity is null)
        {
            return false;
        }

        _context.NachaRecordLayouts.Remove(entity);
        await _context.SaveChangesAsync(ct);
        return true;
    }

    private static NachaRecordLayoutDto ToDto(NachaRecordLayout entity)
    {
        return new NachaRecordLayoutDto
        {
            Id = entity.Id,
            RecordType = entity.RecordType ?? string.Empty,
            RecordCode = entity.RecordCode ?? string.Empty,
            TotalLength = entity.TotalLength,
            Description = entity.Description,
            Fields = entity.Fields
                .OrderBy(f => f.StartPosition)
                .Select(f => new NachaRecordFieldDto
                {
                    Id = f.Id,
                    FieldName = f.FieldName ?? string.Empty,
                    StartPosition = f.StartPosition,
                    Length = f.Length,
                    PadChar = f.PadChar.ToString(),
                    Justification = f.Justification.ToString(),
                    DbColumn = f.DbColumn ?? string.Empty,
                    Format = f.Format
                })
                .ToList()
        };
    }

    private static void ApplyChanges(NachaRecordLayout entity, NachaRecordLayoutDto request)
    {
        entity.RecordType = request.RecordType.Trim();
        entity.RecordCode = request.RecordCode.Trim();
        entity.TotalLength = request.TotalLength;
        entity.Description = string.IsNullOrWhiteSpace(request.Description) ? null : request.Description.Trim();

        var incoming = request.Fields
            .Where(f => !string.IsNullOrWhiteSpace(f.FieldName))
            .Select(f => new NachaRecordField
            {
                Id = f.Id,
                FieldName = f.FieldName.Trim(),
                StartPosition = f.StartPosition,
                Length = f.Length,
                PadChar = string.IsNullOrWhiteSpace(f.PadChar) ? ' ' : f.PadChar[0],
                Justification = string.IsNullOrWhiteSpace(f.Justification) ? 'L' : f.Justification[0],
                DbColumn = f.DbColumn.Trim(),
                Format = string.IsNullOrWhiteSpace(f.Format) ? null : f.Format.Trim()
            })
            .ToList();

        var toRemove = entity.Fields
            .Where(existing => incoming.All(f => f.Id == 0 || f.Id != existing.Id))
            .ToList();

        foreach (var removed in toRemove)
        {
            entity.Fields.Remove(removed);
        }

        foreach (var incomingField in incoming)
        {
            if (incomingField.Id == 0)
            {
                entity.Fields.Add(new NachaRecordField
                {
                    FieldName = incomingField.FieldName,
                    StartPosition = incomingField.StartPosition,
                    Length = incomingField.Length,
                    PadChar = incomingField.PadChar,
                    Justification = incomingField.Justification,
                    DbColumn = incomingField.DbColumn,
                    Format = incomingField.Format
                });
                continue;
            }

            var existing = entity.Fields.FirstOrDefault(f => f.Id == incomingField.Id);
            if (existing is null)
            {
                entity.Fields.Add(new NachaRecordField
                {
                    FieldName = incomingField.FieldName,
                    StartPosition = incomingField.StartPosition,
                    Length = incomingField.Length,
                    PadChar = incomingField.PadChar,
                    Justification = incomingField.Justification,
                    DbColumn = incomingField.DbColumn,
                    Format = incomingField.Format
                });
                continue;
            }

            existing.FieldName = incomingField.FieldName;
            existing.StartPosition = incomingField.StartPosition;
            existing.Length = incomingField.Length;
            existing.PadChar = incomingField.PadChar;
            existing.Justification = incomingField.Justification;
            existing.DbColumn = incomingField.DbColumn;
            existing.Format = incomingField.Format;
        }
    }
}

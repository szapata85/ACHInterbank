using Cfa.ACHInterbank.Application.Scheduler.Interfaces;
using Cfa.ACHInterbank.Domain.Entities.SchedulerTask;
using Cfa.ACHInterbank.Domain.Entities.SchedulerTask.Dtos;
using Cfa.ACHInterbank.Domain.Models.Configurations;
using Cfa.ACHInterbank.Persistence.DataBase;
using Microsoft.EntityFrameworkCore;

namespace Cfa.ACHInterbank.Persistence.Scheduler;

[Scoped]
public class TaskDefinitionAppService : ITaskDefinitionAppService
{
    private readonly AchDbContext _context;

    public TaskDefinitionAppService(AchDbContext context)
    {
        _context = context;
    }

    public async Task<IReadOnlyList<TaskDefinitionDto>> GetAllAsync(CancellationToken ct = default)
    {
        var items = await _context.TaskDefinitions
            .AsNoTracking()
            .Include(t => t.Parameters)
            .OrderBy(t => t.Id)
            .ToListAsync(ct);

        return items.Select(ToDto).ToList();
    }

    public async Task<TaskDefinitionDto?> GetByIdAsync(int id, CancellationToken ct = default)
    {
        var item = await _context.TaskDefinitions
            .AsNoTracking()
            .Include(t => t.Parameters)
            .FirstOrDefaultAsync(t => t.Id == id, ct);

        return item is null ? null : ToDto(item);
    }

    public async Task<TaskDefinitionDto> CreateAsync(TaskDefinitionDto request, CancellationToken ct = default)
    {
        var entity = new TaskDefinition();
        ApplyChanges(entity, request);

        _context.TaskDefinitions.Add(entity);
        await _context.SaveChangesAsync(ct);

        var created = await GetByIdAsync(entity.Id, ct);
        return created ?? ToDto(entity);
    }

    public async Task<TaskDefinitionDto?> UpdateAsync(int id, TaskDefinitionDto request, CancellationToken ct = default)
    {
        var entity = await _context.TaskDefinitions
            .Include(t => t.Parameters)
            .FirstOrDefaultAsync(t => t.Id == id, ct);

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
        var entity = await _context.TaskDefinitions.FindAsync([id], ct);
        if (entity is null)
        {
            return false;
        }

        _context.TaskDefinitions.Remove(entity);
        await _context.SaveChangesAsync(ct);
        return true;
    }

    private static TaskDefinitionDto ToDto(TaskDefinition entity)
    {
        return new TaskDefinitionDto
        {
            Id = entity.Id,
            Code = entity.Code,
            Name = entity.Name,
            Status = entity.Status,
            CalendarPolicy = entity.CalendarPolicy,
            TimeZoneId = entity.TimeZoneId,
            ConcurrencyPolicy = entity.ConcurrencyPolicy,
            RetryOnFailure = entity.RetryOnFailure,
            MaxRetries = entity.MaxRetries,
            RetryBackoffSeconds = entity.RetryBackoffSeconds,
            PeriodicityType = entity.PeriodicityType,
            N = entity.N,
            Minute = entity.Minute,
            TimeOfDay = entity.TimeOfDay?.ToString("HH:mm"),
            WeeklyDay = entity.WeeklyDay,
            MonthDay = entity.MonthDay,
            CronExpression = entity.CronExpression,
            StartAt = entity.StartAt,
            EndAt = entity.EndAt,
            Parameters = entity.Parameters
                .OrderBy(p => p.Id)
                .Select(p => new TaskParameterDto
                {
                    Id = p.Id,
                    Key = p.Key,
                    Value = p.Value
                })
                .ToList()
        };
    }

    private static void ApplyChanges(TaskDefinition entity, TaskDefinitionDto request)
    {
        entity.Code = request.Code.Trim();
        entity.Name = request.Name.Trim();
        entity.Status = request.Status;
        entity.CalendarPolicy = request.CalendarPolicy;
        entity.TimeZoneId = string.IsNullOrWhiteSpace(request.TimeZoneId) ? null : request.TimeZoneId.Trim();
        entity.ConcurrencyPolicy = request.ConcurrencyPolicy;
        entity.RetryOnFailure = request.RetryOnFailure;
        entity.MaxRetries = request.MaxRetries;
        entity.RetryBackoffSeconds = request.RetryBackoffSeconds;
        entity.PeriodicityType = request.PeriodicityType;
        entity.N = request.N;
        entity.Minute = request.Minute;
        entity.TimeOfDay = ParseTimeOfDay(request.TimeOfDay);
        entity.WeeklyDay = request.WeeklyDay;
        entity.MonthDay = request.MonthDay;
        entity.CronExpression = string.IsNullOrWhiteSpace(request.CronExpression) ? null : request.CronExpression.Trim();
        entity.StartAt = request.StartAt;
        entity.EndAt = request.EndAt;

        var incoming = request.Parameters
            .Where(p => !string.IsNullOrWhiteSpace(p.Key))
            .Select(p => new TaskParameter
            {
                Id = p.Id,
                Key = p.Key.Trim(),
                Value = p.Value?.Trim() ?? string.Empty
            })
            .ToList();

        var toRemove = entity.Parameters
            .Where(existing => incoming.All(p => p.Id == 0 || p.Id != existing.Id))
            .ToList();

        foreach (var removed in toRemove)
        {
            entity.Parameters.Remove(removed);
        }

        foreach (var incomingParam in incoming)
        {
            if (incomingParam.Id == 0)
            {
                entity.Parameters.Add(new TaskParameter
                {
                    Key = incomingParam.Key,
                    Value = incomingParam.Value
                });
                continue;
            }

            var existing = entity.Parameters.FirstOrDefault(p => p.Id == incomingParam.Id);
            if (existing is null)
            {
                entity.Parameters.Add(new TaskParameter
                {
                    Key = incomingParam.Key,
                    Value = incomingParam.Value
                });
                continue;
            }

            existing.Key = incomingParam.Key;
            existing.Value = incomingParam.Value;
        }
    }

    private static TimeOnly? ParseTimeOfDay(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        if (TimeOnly.TryParse(value, out var time))
        {
            return time;
        }

        return null;
    }
}

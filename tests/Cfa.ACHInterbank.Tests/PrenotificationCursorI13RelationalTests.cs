using Cfa.ACHInterbank.Domain.Entities.SchedulerTask;
using Cfa.ACHInterbank.Domain.Entities.SchedulerTask.enums;
using Cfa.ACHInterbank.Persistence.ACH.Quartz.Jobs.Implementation;
using Cfa.ACHInterbank.Persistence.DataBase;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Cfa.ACHInterbank.Tests;

/// <summary>
/// APN-I13: small relational regression for the existing I12 cursor storage.
/// SQLite is an in-process relational smoke test, NOT SQL Server/PostgreSQL UAT.
/// No job, scheduler, schema or production behavior is added here.
/// </summary>
public sealed class PrenotificationCursorI13RelationalTests
{
    private const int TaskId = 9313;
    private const string CursorKey = PrenotificationCandidateScan.CursorParameterKey;

    [Fact]
    public async Task I13_CursorPersistsAcrossDbContexts_WithoutOverwritingBatchSize()
    {
        await using var connection = new SqliteConnection("DataSource=:memory:");
        await connection.OpenAsync();
        var options = new DbContextOptionsBuilder<AchDbContext>().UseSqlite(connection).Options;

        await using (var db = NewContext(options))
        {
            await db.Database.EnsureCreatedAsync();
            db.TaskDefinitions.Add(new TaskDefinition
            {
                Id = TaskId, Code = "APN-I13-TEST", Name = "APN-I13 cursor persistence",
                PeriodicityType = PeriodicityTypeEnum.EveryNMinutes, N = 30
            });
            db.TaskParameters.AddRange(
                new TaskParameter { TaskDefinitionId = TaskId, Key = "BatchSize", Value = "500" },
                new TaskParameter { TaskDefinitionId = TaskId, Key = CursorKey, Value = "1201" });
            await db.SaveChangesAsync();
        }

        await using (var db = NewContext(options))
        {
            var parameter = await db.TaskParameters.SingleAsync(
                x => x.TaskDefinitionId == TaskId && x.Key == CursorKey);
            Assert.Equal(1201, PrenotificationCandidateScan.ParseCursor(parameter.Value));
            parameter.Value = "1402";
            await db.SaveChangesAsync();
        }

        await using (var db = NewContext(options))
        {
            var values = await db.TaskParameters.AsNoTracking()
                .Where(x => x.TaskDefinitionId == TaskId)
                .ToDictionaryAsync(x => x.Key, x => x.Value);
            Assert.Equal("1402", values[CursorKey]);
            Assert.Equal("500", values["BatchSize"]);
            Assert.Equal(2, values.Count);
        }
    }

    [Fact]
    public async Task I13_ExistingUniqueTaskParameterIndex_PreventsDuplicateCursor()
    {
        await using var connection = new SqliteConnection("DataSource=:memory:");
        await connection.OpenAsync();
        var options = new DbContextOptionsBuilder<AchDbContext>().UseSqlite(connection).Options;
        await using var db = NewContext(options);
        await db.Database.EnsureCreatedAsync();
        db.TaskDefinitions.Add(new TaskDefinition
        {
            Id = TaskId, Code = "APN-I13-UNIQUE", Name = "APN-I13 unique cursor",
            PeriodicityType = PeriodicityTypeEnum.EveryNMinutes, N = 30
        });
        db.TaskParameters.Add(new TaskParameter
        {
            TaskDefinitionId = TaskId, Key = CursorKey, Value = "10"
        });
        await db.SaveChangesAsync();

        db.TaskParameters.Add(new TaskParameter
        {
            TaskDefinitionId = TaskId, Key = CursorKey, Value = "11"
        });
        await Assert.ThrowsAsync<DbUpdateException>(() => db.SaveChangesAsync());
    }

    private static AchDbContext NewContext(DbContextOptions<AchDbContext> options) =>
        new(options) { AuditEnabled = false };
}

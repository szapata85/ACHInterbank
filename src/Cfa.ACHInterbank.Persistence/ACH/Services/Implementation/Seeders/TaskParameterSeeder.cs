using Cfa.ACHInterbank.Application.DataBase;
using Cfa.ACHInterbank.Domain.Entities.SchedulerTask;
using Cfa.ACHInterbank.Domain.Models.Configurations;
using Cfa.ACHInterbank.Persistence.DataBase;
using Microsoft.EntityFrameworkCore;

namespace Cfa.ACHInterbank.Persistence.ACH.Services.Implementation.Seeders;

[Scoped]
public class TaskParameterSeeder : IDbSeeder
{
    private readonly AchDbContext _context;

    public TaskParameterSeeder(AchDbContext context)
    {
        _context = context;
    }

    int IDbSeeder.Order => 5;

    public async Task SeedAsync()
    {
        var seedTask = await _context.TaskDefinitions
            .FirstOrDefaultAsync(t => t.Code == "SeedBankHolidays");

        if (seedTask is not null)
        {
            if (!_context.TaskParameters.Any(p => p.TaskDefinitionId == seedTask.Id && p.Key == "SeedNextYears"))
            {
                _context.TaskParameters.Add(new TaskParameter
                {
                    TaskDefinitionId = seedTask.Id,
                    Key = "SeedNextYears",
                    Value = "1"
                });
            }

            if (!_context.TaskParameters.Any(p => p.TaskDefinitionId == seedTask.Id && p.Key == "Years"))
            {
                _context.TaskParameters.Add(new TaskParameter
                {
                    TaskDefinitionId = seedTask.Id,
                    Key = "Years",
                    Value = string.Empty
                });
            }
        }

        var tacitTask = await _context.TaskDefinitions
            .FirstOrDefaultAsync(t => t.Code == "AchTacitAcceptanceJob");

        if (tacitTask is not null && !_context.TaskParameters.Any(p => p.TaskDefinitionId == tacitTask.Id && p.Key == "BatchSize"))
        {
            _context.TaskParameters.Add(new TaskParameter
            {
                TaskDefinitionId = tacitTask.Id,
                Key = "BatchSize",
                Value = "500"
            });
        }

        var contrapartidasTask = await _context.TaskDefinitions
            .FirstOrDefaultAsync(t => t.Code == "AchContrapartidasByCycle");

        if (contrapartidasTask is not null
            && !_context.TaskParameters.Any(p => p.TaskDefinitionId == contrapartidasTask.Id && p.Key == "MaxTransactionsPerCycle"))
        {
            _context.TaskParameters.Add(new TaskParameter
            {
                TaskDefinitionId = contrapartidasTask.Id,
                Key = "MaxTransactionsPerCycle",
                Value = "1000"
            });
        }

        if (contrapartidasTask is not null
            && !_context.TaskParameters.Any(p => p.TaskDefinitionId == contrapartidasTask.Id && p.Key == "ChunkSize"))
        {
            _context.TaskParameters.Add(new TaskParameter
            {
                TaskDefinitionId = contrapartidasTask.Id,
                Key = "ChunkSize",
                Value = "300"
            });
        }

        if (contrapartidasTask is not null
            && !_context.TaskParameters.Any(p => p.TaskDefinitionId == contrapartidasTask.Id && p.Key == "MaxCyclesPerRun"))
        {
            _context.TaskParameters.Add(new TaskParameter
            {
                TaskDefinitionId = contrapartidasTask.Id,
                Key = "MaxCyclesPerRun",
                Value = "20"
            });
        }

        var incomingNachaTask = await _context.TaskDefinitions
            .FirstOrDefaultAsync(t => t.Code == "IncomingNachaPostProcessing");

        if (incomingNachaTask is not null
            && !_context.TaskParameters.Any(p => p.TaskDefinitionId == incomingNachaTask.Id && p.Key == "ChunkSize"))
        {
            _context.TaskParameters.Add(new TaskParameter
            {
                TaskDefinitionId = incomingNachaTask.Id,
                Key = "ChunkSize",
                Value = "100"
            });
        }

        await _context.SaveChangesAsync();
    }
}

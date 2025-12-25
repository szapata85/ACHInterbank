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

        if (seedTask is null)
        {
            return;
        }

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

        await _context.SaveChangesAsync();
    }
}

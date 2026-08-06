using Cfa.ACHInterbank.Persistence.ACH.Services.Implementation.Seeders;
using Cfa.ACHInterbank.Persistence.DataBase;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;

namespace Cfa.ACHInterbank.Tests;

public sealed class SchedulerTaskDefinitionSeederTests
{
    [Fact]
    public async Task SeedAsync_ShouldBeIdempotent_AndPersistFunctionalMetadataAndSafeManualEvaluation()
    {
        var options = new DbContextOptionsBuilder<AchDbContext>()
            .UseInMemoryDatabase(nameof(SeedAsync_ShouldBeIdempotent_AndPersistFunctionalMetadataAndSafeManualEvaluation))
            .Options;
        await using var context = new AchDbContext(options);
        var seeder = new TaskDefinitionSeeder(context);

        await seeder.SeedAsync();
        await seeder.SeedAsync();

        var tasks = await context.TaskDefinitions.AsNoTracking().ToListAsync();
        tasks.Should().HaveCount(7);
        tasks.Select(x => x.Code).Should().OnlyHaveUniqueItems();
        tasks.Single(x => x.Code == "SeedBankHolidays").Name.Should().Be("Actualizar días festivos");
        tasks.Single(x => x.Code == "AchCycleSeeder").Name.Should().Be("Actualizar ciclos de compensación");
        var contrapartidas = tasks.Single(x => x.Code == "AchContrapartidasByCycle");
        contrapartidas.ManualExecutionEnabled.Should().BeTrue();
        contrapartidas.RetryOnFailure.Should().BeFalse();
        var transacciones = tasks.Single(x => x.Code == "IncomingNachaPostProcessing");
        transacciones.ManualExecutionEnabled.Should().BeTrue();
        transacciones.RetryOnFailure.Should().BeFalse();
    }
}

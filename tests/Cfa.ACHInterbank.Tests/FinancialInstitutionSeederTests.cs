using Cfa.ACHInterbank.Domain.Entities.Transactions.Enums;
using Cfa.ACHInterbank.Domain.Models.ACH;
using Cfa.ACHInterbank.Persistence.ACH.Services.Implementation.Seeders;
using Cfa.ACHInterbank.Persistence.DataBase;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using Moq;
using Xunit;

namespace Cfa.ACHInterbank.Tests;

public class FinancialInstitutionSeederTests
{
    [Fact]
    public async Task SeedAsync_RestauraCfaComoUnicoDefaultSource_CuandoExisteDriftUat()
    {
        using var connection = new SqliteConnection("DataSource=:memory:");
        await connection.OpenAsync();

        var options = new DbContextOptionsBuilder<AchDbContext>()
            .UseSqlite(connection)
            .Options;

        await using var context = new AchDbContext(options);
        await context.Database.EnsureCreatedAsync();

        var cfa = new FinancialInstitution
        {
            Id = 34,
            Name = "Cooperativa Financiera de Antioquia",
            RoutingNumber = "00001",
            TransitCode = "283",
            Status = FinancialInstitutionStatus.Active,
            IsDefaultSource = false
        };
        cfa.CalculateCheckDigit();

        var bancoUat = new FinancialInstitution
        {
            Id = 92,
            Name = "Banco UAT Origen",
            RoutingNumber = "99999",
            TransitCode = "001",
            Status = FinancialInstitutionStatus.Active,
            IsDefaultSource = true
        };
        bancoUat.CalculateCheckDigit();

        context.FinancialInstitutions.AddRange(cfa, bancoUat);
        await context.SaveChangesAsync();

        var environment = new Mock<IHostEnvironment>();
        environment.SetupGet(x => x.EnvironmentName).Returns("Development");
        var sut = new FinancialInstitutionSeeder(context, environment.Object);

        await sut.SeedAsync();

        var defaults = await context.FinancialInstitutions
            .Where(x => x.IsDefaultSource && x.Status == FinancialInstitutionStatus.Active)
            .OrderBy(x => x.Id)
            .ToListAsync();

        Assert.Single(defaults);
        Assert.Equal("Cooperativa Financiera de Antioquia", defaults[0].Name);
        Assert.False(await context.FinancialInstitutions
            .Where(x => x.Name == "Banco UAT Origen")
            .Select(x => x.IsDefaultSource)
            .SingleAsync());
    }
}

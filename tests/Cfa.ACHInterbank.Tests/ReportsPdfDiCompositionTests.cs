using Cfa.ACHInterbank.Application.Reports.Interfaces;
using Cfa.ACHInterbank.Persistence;
using Cfa.ACHInterbank.Persistence.Reports;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Cfa.ACHInterbank.Tests;

public sealed class ReportsPdfDiCompositionTests
{
    [Fact]
    public void AddPersistence_ShouldRegisterQuestPdfReportGenerator()
    {
        var services = new ServiceCollection();
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Database:Provider"] = "SqlServer",
                ["ConnectionStrings:SqlConnection"] = "Server=localhost;Database=test;User Id=test;Password=test;TrustServerCertificate=True"
            })
            .Build();

        services.AddLogging();
        services.AddPersistence(configuration);

        services.Should().Contain(descriptor =>
            descriptor.ServiceType == typeof(IReportGenerator)
            && descriptor.ImplementationType == typeof(QuestPdfReportGenerator)
            && descriptor.Lifetime == ServiceLifetime.Scoped);
    }
}

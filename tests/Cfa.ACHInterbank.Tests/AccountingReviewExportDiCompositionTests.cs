using System.Text;
using Cfa.ACHInterbank.Api.Controllers;
using Cfa.ACHInterbank.Application;
using Cfa.ACHInterbank.Application.ACH.Interfaces;
using Cfa.ACHInterbank.Application.Reports.Export.Implementation;
using Cfa.ACHInterbank.Application.Reports.Export.Interfaces;
using Cfa.ACHInterbank.Application.Reports.Interfaces;
using Cfa.ACHInterbank.Application.Reports.Implementation;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Moq;

namespace Cfa.ACHInterbank.Tests;

public class AccountingReviewExportDiCompositionTests
{
    [Fact]
    public void AddApplication_ShouldRegisterAccountingReviewExportServices()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddApplication();

        services.Should().Contain(s => s.ServiceType == typeof(IAccountingReviewExportAppService) && s.ImplementationType == typeof(AccountingReviewExportAppService));
        services.Should().Contain(s => s.ServiceType == typeof(IAccountingReviewReportExporter) && s.ImplementationType == typeof(AccountingReviewReportExporter));
        services.Should().Contain(s => s.ServiceType == typeof(IAccountingReviewReportBuilder) && s.ImplementationType == typeof(AccountingReviewReportBuilder));
    }

    [Fact]
    public async Task AccountingReviewExportAppService_ShouldResolveWithDependencies()
    {
        using var provider = BuildExportOnlyProvider();

        var service = provider.GetRequiredService<IAccountingReviewExportAppService>();
        var result = await service.ExportAsync(new AccountingReviewExportApiRequest { Format = "csv", RequestedBy = "qa" }, CancellationToken.None);

        result.Content.Should().NotBeNullOrEmpty();
        result.ContentType.Should().Be("text/csv");
        result.FileName.Should().EndWith(".csv");

        var csv = Encoding.UTF8.GetString(result.Content);
        csv.Should().Contain("NO contabiliza");
        csv.Should().Contain("FRONTERA_NO_CONTABLE");
    }

    [Fact]
    public async Task ReportsController_ShouldBeConstructable_WithAccountingReviewExportDependency()
    {
        using var provider = BuildControllerProvider();

        var controller = ActivatorUtilities.CreateInstance<ReportsController>(provider);
        var action = await controller.ExportAccountingReview(new AccountingReviewExportApiRequest { Format = "csv", RequestedBy = "qa" }, CancellationToken.None);

        controller.Should().NotBeNull();
        action.Should().BeOfType<FileContentResult>().Which.ContentType.Should().Be("text/csv");
    }

    [Fact]
    public void AddApplication_ShouldRegisterAccountingReviewExportServices_ByConventionNames()
    {
        typeof(AccountingReviewExportAppService).GetInterfaces().Should().Contain(typeof(IAccountingReviewExportAppService));
        typeof(AccountingReviewReportExporter).GetInterfaces().Should().Contain(typeof(IAccountingReviewReportExporter));
        typeof(AccountingReviewReportBuilder).GetInterfaces().Should().Contain(typeof(IAccountingReviewReportBuilder));

        typeof(IAccountingReviewExportAppService).Name.Should().Be($"I{nameof(AccountingReviewExportAppService)}");
        typeof(IAccountingReviewReportExporter).Name.Should().Be($"I{nameof(AccountingReviewReportExporter)}");
        typeof(IAccountingReviewReportBuilder).Name.Should().Be($"I{nameof(AccountingReviewReportBuilder)}");
    }

    [Theory]
    [InlineData("pdf", "application/pdf")]
    [InlineData("csv", "text/csv")]
    [InlineData("xlsx", "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet")]
    public async Task DiComposition_ShouldNotRequirePersistenceForExportOnlyPath(string format, string expectedContentType)
    {
        using var provider = BuildExportOnlyProvider();

        var service = provider.GetRequiredService<IAccountingReviewExportAppService>();
        var result = await service.ExportAsync(new AccountingReviewExportApiRequest { Format = format, RequestedBy = "qa" }, CancellationToken.None);

        result.ContentType.Should().Be(expectedContentType);
        result.Content.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task DiComposition_ShouldKeepNonAccountingBoundary()
    {
        using var provider = BuildExportOnlyProvider();

        var service = provider.GetRequiredService<IAccountingReviewExportAppService>();
        var result = await service.ExportAsync(new AccountingReviewExportApiRequest { Format = "csv", RequestedBy = "qa" }, CancellationToken.None);
        var csv = Encoding.UTF8.GetString(result.Content);

        csv.Should().Contain("NO contabiliza");
        csv.Should().NotContain("LedgerId").And.NotContain("JournalId").And.NotContain("PostingId").And.NotContain("AccountingEntryId").And.NotContain("AccountingPosted").And.NotContain("CudSettlementApi");
    }

    private static ServiceProvider BuildExportOnlyProvider()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddApplication();
        return services.BuildServiceProvider(new ServiceProviderOptions { ValidateScopes = true, ValidateOnBuild = false });
    }

    private static ServiceProvider BuildControllerProvider()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddApplication();
        services.AddSingleton(Mock.Of<IReportGenerator>());
        services.AddSingleton(Mock.Of<IAchTransactionReportService>());
        services.AddSingleton(Mock.Of<IAchReturnRejectionReportService>());
        services.AddSingleton(Mock.Of<IAchNachaCycleReportService>());
        services.AddSingleton(Mock.Of<IAchReconciliationReportService>());
        services.AddSingleton(Mock.Of<IAchAuditHistoryReportService>());
        services.AddSingleton(Mock.Of<IClearingHouseService>());
        return services.BuildServiceProvider(new ServiceProviderOptions { ValidateScopes = true, ValidateOnBuild = false });
    }
}

using System.Reflection;
using Cfa.ACHInterbank.Api.Controllers;
using Cfa.ACHInterbank.Application.Reports.Models;
using Cfa.ACHInterbank.Persistence.ACH.Services.Implementation;
using Xunit;

namespace Cfa.ACHInterbank.Tests;

public sealed class AccountingReviewBoundaryCharacterizationTests
{
    private static readonly string[] ForbiddenServiceNames =
    [
        "AccountingPostingService",
        "LedgerPostingService",
        "JournalEntryService"
    ];

    private static readonly string[] ForbiddenEntityNames =
    [
        "JournalEntry",
        "LedgerEntry",
        "AccountingEntry",
        "AccountingPosting",
        "AccountingMovement"
    ];

    private static readonly string[] ForbiddenControllerNames =
    [
        "AccountingController",
        "LedgerController",
        "JournalController",
        "PostingController"
    ];

    [Fact]
    public void System_ShouldNotExposeAccountingPostingServices_CurrentBoundary()
    {
        var productiveAssemblies = GetProductiveAssemblies();
        var allTypes = productiveAssemblies.SelectMany(a => a.GetTypes()).ToArray();

        foreach (var forbidden in ForbiddenServiceNames)
        {
            Assert.DoesNotContain(allTypes, t => t.Name.Equals(forbidden, StringComparison.Ordinal));
        }

        var accountingLikeTypes = allTypes
            .Where(t => t.Name.Contains("Accounting", StringComparison.OrdinalIgnoreCase))
            .Select(t => t.FullName ?? t.Name)
            .ToArray();

        Assert.All(accountingLikeTypes, typeName =>
            Assert.DoesNotContain("Posting", typeName, StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void System_ShouldNotExposeJournalOrLedgerEntities_CurrentBoundary()
    {
        var productiveAssemblies = GetProductiveAssemblies();
        var allTypes = productiveAssemblies.SelectMany(a => a.GetTypes()).ToArray();

        foreach (var forbidden in ForbiddenEntityNames)
        {
            Assert.DoesNotContain(allTypes, t => t.Name.Equals(forbidden, StringComparison.Ordinal));
        }

        Assert.Contains(allTypes, t => t.Name == "AchTransactionStateEvent");
    }

    [Fact]
    public void System_ShouldNotExposeAccountingApiEndpoints_CurrentBoundary()
    {
        var apiAssembly = typeof(ReportsController).Assembly;
        var controllers = apiAssembly.GetTypes().Where(t => t.Name.EndsWith("Controller", StringComparison.Ordinal)).ToArray();

        foreach (var forbidden in ForbiddenControllerNames)
        {
            Assert.DoesNotContain(controllers, c => c.Name.Equals(forbidden, StringComparison.Ordinal));
        }

        Assert.Contains(controllers, c => c.Name == nameof(ReportsController));
    }

    [Fact]
    public void Reports_ShouldBeClassifiedAsAccountingReviewSupport_NotAccountingPosting()
    {
        var methodNames = typeof(ReportsController).GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly)
            .Select(m => m.Name)
            .ToArray();

        Assert.Contains(methodNames, m => m.Contains("Report", StringComparison.OrdinalIgnoreCase)
            || m.Contains("Transactions", StringComparison.OrdinalIgnoreCase)
            || m.Contains("Reconciliation", StringComparison.OrdinalIgnoreCase));

        Assert.DoesNotContain(methodNames, m => m.Contains("PostAccounting", StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(methodNames, m => m.Contains("CreateJournalEntry", StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(methodNames, m => m.Contains("GenerateAccountingEntry", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void IncomingOrphanManualResolution_ShouldRemainAuditOnly_NotAccountingPosting()
    {
        var methods = typeof(IncomingNachaOrphanManualResolutionService).GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly)
            .Select(m => m.Name)
            .ToArray();

        Assert.Contains(methods, m => m.Contains("Resolve", StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(methods, m => m.Contains("Journal", StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(methods, m => m.Contains("Ledger", StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(methods, m => m.Contains("Posting", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void IncomingUnresolvedOrphan_ShouldNotBeAccountingApplied()
    {
        var methods = typeof(IncomingNachaOrphanManualResolutionService).GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly)
            .Select(m => m.Name)
            .ToArray();

        Assert.Contains(methods, m => m.Contains("Resolve", StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(methods, m => m.Contains("Accounting", StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(methods, m => m.Contains("Journal", StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(methods, m => m.Contains("Ledger", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void RejectedTotal_ShouldNotCreateAccountingPosting()
    {
        var methods = typeof(Cfa.ACHInterbank.Persistence.Reports.AchReturnRejectionReportService)
            .GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly)
            .Select(m => m.Name)
            .ToArray();

        Assert.Contains("GetRejectionsAsync", methods);
        Assert.DoesNotContain(methods, m => m.Contains("Accounting", StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(methods, m => m.Contains("Posting", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void RejectedPartial_ShouldRepresentRecordLevelReporting_NotAmountPartialAccounting()
    {
        var responseType = typeof(Cfa.ACHInterbank.Application.Reports.Models.AchReturnRejectionReportResponseDto);
        var properties = responseType.GetProperties().Select(p => p.Name).ToArray();

        Assert.Contains("Items", properties);
        Assert.Contains("Totals", properties);
        Assert.DoesNotContain("PartialAmount", properties);
        Assert.DoesNotContain("AccountingEntry", properties);
    }

    [Fact]
    public void ReturnOfReturn_ShouldBeReportableButNotAccountingPosted_CurrentBoundary()
    {
        var controller = typeof(Cfa.ACHInterbank.Api.Controllers.AchReturnOfReturnController);
        var methodNames = controller.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly)
            .Select(m => m.Name)
            .ToArray();

        Assert.Contains("Evaluate", methodNames);
        Assert.Contains("GenerateAuditFile", methodNames);
        Assert.Contains("GenerateNachaFile", methodNames);
        Assert.DoesNotContain(methodNames, m => m.Contains("AccountingPosted", StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(methodNames, m => m.Contains("Journal", StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(methodNames, m => m.Contains("Ledger", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void ReportsController_ShouldExposeOperationalReports_ForThirdPartyReview()
    {
        var methods = typeof(ReportsController).GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly)
            .Select(m => m.Name)
            .ToArray();

        Assert.Contains("GetSentTransactions", methods);
        Assert.Contains("GetReceivedTransactions", methods);
        Assert.Contains("GetReturns", methods);
        Assert.Contains("GetRejections", methods);
        Assert.Contains("GetNachaFiles", methods);
        Assert.Contains("GetCycles", methods);
        Assert.Contains("GetReconciliation", methods);
        Assert.Contains("GetAudit", methods);
        Assert.Contains("GetHistory", methods);
    }

    [Fact]
    public void Traceability_ShouldCorrelateTransactionWithFileCycleEvents_ForReview()
    {
        var methods = typeof(AchTraceabilityService).GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly)
            .Select(m => m.Name)
            .ToArray();

        Assert.Contains("GetTransactionTraceabilityAsync", methods);
        Assert.Contains("GetTraceabilityReportAsync", methods);
        Assert.DoesNotContain(methods, m => m.Contains("Journal", StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(methods, m => m.Contains("Ledger", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void ReconciliationReport_ShouldBeOperationalDifferenceReport_NotAccountingLedger()
    {
        var props = typeof(AchReconciliationReportResponseDto).GetProperties().Select(p => p.Name).ToArray();
        Assert.Contains("Differences", props);
        Assert.DoesNotContain("Ledger", props);
        Assert.DoesNotContain("Journal", props);
        Assert.DoesNotContain("AccountingPosted", props);
    }

    [Fact]
    public void Reporting_ShouldNotHaveFormalAccountingReviewStateMachine_Yet()
    {
        var allTypes = GetProductiveAssemblies().SelectMany(a => a.GetTypes()).ToArray();
        var forbiddenStates = new[]
        {
            "ReportPending", "ReportGenerated", "ReportExported",
            "ReconciliationPending", "Reconciled", "ReconciliationMismatch",
            "ThirdPartyAccountingReviewPending", "ThirdPartyAccountingReviewed"
        };

        foreach (var stateName in forbiddenStates)
        {
            Assert.DoesNotContain(allTypes, t => t.Name.Equals(stateName, StringComparison.Ordinal));
        }
    }

    [Fact]
    public void Reporting_ShouldNotExposeCudAccountingSettlementApi_CurrentBoundary()
    {
        var controllerNames = typeof(ReportsController).Assembly.GetTypes()
            .Where(t => t.Name.EndsWith("Controller", StringComparison.Ordinal))
            .Select(t => t.Name)
            .ToArray();

        Assert.DoesNotContain(controllerNames, n => n.Contains("Cud", StringComparison.OrdinalIgnoreCase) && n.Contains("Settlement", StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(controllerNames, n => n.Contains("Accounting", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void ReportingBoundary_ShouldNotUseWordsAsientoOrLedgerInProductiveApiSurface()
    {
        var apiTypes = typeof(ReportsController).Assembly.GetTypes()
            .Where(t => t.IsClass && t.Namespace != null && t.Namespace.Contains("Controllers", StringComparison.Ordinal))
            .ToArray();

        var names = apiTypes.SelectMany(t => t.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly)
            .Select(m => $"{t.Name}.{m.Name}")).ToArray();

        Assert.DoesNotContain(names, n => n.Contains("Asiento", StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(names, n => n.Contains("Ledger", StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(names, n => n.Contains("Journal", StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(names, n => n.Contains("Posting", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Reporting_ShouldSupportThirdPartyReviewTerminology()
    {
        Assert.NotNull(typeof(ReportsController));
        Assert.NotNull(typeof(AchTraceabilityController));
        Assert.NotNull(typeof(Cfa.ACHInterbank.Persistence.Reports.AchTransactionReportService));
        Assert.NotNull(typeof(Cfa.ACHInterbank.Persistence.Reports.AchAuditHistoryReportService));
    }

    private static Assembly[] GetProductiveAssemblies()
    {
        return
        [
            typeof(ReportsController).Assembly,
            typeof(AchTraceabilityService).Assembly,
            typeof(AchReconciliationReportResponseDto).Assembly,
            typeof(Cfa.ACHInterbank.Domain.Models.ACH.AchTransaction).Assembly
        ];
    }
}

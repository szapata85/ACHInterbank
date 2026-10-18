using System.Reflection;
using Cfa.ACHInterbank.Api.Controllers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Routing;
using Xunit;

namespace Cfa.ACHInterbank.Tests;

public sealed class ReportsR8AContractCharacterizationTests
{
    private static readonly IReadOnlyDictionary<string, (string Verb, string Route, string Policy)> ExpectedActions =
        new Dictionary<string, (string Verb, string Route, string Policy)>(StringComparer.Ordinal)
        {
            ["GetSentTransactions"] = ("GET", "transactions/sent", "CanReadAch"),
            ["GetReceivedTransactions"] = ("GET", "transactions/received", "CanReadAch"),
            ["GetSentTransactionsPdf"] = ("GET", "transactions/sent/pdf", "CanReadAch"),
            ["GetReceivedTransactionsPdf"] = ("GET", "transactions/received/pdf", "CanReadAch"),
            ["GetReturns"] = ("GET", "returns", "CanReadAch"),
            ["GetRejections"] = ("GET", "rejections", "CanReadAch"),
            ["GetReturnsPdf"] = ("GET", "returns/pdf", "CanReadAch"),
            ["GetRejectionsPdf"] = ("GET", "rejections/pdf", "CanReadAch"),
            ["GetNachaFiles"] = ("GET", "nacha-files", "CanReadAch"),
            ["GetNachaFilesPdf"] = ("GET", "nacha-files/pdf", "CanReadAch"),
            ["GetCycles"] = ("GET", "cycles", "CanReadAch"),
            ["GetCyclesPdf"] = ("GET", "cycles/pdf", "CanReadAch"),
            ["GetReconciliation"] = ("GET", "reconciliation", "CanReadAch"),
            ["GetReconciliationPdf"] = ("GET", "reconciliation/pdf", "CanReadAch"),
            ["GetAudit"] = ("GET", "audit", "CanReadAch"),
            ["GetAuditPdf"] = ("GET", "audit/pdf", "CanReadAch"),
            ["GetHistory"] = ("GET", "history", "CanReadAch"),
            ["GetHistoryPdf"] = ("GET", "history/pdf", "CanReadAch"),
            ["GetTraceabilityPdf"] = ("GET", "traceability/pdf", "CanReadAch"),
            ["ExportAccountingReview"] = ("POST", "accounting-review/export", "CanReadAch")
        };

    [Fact]
    public void ReportsHttpContract_ShouldRemainRoutePolicyAndOpenApiControllerNameEquivalent()
    {
        var apiAssembly = typeof(AuthController).Assembly;
        var reportActions = apiAssembly.GetTypes()
            .Where(type => !type.IsAbstract && typeof(ControllerBase).IsAssignableFrom(type))
            .Where(type => type.GetCustomAttributes<RouteAttribute>(inherit: true)
                .Any(route => string.Equals(route.Template, "api/reports", StringComparison.Ordinal)))
            .SelectMany(type => type.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly)
                .Select(method => (Controller: type, Method: method, Http: method.GetCustomAttributes<HttpMethodAttribute>(inherit: true).SingleOrDefault()))
                .Where(entry => entry.Http is not null))
            .ToArray();

        Assert.Equal(ExpectedActions.Count, reportActions.Length);

        foreach (var expected in ExpectedActions)
        {
            var action = Assert.Single(reportActions, candidate => candidate.Method.Name == expected.Key);
            var http = Assert.IsAssignableFrom<HttpMethodAttribute>(action.Http);

            Assert.Equal(expected.Value.Route, http.Template);
            Assert.Equal(expected.Value.Verb, Assert.Single(http.HttpMethods));

            var policies = action.Method.GetCustomAttributes<AuthorizeAttribute>(inherit: true)
                .Select(attribute => attribute.Policy)
                .Where(policy => !string.IsNullOrWhiteSpace(policy))
                .Distinct(StringComparer.Ordinal)
                .ToArray();
            Assert.Equal(expected.Value.Policy, Assert.Single(policies));

            Assert.Contains(action.Controller.GetCustomAttributes<AuthorizeAttribute>(inherit: true),
                attribute => string.IsNullOrWhiteSpace(attribute.Policy));

            var logicalControllerName = action.Controller.GetCustomAttribute<ControllerContractNameAttribute>(inherit: true)?.Name
                ?? action.Controller.Name[..^"Controller".Length];
            Assert.Equal("Reports", logicalControllerName);
        }
    }
}

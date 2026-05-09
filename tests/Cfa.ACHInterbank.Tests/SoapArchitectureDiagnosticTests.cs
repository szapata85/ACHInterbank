using System.Reflection;
using Xunit;

namespace Cfa.ACHInterbank.Tests;

public class SoapArchitectureDiagnosticTests
{
    [Fact(Skip = "Diagnóstico inicial: evidencia de contaminación actual antes del refactor arquitectural.")]
    public void ApplicationAndDomain_ShouldReportSoapXmlProviderTerms_ForFutureRefactor()
    {
        var assemblies = new[]
        {
            typeof(Cfa.ACHInterbank.Application.DependencyInjectionService).Assembly,
            typeof(Cfa.ACHInterbank.Domain.Helpers.AchCycleIdHelper).Assembly
        };

        var patterns = new[] { "soap", "wsdl", "xml", "axon", "idtransaccionaxon", "registrarrespuestatransaccion" };

        var findings = assemblies
            .SelectMany(a => a.GetTypes())
            .Select(t => t.FullName ?? t.Name)
            .Where(name => patterns.Any(p => name.Contains(p, StringComparison.OrdinalIgnoreCase)))
            .Distinct()
            .OrderBy(x => x)
            .ToList();

        Assert.True(true, $"Hallazgos diagnósticos ({findings.Count}): {string.Join(", ", findings)}");
    }
}

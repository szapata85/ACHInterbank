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

    [Fact]
    public void NewAchResponseContracts_ShouldNotContainSoapOrProviderTerms()
    {
        var contractTypes = new[]
        {
            typeof(Cfa.ACHInterbank.Application.ACH.Responses.Models.RegistrarRespuestaAchCommand),
            typeof(Cfa.ACHInterbank.Application.ACH.Responses.Models.ResultadoRegistroRespuestaAch),
            typeof(Cfa.ACHInterbank.Application.ACH.Responses.Interfaces.IRespuestaTransaccionesAchGateway),
            typeof(Cfa.ACHInterbank.Application.ACH.Responses.Interfaces.IRegistrarRespuestaAchUseCase)
        };

        var forbiddenTerms = new[] { "Axon", "Soap", "Xml", "Wsdl", "Envelope", "idTransaccionAxon", "RegistrarRespuestaTransaccion" };

        foreach (var type in contractTypes)
        {
            Assert.DoesNotContain(forbiddenTerms, term => (type.FullName ?? type.Name).Contains(term, StringComparison.OrdinalIgnoreCase));

            foreach (var property in type.GetProperties())
            {
                Assert.DoesNotContain(forbiddenTerms, term => property.Name.Contains(term, StringComparison.OrdinalIgnoreCase));
                Assert.DoesNotContain(forbiddenTerms, term => (property.PropertyType.FullName ?? property.PropertyType.Name).Contains(term, StringComparison.OrdinalIgnoreCase));
            }

            foreach (var method in type.GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly))
            {
                Assert.DoesNotContain(forbiddenTerms, term => method.Name.Contains(term, StringComparison.OrdinalIgnoreCase));
            }
        }
    }

}

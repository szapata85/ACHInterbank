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
    public void Domain_ShouldNotReferenceApplicationAssembly()
    {
        var domainAssembly = typeof(Cfa.ACHInterbank.Domain.Models.ACH.AchResponseStatusMapping).Assembly;
        var referenced = domainAssembly.GetReferencedAssemblies();

        Assert.DoesNotContain(referenced, x => string.Equals(x.Name, "Cfa.ACHInterbank.Application", StringComparison.Ordinal));
    }

    [Fact]
    public void There_ShouldBeOnlyOneTipoRespuestaAchType()
    {
        var assemblies = new[]
        {
            typeof(Cfa.ACHInterbank.Application.DependencyInjectionService).Assembly,
            typeof(Cfa.ACHInterbank.Domain.Helpers.AchCycleIdHelper).Assembly
        };

        var matches = assemblies
            .SelectMany(a => a.GetTypes())
            .Where(t => string.Equals(t.Name, "TipoRespuestaAch", StringComparison.Ordinal))
            .ToList();

        Assert.Single(matches);
        Assert.Equal("Cfa.ACHInterbank.Domain.Models.ACH.Enums.TipoRespuestaAch", matches[0].FullName);
    }

    [Fact]
    public void NewAchResponseContracts_ShouldNotContainSoapOrProviderTerms()
    {
        var contractTypes = new[]
        {
            typeof(Cfa.ACHInterbank.Domain.Models.ACH.Enums.TipoRespuestaAch),
            typeof(Cfa.ACHInterbank.Application.ACH.Responses.Models.RegistrarRespuestaAchCommand),
            typeof(Cfa.ACHInterbank.Application.ACH.Responses.Models.ResultadoRegistroRespuestaAch),
            typeof(Cfa.ACHInterbank.Application.ACH.Responses.Interfaces.IRespuestaTransaccionesAchGateway),
            typeof(Cfa.ACHInterbank.Application.ACH.Responses.Interfaces.IRegistrarRespuestaAchUseCase),
            typeof(Cfa.ACHInterbank.Application.ACH.Responses.Homologation.Models.AchResponseStatusMappingModel),
            typeof(Cfa.ACHInterbank.Application.ACH.Responses.Homologation.Models.HomologarRespuestaAchRequest),
            typeof(Cfa.ACHInterbank.Application.ACH.Responses.Homologation.Models.HomologarRespuestaAchResult),
            typeof(Cfa.ACHInterbank.Application.ACH.Responses.Homologation.Interfaces.IAchResponseStatusMappingRepository),
            typeof(Cfa.ACHInterbank.Application.ACH.Responses.Homologation.Interfaces.IRespuestaAchStatusMappingService),
            typeof(Cfa.ACHInterbank.Application.ACH.Responses.Homologation.Services.RespuestaAchStatusMappingService),
            typeof(Cfa.ACHInterbank.Domain.Models.ACH.AchResponseStatusMapping),
            typeof(Cfa.ACHInterbank.Persistence.ACH.Repositories.Implementation.AchResponseStatusMappingRepository)
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

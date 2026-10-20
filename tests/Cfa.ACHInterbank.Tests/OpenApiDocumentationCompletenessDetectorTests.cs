using Microsoft.OpenApi;

namespace Cfa.ACHInterbank.Tests;

public sealed class OpenApiDocumentationCompletenessDetectorTests
{
    [Fact]
    public void Detect_ShouldIdentifyMissingEmptyOrWhitespaceDocumentation()
    {
        var missingDocumentation = new OpenApiOperation();
        var emptySummary = new OpenApiOperation
        {
            Summary = string.Empty,
            Description = "Explica de forma específica el comportamiento observado de la operación."
        };
        var whitespaceDescription = new OpenApiOperation
        {
            Summary = "Consulta una configuración específica.",
            Description = "   \t\r\n "
        };

        OpenApiDocumentationCompletenessDetector.Detect(missingDocumentation)
            .Should().Be(
                OpenApiDocumentationIssue.SummaryIncomplete |
                OpenApiDocumentationIssue.DescriptionIncomplete);
        OpenApiDocumentationCompletenessDetector.Detect(emptySummary)
            .Should().Be(OpenApiDocumentationIssue.SummaryIncomplete);
        OpenApiDocumentationCompletenessDetector.Detect(whitespaceDescription)
            .Should().Be(OpenApiDocumentationIssue.DescriptionIncomplete);
    }

    [Theory]
    [InlineData("Endpoint de la API ACH Interbank.")]
    [InlineData("   ENDPOINT   DE LA API   ach interbank.   ")]
    [InlineData("Operación del módulo X.")]
    [InlineData("  OPERACIÓN   DEL   MÓDULO   Transacciones.  ")]
    [InlineData("Consulta información del sistema.")]
    [InlineData("  consulta   INFORMACIÓN del   sistema.  ")]
    [InlineData("DOCUMENTATION INCOMPLETE")]
    [InlineData("  documentation   incomplete  ")]
    public void Detect_ShouldIdentifyKnownPlaceholdersIgnoringCaseAndExtraWhitespace(string placeholder)
    {
        var operation = new OpenApiOperation
        {
            Summary = placeholder,
            Description = placeholder
        };

        OpenApiDocumentationCompletenessDetector.Detect(operation)
            .Should().Be(
                OpenApiDocumentationIssue.SummaryIncomplete |
                OpenApiDocumentationIssue.DescriptionIncomplete);
        OpenApiDocumentationCompletenessDetector.IsIncomplete(operation).Should().BeTrue();
    }

    [Fact]
    public void Detect_ShouldAcceptSpecificDocumentation()
    {
        var operation = new OpenApiOperation
        {
            Summary = "Consulta el ciclo activo configurado para una cámara de compensación.",
            Description = "Devuelve la configuración del ciclo activo que ya existe para la cámara indicada por la operación."
        };

        OpenApiDocumentationCompletenessDetector.Detect(operation)
            .Should().Be(OpenApiDocumentationIssue.None);
        OpenApiDocumentationCompletenessDetector.IsIncomplete(operation).Should().BeFalse();
    }
}

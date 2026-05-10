namespace Cfa.ACHInterbank.Tests;

public class AchResponsesApiIntegrationTests
{
    private const string SkipReason = "Host de integración aislado pendiente por dependencias globales del pipeline (seguirá en siguiente iteración).";

    [Fact(Skip = SkipReason)] public void ProcessEndpoint_ShouldReturnBadRequest_WhenRequestInvalid() { }
    [Fact(Skip = SkipReason)] public void ProcessEndpoint_ShouldReturnOk_WhenUseCaseReturnsProcessed() { }
    [Fact(Skip = SkipReason)] public void ProcessEndpoint_ShouldReturnUnprocessable_WhenUseCaseReturnsNotProcessed() { }
    [Fact(Skip = SkipReason)] public void SendNotificationEndpoint_ShouldReturnBadRequest_WhenRequestInvalid() { }
    [Fact(Skip = SkipReason)] public void SendNotificationEndpoint_ShouldReturnNotFound_WhenAttemptNotFound() { }
    [Fact(Skip = SkipReason)] public void SendNotificationEndpoint_ShouldReturnOk_WhenFunctionalErrorAudited() { }
    [Fact(Skip = SkipReason)] public void SendNotificationEndpoint_ShouldReturnOk_WhenTechnicalErrorAudited() { }
    [Fact(Skip = SkipReason)] public void SearchEndpoint_ShouldReturnOkPagedResponse() { }
    [Fact(Skip = SkipReason)] public void DetailEndpoint_ShouldReturnNotFound_WhenMissing() { }
    [Fact(Skip = SkipReason)] public void AttemptsEndpoint_ShouldReturnOkList() { }
    [Fact(Skip = SkipReason)] public void MappingsEndpoint_ShouldReturnBadRequest_WhenTipoRespuestaInvalid() { }
    [Fact(Skip = SkipReason)] public void MappingsEndpoint_ShouldReturnOk_WhenTipoRespuestaValid() { }
    [Fact(Skip = SkipReason)] public void OpenApi_ShouldReturnSuccess() { }
    [Fact(Skip = SkipReason)] public void OpenApi_ShouldIncludeAchResponseEndpoints() { }
    [Fact(Skip = SkipReason)] public void OpenApi_ShouldNotExposePhysicalSoapFieldInAchResponseSchemas() { }
}

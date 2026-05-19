using Cfa.ACHInterbank.Api.Contracts.AchResponses;
using Cfa.ACHInterbank.Api.Controllers;
using Cfa.ACHInterbank.Api.Mappers.AchResponses;
using Cfa.ACHInterbank.Api.Validation.AchResponses;
using Cfa.ACHInterbank.Application.ACH.Responses.Homologation.Interfaces;
using Cfa.ACHInterbank.Application.ACH.Responses.Notification.Interfaces;
using Cfa.ACHInterbank.Application.ACH.Responses.Notification.Models;
using Cfa.ACHInterbank.Application.ACH.Responses.Processing.Interfaces;
using Cfa.ACHInterbank.Application.ACH.Responses.Processing.Models;
using Cfa.ACHInterbank.Application.ACH.Responses.Queries.Models;
using Cfa.ACHInterbank.Application.ACH.Responses.Repositories;
using Cfa.ACHInterbank.Domain.Models.ACH.Enums;
using Cfa.ACHInterbank.Persistence.DataBase;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Moq;
using System.Reflection;

namespace Cfa.ACHInterbank.Tests;

public class AchResponsesControllerTests
{
    [Fact]
    public void AchResponsesController_ShouldRequireAuthorization()
    {
        var controllerType = typeof(AchResponsesController);

        controllerType.GetCustomAttribute<AuthorizeAttribute>().Should().NotBeNull();
        controllerType.GetCustomAttribute<AllowAnonymousAttribute>().Should().BeNull();
    }

    [Fact]
    public async Task Process_ShouldReturnBadRequest_WhenRequestInvalid(){var c=new AchResponsesController();var r=await c.Process(new("X","","",null,null,"",null,null,0,"",0,null,null),new ProcesarRespuestaAchRequestValidator(),new ProcesarRespuestaAchApiMapper(),Mock.Of<IProcesarRespuestaAchUseCase>(),default);r.Should().BeOfType<BadRequestObjectResult>();}

    [Fact]
    public async Task Process_ShouldReturnOk_WhenUseCaseProcessesResponse(){var uc=new Mock<IProcesarRespuestaAchUseCase>();uc.Setup(x=>x.ExecuteAsync(It.IsAny<ProcesarRespuestaAchCommand>(),It.IsAny<CancellationToken>())).ReturnsAsync(new ProcesarRespuestaAchResult(Guid.NewGuid(),true,false,true,true,true,AchResponseProcessingStatus.Notificada,null,"h"));var c=new AchResponsesController();var r=await c.Process(new("Transaccion","TX","ACH",null,null,"E",null,null,1,"C",1,null,null),new ProcesarRespuestaAchRequestValidator(),new ProcesarRespuestaAchApiMapper(),uc.Object,default);r.Should().BeOfType<OkObjectResult>();}

    [Fact]
    public async Task Process_ShouldReturnUnprocessableOrBadRequest_WhenApplicationValidationFails(){var uc=new Mock<IProcesarRespuestaAchUseCase>();uc.Setup(x=>x.ExecuteAsync(It.IsAny<ProcesarRespuestaAchCommand>(),It.IsAny<CancellationToken>())).ReturnsAsync(new ProcesarRespuestaAchResult(null,false,false,false,false,false,AchResponseProcessingStatus.ErrorFuncional,"x",null));var c=new AchResponsesController();var r=await c.Process(new("Transaccion","TX","ACH",null,null,"E",null,null,1,"C",1,null,null),new ProcesarRespuestaAchRequestValidator(),new ProcesarRespuestaAchApiMapper(),uc.Object,default);r.Should().BeOfType<UnprocessableEntityObjectResult>();}

    [Fact]
    public async Task SendNotification_ShouldReturnBadRequest_WhenRequestInvalid(){var c=new AchResponsesController();var r=await c.SendNotification(new(0,null),new NotificarRespuestaAchRequestValidator(),new NotificarRespuestaAchApiMapper(),Mock.Of<INotificarRespuestaAchUseCase>(),default);r.Should().BeOfType<BadRequestObjectResult>();}

    [Fact]
    public async Task SendNotification_ShouldReturnNotFound_WhenAttemptDoesNotExist(){var uc=new Mock<INotificarRespuestaAchUseCase>();uc.Setup(x=>x.ExecuteAsync(It.IsAny<NotificarRespuestaAchCommand>(),It.IsAny<CancellationToken>())).ReturnsAsync(new NotificarRespuestaAchResult(false,false,false,false,false,null,null,null,null,null,null));var c=new AchResponsesController();var r=await c.SendNotification(new(1,null),new NotificarRespuestaAchRequestValidator(),new NotificarRespuestaAchApiMapper(),uc.Object,default);r.Should().BeOfType<NotFoundResult>();}

    [Fact]
    public async Task SendNotification_ShouldReturnOk_WhenGatewayFunctionalErrorIsAudited(){var uc=new Mock<INotificarRespuestaAchUseCase>();uc.Setup(x=>x.ExecuteAsync(It.IsAny<NotificarRespuestaAchCommand>(),It.IsAny<CancellationToken>())).ReturnsAsync(new NotificarRespuestaAchResult(true,true,false,true,false,AchResponseNotificationStatus.ErrorFuncional,AchResponseProcessingStatus.ErrorFuncional,"E1","Err",null,null));var c=new AchResponsesController();var r=await c.SendNotification(new(1,null),new NotificarRespuestaAchRequestValidator(),new NotificarRespuestaAchApiMapper(),uc.Object,default);r.Should().BeOfType<OkObjectResult>();}

    [Fact]
    public async Task SendNotification_ShouldReturnOk_WhenTechnicalErrorIsAudited(){var uc=new Mock<INotificarRespuestaAchUseCase>();uc.Setup(x=>x.ExecuteAsync(It.IsAny<NotificarRespuestaAchCommand>(),It.IsAny<CancellationToken>())).ReturnsAsync(new NotificarRespuestaAchResult(true,true,false,false,true,AchResponseNotificationStatus.ErrorTecnico,AchResponseProcessingStatus.PendienteReintento,null,null,"t",null));var c=new AchResponsesController();var r=await c.SendNotification(new(1,null),new NotificarRespuestaAchRequestValidator(),new NotificarRespuestaAchApiMapper(),uc.Object,default);r.Should().BeOfType<OkObjectResult>();}

    [Fact]
    public async Task Search_ShouldReturnPagedResponses(){var repo=new Mock<IAchResponseRepository>();repo.Setup(x=>x.SearchAsync(It.IsAny<AchResponseSearchQuery>(),It.IsAny<CancellationToken>())).ReturnsAsync(new PagedResult<AchResponseListItemModel>([],1,10,0));var c=new AchResponsesController();var r=await c.Search(new(null,null,null,null,null,null,null,null,null,null,1,10),new AchResponseQueryApiMapper(),repo.Object,default);r.Should().BeOfType<OkObjectResult>();}

    [Fact]
    public async Task GetDetail_ShouldReturnNotFound_WhenResponseDoesNotExist(){var repo=new Mock<IAchResponseRepository>();repo.Setup(x=>x.FindDetailByIdAsync(It.IsAny<Guid>(),It.IsAny<CancellationToken>())).ReturnsAsync((AchResponseDetailModel?)null);var c=new AchResponsesController();var r=await c.GetDetail(Guid.NewGuid(),new AchResponseQueryApiMapper(),repo.Object,default);r.Should().BeOfType<NotFoundResult>();}

    [Fact]
    public async Task GetDetail_ShouldReturnOk_WhenResponseExists(){var repo=new Mock<IAchResponseRepository>();repo.Setup(x=>x.FindDetailByIdAsync(It.IsAny<Guid>(),It.IsAny<CancellationToken>())).ReturnsAsync(new AchResponseDetailModel(Guid.NewGuid(),"Transaccion","TX","ACH",null,null,"E",null,null,null,null,null,null,1,"H","Notificada",null,true,null,DateTime.UtcNow,DateTime.UtcNow,null,[]));var c=new AchResponsesController();var r=await c.GetDetail(Guid.NewGuid(),new AchResponseQueryApiMapper(),repo.Object,default);r.Should().BeOfType<OkObjectResult>();}

    [Fact]
    public async Task GetAttempts_ShouldReturnOkListWithoutPayloads(){var repo=new Mock<IAchResponseNotificationAttemptRepository>();repo.Setup(x=>x.FindPublicByResponseIdAsync(It.IsAny<Guid>(),It.IsAny<CancellationToken>())).ReturnsAsync([new AchResponseNotificationAttemptModel(1,Guid.NewGuid(),1,"Pendiente",1,"C","TX",1,null,1,null,null,null,null,null,DateTime.UtcNow,null)]);var c=new AchResponsesController();var r=await c.GetAttempts(Guid.NewGuid(),new AchResponseQueryApiMapper(),repo.Object,default);r.Should().BeOfType<OkObjectResult>();typeof(AchResponseNotificationAttemptResponse).GetProperties().Select(x=>x.Name).Should().NotContain(new[]{"RequestPayload","ResponsePayload"});}

    [Fact]
    public async Task GetMappings_ShouldReturnBadRequest_WhenTipoRespuestaInvalid(){var c=new AchResponsesController();var r=await c.GetMappings(null,"INVALID",null,new AchResponseQueryApiMapper(),Mock.Of<IAchResponseStatusMappingRepository>(),default);r.Should().BeOfType<BadRequestObjectResult>();}

    [Fact]
    public async Task GetMappings_ShouldReturnOk_WhenFiltersValid(){var repo=new Mock<IAchResponseStatusMappingRepository>();repo.Setup(x=>x.ListAsync(It.IsAny<string?>(),It.IsAny<TipoRespuestaAch?>(),It.IsAny<bool?>(),It.IsAny<CancellationToken>())).ReturnsAsync([new AchResponseStatusMappingListItemModel(1,"ACH","Transaccion","E",null,1,1,"ok",null,null,false,true,true,DateTime.UtcNow,null)]);var c=new AchResponsesController();var r=await c.GetMappings("ACH","Transaccion",true,new AchResponseQueryApiMapper(),repo.Object,default);r.Should().BeOfType<OkObjectResult>();}

    [Fact]
    public void Endpoints_ShouldNotExposeSoapOrProviderFields(){var forbidden=new[]{"Axon","Soap","Xml","Wsdl","Envelope","SOAPAction","idTransaccionAxon","IdTransaccionAxon"};var types=new[]{typeof(ProcesarRespuestaAchRequest),typeof(ProcesarRespuestaAchResponse),typeof(NotificarRespuestaAchResponse),typeof(AchResponseDetailResponse)};foreach(var t in types)t.GetProperties().Select(x=>x.Name).Should().NotContain(p=>forbidden.Any(f=>p.Contains(f,StringComparison.OrdinalIgnoreCase)));}

    [Fact]
    public void AchResponsesController_ShouldNotUseDbContextDirectly()
    {
        var controllerType = typeof(AchResponsesController);
        controllerType.GetFields(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public)
            .Select(x => x.FieldType)
            .Should().NotContain(typeof(AchDbContext));

        controllerType.GetConstructors()
            .SelectMany(x => x.GetParameters())
            .Select(x => x.ParameterType)
            .Should().NotContain(typeof(AchDbContext));

        controllerType.GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly)
            .SelectMany(x => x.GetParameters())
            .Select(x => x.ParameterType)
            .Should().NotContain(typeof(AchDbContext));
    }

    [Fact]
    public void AchResponsesController_ShouldNotReferenceSoapOrExternalTypes()
    {
        var controllerType = typeof(AchResponsesController);
        var referencedTypes = controllerType.GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly)
            .SelectMany(x => x.GetParameters())
            .Select(x => x.ParameterType.FullName ?? string.Empty)
            .ToList();

        referencedTypes.Should().NotContain(x => x.Contains("Cfa.ACHInterbank.External", StringComparison.Ordinal));
        referencedTypes.Should().NotContain(x => x.Contains("Soap", StringComparison.OrdinalIgnoreCase));
        referencedTypes.Should().NotContain(x => x.Contains("Ws", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void GetMappings_ShouldUseExpectedAbsoluteRoute()
    {
        var action = typeof(AchResponsesController).GetMethod(nameof(AchResponsesController.GetMappings));
        action.Should().NotBeNull();

        var routeAttribute = action!.GetCustomAttribute<HttpGetAttribute>();
        routeAttribute.Should().NotBeNull();
        routeAttribute!.Template.Should().Be("/api/ach/response-status-mappings");
    }
}

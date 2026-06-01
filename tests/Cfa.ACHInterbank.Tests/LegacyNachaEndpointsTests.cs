using System;
using System.Linq;
using System.Threading;
using Cfa.ACHInterbank.Api.Controllers;
using Cfa.ACHInterbank.Application.ACH.Interfaces;
using Cfa.ACHInterbank.Domain.Entities.Ach.Dtos;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Xunit;

namespace Cfa.ACHInterbank.Tests;

public class LegacyNachaEndpointsTests
{
    [Fact]
    public void LegacyLayoutsEndpoint_ShouldBeMarkedDeprecatedOrDiagnostic()
    {
#pragma warning disable CS0618
        var controllerType = typeof(NachaRecordLayoutsController);
#pragma warning restore CS0618

        Assert.NotNull(controllerType.GetCustomAttributes(typeof(ObsoleteAttribute), inherit: false).SingleOrDefault());
        Assert.Contains(controllerType.GetMethods(), method => method.Name == "GetAll");
    }

    [Fact]
    public void LegacyDefinitionsEndpoint_ShouldBeMarkedDeprecatedOrDiagnostic()
    {
#pragma warning disable CS0618
        var controllerType = typeof(NachaRecordDefinitionsController);
#pragma warning restore CS0618

        Assert.NotNull(controllerType.GetCustomAttributes(typeof(ObsoleteAttribute), inherit: false).SingleOrDefault());
        Assert.Contains(controllerType.GetMethods(), method => method.Name == "GetAll");
    }

    [Fact]
    public void LegacyMutableActions_ShouldBeBlockedOrAbsent()
    {
        var layoutsService = new Mock<INachaRecordLayoutAppService>(MockBehavior.Strict);
        var definitionsService = new Mock<INachaRecordDefinitionAppService>(MockBehavior.Strict);
#pragma warning disable CS0618
        var layouts = new NachaRecordLayoutsController(layoutsService.Object);
        var definitions = new NachaRecordDefinitionsController(definitionsService.Object);
#pragma warning restore CS0618

        var layoutResult = Assert.IsType<ObjectResult>(layouts.Create(new NachaRecordLayoutDto(), CancellationToken.None).Result);
        var definitionResult = Assert.IsType<ObjectResult>(definitions.Create(new NachaRecordDefinitionDto(), CancellationToken.None).Result);

        Assert.Equal(410, layoutResult.StatusCode);
        Assert.Equal(410, definitionResult.StatusCode);
        layoutsService.VerifyNoOtherCalls();
        definitionsService.VerifyNoOtherCalls();
    }

    [Fact]
    public void LegacyEndpoints_ShouldNotBeUsedForOfficialGeneration()
    {
        var generationMethods = typeof(NachaExportController)
            .GetConstructors()
            .SelectMany(ctor => ctor.GetParameters())
            .Select(parameter => parameter.ParameterType)
            .ToArray();

        Assert.DoesNotContain(typeof(INachaRecordLayoutAppService), generationMethods);
        Assert.DoesNotContain(typeof(INachaRecordDefinitionAppService), generationMethods);
    }
}

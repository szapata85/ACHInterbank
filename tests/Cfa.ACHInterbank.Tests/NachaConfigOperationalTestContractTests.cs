using System.Reflection;
using Cfa.ACHInterbank.Api.Controllers;
using Cfa.ACHInterbank.Application.ACH.Models;
using Cfa.ACHInterbank.Application.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Cfa.ACHInterbank.Tests;

public sealed class NachaConfigOperationalTestContractTests
{
    [Fact]
    public void TestConfiguration_UsesReadPolicyAndStableRoute()
    {
        var method = typeof(NachaConfigProfilesController).GetMethod(nameof(NachaConfigProfilesController.TestConfiguration));
        Assert.NotNull(method);

        var post = method!.GetCustomAttribute<HttpPostAttribute>();
        Assert.Equal("perfiles/{id:int}/probar", post!.Template);

        var authorize = method.GetCustomAttributes<AuthorizeAttribute>().Single();
        Assert.Equal(P1Policies.ConfigRead, authorize.Policy);

        var responses = method.GetCustomAttributes<ProducesResponseTypeAttribute>().ToArray();
        Assert.Contains(responses, response => response.StatusCode == 200 && response.Type == typeof(NachaConfigOperationalTestResultDto));
        Assert.Contains(responses, response => response.StatusCode == 404);
    }

    [Fact]
    public void OperationalTestDto_ExposesHumanizedValidationSelectionAndPreviewContract()
    {
        var names = typeof(NachaConfigOperationalTestResultDto).GetProperties()
            .Select(property => property.Name)
            .ToHashSet(StringComparer.Ordinal);

        var required = new[]
        {
            nameof(NachaConfigOperationalTestResultDto.ProfileId),
            nameof(NachaConfigOperationalTestResultDto.ProcessDateUtc),
            nameof(NachaConfigOperationalTestResultDto.CanPublish),
            nameof(NachaConfigOperationalTestResultDto.ResultLabelEs),
            nameof(NachaConfigOperationalTestResultDto.ErroresBloqueantes),
            nameof(NachaConfigOperationalTestResultDto.Advertencias),
            nameof(NachaConfigOperationalTestResultDto.Issues),
            nameof(NachaConfigOperationalTestResultDto.Selection),
            nameof(NachaConfigOperationalTestResultDto.Records)
        };
        foreach (var property in required)
        {
            Assert.Contains(property, names);
        }

        Assert.NotNull(typeof(NachaConfigRecordPreviewDto).GetProperty(nameof(NachaConfigRecordPreviewDto.RenderedRecord)));
        Assert.NotNull(typeof(NachaConfigFieldPreviewDto).GetProperty(nameof(NachaConfigFieldPreviewDto.SourceSummaryEs)));
        Assert.NotNull(typeof(NachaConfigFieldPreviewDto).GetProperty(nameof(NachaConfigFieldPreviewDto.SampleValue)));
    }
}

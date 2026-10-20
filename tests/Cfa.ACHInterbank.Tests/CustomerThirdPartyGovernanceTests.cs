using System.Reflection;
using Cfa.ACHInterbank.Api.Controllers;
using Cfa.ACHInterbank.Domain.Entities.Transactions.Enums;
using Cfa.ACHInterbank.Domain.Models.ACH;
using Microsoft.AspNetCore.Mvc;
using Xunit;

namespace Cfa.ACHInterbank.Tests;

public sealed class CustomerThirdPartyGovernanceTests
{
    [Fact]
    public void Controller_DoesNotExposeManualStatusWriteEndpoint()
    {
        var writeActions = typeof(CustomerThirdPartiesController)
            .GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly)
            .Where(method =>
                method.GetCustomAttribute<HttpPostAttribute>() is not null
                || method.GetCustomAttribute<HttpPutAttribute>() is not null
                || method.GetCustomAttribute<HttpPatchAttribute>() is not null
                || method.GetCustomAttribute<HttpDeleteAttribute>() is not null)
            .ToList();

        Assert.Empty(writeActions);
    }

    [Fact]
    public void Domain_RejectsManualPendingResult()
    {
        var thirdParty = PendingThirdParty();

        Assert.Throws<ArgumentOutOfRangeException>(() =>
            thirdParty.ApplyAutomaticNachaResult(
                CustomerThirdPartyStatusEnum.Pending,
                91,
                "CYCLE-1",
                DateTime.UtcNow,
                "Intento manual",
                "automatic-evidence"));
    }

    [Fact]
    public void Domain_RequiresAutomaticEvidence()
    {
        var thirdParty = PendingThirdParty();

        Assert.Throws<ArgumentException>(() =>
            thirdParty.ApplyAutomaticNachaResult(
                CustomerThirdPartyStatusEnum.Active,
                91,
                "CYCLE-1",
                DateTime.UtcNow,
                "Respuesta válida",
                string.Empty));
    }

    [Fact]
    public void Domain_DoesNotResolveFinalPrenotificationTwice()
    {
        var thirdParty = PendingThirdParty();
        var first = thirdParty.ApplyAutomaticNachaResult(
            CustomerThirdPartyStatusEnum.Active,
            91,
            "CYCLE-1",
            DateTime.UtcNow,
            "Aceptación tácita automática",
            "state-event:1");
        var second = thirdParty.ApplyAutomaticNachaResult(
            CustomerThirdPartyStatusEnum.Rejected,
            91,
            "CYCLE-2",
            DateTime.UtcNow.AddMinutes(1),
            "Respuesta duplicada",
            "state-event:2");

        Assert.True(first);
        Assert.False(second);
        Assert.Equal(CustomerThirdPartyStatusEnum.Active, thirdParty.Status);
        Assert.Equal("CYCLE-1", thirdParty.ValidationCycleId);
    }

    [Fact]
    public void Domain_RejectsResponseForAnotherPrenotification()
    {
        var thirdParty = PendingThirdParty();

        Assert.Throws<InvalidOperationException>(() =>
            thirdParty.ApplyAutomaticNachaResult(
                CustomerThirdPartyStatusEnum.Rejected,
                92,
                "CYCLE-1",
                DateTime.UtcNow,
                "Rechazo automático",
                "response:1"));
    }

    private static CustomerThirdParty PendingThirdParty()
        => new()
        {
            CustomerId = 1,
            DestinationInstitutionId = 2,
            DestinationAccountNumber = "123456789",
            RecipientIdNumber = "REC-01",
            PrenotificationTransactionId = 91
        };
}

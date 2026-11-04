using System.Reflection;
using Cfa.ACHInterbank.Api.Controllers;
using Cfa.ACHInterbank.Application.ACH.Models;
using Cfa.ACHInterbank.Application.ACH.Services;
using Cfa.ACHInterbank.Application.Security;
using Cfa.ACHInterbank.Domain.Models.ACH.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Xunit;

namespace Cfa.ACHInterbank.Tests;

public sealed class PrenotificationSimulatorI03ContractTests
{
    private static PrenotificationSimulationContractRequest Valid() => new()
    {
        Outcome = PrenotificationSimulationOutcome.Returned,
        ClearingHouseCode = "CENIT",
        OriginalPrenotificationIds = [11, 12],
        OriginalCycleId = "CENIT-C1",
        ResponseCycleId = "CENIT-C2",
        BusinessDate = new DateOnly(2026, 11, 2),
        RespondingFinancialInstitutionId = 55,
        ReasonCode = "R02",
        IdempotencyKey = "sample-request-01"
    };

    private static PrenotificationSimulationContractRequest AnotherKey()
    {
        var req = Valid();
        req.IdempotencyKey = "sample-request-02";
        return req;
    }

    [Fact]
    public void SameScopedKeySameGuid_AndDifferentScopeDifferentGuid()
    {
        var req = Valid();
        var id = PrenotificationSimulationIdentity.ReservationId(req);
        Assert.Equal(id, PrenotificationSimulationIdentity.ReservationId(req));
        Assert.NotEqual(id, PrenotificationSimulationIdentity.ReservationId(AnotherKey()));
        req.ClearingHouseCode = "ACHCOL";
        Assert.NotEqual(id, PrenotificationSimulationIdentity.ReservationId(req));
    }

    [Fact]
    public void ReorderedIdsHaveSameFingerprint_ButDifferentPayloadIsRejected()
    {
        var req = Valid();
        var hash = PrenotificationSimulationIdentity.RequestHash(req);
        req.OriginalPrenotificationIds = [12, 11];
        Assert.Equal(hash, PrenotificationSimulationIdentity.RequestHash(req));
        Assert.True(PrenotificationSimulationIdentity.MatchesStoredPayload(PrenotificationSimulationIdentity.AuditMarker(hash), hash));
        req.ReasonCode = "R03";
        Assert.NotEqual(hash, PrenotificationSimulationIdentity.RequestHash(req));
        Assert.False(PrenotificationSimulationIdentity.MatchesStoredPayload(
            PrenotificationSimulationIdentity.AuditMarker(hash), PrenotificationSimulationIdentity.RequestHash(req)));
    }

    [Fact]
    public void ExplicitReturnRouteRequiresDifferentialPermission()
    {
        var method = typeof(NachaInboundSimulatorController).GetMethod(nameof(NachaInboundSimulatorController.GeneratePrenotificationReturn));
        Assert.NotNull(method);
        Assert.Equal("prenotifications/generate-return", method!.GetCustomAttribute<HttpPostAttribute>()?.Template);
        Assert.Contains(method.GetCustomAttributes<AuthorizeAttribute>(), x => x.Policy == P1Policies.NachaSimulatorGenerateDifferential);
        Assert.False(PrenotificationSimulationContract.Validate(Valid()).ExecutionEnabled); // forma != ejecucion
    }
}

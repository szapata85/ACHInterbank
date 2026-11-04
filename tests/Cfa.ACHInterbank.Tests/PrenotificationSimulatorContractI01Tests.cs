using System.Text.Json;
using Cfa.ACHInterbank.Application.ACH.Models;
using Cfa.ACHInterbank.Application.ACH.Services;
using Cfa.ACHInterbank.Domain.Models.ACH.Enums;
using Xunit;

namespace Cfa.ACHInterbank.Tests;

public sealed class PrenotificationSimulatorContractI01Tests
{
    private static PrenotificationSimulationContractRequest Valid(PrenotificationSimulationOutcome outcome)
        => new()
        {
            Outcome = outcome,
            ClearingHouseCode = "ACHCOL",
            OriginalPrenotificationIds = [101, 102],
            OriginalCycleId = "ACH-ORIGINAL-001",
            ResponseCycleId = outcome == PrenotificationSimulationOutcome.Returned ? "ACH-RETURN-002" : null,
            BusinessDate = new DateOnly(2026, 11, 2),
            RespondingFinancialInstitutionId = 7,
            ReasonCode = outcome == PrenotificationSimulationOutcome.Returned ? "R04" : null,
            IdempotencyKey = outcome == PrenotificationSimulationOutcome.Returned ? "test-i01-unique-001" : null
        };

    [Theory]
    [InlineData("ACHCOL", PrenotificationSimulationOutcome.Returned)]
    [InlineData("CENIT", PrenotificationSimulationOutcome.Returned)]
    [InlineData("ACHCOL", PrenotificationSimulationOutcome.NoReturnByDeadline)]
    [InlineData("CENIT", PrenotificationSimulationOutcome.NoReturnByDeadline)]
    public void WellFormedContracts_AreNotExecutableYet(string clearingHouseCode, PrenotificationSimulationOutcome outcome)
    {
        var request = Valid(outcome);
        request.ClearingHouseCode = clearingHouseCode;
        var response = PrenotificationSimulationContract.Validate(request);

        Assert.True(response.ShapeValid);
        Assert.False(response.ExecutionEnabled);
        Assert.Empty(response.Violations);
    }

    [Fact]
    public void NewScenarioDoesNotOverwriteLegacyEnumOrReturnModes()
    {
        Assert.Equal(3, (int)NachaInboundSimulationType.IncomingPrenotificationResponse);
        Assert.Equal(10, (int)NachaInboundSimulationType.IncomingPrenotificationReturn);
        Assert.Equal(4, (int)InboundResponseMode.Returned);
        Assert.Equal("\"IncomingPrenotificationResponse\"", JsonSerializer.Serialize(
            NachaInboundSimulationType.IncomingPrenotificationResponse,
            new JsonSerializerOptions { Converters = { new System.Text.Json.Serialization.JsonStringEnumConverter() } }));
    }

    [Fact]
    public void SerializedContractUsesStringOutcome_AndKeepsIdentifiers()
    {
        var request = Valid(PrenotificationSimulationOutcome.Returned);
        var json = JsonSerializer.Serialize(request);
        Assert.Contains("\"Outcome\":\"Returned\"", json, StringComparison.Ordinal);
        var parsed = JsonSerializer.Deserialize<PrenotificationSimulationContractRequest>(json);
        Assert.NotNull(parsed);
        Assert.Equal(request.OriginalPrenotificationIds, parsed.OriginalPrenotificationIds);
        Assert.Equal(PrenotificationSimulationOutcome.Returned, parsed.Outcome);
    }

    [Fact]
    public void CapabilitiesNeverAdvertiseExecutableOutcome()
    {
        var capabilities = PrenotificationSimulationContract.Describe();
        Assert.Equal("IncomingPrenotificationReturn", capabilities.FileReturnScenario);
        Assert.Equal("IncomingPrenotificationResponse", capabilities.LegacyScenario);
        Assert.Equal(2, capabilities.Outcomes.Count);
        Assert.All(capabilities.Outcomes, mode => Assert.False(mode.ExecutionEnabled));
        Assert.True(Assert.Single(capabilities.Outcomes, x => x.Outcome == PrenotificationSimulationOutcome.Returned).CreatesNachaFile);
        Assert.False(Assert.Single(capabilities.Outcomes, x => x.Outcome == PrenotificationSimulationOutcome.NoReturnByDeadline).CreatesNachaFile);
        Assert.False(capabilities.AutoImported);
        Assert.False(capabilities.ExternalTransmission);
    }

    [Fact]
    public void ReturnedRequiresResponseCycleCauseAndIdempotencyKey()
    {
        var request = Valid(PrenotificationSimulationOutcome.Returned);
        request.ResponseCycleId = null;
        request.ReasonCode = null;
        request.IdempotencyKey = null;
        var result = PrenotificationSimulationContract.Validate(request);
        Assert.False(result.ShapeValid);
        Assert.Contains(result.Violations, x => x.Code == "RESPONSE_CYCLE_REQUIRED");
        Assert.Contains(result.Violations, x => x.Code == "RETURN_REASON_REQUIRED");
        Assert.Contains(result.Violations, x => x.Code == "IDEMPOTENCY_KEY_REQUIRED");
    }

    [Fact]
    public void NoReturnRejectsFictitiousCauseAndResponseCycle()
    {
        var request = Valid(PrenotificationSimulationOutcome.NoReturnByDeadline);
        request.ReasonCode = "R04";
        request.ResponseCycleId = "FAKE-RESPONSE";
        var result = PrenotificationSimulationContract.Validate(request);
        Assert.False(result.ShapeValid);
        Assert.Contains(result.Violations, x => x.Code == "NO_RETURN_HAS_NO_REASON");
        Assert.Contains(result.Violations, x => x.Code == "NO_RETURN_HAS_NO_RESPONSE_CYCLE");
    }

    [Fact]
    public void ContractRejectsInvalidChamberAndDuplicateOrInvalidIdentifiers()
    {
        var request = Valid(PrenotificationSimulationOutcome.Returned);
        request.ClearingHouseCode = "UNKNOWN";
        request.OriginalPrenotificationIds = [101, 101, 102];
        request.OriginalCycleId = "";
        request.BusinessDate = default;
        request.RespondingFinancialInstitutionId = 0;
        var result = PrenotificationSimulationContract.Validate(request);
        Assert.False(result.ShapeValid);
        Assert.Contains(result.Violations, x => x.Code == "CLEARING_HOUSE_UNSUPPORTED");
        Assert.Contains(result.Violations, x => x.Code == "PRENOTIFICATION_IDS_DUPLICATED");
        Assert.Contains(result.Violations, x => x.Code == "ORIGINAL_CYCLE_REQUIRED");
        Assert.Contains(result.Violations, x => x.Code == "BUSINESS_DATE_REQUIRED");
        Assert.Contains(result.Violations, x => x.Code == "RESPONDING_FINANCIAL_INSTITUTION_REQUIRED");
    }

    [Fact]
    public void UnknownOutcomeFailsClosed_AndCapabilitiesUseTextCodes()
    {
        var request = Valid(PrenotificationSimulationOutcome.Returned);
        request.Outcome = (PrenotificationSimulationOutcome)90;
        var result = PrenotificationSimulationContract.Validate(request);
        Assert.Contains(result.Violations, x => x.Code == "PRENOTIFICATION_OUTCOME_INVALID");
        var json = JsonSerializer.Serialize(PrenotificationSimulationContract.Describe());
        Assert.Contains("\"Outcome\":\"NoReturnByDeadline\"", json, StringComparison.Ordinal);
    }
}

using System.Text.Json;
using Cfa.ACHInterbank.Domain.Models.ACH;
using Cfa.ACHInterbank.Persistence.ACH.Services.Implementation;
using Xunit;

namespace Cfa.ACHInterbank.Tests;

/// <summary>F4.1 policy tests are local/unit-only: no DB, SOAP, or money movement.</summary>
public sealed class IncomingNachaFileBasedDispatchEligibilityF41Tests
{
    private readonly IncomingNachaDispatchEligibilityPolicy _sut = new();

    [Theory]
    [InlineData("22", IncomingNachaFunctionalClass.CreditoEntrante, 8, 8, "H", false)]
    [InlineData("32", IncomingNachaFunctionalClass.CreditoEntrante, 9, 9, "I", true)]
    [InlineData("52", IncomingNachaFunctionalClass.CreditoEntrante, 8, 10, "H", true)]
    [InlineData("27", IncomingNachaFunctionalClass.DebitoEntrante, 8, 8, "H", false)]
    [InlineData("37", IncomingNachaFunctionalClass.DebitoEntrante, 9, 9, "I", true)]
    [InlineData("55", IncomingNachaFunctionalClass.DebitoEntrante, 8, 10, "H", false)]
    public async Task MonetaryCodes_MatchFunctionalDirection_AreEligibleButNotDispatched(
        string code, IncomingNachaFunctionalClass functionalClass, int sequence, int cycle,
        string modifier, bool encrypted)
    {
        var (ingestion, classification, header, entry, clearingHouse) = Seed(sequence, cycle, modifier, encrypted);
        entry.TransactionCode = code;
        classification.FunctionalClass = functionalClass;
        var result = await _sut.EvaluateFileBasedAsync(ingestion, classification, header, entry, clearingHouse, true);

        Assert.True(result.IsEligible);
        Assert.False(result.IsBlocked);
        Assert.False(result.IsWaitingWindow);
        Assert.Contains("NOT_AUTHORIZED_FOR_SOAP", result.Reason);
        using var evidence = JsonDocument.Parse(result.EvidenceJson);
        Assert.False(evidence.RootElement.GetProperty("transportAuthorized").GetBoolean());
        Assert.Equal(cycle, evidence.RootElement.GetProperty("ExternalCycleNumber").GetInt32());
        Assert.DoesNotContain("ACCOUNT-1", result.EvidenceJson);
    }

    [Theory]
    [InlineData("23", IncomingNachaFunctionalClass.CreditoEntrante)]
    [InlineData("28", IncomingNachaFunctionalClass.DebitoEntrante)]
    [InlineData("33", IncomingNachaFunctionalClass.CreditoEntrante)]
    [InlineData("38", IncomingNachaFunctionalClass.DebitoEntrante)]
    [InlineData("53", IncomingNachaFunctionalClass.CreditoEntrante)]
    [InlineData("57", IncomingNachaFunctionalClass.DebitoEntrante)]
    [InlineData("27", IncomingNachaFunctionalClass.CreditoEntrante)]
    [InlineData("22", IncomingNachaFunctionalClass.DebitoEntrante)]
    [InlineData("99", IncomingNachaFunctionalClass.CreditoEntrante)]
    public async Task PrenoteUnknownOrDirectionMismatch_MustFailClosed(string code, IncomingNachaFunctionalClass classValue)
    {
        var f = Seed(); f.entry.TransactionCode = code; f.classification.FunctionalClass = classValue;
        Assert.False((await Evaluate(f)).IsEligible);
    }

    [Theory]
    [InlineData(IncomingNachaFunctionalClass.Prenotificacion)]
    [InlineData(IncomingNachaFunctionalClass.Devolucion)]
    [InlineData(IncomingNachaFunctionalClass.DevolucionDevolucion)]
    [InlineData(IncomingNachaFunctionalClass.RechazadaOperador)]
    [InlineData(IncomingNachaFunctionalClass.RetornoEpr)]
    [InlineData(IncomingNachaFunctionalClass.NoProcesable)]
    [InlineData(IncomingNachaFunctionalClass.Ambigua)]
    [InlineData(IncomingNachaFunctionalClass.Inconsistente)]
    public async Task NonMonetaryFunctionalClass_MustFailClosed(IncomingNachaFunctionalClass classValue)
    {
        var f = Seed(); f.classification.FunctionalClass = classValue;
        Assert.False((await Evaluate(f)).IsEligible);
    }

    [Fact]
    public async Task FeatureFlagDisabled_MustNotAuthorizeEligibility()
    {
        var f = Seed();
        Assert.Equal("ACHCOL_FLAG_DISABLED", (await _sut.EvaluateFileBasedAsync(
            f.ingestion, f.classification, f.header, f.entry, f.clearingHouse, false)).Reason);
    }

    [Fact]
    public async Task InvalidChamberOrInactiveChamber_MustFailClosed()
    {
        var f = Seed(); f.clearingHouse.Code = "CENIT";
        Assert.False((await Evaluate(f)).IsEligible);
        f.clearingHouse.Code = "ACHCOL"; f.clearingHouse.IsActive = false;
        Assert.False((await Evaluate(f)).IsEligible);
        f.clearingHouse.IsActive = true; f.clearingHouse.ClearingHouseId = 777;
        Assert.False((await Evaluate(f)).IsEligible);
    }

    [Fact]
    public async Task BlockedIngestionOrAmbiguousCycle_MustFailClosed()
    {
        var f = Seed(); f.ingestion.IngestionStatus = IncomingNachaIngestionStatus.Bloqueado;
        Assert.False((await Evaluate(f)).IsEligible);
        f.ingestion.IngestionStatus = IncomingNachaIngestionStatus.Completado;
        f.ingestion.CycleResolutionStatus = IncomingNachaCycleResolutionStatus.Ambiguo;
        Assert.False((await Evaluate(f)).IsEligible);
        f.ingestion.CycleResolutionStatus = IncomingNachaCycleResolutionStatus.ResueltoSinCiclo;
        f.ingestion.ResolvedAchCycleId = "CYCLE-FAKE";
        Assert.False((await Evaluate(f)).IsEligible);
    }

    [Fact]
    public async Task FilenameCycleSequenceOrEnvelopeMismatch_MustFailClosed()
    {
        var f = Seed(8, 10, "H", encrypted: true);
        f.ingestion.ExternalCycleNumber = 8;
        Assert.False((await Evaluate(f)).IsEligible);
        f.ingestion.ExternalCycleNumber = 10;
        f.ingestion.FileIdModifier = "I";
        Assert.False((await Evaluate(f)).IsEligible);
        f.ingestion.FileIdModifier = "H";
        f.ingestion.FileProtectionKind = "PlainNacha";
        Assert.False((await Evaluate(f)).IsEligible);
        f.ingestion.FileProtectionKind = "DigitalEnvelope";
        f.ingestion.CanonicalPlainFileName = "0001283.008.20260731.9.OUT";
        Assert.False((await Evaluate(f)).IsEligible);
    }

    [Fact]
    public async Task UnmatchedHeaderOrEntry_MustFailClosed()
    {
        var f = Seed(); f.header.FileIdModifier = "I";
        Assert.False((await Evaluate(f)).IsEligible);
        f.header.FileIdModifier = "H"; f.classification.EntryDetailId = 999;
        Assert.False((await Evaluate(f)).IsEligible);
        f.classification.EntryDetailId = f.entry.EntryDetailID;
        f.header.IncomingNachaFileIngestionId = Guid.NewGuid();
        Assert.False((await Evaluate(f)).IsEligible);
    }

    [Fact]
    public async Task ReturnOrManualOrPrenoteStatus_MustFailClosed()
    {
        var f = Seed(); f.classification.ReturnReasonCode = "R01";
        Assert.False((await Evaluate(f)).IsEligible);
        f.classification.ReturnReasonCode = null; f.classification.OriginalTraceRef = "987654321012345";
        Assert.False((await Evaluate(f)).IsEligible);
        f.classification.OriginalTraceRef = null; f.classification.RequiresManualResolution = true;
        Assert.False((await Evaluate(f)).IsEligible);
        f.classification.RequiresManualResolution = false;
        f.classification.PrenoteStatus = IncomingNachaPrenoteStatus.ActivaTercero;
        Assert.False((await Evaluate(f)).IsEligible);
    }

    [Fact]
    public async Task MissingMonetaryIdentity_MustFailClosed()
    {
        var f = Seed(); f.entry.SequenceNumber = "123";
        Assert.False((await Evaluate(f)).IsEligible);
        f.entry.SequenceNumber = "123456789012345";
        f.entry.Amount = 0;
        Assert.False((await Evaluate(f)).IsEligible);
    }

    private Task<Cfa.ACHInterbank.Application.ACH.Models.IncomingNachaDispatchEligibilityResult> Evaluate(
        (IncomingNachaFileIngestion ingestion, IncomingNachaEntryClassification classification, NachaHeader header, EntryDetail entry, ClearingHouse clearingHouse) f)
        => _sut.EvaluateFileBasedAsync(f.ingestion, f.classification, f.header, f.entry, f.clearingHouse, true);

    private static (IncomingNachaFileIngestion ingestion, IncomingNachaEntryClassification classification, NachaHeader header, EntryDetail entry, ClearingHouse clearingHouse)
        Seed(int sequence = 8, int externalCycle = 8, string modifier = "H", bool encrypted = false)
    {
        var id = Guid.NewGuid();
        var physical = $"0001283.{sequence:000}.20260731.{externalCycle}.OUT";
        var ingestion = new IncomingNachaFileIngestion
        {
            Id = id, FileName = physical + (encrypted ? ".env" : ""),
            CanonicalPlainFileName = physical,
            FileIdModifier = modifier, FileSequence = sequence, ExternalCycleNumber = externalCycle,
            FileProtectionKind = encrypted ? "DigitalEnvelope" : "PlainNacha",
            FileNameDate = new DateTime(2026, 7, 31, 0, 0, 0, DateTimeKind.Utc),
            OperationalDate = new DateTime(2026, 7, 31, 0, 0, 0, DateTimeKind.Utc),
            ResolvedClearingHouseId = 1,
            ResolutionMode = IncomingNachaDispatchSourceKinds.AchColFileBased,
            CycleResolutionStatus = IncomingNachaCycleResolutionStatus.ResueltoSinCiclo,
            IngestionStatus = IncomingNachaIngestionStatus.Completado
        };
        var header = new NachaHeader
        {
            NachaID = "NACHA-123", IncomingNachaFileIngestionId = id,
            FileIdModifier = modifier, ImmediateOrigin = "0001283"
        };
        var entry = new EntryDetail
        {
            EntryDetailID = 12, NachaID = header.NachaID, BatchNumber = 1,
            TransactionCode = "22", Amount = 100m,
            ReceivingParticipantEntityCode = "22222222", AccountNumber = "ACCOUNT-1",
            SequenceNumber = "123456789012345", RecipIdNumber = "900123456"
        };
        var classification = new IncomingNachaEntryClassification
        {
            Id = Guid.NewGuid(), IncomingNachaFileIngestionId = id,
            EntryDetailId = entry.EntryDetailID, FunctionalClass = IncomingNachaFunctionalClass.CreditoEntrante,
            EligibilityStatus = IncomingNachaEligibilityStatus.Elegible
        };
        var clearingHouse = new ClearingHouse
        {
            Id = 1, ClearingHouseId = 1, Code = "ACHCOL", Name = "ACH Colombia", OriginCode = "0001283", IsActive = true
        };
        return (ingestion, classification, header, entry, clearingHouse);
    }
}

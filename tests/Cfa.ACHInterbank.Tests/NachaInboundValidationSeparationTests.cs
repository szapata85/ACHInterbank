using Cfa.ACHInterbank.Application.ACH.Models;
using Cfa.ACHInterbank.Persistence.ACH.Services.Implementation;
using FluentAssertions;
using Xunit;
using BatchHeader = Cfa.ACHInterbank.Application.ACH.Models.ParsedBatchHeader;
using EntryDetail = Cfa.ACHInterbank.Application.ACH.Models.ParsedEntryDetail;
using AddendaRecord = Cfa.ACHInterbank.Application.ACH.Models.ParsedAddendaRecord;
using NachaHeader = Cfa.ACHInterbank.Application.ACH.Models.ParsedNachaHeader;

namespace Cfa.ACHInterbank.Tests;

public class NachaInboundValidationSeparationTests
{
    [Fact]
    public void SequenceValidation_ShouldPreserveInFileDuplicateFailureSemantics()
    {
        var sut = new NachaInboundValidator(new Dictionary<string, Cfa.ACHInterbank.Domain.Models.ACH.AchFileRejectionCode>());
        var batch = new BatchHeader { BatchNumber = 1, OriginParticipantEntityCode = "12345678" };
        var header = new NachaHeader { FileCreationDate = "260815" };
        var entry = new EntryDetail { SequenceNumber = "123456780000001" };
        var last = new Dictionary<int, int>();
        var seen = new HashSet<string>(StringComparer.Ordinal);

        sut.ValidateEntrySequenceStructure(entry, batch, header, last, seen, enforceBatchSequenceOrder: true)
            .Should().Be(new DateTime(2026, 8, 15));

        var action = () => sut.ValidateEntrySequenceStructure(entry, batch, header, last, seen, enforceBatchSequenceOrder: true);

        action.Should().Throw<InvalidOperationException>()
            .WithMessage("*[Regulatory]*D04*Número de secuencia duplicado en el archivo.*");
    }

    [Fact]
    public void CenitAddendaValidation_ShouldPreserveOwnerAssociationFailure()
    {
        var sut = new NachaInboundValidator(new Dictionary<string, Cfa.ACHInterbank.Domain.Models.ACH.AchFileRejectionCode>());
        var entry = new EntryDetail { SequenceNumber = "123456780000001" };
        var addenda = new AddendaRecord
        {
            CodeTypeAddendumRecord = "05",
            AddendumSequence = "0001",
            EntryDetailSequenceNumber = "0000002"
        };

        var action = () => NachaInboundCenitValidationRules.ValidateAddenda(entry, addenda, parsedCount: 1, isCtx: false);

        action.Should().Throw<InvalidOperationException>()
            .WithMessage("*CENIT-2026-CTX-IN-T7-ASSOCIATION*");
    }

    [Fact]
    public void CenitBatchValidation_ShouldKeepPpdCcdSeparateFromCtx()
    {
        var sut = new NachaInboundValidator(new Dictionary<string, Cfa.ACHInterbank.Domain.Models.ACH.AchFileRejectionCode>());
        var batch = new BatchHeader
        {
            StandardEntryClassCode = "CTX",
            ServiceClassCode = "200"
        };

        var action = () => NachaInboundCenitValidationRules.ValidateBatchService(
            CenitOrdinaryInbound2026Layout.CardinalityOriginalProfileCode,
            batch);

        action.Should().Throw<InvalidOperationException>()
            .WithMessage("*CENIT_INBOUND_PROFILE_SERVICE_MISMATCH*");
    }

    [Fact]
    public void ReturnValidation_ShouldPreserveType99ReasonAndOriginalTraceRequirements()
    {
        var sut = new NachaInboundValidator(new Dictionary<string, Cfa.ACHInterbank.Domain.Models.ACH.AchFileRejectionCode>());
        var entry = new EntryDetail
        {
            TransactionCode = "21",
            SequenceNumber = "123456780000001"
        };
        var addenda = new AddendaRecord
        {
            CodeTypeAddendumRecord = "99",
            ReturnReasonCode = "R01",
            OriginalTraceNumber = "876543210000001",
            EntryDetailSequenceNumber = "0000001"
        };
        var failures = new List<NachaValidationFailure>();

        var valid = sut.EnforceAddendaRequirements([entry], [addenda], failures, new HashSet<string>(StringComparer.Ordinal));

        valid.Should().ContainSingle().Which.Should().BeSameAs(entry);
        failures.Should().BeEmpty();
    }
    [Theory]
    [InlineData("D01")]
    [InlineData("D02")]
    [InlineData("D04")]
    [InlineData("D05")]
    [InlineData("D06")]
    public void CenitNachaFatal_ShouldNotUseStaCodesOrStaDescriptions(string legacyStaCode)
    {
        var catalog = new Dictionary<string, Cfa.ACHInterbank.Domain.Models.ACH.AchFileRejectionCode>
        {
            [legacyStaCode] = new() { Code = legacyStaCode, Description = "Descripción exclusiva STA", IsActive = true }
        };
        var sut = new NachaInboundValidator(catalog);
        sut.SelectNachaErrorDomain(true);

        var action = () => sut.ThrowRegulatory(legacyStaCode, "Control físico NACHA no válido.");

        action.Should().Throw<InvalidOperationException>()
            .WithMessage("*[Regulatory]*CENIT-NACHA-FATAL: Control físico NACHA no válido.*")
            .Which.Message.Should().NotContain(legacyStaCode).And.NotContain("STA");
    }

    [Fact]
    public void CenitDuplicateEntryRule_ShouldNotReportStaDuplicateFileCode()
    {
        var catalog = new Dictionary<string, Cfa.ACHInterbank.Domain.Models.ACH.AchFileRejectionCode>
        {
            ["D04"] = new() { Code = "D04", Description = "Archivo duplicado STA", IsActive = true }
        };
        var sut = new NachaInboundValidator(catalog);
        sut.SelectNachaErrorDomain(true);
        var batch = new BatchHeader { BatchNumber = 1, OriginParticipantEntityCode = "12345678" };
        var header = new NachaHeader { FileCreationDate = "260815" };
        var entry = new EntryDetail { SequenceNumber = "123456780000001" };
        var last = new Dictionary<int, int>();
        var seen = new HashSet<string>(StringComparer.Ordinal);
        sut.ValidateEntrySequenceStructure(entry, batch, header, last, seen, true);

        var action = () => sut.ValidateEntrySequenceStructure(entry, batch, header, last, seen, true);

        action.Should().Throw<InvalidOperationException>()
            .WithMessage("*[Regulatory]*CENIT-NACHA-FATAL: Número de secuencia duplicado en el archivo.*")
            .Which.Message.Should().NotContain("D04").And.NotContain("STA");
    }

    [Fact]
    public void AchColombiaNacha_ShouldRetainLegacyCodeLookup()
    {
        var catalog = new Dictionary<string, Cfa.ACHInterbank.Domain.Models.ACH.AchFileRejectionCode>
        {
            ["D04"] = new() { Code = "D04", Description = "Conteo inválido ACH", IsActive = true }
        };
        var sut = new NachaInboundValidator(catalog);
        sut.SelectNachaErrorDomain(false);

        var action = () => sut.ThrowRegulatory("D04", "Conteo divergente.");

        action.Should().Throw<InvalidOperationException>()
            .WithMessage("*[Regulatory]*D04: Conteo inválido ACH. Conteo divergente.*");
    }

    [Theory]
    [InlineData(CenitOrdinaryInbound2026Layout.OriginalProfileCode)]
    [InlineData(CenitOrdinaryInbound2026Layout.CtxOriginalProfileCode)]
    [InlineData(CenitReturnIn2026Layout.ProfileCode)]
    [InlineData(CenitReturnOfReturn2026Layout.InProfileCode)]
    public void KnownCenitInboundProfiles_ShouldSelectSeparatedPhysicalDomain(string profileCode)
    {
        NachaInboundValidator.IsCenitNachaProfileCode(profileCode).Should().BeTrue();
    }

    [Fact]
    public void UnknownAndAchProfiles_ShouldNotBeClassifiedAsCenit()
    {
        NachaInboundValidator.IsCenitNachaProfileCode(null).Should().BeFalse();
        NachaInboundValidator.IsCenitNachaProfileCode("ACH_COLOMBIA").Should().BeFalse();
    }

    [Fact]
    public void CenitTransactionFormalFailure_ShouldRemainEntryScopedAndWithoutStaCodes()
    {
        var sut = new NachaInboundValidator(new Dictionary<string, Cfa.ACHInterbank.Domain.Models.ACH.AchFileRejectionCode>());
        sut.SelectNachaErrorDomain(true);
        var entry = new EntryDetail { TransactionCode = "21", SequenceNumber = "123456780000001" };
        var failures = new List<NachaValidationFailure>();

        var valid = sut.EnforceAddendaRequirements([entry], [], failures, new HashSet<string>());

        valid.Should().BeEmpty();
        failures.Should().ContainSingle();
        failures[0].Reason.Should().NotContain("D01").And.NotContain("D02")
            .And.NotContain("D03").And.NotContain("D04");
    }
}

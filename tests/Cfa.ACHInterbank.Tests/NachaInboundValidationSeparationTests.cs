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
}

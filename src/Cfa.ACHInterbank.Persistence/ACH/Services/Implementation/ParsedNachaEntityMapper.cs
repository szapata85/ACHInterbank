using Cfa.ACHInterbank.Application.ACH.Models;
using Cfa.ACHInterbank.Domain.Models.ACH;

namespace Cfa.ACHInterbank.Persistence.ACH.Services.Implementation;

internal static class ParsedNachaEntityMapper
{
    public static IReadOnlyList<NachaHeader> Map(IReadOnlyList<ParsedNachaHeader> source)
        => source.Select(MapHeader).ToList();

    private static NachaHeader MapHeader(ParsedNachaHeader source)
    {
        var header = new NachaHeader
        {
            NachaID = source.NachaID,
            PriorityCode = source.PriorityCode,
            ImmediateDestination = source.ImmediateDestination,
            ImmediateOrigin = source.ImmediateOrigin,
            FileCreationDate = source.FileCreationDate,
            FileCreationTime = source.FileCreationTime,
            FileIdModifier = source.FileIdModifier,
            RecordSize = source.RecordSize,
            BlockingFactor = source.BlockingFactor,
            FormatCode = source.FormatCode,
            ImmediateDestinationName = source.ImmediateDestinationName,
            ImmediateOriginName = source.ImmediateOriginName,
            ReferenceCode = source.ReferenceCode,
            ClearingHouseId = source.ClearingHouseId,
            CycleNumber = source.CycleNumber,
            AchCycleId = source.AchCycleId,
            IncomingNachaFileIngestionId = source.IncomingNachaFileIngestionId
        };

        var batches = source.Batches.Select(x => new BatchHeader
        {
            ServiceClassCode = x.ServiceClassCode,
            CompanyName = x.CompanyName,
            DiscretionaryData = x.DiscretionaryData,
            CompanyId = x.CompanyId,
            StandardEntryClassCode = x.StandardEntryClassCode,
            CompanyEntryDescription = x.CompanyEntryDescription,
            DescriptiveDate = x.DescriptiveDate,
            EffectiveEntryDate = x.EffectiveEntryDate,
            CompensationDate = x.CompensationDate,
            OriginUserStatusCode = x.OriginUserStatusCode,
            OriginParticipantEntityCode = x.OriginParticipantEntityCode,
            BatchNumber = x.BatchNumber,
            NachaID = header.NachaID,
            NachaHeader = header
        }).ToList();

        var batchByNumber = batches.ToDictionary(x => x.BatchNumber);
        var entries = source.EntryDetails.Select(x => new EntryDetail
        {
            TransactionCode = x.TransactionCode,
            ReceivingParticipantEntityCode = x.ReceivingParticipantEntityCode,
            CheckDigit = x.CheckDigit,
            AccountNumber = x.AccountNumber,
            Amount = x.Amount,
            RecipIdNumber = x.RecipIdNumber,
            RecipUserName = x.RecipUserName,
            DiscreData = x.DiscreData,
            AddendumIndicator = x.AddendumIndicator,
            SequenceNumber = x.SequenceNumber,
            BatchNumber = x.BatchNumber,
            NachaID = header.NachaID,
            NachaHeader = header,
            BatchHeader = batchByNumber.GetValueOrDefault(x.BatchNumber)
        }).ToList();

        var entryBySuffix = entries
            .Where(x => !string.IsNullOrWhiteSpace(x.SequenceNumber))
            .GroupBy(x => SequenceSuffix(x.SequenceNumber!), StringComparer.Ordinal)
            .Where(x => x.Count() == 1)
            .ToDictionary(x => x.Key, x => x.Single(), StringComparer.Ordinal);

        var addendas = source.AddendaRecords.Select(x =>
        {
            var entry = entryBySuffix.GetValueOrDefault(x.EntryDetailSequenceNumber?.Trim() ?? string.Empty);
            return new AddendaRecord
            {
                CodeTypeAddendumRecord = x.CodeTypeAddendumRecord,
                BusinessType = x.BusinessType,
                IdUserOrig = x.IdUserOrig,
                PurposeOfTransaction = x.PurposeOfTransaction,
                InvoiceOrAccountNumber = x.InvoiceOrAccountNumber,
                InfofromOriginator = x.InfofromOriginator,
                CollectorId = x.CollectorId,
                ReceiverCustomerCode = x.ReceiverCustomerCode,
                ServiceDescription = x.ServiceDescription,
                PaymentRelatedInformation = x.PaymentRelatedInformation,
                ReturnReasonCode = x.ReturnReasonCode,
                OriginalTraceNumber = x.OriginalTraceNumber,
                NewTraceNumber = x.NewTraceNumber,
                AddendumSequence = x.AddendumSequence,
                EntryDetailSequenceNumber = x.EntryDetailSequenceNumber,
                NachaID = header.NachaID,
                NachaHeader = header,
                EntryDetail = entry
            };
        }).ToList();

        var controls = source.BatchControls.Select(x =>
        {
            var batch = int.TryParse(x.BatchNumber, out var batchNumber)
                ? batchByNumber.GetValueOrDefault(batchNumber)
                : null;
            return new BatchControl
            {
                BatchTranClassCode = x.BatchTranClassCode,
                EntryAddendaCount = x.EntryAddendaCount,
                EntryHash = x.EntryHash,
                TotalDebitAmount = x.TotalDebitAmount,
                TotalCreditAmount = x.TotalCreditAmount,
                IdUserOrig = x.IdUserOrig,
                CodAutMessage = x.CodAutMessage,
                Reserved = x.Reserved,
                IdOrigEntity = x.IdOrigEntity,
                BatchNumber = x.BatchNumber,
                NachaID = header.NachaID,
                NachaHeader = header,
                BatchHeader = batch
            };
        }).ToList();

        header.Batches = batches;
        header.EntryDetails = entries;
        header.AddendaRecords = addendas;
        header.BatchControls = controls;
        header.FileControls = source.FileControls.Select(x => new FileControl
        {
            BatchCount = x.BatchCount,
            BlockCount = x.BlockCount,
            EntryAddendaCount = x.EntryAddendaCount,
            EntryHash = x.EntryHash,
            TotalDebitAmount = x.TotalDebitAmount,
            TotalCreditAmount = x.TotalCreditAmount,
            Reserved = x.Reserved,
            NachaID = header.NachaID,
            NachaHeader = header
        }).ToList();

        return header;
    }

    private static string SequenceSuffix(string value)
        => value.Length <= 7 ? value : value[^7..];
}

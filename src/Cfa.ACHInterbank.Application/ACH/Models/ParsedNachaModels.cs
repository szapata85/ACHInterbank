namespace Cfa.ACHInterbank.Application.ACH.Models;

/// <summary>
/// Vista mínima, no persistente y no rastreada, usada antes del parseo completo.
/// </summary>
public sealed record NachaHeaderPreview(
    string ImmediateDestination,
    string ImmediateOrigin,
    DateOnly FileCreationDate,
    TimeOnly? FileCreationTime,
    DateOnly? EffectiveDate,
    int? CycleNumber,
    string FileIdentifier);

/// <summary>
/// Árbol temporal completo. Ningún tipo de este archivo es una entidad EF Core.
/// </summary>
public sealed class ParsedNachaFile
{
    public List<ParsedNachaHeader> Headers { get; } = [];
}

public sealed class ParsedNachaHeader
{
    public string? NachaID { get; set; }
    public string? PriorityCode { get; set; }
    public string? ImmediateDestination { get; set; }
    public string? ImmediateOrigin { get; set; }
    public string? FileCreationDate { get; set; }
    public string? FileCreationTime { get; set; }
    public string? FileIdModifier { get; set; }
    public string? RecordSize { get; set; }
    public string? BlockingFactor { get; set; }
    public string? FormatCode { get; set; }
    public string? ImmediateDestinationName { get; set; }
    public string? ImmediateOriginName { get; set; }
    public string? ReferenceCode { get; set; }
    public int? ClearingHouseId { get; set; }
    public int CycleNumber { get; set; }
    public string? AchCycleId { get; set; }
    public Guid? IncomingNachaFileIngestionId { get; set; }
    public List<ParsedBatchHeader> Batches { get; set; } = [];
    public List<ParsedEntryDetail> EntryDetails { get; set; } = [];
    public List<ParsedAddendaRecord> AddendaRecords { get; set; } = [];
    public List<ParsedBatchControl> BatchControls { get; set; } = [];
    public List<ParsedFileControl> FileControls { get; set; } = [];
}

public sealed class ParsedBatchHeader
{
    public int BatchID { get; set; }
    public string? ServiceClassCode { get; set; }
    public string? CompanyName { get; set; }
    public string? DiscretionaryData { get; set; }
    public string? CompanyId { get; set; }
    public string? StandardEntryClassCode { get; set; }
    public string? CompanyEntryDescription { get; set; }
    public string? DescriptiveDate { get; set; }
    public string? EffectiveEntryDate { get; set; }
    public string? CompensationDate { get; set; }
    public string? OriginUserStatusCode { get; set; }
    public string? OriginParticipantEntityCode { get; set; }
    public int BatchNumber { get; set; }
    public string? NachaID { get; set; }
}

public sealed class ParsedEntryDetail
{
    public int EntryDetailID { get; set; }
    public string? TransactionCode { get; set; }
    public string? ReceivingParticipantEntityCode { get; set; }
    public string? CheckDigit { get; set; }
    public string? AccountNumber { get; set; }
    public decimal? Amount { get; set; }
    public string? RecipIdNumber { get; set; }
    public string? RecipUserName { get; set; }
    public string? DiscreData { get; set; }
    public string? AddendumIndicator { get; set; }
    public string? SequenceNumber { get; set; }
    public int BatchNumber { get; set; }
    public string? NachaID { get; set; }
    public ParsedNachaHeader? NachaHeader { get; set; }
}

public sealed class ParsedAddendaRecord
{
    public int AddendaID { get; set; }
    public string? CodeTypeAddendumRecord { get; set; }
    public string? BusinessType { get; set; }
    public string? IdUserOrig { get; set; }
    public string? PurposeOfTransaction { get; set; }
    public string? InvoiceOrAccountNumber { get; set; }
    public string? InfofromOriginator { get; set; }
    public string? CollectorId { get; set; }
    public string? ReceiverCustomerCode { get; set; }
    public string? ServiceDescription { get; set; }
    public string? PaymentRelatedInformation { get; set; }
    public string? ReturnReasonCode { get; set; }
    public string? OriginalTraceNumber { get; set; }
    public string? NewTraceNumber { get; set; }
    public string? AddendumSequence { get; set; }
    public string? EntryDetailSequenceNumber { get; set; }
    public string? NachaID { get; set; }
}

public sealed class ParsedBatchControl
{
    public int BatchControlID { get; set; }
    public string? BatchTranClassCode { get; set; }
    public int? EntryAddendaCount { get; set; }
    public long? EntryHash { get; set; }
    public decimal TotalDebitAmount { get; set; }
    public decimal TotalCreditAmount { get; set; }
    public string? IdUserOrig { get; set; }
    public string? CodAutMessage { get; set; }
    public string? Reserved { get; set; }
    public string? IdOrigEntity { get; set; }
    public string? BatchNumber { get; set; }
    public string? NachaID { get; set; }
}

public sealed class ParsedFileControl
{
    public int FileControlID { get; set; }
    public int BatchCount { get; set; }
    public int BlockCount { get; set; }
    public int EntryAddendaCount { get; set; }
    public long EntryHash { get; set; }
    public decimal TotalDebitAmount { get; set; }
    public decimal TotalCreditAmount { get; set; }
    public string? Reserved { get; set; }
    public string? NachaID { get; set; }
}

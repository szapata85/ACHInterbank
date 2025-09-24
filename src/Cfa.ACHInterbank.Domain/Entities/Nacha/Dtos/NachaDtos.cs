namespace Cfa.ACHInterbank.Domain.Entities.Nacha.Dtos;

public class FileHeaderDto
{
    public string PriorityCode { get; set; } = "";
    public string ImmediateDestination { get; set; } = "";
    public string ImmediateOrigin { get; set; } = "";
    public DateTime FileCreationDate { get; set; }
    public DateTime FileCreationTime { get; set; }
    public string ReferenceCode { get; set; } = "";
}

public class BatchHeaderDto
{
    public string ServiceClassCode { get; set; } = "";
    public string CompanyName { get; set; } = "";
    public string CompanyIdentification { get; set; } = "";
    public string CompanyEntryDescription { get; set; } = "";
    public string OriginOrOdfi { get; set; } = "";
    public DateTime EffectiveEntryDate { get; set; }
}

public class EntryDetailDto
{
    public string TransactionCode { get; set; } = "";
    public string RoutingNumber { get; set; } = "";
    public string AccountNumber { get; set; } = "";
    public long AmountCents { get; set; }
    public string Reference { get; set; } = "";
}

public class AddendaFileDto
{
    public string Information { get; set; } = "";
    public int SequenceNumber { get; set; }
    public int EntryDetailSequenceNumber { get; set; }
}

public class BatchControlDto
{
    public string ServiceClassCode { get; set; } = "";
    public int EntryAddendaCount { get; set; }
    public long TotalDebitAmountCents { get; set; }
    public long TotalCreditAmountCents { get; set; }
    public string CompanyIdentification { get; set; } = "";
}

public class FileControlDto
{
    public int BatchCount { get; set; }
    public int BlockCount { get; set; }
    public int EntryAddendaCount { get; set; }
    public long TotalDebitAmountCents { get; set; }
    public long TotalCreditAmountCents { get; set; }
}


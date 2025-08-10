using Cfa.ACHInterbank.Application.ACH.Interfaces;
using Cfa.ACHInterbank.Domain.Models.ACH;
using Cfa.ACHInterbank.Persistence.DataBase;

namespace Cfa.ACHInterbank.Persistence.ACH.Services;

public class NachaParserServiceScoped : INachaParserServiceScoped
{
    private readonly AchDbContext _context;

    public NachaParserServiceScoped(AchDbContext context)
    {
        _context = context;
    }

    public async Task ParseAndSaveAsync(Stream nachaStream)
    {
        using var reader = new StreamReader(nachaStream);
        string? linefull = await reader.ReadLineAsync();
        int LenghtLine = int.Parse(linefull!.Substring(36, 3));


        List<string> lines = Enumerable.Range(0, (int)Math.Ceiling((double)linefull.Length / LenghtLine))
                      .Select(i => linefull.Substring(i * LenghtLine, Math.Min(LenghtLine, linefull.Length - i * LenghtLine)))
                      .ToList();


        foreach (string line in lines)
        {
            if (string.IsNullOrWhiteSpace(line)) continue;

            char recordType = line[0];

            switch (recordType)
            {
                case '1':
                    _context.NachaHeaders.Add(ParseFileHeader(line));
                    break;
                case '5':
                    _context.BatchHeaders.Add(ParseBatchHeader(line));
                    break;
                case '6':
                    _context.EntryDetails.Add(ParseEntryDetail(line));
                    break;
                case '7':
                    _context.AddendaRecords.Add(ParseAddenda(line));
                    break;
                case '8':
                    _context.BatchControls.Add(ParseBatchControl(line));
                    break;
                case '9':
                    _context.FileControls.Add(ParseFileControl(line));
                    break;
            }
        }

        await _context.SaveChangesAsync();
    }

    private NachaHeader ParseFileHeader(string line)
    {
        return new NachaHeader
        {
            PriorityCode = line.Substring(1, 2),
            ImmediateDestination = line.Substring(3, 10).Trim(),
            ImmediateOrigin = line.Substring(13, 10).Trim(),
            FileCreationDate = line.Substring(23, 8),
            FileCreationTime = line.Substring(31, 4),
            FileIdModifier = line.Substring(35, 1),
            RecordSize = line.Substring(36, 3),
            BlockingFactor = line.Substring(39, 2),
            FormatCode = line.Substring(41, 1),
            ImmediateDestinationName = line.Substring(42, 23).Trim(),
            ImmediateOriginName = line.Substring(65, 23).Trim(),
            ReferenceCode = line.Substring(88, 8).Trim()
        };
    }

    private BatchHeader ParseBatchHeader(string line)
    {
        var varreturn = new BatchHeader
        {
            ServiceClassCode = line.Substring(1, 3),
            CompanyName = line.Substring(4, 16).Trim(),
            DiscretionaryData = line.Substring(20, 20).Trim(),
            CompanyId = line.Substring(40, 10).Trim(),
            StandardEntryClassCode = line.Substring(50, 3),
            CompanyEntryDescription = line.Substring(53, 10).Trim(),
            EffectiveEntryDate = line.Substring(69, 6),
            OdfiIdentification = line.Substring(79, 8)
        };

        return varreturn;
    }

    private EntryDetail ParseEntryDetail(string line)
    {
        return new EntryDetail
        {
            TransactionCode = line.Substring(1, 2),
            ReceivingDfiIdentification = line.Substring(3, 8),
            DfiAccountNumber = line.Substring(12, 17).Trim(),
            Amount = Convert.ToDecimal(line.Substring(29, 10)) / 100,
            IndividualIdNumber = line.Substring(39, 15).Trim(),
            IndividualName = line.Substring(54, 22).Trim(),
            TraceNumber = line.Substring(79, 15)
        };
    }

    private AddendaRecord ParseAddenda(string line)
    {
        return new AddendaRecord
        {
            PaymentRelatedInformation = line.Substring(3, 80).Trim(),
            AddendaSequenceNumber = line.Substring(83, 4),
            EntryDetailSequenceNumber = line.Substring(87, 7)
        };
    }

    private BatchControl ParseBatchControl(string line)
    {
        return new BatchControl
        {
            ServiceClassCode = line.Substring(1, 3),
            EntryAddendaCount = int.Parse(line.Substring(4, 6)),
            EntryHash = Convert.ToDecimal(line.Substring(10, 10)),
            TotalDebitAmount = Convert.ToDecimal(line.Substring(20, 12)) / 100,
            TotalCreditAmount = Convert.ToDecimal(line.Substring(32, 12)) / 100,
            CompanyId = line.Substring(44, 10).Trim(),
            OdfiIdentification = line.Substring(79, 8)
        };
    }

    private FileControl ParseFileControl(string line)
    {
        return new FileControl
        {
            BatchCount = int.Parse(line.Substring(1, 6)),
            BlockCount = int.Parse(line.Substring(7, 6)),
            EntryAddendaCount = int.Parse(line.Substring(13, 8)),
            EntryHash = Convert.ToDecimal(line.Substring(21, 10)),
            TotalDebitAmount = Convert.ToDecimal(line.Substring(31, 12)) / 100,
            TotalCreditAmount = Convert.ToDecimal(line.Substring(43, 12)) / 100
        };
    }
}

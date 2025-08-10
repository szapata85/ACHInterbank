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

        IEnumerable<char> recordsTypes = lines.Select(a => a[0]).Distinct();

        foreach (char recordType in recordsTypes)
        {
            List<string> resultLine = lines.Where(a => a[0] == recordType).ToList();


            switch (recordType)
            {
                case '1':
                    _context.NachaHeaders.AddRange(ParseFileHeaderLinq(resultLine));
                    break;
                case '5':
                    _context.BatchHeaders.AddRange(ParseBatchHeaderLinq(resultLine));
                    break;
                case '6':
                    //_context.EntryDetails.Add(ParseEntryDetail(line));
                    break;
                case '7':
                    //_context.AddendaRecords.Add(ParseAddenda(line));
                    break;
                case '8':
                    //_context.BatchControls.Add(ParseBatchControl(line));
                    break;
                case '9':
                    //_context.FileControls.Add(ParseFileControl(line));
                    break;
            }
        }

        await _context.SaveChangesAsync();
    }

    private List<NachaHeader> ParseFileHeaderLinq(List<string> line)
    {
        return line.Select(a => new NachaHeader
        {
            PriorityCode = a.Substring(1, 2),
            ImmediateDestination = a.Substring(3, 10).Trim(),
            ImmediateOrigin = a.Substring(13, 10).Trim(),
            FileCreationDate = a.Substring(23, 8),
            FileCreationTime = a.Substring(31, 4),
            FileIdModifier = a.Substring(35, 1),
            RecordSize = a.Substring(36, 3),
            BlockingFactor = a.Substring(39, 2),
            FormatCode = a.Substring(41, 1),
            ImmediateDestinationName = a.Substring(42, 23).Trim(),
            ImmediateOriginName = a.Substring(65, 23).Trim(),
            ReferenceCode = a.Substring(88, 8).Trim()
        }).ToList();
    }

    private List<BatchHeader> ParseBatchHeaderLinq(List<string> line)
    {
        return line.Select(a => new BatchHeader
        {
            ServiceClassCode = a.Substring(1, 3),
            CompanyName = a.Substring(4, 16).Trim(),
            DiscretionaryData = a.Substring(20, 20).Trim(),
            CompanyId = a.Substring(40, 10).Trim(),
            StandardEntryClassCode = a.Substring(50, 3),
            CompanyEntryDescription = a.Substring(53, 10).Trim(),
            DescriptiveDate = a.Substring(63, 8).Trim(),
            EffectiveEntryDate = a.Substring(71, 8),
            CompensationDate = a.Substring(79, 3),
            OriginUserStatusCode = a.Substring(82, 1),
            OriginParticipantEntityCode = a.Substring(83, 8),
            BatchNumber = int.Parse(a.Substring(91, 7))
        }).ToList();
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

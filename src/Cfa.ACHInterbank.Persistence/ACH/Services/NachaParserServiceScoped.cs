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
        try
        {
            _context.ChangeTracker.AutoDetectChangesEnabled = false;

            using var reader = new StreamReader(nachaStream);
            string? linefull = await reader.ReadLineAsync();
            int LenghtLine = int.Parse(linefull!.Substring(36, 3));


            List<string> lines = Enumerable.Range(0, (int)Math.Ceiling((double)linefull.Length / LenghtLine))
                          .Select(i => linefull.Substring(i * LenghtLine, Math.Min(LenghtLine, linefull.Length - i * LenghtLine)))
                          .ToList();

            IEnumerable<char> recordsTypes = lines.Select(a => a[0]).Distinct();

            List<NachaHeader> LstNachaHeader = new();
            List<BatchHeader> LstBatchHeader = new();
            List<EntryDetail> LstEntryDetail = new();
            List<AddendaRecord> LstAddendaRecord = new();

            foreach (char recordType in recordsTypes)
            {
                List<string> resultLine = lines.Where(a => a[0] == recordType).ToList();


                switch (recordType)
                {
                    case '1':
                        LstNachaHeader = ParseFileHeaderLinq(resultLine);
                        break;
                    case '5':
                        LstBatchHeader = ParseBatchHeaderLinq(resultLine);
                        break;
                    case '6':
                        LstEntryDetail = ParseEntryDetailLinq(resultLine);
                        break;
                    case '7':
                        LstAddendaRecord = ParseAddendaLinq(resultLine);
                        break;
                    case '8':
                        //_context.BatchControls.Add(ParseBatchControl(line));
                        break;
                    case '9':
                        //_context.FileControls.Add(ParseFileControl(line));
                        break;
                }
            }

            LstNachaHeader[0].Batches = LstBatchHeader;
            LstNachaHeader[0].EntryDetails = LstEntryDetail;
            LstNachaHeader[0].AddendaRecords = LstAddendaRecord;
            _context.NachaHeaders.AddRange(LstNachaHeader);

            await _context.SaveChangesAsync();
            _context.ChangeTracker.AutoDetectChangesEnabled = true;
        }
        catch(Exception ex)
        {
            var mensaje = ex.GetBaseException().ToString();
        }
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
            StandardEntryClassCode = a.Substring(50, 3).Trim(),
            CompanyEntryDescription = a.Substring(53, 10).Trim(),
            DescriptiveDate = a.Substring(63, 8).Trim(),
            EffectiveEntryDate = a.Substring(71, 8).Trim(),
            CompensationDate = a.Substring(79, 3).Trim(),
            OriginUserStatusCode = a.Substring(82, 1).Trim(),
            OriginParticipantEntityCode = a.Substring(83, 8).Trim(),
            BatchNumber = int.Parse(a.Substring(91, 7).Trim())
        }).ToList();
    }

    private List<EntryDetail> ParseEntryDetailLinq(List<string> line)
    {
        return line.Select(a => new EntryDetail
        {
            TransactionCode = a.Substring(1, 2).Trim(),
            ReceivingParticipantEntityCode = a.Substring(3, 8).Trim(),
            CheckDigit = a.Substring(11, 1).Trim(),
            AccountNumber = a.Substring(12, 17).Trim(),
            Amount = Convert.ToDecimal(a.Substring(29, 18)) / 100,
            RecipIdNumber = a.Substring(47, 15).Trim(),
            RecipUserName = a.Substring(62, 22).Trim(),
            DiscreData = a.Substring(84, 2).Trim(),
            AddendumIndicator = a.Substring(86, 1).Trim(),
            SequenceNumber = a.Substring(87, 15).Trim()
        }).ToList();
    }

    private List<AddendaRecord> ParseAddendaLinq(List<string> line)
    {
        List<AddendaRecord> resultAddendaRecord = new();

        resultAddendaRecord = line.Select(a => new AddendaRecord
        {
            CodeTypeAddendumRecord = a.Substring(1, 2).Trim(),
            IdUserOrig = a.Substring(3,15).Trim(),
            PurposeOfTransaction = a.Substring(20, 10).Trim(),
            InvoiceOrAccountNumber = (a.Substring(20, 10).ToUpper().Trim() == "TRANSFER")?a.Substring(30,24).Trim() : a.Substring(30, 53).Trim(),
            InfofromOriginator = (a.Substring(20, 10).ToUpper().Trim() == "TRANSFER") ? a.Substring(56, 24).Trim() : null,
            AddendumSequence = a.Substring(83, 4).Trim(),
            EntryDetailSequenceNumber = a.Substring(87, 7).Trim()
        }).ToList();

        return resultAddendaRecord;
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

using Cfa.ACHInterbank.Application.ACH.Interfaces;
using Cfa.ACHInterbank.Application.Helpers.Hash;
using Cfa.ACHInterbank.Domain.Models.ACH;
using Cfa.ACHInterbank.Domain.Models.Configurations;
using Cfa.ACHInterbank.Persistence.DataBase;
using Microsoft.EntityFrameworkCore;

namespace Cfa.ACHInterbank.Persistence.ACH.Services;

[Scoped]
public class NachaParserService : INachaParserService
{
    private readonly AchDbContext _context;

    public NachaParserService(AchDbContext context)
    {
        _context = context;
    }

    public async Task ParseAndSaveAsync(Stream nachaStream, string FileName)
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

            foreach (char recordType in recordsTypes)
            {
                List<string> resultLine = lines.Where(a => a[0] == recordType).ToList();


                switch (recordType)
                {
                    case '1':
                        // Precargar mapeo de códigos desde la BD
                        Dictionary<string, int> clearingHouseMap = await _context.ClearingHouses
                            .AsNoTracking()
                            .ToDictionaryAsync(ch => ch.OriginCode.Trim(), ch => ch.Id);

                        LstNachaHeader = ParseFileHeaderLinq(resultLine, clearingHouseMap, FileName);

                        //Validación de existencia
                        bool NachaHeadersExists = await _context.NachaHeaders
                            .AnyAsync(p => p.FileCreationDate == LstNachaHeader[0].FileCreationDate
                                        && p.FileCreationTime == LstNachaHeader[0].FileCreationTime
                                        && p.FileIdModifier == LstNachaHeader[0].FileIdModifier
                                        && p.ImmediateOrigin == LstNachaHeader[0].ImmediateOrigin);

                        if (NachaHeadersExists)
                            throw new ArgumentException("El Archivo NACHA ya existe!");

                        break;
                    case '5':
                        LstNachaHeader[0].Batches = ParseBatchHeaderLinq(resultLine);
                        break;
                    case '6':
                        LstNachaHeader[0].EntryDetails = ParseEntryDetailLinq(resultLine);
                        break;
                    case '7':
                        LstNachaHeader[0].AddendaRecords = ParseAddendaLinq(resultLine);
                        break;
                    case '8':
                        LstNachaHeader[0].BatchControls = ParseBatchControlLinq(resultLine);
                        break;
                    case '9':
                        LstNachaHeader[0].FileControls = ParseFileControlLinq(resultLine);
                        break;
                }
            }

            _context.NachaHeaders.AddRange(LstNachaHeader);

            await _context.SaveChangesAsync();
            _context.ChangeTracker.AutoDetectChangesEnabled = true;
        }
        catch (Exception ex)
        {
            var mensaje = ex.GetBaseException().ToString();
        }
    }

    private List<NachaHeader> ParseFileHeaderLinq(List<string> line, Dictionary<string, int> clearingHouseMap, string FileName)
    {
        var parts = FileName.Split('.');
        var cycleNumber = int.Parse(parts[1]);



        return line.Select(a =>
        {
            string ImmediateOrigin = a.Substring(13, 10).Trim();
            int? ClearingHouseId = clearingHouseMap.TryGetValue(ImmediateOrigin, out var chId) ? chId : (int?)null;

            int? AchCycleId = _context.AchCycles.Where(c => c.ClearingHouseId == ClearingHouseId &&
                                         c.ProcessingDate == DateTime.Today &&
                                         c.CycleName.Contains(cycleNumber.ToString())
                                   ).Select(c => (int?)c.Id)
                             .FirstOrDefault();

            return new NachaHeader
            {
                NachaID = HashHelper.GenerateHashSha1(
                    $"{a.Substring(3, 10).Trim()}{ImmediateOrigin}{a.Substring(23, 8).Trim()}{a.Substring(31, 4).Trim()}"),
                PriorityCode = a.Substring(1, 2),
                ImmediateDestination = a.Substring(3, 10).Trim(),
                ImmediateOrigin = ImmediateOrigin,
                FileCreationDate = a.Substring(23, 8),
                FileCreationTime = a.Substring(31, 4),
                FileIdModifier = a.Substring(35, 1),
                RecordSize = a.Substring(36, 3),
                BlockingFactor = a.Substring(39, 2),
                FormatCode = a.Substring(41, 1),
                ImmediateDestinationName = a.Substring(42, 23).Trim(),
                ImmediateOriginName = a.Substring(65, 23).Trim(),
                ReferenceCode = a.Substring(88, 8).Trim(),
                ClearingHouseId = ClearingHouseId,
                CycleNumber = cycleNumber,
                // 🔹 Relacionar con AchCycle (si existe en BD)
                AchCycleId = AchCycleId
            };
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
        return line.Select(a => new AddendaRecord
        {
            CodeTypeAddendumRecord = a.Substring(1, 2).Trim(),
            IdUserOrig = a.Substring(3, 15).Trim(),
            PurposeOfTransaction = a.Substring(20, 10).Trim(),
            InvoiceOrAccountNumber = (a.Substring(20, 10).ToUpper().Trim() == "TRANSFER") ? a.Substring(30, 24).Trim() : a.Substring(30, 53).Trim(),
            InfofromOriginator = (a.Substring(20, 10).ToUpper().Trim() == "TRANSFER") ? a.Substring(56, 24).Trim() : null,
            AddendumSequence = a.Substring(83, 4).Trim(),
            EntryDetailSequenceNumber = a.Substring(87, 7).Trim()
        }).ToList();
    }


    private List<BatchControl> ParseBatchControlLinq(List<string> line)
    {
        return line.Select(a => new BatchControl
        {
            BatchTranClassCode = a.Substring(1, 3),
            EntryAddendaCount = int.Parse(a.Substring(4, 6)),
            TotalEntry = int.Parse(a.Substring(10, 10)),
            TotalDebitAmount = Convert.ToDecimal(a.Substring(20, 18).Trim()) / 100,
            TotalCreditAmount = Convert.ToDecimal(a.Substring(38, 18).Trim()) / 100,
            IdUserOrig = a.Substring(56, 10).Trim(),
            CodAutMessage = a.Substring(66, 19),
            IdOrigEntity = a.Substring(91, 8),
            BatchNumber = a.Substring(99, 7),
        }).ToList();
    }

    private List<FileControl> ParseFileControlLinq(List<string> line)
    {
        return line.Take(1).Select(a => new FileControl
        {
            BatchCount = int.Parse(a.Substring(1, 6)),
            BlockCount = int.Parse(a.Substring(7, 6)),
            EntryAddendaCount = int.Parse(a.Substring(13, 8)),
            TotalControl = Convert.ToDecimal(a.Substring(21, 10)) / 100,
            TotalDebitAmount = Convert.ToDecimal(a.Substring(31, 18)) / 100,
            TotalCreditAmount = Convert.ToDecimal(a.Substring(49, 18)) / 100
        }).ToList();
    }
}

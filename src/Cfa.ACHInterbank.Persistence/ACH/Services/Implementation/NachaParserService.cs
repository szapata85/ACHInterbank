using Cfa.ACHInterbank.Application.ACH.Interfaces;
using Cfa.ACHInterbank.Application.ACH.Models;
using Cfa.ACHInterbank.Application.Helpers.Hash;
using Cfa.ACHInterbank.Domain.Entities.Transactions.Enums;
using Cfa.ACHInterbank.Domain.Models.ACH;
using Cfa.ACHInterbank.Domain.Models.Configurations;
using Cfa.ACHInterbank.Persistence.DataBase;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Cfa.ACHInterbank.Persistence.ACH.Services.Implementation;

[Scoped]
public class NachaParserService : INachaParserService
{
    private readonly AchDbContext _context;
    private readonly ILogger<NachaParserService> _logger;
    private HashSet<string>? _configuredTransactionCodes;

    public NachaParserService(AchDbContext context, ILogger<NachaParserService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<IReadOnlyList<NachaValidationFailure>> ParseAndSaveAsync(Stream nachaStream, string FileName)
    {
        var failures = new List<NachaValidationFailure>();

        try
        {
            _context.ChangeTracker.AutoDetectChangesEnabled = false;

            using var reader = new StreamReader(nachaStream);
            string? linefull = await reader.ReadLineAsync();
            int LenghtLine = int.Parse(linefull!.Substring(36, 3));

            List<string> lines = Enumerable.Range(0, (int)Math.Ceiling((double)linefull.Length / LenghtLine))
                .Select(i => linefull.Substring(i * LenghtLine, Math.Min(LenghtLine, linefull.Length - i * LenghtLine)))
                .ToList();

            var clearingHouseMap = await _context.ClearingHouses
                .AsNoTracking()
                .ToDictionaryAsync(ch => ch.OriginCode.Trim(), ch => ch.Id);

            List<NachaHeader> headers = new();
            NachaHeader? currentHeader = null;
            BatchHeader? currentBatch = null;
            EntryDetail? lastEntry = null;
            var entryDetails = new List<EntryDetail>();
            var addendaRecords = new List<AddendaRecord>();
            var batchControls = new List<BatchControl>();
            var fileControls = new List<FileControl>();

            foreach (var line in lines)
            {
                var recordType = line[0];
                switch (recordType)
                {
                    case '1':
                        currentHeader = ParseFileHeaderLinq([line], clearingHouseMap, FileName).FirstOrDefault();
                        if (currentHeader is null)
                        {
                            break;
                        }
                        currentHeader.Batches = new List<BatchHeader>();
                        currentHeader.EntryDetails = new List<EntryDetail>();
                        currentHeader.AddendaRecords = new List<AddendaRecord>();
                        currentHeader.BatchControls = new List<BatchControl>();
                        currentHeader.FileControls = new List<FileControl>();

                        bool NachaHeadersExists = await _context.NachaHeaders
                            .AnyAsync(p => p.FileCreationDate == currentHeader.FileCreationDate
                                        && p.FileCreationTime == currentHeader.FileCreationTime
                                        && p.FileIdModifier == currentHeader.FileIdModifier
                                        && p.ImmediateOrigin == currentHeader.ImmediateOrigin);

                        if (NachaHeadersExists)
                        {
                            throw new ArgumentException("El Archivo NACHA ya existe!");
                        }

                        headers.Add(currentHeader);
                        break;
                    case '5':
                        currentBatch = ParseBatchHeaderLinq([line]).FirstOrDefault();
                        if (currentBatch is not null)
                        {
                            currentBatch.NachaID = currentHeader?.NachaID;
                            currentHeader?.Batches?.Add(currentBatch);
                        }
                        break;
                    case '6':
                        var entry = ParseEntryDetailLinq([line]).FirstOrDefault();
                        if (entry is null)
                        {
                            break;
                        }

                        entry.NachaID = currentHeader?.NachaID;

                        var (isValid, failureReason) = await ValidateEntryAsync(entry, currentBatch, failures);
                        if (isValid)
                        {
                            entryDetails.Add(entry);
                            lastEntry = entry;
                        }
                        else
                        {
                            lastEntry = null;
                        }

                        if (PrenoteCodes.Contains(entry.TransactionCode ?? string.Empty))
                        {
                            await UpdateThirdPartyStatusAsync(entry, currentHeader?.AchCycleId, isValid, failureReason);
                        }
                        break;
                    case '7':
                        var addenda = ParseAddendaLinq([line]).FirstOrDefault();
                        if (addenda is not null)
                        {
                            addenda.NachaID = currentHeader?.NachaID;
                            if (lastEntry is null)
                            {
                                failures.Add(new NachaValidationFailure("7", currentBatch?.BatchNumber.ToString(), null, null,
                                    "Registro Addenda sin detalle asociado."));
                            }
                            else
                            {
                                addenda.EntryDetailSequenceNumber ??= GetEntrySequenceSuffix(lastEntry.SequenceNumber);
                                addendaRecords.Add(addenda);
                            }
                        }
                        break;
                    case '8':
                        var batchControl = ParseBatchControlLinq([line]).FirstOrDefault();
                        if (batchControl is not null)
                        {
                            batchControls.Add(batchControl);
                        }
                        break;
                    case '9':
                        var fileControl = ParseFileControlLinq([line]).FirstOrDefault();
                        if (fileControl is not null)
                        {
                            fileControls.Add(fileControl);
                        }
                        break;
                }
            }

            var validEntries = EnforceAddendaRequirements(entryDetails, addendaRecords, failures);
            if (currentHeader is not null)
            {
                currentHeader.EntryDetails = validEntries;
                currentHeader.AddendaRecords = addendaRecords
                    .Where(addenda => validEntries.Any(entry => IsAddendaForEntry(entry, addenda)))
                    .ToList();
                currentHeader.BatchControls = batchControls;
                currentHeader.FileControls = fileControls;
            }

            _context.NachaHeaders.AddRange(headers);

            await _context.SaveChangesAsync();
            _context.ChangeTracker.AutoDetectChangesEnabled = true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error procesando archivo NACHA: {FileName}", FileName);
            throw;
        }

        return failures;
    }

    private List<NachaHeader> ParseFileHeaderLinq(List<string> line, Dictionary<string, int> clearingHouseMap, string FileName)
    {
        var parts = FileName.Split('.');
        var cycleNumber = int.Parse(parts[1]);



        return line.Select(a =>
        {
            string ImmediateOrigin = a.Substring(13, 10).Trim();
            int? ClearingHouseId = clearingHouseMap.TryGetValue(ImmediateOrigin, out var chId) ? chId : null;

            string? AchCycleId = _context.AchCycles.Where(c => c.ClearingHouseId == ClearingHouseId &&
                                         c.ProcessingDate == DateTime.Today &&
                                         c.CycleName.Contains(cycleNumber.ToString())
                                   ).Select(c => c.Id)
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
            AccountNumber = a.Substring(12, 17).TrimEnd(),
            Amount = Convert.ToDecimal(a.Substring(29, 18)) / 100,
            RecipIdNumber = a.Substring(47, 15).TrimEnd(),
            RecipUserName = a.Substring(62, 22).Trim(),
            DiscreData = a.Substring(84, 2),
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
            InvoiceOrAccountNumber = a.Substring(20, 10).ToUpper().Trim() == "TRANSFER" ? a.Substring(30, 24).Trim() : a.Substring(30, 53).Trim(),
            InfofromOriginator = a.Substring(20, 10).ToUpper().Trim() == "TRANSFER" ? a.Substring(56, 24).Trim() : null,
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

    private static readonly HashSet<string> FallbackTransactionCodes = new(StringComparer.OrdinalIgnoreCase)
    {
        "21", "22", "23", "26", "27", "28",
        "31", "32", "33", "36", "37", "38",
        "42", "51", "52", "53", "55", "56", "57"
    };

    private static readonly HashSet<string> CreditCodes = new(StringComparer.OrdinalIgnoreCase)
    {
        "21", "22", "23", "31", "32", "33", "42", "51", "52", "53"
    };

    private static readonly HashSet<string> DebitCodes = new(StringComparer.OrdinalIgnoreCase)
    {
        "26", "27", "28", "36", "37", "38", "55", "56", "57"
    };

    private static readonly HashSet<string> PrenoteCodes = new(StringComparer.OrdinalIgnoreCase)
    {
        "23", "33", "53", "28", "38", "57"
    };

    private async Task<(bool IsValid, string? FailureReason)> ValidateEntryAsync(
        EntryDetail entry,
        BatchHeader? batch,
        List<NachaValidationFailure> failures)
    {
        var code = entry.TransactionCode ?? string.Empty;
        var configuredCodes = await GetConfiguredTransactionCodesAsync();
        if (!configuredCodes.Contains(code))
        {
            const string reason = "Código de transacción inválido.";
            failures.Add(new NachaValidationFailure("6", batch?.BatchNumber.ToString(), entry.SequenceNumber, code, reason));
            return (false, reason);
        }

        var isCredit = CreditCodes.Contains(code);
        var isDebit = DebitCodes.Contains(code);
        var serviceClassCode = batch?.ServiceClassCode?.Trim();

        if (serviceClassCode == "220" && !isCredit)
        {
            const string reason = "Lote exclusivo de crédito (220) no permite débitos.";
            failures.Add(new NachaValidationFailure("6", batch?.BatchNumber.ToString(), entry.SequenceNumber, code, reason));
            return (false, reason);
        }

        if (serviceClassCode == "225" && !isDebit)
        {
            const string reason = "Lote exclusivo de débito (225) no permite créditos.";
            failures.Add(new NachaValidationFailure("6", batch?.BatchNumber.ToString(), entry.SequenceNumber, code, reason));
            return (false, reason);
        }

        if (PrenoteCodes.Contains(code) && entry.Amount.GetValueOrDefault() != 0m)
        {
            const string reason = "Prenotificación debe tener valor 0.";
            failures.Add(new NachaValidationFailure("6", batch?.BatchNumber.ToString(), entry.SequenceNumber, code, reason));
            return (false, reason);
        }

        if (!string.Equals(entry.AddendumIndicator, "1", StringComparison.OrdinalIgnoreCase))
        {
            const string reason = "El registro 7 es obligatorio para todas las transacciones.";
            failures.Add(new NachaValidationFailure("6", batch?.BatchNumber.ToString(), entry.SequenceNumber, code, reason));
            return (false, reason);
        }

        var requiresIdentityValidation = isDebit || ShouldValidateCreditIdentity(entry.DiscreData);
        if (requiresIdentityValidation)
        {
            if (string.IsNullOrWhiteSpace(entry.RecipIdNumber))
            {
                const string reason = "R17: La identificación no coincide con cuenta del usuario receptor.";
                failures.Add(new NachaValidationFailure("6", batch?.BatchNumber.ToString(), entry.SequenceNumber, code, reason));
                return (false, reason);
            }

            var accountNumber = entry.AccountNumber ?? string.Empty;
            var recipientId = entry.RecipIdNumber ?? string.Empty;
            var matches = await _context.Customers
                .AsNoTracking()
                .AnyAsync(c => c.AccountNumber == accountNumber && c.DocumentNumber == recipientId);

            if (!matches)
            {
                const string reason = "R17: La identificación no coincide con cuenta del usuario receptor.";
                failures.Add(new NachaValidationFailure("6", batch?.BatchNumber.ToString(), entry.SequenceNumber, code, reason));
                return (false, reason);
            }
        }

        return (true, null);
    }

    private async Task UpdateThirdPartyStatusAsync(
        EntryDetail entry,
        string? validationCycleId,
        bool isValid,
        string? failureReason)
    {
        if (string.IsNullOrWhiteSpace(entry.AccountNumber) || string.IsNullOrWhiteSpace(entry.RecipIdNumber))
        {
            return;
        }

        var thirdParty = await _context.CustomerThirdParties
            .FirstOrDefaultAsync(t =>
                t.DestinationAccountNumber == entry.AccountNumber &&
                t.RecipientIdNumber == entry.RecipIdNumber &&
                t.Status == CustomerThirdPartyStatusEnum.Pending);

        if (thirdParty is null)
        {
            return;
        }

        thirdParty.Status = isValid ? CustomerThirdPartyStatusEnum.Active : CustomerThirdPartyStatusEnum.Rejected;
        thirdParty.ValidationCycleId = validationCycleId;
        thirdParty.ValidationReceivedAt = DateTime.UtcNow;
        thirdParty.ValidationMessage = isValid ? null : failureReason;
        _context.Entry(thirdParty).State = EntityState.Modified;
    }

    private static List<EntryDetail> EnforceAddendaRequirements(
        List<EntryDetail> entries,
        List<AddendaRecord> addendaRecords,
        List<NachaValidationFailure> failures)
    {
        var validEntries = new List<EntryDetail>();
        foreach (var entry in entries)
        {
            if (!addendaRecords.Any(addenda => IsAddendaForEntry(entry, addenda)))
            {
                failures.Add(new NachaValidationFailure("6", null, entry.SequenceNumber, entry.TransactionCode,
                    "No se encontró registro 7 asociado al detalle."));
                continue;
            }

            validEntries.Add(entry);
        }

        return validEntries;
    }

    private static bool IsAddendaForEntry(EntryDetail entry, AddendaRecord addenda)
    {
        var entrySequence = GetEntrySequenceSuffix(entry.SequenceNumber);
        return !string.IsNullOrWhiteSpace(entrySequence) &&
               string.Equals(addenda.EntryDetailSequenceNumber, entrySequence, StringComparison.OrdinalIgnoreCase);
    }

    private static string? GetEntrySequenceSuffix(string? sequence)
    {
        if (string.IsNullOrWhiteSpace(sequence))
        {
            return null;
        }

        return sequence.Length <= 7 ? sequence : sequence[^7..];
    }


    private async Task<HashSet<string>> GetConfiguredTransactionCodesAsync()
    {
        if (_configuredTransactionCodes is not null)
        {
            return _configuredTransactionCodes;
        }

        var configuredCodes = await _context.TransactionCodes
            .AsNoTracking()
            .Select(x => x.Code)
            .ToListAsync();

        _configuredTransactionCodes = configuredCodes.Count == 0
            ? new HashSet<string>(FallbackTransactionCodes, StringComparer.OrdinalIgnoreCase)
            : new HashSet<string>(configuredCodes, StringComparer.OrdinalIgnoreCase);

        return _configuredTransactionCodes;
    }

    private static bool ShouldValidateCreditIdentity(string? discretionaryData)
    {
        if (string.IsNullOrWhiteSpace(discretionaryData))
        {
            return false;
        }

        return discretionaryData.StartsWith("V", StringComparison.OrdinalIgnoreCase);
    }
}

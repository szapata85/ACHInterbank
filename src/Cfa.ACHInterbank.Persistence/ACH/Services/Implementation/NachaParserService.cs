using Cfa.ACHInterbank.Application.ACH.Interfaces;
using Cfa.ACHInterbank.Application.ACH.Models;
using Cfa.ACHInterbank.Application.Helpers.Hash;
using Cfa.ACHInterbank.Application.Helpers.ACH;
using Cfa.ACHInterbank.Domain.Entities.Transactions.Enums;
using Cfa.ACHInterbank.Domain.Helpers;
using Cfa.ACHInterbank.Domain.Models.ACH;
using Cfa.ACHInterbank.Domain.Models.Configurations;
using Cfa.ACHInterbank.Persistence.DataBase;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Globalization;
using System.Text.RegularExpressions;

namespace Cfa.ACHInterbank.Persistence.ACH.Services.Implementation;

[Scoped]
public class NachaParserService : INachaParserService
{
    private readonly AchDbContext _context;
    private readonly ILogger<NachaParserService> _logger;
    private readonly IAchStateTransitionService _stateTransitionService;
    private HashSet<string>? _configuredTransactionCodes;

    public NachaParserService(
        AchDbContext context,
        ILogger<NachaParserService> logger,
        IAchStateTransitionService stateTransitionService)
    {
        _context = context;
        _logger = logger;
        _stateTransitionService = stateTransitionService;
    }

    public async Task<IReadOnlyList<NachaValidationFailure>> ParseAndSaveAsync(Stream nachaStream, string FileName, CancellationToken ct = default)
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
                .ToDictionaryAsync(ch => ch.OriginCode.Trim(), ch => ch.Id, ct);

            List<NachaHeader> headers = new();
            NachaHeader? currentHeader = null;
            BatchHeader? currentBatch = null;
            EntryDetail? lastEntry = null;
            var entryDetails = new List<EntryDetail>();
            var addendaRecords = new List<AddendaRecord>();
            var batchControls = new List<BatchControl>();
            var fileControls = new List<FileControl>();
            var lastConsecutiveByBatch = new Dictionary<int, int>();
            var seenSequenceNumbers = new HashSet<string>(StringComparer.Ordinal);

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
                                        && p.ImmediateOrigin == currentHeader.ImmediateOrigin, ct);

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

                        await ValidateEntrySequencePolicyAsync(entry, currentBatch, currentHeader, lastConsecutiveByBatch, seenSequenceNumbers, ct);

                        var (isValid, failureReason) = await ValidateEntryAsync(entry, currentBatch, failures, ct);
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
                            await UpdateThirdPartyStatusAsync(entry, currentHeader?.AchCycleId, isValid, failureReason, ct);
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
            ValidateBatchSequenceAndControlConsistency(headers.SelectMany(h => h.Batches ?? []), batchControls);

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

            await _context.SaveChangesAsync(ct);
            var validAddendas = currentHeader?.AddendaRecords?.ToList() ?? [];
            await ApplyReturnStateTransitionsAsync(validEntries, validAddendas, failures, ct);
            _context.ChangeTracker.AutoDetectChangesEnabled = true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error procesando archivo NACHA: {FileName}", FileName);
            throw;
        }

        return failures;
    }

    private async Task ValidateEntrySequencePolicyAsync(
        EntryDetail entry,
        BatchHeader? batch,
        NachaHeader? header,
        Dictionary<int, int> lastConsecutiveByBatch,
        HashSet<string> seenSequenceNumbers,
        CancellationToken ct)
    {
        if (batch is null)
        {
            throw new InvalidOperationException("Error Fatal ID 7: no se encontró lote (registro tipo 5) para validar el Número de Secuencia del registro tipo 6.");
        }

        var rawSequence = (entry.SequenceNumber ?? string.Empty).Trim();
        if (rawSequence.Length != 15 || rawSequence.Any(c => !char.IsDigit(c)))
        {
            throw new InvalidOperationException("Error Fatal ID 7: el Número de Secuencia del registro tipo 6 (posiciones 88-102) debe contener exactamente 15 dígitos numéricos.");
        }

        if (!seenSequenceNumbers.Add(rawSequence))
        {
            throw new InvalidOperationException($"Error Fatal ID 7: número de secuencia duplicado en el archivo ({rawSequence}).");
        }

        var originSegment = rawSequence[..8];
        var consecutiveSegment = rawSequence[8..];

        var batchOrigin = (batch.OriginParticipantEntityCode ?? string.Empty).Trim();
        if (batchOrigin.Length != 8 || batchOrigin.Any(c => !char.IsDigit(c)))
        {
            throw new InvalidOperationException("Error Fatal ID 7: el código de entidad originadora del registro tipo 5 debe ser numérico de 8 dígitos para validar secuencia.");
        }

        if (!string.Equals(originSegment, batchOrigin, StringComparison.Ordinal))
        {
            throw new InvalidOperationException($"Error Fatal ID 7: el segmento de entidad del Número de Secuencia ({originSegment}) no coincide con la entidad originadora del lote ({batchOrigin}).");
        }

        var consecutiveValue = int.Parse(consecutiveSegment);
        if (consecutiveValue < 1)
        {
            throw new InvalidOperationException("Error Fatal ID 7: el segmento consecutivo del Número de Secuencia debe iniciar en 0000001 o superior.");
        }

        if (consecutiveValue > 6_999_999)
        {
            throw new InvalidOperationException("Error Fatal ID 7: el segmento consecutivo del Número de Secuencia excede 6999999. El rango 7000001-9999999 está reservado para PSE.");
        }

        if (lastConsecutiveByBatch.TryGetValue(batch.BatchNumber, out var previousConsecutive) && consecutiveValue <= previousConsecutive)
        {
            throw new InvalidOperationException($"Error Fatal ID 7: la secuencia del lote no es ascendente. Anterior={previousConsecutive:0000000}, actual={consecutiveValue:0000000}.");
        }

        lastConsecutiveByBatch[batch.BatchNumber] = consecutiveValue;

        var processingDate = ParseNachaProcessingDate(header?.FileCreationDate) ?? DateTime.Today;
        var existsInPreviousCycle = await _context.AchTransactions
            .AsNoTracking()
            .AnyAsync(t => t.EffectiveEntryDate.Date == processingDate
                           && t.TraceNumber == rawSequence, ct);

        if (existsInPreviousCycle)
        {
            throw new InvalidOperationException($"Error Fatal ID 7: número de secuencia duplicado para la fecha de proceso {processingDate:yyyy-MM-dd} ({rawSequence}).");
        }
    }

    private static void ValidateBatchSequenceAndControlConsistency(IEnumerable<BatchHeader> batchHeaders, IEnumerable<BatchControl> batchControls)
    {
        var headers = batchHeaders.ToList();
        var controls = batchControls.ToList();

        if (headers.Count == 0)
        {
            return;
        }

        if (headers.Count != controls.Count)
        {
            throw new InvalidOperationException("Error Fatal D18: la cantidad de registros tipo 5 no coincide con los registros tipo 8 del archivo.");
        }

        for (int index = 0; index < headers.Count; index++)
        {
            var expectedBatchNumber = index + 1;
            var header = headers[index];
            var control = controls[index];

            if (header.BatchNumber != expectedBatchNumber)
            {
                throw new InvalidOperationException($"Error Fatal ID 5: el Número de Lote del registro tipo 5 debe iniciar en 0000001 y ser secuencial ascendente. Se esperaba {expectedBatchNumber:0000000} y se recibió {header.BatchNumber:0000000}.");
            }

            if (!int.TryParse(control.BatchNumber?.Trim(), out var controlBatchNumber))
            {
                throw new InvalidOperationException("Error Fatal D18: el Número de Lote del registro tipo 8 debe ser numérico en posiciones 100-106.");
            }

            if (controlBatchNumber != expectedBatchNumber || controlBatchNumber != header.BatchNumber)
            {
                throw new InvalidOperationException($"Error Fatal D18: inconsistencia en Número de Lote entre registros tipo 5 y tipo 8. Se esperaba {expectedBatchNumber:0000000}, tipo 5={header.BatchNumber:0000000}, tipo 8={controlBatchNumber:0000000}.");
            }
        }
    }

    private List<NachaHeader> ParseFileHeaderLinq(List<string> line, Dictionary<string, int> clearingHouseMap, string FileName)
    {
        int cycleNumber = ExtractCycleNumberFromFileName(FileName);

        return line.Select(a =>
        {
            string immediateOrigin = a.Substring(13, 10).Trim();
            int? clearingHouseId = clearingHouseMap.TryGetValue(immediateOrigin, out var chId) ? chId : null;
            string fileCreationDate = a.Substring(23, 8);
            DateTime? processingDate = ParseNachaProcessingDate(fileCreationDate);

            IQueryable<AchCycle> cycleQuery = _context.AchCycles
                .Where(c => c.ClearingHouseId == clearingHouseId);

            if (processingDate.HasValue)
            {
                cycleQuery = cycleQuery.Where(c => c.ProcessingDate == processingDate.Value.Date);
            }

            if (cycleNumber > 0)
            {
                cycleQuery = cycleQuery.Where(c => c.CycleName.Contains(cycleNumber.ToString()));
            }

            string? achCycleId = cycleQuery
                .OrderByDescending(c => c.ProcessingDate)
                .ThenByDescending(c => c.CutoffTime)
                .Select(c => c.Id)
                .FirstOrDefault();

            return new NachaHeader
            {
                NachaID = HashHelper.GenerateHashSha1(
                    $"{a.Substring(3, 10).Trim()}{immediateOrigin}{fileCreationDate.Trim()}{a.Substring(31, 4).Trim()}"),
                PriorityCode = a.Substring(1, 2),
                ImmediateDestination = a.Substring(3, 10).Trim(),
                ImmediateOrigin = immediateOrigin,
                FileCreationDate = fileCreationDate,
                FileCreationTime = a.Substring(31, 4),
                FileIdModifier = a.Substring(35, 1),
                RecordSize = a.Substring(36, 3),
                BlockingFactor = a.Substring(39, 2),
                FormatCode = a.Substring(41, 1),
                ImmediateDestinationName = a.Substring(42, 23).Trim(),
                ImmediateOriginName = a.Substring(65, 23).Trim(),
                ReferenceCode = a.Substring(88, 8).Trim(),
                ClearingHouseId = clearingHouseId,
                CycleNumber = cycleNumber,
                AchCycleId = achCycleId
            };
        }).ToList();
    }

    private static int ExtractCycleNumberFromFileName(string fileName)
    {
        if (string.IsNullOrWhiteSpace(fileName))
        {
            return 0;
        }

        var nameWithoutExtension = Path.GetFileNameWithoutExtension(fileName);
        if (string.IsNullOrWhiteSpace(nameWithoutExtension))
        {
            return 0;
        }

        var segments = nameWithoutExtension.Split('.', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        foreach (var segment in segments)
        {
            if (int.TryParse(segment, out var cycleNumber) && cycleNumber > 0)
            {
                return cycleNumber;
            }
        }

        return 0;
    }

    private static DateTime? ParseNachaProcessingDate(string? fileCreationDate)
    {
        if (string.IsNullOrWhiteSpace(fileCreationDate))
        {
            return null;
        }

        var value = fileCreationDate.Trim();
        if (DateTime.TryParseExact(value, "yyyyMMdd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var yyyyMMdd))
        {
            return yyyyMMdd.Date;
        }

        if (DateTime.TryParseExact(value, "yyMMdd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var yyMMdd))
        {
            return yyMMdd.Date;
        }

        return null;
    }

    private List<BatchHeader> ParseBatchHeaderLinq(List<string> line)
    {
        return line.Select(a =>
        {
            if (a.Length < BatchHeaderType5JulianDateValidator.RecordLength || a[0] != '5')
            {
                throw new InvalidOperationException("El Registro Tipo 5 debe iniciar con '5' y tener longitud fija de 106 caracteres.");
            }

            var rawCompensationDate = a.Substring(BatchHeaderType5JulianDateValidator.JulianDateStartIndex, BatchHeaderType5JulianDateValidator.JulianDateLength);
            var compensationValidation = BatchHeaderType5JulianDateValidator.ValidateAndFormat(rawCompensationDate);
            if (!compensationValidation.IsValid)
            {
                throw new InvalidOperationException(compensationValidation.ErrorMessage ?? "Error Fatal 65: la Fecha de Compensación Juliana contiene caracteres no numéricos.");
            }

            var rawBatchNumber = a.Substring(91, 7);
            if (!rawBatchNumber.All(char.IsDigit))
            {
                throw new InvalidOperationException("Error Fatal ID 5: el Número de Lote del registro tipo 5 (posiciones 92-98) debe ser numérico de 7 dígitos.");
            }

            return new BatchHeader
            {
                ServiceClassCode = a.Substring(1, 3),
                CompanyName = a.Substring(4, 16).Trim(),
                DiscretionaryData = a.Substring(20, 20).Trim(),
                CompanyId = a.Substring(40, 10).Trim(),
                StandardEntryClassCode = a.Substring(50, 3).Trim(),
                CompanyEntryDescription = a.Substring(53, 10).Trim(),
                DescriptiveDate = a.Substring(63, 8).Trim(),
                EffectiveEntryDate = a.Substring(71, 8).Trim(),
                CompensationDate = compensationValidation.FormattedValue.Trim(),
                OriginUserStatusCode = a.Substring(82, 1).Trim(),
                OriginParticipantEntityCode = a.Substring(83, 8).Trim(),
                BatchNumber = int.Parse(rawBatchNumber)
            };
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
            RecipUserName = a.Substring(62, 22),
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
            BusinessType = a.Substring(1, 2).Trim() == "99"
                ? "Return"
                : ParseBusinessTypeFromType05(a),
            CollectorId = a.Substring(1, 2).Trim() == "05" ? a.Substring(3, 13).Trim() : null,
            ReceiverCustomerCode = a.Substring(1, 2).Trim() == "05" ? a.Substring(16, 30).Trim() : null,
            ServiceDescription = a.Substring(1, 2).Trim() == "05" ? a.Substring(46, 15).Trim() : null,
            IdUserOrig = a.Substring(1, 2).Trim() == "99" ? a.Substring(3, 3).Trim() : a.Substring(3, 13).Trim(),
            PurposeOfTransaction = a.Substring(1, 2).Trim() == "99" ? null : a.Substring(20, 10).Trim(),
            InvoiceOrAccountNumber = a.Substring(1, 2).Trim() == "99" ? a.Substring(6, 15).Trim() : a.Substring(30, 53).Trim(),
            InfofromOriginator = a.Substring(1, 2).Trim() == "99" ? a.Substring(81, 15).Trim() : null,
            ReturnReasonCode = a.Substring(1, 2).Trim() == "99" ? a.Substring(3, 3).Trim() : null,
            OriginalTraceNumber = a.Substring(1, 2).Trim() == "99" ? a.Substring(6, 15).Trim() : null,
            NewTraceNumber = a.Substring(1, 2).Trim() == "99" ? a.Substring(81, 15).Trim() : null,
            AddendumSequence = a.Substring(1, 2).Trim() == "99" ? null : a.Substring(83, 4).Trim(),
            EntryDetailSequenceNumber = a.Substring(1, 2).Trim() == "99" ? a.Substring(99, 7).Trim() : a.Substring(87, 7).Trim()
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

    private static string ParseBusinessTypeFromType05(string addendaLine)
    {
        var collectorId = addendaLine.Substring(3, 13).Trim();
        var receiverCustomerCode = addendaLine.Substring(16, 30).Trim();
        var serviceDescription = addendaLine.Substring(46, 15).Trim();

        return !string.IsNullOrWhiteSpace(collectorId)
               && !string.IsNullOrWhiteSpace(receiverCustomerCode)
               && !string.IsNullOrWhiteSpace(serviceDescription)
            ? "Debit"
            : "Credit";
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

    private static readonly HashSet<string> ReturnCodes = new(StringComparer.OrdinalIgnoreCase)
    {
        "21", "26", "31", "36", "51", "56"
    };

    private async Task<(bool IsValid, string? FailureReason)> ValidateEntryAsync(
        EntryDetail entry,
        BatchHeader? batch,
        List<NachaValidationFailure> failures,
        CancellationToken ct)
    {
        var code = entry.TransactionCode ?? string.Empty;
        var configuredCodes = await GetConfiguredTransactionCodesAsync(ct);
        if (!configuredCodes.Contains(code))
        {
            const string reason = "Código de transacción inválido.";
            failures.Add(new NachaValidationFailure("6", batch?.BatchNumber.ToString(), entry.SequenceNumber, code, reason));
            return (false, reason);
        }

        var isCredit = CreditCodes.Contains(code);
        var isDebit = DebitCodes.Contains(code);
        var serviceClassCode = batch?.ServiceClassCode?.Trim();
        var isPseCcdCredit = isCredit && IsPseCcdBatch(batch);

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

        var checkDigitValidationReason = await ValidateType6CheckDigitAsync(entry, ct);
        if (checkDigitValidationReason is not null)
        {
            failures.Add(new NachaValidationFailure("6", batch?.BatchNumber.ToString(), entry.SequenceNumber, code, checkDigitValidationReason));
            return (false, checkDigitValidationReason);
        }

        var receiverNameValidationReason = ValidateType6ReceiverName(entry);
        if (receiverNameValidationReason is not null)
        {
            failures.Add(new NachaValidationFailure("6", batch?.BatchNumber.ToString(), entry.SequenceNumber, code, receiverNameValidationReason));
            return (false, receiverNameValidationReason);
        }

        var requiresIdentityValidation = isDebit || ShouldValidateCreditIdentity(entry.DiscreData);

        if (isPseCcdCredit)
        {
            var accountNumber = entry.AccountNumber ?? string.Empty;
            var accountExists = await _context.CustomerAccounts
                .AsNoTracking()
                .AnyAsync(a => a.AccountNumber == accountNumber, ct);

            if (!accountExists)
            {
                const string reason = "R03: Cuenta de destino no existe en la entidad receptora.";
                failures.Add(new NachaValidationFailure("6", batch?.BatchNumber.ToString(), entry.SequenceNumber, code, reason));
                return (false, reason);
            }

            // Excepción de negocio para CCD/PSE como banco receptor:
            // si la cuenta existe, no se aplican validaciones adicionales de límites.
            return (true, null);
        }

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
                .AnyAsync(c => c.DocumentNumber == recipientId && c.Accounts.Any(a => a.AccountNumber == accountNumber), ct);

            if (!matches)
            {
                const string reason = "R17: La identificación no coincide con cuenta del usuario receptor.";
                failures.Add(new NachaValidationFailure("6", batch?.BatchNumber.ToString(), entry.SequenceNumber, code, reason));
                return (false, reason);
            }
        }

        return (true, null);
    }

    private static string? ValidateType6ReceiverName(EntryDetail entry)
    {
        return NachaReceiverNameHelper.ValidateType6RawField(entry.RecipUserName);
    }

    private async Task<string?> ValidateType6CheckDigitAsync(EntryDetail entry, CancellationToken ct)
    {
        var receivingCode = (entry.ReceivingParticipantEntityCode ?? string.Empty).Trim();
        if (receivingCode.Length != 8 || receivingCode.Any(c => !char.IsDigit(c)))
        {
            return "Error Fatal ID 35: el Código Entidad Participante Receptor (posiciones 4-11) debe contener 8 dígitos numéricos.";
        }

        var fileCheckDigit = (entry.CheckDigit ?? string.Empty).Trim();
        if (fileCheckDigit.Length != 1 || !char.IsDigit(fileCheckDigit[0]))
        {
            return "Error Fatal ID 35: el Dígito de Chequeo (posición 12) debe ser numérico de longitud 1.";
        }

        var expectedCheckDigit = DigitoChequeoHelper.CalcularDigitoChequeo(receivingCode);
        if (!string.Equals(fileCheckDigit, expectedCheckDigit, StringComparison.Ordinal))
        {
            return $"Error Fatal ID 35: el Dígito de Chequeo (posición 12) no corresponde al Código Entidad Participante Receptor (posiciones 4-11). Valor archivo={fileCheckDigit}, calculado={expectedCheckDigit}.";
        }

        var institution = await _context.FinancialInstitutions
            .AsNoTracking()
            .FirstOrDefaultAsync(fi => (fi.RoutingNumber + fi.TransitCode) == receivingCode, ct);

        if (institution is not null)
        {
            var dbCheckDigit = (institution.CheckDigit ?? string.Empty).Trim();
            if (dbCheckDigit.Length != 1 || !char.IsDigit(dbCheckDigit[0]))
            {
                return $"Error Fatal ID 35: el dígito de chequeo almacenado en FinancialInstitutions para el código {receivingCode} no es válido.";
            }

            if (!string.Equals(dbCheckDigit, expectedCheckDigit, StringComparison.Ordinal))
            {
                return $"Error Fatal ID 35: inconsistencia en FinancialInstitutions.CheckDigit para el código {receivingCode}. Base de datos={dbCheckDigit}, calculado={expectedCheckDigit}.";
            }
        }

        return null;
    }

    private static bool IsPseCcdBatch(BatchHeader? batch)
    {
        if (batch is null)
        {
            return false;
        }

        if (!string.Equals(batch.StandardEntryClassCode?.Trim(), "CCD", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        var pseHints = new[]
        {
            batch.CompanyEntryDescription,
            batch.CompanyName,
            batch.DiscretionaryData,
            batch.CompanyId
        };

        return pseHints.Any(value =>
            !string.IsNullOrWhiteSpace(value) &&
            value.Contains("PSE", StringComparison.OrdinalIgnoreCase));
    }

    private async Task UpdateThirdPartyStatusAsync(
        EntryDetail entry,
        string? validationCycleId,
        bool isValid,
        string? failureReason,
        CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(entry.AccountNumber) || string.IsNullOrWhiteSpace(entry.RecipIdNumber))
        {
            return;
        }

        var thirdParty = await _context.CustomerThirdParties
            .FirstOrDefaultAsync(t =>
                t.DestinationAccountNumber == entry.AccountNumber &&
                t.RecipientIdNumber == entry.RecipIdNumber &&
                t.Status == CustomerThirdPartyStatusEnum.Pending, ct);

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
            var relatedAddendas = addendaRecords
                .Where(addenda => IsAddendaForEntry(entry, addenda))
                .ToList();

            if (!relatedAddendas.Any())
            {
                failures.Add(new NachaValidationFailure("6", null, entry.SequenceNumber, entry.TransactionCode,
                    "No se encontró registro 7 asociado al detalle."));
                continue;
            }

            if (ReturnCodes.Contains(entry.TransactionCode ?? string.Empty))
            {
                var hasReturnAddenda = relatedAddendas.Any(addenda =>
                    string.Equals(addenda.CodeTypeAddendumRecord?.Trim(), "99", StringComparison.OrdinalIgnoreCase));

                if (!hasReturnAddenda)
                {
                    failures.Add(new NachaValidationFailure("7", null, entry.SequenceNumber, entry.TransactionCode,
                        "Las devoluciones (21/26/31/36/51/56) deben incluir adenda tipo 99."));
                    continue;
                }

                var hasReturnReason = relatedAddendas.Any(addenda => HasReturnReason(addenda));
                if (!hasReturnReason)
                {
                    failures.Add(new NachaValidationFailure("7", null, entry.SequenceNumber, entry.TransactionCode,
                        "La adenda de devolución debe incluir causal (Rxx o DEV14)."));
                    continue;
                }

                var hasOriginalTraceReference = relatedAddendas.Any(addenda =>
                    !string.IsNullOrWhiteSpace(addenda.OriginalTraceNumber) &&
                    addenda.OriginalTraceNumber.Trim().Length == 15);

                if (!hasOriginalTraceReference)
                {
                    failures.Add(new NachaValidationFailure("7", null, entry.SequenceNumber, entry.TransactionCode,
                        "La devolución debe incluir referencia de secuencia original en la adenda."));
                    continue;
                }
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

    private static bool HasReturnReason(AddendaRecord addenda)
    {
        return !string.IsNullOrWhiteSpace(addenda.ReturnReasonCode)
               && Regex.IsMatch(addenda.ReturnReasonCode.Trim(), @"^R\d{2}$|^DEV14$", RegexOptions.IgnoreCase);
    }

    private static string? GetEntrySequenceSuffix(string? sequence)
    {
        if (string.IsNullOrWhiteSpace(sequence))
        {
            return null;
        }

        return sequence.Length <= 7 ? sequence : sequence[^7..];
    }


    private async Task ApplyReturnStateTransitionsAsync(
        IReadOnlyList<EntryDetail> validEntries,
        IReadOnlyList<AddendaRecord> validAddendas,
        IReadOnlyList<NachaValidationFailure> failures,
        CancellationToken ct)
    {
        await ApplyReturnedByEprTransitionsAsync(validEntries, validAddendas, ct);
        await ApplyReturnedByOperatorTransitionsAsync(failures, ct);
    }

    private async Task ApplyReturnedByEprTransitionsAsync(
        IReadOnlyList<EntryDetail> validEntries,
        IReadOnlyList<AddendaRecord> validAddendas,
        CancellationToken ct)
    {
        var processedTransactionIds = new HashSet<int>();

        foreach (var entry in validEntries.Where(e => ReturnCodes.Contains(e.TransactionCode ?? string.Empty)))
        {
            var relatedAddenda = validAddendas
                .Where(addenda => IsAddendaForEntry(entry, addenda)
                    && string.Equals(addenda.CodeTypeAddendumRecord?.Trim(), "99", StringComparison.OrdinalIgnoreCase))
                .ToList();

            var reasonCode = ExtractReturnReasonCode(relatedAddenda);
            var originalTraceRef = ResolveOriginalTraceReference(entry, relatedAddenda);

            if (string.IsNullOrWhiteSpace(reasonCode) || string.IsNullOrWhiteSpace(originalTraceRef))
            {
                continue;
            }

            var transaction = await FindTransactionByTraceReferenceAsync(originalTraceRef, ct);
            if (transaction is null || !processedTransactionIds.Add(transaction.Id))
            {
                continue;
            }

            try
            {
                await _stateTransitionService.TransitionAsync(
                    transaction.Id,
                    AchTransferStateEnum.ReturnedByEpr,
                    AchStateEventSourceEnum.Epr,
                    reasonCode: reasonCode,
                    payloadJson: BuildParserPayload(entry, relatedAddenda),
                    originalTraceRef: originalTraceRef);
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogWarning(ex,
                    "No se pudo aplicar transición ReturnedByEpr para transacción {TransactionId} (trace {TraceRef}).",
                    transaction.Id,
                    originalTraceRef);
            }
        }
    }

    private async Task ApplyReturnedByOperatorTransitionsAsync(IReadOnlyList<NachaValidationFailure> failures,
        CancellationToken ct)
    {
        var processedTransactionIds = new HashSet<int>();

        foreach (var failure in failures.Where(f => !string.IsNullOrWhiteSpace(f.EntrySequence)))
        {
            var transaction = await FindTransactionByTraceReferenceAsync(failure.EntrySequence, ct);
            if (transaction is null || !processedTransactionIds.Add(transaction.Id))
            {
                continue;
            }

            var reasonCode = ExtractOperatorReasonCode(failure.Reason);

            try
            {
                await _stateTransitionService.TransitionAsync(
                    transaction.Id,
                    AchTransferStateEnum.ReturnedByOperator,
                    AchStateEventSourceEnum.Operator,
                    reasonCode: reasonCode,
                    payloadJson: BuildOperatorFailurePayload(failure));
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogWarning(ex,
                    "No se pudo aplicar transición ReturnedByOperator para transacción {TransactionId} asociada a secuencia {EntrySequence}.",
                    transaction.Id,
                    failure.EntrySequence);
            }
        }
    }

    private async Task<AchTransaction?> FindTransactionByTraceReferenceAsync(string? traceReference, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(traceReference))
        {
            return null;
        }

        var normalized = traceReference.Trim();

        var transaction = await _context.AchTransactions
            .FirstOrDefaultAsync(t => t.TraceNumber == normalized, ct);

        if (transaction is not null)
        {
            return transaction;
        }

        var sequenceSuffix = GetEntrySequenceSuffix(normalized);
        if (!int.TryParse(sequenceSuffix, out var sequenceNumber))
        {
            return null;
        }

        return await _context.AchTransactions
            .FirstOrDefaultAsync(t => t.TraceSequenceNumber == sequenceNumber, ct);
    }

    private static string? ResolveOriginalTraceReference(EntryDetail entry, IReadOnlyList<AddendaRecord> relatedAddenda)
    {
        var originalTrace = relatedAddenda
            .Select(addenda => addenda.OriginalTraceNumber)
            .FirstOrDefault(value => !string.IsNullOrWhiteSpace(value));

        if (!string.IsNullOrWhiteSpace(originalTrace))
        {
            var digits = new string(originalTrace.Where(char.IsDigit).ToArray());
            if (digits.Length == 15)
            {
                return digits;
            }
        }

        return null;
    }

    private static string? ExtractReturnReasonCode(IReadOnlyList<AddendaRecord> relatedAddenda)
    {
        return relatedAddenda
            .Select(addenda => addenda.ReturnReasonCode?.Trim().ToUpperInvariant())
            .FirstOrDefault(code => !string.IsNullOrWhiteSpace(code));
    }

    private static string ExtractOperatorReasonCode(string reason)
    {
        var match = Regex.Match(reason ?? string.Empty, @"\bD\d{2}\b", RegexOptions.IgnoreCase);
        return match.Success ? match.Value.ToUpperInvariant() : "D00";
    }

    private static string BuildOperatorFailurePayload(NachaValidationFailure failure)
    {
        return System.Text.Json.JsonSerializer.Serialize(new
        {
            recordType = failure.RecordType,
            entrySequence = failure.EntrySequence,
            message = failure.Reason
        });
    }

    private static string BuildParserPayload(EntryDetail entry, IReadOnlyList<AddendaRecord> relatedAddenda)
    {
        var addendaCount = relatedAddenda.Count;
        return $"{{\"transactionCode\":\"{entry.TransactionCode}\",\"entrySequence\":\"{entry.SequenceNumber}\",\"addendaCount\":{addendaCount}}}";
    }

    private async Task<HashSet<string>> GetConfiguredTransactionCodesAsync(CancellationToken ct)
    {
        if (_configuredTransactionCodes is not null)
        {
            return _configuredTransactionCodes;
        }

        var configuredCodes = await _context.TransactionCodes
            .AsNoTracking()
            .Select(x => x.Code)
            .ToListAsync(ct);

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

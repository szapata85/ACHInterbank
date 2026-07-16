using Cfa.ACHInterbank.Application.DataBase;
using Cfa.ACHInterbank.Application.ACH.Interfaces;
using Cfa.ACHInterbank.Domain.Entities.Transactions.Enums;
using Cfa.ACHInterbank.Domain.Models.ACH;
using Cfa.ACHInterbank.Domain.Models.Configurations;
using Cfa.ACHInterbank.Persistence.DataBase;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;

namespace Cfa.ACHInterbank.Persistence.ACH.Services.Implementation.Seeders;

[Scoped]
public sealed class BulkTransactionScenarioSeeder : IDbSeeder
{
    private const string SeedReferencePrefix = "SEED-BULK-";
    private const int Type7PurposeLength = 10;

    private readonly AchDbContext _context;
    private readonly IHostEnvironment _environment;
    private readonly IBankHoliday? _holidayService;

    public BulkTransactionScenarioSeeder(
        AchDbContext context,
        IHostEnvironment environment,
        IBankHoliday? holidayService = null)
    {
        _context = context;
        _environment = environment;
        _holidayService = holidayService;
    }

    public int Order => 6;

    public async Task SeedAsync()
    {
        if (!_environment.IsDevelopment() && !_environment.IsEnvironment("Testing"))
        {
            return;
        }

        var alreadySeeded = await _context.AchTransactions
            .AsNoTracking()
            .AnyAsync(t => t.Reference.StartsWith(SeedReferencePrefix));

        if (alreadySeeded)
        {
            await NormalizeSeedRoutingNumbersAsync();
            await EnsureDebitPrenotificationPrerequisitesAsync();
            return;
        }

        var cycle = await _context.AchCycles
            .AsNoTracking()
            .OrderByDescending(c => c.ProcessingDate)
            .FirstOrDefaultAsync();

        if (cycle is null)
        {
            return;
        }

        var sourceInstitution = await _context.FinancialInstitutions
            .AsNoTracking()
            .FirstOrDefaultAsync(fi => fi.IsDefaultSource && fi.Status == FinancialInstitutionStatus.Active)
            ?? await _context.FinancialInstitutions.AsNoTracking().FirstOrDefaultAsync(fi => fi.Status == FinancialInstitutionStatus.Active);

        var destinationInstitutions = await _context.FinancialInstitutions
            .AsNoTracking()
            .Where(fi => fi.Status == FinancialInstitutionStatus.Active)
            .OrderBy(fi => fi.Id)
            .Take(4)
            .ToListAsync();

        var companyEntryDescription = await _context.CompanyEntryDescriptionCatalogs
            .AsNoTracking()
            .Where(c => c.IsActive && c.Term.StartsWith("MULTICREDIT"))
            .OrderBy(c => c.Id)
            .Select(c => new { c.Id, c.Term })
            .FirstOrDefaultAsync();

        if (sourceInstitution is null || destinationInstitutions.Count == 0 || companyEntryDescription is null)
        {
            return;
        }

        var batches = new List<AchBatch>();
        var transactions = new List<AchTransaction>();
        var addendas = new List<AchTransactionAddenda>();

        int nextBatchSequence = await _context.AchBatches
            .Where(b => b.AchCycleId == cycle.Id)
            .Select(b => (int?)b.BatchSequenceNumber)
            .MaxAsync() ?? 0;

        int nextTrace = await _context.AchTransactions
            .Where(t => t.EffectiveEntryDate.Date == cycle.ProcessingDate.Date)
            .Select(t => (int?)t.TraceSequenceNumber)
            .MaxAsync() ?? 0;

        BuildScenario(
            label: "VALID",
            itemCount: 24,
            includeMixedTypes: false,
            seedExistingCollisionReferences: false,
            cycle,
            sourceInstitution,
            destinationInstitutions,
            companyEntryDescription.Id,
            companyEntryDescription.Term,
            ref nextBatchSequence,
            ref nextTrace,
            batches,
            transactions,
            addendas);

        BuildScenario(
            label: "MIXED",
            itemCount: 30,
            includeMixedTypes: true,
            seedExistingCollisionReferences: false,
            cycle,
            sourceInstitution,
            destinationInstitutions,
            companyEntryDescription.Id,
            companyEntryDescription.Term,
            ref nextBatchSequence,
            ref nextTrace,
            batches,
            transactions,
            addendas);

        BuildScenario(
            label: "PARTIAL-ANCHOR",
            itemCount: 6,
            includeMixedTypes: true,
            seedExistingCollisionReferences: true,
            cycle,
            sourceInstitution,
            destinationInstitutions,
            companyEntryDescription.Id,
            companyEntryDescription.Term,
            ref nextBatchSequence,
            ref nextTrace,
            batches,
            transactions,
            addendas);

        BuildScenario(
            label: "VOLUME",
            itemCount: 180,
            includeMixedTypes: true,
            seedExistingCollisionReferences: false,
            cycle,
            sourceInstitution,
            destinationInstitutions,
            companyEntryDescription.Id,
            companyEntryDescription.Term,
            ref nextBatchSequence,
            ref nextTrace,
            batches,
            transactions,
            addendas);

        _context.AchBatches.AddRange(batches);
        _context.AchTransactions.AddRange(transactions);
        _context.AchTransactionAddendas.AddRange(addendas);

        await _context.SaveChangesAsync();
        await NormalizeSeedRoutingNumbersAsync();
        await EnsureDebitPrenotificationPrerequisitesAsync();
    }

    private async Task NormalizeSeedRoutingNumbersAsync()
    {
        var massCreditDescription = await _context.CompanyEntryDescriptionCatalogs
            .AsNoTracking()
            .Where(x => x.IsActive && x.Term.StartsWith("MULTICREDIT"))
            .OrderBy(x => x.Id)
            .Select(x => new { x.Id, x.Term })
            .FirstOrDefaultAsync()
            ?? throw new InvalidOperationException("El catálogo activo no contiene el concepto MULTICREDIT requerido para los escenarios masivos.");

        var transactions = await _context.AchTransactions
            .Include(x => x.SourceInstitution)
            .Include(x => x.DestinationInstitution)
            .Include(x => x.AchBatch)
            .Include(x => x.Addendas)
            .Where(x => x.Reference.StartsWith(SeedReferencePrefix))
            .ToListAsync();

        foreach (var transaction in transactions)
        {
            if (transaction.Type == TransactionTypeEnum.Reversal)
            {
                transaction.Type = TransactionTypeEnum.Debit;
                transaction.OriginalTraceRef = string.Empty;
            }

            transaction.CompanyEntryDescriptionId = massCreditDescription.Id;
            transaction.AchBatch.CompanyEntryDescriptionId = massCreditDescription.Id;
            transaction.AchBatch.CompanyEntryDescription = massCreditDescription.Term;

            var sourceWithCheckDigit = BuildRoutingWithCheckDigit(transaction.SourceInstitution);
            if (string.Equals(transaction.OriginatingDFI, sourceWithCheckDigit, StringComparison.Ordinal))
            {
                transaction.OriginatingDFI = BuildBaseRouting(transaction.SourceInstitution);
            }

            var destinationWithCheckDigit = BuildRoutingWithCheckDigit(transaction.DestinationInstitution);
            if (string.Equals(transaction.ReceivingDFI, destinationWithCheckDigit, StringComparison.Ordinal))
            {
                transaction.ReceivingDFI = BuildBaseRouting(transaction.DestinationInstitution);
            }

            foreach (var addenda in transaction.Addendas.Where(x => !string.IsNullOrWhiteSpace(x.Purpose)))
            {
                addenda.Purpose = NormalizeType7Purpose(addenda.Purpose!);
            }

            foreach (var addenda in transaction.Addendas.Where(x => x.BusinessType == AchAddendaBusinessType.Credit))
            {
                addenda.Purpose = NormalizeType7Purpose(massCreditDescription.Term);
            }

            foreach (var addenda in transaction.Addendas.Where(x => x.BusinessType == AchAddendaBusinessType.Debit))
            {
                addenda.CollectorId = transaction.CompanyIdentification;
                addenda.ReceiverCustomerCode = transaction.RecipientIdNumber;
                addenda.ServiceDescription = massCreditDescription.Term;
            }
        }

        await _context.SaveChangesAsync();
    }

    private async Task EnsureDebitPrenotificationPrerequisitesAsync()
    {
        var debits = await _context.AchTransactions
            .AsNoTracking()
            .Where(t => t.Reference.StartsWith(SeedReferencePrefix)
                        && t.Type == TransactionTypeEnum.Debit
                        && !t.IsPrenotification)
            .OrderBy(t => t.Id)
            .ToListAsync();

        if (debits.Count == 0)
        {
            return;
        }

        var existingOriginalTraces = await _context.AchTransactions
            .AsNoTracking()
            .Where(t => t.Reference.StartsWith($"{SeedReferencePrefix}PRE-")
                        && t.IsPrenotification
                        && t.OriginalTraceRef != string.Empty)
            .Select(t => t.OriginalTraceRef)
            .ToHashSetAsync();

        var nextTrace = await _context.AchTransactions
            .Select(t => (int?)t.TraceSequenceNumber)
            .MaxAsync() ?? 0;

        foreach (var debit in debits.Where(x => !existingOriginalTraces.Contains(x.TraceNumber)))
        {
            var effectiveDate = SubtractBusinessDays(debit.EffectiveEntryDate.Date, 3);
            var traceSequence = ++nextTrace;
            var routingPrefix = (debit.OriginatingDFI ?? string.Empty).Trim();
            var routing8 = routingPrefix.Length >= 8 ? routingPrefix[..8] : routingPrefix.PadLeft(8, '0');
            var reference = $"{SeedReferencePrefix}PRE-{debit.Id}";
            var timestamp = new DateTimeOffset(effectiveDate, TimeSpan.Zero);

            var prenotification = new AchTransaction
            {
                AchBatchId = debit.AchBatchId,
                AchCycleId = debit.AchCycleId,
                Amount = 0m,
                TransactionExternalId = reference,
                Reference = reference,
                Type = TransactionTypeEnum.Prenotification,
                IsPrenotification = true,
                TransactionCode = debit.TransactionCode switch
                {
                    "27" => "28",
                    "37" => "38",
                    "55" => "57",
                    _ => throw new InvalidOperationException($"Código débito semilla no soportado para prenotificación: {debit.TransactionCode}.")
                },
                ServiceClassCode = debit.ServiceClassCode,
                CompanyEntryDescriptionId = debit.CompanyEntryDescriptionId,
                CompanyName = debit.CompanyName,
                CompanyIdentification = debit.CompanyIdentification,
                OriginatingDFI = debit.OriginatingDFI ?? string.Empty,
                ReceivingDFI = debit.ReceivingDFI ?? string.Empty,
                TraceSequenceNumber = traceSequence,
                TraceNumber = $"{routing8}{traceSequence:0000000}",
                EffectiveEntryDate = effectiveDate,
                AddendaRecordIndicator = true,
                State = AchTransferStateEnum.AppliedTacitly,
                StateChangedAtUtc = effectiveDate,
                SourceAccountNumber = debit.SourceAccountNumber,
                DestinationAccountNumber = debit.DestinationAccountNumber,
                SourceInstitutionId = debit.SourceInstitutionId,
                DestinationInstitutionId = debit.DestinationInstitutionId,
                RecipientIdNumber = debit.RecipientIdNumber,
                DiscretionaryData = debit.DiscretionaryData,
                OriginalTraceRef = debit.TraceNumber,
                CreatedAt = timestamp,
                UpdatedAt = timestamp
            };

            prenotification.Addendas.Add(new AchTransactionAddenda
            {
                AddendaType = "05",
                BusinessType = AchAddendaBusinessType.Debit,
                Information = $"PRENOTIFICACION {debit.Reference}",
                CollectorId = debit.CompanyIdentification,
                ReceiverCustomerCode = debit.RecipientIdNumber,
                ServiceDescription = "PRENOTIF",
                SequenceNumber = 1,
                CreatedAt = timestamp,
                UpdatedAt = timestamp
            });
            prenotification.StateEvents.Add(new AchTransactionStateEvent
            {
                FromState = AchTransferStateEnum.Pending,
                ToState = AchTransferStateEnum.AppliedTacitly,
                Source = AchStateEventSourceEnum.System,
                ReasonCode = "SEED_PREREQUISITE",
                CreatedAt = timestamp,
                UpdatedAt = timestamp
            });

            _context.AchTransactions.Add(prenotification);
        }

        await _context.SaveChangesAsync();
    }

    private DateTime SubtractBusinessDays(DateTime endDate, int days)
    {
        var date = endDate.Date;
        var remaining = days;

        while (remaining > 0)
        {
            date = date.AddDays(-1);
            var holidays = _holidayService?.GetHolidays(date.Year)
                .Select(x => x.Date)
                .ToHashSet() ?? [];
            if (date.DayOfWeek is not (DayOfWeek.Saturday or DayOfWeek.Sunday)
                && !holidays.Contains(DateOnly.FromDateTime(date)))
            {
                remaining--;
            }
        }

        return date;
    }

    private static void BuildScenario(
        string label,
        int itemCount,
        bool includeMixedTypes,
        bool seedExistingCollisionReferences,
        AchCycle cycle,
        FinancialInstitution sourceInstitution,
        IReadOnlyList<FinancialInstitution> destinations,
        int companyEntryDescriptionId,
        string companyEntryDescriptionTerm,
        ref int nextBatchSequence,
        ref int nextTrace,
        ICollection<AchBatch> batches,
        ICollection<AchTransaction> transactions,
        ICollection<AchTransactionAddenda> addendas)
    {
        var batch = new AchBatch
        {
            AchCycleId = cycle.Id,
            EffectiveEntryDate = cycle.ProcessingDate.Date,
            BatchSequenceNumber = ++nextBatchSequence,
            CompanyName = "EMPRESA SEMILLA",
            CompanyIdentification = "900123456",
            CompanyEntryDescriptionId = companyEntryDescriptionId,
            CompanyEntryDescription = companyEntryDescriptionTerm,
            OriginOrOdfi = BuildBaseRouting(sourceInstitution),
            ServiceClassCode = "200"
        };

        batches.Add(batch);

        decimal totalCredit = 0m;
        decimal totalDebit = 0m;

        for (var index = 1; index <= itemCount; index++)
        {
            var destination = destinations[(index - 1) % destinations.Count];
            var type = ResolveType(includeMixedTypes, index);
            var isPrenotification = type == TransactionTypeEnum.Prenotification;
            var amount = isPrenotification ? 0m : 100_000m + (index * 1_250m);

            var reference = seedExistingCollisionReferences && index <= 2
                ? $"SEED-BULK-PARTIAL-EXIST-{index:000}"
                : $"SEED-BULK-{label}-{index:000}";

            var transactionCode = ResolveTransactionCode(type, index);
            var traceNumber = BuildTraceNumber(sourceInstitution, ++nextTrace);

            var transaction = new AchTransaction
            {
                AchBatch = batch,
                AchCycleId = cycle.Id,
                Amount = amount,
                Reference = reference,
                Type = type,
                IsPrenotification = isPrenotification,
                TransactionCode = transactionCode,
                ServiceClassCode = "200",
                CompanyEntryDescriptionId = companyEntryDescriptionId,
                CompanyName = "EMPRESA SEMILLA",
                CompanyIdentification = "900123456",
                OriginatingDFI = BuildBaseRouting(sourceInstitution),
                ReceivingDFI = BuildBaseRouting(destination),
                TraceSequenceNumber = nextTrace,
                TraceNumber = traceNumber,
                EffectiveEntryDate = cycle.ProcessingDate.Date,
                AddendaRecordIndicator = true,
                State = AchTransferStateEnum.Pending,
                StateChangedAtUtc = DateTime.UtcNow,
                SourceAccountNumber = $"122000{index:000000}",
                DestinationAccountNumber = $"411000{index:000000}",
                SourceInstitutionId = sourceInstitution.Id,
                DestinationInstitutionId = destination.Id,
                RecipientIdNumber = $"10{index:00000000}",
                DiscretionaryData = type == TransactionTypeEnum.Credit && index % 3 == 0 ? "V" : string.Empty
            };

            transactions.Add(transaction);

            addendas.Add(new AchTransactionAddenda
            {
                Transaction = transaction,
                AddendaType = "05",
                BusinessType = type is TransactionTypeEnum.Debit or TransactionTypeEnum.Return or TransactionTypeEnum.Reversal
                    ? AchAddendaBusinessType.Debit
                    : AchAddendaBusinessType.Credit,
                Information = $"SEMILLA {label} ITEM {index:000}",
                Purpose = NormalizeType7Purpose(companyEntryDescriptionTerm),
                Reference = $"REF{index:00000000000000000000000000000000000000000000000000}"[..53],
                CollectorId = type is TransactionTypeEnum.Debit or TransactionTypeEnum.Return or TransactionTypeEnum.Reversal
                    ? transaction.CompanyIdentification
                    : null,
                ReceiverCustomerCode = type is TransactionTypeEnum.Debit or TransactionTypeEnum.Return or TransactionTypeEnum.Reversal
                    ? transaction.RecipientIdNumber
                    : null,
                ServiceDescription = type is TransactionTypeEnum.Debit or TransactionTypeEnum.Return or TransactionTypeEnum.Reversal
                    ? companyEntryDescriptionTerm
                    : null,
                SequenceNumber = 1
            });

            if (type is TransactionTypeEnum.Debit or TransactionTypeEnum.Return or TransactionTypeEnum.Reversal)
            {
                totalDebit += amount;
            }
            else
            {
                totalCredit += amount;
            }
        }

        batch.TotalCreditAmount = totalCredit;
        batch.TotalDebitAmount = totalDebit;
        batch.ServiceClassCode = totalCredit > 0m && totalDebit > 0m ? "200" : totalDebit > 0m ? "225" : "220";
    }

    private static string NormalizeType7Purpose(string value)
    {
        var normalized = value.Trim();
        return normalized.Length <= Type7PurposeLength
            ? normalized
            : normalized[..Type7PurposeLength];
    }

    private static TransactionTypeEnum ResolveType(bool includeMixedTypes, int index)
    {
        if (!includeMixedTypes)
        {
            return index % 6 == 0 ? TransactionTypeEnum.Prenotification : TransactionTypeEnum.Credit;
        }

        return (index % 5) switch
        {
            0 => TransactionTypeEnum.Credit,
            1 => TransactionTypeEnum.Debit,
            2 => TransactionTypeEnum.Prenotification,
            3 => TransactionTypeEnum.Debit,
            _ => TransactionTypeEnum.Credit
        };
    }

    private static string ResolveTransactionCode(TransactionTypeEnum type, int index)
    {
        return type switch
        {
            TransactionTypeEnum.Debit => index % 2 == 0 ? "27" : "37",
            TransactionTypeEnum.Prenotification => "23",
            TransactionTypeEnum.Reversal => "27",
            TransactionTypeEnum.Return => "27",
            _ => index % 2 == 0 ? "22" : "32"
        };
    }

    private static string BuildBaseRouting(FinancialInstitution institution)
    {
        return $"{institution.RoutingNumber?.Trim()}{institution.TransitCode?.Trim()}";
    }

    private static string BuildRoutingWithCheckDigit(FinancialInstitution institution)
    {
        var baseRouting = BuildBaseRouting(institution);
        var checkDigit = string.IsNullOrWhiteSpace(institution.CheckDigit) ? "0" : institution.CheckDigit.Trim();
        return $"{baseRouting}{checkDigit}";
    }

    private static string BuildTraceNumber(FinancialInstitution institution, int traceSequence)
    {
        var baseRouting = BuildBaseRouting(institution);
        var routing8 = baseRouting.Length >= 8 ? baseRouting[..8] : baseRouting.PadLeft(8, '0');
        return $"{routing8}{traceSequence:0000000}";
    }
}

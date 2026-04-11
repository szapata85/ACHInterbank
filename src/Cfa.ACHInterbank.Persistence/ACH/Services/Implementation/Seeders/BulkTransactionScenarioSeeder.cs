using Cfa.ACHInterbank.Application.DataBase;
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

    private readonly AchDbContext _context;
    private readonly IHostEnvironment _environment;

    public BulkTransactionScenarioSeeder(AchDbContext context, IHostEnvironment environment)
    {
        _context = context;
        _environment = environment;
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
            .Where(c => c.IsActive)
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
                OriginatingDFI = BuildRoutingWithCheckDigit(sourceInstitution),
                ReceivingDFI = BuildRoutingWithCheckDigit(destination),
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
                Purpose = "NOMINA",
                Reference = $"REF{index:00000000000000000000000000000000000000000000000000}"[..53],
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
            3 => TransactionTypeEnum.Reversal,
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

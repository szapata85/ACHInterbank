using Cfa.ACHInterbank.Application.DataBase;
using Cfa.ACHInterbank.Domain.Models.ACH;
using Cfa.ACHInterbank.Domain.Models.Configurations;
using Cfa.ACHInterbank.Persistence.DataBase;

namespace Cfa.ACHInterbank.Persistence.ACH.Services.Implementation.Seeders;

[Scoped]
public class RegulatoryCatalogSeeder : IDbSeeder
{
    private readonly AchDbContext _context;

    public RegulatoryCatalogSeeder(AchDbContext context)
    {
        _context = context;
    }

    int IDbSeeder.Order => 3;

    public async Task SeedAsync()
    {
        if (!_context.AchReturnCodes.Any())
        {
            _context.AchReturnCodes.AddRange(BuildReturnCodes());
        }

        if (!_context.AchFileRejectionCodes.Any())
        {
            _context.AchFileRejectionCodes.AddRange(BuildFileRejectionCodes());
        }

        if (!_context.AchTransactionTypePolicies.Any())
        {
            _context.AchTransactionTypePolicies.AddRange(BuildTransactionTypePolicies());
        }

        if (!_context.AchReturnPolicies.Any())
        {
            _context.AchReturnPolicies.AddRange(BuildReturnPolicies());
        }

        if (!_context.AchReturnOfReturnPolicies.Any())
        {
            _context.AchReturnOfReturnPolicies.AddRange(BuildReturnOfReturnPolicies());
        }

        if (!_context.AchPrenotificationPolicies.Any())
        {
            _context.AchPrenotificationPolicies.AddRange(BuildPrenotificationPolicies());
        }

        await _context.SaveChangesAsync();
    }

    private static IEnumerable<AchReturnCode> BuildReturnCodes()
    {
        var codes = new[] { "R01", "R02", "R03", "R04", "R06", "R07", "R08", "R09", "R10", "R12", "R13", "R14", "R15", "R16", "R17", "R20", "R23", "R29", "R31" };
        return codes.Select(code => new AchReturnCode
        {
            Code = code,
            Description = $"Causal regulatoria {code}",
            AppliesToDebit = true,
            AppliesToCredit = true,
            AppliesToPrenotification = code is "R03" or "R29",
            AppliesToReturn = true,
            RequiresAddenda = true,
            MaxDaysAllowed = code is "R31" ? 15 : 1,
            IsActive = true,
            RegulatorySource = "CENIT"
        });
    }

    private static IEnumerable<AchFileRejectionCode> BuildFileRejectionCodes()
    {
        return new[]
        {
            new AchFileRejectionCode { Code = "D01", Description = "Archivo duplicado", Severity = "Fatal", AppliesToStage = "Validation", IsRetryable = false },
            new AchFileRejectionCode { Code = "D02", Description = "Formato inválido", Severity = "Fatal", AppliesToStage = "Parser", IsRetryable = true },
            new AchFileRejectionCode { Code = "D03", Description = "Operador incorrecto", Severity = "Fatal", AppliesToStage = "Transmission", IsRetryable = false },
            new AchFileRejectionCode { Code = "D04", Description = "Secuencia inválida", Severity = "Fatal", AppliesToStage = "Validation", IsRetryable = true },
            new AchFileRejectionCode { Code = "D05", Description = "Control hash inválido", Severity = "Fatal", AppliesToStage = "Validation", IsRetryable = true },
            new AchFileRejectionCode { Code = "D06", Description = "Campo obligatorio ausente", Severity = "Partial", AppliesToStage = "Parser", IsRetryable = true }
        };
    }

    private static IEnumerable<AchTransactionTypePolicy> BuildTransactionTypePolicies()
    {
        return new[]
        {
            new AchTransactionTypePolicy { TransactionType = "ReturnDebit", PriorityOrder = 100, IsMonetary = true, RequiresPrenotification = false, CanBeReturned = false, CanBeReturnedAgain = true },
            new AchTransactionTypePolicy { TransactionType = "ReturnCredit", PriorityOrder = 95, IsMonetary = true, RequiresPrenotification = false, CanBeReturned = false, CanBeReturnedAgain = true },
            new AchTransactionTypePolicy { TransactionType = "Debit", PriorityOrder = 90, IsMonetary = true, RequiresPrenotification = true, CanBeReturned = true, CanBeReturnedAgain = false },
            new AchTransactionTypePolicy { TransactionType = "Credit", PriorityOrder = 80, IsMonetary = true, RequiresPrenotification = false, CanBeReturned = true, CanBeReturnedAgain = false },
            new AchTransactionTypePolicy { TransactionType = "Prenotification", PriorityOrder = 70, IsMonetary = false, RequiresPrenotification = false, CanBeReturned = true, CanBeReturnedAgain = false },
            new AchTransactionTypePolicy { TransactionType = "Return", PriorityOrder = 100, IsMonetary = true, RequiresPrenotification = false, CanBeReturned = false, CanBeReturnedAgain = true },
            new AchTransactionTypePolicy { TransactionType = "ReturnOfReturn", PriorityOrder = 60, IsMonetary = true, RequiresPrenotification = false, CanBeReturned = false, CanBeReturnedAgain = false }
        };
    }

    private static IEnumerable<AchReturnPolicy> BuildReturnPolicies()
    {
        return new[]
        {
            new AchReturnPolicy { TransactionType = "Debit", AllowedReturnCodesCsv = "R01,R02,R03,R04,R06,R07,R08,R09,R10,R12,R13,R14,R15,R16,R17,R20,R23,R29,R31", MaxDays = 15, RequiredOriginalTransactionState = "Pending", AllowsReturnOfReturn = true, RequiresAddenda = true },
            new AchReturnPolicy { TransactionType = "Credit", AllowedReturnCodesCsv = "R03,R04,R20,R23,R31", MaxDays = 1, RequiredOriginalTransactionState = "Pending", AllowsReturnOfReturn = true, RequiresAddenda = true },
            new AchReturnPolicy { TransactionType = "Prenotification", AllowedReturnCodesCsv = "R03,R29", MaxDays = 1, RequiredOriginalTransactionState = "Pending", AllowsReturnOfReturn = false, RequiresAddenda = true },
            new AchReturnPolicy { TransactionType = "Return", AllowedReturnCodesCsv = "R01,R02,R03,R09,R10", MaxDays = 15, RequiredOriginalTransactionState = "ReturnedByEpr", AllowsReturnOfReturn = true, RequiresAddenda = true }
        };
    }

    private static IEnumerable<AchReturnOfReturnPolicy> BuildReturnOfReturnPolicies()
    {
        return new[]
        {
            new AchReturnOfReturnPolicy { OriginalReturnCode = "R01", AllowedNewReturnCodesCsv = "R02,R03,R09", MaxDays = 15, RequiredOriginalState = "ReturnedByOperator", IsUniquePerTransaction = true },
            new AchReturnOfReturnPolicy { OriginalReturnCode = "R02", AllowedNewReturnCodesCsv = "R03,R10", MaxDays = 15, RequiredOriginalState = "ReturnedByOperator", IsUniquePerTransaction = true },
            new AchReturnOfReturnPolicy { OriginalReturnCode = "R03", AllowedNewReturnCodesCsv = "R03,R31", MaxDays = 15, RequiredOriginalState = "ReturnedByOperator", IsUniquePerTransaction = true }
        };
    }

    private static IEnumerable<AchPrenotificationPolicy> BuildPrenotificationPolicies()
    {
        return new[]
        {
            new AchPrenotificationPolicy { TransactionType = "Debit", IsRequired = true, RequiresAddenda = true, BlocksMonetaryTransactionIfMissing = true },
            new AchPrenotificationPolicy { TransactionType = "Credit", IsRequired = false, RequiresAddenda = false, BlocksMonetaryTransactionIfMissing = false },
            new AchPrenotificationPolicy { TransactionType = "Prenotification", IsRequired = false, RequiresAddenda = true, BlocksMonetaryTransactionIfMissing = false },
            new AchPrenotificationPolicy { TransactionType = "Return", IsRequired = false, RequiresAddenda = true, BlocksMonetaryTransactionIfMissing = false },
            new AchPrenotificationPolicy { TransactionType = "ReturnOfReturn", IsRequired = false, RequiresAddenda = true, BlocksMonetaryTransactionIfMissing = false }
        };
    }
}

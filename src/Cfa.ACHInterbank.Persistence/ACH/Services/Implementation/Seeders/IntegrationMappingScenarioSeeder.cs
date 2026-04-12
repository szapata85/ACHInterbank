using Cfa.ACHInterbank.Application.DataBase;
using Cfa.ACHInterbank.Application.Integrations.Dtos;
using Cfa.ACHInterbank.Domain.Entities.Integrations;
using Cfa.ACHInterbank.Domain.Entities.Transactions.Enums;
using Cfa.ACHInterbank.Domain.Models.ACH;
using Cfa.ACHInterbank.Persistence.DataBase;
using Cfa.ACHInterbank.Persistence.Integrations.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;

namespace Cfa.ACHInterbank.Persistence.ACH.Services.Implementation.Seeders;

[Scoped]
public sealed class IntegrationMappingScenarioSeeder : IDbSeeder
{
    private readonly AchDbContext _context;
    private readonly IHostEnvironment _environment;

    public IntegrationMappingScenarioSeeder(AchDbContext context, IHostEnvironment environment)
    {
        _context = context;
        _environment = environment;
    }

    public int Order => 7;

    public async Task SeedAsync()
    {
        if (!_environment.IsDevelopment() && !_environment.IsEnvironment("Testing"))
        {
            return;
        }

        var catalogService = new IntegrationCatalogService(_context);
        var methods = await catalogService.GetMethodsAsync();
        var method = methods.FirstOrDefault(x => x.Code == "WSCFAACH.Proc_Contrapartidas");
        if (method is null)
        {
            return;
        }

        var methodId = method.Id;

        var existingPublished = await _context.IntegrationMappingSets
            .AsNoTracking()
            .AnyAsync(x => x.MethodId == methodId && x.Status == IntegrationMappingSetStatusEnum.Published);
        if (existingPublished)
        {
            await EnsureSampleTransactionsAsync();
            return;
        }

        var parameters = await _context.IntegrationMethodParameters
            .AsNoTracking()
            .Where(x => x.MethodId == methodId && x.IsActive)
            .OrderBy(x => x.SortOrder)
            .ToListAsync();

        var draftValid = new IntegrationMappingSet
        {
            MethodId = methodId,
            Name = "ProcContrapartidas Draft Válido",
            Status = IntegrationMappingSetStatusEnum.Draft,
            Version = 0,
            IsActive = true,
            Notes = "Borrador válido para configuración funcional",
            CreatedBy = "seed"
        };

        var published = new IntegrationMappingSet
        {
            MethodId = methodId,
            Name = "ProcContrapartidas Published",
            Status = IntegrationMappingSetStatusEnum.Published,
            Version = 1,
            IsActive = true,
            Notes = "Versión publicada de referencia",
            PublishedAtUtc = DateTime.UtcNow,
            PublishedBy = "seed",
            CreatedBy = "seed"
        };

        var draftInvalid = new IntegrationMappingSet
        {
            MethodId = methodId,
            Name = "ProcContrapartidas Draft Inválido",
            Status = IntegrationMappingSetStatusEnum.Draft,
            Version = 0,
            IsActive = true,
            Notes = "Borrador con errores intencionales",
            CreatedBy = "seed"
        };

        var clonedDraft = new IntegrationMappingSet
        {
            MethodId = methodId,
            Name = "ProcContrapartidas Clone Draft",
            Status = IntegrationMappingSetStatusEnum.Draft,
            Version = 0,
            IsActive = true,
            Notes = "Clon de versión publicada",
            CreatedBy = "seed"
        };

        _context.IntegrationMappingSets.AddRange(draftValid, published, draftInvalid, clonedDraft);
        await _context.SaveChangesAsync();

        var publishedRules = BuildPublishedRules(methodId, published.Id, parameters);
        var validRules = BuildDefaultValidRules(methodId, draftValid.Id, parameters);
        var invalidRules = BuildInvalidRules(methodId, draftInvalid.Id, parameters);
        var clonedRules = publishedRules.Select(r => CloneRule(r, clonedDraft.Id)).ToList();

        _context.IntegrationMappingRules.AddRange(validRules);
        _context.IntegrationMappingRules.AddRange(publishedRules);
        _context.IntegrationMappingRules.AddRange(invalidRules);
        _context.IntegrationMappingRules.AddRange(clonedRules);

        _context.IntegrationMappingSetHistory.AddRange(
            BuildHistory(draftValid, "SeedDraftValid"),
            BuildHistory(published, "SeedPublished"),
            BuildHistory(draftInvalid, "SeedDraftInvalid"),
            BuildHistory(clonedDraft, "SeedCloned"));

        await _context.SaveChangesAsync();
        await EnsureSampleTransactionsAsync();
    }

    private static List<IntegrationMappingRule> BuildDefaultValidRules(int methodId, Guid mappingSetId, IReadOnlyCollection<IntegrationMethodParameter> parameters)
        => parameters
            .Where(p => p.Required)
            .Select(p => new IntegrationMappingRule
            {
                MappingSetId = mappingSetId,
                MethodId = methodId,
                ParameterId = p.Id,
                SourceKind = IntegrationSourceKindEnum.Constant,
                FixedValue = DefaultValueFor(p),
                Priority = 1,
                Enabled = true,
                CreatedBy = "seed"
            })
            .ToList();

    private static List<IntegrationMappingRule> BuildPublishedRules(int methodId, Guid mappingSetId, IReadOnlyCollection<IntegrationMethodParameter> parameters)
    {
        var rules = BuildDefaultValidRules(methodId, mappingSetId, parameters);

        AddPathRule("ClearingHouseId", IntegrationSourceKindEnum.ClearingHouse, "clearinghouse.id", "1");
        AddPathRule("ClearingHouseCode", IntegrationSourceKindEnum.ClearingHouse, "clearinghouse.code", "ACH");
        AddPathRule("CycleId", IntegrationSourceKindEnum.Cycle, "cycle.id", "CYCLE");
        AddPathRule("CycleName", IntegrationSourceKindEnum.Cycle, "cycle.cycleName", "CYCLE-NAME");
        AddPathRule("Transactions[].TransactionId", IntegrationSourceKindEnum.Transaction, "transaction.id", "1");
        AddPathRule("Transactions[].Amount", IntegrationSourceKindEnum.Transaction, "transaction.amount", "100");
        AddPathRule("Transactions[].Reference", IntegrationSourceKindEnum.Transaction, "transaction.reference", "REF");
        AddPathRule("Transactions[].AchBatchId", IntegrationSourceKindEnum.Batch, "batch.id", "1");
        AddPathRule("Transactions[].Addendas[].AddendaType", IntegrationSourceKindEnum.Addenda, "addenda.addendaType", "05");
        AddPathRule("Transactions[].Addendas[].Information", IntegrationSourceKindEnum.Addenda, "addenda.information", "INFO");

        return rules;

        void AddPathRule(string parameterPath, IntegrationSourceKindEnum kind, string sourcePath, string fallback)
        {
            var parameter = parameters.FirstOrDefault(p => p.ParameterPath == parameterPath);
            if (parameter is null)
            {
                return;
            }

            rules.RemoveAll(r => r.ParameterId == parameter.Id);
            rules.Add(new IntegrationMappingRule
            {
                MappingSetId = mappingSetId,
                MethodId = methodId,
                ParameterId = parameter.Id,
                SourceKind = kind,
                SourceFieldPath = sourcePath,
                DefaultValue = fallback,
                Priority = 1,
                Enabled = true,
                CreatedBy = "seed"
            });
        }
    }

    private static List<IntegrationMappingRule> BuildInvalidRules(int methodId, Guid mappingSetId, IReadOnlyCollection<IntegrationMethodParameter> parameters)
    {
        var rules = BuildDefaultValidRules(methodId, mappingSetId, parameters);
        var requiredTxId = parameters.FirstOrDefault(x => x.ParameterPath == "Transactions[].TransactionId");
        if (requiredTxId is not null)
        {
            rules.RemoveAll(x => x.ParameterId == requiredTxId.Id);
            rules.Add(new IntegrationMappingRule
            {
                MappingSetId = mappingSetId,
                MethodId = methodId,
                ParameterId = requiredTxId.Id,
                SourceKind = IntegrationSourceKindEnum.Transaction,
                SourceFieldPath = "",
                Priority = 1,
                Enabled = false,
                CreatedBy = "seed"
            });
        }

        var amount = parameters.FirstOrDefault(x => x.ParameterPath == "Transactions[].Amount");
        if (amount is not null)
        {
            rules.Add(new IntegrationMappingRule
            {
                MappingSetId = mappingSetId,
                MethodId = methodId,
                ParameterId = amount.Id,
                SourceKind = IntegrationSourceKindEnum.Transaction,
                SourceFieldPath = "transaction.amount",
                TransformationCode = "NotAllowed",
                Priority = 1,
                Enabled = true,
                CreatedBy = "seed"
            });
        }

        return rules;
    }

    private static IntegrationMappingRule CloneRule(IntegrationMappingRule source, Guid targetMappingSetId)
        => new()
        {
            MappingSetId = targetMappingSetId,
            MethodId = source.MethodId,
            ParameterId = source.ParameterId,
            SourceKind = source.SourceKind,
            SourceCatalogFieldId = source.SourceCatalogFieldId,
            SourceFieldPath = source.SourceFieldPath,
            FixedValue = source.FixedValue,
            DefaultValue = source.DefaultValue,
            TransformationCode = source.TransformationCode,
            FormatMask = source.FormatMask,
            Priority = source.Priority,
            RequiredOverride = source.RequiredOverride,
            Enabled = source.Enabled,
            ConditionExpression = source.ConditionExpression,
            CreatedBy = "seed"
        };

    private static IntegrationMappingSetHistory BuildHistory(IntegrationMappingSet set, string action)
        => new()
        {
            MappingSetId = set.Id,
            MethodId = set.MethodId,
            Version = set.Version,
            Status = set.Status,
            Action = action,
            PerformedBy = "seed",
            PerformedAtUtc = DateTime.UtcNow,
            SnapshotJson = $"{{\"mappingSet\":\"{set.Name}\"}}",
            SnapshotHash = Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(set.Name))),
            CreatedBy = "seed"
        };

    private static string DefaultValueFor(IntegrationMethodParameter parameter)
        => parameter.DataType.ToLowerInvariant() switch
        {
            "int" or "long" => "1",
            "decimal" or "double" or "float" => "1.00",
            "datetime" => DateTime.UtcNow.ToString("O"),
            "timespan" => "08:00:00",
            _ => "SEED"
        };

    private async Task EnsureSampleTransactionsAsync()
    {
        var hasTransactions = await _context.AchTransactions.AsNoTracking().AnyAsync();
        if (hasTransactions)
        {
            return;
        }

        var cycle = await _context.AchCycles.AsNoTracking().FirstOrDefaultAsync();
        if (cycle is null)
        {
            cycle = new AchCycle
            {
                Id = "SEED-CYCLE",
                CycleName = "Ciclo Seed",
                ProcessingDate = DateTime.UtcNow.Date,
                StartTime = TimeSpan.FromHours(8),
                EndTime = TimeSpan.FromHours(17),
                CutoffTime = TimeSpan.FromHours(16),
                ClearingHouseId = 1,
                CreatedBy = "seed"
            };
            _context.AchCycles.Add(cycle);
            await _context.SaveChangesAsync();
        }

        var batch = new AchBatch
        {
            AchCycleId = cycle.Id,
            EffectiveEntryDate = cycle.ProcessingDate,
            BatchSequenceNumber = 1,
            CompanyName = "SEED COMPANY",
            CompanyIdentification = "900123456",
            CompanyEntryDescription = "PAGOS",
            CompanyEntryDescriptionId = 1,
            OriginOrOdfi = "000010070",
            CreatedBy = "seed"
        };

        var tx = new AchTransaction
        {
            AchCycleId = cycle.Id,
            AchBatch = batch,
            Amount = 2500m,
            Reference = "SEED-MAPPING-001",
            Type = TransactionTypeEnum.Credit,
            TransactionCode = "22",
            CompanyEntryDescriptionId = 1,
            CompanyName = "SEED COMPANY",
            CompanyIdentification = "900123456",
            OriginatingDFI = "000010070",
            ReceivingDFI = "000010010",
            TraceNumber = "000010070000123",
            TraceSequenceNumber = 123,
            EffectiveEntryDate = cycle.ProcessingDate,
            SourceInstitutionId = 1,
            DestinationInstitutionId = 2,
            SourceAccountNumber = "123456789",
            DestinationAccountNumber = "987654321",
            CreatedBy = "seed"
        };

        _context.AchTransactions.Add(tx);
        _context.AchTransactionAddendas.Add(new AchTransactionAddenda
        {
            Transaction = tx,
            AddendaType = "05",
            BusinessType = AchAddendaBusinessType.Credit,
            Information = "SEED INFO",
            SequenceNumber = 1,
            CreatedBy = "seed"
        });

        await _context.SaveChangesAsync();
    }
}

using Cfa.ACHInterbank.Application.DataBase;
using Cfa.ACHInterbank.Application.Integrations.Dtos;
using Cfa.ACHInterbank.Domain.Entities.Integrations;
using Cfa.ACHInterbank.Domain.Entities.Transactions.Enums;
using Cfa.ACHInterbank.Domain.Models.ACH;
using Cfa.ACHInterbank.Domain.Models.ACH.Enums;
using Cfa.ACHInterbank.Domain.Models.Configurations;
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

        await new IntegrationCatalogBootstrapper(_context).EnsureAsync();
        await new IntegrationMappingBootstrapper(_context).EnsureAsync();

        var method = await _context.IntegrationMethods
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Code == "WSCFAACH.Proc_Contrapartidas" && x.IsActive);
        if (method is null)
        {
            return;
        }

        var methodId = method.Id;

        var demoAlreadySeeded = await _context.IntegrationMappingSets
            .AsNoTracking()
            .AnyAsync(x => x.MethodId == methodId && x.Name == "ProcContrapartidas Draft Valido");
        if (demoAlreadySeeded)
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
            Name = "ProcContrapartidas Draft Valido",
            Status = IntegrationMappingSetStatusEnum.Draft,
            Version = 0,
            IsActive = true,
            Notes = "Borrador valido para configuracion funcional"
        };

        var draftInvalid = new IntegrationMappingSet
        {
            MethodId = methodId,
            Name = "ProcContrapartidas Draft Invalido",
            Status = IntegrationMappingSetStatusEnum.Draft,
            Version = 0,
            IsActive = true,
            Notes = "Borrador con errores intencionales"
        };

        var clonedDraft = new IntegrationMappingSet
        {
            MethodId = methodId,
            Name = "ProcContrapartidas Clone Draft",
            Status = IntegrationMappingSetStatusEnum.Draft,
            Version = 0,
            IsActive = true,
            Notes = "Clon de version publicada"
        };

        _context.IntegrationMappingSets.AddRange(draftValid, draftInvalid, clonedDraft);
        await _context.SaveChangesAsync();

        var published = await _context.IntegrationMappingSets
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.MethodId == methodId && x.Status == IntegrationMappingSetStatusEnum.Published && x.IsActive);
        if (published is null)
        {
            return;
        }

        var publishedRules = await _context.IntegrationMappingRules
            .AsNoTracking()
            .Where(x => x.MappingSetId == published.Id)
            .OrderBy(x => x.ParameterId)
            .ToListAsync();
        var validRules = BuildDefaultValidRules(methodId, draftValid.Id, parameters);
        var invalidRules = BuildInvalidRules(methodId, draftInvalid.Id, parameters);
        var clonedRules = publishedRules.Select(r => CloneRule(r, clonedDraft.Id)).ToList();

        _context.IntegrationMappingRules.AddRange(validRules);
        _context.IntegrationMappingRules.AddRange(invalidRules);
        _context.IntegrationMappingRules.AddRange(clonedRules);

        _context.IntegrationMappingSetHistory.AddRange(
            BuildHistory(draftValid, "SeedDraftValid"),
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
                Enabled = true
            })
            .ToList();

    private static List<IntegrationMappingRule> BuildPublishedRules(int methodId, Guid mappingSetId, IReadOnlyCollection<IntegrationMethodParameter> parameters)
    {
        var rules = BuildDefaultValidRules(methodId, mappingSetId, parameters);

        AddPathRule("OFNIT", IntegrationSourceKindEnum.Transaction, "transaction.companyidentification", "900123456");
        AddPathRule("OFEMP", IntegrationSourceKindEnum.ClearingHouse, "clearinghouse.code", "ACH");
        AddPathRule("OFCTA", IntegrationSourceKindEnum.Transaction, "transaction.originatingdfi", "000010070");
        AddPathRule("OFDD", IntegrationSourceKindEnum.Constant, "constant.value", "C");
        AddPathRule("OFFECHEFEC", IntegrationSourceKindEnum.Cycle, "cycle.processingdate", DateTime.UtcNow.ToString("yyyyMMdd"));
        AddPathRule("OFMONCRE", IntegrationSourceKindEnum.Transaction, "transaction.amount", "0");
        AddPathRule("OFMONDEB", IntegrationSourceKindEnum.Constant, "constant.value", "0");
        AddPathRule("OFIDARCH", IntegrationSourceKindEnum.Batch, "batch.id", "1");
        AddPathRule("OFIDLOT", IntegrationSourceKindEnum.Batch, "batch.id", "1");
        AddPathRule("OFIDTX", IntegrationSourceKindEnum.Transaction, "transaction.reference", "REF-1");
        AddPathRule("OFIDEBAPLI", IntegrationSourceKindEnum.Transaction, "transaction.id", "1");
        AddPathRule("OFIDCAMCOMPE", IntegrationSourceKindEnum.ClearingHouse, "clearinghouse.id", "1");
        AddPathRule("OFDIRECCIONIP", IntegrationSourceKindEnum.Constant, "constant.value", "0.0.0.0");

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
                Enabled = true
            });
        }
    }

    private static List<IntegrationMappingRule> BuildInvalidRules(int methodId, Guid mappingSetId, IReadOnlyCollection<IntegrationMethodParameter> parameters)
    {
        var rules = BuildDefaultValidRules(methodId, mappingSetId, parameters);
        var requiredTxId = parameters.FirstOrDefault(x => x.ParameterPath == "OFIDTX");
        if (requiredTxId is not null)
        {
            rules.RemoveAll(x => x.ParameterId == requiredTxId.Id);
            rules.Add(new IntegrationMappingRule
            {
                MappingSetId = mappingSetId,
                MethodId = methodId,
                ParameterId = requiredTxId.Id,
                SourceKind = IntegrationSourceKindEnum.Constant,
                SourceFieldPath = "",
                Priority = 1,
                Enabled = false
            });
        }

        var amount = parameters.FirstOrDefault(x => x.ParameterPath == "OFMONCRE");
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
                Enabled = true
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
            ConditionExpression = source.ConditionExpression
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
            SnapshotHash = Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(set.Name)))
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

    private Task EnsureSampleTransactionsAsync()
    {
        // Los datos transaccionales de ejemplo no deben persistirse por seed runtime.
        // Las pruebas y validaciones deben crear sus propios datos explícitos.
        return Task.CompletedTask;
    }

}

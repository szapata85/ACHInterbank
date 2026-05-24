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

        var catalogService = new IntegrationCatalogService(_context);
        var methods = await catalogService.GetMethodsAsync();
        await EnsurePublishedReferenceMappingAsync(
            "WSCFAACH.Proc_Transacciones",
            "ProcTransacciones Published NACHA desagregado",
            ProcTransaccionesSourcePathFor);
        await EnsurePublishedReferenceMappingAsync(
            "WSAXON.RegistrarRespuestaTransaccion",
            "RegistrarRespuestaTransaccion Published respuesta diferencial",
            RegistrarRespuestaSourcePathFor);
        await EnsureDifferentialPrenotificationResponseStatusMappingsAsync();

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
            Notes = "Borrador válido para configuración funcional"
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
            PublishedBy = "seed"
        };

        var draftInvalid = new IntegrationMappingSet
        {
            MethodId = methodId,
            Name = "ProcContrapartidas Draft Inválido",
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
            Notes = "Clon de versión publicada"
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

    private async Task EnsurePublishedReferenceMappingAsync(
        string methodCode,
        string mappingName,
        Func<string, string?> sourcePathFor)
    {
        var method = await _context.IntegrationMethods
            .FirstOrDefaultAsync(x => x.Code == methodCode && x.IsActive);
        if (method is null)
        {
            return;
        }

        var existingPublished = await _context.IntegrationMappingSets
            .AsNoTracking()
            .AnyAsync(x => x.MethodId == method.Id
                && x.Status == IntegrationMappingSetStatusEnum.Published
                && x.IsActive);
        if (existingPublished)
        {
            return;
        }

        var parameters = await _context.IntegrationMethodParameters
            .AsNoTracking()
            .Where(x => x.MethodId == method.Id && x.IsActive)
            .OrderBy(x => x.SortOrder)
            .ToListAsync();

        var published = new IntegrationMappingSet
        {
            MethodId = method.Id,
            Name = mappingName,
            Status = IntegrationMappingSetStatusEnum.Published,
            Version = 1,
            IsActive = true,
            Notes = "Mapping UAT/local de referencia. No habilita transmision externa.",
            PublishedAtUtc = DateTime.UtcNow,
            PublishedBy = "seed"
        };

        _context.IntegrationMappingSets.Add(published);
        await _context.SaveChangesAsync();

        foreach (var parameter in parameters)
        {
            var sourcePath = sourcePathFor(parameter.ParameterPath);
            _context.IntegrationMappingRules.Add(new IntegrationMappingRule
            {
                MappingSetId = published.Id,
                MethodId = method.Id,
                ParameterId = parameter.Id,
                SourceKind = SourceKindFor(sourcePath),
                SourceFieldPath = sourcePath ?? string.Empty,
                FixedValue = sourcePath is null ? DefaultValueFor(parameter) : null,
                DefaultValue = parameter.Required ? null : DefaultValueFor(parameter),
                Priority = 1,
                Enabled = true
            });
        }

        _context.IntegrationMappingSetHistory.Add(BuildHistory(published, "SeedPublishedReference"));
        await _context.SaveChangesAsync();
    }

    private async Task EnsureDifferentialPrenotificationResponseStatusMappingsAsync()
    {
        await EnsureResponseStatusMappingAsync(
            camara: "ACH",
            tipoRespuesta: TipoRespuestaAch.Prenota,
            estadoExterno: "00",
            causalExterna: null,
            idEstadoInterno: 1,
            idEstadoServicioExterno: 1,
            estadoInternoNombre: "Aprobada",
            causalNormalizada: null,
            descripcionCausal: null,
            requiereCausal: false);

        await EnsureResponseStatusMappingAsync(
            camara: "ACH",
            tipoRespuesta: TipoRespuestaAch.Prenota,
            estadoExterno: "RJ",
            causalExterna: "R03",
            idEstadoInterno: 2,
            idEstadoServicioExterno: 2,
            estadoInternoNombre: "Rechazada",
            causalNormalizada: "R03",
            descripcionCausal: "Cuenta no localizada",
            requiereCausal: true);
    }

    private async Task EnsureResponseStatusMappingAsync(
        string camara,
        TipoRespuestaAch tipoRespuesta,
        string estadoExterno,
        string? causalExterna,
        int idEstadoInterno,
        int idEstadoServicioExterno,
        string estadoInternoNombre,
        string? causalNormalizada,
        string? descripcionCausal,
        bool requiereCausal)
    {
        var exists = await _context.AchResponseStatusMappings.AnyAsync(x =>
            x.CodigoCamaraCompensacion == camara
            && x.TipoRespuesta == tipoRespuesta
            && x.CodigoEstadoExterno == estadoExterno
            && x.CodigoCausalExterna == causalExterna
            && x.Activo);
        if (exists)
        {
            return;
        }

        _context.AchResponseStatusMappings.Add(new AchResponseStatusMapping
        {
            CodigoCamaraCompensacion = camara,
            TipoRespuesta = tipoRespuesta,
            CodigoEstadoExterno = estadoExterno,
            CodigoCausalExterna = causalExterna,
            IdEstadoInterno = idEstadoInterno,
            IdEstadoServicioExterno = idEstadoServicioExterno,
            EstadoInternoNombre = estadoInternoNombre,
            CausalNormalizada = causalNormalizada,
            DescripcionCausalNormalizada = descripcionCausal,
            RequiereCausal = requiereCausal,
            PermiteNotificacion = true,
            Activo = true,
            FechaInicioVigencia = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc),
            FechaCreacion = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc)
        });

        await _context.SaveChangesAsync();
    }

    private static string? ProcTransaccionesSourcePathFor(string parameterPath)
        => parameterPath switch
        {
            "TIPTRAN" => "entryDetails.transactionCode",
            "BCORECEP" => "nachaHeaders.immediateDestination",
            "BCOORIG" => "nachaHeaders.immediateOrigin",
            "NORIG" => "batchHeaders.companyName",
            "NCTAORIG" => "batchHeaders.companyId",
            "IDORIG" => "batchHeaders.companyId",
            "DESTRAN" => "batchHeaders.companyEntryDescription",
            "FECEFEC" => "batchHeaders.effectiveEntryDate",
            "NCTARECEP" => "entryDetails.accountNumber",
            "MONTO" => "entryDetails.amount",
            "NRECEP" => "entryDetails.recipUserName",
            "IDRECEP" => "entryDetails.recipIdNumber",
            "INFPAG" => "addendaRecords.infofromOriginator",
            "IDTRAN" => "entryDetails.sequenceNumber",
            "IDLOTE" => "batchHeaders.batchNumber",
            "REGLOTE" => "batchControls.entryAddendaCount",
            "LIBRE1" => "fileControls.blockCount",
            _ => null
        };

    private static string? RegistrarRespuestaSourcePathFor(string parameterPath)
        => parameterPath switch
        {
            "ANSIDLOTE" => "batchHeaders.batchNumber",
            "ANSST" => "differentialResponse.codigoEstadoExterno",
            "ANCLC" => "differentialResponse.codigoCausalExterna",
            "ANSIDTX" => "entryDetails.sequenceNumber",
            "ANSIDREVER" => "addendaRecords.originalTraceNumber",
            _ => null
        };

    private static IntegrationSourceKindEnum SourceKindFor(string? sourcePath)
    {
        if (sourcePath is null) return IntegrationSourceKindEnum.Constant;
        if (sourcePath.StartsWith("nachaHeaders.", StringComparison.OrdinalIgnoreCase)) return IntegrationSourceKindEnum.NachaHeader;
        if (sourcePath.StartsWith("batchHeaders.", StringComparison.OrdinalIgnoreCase)) return IntegrationSourceKindEnum.BatchHeader;
        if (sourcePath.StartsWith("entryDetails.", StringComparison.OrdinalIgnoreCase)) return IntegrationSourceKindEnum.EntryDetail;
        if (sourcePath.StartsWith("addendaRecords.", StringComparison.OrdinalIgnoreCase)) return IntegrationSourceKindEnum.AddendaRecord;
        if (sourcePath.StartsWith("batchControls.", StringComparison.OrdinalIgnoreCase)) return IntegrationSourceKindEnum.BatchControl;
        if (sourcePath.StartsWith("fileControls.", StringComparison.OrdinalIgnoreCase)) return IntegrationSourceKindEnum.FileControl;
        if (sourcePath.StartsWith("prenotification.", StringComparison.OrdinalIgnoreCase)) return IntegrationSourceKindEnum.Prenotification;
        if (sourcePath.StartsWith("differentialResponse.", StringComparison.OrdinalIgnoreCase)) return IntegrationSourceKindEnum.DifferentialResponse;
        if (sourcePath.StartsWith("transaction.", StringComparison.OrdinalIgnoreCase)) return IntegrationSourceKindEnum.Transaction;
        return IntegrationSourceKindEnum.Constant;
    }

}

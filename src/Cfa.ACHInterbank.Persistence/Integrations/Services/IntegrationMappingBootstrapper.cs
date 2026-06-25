using System.Globalization;
using Cfa.ACHInterbank.Domain.Entities.Integrations;
using Cfa.ACHInterbank.Domain.Models.ACH.Enums;
using Cfa.ACHInterbank.Domain.Models.ACH;
using Cfa.ACHInterbank.Persistence.DataBase;
using Microsoft.EntityFrameworkCore;

namespace Cfa.ACHInterbank.Persistence.Integrations.Services;

public sealed class IntegrationMappingBootstrapper
{
    private readonly AchDbContext _context;
    private readonly IntegrationCatalogBootstrapper _catalogBootstrapper;

    public IntegrationMappingBootstrapper(AchDbContext context)
    {
        _context = context;
        _catalogBootstrapper = new IntegrationCatalogBootstrapper(context);
    }

    public async Task EnsureAsync(CancellationToken ct = default)
    {
        await _catalogBootstrapper.EnsureAsync(ct);

        await EnsurePublishedContrapartidasMappingAsync(ct);
        await EnsurePublishedReferenceMappingAsync(
            "WSCFAACH.Proc_Transacciones",
            "ProcTransacciones Published NACHA desagregado",
            ProcTransaccionesSourcePathFor,
            ct);
        await EnsurePublishedRegistrarRespuestaMappingAsync(ct);
        await EnsureDifferentialPrenotificationResponseStatusMappingsAsync(ct);
    }

    private async Task EnsurePublishedContrapartidasMappingAsync(CancellationToken ct)
    {
        var method = await _context.IntegrationMethods
            .FirstOrDefaultAsync(x => x.Code == "WSCFAACH.Proc_Contrapartidas" && x.IsActive, ct);
        if (method is null)
        {
            return;
        }

        var existingPublished = await _context.IntegrationMappingSets
            .AsNoTracking()
            .AnyAsync(x => x.MethodId == method.Id
                && x.Status == IntegrationMappingSetStatusEnum.Published
                && x.IsActive, ct);
        if (existingPublished)
        {
            return;
        }

        var parameters = await _context.IntegrationMethodParameters
            .AsNoTracking()
            .Where(x => x.MethodId == method.Id && x.IsActive)
            .OrderBy(x => x.SortOrder)
            .ToListAsync(ct);

        var published = new IntegrationMappingSet
        {
            MethodId = method.Id,
            Name = "ProcContrapartidas Published",
            Status = IntegrationMappingSetStatusEnum.Published,
            Version = 1,
            IsActive = true,
            Notes = "Version publicada de referencia funcional",
            PublishedAtUtc = DateTime.UtcNow,
            PublishedBy = "seed"
        };

        _context.IntegrationMappingSets.Add(published);
        await _context.SaveChangesAsync(ct);

        var publishedRules = BuildPublishedRules(method.Id, published.Id, parameters);
        _context.IntegrationMappingRules.AddRange(publishedRules);
        _context.IntegrationMappingSetHistory.Add(BuildHistory(published, "SeedPublished"));
        await _context.SaveChangesAsync(ct);
    }

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

    private async Task EnsurePublishedReferenceMappingAsync(
        string methodCode,
        string mappingName,
        Func<string, string?> sourcePathFor,
        CancellationToken ct)
    {
        var method = await _context.IntegrationMethods
            .FirstOrDefaultAsync(x => x.Code == methodCode && x.IsActive, ct);
        if (method is null)
        {
            return;
        }

        var existingPublished = await _context.IntegrationMappingSets
            .AsNoTracking()
            .AnyAsync(x => x.MethodId == method.Id
                && x.Status == IntegrationMappingSetStatusEnum.Published
                && x.IsActive, ct);
        if (existingPublished)
        {
            return;
        }

        var parameters = await _context.IntegrationMethodParameters
            .AsNoTracking()
            .Where(x => x.MethodId == method.Id && x.IsActive)
            .OrderBy(x => x.SortOrder)
            .ToListAsync(ct);

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
        await _context.SaveChangesAsync(ct);

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
        await _context.SaveChangesAsync(ct);
    }

    private async Task EnsurePublishedRegistrarRespuestaMappingAsync(CancellationToken ct)
    {
        const string methodCode = "WSAXON.RegistrarRespuestaTransaccion";
        const string mappingName = "RegistrarRespuestaTransaccion Published respuesta diferencial";

        var method = await _context.IntegrationMethods
            .FirstOrDefaultAsync(x => x.Code == methodCode && x.IsActive, ct);
        if (method is null)
        {
            return;
        }

        var parameters = await _context.IntegrationMethodParameters
            .AsNoTracking()
            .Where(x => x.MethodId == method.Id && x.IsActive)
            .OrderBy(x => x.SortOrder)
            .ToListAsync(ct);

        var publishedSets = await _context.IntegrationMappingSets
            .Where(x => x.MethodId == method.Id
                && x.Status == IntegrationMappingSetStatusEnum.Published
                && x.IsActive)
            .OrderByDescending(x => x.Version)
            .ToListAsync(ct);

        foreach (var published in publishedSets)
        {
            var rules = await _context.IntegrationMappingRules
                .AsNoTracking()
                .Where(x => x.MappingSetId == published.Id && x.Enabled)
                .ToListAsync(ct);

            if (IsRegistrarRespuestaMappingCompatible(parameters, rules))
            {
                return;
            }
        }

        var hasManualPublished = publishedSets.Any(x =>
            !string.Equals(x.PublishedBy, "seed", StringComparison.OrdinalIgnoreCase)
            || !string.Equals(x.Name, mappingName, StringComparison.OrdinalIgnoreCase));
        if (hasManualPublished)
        {
            return;
        }

        foreach (var legacySeed in publishedSets)
        {
            legacySeed.Status = IntegrationMappingSetStatusEnum.Archived;
            legacySeed.IsActive = false;
            _context.IntegrationMappingSetHistory.Add(BuildHistory(legacySeed, "ArchivedByWsdlContractRealignment"));
        }

        var nextVersion = (await _context.IntegrationMappingSets
            .Where(x => x.MethodId == method.Id)
            .Select(x => (int?)x.Version)
            .MaxAsync(ct) ?? 0) + 1;

        var publishedSet = new IntegrationMappingSet
        {
            MethodId = method.Id,
            Name = mappingName,
            Status = IntegrationMappingSetStatusEnum.Published,
            Version = nextVersion,
            IsActive = true,
            Notes = "Mapping UAT/local de referencia alineado al WSDL real. No habilita transmision externa.",
            PublishedAtUtc = DateTime.UtcNow,
            PublishedBy = "seed"
        };

        _context.IntegrationMappingSets.Add(publishedSet);
        await _context.SaveChangesAsync(ct);

        foreach (var parameter in parameters)
        {
            var sourcePath = RegistrarRespuestaSourcePathFor(parameter.ParameterPath);
            _context.IntegrationMappingRules.Add(new IntegrationMappingRule
            {
                MappingSetId = publishedSet.Id,
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

        _context.IntegrationMappingSetHistory.Add(BuildHistory(publishedSet, "SeedPublishedReferenceWsdl"));
        await _context.SaveChangesAsync(ct);
    }

    private static bool IsRegistrarRespuestaMappingCompatible(
        IReadOnlyCollection<IntegrationMethodParameter> parameters,
        IReadOnlyCollection<IntegrationMappingRule> rules)
    {
        var requiredParameterIds = parameters
            .Where(x => x.Required)
            .Select(x => x.Id)
            .ToHashSet();

        return requiredParameterIds.Count > 0
            && requiredParameterIds.All(id => rules.Any(rule => rule.ParameterId == id));
    }

    private async Task EnsureDifferentialPrenotificationResponseStatusMappingsAsync(CancellationToken ct)
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
            requiereCausal: false,
            ct);

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
            requiereCausal: true,
            ct);
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
        bool requiereCausal,
        CancellationToken ct)
    {
        var exists = await _context.AchResponseStatusMappings.AnyAsync(x =>
            x.CodigoCamaraCompensacion == camara
            && x.TipoRespuesta == tipoRespuesta
            && x.CodigoEstadoExterno == estadoExterno
            && x.CodigoCausalExterna == causalExterna
            && x.Activo, ct);
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

        await _context.SaveChangesAsync(ct);
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
            "idCanal" => "differentialResponse.idCanal",
            "nombreCanal" => "differentialResponse.nombreCanal",
            "idTransaccion" => "differentialResponse.idTransaccion",
            "idEstado" => "differentialResponse.idEstado",
            "causal" => "differentialResponse.codigoCausalExterna",
            "idTransaccionAxon" => "differentialResponse.idTransaccionServicioExterno",
            "descripcionCausal" => "differentialResponse.descripcionCausalExterna",
            _ => null
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

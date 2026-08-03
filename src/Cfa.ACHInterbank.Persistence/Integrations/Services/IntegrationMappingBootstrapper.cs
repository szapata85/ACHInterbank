using System.Globalization;
using Cfa.ACHInterbank.Domain.Entities.Integrations;
using Cfa.ACHInterbank.Domain.Models.ACH.Enums;
using Cfa.ACHInterbank.Domain.Models.ACH;
using Cfa.ACHInterbank.Persistence.DataBase;
using Microsoft.EntityFrameworkCore;

namespace Cfa.ACHInterbank.Persistence.Integrations.Services;

public sealed class IntegrationMappingBootstrapper
{
    private const string ArchivedInvalidSeedContractAction = "ArchivedInvalidSeedContract";
    private static readonly string PreviousRegistrarArchiveAction = string.Concat("Archived", "By", "Wsdl", "ContractRealignment");

    private static readonly string[] RegistrarRespuestaWsdlParameterPaths =
    [
        "idCanal",
        "nombreCanal",
        "idTransaccion",
        "idEstado",
        "causal",
        "idTransaccionAxon",
        "descripcionCausal"
    ];

    private static readonly string[] RegistrarRespuestaNonWsdlParameterPaths =
    [
        "ANSIDLOTE",
        "ANSST",
        "ANCLC",
        "ANSIDTX",
        "ANSIDREVER"
    ];

    private readonly AchDbContext _context;
    private readonly IntegrationCatalogBootstrapper _catalogBootstrapper;
    private readonly IntegrationMappingSnapshotBuilder _snapshotBuilder;

    public IntegrationMappingBootstrapper(AchDbContext context, IntegrationMappingSnapshotBuilder? snapshotBuilder = null)
    {
        _context = context;
        _catalogBootstrapper = new IntegrationCatalogBootstrapper(context);
        _snapshotBuilder = snapshotBuilder ?? new IntegrationMappingSnapshotBuilder(context);
    }

    public async Task EnsureAsync(CancellationToken ct = default)
    {
        await _catalogBootstrapper.EnsureAsync(ct);

        await NormalizeRegistrarRespuestaHistoryActionsAsync(ct);
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

        var existingPublishedSets = await _context.IntegrationMappingSets
            .Where(x => x.MethodId == method.Id
                && x.Status == IntegrationMappingSetStatusEnum.Published
                && x.IsActive)
            .OrderByDescending(x => x.Version)
            .ToListAsync(ct);

        var parameters = await _context.IntegrationMethodParameters
            .AsNoTracking()
            .Where(x => x.MethodId == method.Id && x.IsActive)
            .OrderBy(x => x.SortOrder)
            .ToListAsync(ct);

        if (existingPublishedSets.Any(x => !string.Equals(x.PublishedBy, "seed", StringComparison.OrdinalIgnoreCase)))
        {
            return;
        }

        foreach (var existing in existingPublishedSets)
        {
            var existingRules = await _context.IntegrationMappingRules
                .AsNoTracking()
                .Where(x => x.MappingSetId == existing.Id && x.Enabled)
                .ToListAsync(ct);
            if (IsProcContrapartidasMappingCompatible(method.Id, existing.Id, parameters, existingRules))
            {
                return;
            }
        }

        foreach (var invalid in existingPublishedSets)
        {
            invalid.Status = IntegrationMappingSetStatusEnum.Archived;
            invalid.IsActive = false;
        }

        var nextVersion = (await _context.IntegrationMappingSets
            .Where(x => x.MethodId == method.Id)
            .Select(x => (int?)x.Version)
            .MaxAsync(ct) ?? 0) + 1;

        var published = new IntegrationMappingSet
        {
            MethodId = method.Id,
            Name = "ProcContrapartidas Published",
            Status = IntegrationMappingSetStatusEnum.Published,
            Version = nextVersion,
            IsActive = true,
            Notes = "Version publicada de referencia funcional",
            PublishedAtUtc = DateTime.UtcNow,
            PublishedBy = "seed"
        };

        _context.IntegrationMappingSets.Add(published);
        var publishedRules = BuildPublishedRules(method.Id, published.Id, parameters);
        _context.IntegrationMappingRules.AddRange(publishedRules);
        await _context.SaveChangesAsync(ct);
        foreach (var invalid in existingPublishedSets)
        {
            _context.IntegrationMappingSetHistory.Add(await BuildHistoryAsync(invalid, "ArchivedInvalidProcContrapartidasMapping", ct));
        }
        _context.IntegrationMappingSetHistory.Add(await BuildHistoryAsync(published, "SeedPublished", ct));
        await _context.SaveChangesAsync(ct);
    }

    private static List<IntegrationMappingRule> BuildPublishedRules(int methodId, Guid mappingSetId, IReadOnlyCollection<IntegrationMethodParameter> parameters)
    {
        var rules = BuildDefaultValidRules(methodId, mappingSetId, parameters);

        AddPathRule("OFNIT", IntegrationSourceKindEnum.Transaction, "transaction.companyidentification", "900123456");
        AddPathRule("OFEMP", IntegrationSourceKindEnum.ClearingHouse, "clearinghouse.code", "ACH");
        AddPathRule("OFCTA", IntegrationSourceKindEnum.Transaction, "transaction.sourceAccountNumber", null);
        AddConstantRule("OFDD", "TRANSFER  ");
        AddPathRule("OFFECHEFEC", IntegrationSourceKindEnum.Cycle, "cycle.processingdate", null);
        AddPathRule("OFMONDEB", IntegrationSourceKindEnum.Transaction, "transaction.amount", "0");
        AddConstantRule("OFMONCRE", "0");
        AddPathRule("OFIDARCH", IntegrationSourceKindEnum.Batch, "batch.id", "1");
        AddPathRule("OFIDLOT", IntegrationSourceKindEnum.Batch, "batch.id", "1");
        AddConstantRule("OFST", "OO");
        AddConstantRule("OFIDTX", "0");
        AddConstantRule("OFIDREVER", "0");
        AddConstantRule("OFIDEBAPLI", "1");
        AddPathRule("OFIDCAMCOMPE", IntegrationSourceKindEnum.ClearingHouse, "clearinghouse.id", "1");
        AddPathRule("OFDIRECCIONIP", IntegrationSourceKindEnum.Constant, "constant.value", "0.0.0.0");
        AddPathRule("OFLIBRE", IntegrationSourceKindEnum.Transaction, "transaction.reference", null);
        AddPathRule("OFLIBRE1", IntegrationSourceKindEnum.Transaction, "transaction.id", null);

        return rules;

        void AddPathRule(string parameterPath, IntegrationSourceKindEnum kind, string sourcePath, string? fallback)
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

        void AddConstantRule(string parameterPath, string value)
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
                SourceKind = IntegrationSourceKindEnum.Constant,
                FixedValue = value,
                DefaultValue = value,
                Priority = 1,
                Enabled = true
            });
        }
    }

    private static bool IsProcContrapartidasMappingCompatible(
        int methodId,
        Guid mappingSetId,
        IReadOnlyCollection<IntegrationMethodParameter> parameters,
        IReadOnlyCollection<IntegrationMappingRule> rules)
    {
        var expected = BuildPublishedRules(methodId, mappingSetId, parameters);
        if (rules.Count != expected.Count)
        {
            return false;
        }

        return expected.All(desired => rules.Any(actual =>
            actual.ParameterId == desired.ParameterId
            && actual.SourceKind == desired.SourceKind
            && string.Equals(actual.SourceFieldPath ?? string.Empty, desired.SourceFieldPath ?? string.Empty, StringComparison.OrdinalIgnoreCase)
            && string.Equals(actual.FixedValue, desired.FixedValue, StringComparison.Ordinal)
            && string.Equals(actual.DefaultValue, desired.DefaultValue, StringComparison.Ordinal)
            && actual.Enabled));
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

        var existingPublishedSets = await _context.IntegrationMappingSets
            .Where(x => x.MethodId == method.Id
                && x.Status == IntegrationMappingSetStatusEnum.Published
                && x.IsActive)
            .OrderByDescending(x => x.Version)
            .ToListAsync(ct);

        var parameters = await _context.IntegrationMethodParameters
            .AsNoTracking()
            .Where(x => x.MethodId == method.Id && x.IsActive)
            .OrderBy(x => x.SortOrder)
            .ToListAsync(ct);

        if (existingPublishedSets.Any(x => !string.Equals(x.PublishedBy, "seed", StringComparison.OrdinalIgnoreCase)))
        {
            return;
        }

        foreach (var existing in existingPublishedSets)
        {
            var existingRules = await _context.IntegrationMappingRules
                .AsNoTracking()
                .Where(x => x.MappingSetId == existing.Id && x.Enabled)
                .ToListAsync(ct);
            if (IsProcTransaccionesMappingCompatible(parameters, existingRules))
            {
                return;
            }
        }

        foreach (var invalid in existingPublishedSets)
        {
            invalid.Status = IntegrationMappingSetStatusEnum.Archived;
            invalid.IsActive = false;
        }

        var nextVersion = (await _context.IntegrationMappingSets
            .Where(x => x.MethodId == method.Id)
            .Select(x => (int?)x.Version)
            .MaxAsync(ct) ?? 0) + 1;

        var published = new IntegrationMappingSet
        {
            MethodId = method.Id,
            Name = mappingName,
            Status = IntegrationMappingSetStatusEnum.Published,
            Version = nextVersion,
            IsActive = true,
            Notes = "Mapping UAT/local de referencia. No habilita transmision externa.",
            PublishedAtUtc = DateTime.UtcNow,
            PublishedBy = "seed"
        };

        _context.IntegrationMappingSets.Add(published);

        foreach (var parameter in parameters.Where(x => x.Direction == IntegrationParameterDirectionEnum.Input))
        {
            var sourcePath = sourcePathFor(parameter.ParameterPath);
            var fixedValue = ProcTransaccionesFixedValueFor(parameter.ParameterPath);
            if (sourcePath is null && fixedValue is null && !parameter.Required)
            {
                continue;
            }

            _context.IntegrationMappingRules.Add(new IntegrationMappingRule
            {
                MappingSetId = published.Id,
                MethodId = method.Id,
                ParameterId = parameter.Id,
                SourceKind = SourceKindFor(sourcePath),
                SourceFieldPath = sourcePath ?? string.Empty,
                FixedValue = fixedValue,
                DefaultValue = null,
                Priority = 1,
                Enabled = true
            });
        }

        await _context.SaveChangesAsync(ct);
        foreach (var invalid in existingPublishedSets)
        {
            _context.IntegrationMappingSetHistory.Add(await BuildHistoryAsync(invalid, "ArchivedInvalidProcTransaccionesMapping", ct));
        }

        _context.IntegrationMappingSetHistory.Add(await BuildHistoryAsync(published, "SeedPublishedReference", ct));
        await _context.SaveChangesAsync(ct);
    }

    private static bool IsProcTransaccionesMappingCompatible(
        IReadOnlyCollection<IntegrationMethodParameter> parameters,
        IReadOnlyCollection<IntegrationMappingRule> rules)
    {
        foreach (var parameter in parameters.Where(x => x.Direction == IntegrationParameterDirectionEnum.Input))
        {
            var expectedSource = ProcTransaccionesSourcePathFor(parameter.ParameterPath);
            var expectedFixed = ProcTransaccionesFixedValueFor(parameter.ParameterPath);
            var rule = rules.FirstOrDefault(x => x.ParameterId == parameter.Id);
            if (expectedSource is null && expectedFixed is null)
            {
                if (rule is not null && (IsPlaceholder(rule.DefaultValue) || IsPlaceholder(rule.FixedValue))) return false;
                continue;
            }

            if (rule is null
                || !string.Equals(rule.SourceFieldPath, expectedSource ?? string.Empty, StringComparison.OrdinalIgnoreCase)
                || !string.Equals(rule.FixedValue, expectedFixed, StringComparison.Ordinal))
            {
                return false;
            }
        }

        return rules.All(x => !IsPlaceholder(x.DefaultValue) && !IsPlaceholder(x.FixedValue));
    }

    private static string? ProcTransaccionesFixedValueFor(string parameterPath)
        => parameterPath switch
        {
            "DISCRE" => "V",
            "IREVER" => "0",
            _ => null
        };

    private static bool IsPlaceholder(string? value)
        => string.Equals(value?.Trim(), "SEED", StringComparison.OrdinalIgnoreCase)
            || string.Equals(value?.Trim(), "PLACEHOLDER", StringComparison.OrdinalIgnoreCase)
            || string.Equals(value?.Trim(), "TODO", StringComparison.OrdinalIgnoreCase);

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

        IntegrationMappingSet? compatiblePublishedSet = null;
        foreach (var published in publishedSets)
        {
            var rules = await _context.IntegrationMappingRules
                .AsNoTracking()
                .Where(x => x.MappingSetId == published.Id)
                .ToListAsync(ct);

            if (compatiblePublishedSet is null
                && IsRegistrarRespuestaMappingCompatible(parameters, rules))
            {
                compatiblePublishedSet = published;
            }
        }

        var invalidPublishedSets = compatiblePublishedSet is null
            ? publishedSets
            : publishedSets.Where(x => x.Id != compatiblePublishedSet.Id).ToList();

        if (compatiblePublishedSet is not null)
        {
            if (invalidPublishedSets.Count == 0)
            {
                return;
            }

            foreach (var invalidPublishedSet in invalidPublishedSets)
            {
                invalidPublishedSet.Status = IntegrationMappingSetStatusEnum.Archived;
                invalidPublishedSet.IsActive = false;
            }

            await _context.SaveChangesAsync(ct);
            foreach (var invalidPublishedSet in invalidPublishedSets)
            {
                _context.IntegrationMappingSetHistory.Add(await BuildHistoryAsync(invalidPublishedSet, ArchivedInvalidSeedContractAction, ct));
            }

            await _context.SaveChangesAsync(ct);
            return;
        }

        foreach (var invalidPublishedSet in invalidPublishedSets)
        {
            invalidPublishedSet.Status = IntegrationMappingSetStatusEnum.Archived;
            invalidPublishedSet.IsActive = false;
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

        foreach (var parameter in parameters)
        {
            var sourcePath = RegistrarRespuestaSourcePathFor(parameter.ParameterPath)
                ?? throw new InvalidOperationException($"No existe una fuente diferencial canonica para '{parameter.ParameterPath}'.");
            _context.IntegrationMappingRules.Add(new IntegrationMappingRule
            {
                MappingSetId = publishedSet.Id,
                MethodId = method.Id,
                ParameterId = parameter.Id,
                SourceKind = IntegrationSourceKindEnum.DifferentialResponse,
                SourceFieldPath = sourcePath,
                FixedValue = null,
                DefaultValue = null,
                Priority = 1,
                Enabled = true
            });
        }

        await _context.SaveChangesAsync(ct);
        foreach (var invalidPublishedSet in invalidPublishedSets)
        {
            _context.IntegrationMappingSetHistory.Add(await BuildHistoryAsync(invalidPublishedSet, ArchivedInvalidSeedContractAction, ct));
        }

        _context.IntegrationMappingSetHistory.Add(await BuildHistoryAsync(publishedSet, "SeedPublishedReferenceWsdl", ct));
        await _context.SaveChangesAsync(ct);
    }

    private static bool IsRegistrarRespuestaMappingCompatible(
        IReadOnlyCollection<IntegrationMethodParameter> parameters,
        IReadOnlyCollection<IntegrationMappingRule> rules)
    {
        var expectedParameterPaths = RegistrarRespuestaWsdlParameterPaths
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        var activeParameterPaths = parameters
            .Select(x => x.ParameterPath)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        if (parameters.Count != RegistrarRespuestaWsdlParameterPaths.Length
            || !activeParameterPaths.SetEquals(expectedParameterPaths)
            || activeParameterPaths.Overlaps(RegistrarRespuestaNonWsdlParameterPaths))
        {
            return false;
        }

        if (rules.Count != RegistrarRespuestaWsdlParameterPaths.Length
            || rules.Any(x => !x.Enabled)
            || rules.GroupBy(x => x.ParameterId).Any(x => x.Count() != 1))
        {
            return false;
        }

        foreach (var parameter in parameters)
        {
            var rule = rules.SingleOrDefault(x => x.ParameterId == parameter.Id);
            var expectedSourcePath = RegistrarRespuestaSourcePathFor(parameter.ParameterPath);
            if (rule is null
                || rule.MethodId != parameter.MethodId
                || rule.SourceKind != IntegrationSourceKindEnum.DifferentialResponse
                || !string.Equals(rule.SourceFieldPath, expectedSourcePath, StringComparison.OrdinalIgnoreCase)
                || rule.FixedValue is not null
                || rule.DefaultValue is not null)
            {
                return false;
            }
        }

        return true;
    }

    private async Task NormalizeRegistrarRespuestaHistoryActionsAsync(CancellationToken ct)
    {
        var method = await _context.IntegrationMethods
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Code == "WSAXON.RegistrarRespuestaTransaccion", ct);
        if (method is null)
        {
            return;
        }

        var previousActionRows = await _context.IntegrationMappingSetHistory
            .Where(x => x.MethodId == method.Id
                && x.Action == PreviousRegistrarArchiveAction)
            .ToListAsync(ct);
        foreach (var row in previousActionRows)
        {
            row.Action = ArchivedInvalidSeedContractAction;
        }

        if (previousActionRows.Count > 0)
        {
            await _context.SaveChangesAsync(ct);
        }
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
            "TIPTRAN" => "transaction.transactionCode",
            "BCORECEP" => "destinationInstitution.transitCodeNormalized",
            "BCOORIG" => "sourceInstitution.transitCodeNormalized",
            "NORIG" => "sourceInstitution.name",
            "NCTAORIG" => "entrydetails.accountnumber",
            "IDORIG" => "transaction.companyIdentification",
            "DESTRAN" => "procTransacciones.paymentInformation",
            "FECEFEC" => "transaction.effectiveEntryDate",
            "NCTARECEP" => "transaction.destinationAccountNumber",
            "MONTO" => "transaction.amount",
            "NRECEP" => "entryDetails.recipUserName",
            "IDRECEP" => "entryDetails.recipIdNumber",
            "INFPAG" => "procTransacciones.paymentInformation",
            "IDTRAN" => "transaction.traceSequenceNumber",
            "IDLOTE" => "procTransacciones.functionalBatchId",
            "IDCAMCOMPE" => "cycle.clearingHouseId",
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

    private async Task<IntegrationMappingSetHistory> BuildHistoryAsync(IntegrationMappingSet set, string action, CancellationToken ct)
    {
        var snapshot = await _snapshotBuilder.BuildAsync(set.Id, ct);
        return new IntegrationMappingSetHistory
        {
            MappingSetId = set.Id,
            MethodId = set.MethodId,
            Version = set.Version,
            Status = set.Status,
            Action = action,
            PerformedBy = "seed",
            PerformedAtUtc = DateTime.UtcNow,
            SnapshotJson = snapshot.SnapshotJson,
            SnapshotHash = snapshot.SnapshotHash
        };
    }

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
        if (sourcePath.StartsWith("cycle.", StringComparison.OrdinalIgnoreCase)) return IntegrationSourceKindEnum.Cycle;
        if (sourcePath.StartsWith("destinationInstitution.", StringComparison.OrdinalIgnoreCase)) return IntegrationSourceKindEnum.Transaction;
        if (sourcePath.StartsWith("sourceInstitution.", StringComparison.OrdinalIgnoreCase)) return IntegrationSourceKindEnum.Transaction;
        if (sourcePath.StartsWith("procTransacciones.", StringComparison.OrdinalIgnoreCase)) return IntegrationSourceKindEnum.Transaction;
        return IntegrationSourceKindEnum.Constant;
    }
}

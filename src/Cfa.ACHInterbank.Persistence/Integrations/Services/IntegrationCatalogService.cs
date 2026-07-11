using Cfa.ACHInterbank.Application.Integrations.Dtos;
using Cfa.ACHInterbank.Application.Integrations.Interfaces;
using Cfa.ACHInterbank.Domain.Entities.Integrations;
using Cfa.ACHInterbank.Domain.Models.ACH;
using Cfa.ACHInterbank.Domain.Models.Configurations;
using Cfa.ACHInterbank.Persistence.DataBase;
using Microsoft.EntityFrameworkCore;

namespace Cfa.ACHInterbank.Persistence.Integrations.Services;

[Scoped]
public class IntegrationCatalogService : IIntegrationCatalogService
{
    private readonly AchDbContext _context;

    private static readonly IReadOnlyCollection<IntegrationTransformationCatalogDto> Transformations =
    [
        new("Trim", "Trim", "Elimina espacios al inicio y final", false),
        new("Uppercase", "Uppercase", "Convierte a mayúsculas", false),
        new("Lowercase", "Lowercase", "Convierte a minúsculas", false),
        new("PadLeft", "PadLeft", "Rellena por la izquierda", true),
        new("PadRight", "PadRight", "Rellena por la derecha", true),
        new("Substring", "Substring", "Extrae subcadena según máscara", true),
        new("Concat", "Concat", "Concatena valores", true, true),
        new("DateFormat", "DateFormat", "Formatea fecha", true),
        new("NumericFormat", "NumericFormat", "Formatea número", true),
        new("NullIfEmpty", "NullIfEmpty", "Devuelve null si cadena vacía", false),
        new("DefaultIfNull", "DefaultIfNull", "Usa default si valor null", true)
    ];

    public IntegrationCatalogService(AchDbContext context)
    {
        _context = context;
    }

    public async Task<IReadOnlyCollection<IntegrationMethodDto>> GetMethodsAsync(CancellationToken ct = default)
    {
        await EnsureSeedAsync(ct);
        var methods = await _context.Set<IntegrationMethod>()
            .AsNoTracking()
            .OrderBy(x => x.Id)
            .ToListAsync(ct);

        return methods
            .Select(x =>
            {
                var classification = ClassifyMethod(x.Code);
                return new IntegrationMethodDto(
                    x.Id,
                    x.Code,
                    x.DisplayName,
                    x.SoapClientCode,
                    x.IsActive,
                    classification.IntegrationKey,
                    classification.OperationKey,
                    classification.MappingDirection,
                    classification.MappingPurpose,
                    classification.FunctionalNature,
                    classification.FunctionalOriginator,
                    classification.MovesMoney);
            })
            .ToList();
    }

    public async Task<IReadOnlyCollection<IntegrationMethodParameterDto>> GetMethodParametersAsync(int methodId, CancellationToken ct = default)
    {
        await EnsureSeedAsync(ct);
        return await _context.Set<IntegrationMethodParameter>()
            .AsNoTracking()
            .Where(x => x.MethodId == methodId && x.IsActive)
            .OrderBy(x => x.SortOrder)
            .ThenBy(x => x.ParameterPath)
            .Select(x => new IntegrationMethodParameterDto(
                x.Id,
                x.MethodId,
                x.ParameterPath,
                x.DisplayName,
                x.DescriptionEs,
                x.Category,
                x.ExampleValue,
                x.UiHelpText,
                x.DataType,
                x.Direction,
                x.Cardinality,
                x.Required,
                x.SortOrder,
                x.IsActive))
            .ToListAsync(ct);
    }

    public async Task<IReadOnlyCollection<IntegrationSourceCatalogFieldDto>> GetSourceCatalogAsync(int? methodId, CancellationToken ct = default)
    {
        await EnsureSeedAsync(ct);
        return await _context.Set<IntegrationSourceCatalogField>()
            .AsNoTracking()
            .Where(x => x.IsActive)
            .Where(x => !methodId.HasValue || x.MethodId == null || x.MethodId == methodId.Value)
            .OrderBy(x => x.SourceKind)
            .ThenBy(x => x.SortOrder)
            .ThenBy(x => x.FieldPath)
            .Select(x => new IntegrationSourceCatalogFieldDto(
                x.Id,
                x.MethodId,
                x.SourceKind,
                x.EntityName,
                x.FieldPath,
                x.DisplayName,
                x.DataType,
                x.Cardinality,
                x.Nullable,
                x.SortOrder,
                x.IsActive))
            .ToListAsync(ct);
    }

    public Task<IReadOnlyCollection<IntegrationTransformationCatalogDto>> GetTransformationsAsync(CancellationToken ct = default)
        => Task.FromResult(Transformations);

    private async Task EnsureSeedAsync(CancellationToken ct)
        => await new IntegrationCatalogBootstrapper(_context).EnsureAsync(ct);

    private async Task<IntegrationMethod> EnsureMethodAsync(string code, string displayName, string soapClientCode, CancellationToken ct)
    {
        var existing = await _context.Set<IntegrationMethod>()
            .FirstOrDefaultAsync(x => x.Code == code, ct);

        if (existing is null)
        {
            existing = new IntegrationMethod
            {
                Code = code,
                DisplayName = displayName,
                SoapClientCode = soapClientCode,
                IsActive = true
            };
            _context.Set<IntegrationMethod>().Add(existing);
            await _context.SaveChangesAsync(ct);
        }
        else
        {
            existing.DisplayName = displayName;
            existing.SoapClientCode = soapClientCode;
            existing.IsActive = true;
        }

        return existing;
    }

    private async Task EnsureParametersAsync(int methodId, IReadOnlyCollection<ParameterSeedSpec> specs, CancellationToken ct)
    {
        var existing = await _context.Set<IntegrationMethodParameter>()
            .Where(x => x.MethodId == methodId)
            .ToListAsync(ct);

        var byKey = existing.ToDictionary(x => x.ParameterPath, StringComparer.OrdinalIgnoreCase);

        foreach (var spec in specs)
        {
            if (!byKey.TryGetValue(spec.TechnicalName, out var parameter))
            {
                parameter = new IntegrationMethodParameter
                {
                    MethodId = methodId,
                    ParameterPath = spec.TechnicalName
                };
                _context.Set<IntegrationMethodParameter>().Add(parameter);
            }

            parameter.DisplayName = spec.DisplayNameEs;
            parameter.DescriptionEs = spec.DescriptionEs;
            parameter.Category = spec.Category;
            parameter.ExampleValue = spec.ExampleValue;
            parameter.UiHelpText = spec.UiHelpText;
            parameter.DataType = spec.DataType;
            parameter.Direction = spec.Direction;
            parameter.Cardinality = IntegrationParameterCardinalityEnum.Scalar;
            parameter.Required = spec.Required;
            parameter.SortOrder = spec.SortOrder;
            parameter.IsActive = true;
        }

        var allowed = specs.Select(x => x.TechnicalName).ToHashSet(StringComparer.OrdinalIgnoreCase);
        foreach (var stale in existing.Where(x => !allowed.Contains(x.ParameterPath)))
        {
            stale.IsActive = false;
        }
    }

    private async Task EnsureSourceCatalogAsync(int methodId, IReadOnlyCollection<SourceSeedSpec> specs, CancellationToken ct)
    {
        var existing = await _context.Set<IntegrationSourceCatalogField>()
            .Where(x => x.MethodId == methodId)
            .ToListAsync(ct);

        var byPath = existing.ToDictionary(x => x.FieldPath, StringComparer.OrdinalIgnoreCase);

        foreach (var spec in specs)
        {
            if (!byPath.TryGetValue(spec.FieldPath, out var field))
            {
                field = new IntegrationSourceCatalogField
                {
                    MethodId = methodId,
                    FieldPath = spec.FieldPath
                };
                _context.Set<IntegrationSourceCatalogField>().Add(field);
            }

            field.SourceKind = spec.SourceKind;
            field.EntityName = spec.EntityName;
            field.DisplayName = spec.DisplayName;
            field.DataType = spec.DataType;
            field.Cardinality = IntegrationParameterCardinalityEnum.Scalar;
            field.Nullable = spec.Nullable;
            field.SortOrder = spec.SortOrder;
            field.IsActive = true;
        }

        var allowed = specs.Select(x => x.FieldPath).ToHashSet(StringComparer.OrdinalIgnoreCase);
        foreach (var stale in existing.Where(x => !allowed.Contains(x.FieldPath)))
        {
            stale.IsActive = false;
        }
    }

    private async Task EnsureAdditionalSourceCatalogAsync(int methodId, IReadOnlyCollection<SourceSeedSpec> specs, CancellationToken ct)
    {
        var existing = await _context.Set<IntegrationSourceCatalogField>()
            .Where(x => x.MethodId == methodId)
            .ToListAsync(ct);

        var byPath = existing.ToDictionary(x => x.FieldPath, StringComparer.OrdinalIgnoreCase);

        foreach (var spec in specs)
        {
            if (!byPath.TryGetValue(spec.FieldPath, out var field))
            {
                field = new IntegrationSourceCatalogField
                {
                    MethodId = methodId,
                    FieldPath = spec.FieldPath
                };
                _context.Set<IntegrationSourceCatalogField>().Add(field);
            }

            field.SourceKind = spec.SourceKind;
            field.EntityName = spec.EntityName;
            field.DisplayName = spec.DisplayName;
            field.DataType = spec.DataType;
            field.Cardinality = IntegrationParameterCardinalityEnum.Scalar;
            field.Nullable = spec.Nullable;
            field.SortOrder = spec.SortOrder;
            field.IsActive = true;
        }
    }

    private static IReadOnlyCollection<ParameterSeedSpec> BuildProcContrapartidasTechnicalCatalog()
    {
        var i = 1;
        return
        [
            Spec("OFNIT", "NIT origen", "Identificador tributario de la entidad origen.", "Identificación", "900123456", "Seleccione el NIT institucional a enviar.", "string", true, i++),
            Spec("OFEMP", "Código empresa", "Código de empresa/origen definido para ACH.", "Identificación", "EMP001", "Use el código empresarial registrado en ACH.", "string", true, i++),
            Spec("OFCTA", "Cuenta origen", "Cuenta origen del movimiento.", "Cuenta", "001234567890", "Mapee la cuenta origen del sistema.", "string", true, i++),
            Spec("OFDD", "Débito/Crédito", "Indicador de naturaleza de operación.", "Control", "D", "Use el indicador de negocio aprobado (D/C).", "string", true, i++),
            Spec("OFFECHEFEC", "Fecha efectiva", "Fecha efectiva de compensación.", "Fechas", "20260413", "Formatee la fecha según regla operativa ACH.", "string", true, i++),
            Spec("OFMONDEB", "Monto débito", "Monto débito de la operación.", "Montos", "150000.25", "Debe representar solo débitos.", "decimal", true, i++),
            Spec("OFMONCRE", "Monto crédito", "Monto crédito de la operación.", "Montos", "0", "Debe representar solo créditos.", "decimal", true, i++),
            Spec("OFIDARCH", "Id archivo", "Identificador del archivo de envío.", "Control", "1001", "Mapee el id de archivo del lote de salida.", "int", true, i++),
            Spec("OFIDLOT", "Id lote", "Identificador del lote ACH.", "Control", "2001", "Mapee el id del lote funcional.", "int", true, i++),
            Spec("OFST", "Estado origen", "Estado funcional del registro origen.", "Estado", "PENDIENTE", "Utilice estado de negocio vigente.", "string", true, i++),
            Spec("OFIDTX", "Id transacción origen", "Identificador de transacción origen.", "Identificación", "TX-2026-0001", "Use el identificador único transaccional.", "string", true, i++),
            Spec("OFIDREVER", "Id reverso origen", "Identificador de reverso origen.", "Control", "0", "Use 0 si no aplica reverso.", "int", true, i++),
            Spec("OFIDEBAPLI", "Id débito aplicado", "Id interno de débito aplicado.", "Control", "345", "Mapee id de aplicación de débito.", "int", true, i++),
            Spec("OFIDCAMCOMPE", "Id cámara compensación", "Identificador de cámara compensadora.", "Control", "12", "Use id de cámara del ciclo.", "int", true, i++),
            Spec("OFDIRECCIONIP", "Dirección IP origen", "IP de origen de la operación.", "Seguridad", "10.10.10.1", "IP del origen según trazabilidad.", "string", true, i++),
            Spec("OFLIBRE", "Campo libre", "Campo libre de negocio (texto).", "Complementario", "Observación", "Use solo si su flujo lo requiere.", "string", true, i++),
            Spec("OFLIBRE1", "Campo libre numérico", "Campo libre de negocio (numérico).", "Complementario", "1", "Use valor numérico controlado.", "int", true, i++),
            Spec("ANSIDLOTE", "Id lote respuesta", "Campo contractual para identificar lote de respuesta.", "Respuesta esperada", "0", "Campo reservado por contrato legado.", "int", false, i++),
            Spec("ANSST", "Estado respuesta", "Campo contractual de estado de respuesta.", "Respuesta esperada", "", "Campo reservado por contrato legado.", "string", false, i++),
            Spec("ANCLC", "Código local respuesta", "Campo contractual de código local.", "Respuesta esperada", "", "Campo reservado por contrato legado.", "string", false, i++),
            Spec("ANSIDTX", "Id transacción respuesta", "Campo contractual de id transacción de respuesta.", "Respuesta esperada", "", "Campo reservado por contrato legado.", "string", false, i++),
            Spec("ANSIDREVER", "Id reverso respuesta", "Campo contractual de reverso de respuesta.", "Respuesta esperada", "0", "Campo reservado por contrato legado.", "int", false, i++)
        ];
    }

    private static IReadOnlyCollection<ParameterSeedSpec> BuildProcTransaccionesTechnicalCatalog()
    {
        var i = 1;
        return
        [
            Spec("TREG", "Tipo de registro", "Tipo de registro ACH transacción.", "Entrada transacción", "6", "Tipo de registro según layout ACH.", "string", true, i++),
            Spec("TIPTRAN", "Tipo transacción", "Código de tipo de transacción.", "Entrada transacción", "22", "Código según tabla operativa.", "int", true, i++),
            Spec("BCORECEP", "Banco receptor", "Código banco receptor.", "Entrada transacción", "1007", "Código bancario receptor.", "int", true, i++),
            Spec("BCOORIG", "Banco origen", "Código banco originador.", "Entrada transacción", "1001", "Código bancario origen.", "int", true, i++),
            Spec("NORIG", "Nombre origen", "Nombre del originador.", "Entrada transacción", "EMPRESA ORIGEN", "Nombre homologado del originador.", "string", true, i++),
            Spec("NCTAORIG", "Cuenta origen", "Número de cuenta origen.", "Entrada transacción", "001234567890", "Cuenta origen validada cuando venga informada.", "string", false, i++),
            Spec("IDORIG", "Id origen", "Identificación origen.", "Entrada transacción", "900123456", "Documento/NIT origen.", "string", true, i++),
            Spec("DESTRAN", "Descripción", "Descripción de transacción.", "Entrada transacción", "PAGO NOMINA", "Descripción visible de negocio.", "string", true, i++),
            Spec("FECEFEC", "Fecha efectiva", "Fecha efectiva en formato entero.", "Entrada transacción", "20260413", "Fecha efectiva según especificación.", "int", true, i++),
            Spec("NCTARECEP", "Cuenta receptor", "Cuenta destino/receptor.", "Entrada transacción", "009998887777", "Cuenta del receptor.", "string", true, i++),
            Spec("MONTO", "Monto", "Monto de la transacción.", "Entrada transacción", "250000.75", "Monto exacto con formato numérico válido.", "double", true, i++),
            Spec("NRECEP", "Nombre receptor", "Nombre del receptor.", "Entrada transacción", "JUAN PEREZ", "Nombre receptor en mayúsculas.", "string", true, i++),
            Spec("IDRECEP", "Id receptor", "Identificación del receptor.", "Entrada transacción", "1099001122", "Documento receptor.", "string", true, i++),
            Spec("DISCRE", "Discrecional", "Campo discrecional receptor.", "Entrada transacción", "", "Campo opcional según operación.", "string", false, i++),
            Spec("CONV", "Convenio", "Código de convenio.", "Entrada transacción", "CNV01", "Convenio aplicable a la operación.", "string", true, i++),
            Spec("PROD", "Producto", "Código de producto.", "Entrada transacción", "ACH", "Producto financiero asociado.", "string", true, i++),
            Spec("INFPAG", "Información pago", "Información adicional de pago.", "Entrada transacción", "NOMINA ABRIL", "Texto informativo del pago.", "string", true, i++),
            Spec("IDTRAN", "Id transacción", "Identificador numérico de transacción.", "Entrada transacción", "9876543210", "Id único de transacción.", "long", true, i++),
            Spec("IDLOTE", "Id lote", "Identificador de lote.", "Entrada transacción", "LOTE-001", "Id de lote operacional.", "string", true, i++),
            Spec("REGLOTE", "Registro lote", "Registro secuencial de lote.", "Entrada transacción", "1", "Número de registro en lote.", "long", true, i++),
            Spec("IREVER", "Indicador reverso", "Indicador de reverso.", "Entrada transacción", "0", "0 normal, 1 reverso.", "int", true, i++),
            Spec("LIBRE", "Campo libre", "Campo libre texto.", "Entrada transacción", "OBS", "Campo complementario opcional.", "string", true, i++),
            Spec("IDCAMCOMPE", "Id cámara", "Id cámara compensadora.", "Entrada transacción", "12", "Id de cámara vigente.", "int", true, i++),
            Spec("DIRECCIONIP", "Dirección IP", "IP de origen.", "Entrada transacción", "10.10.10.1", "IP para trazabilidad.", "string", true, i++),
            Spec("LIBRE1", "Campo libre numérico", "Campo libre numérico.", "Entrada transacción", "1", "Campo complementario numérico.", "int", true, i++),
            Spec("ILR", "Indicador ILR", "Indicador legacy ILR observado en tramas de Proc_Transacciones.", "Entrada transacción", "A", "Campo opcional; valores observados A/B.", "string", false, i++),
            Spec("RTAACH", "Respuesta ACH", "Campo contractual de respuesta ACH.", "Respuesta esperada", "", "Campo de salida reservado por contrato legado.", "string", false, i++, IntegrationParameterDirectionEnum.Output),
            Spec("RTALOC", "Respuesta local", "Campo contractual de respuesta local.", "Respuesta esperada", "", "Campo de salida reservado por contrato legado.", "string", false, i, IntegrationParameterDirectionEnum.Output)
        ];
    }

    private static IReadOnlyCollection<ParameterSeedSpec> BuildRegistrarRespuestaTransaccionTechnicalCatalog()
    {
        var i = 1;
        return
        [
            Spec("idCanal", "Id canal", "Identificador numerico del canal que registra la respuesta.", "Respuesta transaccion", "1", "Use el id de canal homologado para respuestas ACH.", "int", true, i++),
            Spec("nombreCanal", "Nombre canal", "Nombre del canal que registra la respuesta.", "Respuesta transaccion", "ACH", "Use el nombre de canal homologado.", "string", true, i++),
            Spec("idTransaccion", "Id transaccion", "Identificador de la transaccion notificada.", "Respuesta transaccion", "TX-2026-0001", "Debe corresponder a la transaccion diferencial recibida.", "string", true, i++),
            Spec("idEstado", "Id estado", "Identificador interno/externo homologado del estado a registrar.", "Respuesta transaccion", "1", "Use el estado homologado por la tabla de respuestas.", "int", true, i++),
            Spec("causal", "Causal", "Codigo causal asociado a la respuesta, si aplica.", "Respuesta transaccion", "R03", "Mapee causal/codigo homologado cuando aplique.", "string", false, i++),
            Spec("idTransaccionAxon", "Id transaccion Axon", "Identificador de transaccion del servicio externo Axon.", "Respuesta transaccion", "1001", "Use el id de transaccion servicio externo recibido.", "int", true, i++),
            Spec("descripcionCausal", "Descripcion causal", "Descripcion funcional de la causal, si aplica.", "Respuesta transaccion", "Cuenta no localizada", "Use la descripcion homologada o externa disponible.", "string", false, i++)
        ];
    }

    private static IReadOnlyCollection<SourceSeedSpec> BuildBusinessSourceCatalog(int methodId)
    {
        var order = 1;
        return
        [
            Source(methodId, IntegrationSourceKindEnum.Transaction, nameof(AchTransaction), "transaction.id", "Id transacción", "int", false, order++),
            Source(methodId, IntegrationSourceKindEnum.Transaction, nameof(AchTransaction), "transaction.transactionExternalId", "Id operación cliente", "string", true, order++),
            Source(methodId, IntegrationSourceKindEnum.Transaction, nameof(AchTransaction), "transaction.reference", "Referencia transacción", "string", true, order++),
            Source(methodId, IntegrationSourceKindEnum.Transaction, nameof(AchTransaction), "transaction.amount", "Monto transacción", "decimal", false, order++),
            Source(methodId, IntegrationSourceKindEnum.Transaction, nameof(AchTransaction), "transaction.traceNumber", "Trazabilidad transacción", "string", false, order++),
            Source(methodId, IntegrationSourceKindEnum.Transaction, nameof(AchTransaction), "transaction.companyIdentification", "NIT/Id empresa origen", "string", false, order++),
            Source(methodId, IntegrationSourceKindEnum.Transaction, nameof(AchTransaction), "transaction.sourceAccountNumber", "Cuenta origen", "string", false, order++),
            Source(methodId, IntegrationSourceKindEnum.Transaction, nameof(AchTransaction), "transaction.effectiveEntryDate", "Fecha efectiva transacción", "datetime", false, order++),
            Source(methodId, IntegrationSourceKindEnum.Batch, nameof(AchBatch), "batch.id", "Id lote", "int", false, order++),
            Source(methodId, IntegrationSourceKindEnum.Cycle, nameof(AchCycle), "cycle.id", "Id ciclo", "string", false, order++),
            Source(methodId, IntegrationSourceKindEnum.Cycle, nameof(AchCycle), "cycle.processingDate", "Fecha proceso ciclo", "datetime", false, order++),
            Source(methodId, IntegrationSourceKindEnum.ClearingHouse, nameof(ClearingHouse), "clearinghouse.id", "Id cámara", "int", false, order++),
            Source(methodId, IntegrationSourceKindEnum.ClearingHouse, nameof(ClearingHouse), "clearinghouse.code", "Código cámara", "string", false, order++),
            Source(methodId, IntegrationSourceKindEnum.Cycle, "ExecutionContext", "execution.datetimeUtc", "Fecha/hora ejecución UTC", "datetime", false, order++),
            Source(methodId, IntegrationSourceKindEnum.Cycle, "ExecutionContext", "execution.dateYyyyMMdd", "Fecha ejecución yyyymmdd", "string", false, order++),
            Source(methodId, IntegrationSourceKindEnum.Constant, "Constant", "constant.value", "Valor fijo", "string", true, order++)
        ];
    }

    private static IReadOnlyCollection<SourceSeedSpec> BuildNachaSourceCatalog(int methodId)
    {
        var order = 1000;
        return
        [
            Source(methodId, IntegrationSourceKindEnum.NachaHeader, nameof(NachaHeader), "nachaHeaders.nachaId", "Archivo NACHA-M > Encabezado > Id interno", "string", true, order++),
            Source(methodId, IntegrationSourceKindEnum.NachaHeader, nameof(NachaHeader), "nachaHeaders.immediateOrigin", "Archivo NACHA-M > Encabezado > Originador inmediato", "string", true, order++),
            Source(methodId, IntegrationSourceKindEnum.NachaHeader, nameof(NachaHeader), "nachaHeaders.immediateDestination", "Archivo NACHA-M > Encabezado > Destino inmediato", "string", true, order++),
            Source(methodId, IntegrationSourceKindEnum.NachaHeader, nameof(NachaHeader), "nachaHeaders.fileIdModifier", "Archivo NACHA-M > Encabezado > Modificador archivo", "string", true, order++),
            Source(methodId, IntegrationSourceKindEnum.NachaHeader, nameof(NachaHeader), "nachaHeaders.referenceCode", "Archivo NACHA-M > Encabezado > Referencia", "string", true, order++),
            Source(methodId, IntegrationSourceKindEnum.BatchHeader, nameof(BatchHeader), "batchHeaders.companyId", "Archivo NACHA-M > Lote > Id empresa", "string", true, order++),
            Source(methodId, IntegrationSourceKindEnum.BatchHeader, nameof(BatchHeader), "batchHeaders.companyName", "Archivo NACHA-M > Lote > Empresa", "string", true, order++),
            Source(methodId, IntegrationSourceKindEnum.BatchHeader, nameof(BatchHeader), "batchHeaders.standardEntryClassCode", "Archivo NACHA-M > Lote > SEC", "string", true, order++),
            Source(methodId, IntegrationSourceKindEnum.BatchHeader, nameof(BatchHeader), "batchHeaders.companyEntryDescription", "Archivo NACHA-M > Lote > Descripcion", "string", true, order++),
            Source(methodId, IntegrationSourceKindEnum.BatchHeader, nameof(BatchHeader), "batchHeaders.effectiveEntryDate", "Archivo NACHA-M > Lote > Fecha efectiva", "string", true, order++),
            Source(methodId, IntegrationSourceKindEnum.BatchHeader, nameof(BatchHeader), "batchHeaders.originParticipantEntityCode", "Archivo NACHA-M > Lote > Entidad originadora", "string", true, order++),
            Source(methodId, IntegrationSourceKindEnum.BatchHeader, nameof(BatchHeader), "batchHeaders.batchNumber", "Archivo NACHA-M > Lote > Numero lote", "int", true, order++),
            Source(methodId, IntegrationSourceKindEnum.EntryDetail, nameof(EntryDetail), "entryDetails.transactionCode", "Archivo NACHA-M > Detalle > Codigo transaccion", "string", true, order++),
            Source(methodId, IntegrationSourceKindEnum.EntryDetail, nameof(EntryDetail), "entryDetails.receivingParticipantEntityCode", "Archivo NACHA-M > Detalle > Entidad receptora", "string", true, order++),
            Source(methodId, IntegrationSourceKindEnum.EntryDetail, nameof(EntryDetail), "entryDetails.accountNumber", "Archivo NACHA-M > Detalle > Cuenta", "string", true, order++),
            Source(methodId, IntegrationSourceKindEnum.EntryDetail, nameof(EntryDetail), "entryDetails.amount", "Archivo NACHA-M > Detalle > Monto", "decimal", true, order++),
            Source(methodId, IntegrationSourceKindEnum.EntryDetail, nameof(EntryDetail), "entryDetails.recipIdNumber", "Archivo NACHA-M > Detalle > Identificacion receptor", "string", true, order++),
            Source(methodId, IntegrationSourceKindEnum.EntryDetail, nameof(EntryDetail), "entryDetails.recipUserName", "Archivo NACHA-M > Detalle > Nombre receptor", "string", true, order++),
            Source(methodId, IntegrationSourceKindEnum.EntryDetail, nameof(EntryDetail), "entryDetails.sequenceNumber", "Archivo NACHA-M > Detalle > Trace number", "string", true, order++),
            Source(methodId, IntegrationSourceKindEnum.AddendaRecord, nameof(AddendaRecord), "addendaRecords.infofromOriginator", "Archivo NACHA-M > Addenda > Informacion originador", "string", true, order++),
            Source(methodId, IntegrationSourceKindEnum.AddendaRecord, nameof(AddendaRecord), "addendaRecords.invoiceOrAccountNumber", "Archivo NACHA-M > Addenda > Factura/cuenta", "string", true, order++),
            Source(methodId, IntegrationSourceKindEnum.AddendaRecord, nameof(AddendaRecord), "addendaRecords.returnReasonCode", "Archivo NACHA-M > Addenda > Causal retorno", "string", true, order++),
            Source(methodId, IntegrationSourceKindEnum.AddendaRecord, nameof(AddendaRecord), "addendaRecords.originalTraceNumber", "Archivo NACHA-M > Addenda > Trace original", "string", true, order++),
            Source(methodId, IntegrationSourceKindEnum.BatchControl, nameof(BatchControl), "batchControls.entryAddendaCount", "Archivo NACHA-M > Control lote > Conteo entradas/addenda", "int", true, order++),
            Source(methodId, IntegrationSourceKindEnum.BatchControl, nameof(BatchControl), "batchControls.entryHash", "Archivo NACHA-M > Control lote > Hash entradas", "long", true, order++),
            Source(methodId, IntegrationSourceKindEnum.BatchControl, nameof(BatchControl), "batchControls.totalDebitAmount", "Archivo NACHA-M > Control lote > Total debitos", "decimal", true, order++),
            Source(methodId, IntegrationSourceKindEnum.BatchControl, nameof(BatchControl), "batchControls.totalCreditAmount", "Archivo NACHA-M > Control lote > Total creditos", "decimal", true, order++),
            Source(methodId, IntegrationSourceKindEnum.FileControl, nameof(FileControl), "fileControls.batchCount", "Archivo NACHA-M > Control archivo > Conteo lotes", "int", true, order++),
            Source(methodId, IntegrationSourceKindEnum.FileControl, nameof(FileControl), "fileControls.blockCount", "Archivo NACHA-M > Control archivo > Block count", "int", true, order++),
            Source(methodId, IntegrationSourceKindEnum.FileControl, nameof(FileControl), "fileControls.entryAddendaCount", "Archivo NACHA-M > Control archivo > Conteo entradas/addenda", "int", true, order++),
            Source(methodId, IntegrationSourceKindEnum.FileControl, nameof(FileControl), "fileControls.entryHash", "Archivo NACHA-M > Control archivo > Hash entradas", "long", true, order++),
            Source(methodId, IntegrationSourceKindEnum.FileControl, nameof(FileControl), "fileControls.totalDebitAmount", "Archivo NACHA-M > Control archivo > Total debitos", "decimal", true, order++),
            Source(methodId, IntegrationSourceKindEnum.FileControl, nameof(FileControl), "fileControls.totalCreditAmount", "Archivo NACHA-M > Control archivo > Total creditos", "decimal", true, order++),
            Source(methodId, IntegrationSourceKindEnum.Prenotification, nameof(AchTransaction), "prenotification.reference", "Prenotificacion > Referencia", "string", true, order++),
            Source(methodId, IntegrationSourceKindEnum.Prenotification, nameof(AchTransaction), "prenotification.state", "Prenotificacion > Estado", "string", true, order++),
            Source(methodId, IntegrationSourceKindEnum.DifferentialResponse, "AchResponse", "differentialResponse.idTransaccion", "Respuesta diferencial > Id transaccion", "string", true, order++),
            Source(methodId, IntegrationSourceKindEnum.DifferentialResponse, "AchResponse", "differentialResponse.idCanal", "Respuesta diferencial > Id canal", "int", false, order++),
            Source(methodId, IntegrationSourceKindEnum.DifferentialResponse, "AchResponse", "differentialResponse.nombreCanal", "Respuesta diferencial > Nombre canal", "string", false, order++),
            Source(methodId, IntegrationSourceKindEnum.DifferentialResponse, "AchResponse", "differentialResponse.idEstado", "Respuesta diferencial > Id estado", "int", false, order++),
            Source(methodId, IntegrationSourceKindEnum.DifferentialResponse, "AchResponse", "differentialResponse.codigoEstadoExterno", "Respuesta diferencial > Estado externo", "string", true, order++),
            Source(methodId, IntegrationSourceKindEnum.DifferentialResponse, "AchResponse", "differentialResponse.codigoCausalExterna", "Respuesta diferencial > Causal externa", "string", true, order++),
            Source(methodId, IntegrationSourceKindEnum.DifferentialResponse, "AchResponse", "differentialResponse.idTransaccionServicioExterno", "Respuesta diferencial > Id transaccion servicio externo", "int", false, order++),
            Source(methodId, IntegrationSourceKindEnum.DifferentialResponse, "AchResponse", "differentialResponse.descripcionCausalExterna", "Respuesta diferencial > Descripcion causal externa", "string", true, order++)
        ];
    }

    private static ParameterSeedSpec Spec(
        string technicalName,
        string displayNameEs,
        string descriptionEs,
        string category,
        string exampleValue,
        string uiHelpText,
        string dataType,
        bool required,
        int sortOrder,
        IntegrationParameterDirectionEnum direction = IntegrationParameterDirectionEnum.Input)
        => new(technicalName, displayNameEs, descriptionEs, category, exampleValue, uiHelpText, dataType, required, sortOrder, direction);

    private static SourceSeedSpec Source(
        int methodId,
        IntegrationSourceKindEnum sourceKind,
        string entityName,
        string fieldPath,
        string displayName,
        string dataType,
        bool nullable,
        int sortOrder)
        => new(methodId, sourceKind, entityName, fieldPath, displayName, dataType, nullable, sortOrder);

    private static IntegrationMethodClassification ClassifyMethod(string methodCode)
    {
        var parts = (methodCode ?? string.Empty).Split('.', 2, StringSplitOptions.TrimEntries);
        var integrationKey = (parts.Length > 0 ? parts[0] : string.Empty) ?? string.Empty;
        var operationKey = (parts.Length > 1 ? parts[1] : methodCode) ?? string.Empty;

        return methodCode switch
        {
            "WSCFAACH.Proc_Contrapartidas" => new(
                integrationKey,
                operationKey,
                "OutboundRequest",
                "MonetaryDebitRequest",
                "Debito monetario",
                "CFA originadora",
                true),
            "WSCFAACH.Proc_Transacciones" => new(
                integrationKey,
                operationKey,
                "OutboundRequest",
                "MonetaryCreditRequest",
                "Credito monetario",
                "Entidad financiera externa; CFA receptora",
                true),
            "WSAXON.RegistrarRespuestaTransaccion" => new(
                integrationKey,
                operationKey,
                "InboundResponse",
                "DifferentialResponseNotification",
                "Respuesta diferencial / notificacion",
                "Entidad/camara/proveedor externo",
                false),
            _ => new(
                integrationKey,
                operationKey,
                "Unclassified",
                "Unclassified",
                "No clasificado",
                "No definido",
                false)
        };
    }

    private sealed record ParameterSeedSpec(
        string TechnicalName,
        string DisplayNameEs,
        string DescriptionEs,
        string Category,
        string ExampleValue,
        string UiHelpText,
        string DataType,
        bool Required,
        int SortOrder,
        IntegrationParameterDirectionEnum Direction);

    private sealed record SourceSeedSpec(
        int MethodId,
        IntegrationSourceKindEnum SourceKind,
        string EntityName,
        string FieldPath,
        string DisplayName,
        string DataType,
        bool Nullable,
        int SortOrder);

    private sealed record IntegrationMethodClassification(
        string IntegrationKey,
        string OperationKey,
        string MappingDirection,
        string MappingPurpose,
        string FunctionalNature,
        string FunctionalOriginator,
        bool MovesMoney);
}

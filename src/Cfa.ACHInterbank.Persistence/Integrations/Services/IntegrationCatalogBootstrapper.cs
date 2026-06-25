using Cfa.ACHInterbank.Domain.Entities.Integrations;
using Cfa.ACHInterbank.Domain.Models.ACH;
using Cfa.ACHInterbank.Domain.Models.Configurations;
using Cfa.ACHInterbank.Persistence.DataBase;
using Microsoft.EntityFrameworkCore;

namespace Cfa.ACHInterbank.Persistence.Integrations.Services;

public sealed class IntegrationCatalogBootstrapper
{
    private readonly AchDbContext _context;

    public IntegrationCatalogBootstrapper(AchDbContext context)
    {
        _context = context;
    }

    public async Task EnsureAsync(CancellationToken ct = default)
    {
        var contrapartidas = await EnsureMethodAsync(
            code: "WSCFAACH.Proc_Contrapartidas",
            displayName: "Proc_Contrapartidas",
            soapClientCode: "WscfaachSoapClient",
            ct);

        var transacciones = await EnsureMethodAsync(
            code: "WSCFAACH.Proc_Transacciones",
            displayName: "Proc_Transacciones",
            soapClientCode: "WscfaachSoapClient",
            ct);

        var respuestasTransacciones = await EnsureMethodAsync(
            code: "WSAXON.RegistrarRespuestaTransaccion",
            displayName: "RegistrarRespuestaTransaccion",
            soapClientCode: "WsAxonRespuestaTransaccionesSoapClient",
            ct);

        await EnsureParametersAsync(contrapartidas.Id, BuildProcContrapartidasTechnicalCatalog(), ct);
        await EnsureParametersAsync(transacciones.Id, BuildProcTransaccionesTechnicalCatalog(), ct);
        await EnsureParametersAsync(respuestasTransacciones.Id, BuildRegistrarRespuestaTransaccionTechnicalCatalog(), ct);
        await EnsureSourceCatalogAsync(contrapartidas.Id, BuildBusinessSourceCatalog(contrapartidas.Id), ct);
        await EnsureSourceCatalogAsync(transacciones.Id, BuildBusinessSourceCatalog(transacciones.Id), ct);
        await EnsureSourceCatalogAsync(respuestasTransacciones.Id, BuildBusinessSourceCatalog(respuestasTransacciones.Id), ct);
        await EnsureAdditionalSourceCatalogAsync(contrapartidas.Id, BuildNachaSourceCatalog(contrapartidas.Id), ct);
        await EnsureAdditionalSourceCatalogAsync(transacciones.Id, BuildNachaSourceCatalog(transacciones.Id), ct);
        await EnsureAdditionalSourceCatalogAsync(respuestasTransacciones.Id, BuildNachaSourceCatalog(respuestasTransacciones.Id), ct);

        await _context.SaveChangesAsync(ct);
    }

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
            Spec("OFNIT", "NIT origen", "Identificador tributario de la entidad origen.", "Identificacion", "900123456", "Seleccione el NIT institucional a enviar.", "string", true, i++),
            Spec("OFEMP", "Codigo empresa", "Codigo de empresa/origen definido para ACH.", "Identificacion", "EMP001", "Use el codigo empresarial registrado en ACH.", "string", true, i++),
            Spec("OFCTA", "Cuenta origen", "Cuenta origen del movimiento.", "Cuenta", "001234567890", "Mapee la cuenta origen del sistema.", "string", true, i++),
            Spec("OFDD", "Debito/Credito", "Indicador de naturaleza de operacion.", "Control", "D", "Use el indicador de negocio aprobado (D/C).", "string", true, i++),
            Spec("OFFECHEFEC", "Fecha efectiva", "Fecha efectiva de compensacion.", "Fechas", "20260413", "Formatee la fecha segun regla operativa ACH.", "string", true, i++),
            Spec("OFMONDEB", "Monto debito", "Monto debito de la operacion.", "Montos", "150000.25", "Debe representar solo debitos.", "decimal", true, i++),
            Spec("OFMONCRE", "Monto credito", "Monto credito de la operacion.", "Montos", "0", "Debe representar solo creditos.", "decimal", true, i++),
            Spec("OFIDARCH", "Id archivo", "Identificador del archivo de envio.", "Control", "1001", "Mapee el id de archivo del lote de salida.", "int", true, i++),
            Spec("OFIDLOT", "Id lote", "Identificador del lote ACH.", "Control", "2001", "Mapee el id del lote funcional.", "int", true, i++),
            Spec("OFST", "Estado origen", "Estado funcional del registro origen.", "Estado", "PENDIENTE", "Utilice estado de negocio vigente.", "string", true, i++),
            Spec("OFIDTX", "Id transaccion origen", "Identificador de transaccion origen.", "Identificacion", "TX-2026-0001", "Use el identificador unico transaccional.", "string", true, i++),
            Spec("OFIDREVER", "Id reverso origen", "Identificador de reverso origen.", "Control", "0", "Use 0 si no aplica reverso.", "int", true, i++),
            Spec("OFIDEBAPLI", "Id debito aplicado", "Id interno de debito aplicado.", "Control", "345", "Mapee id de aplicacion de debito.", "int", true, i++),
            Spec("OFIDCAMCOMPE", "Id camara compensacion", "Identificador de camara compensadora.", "Control", "12", "Use id de camara del ciclo.", "int", true, i++),
            Spec("OFDIRECCIONIP", "Direccion IP origen", "IP de origen de la operacion.", "Seguridad", "10.10.10.1", "IP del origen segun trazabilidad.", "string", true, i++),
            Spec("OFLIBRE", "Campo libre", "Campo libre de negocio (texto).", "Complementario", "Observacion", "Use solo si su flujo lo requiere.", "string", true, i++),
            Spec("OFLIBRE1", "Campo libre numerico", "Campo libre de negocio (numerico).", "Complementario", "1", "Use valor numerico controlado.", "int", true, i++),
            Spec("ANSIDLOTE", "Id lote respuesta", "Campo contractual para identificar lote de respuesta.", "Respuesta esperada", "0", "Campo reservado por contrato legado.", "int", false, i++),
            Spec("ANSST", "Estado respuesta", "Campo contractual de estado de respuesta.", "Respuesta esperada", "", "Campo reservado por contrato legado.", "string", false, i++),
            Spec("ANCLC", "Codigo local respuesta", "Campo contractual de codigo local.", "Respuesta esperada", "", "Campo reservado por contrato legado.", "string", false, i++),
            Spec("ANSIDTX", "Id transaccion respuesta", "Campo contractual de id transaccion de respuesta.", "Respuesta esperada", "", "Campo reservado por contrato legado.", "string", false, i++),
            Spec("ANSIDREVER", "Id reverso respuesta", "Campo contractual de reverso de respuesta.", "Respuesta esperada", "0", "Campo reservado por contrato legado.", "int", false, i++)
        ];
    }

    private static IReadOnlyCollection<ParameterSeedSpec> BuildProcTransaccionesTechnicalCatalog()
    {
        var i = 1;
        return
        [
            Spec("TREG", "Tipo de registro", "Tipo de registro ACH transaccion.", "Entrada transaccion", "6", "Tipo de registro segun layout ACH.", "string", true, i++),
            Spec("TIPTRAN", "Tipo transaccion", "Codigo de tipo de transaccion.", "Entrada transaccion", "22", "Codigo segun tabla operativa.", "int", true, i++),
            Spec("BCORECEP", "Banco receptor", "Codigo banco receptor.", "Entrada transaccion", "1007", "Codigo bancario receptor.", "int", true, i++),
            Spec("BCOORIG", "Banco origen", "Codigo banco originador.", "Entrada transaccion", "1001", "Codigo bancario origen.", "int", true, i++),
            Spec("NORIG", "Nombre origen", "Nombre del originador.", "Entrada transaccion", "EMPRESA ORIGEN", "Nombre homologado del originador.", "string", true, i++),
            Spec("NCTAORIG", "Cuenta origen", "Numero de cuenta origen.", "Entrada transaccion", "001234567890", "Cuenta origen validada.", "string", true, i++),
            Spec("IDORIG", "Id origen", "Identificacion origen.", "Entrada transaccion", "900123456", "Documento/NIT origen.", "string", true, i++),
            Spec("DESTRAN", "Descripcion", "Descripcion de transaccion.", "Entrada transaccion", "PAGO NOMINA", "Descripcion visible de negocio.", "string", true, i++),
            Spec("FECEFEC", "Fecha efectiva", "Fecha efectiva en formato entero.", "Entrada transaccion", "20260413", "Fecha efectiva segun especificacion.", "int", true, i++),
            Spec("NCTARECEP", "Cuenta receptor", "Cuenta destino/receptor.", "Entrada transaccion", "009998887777", "Cuenta del receptor.", "string", true, i++),
            Spec("MONTO", "Monto", "Monto de la transaccion.", "Entrada transaccion", "250000.75", "Monto exacto con formato numerico valido.", "double", true, i++),
            Spec("NRECEP", "Nombre receptor", "Nombre del receptor.", "Entrada transaccion", "JUAN PEREZ", "Nombre receptor en mayusculas.", "string", true, i++),
            Spec("IDRECEP", "Id receptor", "Identificacion del receptor.", "Entrada transaccion", "1099001122", "Documento receptor.", "string", true, i++),
            Spec("DISCRE", "Discrecional", "Campo discrecional receptor.", "Entrada transaccion", "", "Campo opcional segun operacion.", "string", true, i++),
            Spec("CONV", "Convenio", "Codigo de convenio.", "Entrada transaccion", "CNV01", "Convenio aplicable a la operacion.", "string", true, i++),
            Spec("PROD", "Producto", "Codigo de producto.", "Entrada transaccion", "ACH", "Producto financiero asociado.", "string", true, i++),
            Spec("INFPAG", "Informacion pago", "Informacion adicional de pago.", "Entrada transaccion", "NOMINA ABRIL", "Texto informativo del pago.", "string", true, i++),
            Spec("IDTRAN", "Id transaccion", "Identificador numerico de transaccion.", "Entrada transaccion", "9876543210", "Id unico de transaccion.", "long", true, i++),
            Spec("IDLOTE", "Id lote", "Identificador de lote.", "Entrada transaccion", "LOTE-001", "Id de lote operacional.", "string", true, i++),
            Spec("REGLOTE", "Registro lote", "Registro secuencial de lote.", "Entrada transaccion", "1", "Numero de registro en lote.", "long", true, i++),
            Spec("IREVER", "Indicador reverso", "Indicador de reverso.", "Entrada transaccion", "0", "0 normal, 1 reverso.", "int", true, i++),
            Spec("LIBRE", "Campo libre", "Campo libre texto.", "Entrada transaccion", "OBS", "Campo complementario opcional.", "string", true, i++),
            Spec("IDCAMCOMPE", "Id camara", "Id camara compensadora.", "Entrada transaccion", "12", "Id de camara vigente.", "int", true, i++),
            Spec("DIRECCIONIP", "Direccion IP", "IP de origen.", "Entrada transaccion", "10.10.10.1", "IP para trazabilidad.", "string", true, i++),
            Spec("LIBRE1", "Campo libre numerico", "Campo libre numerico.", "Entrada transaccion", "1", "Campo complementario numerico.", "int", true, i++),
            Spec("RTAACH", "Respuesta ACH", "Campo contractual de respuesta ACH.", "Respuesta esperada", "", "Campo reservado por contrato legado.", "string", true, i++),
            Spec("RTALOC", "Respuesta local", "Campo contractual de respuesta local.", "Respuesta esperada", "", "Campo reservado por contrato legado.", "string", true, i++)
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
            Source(methodId, IntegrationSourceKindEnum.Transaction, nameof(AchTransaction), "transaction.id", "Id transaccion", "int", false, order++),
            Source(methodId, IntegrationSourceKindEnum.Transaction, nameof(AchTransaction), "transaction.transactionExternalId", "Id operacion cliente", "string", true, order++),
            Source(methodId, IntegrationSourceKindEnum.Transaction, nameof(AchTransaction), "transaction.reference", "Referencia transaccion", "string", true, order++),
            Source(methodId, IntegrationSourceKindEnum.Transaction, nameof(AchTransaction), "transaction.amount", "Monto transaccion", "decimal", false, order++),
            Source(methodId, IntegrationSourceKindEnum.Transaction, nameof(AchTransaction), "transaction.traceNumber", "Trazabilidad transaccion", "string", false, order++),
            Source(methodId, IntegrationSourceKindEnum.Transaction, nameof(AchTransaction), "transaction.companyIdentification", "NIT/Id empresa origen", "string", false, order++),
            Source(methodId, IntegrationSourceKindEnum.Transaction, nameof(AchTransaction), "transaction.sourceAccountNumber", "Cuenta origen", "string", false, order++),
            Source(methodId, IntegrationSourceKindEnum.Transaction, nameof(AchTransaction), "transaction.effectiveEntryDate", "Fecha efectiva transaccion", "datetime", false, order++),
            Source(methodId, IntegrationSourceKindEnum.Batch, nameof(AchBatch), "batch.id", "Id lote", "int", false, order++),
            Source(methodId, IntegrationSourceKindEnum.Cycle, nameof(AchCycle), "cycle.id", "Id ciclo", "string", false, order++),
            Source(methodId, IntegrationSourceKindEnum.Cycle, nameof(AchCycle), "cycle.processingDate", "Fecha proceso ciclo", "datetime", false, order++),
            Source(methodId, IntegrationSourceKindEnum.ClearingHouse, nameof(ClearingHouse), "clearinghouse.id", "Id camara", "int", false, order++),
            Source(methodId, IntegrationSourceKindEnum.ClearingHouse, nameof(ClearingHouse), "clearinghouse.code", "Codigo camara", "string", false, order++),
            Source(methodId, IntegrationSourceKindEnum.Cycle, "ExecutionContext", "execution.datetimeUtc", "Fecha/hora ejecucion UTC", "datetime", false, order++),
            Source(methodId, IntegrationSourceKindEnum.Cycle, "ExecutionContext", "execution.dateYyyyMMdd", "Fecha ejecucion yyyymmdd", "string", false, order++),
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
}

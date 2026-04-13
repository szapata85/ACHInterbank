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
        return await _context.Set<IntegrationMethod>()
            .AsNoTracking()
            .OrderBy(x => x.Id)
            .Select(x => new IntegrationMethodDto(x.Id, x.Code, x.DisplayName, x.SoapClientCode, x.IsActive))
            .ToListAsync(ct);
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
                x.DataType,
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
    {
        var exists = await _context.Set<IntegrationMethod>().AnyAsync(ct);
        if (exists)
        {
            return;
        }

        var method = new IntegrationMethod
        {
            Code = "WSCFAACH.Proc_Contrapartidas",
            DisplayName = "Proc_Contrapartidas",
            SoapClientCode = "WscfaachSoapClient",
            IsActive = true
        };

        _context.Set<IntegrationMethod>().Add(method);
        await _context.SaveChangesAsync(ct);

        var parameters = BuildProcContrapartidasParameterCatalog(method.Id);
        var sourceCatalog = BuildProcContrapartidasSourceCatalog(method.Id);

        _context.Set<IntegrationMethodParameter>().AddRange(parameters);
        _context.Set<IntegrationSourceCatalogField>().AddRange(sourceCatalog);
        await _context.SaveChangesAsync(ct);
    }

    private static IEnumerable<IntegrationMethodParameter> BuildProcContrapartidasParameterCatalog(int methodId)
    {
        var order = 1;
        yield return Param(methodId, "ClearingHouseId", "Clearing House Id", "int", IntegrationParameterCardinalityEnum.Scalar, true, order++);
        yield return Param(methodId, "ClearingHouseCode", "Clearing House Code", "string", IntegrationParameterCardinalityEnum.Scalar, true, order++);
        yield return Param(methodId, "CycleId", "Cycle Id", "string", IntegrationParameterCardinalityEnum.Scalar, true, order++);
        yield return Param(methodId, "CycleName", "Cycle Name", "string", IntegrationParameterCardinalityEnum.Scalar, true, order++);
        yield return Param(methodId, "ProcessingDate", "Processing Date", "datetime", IntegrationParameterCardinalityEnum.Scalar, true, order++);
        yield return Param(methodId, "StartTime", "Start Time", "timespan", IntegrationParameterCardinalityEnum.Scalar, true, order++);
        yield return Param(methodId, "EndTime", "End Time", "timespan", IntegrationParameterCardinalityEnum.Scalar, true, order++);
        yield return Param(methodId, "CutoffTime", "Cutoff Time", "timespan", IntegrationParameterCardinalityEnum.Scalar, true, order++);
        yield return Param(methodId, "ExecutionDateTime", "Execution Date Time", "datetime", IntegrationParameterCardinalityEnum.Scalar, true, order++);

        yield return Param(methodId, "Transactions", "Transactions", "object", IntegrationParameterCardinalityEnum.Collection, true, order++);
        yield return Param(methodId, "Transactions[].TransactionId", "Transaction Id", "int", IntegrationParameterCardinalityEnum.Scalar, true, order++);
        yield return Param(methodId, "Transactions[].AchBatchId", "Transaction Batch Id", "int", IntegrationParameterCardinalityEnum.Scalar, true, order++);
        yield return Param(methodId, "Transactions[].AchCycleId", "Transaction Cycle Id", "string", IntegrationParameterCardinalityEnum.Scalar, true, order++);
        yield return Param(methodId, "Transactions[].Amount", "Amount", "decimal", IntegrationParameterCardinalityEnum.Scalar, true, order++);
        yield return Param(methodId, "Transactions[].Type", "Type", "string", IntegrationParameterCardinalityEnum.Scalar, true, order++);
        yield return Param(methodId, "Transactions[].TransactionCode", "Transaction Code", "string", IntegrationParameterCardinalityEnum.Scalar, true, order++);
        yield return Param(methodId, "Transactions[].TraceNumber", "Trace Number", "string", IntegrationParameterCardinalityEnum.Scalar, true, order++);
        yield return Param(methodId, "Transactions[].Reference", "Reference", "string", IntegrationParameterCardinalityEnum.Scalar, true, order++);
        yield return Param(methodId, "Transactions[].OriginatingDFI", "Originating DFI", "string", IntegrationParameterCardinalityEnum.Scalar, true, order++);
        yield return Param(methodId, "Transactions[].ReceivingDFI", "Receiving DFI", "string", IntegrationParameterCardinalityEnum.Scalar, true, order++);
        yield return Param(methodId, "Transactions[].CompanyIdentification", "Company Identification", "string", IntegrationParameterCardinalityEnum.Scalar, true, order++);
        yield return Param(methodId, "Transactions[].EffectiveEntryDate", "Effective Entry Date", "datetime", IntegrationParameterCardinalityEnum.Scalar, true, order++);
        yield return Param(methodId, "Transactions[].SourceInstitutionId", "Source Institution Id", "int", IntegrationParameterCardinalityEnum.Scalar, true, order++);
        yield return Param(methodId, "Transactions[].DestinationInstitutionId", "Destination Institution Id", "int", IntegrationParameterCardinalityEnum.Scalar, true, order++);

        yield return Param(methodId, "Transactions[].Addendas", "Addendas", "object", IntegrationParameterCardinalityEnum.Collection, true, order++);
        yield return Param(methodId, "Transactions[].Addendas[].SequenceNumber", "Addenda Sequence Number", "int", IntegrationParameterCardinalityEnum.Scalar, true, order++);
        yield return Param(methodId, "Transactions[].Addendas[].AddendaType", "Addenda Type", "string", IntegrationParameterCardinalityEnum.Scalar, true, order++);
        yield return Param(methodId, "Transactions[].Addendas[].BusinessType", "Business Type", "string", IntegrationParameterCardinalityEnum.Scalar, true, order++);
        yield return Param(methodId, "Transactions[].Addendas[].Information", "Information", "string", IntegrationParameterCardinalityEnum.Scalar, false, order++);
        yield return Param(methodId, "Transactions[].Addendas[].Purpose", "Purpose", "string", IntegrationParameterCardinalityEnum.Scalar, false, order++);
        yield return Param(methodId, "Transactions[].Addendas[].Reference", "Addenda Reference", "string", IntegrationParameterCardinalityEnum.Scalar, false, order++);
        yield return Param(methodId, "Transactions[].Addendas[].CollectorId", "Collector Id", "string", IntegrationParameterCardinalityEnum.Scalar, false, order++);
        yield return Param(methodId, "Transactions[].Addendas[].ReceiverCustomerCode", "Receiver Customer Code", "string", IntegrationParameterCardinalityEnum.Scalar, false, order++);
        yield return Param(methodId, "Transactions[].Addendas[].ServiceDescription", "Service Description", "string", IntegrationParameterCardinalityEnum.Scalar, false, order++);
        yield return Param(methodId, "Transactions[].Addendas[].ReturnReasonCode", "Return Reason Code", "string", IntegrationParameterCardinalityEnum.Scalar, false, order++);
        yield return Param(methodId, "Transactions[].Addendas[].OriginalTraceNumber", "Original Trace Number", "string", IntegrationParameterCardinalityEnum.Scalar, false, order++);
        yield return Param(methodId, "Transactions[].Addendas[].NewTraceNumber", "New Trace Number", "string", IntegrationParameterCardinalityEnum.Scalar, false, order++);
    }

    private static IntegrationMethodParameter Param(
        int methodId,
        string path,
        string display,
        string dataType,
        IntegrationParameterCardinalityEnum cardinality,
        bool required,
        int order)
        => new()
        {
            MethodId = methodId,
            ParameterPath = path,
            DisplayName = display,
            DataType = dataType,
            Cardinality = cardinality,
            Required = required,
            SortOrder = order,
            IsActive = true
        };

    private static IEnumerable<IntegrationSourceCatalogField> BuildProcContrapartidasSourceCatalog(int methodId)
    {
        var fields = new List<(IntegrationSourceKindEnum Kind, string Entity, string Path, string Label, string Type, IntegrationParameterCardinalityEnum Card, bool Nullable)>
        {
            (IntegrationSourceKindEnum.Cycle, nameof(AchCycle), "cycle.id", "Cycle Id", "string", IntegrationParameterCardinalityEnum.Scalar, false),
            (IntegrationSourceKindEnum.Cycle, nameof(AchCycle), "cycle.cycleName", "Cycle Name", "string", IntegrationParameterCardinalityEnum.Scalar, false),
            (IntegrationSourceKindEnum.Cycle, nameof(AchCycle), "cycle.processingDate", "Processing Date", "datetime", IntegrationParameterCardinalityEnum.Scalar, false),
            (IntegrationSourceKindEnum.Cycle, nameof(AchCycle), "cycle.startTime", "Start Time", "timespan", IntegrationParameterCardinalityEnum.Scalar, false),
            (IntegrationSourceKindEnum.Cycle, nameof(AchCycle), "cycle.endTime", "End Time", "timespan", IntegrationParameterCardinalityEnum.Scalar, false),
            (IntegrationSourceKindEnum.Cycle, nameof(AchCycle), "cycle.cutoffTime", "Cutoff Time", "timespan", IntegrationParameterCardinalityEnum.Scalar, false),
            (IntegrationSourceKindEnum.ClearingHouse, nameof(ClearingHouse), "clearingHouse.id", "Clearing House Id", "int", IntegrationParameterCardinalityEnum.Scalar, false),
            (IntegrationSourceKindEnum.ClearingHouse, nameof(ClearingHouse), "clearingHouse.code", "Clearing House Code", "string", IntegrationParameterCardinalityEnum.Scalar, false),
            (IntegrationSourceKindEnum.Batch, nameof(AchBatch), "batch.id", "Batch Id", "int", IntegrationParameterCardinalityEnum.Scalar, false),
            (IntegrationSourceKindEnum.Transaction, nameof(AchTransaction), "transaction.id", "Transaction Id", "int", IntegrationParameterCardinalityEnum.Scalar, false),
            (IntegrationSourceKindEnum.Transaction, nameof(AchTransaction), "transaction.amount", "Amount", "decimal", IntegrationParameterCardinalityEnum.Scalar, false),
            (IntegrationSourceKindEnum.Transaction, nameof(AchTransaction), "transaction.type", "Type", "string", IntegrationParameterCardinalityEnum.Scalar, false),
            (IntegrationSourceKindEnum.Transaction, nameof(AchTransaction), "transaction.transactionCode", "Transaction Code", "string", IntegrationParameterCardinalityEnum.Scalar, false),
            (IntegrationSourceKindEnum.Transaction, nameof(AchTransaction), "transaction.traceNumber", "Trace Number", "string", IntegrationParameterCardinalityEnum.Scalar, false),
            (IntegrationSourceKindEnum.Transaction, nameof(AchTransaction), "transaction.reference", "Reference", "string", IntegrationParameterCardinalityEnum.Scalar, true),
            (IntegrationSourceKindEnum.Transaction, nameof(AchTransaction), "transaction.originatingDfi", "Originating DFI", "string", IntegrationParameterCardinalityEnum.Scalar, true),
            (IntegrationSourceKindEnum.Transaction, nameof(AchTransaction), "transaction.receivingDfi", "Receiving DFI", "string", IntegrationParameterCardinalityEnum.Scalar, true),
            (IntegrationSourceKindEnum.Transaction, nameof(AchTransaction), "transaction.companyIdentification", "Company Identification", "string", IntegrationParameterCardinalityEnum.Scalar, true),
            (IntegrationSourceKindEnum.Transaction, nameof(AchTransaction), "transaction.effectiveEntryDate", "Effective Entry Date", "datetime", IntegrationParameterCardinalityEnum.Scalar, false),
            (IntegrationSourceKindEnum.Transaction, nameof(AchTransaction), "transaction.sourceInstitutionId", "Source Institution Id", "int", IntegrationParameterCardinalityEnum.Scalar, false),
            (IntegrationSourceKindEnum.Transaction, nameof(AchTransaction), "transaction.destinationInstitutionId", "Destination Institution Id", "int", IntegrationParameterCardinalityEnum.Scalar, false),
            (IntegrationSourceKindEnum.Addenda, nameof(AchTransactionAddenda), "addenda.sequenceNumber", "Addenda Sequence Number", "int", IntegrationParameterCardinalityEnum.Scalar, true),
            (IntegrationSourceKindEnum.Addenda, nameof(AchTransactionAddenda), "addenda.addendaType", "Addenda Type", "string", IntegrationParameterCardinalityEnum.Scalar, true),
            (IntegrationSourceKindEnum.Addenda, nameof(AchTransactionAddenda), "addenda.businessType", "Business Type", "string", IntegrationParameterCardinalityEnum.Scalar, false),
            (IntegrationSourceKindEnum.Addenda, nameof(AchTransactionAddenda), "addenda.information", "Information", "string", IntegrationParameterCardinalityEnum.Scalar, true),
            (IntegrationSourceKindEnum.Addenda, nameof(AchTransactionAddenda), "addenda.purpose", "Purpose", "string", IntegrationParameterCardinalityEnum.Scalar, true),
            (IntegrationSourceKindEnum.Addenda, nameof(AchTransactionAddenda), "addenda.reference", "Addenda Reference", "string", IntegrationParameterCardinalityEnum.Scalar, true),
            (IntegrationSourceKindEnum.Addenda, nameof(AchTransactionAddenda), "addenda.collectorId", "Collector Id", "string", IntegrationParameterCardinalityEnum.Scalar, true),
            (IntegrationSourceKindEnum.Addenda, nameof(AchTransactionAddenda), "addenda.receiverCustomerCode", "Receiver Customer Code", "string", IntegrationParameterCardinalityEnum.Scalar, true),
            (IntegrationSourceKindEnum.Addenda, nameof(AchTransactionAddenda), "addenda.serviceDescription", "Service Description", "string", IntegrationParameterCardinalityEnum.Scalar, true),
            (IntegrationSourceKindEnum.Addenda, nameof(AchTransactionAddenda), "addenda.returnReasonCode", "Return Reason Code", "string", IntegrationParameterCardinalityEnum.Scalar, true),
            (IntegrationSourceKindEnum.Addenda, nameof(AchTransactionAddenda), "addenda.originalTraceNumber", "Original Trace Number", "string", IntegrationParameterCardinalityEnum.Scalar, true),
            (IntegrationSourceKindEnum.Addenda, nameof(AchTransactionAddenda), "addenda.newTraceNumber", "New Trace Number", "string", IntegrationParameterCardinalityEnum.Scalar, true),
            (IntegrationSourceKindEnum.Constant, "Constant", "constant.value", "Constant Value", "string", IntegrationParameterCardinalityEnum.Scalar, true),
            (IntegrationSourceKindEnum.Expression, "Expression", "expression.value", "Expression Result", "string", IntegrationParameterCardinalityEnum.Scalar, true)
        };

        var order = 1;
        return fields.Select(f => new IntegrationSourceCatalogField
        {
            MethodId = methodId,
            SourceKind = f.Kind,
            EntityName = f.Entity,
            FieldPath = f.Path,
            DisplayName = f.Label,
            DataType = f.Type,
            Cardinality = f.Card,
            Nullable = f.Nullable,
            SortOrder = order++,
            IsActive = true
        });
    }
}

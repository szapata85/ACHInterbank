using System.Text.Json;
using Cfa.ACHInterbank.Application.Integrations.Dtos;
using Cfa.ACHInterbank.Application.Integrations.Interfaces;
using Cfa.ACHInterbank.Domain.Entities.Integrations;
using Cfa.ACHInterbank.Domain.Models.ACH;
using Cfa.ACHInterbank.Persistence.DataBase;
using Microsoft.EntityFrameworkCore;

namespace Cfa.ACHInterbank.Persistence.Integrations.Services;

[Scoped]
public class IntegrationMappingPreviewService : IIntegrationMappingPreviewService
{
    private readonly AchDbContext _context;

    public IntegrationMappingPreviewService(AchDbContext context)
    {
        _context = context;
    }

    public async Task<IntegrationMappingPreviewResultDto> PreviewAsync(Guid mappingSetId, PreviewIntegrationMappingSetRequest request, CancellationToken ct = default)
    {
        var set = await _context.Set<IntegrationMappingSet>()
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == mappingSetId, ct)
            ?? throw new KeyNotFoundException($"No existe MappingSet {mappingSetId}.");

        var method = await _context.Set<IntegrationMethod>()
            .AsNoTracking()
            .FirstAsync(x => x.Id == set.MethodId, ct);

        var rules = await _context.Set<IntegrationMappingRule>()
            .AsNoTracking()
            .Where(x => x.MappingSetId == mappingSetId && x.Enabled)
            .OrderBy(x => x.ParameterId)
            .ThenBy(x => x.Priority)
            .ToListAsync(ct);

        var parameters = await _context.Set<IntegrationMethodParameter>()
            .AsNoTracking()
            .Where(x => x.MethodId == set.MethodId && x.IsActive)
            .ToDictionaryAsync(x => x.Id, ct);

        var tx = await LoadSampleTransactionAsync(request, ct);
        var cycle = tx?.AchCycle;
        var batch = tx?.AchBatch;
        var clearingHouse = cycle?.ClearingHouse;
        var addenda = tx?.Addendas.OrderBy(a => a.SequenceNumber).FirstOrDefault();

        var previewItems = new List<IntegrationMappingPreviewItemDto>();

        foreach (var rule in rules)
        {
            if (!parameters.TryGetValue(rule.ParameterId, out var parameter))
            {
                continue;
            }

            var resolved = ResolvePreviewValue(rule, tx, addenda, batch, cycle, clearingHouse);

            previewItems.Add(new IntegrationMappingPreviewItemDto(
                parameter.ParameterPath,
                ResolveFromLabel(rule),
                resolved,
                rule.Priority,
                rule.Enabled));
        }

        var json = JsonSerializer.Serialize(previewItems);
        return new IntegrationMappingPreviewResultDto(mappingSetId, method.Id, method.Code, previewItems, json);
    }

    private async Task<AchTransaction?> LoadSampleTransactionAsync(PreviewIntegrationMappingSetRequest request, CancellationToken ct)
    {
        if (request.SampleTransactionId.HasValue)
        {
            return await _context.AchTransactions
                .AsNoTracking()
                .Include(t => t.Addendas)
                .Include(t => t.AchBatch)
                .Include(t => t.AchCycle)
                    .ThenInclude(c => c!.ClearingHouse)
                .FirstOrDefaultAsync(t => t.Id == request.SampleTransactionId.Value, ct);
        }

        var query = _context.AchTransactions
            .AsNoTracking()
            .Include(t => t.Addendas)
            .Include(t => t.AchBatch)
            .Include(t => t.AchCycle)
                .ThenInclude(c => c!.ClearingHouse)
            .OrderByDescending(t => t.Id)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(request.SampleCycleId))
        {
            query = query.Where(t => t.AchCycleId == request.SampleCycleId);
        }

        return await query.FirstOrDefaultAsync(ct);
    }

    private static string ResolveFromLabel(IntegrationMappingRule rule)
    {
        return rule.SourceKind switch
        {
            IntegrationSourceKindEnum.Constant => "Constant",
            IntegrationSourceKindEnum.Expression => "Expression",
            _ => string.IsNullOrWhiteSpace(rule.SourceFieldPath)
                ? $"CatalogField:{rule.SourceCatalogFieldId?.ToString() ?? "n/a"}"
                : rule.SourceFieldPath
        };
    }

    private static string? ResolvePreviewValue(
        IntegrationMappingRule rule,
        AchTransaction? tx,
        AchTransactionAddenda? addenda,
        AchBatch? batch,
        AchCycle? cycle,
        ClearingHouse? clearingHouse)
    {
        if (!string.IsNullOrWhiteSpace(rule.FixedValue))
        {
            return rule.FixedValue;
        }

        if (rule.SourceKind == IntegrationSourceKindEnum.Constant)
        {
            return rule.DefaultValue;
        }

        if (rule.SourceKind == IntegrationSourceKindEnum.Expression)
        {
            return string.IsNullOrWhiteSpace(rule.ConditionExpression)
                ? "[expression]"
                : $"[expression:{rule.ConditionExpression}]";
        }

        var path = rule.SourceFieldPath?.Trim().ToLowerInvariant() ?? string.Empty;
        var raw = path switch
        {
            "transaction.id" => tx?.Id.ToString(),
            "transaction.amount" => tx?.Amount.ToString(),
            "transaction.type" => tx?.Type.ToString(),
            "transaction.transactioncode" => tx?.TransactionCode,
            "transaction.tracenumber" => tx?.TraceNumber,
            "transaction.reference" => tx?.Reference,
            "transaction.originatingdfi" => tx?.OriginatingDFI,
            "transaction.receivingdfi" => tx?.ReceivingDFI,
            "transaction.companyidentification" => tx?.CompanyIdentification,
            "transaction.effectiveentrydate" => tx?.EffectiveEntryDate.ToString("O"),
            "transaction.sourceinstitutionid" => tx?.SourceInstitutionId.ToString(),
            "transaction.destinationinstitutionid" => tx?.DestinationInstitutionId.ToString(),
            "addenda.sequencenumber" => addenda?.SequenceNumber?.ToString(),
            "addenda.addendatype" => addenda?.AddendaType,
            "addenda.businesstype" => addenda?.BusinessType.ToString(),
            "addenda.information" => addenda?.Information,
            "addenda.purpose" => addenda?.Purpose,
            "addenda.reference" => addenda?.Reference,
            "addenda.collectorid" => addenda?.CollectorId,
            "addenda.receivercustomercode" => addenda?.ReceiverCustomerCode,
            "addenda.servicedescription" => addenda?.ServiceDescription,
            "addenda.returnreasoncode" => addenda?.ReturnReasonCode,
            "addenda.originaltracenumber" => addenda?.OriginalTraceNumber,
            "addenda.newtracenumber" => addenda?.NewTraceNumber,
            "batch.id" => batch?.Id.ToString(),
            "cycle.id" => cycle?.Id,
            "cycle.cyclename" => cycle?.CycleName,
            "cycle.processingdate" => cycle?.ProcessingDate.ToString("O"),
            "cycle.starttime" => cycle?.StartTime.ToString(),
            "cycle.endtime" => cycle?.EndTime.ToString(),
            "cycle.cutofftime" => cycle?.CutoffTime.ToString(),
            "clearinghouse.id" => clearingHouse?.Id.ToString(),
            "clearinghouse.code" => clearingHouse?.Code,
            _ => null
        };

        return string.IsNullOrWhiteSpace(raw) ? rule.DefaultValue : raw;
    }
}

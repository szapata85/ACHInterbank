using System.Text.Json;
using Cfa.ACHInterbank.Application.Integrations.Dtos;
using Cfa.ACHInterbank.Application.Integrations.Interfaces;
using Cfa.ACHInterbank.Domain.Entities.Integrations;
using Cfa.ACHInterbank.Domain.Entities.Transactions.Enums;
using Cfa.ACHInterbank.Domain.Models.ACH;
using Cfa.ACHInterbank.Domain.Models.Configurations;
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
            .OrderBy(x => x.SortOrder)
            .ToListAsync(ct);

        var (tx, contextMode) = await LoadSampleTransactionAsync(request, ct);
        var cycle = tx?.AchCycle;
        var batch = tx?.AchBatch;
        var clearingHouse = cycle?.ClearingHouse;
        var addenda = tx?.Addendas.OrderBy(a => a.SequenceNumber).FirstOrDefault();

        var previewItems = new List<IntegrationMappingPreviewItemDto>();
        var payload = new Dictionary<string, string?>();

        foreach (var parameter in parameters)
        {
            var winner = rules
                .Where(x => x.ParameterId == parameter.Id)
                .OrderBy(x => x.Priority)
                .ThenBy(x => x.Id)
                .FirstOrDefault();

            if (winner is null)
            {
                continue;
            }

            var resolved = ResolvePreviewValue(winner, tx, addenda, batch, cycle, clearingHouse, DateTime.UtcNow);
            resolved = ApplyTransformation(winner.TransformationCode, winner.FormatMask, resolved);

            var resolvedFrom = ResolveFromLabel(winner);
            var section = ResolveSection(winner.SourceKind);
            var resolutionKind = ResolveResolutionKind(winner);

            payload[parameter.ParameterPath] = resolved;
            previewItems.Add(new IntegrationMappingPreviewItemDto(
                parameter.Id,
                parameter.ParameterPath,
                resolvedFrom,
                resolved,
                section,
                resolutionKind,
                winner.TransformationCode,
                winner.Priority,
                winner.Enabled));
        }

        var limitedItems = previewItems.Take(Math.Max(1, request.MaxItems)).ToList();
        var rawJson = JsonSerializer.Serialize(limitedItems);
        var payloadJson = JsonSerializer.Serialize(payload.OrderBy(x => x.Key).ToDictionary(x => x.Key, x => x.Value));

        return new IntegrationMappingPreviewResultDto(mappingSetId, method.Id, method.Code, contextMode, previewItems, payloadJson, rawJson);
    }

    private async Task<(AchTransaction? tx, string contextMode)> LoadSampleTransactionAsync(PreviewIntegrationMappingSetRequest request, CancellationToken ct)
    {
        if (request.UseControlledSample)
        {
            return (BuildControlledSample(), "controlled-sample");
        }

        if (request.SampleTransactionId.HasValue)
        {
            var txById = await _context.AchTransactions
                .AsNoTracking()
                .Include(t => t.Addendas)
                .Include(t => t.AchBatch)
                .Include(t => t.AchCycle)
                    .ThenInclude(c => c!.ClearingHouse)
                .FirstOrDefaultAsync(t => t.Id == request.SampleTransactionId.Value, ct);

            return (txById, "real-transaction");
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

        var tx = await query.FirstOrDefaultAsync(ct);
        return tx is null ? (BuildControlledSample(), "controlled-sample") : (tx, "real-latest");
    }

    private static AchTransaction BuildControlledSample()
        => new()
        {
            Id = 999001,
            Amount = 1234.56m,
            Type = TransactionTypeEnum.Credit,
            TransactionCode = "22",
            TraceNumber = "000123456789012",
            Reference = "PREVIEW-TRACE",
            OriginatingDFI = "021000021",
            ReceivingDFI = "031100209",
            CompanyIdentification = "COMPANY123",
            EffectiveEntryDate = DateTime.UtcNow.Date,
            SourceInstitutionId = 100,
            DestinationInstitutionId = 200,
            AchBatch = new AchBatch { Id = 7001 },
            AchCycle = new AchCycle
            {
                Id = "CYCLE-PREVIEW",
                CycleName = "Ciclo Preview",
                ProcessingDate = DateTime.UtcNow.Date,
                StartTime = TimeSpan.FromHours(8),
                EndTime = TimeSpan.FromHours(18),
                CutoffTime = TimeSpan.FromHours(16),
                ClearingHouse = new ClearingHouse { Id = 12, Code = "ACH-TEST", Name = "ACH Demo", OriginCode = "ORG" }
            },
            Addendas =
            [
                new AchTransactionAddenda
                {
                    SequenceNumber = 1,
                    AddendaType = "05",
                    BusinessType = AchAddendaBusinessType.Credit,
                    Information = "Pago de prueba",
                    Purpose = "Preview",
                    Reference = "ADD-001",
                    CollectorId = "COL-1",
                    ReceiverCustomerCode = "RC-1",
                    ServiceDescription = "Servicio demo"
                }
            ]
        };

    private static string ResolveFromLabel(IntegrationMappingRule rule)
    {
        if (!string.IsNullOrWhiteSpace(rule.FixedValue))
        {
            return "FixedValue";
        }

        if (rule.SourceKind == IntegrationSourceKindEnum.Constant)
        {
            return "Default/Constant";
        }

        return rule.SourceKind switch
        {
            IntegrationSourceKindEnum.Expression => $"Expression:{rule.ConditionExpression ?? "rule"}",
            _ => string.IsNullOrWhiteSpace(rule.SourceFieldPath)
                ? $"CatalogField:{rule.SourceCatalogFieldId?.ToString() ?? "n/a"}"
                : rule.SourceFieldPath
        };
    }

    private static string ResolveSection(IntegrationSourceKindEnum sourceKind)
        => sourceKind switch
        {
            IntegrationSourceKindEnum.Cycle or IntegrationSourceKindEnum.ClearingHouse => "ciclo-camara",
            IntegrationSourceKindEnum.Transaction => "transaccion",
            IntegrationSourceKindEnum.Batch => "lote",
            IntegrationSourceKindEnum.Addenda => "addenda",
            _ => "configuracion"
        };

    private static string ResolveResolutionKind(IntegrationMappingRule winner)
    {
        if (!string.IsNullOrWhiteSpace(winner.FixedValue) || !string.IsNullOrWhiteSpace(winner.DefaultValue))
        {
            return "default-fixed";
        }

        if (winner.SourceKind == IntegrationSourceKindEnum.Expression)
        {
            return "expression";
        }

        return "source-field";
    }

    private static string? ApplyTransformation(string? transformationCode, string? formatMask, string? value)
    {
        if (string.IsNullOrWhiteSpace(value) || string.IsNullOrWhiteSpace(transformationCode))
        {
            return value;
        }

        return transformationCode switch
        {
            "Trim" => value.Trim(),
            "Uppercase" => value.ToUpperInvariant(),
            "Lowercase" => value.ToLowerInvariant(),
            "PadLeft" when int.TryParse(formatMask, out var left) => value.PadLeft(left, '0'),
            "PadRight" when int.TryParse(formatMask, out var right) => value.PadRight(right, '0'),
            "NullIfEmpty" => string.IsNullOrWhiteSpace(value) ? null : value,
            _ => value
        };
    }

    private static string? ResolvePreviewValue(
        IntegrationMappingRule rule,
        AchTransaction? tx,
        AchTransactionAddenda? addenda,
        AchBatch? batch,
        AchCycle? cycle,
        ClearingHouse? clearingHouse,
        DateTime executionDateTime)
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
            "transaction.transactionexternalid" => tx?.TransactionExternalId,
            "transaction.amount" => tx?.Amount.ToString(),
            "transaction.type" => tx?.Type.ToString(),
            "transaction.transactioncode" => tx?.TransactionCode,
            "transaction.tracenumber" => tx?.TraceNumber,
            "transaction.reference" => tx?.Reference,
            "transaction.originatingdfi" => tx?.OriginatingDFI,
            "transaction.receivingdfi" => tx?.ReceivingDFI,
            "transaction.companyidentification" => tx?.CompanyIdentification,
            "transaction.sourceaccountnumber" => tx?.SourceAccountNumber,
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
            "execution.datetimeutc" => executionDateTime.ToString("O"),
            "execution.dateyyyymmdd" => executionDateTime.ToString("yyyyMMdd"),
            _ => null
        };

        return string.IsNullOrWhiteSpace(raw) ? rule.DefaultValue : raw;
    }
}

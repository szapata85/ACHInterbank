using System.Globalization;
using Cfa.ACHInterbank.Application.ACH.Models;
using Cfa.ACHInterbank.Application.Integrations.Interfaces;
using Cfa.ACHInterbank.Domain.Entities.Integrations;
using Cfa.ACHInterbank.Domain.Models.ACH;
using Cfa.ACHInterbank.Persistence.DataBase;
using Microsoft.EntityFrameworkCore;

namespace Cfa.ACHInterbank.Persistence.Integrations.Services;

[Scoped]
public class ProcContrapartidasFunctionalMappingResolver : IProcContrapartidasFunctionalMappingResolver
{
    private readonly AchDbContext _context;

    public ProcContrapartidasFunctionalMappingResolver(AchDbContext context)
    {
        _context = context;
    }

    public async Task<ProcContrapartidasRequestContract?> TryResolveAsync(
        AchCycle cycle,
        IReadOnlyCollection<AchTransaction> transactions,
        DateTime executionDateTime,
        CancellationToken ct = default)
    {
        var method = await _context.Set<IntegrationMethod>()
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Code == "WSCFAACH.Proc_Contrapartidas" && x.IsActive, ct);

        if (method is null)
        {
            return null;
        }

        var published = await _context.Set<IntegrationMappingSet>()
            .AsNoTracking()
            .Where(x => x.MethodId == method.Id && x.Status == IntegrationMappingSetStatusEnum.Published)
            .OrderByDescending(x => x.Version)
            .FirstOrDefaultAsync(ct);

        if (published is null)
        {
            return null;
        }

        var rules = await _context.Set<IntegrationMappingRule>()
            .AsNoTracking()
            .Where(x => x.MappingSetId == published.Id && x.Enabled)
            .ToListAsync(ct);

        if (rules.Count == 0)
        {
            return null;
        }

        var parameters = await _context.Set<IntegrationMethodParameter>()
            .AsNoTracking()
            .Where(x => x.MethodId == method.Id && x.IsActive)
            .ToListAsync(ct);

        var sourceFields = await _context.Set<IntegrationSourceCatalogField>()
            .AsNoTracking()
            .Where(x => x.MethodId == method.Id && x.IsActive)
            .ToDictionaryAsync(x => x.Id, ct);

        var ruleByPath = parameters.ToDictionary(
            p => p.ParameterPath,
            p => rules.Where(r => r.ParameterId == p.Id).OrderBy(r => r.Priority).ToList(),
            StringComparer.OrdinalIgnoreCase);

        string ResolveTop(string path)
        {
            var val = ResolveScalar(ruleByPath, sourceFields, path, cycle, transactions.FirstOrDefault(), null, executionDateTime);
            return val ?? string.Empty;
        }

        var txContracts = new List<ProcContrapartidasTransactionContract>();
        foreach (var tx in transactions.OrderBy(t => t.Id))
        {
            var addendas = new List<ProcContrapartidasAddendaContract>();
            foreach (var addenda in tx.Addendas.OrderBy(a => a.SequenceNumber))
            {
                addendas.Add(new ProcContrapartidasAddendaContract
                {
                    SequenceNumber = ParseInt(ResolveScalar(ruleByPath, sourceFields, "Transactions[].Addendas[].SequenceNumber", cycle, tx, addenda, executionDateTime), addenda.SequenceNumber ?? 0),
                    AddendaType = ResolveScalar(ruleByPath, sourceFields, "Transactions[].Addendas[].AddendaType", cycle, tx, addenda, executionDateTime) ?? string.Empty,
                    BusinessType = ResolveScalar(ruleByPath, sourceFields, "Transactions[].Addendas[].BusinessType", cycle, tx, addenda, executionDateTime) ?? string.Empty,
                    Information = ResolveScalar(ruleByPath, sourceFields, "Transactions[].Addendas[].Information", cycle, tx, addenda, executionDateTime) ?? string.Empty,
                    Purpose = ResolveScalar(ruleByPath, sourceFields, "Transactions[].Addendas[].Purpose", cycle, tx, addenda, executionDateTime) ?? string.Empty,
                    Reference = ResolveScalar(ruleByPath, sourceFields, "Transactions[].Addendas[].Reference", cycle, tx, addenda, executionDateTime) ?? string.Empty,
                    CollectorId = ResolveScalar(ruleByPath, sourceFields, "Transactions[].Addendas[].CollectorId", cycle, tx, addenda, executionDateTime) ?? string.Empty,
                    ReceiverCustomerCode = ResolveScalar(ruleByPath, sourceFields, "Transactions[].Addendas[].ReceiverCustomerCode", cycle, tx, addenda, executionDateTime) ?? string.Empty,
                    ServiceDescription = ResolveScalar(ruleByPath, sourceFields, "Transactions[].Addendas[].ServiceDescription", cycle, tx, addenda, executionDateTime) ?? string.Empty,
                    ReturnReasonCode = ResolveScalar(ruleByPath, sourceFields, "Transactions[].Addendas[].ReturnReasonCode", cycle, tx, addenda, executionDateTime) ?? string.Empty,
                    OriginalTraceNumber = ResolveScalar(ruleByPath, sourceFields, "Transactions[].Addendas[].OriginalTraceNumber", cycle, tx, addenda, executionDateTime) ?? string.Empty,
                    NewTraceNumber = ResolveScalar(ruleByPath, sourceFields, "Transactions[].Addendas[].NewTraceNumber", cycle, tx, addenda, executionDateTime) ?? string.Empty
                });
            }

            txContracts.Add(new ProcContrapartidasTransactionContract
            {
                TransactionId = ParseInt(ResolveScalar(ruleByPath, sourceFields, "Transactions[].TransactionId", cycle, tx, null, executionDateTime), tx.Id),
                AchBatchId = ParseInt(ResolveScalar(ruleByPath, sourceFields, "Transactions[].AchBatchId", cycle, tx, null, executionDateTime), tx.AchBatchId),
                AchCycleId = ResolveScalar(ruleByPath, sourceFields, "Transactions[].AchCycleId", cycle, tx, null, executionDateTime) ?? tx.AchCycleId,
                Amount = ParseDecimal(ResolveScalar(ruleByPath, sourceFields, "Transactions[].Amount", cycle, tx, null, executionDateTime), tx.Amount),
                Type = ResolveScalar(ruleByPath, sourceFields, "Transactions[].Type", cycle, tx, null, executionDateTime) ?? tx.Type.ToString(),
                TransactionCode = ResolveScalar(ruleByPath, sourceFields, "Transactions[].TransactionCode", cycle, tx, null, executionDateTime) ?? tx.TransactionCode,
                TraceNumber = ResolveScalar(ruleByPath, sourceFields, "Transactions[].TraceNumber", cycle, tx, null, executionDateTime) ?? tx.TraceNumber,
                Reference = ResolveScalar(ruleByPath, sourceFields, "Transactions[].Reference", cycle, tx, null, executionDateTime) ?? tx.Reference,
                OriginatingDfi = ResolveScalar(ruleByPath, sourceFields, "Transactions[].OriginatingDFI", cycle, tx, null, executionDateTime) ?? tx.OriginatingDFI,
                ReceivingDfi = ResolveScalar(ruleByPath, sourceFields, "Transactions[].ReceivingDFI", cycle, tx, null, executionDateTime) ?? tx.ReceivingDFI,
                CompanyIdentification = ResolveScalar(ruleByPath, sourceFields, "Transactions[].CompanyIdentification", cycle, tx, null, executionDateTime) ?? tx.CompanyIdentification,
                EffectiveEntryDate = ParseDateTime(ResolveScalar(ruleByPath, sourceFields, "Transactions[].EffectiveEntryDate", cycle, tx, null, executionDateTime), tx.EffectiveEntryDate),
                SourceInstitutionId = ParseInt(ResolveScalar(ruleByPath, sourceFields, "Transactions[].SourceInstitutionId", cycle, tx, null, executionDateTime), tx.SourceInstitutionId),
                DestinationInstitutionId = ParseInt(ResolveScalar(ruleByPath, sourceFields, "Transactions[].DestinationInstitutionId", cycle, tx, null, executionDateTime), tx.DestinationInstitutionId),
                Addendas = addendas
            });
        }

        return new ProcContrapartidasRequestContract
        {
            ClearingHouseId = ParseInt(ResolveTop("ClearingHouseId"), cycle.ClearingHouseId),
            ClearingHouseCode = ResolveTop("ClearingHouseCode"),
            CycleId = ResolveTop("CycleId"),
            CycleName = ResolveTop("CycleName"),
            ProcessingDate = ParseDateTime(ResolveTop("ProcessingDate"), cycle.ProcessingDate),
            StartTime = ParseTimeSpan(ResolveTop("StartTime"), cycle.StartTime),
            EndTime = ParseTimeSpan(ResolveTop("EndTime"), cycle.EndTime),
            CutoffTime = ParseTimeSpan(ResolveTop("CutoffTime"), cycle.CutoffTime),
            ExecutionDateTime = ParseDateTime(ResolveTop("ExecutionDateTime"), executionDateTime),
            Transactions = txContracts
        };
    }

    private static string? ResolveScalar(
        IReadOnlyDictionary<string, List<IntegrationMappingRule>> rulesByPath,
        IReadOnlyDictionary<long, IntegrationSourceCatalogField> sourceCatalog,
        string parameterPath,
        AchCycle cycle,
        AchTransaction? tx,
        AchTransactionAddenda? addenda,
        DateTime executionDateTime)
    {
        if (!rulesByPath.TryGetValue(parameterPath, out var rules) || rules.Count == 0)
        {
            return null;
        }

        foreach (var rule in rules.Where(r => r.Enabled).OrderBy(r => r.Priority))
        {
            var sourcePath = !string.IsNullOrWhiteSpace(rule.SourceFieldPath)
                ? rule.SourceFieldPath
                : (rule.SourceCatalogFieldId.HasValue && sourceCatalog.TryGetValue(rule.SourceCatalogFieldId.Value, out var sf)
                    ? sf.FieldPath
                    : string.Empty);

            string? value = rule.SourceKind switch
            {
                IntegrationSourceKindEnum.Constant => rule.FixedValue ?? rule.DefaultValue,
                IntegrationSourceKindEnum.Expression => ResolveExpression(rule.ConditionExpression, executionDateTime, cycle, tx, addenda),
                _ => ResolvePath(sourcePath, cycle, tx, addenda)
            };

            value ??= rule.DefaultValue;
            value = ApplyTransformation(value, rule.TransformationCode, rule.FormatMask);

            if (!string.IsNullOrWhiteSpace(value) || !string.IsNullOrWhiteSpace(rule.DefaultValue))
            {
                return value;
            }
        }

        return null;
    }

    private static string? ResolvePath(string sourcePath, AchCycle cycle, AchTransaction? tx, AchTransactionAddenda? addenda)
    {
        var key = sourcePath.Trim().ToLowerInvariant();
        return key switch
        {
            "cycle.id" => cycle.Id,
            "cycle.cyclename" => cycle.CycleName,
            "cycle.processingdate" => cycle.ProcessingDate.ToString("O"),
            "cycle.starttime" => cycle.StartTime.ToString(),
            "cycle.endtime" => cycle.EndTime.ToString(),
            "cycle.cutofftime" => cycle.CutoffTime.ToString(),
            "clearinghouse.id" => cycle.ClearingHouseId.ToString(CultureInfo.InvariantCulture),
            "clearinghouse.code" => cycle.ClearingHouse?.Code,
            "transaction.id" => tx?.Id.ToString(CultureInfo.InvariantCulture),
            "transaction.achbatchid" => tx?.AchBatchId.ToString(CultureInfo.InvariantCulture),
            "transaction.achcycleid" => tx?.AchCycleId,
            "transaction.amount" => tx?.Amount.ToString(CultureInfo.InvariantCulture),
            "transaction.type" => tx?.Type.ToString(),
            "transaction.transactioncode" => tx?.TransactionCode,
            "transaction.tracenumber" => tx?.TraceNumber,
            "transaction.reference" => tx?.Reference,
            "transaction.originatingdfi" => tx?.OriginatingDFI,
            "transaction.receivingdfi" => tx?.ReceivingDFI,
            "transaction.companyidentification" => tx?.CompanyIdentification,
            "transaction.effectiveentrydate" => tx?.EffectiveEntryDate.ToString("O"),
            "transaction.sourceinstitutionid" => tx?.SourceInstitutionId.ToString(CultureInfo.InvariantCulture),
            "transaction.destinationinstitutionid" => tx?.DestinationInstitutionId.ToString(CultureInfo.InvariantCulture),
            "addenda.sequencenumber" => addenda?.SequenceNumber?.ToString(CultureInfo.InvariantCulture),
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
            _ => null
        };
    }

    private static string? ResolveExpression(string? expression, DateTime executionDateTime, AchCycle cycle, AchTransaction? tx, AchTransactionAddenda? addenda)
    {
        if (string.IsNullOrWhiteSpace(expression))
        {
            return null;
        }

        return expression.Trim().ToLowerInvariant() switch
        {
            "executiondatetime" => executionDateTime.ToString("O"),
            "cycle.processingdate" => cycle.ProcessingDate.ToString("O"),
            "transaction.reference" => tx?.Reference,
            "addenda.reference" => addenda?.Reference,
            _ => null
        };
    }

    private static string? ApplyTransformation(string? value, string? transformationCode, string? formatMask)
    {
        if (value is null)
        {
            return value;
        }

        if (string.IsNullOrWhiteSpace(transformationCode))
        {
            return value;
        }

        var code = transformationCode.Trim();
        return code switch
        {
            "Trim" => value.Trim(),
            "Uppercase" => value.ToUpperInvariant(),
            "Lowercase" => value.ToLowerInvariant(),
            "PadLeft" => value.PadLeft(ParseInt(formatMask, value.Length), '0'),
            "PadRight" => value.PadRight(ParseInt(formatMask, value.Length), '0'),
            "Substring" => ResolveSubstring(value, formatMask),
            "DateFormat" => ResolveDateFormat(value, formatMask),
            "NumericFormat" => ResolveNumericFormat(value, formatMask),
            "NullIfEmpty" => string.IsNullOrWhiteSpace(value) ? null : value,
            "DefaultIfNull" => string.IsNullOrWhiteSpace(value) ? formatMask : value,
            "Concat" => string.IsNullOrWhiteSpace(formatMask) ? value : $"{value}{formatMask}",
            _ => value
        };
    }

    private static string ResolveSubstring(string value, string? formatMask)
    {
        if (string.IsNullOrWhiteSpace(formatMask))
        {
            return value;
        }

        var parts = formatMask.Split(':', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length == 0 || !int.TryParse(parts[0], out var start))
        {
            return value;
        }

        if (parts.Length == 1)
        {
            return start >= value.Length ? string.Empty : value[start..];
        }

        if (!int.TryParse(parts[1], out var length) || start >= value.Length)
        {
            return value;
        }

        return value.Substring(start, Math.Min(length, value.Length - start));
    }

    private static string ResolveDateFormat(string value, string? formatMask)
    {
        if (!DateTime.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out var dt))
        {
            return value;
        }

        return dt.ToString(string.IsNullOrWhiteSpace(formatMask) ? "O" : formatMask, CultureInfo.InvariantCulture);
    }

    private static string ResolveNumericFormat(string value, string? formatMask)
    {
        if (!decimal.TryParse(value, NumberStyles.Any, CultureInfo.InvariantCulture, out var number))
        {
            return value;
        }

        return number.ToString(string.IsNullOrWhiteSpace(formatMask) ? "0.##" : formatMask, CultureInfo.InvariantCulture);
    }

    private static int ParseInt(string? value, int fallback)
        => int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsed) ? parsed : fallback;

    private static decimal ParseDecimal(string? value, decimal fallback)
        => decimal.TryParse(value, NumberStyles.Any, CultureInfo.InvariantCulture, out var parsed) ? parsed : fallback;

    private static DateTime ParseDateTime(string? value, DateTime fallback)
        => DateTime.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out var parsed) ? parsed : fallback;

    private static TimeSpan ParseTimeSpan(string? value, TimeSpan fallback)
        => TimeSpan.TryParse(value, CultureInfo.InvariantCulture, out var parsed) ? parsed : fallback;
}

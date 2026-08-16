using System.Security.Cryptography;
using System.Text;
using Cfa.ACHInterbank.Application.ACH.Interfaces;
using Cfa.ACHInterbank.Application.ACH.Interfaces.ExternalFileNames;
using Cfa.ACHInterbank.Application.ACH.Models;
using Cfa.ACHInterbank.Application.ACH.Models.ExternalFileNames;
using Cfa.ACHInterbank.Domain.Models.ACH;
using Cfa.ACHInterbank.Persistence.DataBase;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Cfa.ACHInterbank.Persistence.ACH.Services.Implementation;

public class AchReturnOfReturnFileGenerationService(
    AchDbContext context,
    IExternalFileNamePolicy? externalFileNamePolicy = null,
    INachaRecordConfigProvider? nachaRecordConfigProvider = null,
    INachaRecordFieldValidator? nachaRecordFieldValidator = null,
    ILogger<AchReturnOfReturnFileGenerationService>? logger = null,
    INachaFileBuilder? nachaFileBuilder = null) : IAchReturnOfReturnFileGenerationService
{
    private readonly IExternalFileNamePolicy? _externalFileNamePolicy = externalFileNamePolicy;
    private readonly INachaRecordConfigProvider? _nachaRecordConfigProvider = nachaRecordConfigProvider;
    private readonly INachaRecordFieldValidator? _nachaRecordFieldValidator = nachaRecordFieldValidator;
    private readonly ILogger<AchReturnOfReturnFileGenerationService> _logger = logger ?? NullLogger<AchReturnOfReturnFileGenerationService>.Instance;
    private readonly INachaFileBuilder? _nachaFileBuilder = nachaFileBuilder;
    public async Task<AchReturnOfReturnFileGenerationResult> GenerateAsync(AchReturnOfReturnFileGenerationRequest request, CancellationToken cancellationToken)
    {
        var failures = new List<AchReturnOfReturnFileGenerationFailure>();
        if (request.ReturnOfReturnFlowIds is null || request.ReturnOfReturnFlowIds.Count == 0)
        {
            failures.Add(new("RETURN_OF_RETURN_FLOW_EMPTY", "Debe enviar al menos un flujo de devolución de devolución.", nameof(request.ReturnOfReturnFlowIds)));
            return new(false, null, null, null, 0, Array.Empty<int>(), failures, null, null);
        }

        var requestedIds = request.ReturnOfReturnFlowIds.Distinct().ToArray();
        var flows = await context.ReturnOfReturnFlows
            .AsNoTracking()
            .Include(x => x.SourceReturnTransaction!).ThenInclude(x => x.AchCycle)
            .Include(x => x.OriginalTransaction).ThenInclude(x => x!.AchCycle)
            .Include(x => x.ReturnOfReturnTransaction).ThenInclude(x => x.AchCycle)
            .Where(x => requestedIds.Contains((int)x.Id))
            .ToListAsync(cancellationToken);

        var foundIds = flows.Select(x => (int)x.Id).ToHashSet();
        var missingIds = requestedIds.Where(x => !foundIds.Contains(x)).ToArray();
        if (missingIds.Length > 0)
        {
            failures.Add(new("RETURN_OF_RETURN_FLOW_NOT_FOUND", $"No se encontraron flujos: {string.Join(",", missingIds)}.", nameof(request.ReturnOfReturnFlowIds)));
            return new(false, null, null, null, 0, Array.Empty<int>(), failures, null, null);
        }

        foreach (var flow in flows)
        {
            if (flow.SourceReturnTransaction is null && flow.OriginalTransaction is null)
            {
                failures.Add(new("SOURCE_RETURN_TRANSACTION_NOT_FOUND", $"No se encontró SourceReturnTransaction para flujo {flow.Id}."));
            }
            if (flow.ReturnOfReturnTransaction is null)
            {
                failures.Add(new("RETURN_OF_RETURN_TRANSACTION_NOT_FOUND", $"No se encontró ReturnOfReturnTransaction para flujo {flow.Id}."));
            }
        }

        if (failures.Count > 0)
        {
            return new(false, null, null, null, 0, flows.Select(x => (int)x.Id).ToArray(), failures, null, null);
        }


        foreach (var flow in flows)
        {
            var sourceClearingHouseId = flow.SourceReturnTransaction?.AchCycle?.ClearingHouseId
                                        ?? flow.OriginalTransaction?.AchCycle?.ClearingHouseId
                                        ?? 0;
            var returnOfReturnClearingHouseId = flow.ReturnOfReturnTransaction?.AchCycle?.ClearingHouseId ?? 0;
            if (sourceClearingHouseId <= 0 || returnOfReturnClearingHouseId <= 0 || sourceClearingHouseId != returnOfReturnClearingHouseId)
            {
                failures.Add(new("CLEARING_HOUSE_MISSING", $"El flujo {flow.Id} no tiene cámara válida/consistente entre origen y devolución de devolución.", "ClearingHouseId"));
            }
        }

        if (failures.Count > 0)
        {
            return new(false, null, null, null, 0, flows.Select(x => (int)x.Id).ToArray(), failures, null, null);
        }

        var clearingHouseIds = flows
            .Select(x => x.ReturnOfReturnTransaction.AchCycle?.ClearingHouseId ?? x.SourceReturnTransaction!.AchCycle?.ClearingHouseId ?? x.OriginalTransaction!.AchCycle.ClearingHouseId)
            .Distinct()
            .ToArray();

        if (clearingHouseIds.Any(x => x <= 0) || clearingHouseIds.Length != 1)
        {
            failures.Add(new("CLEARING_HOUSE_MISSING", "No se pudo resolver una cámara única y válida para la generación del archivo.", "ClearingHouseId"));
            return new(false, null, null, null, 0, flows.Select(x => (int)x.Id).ToArray(), failures, null, null);
        }

        try
        {
            var clearingHouseId = clearingHouseIds[0];
            var fileName = $"ROR_{clearingHouseId}_{request.GeneratedAtUtc:yyyyMMddHHmmss}.ach";
            var lines = new List<string>
            {
                $"ROR|CH:{clearingHouseId}|TS:{request.GeneratedAtUtc:O}|COUNT:{flows.Count}"
            };

            lines.AddRange(flows.OrderBy(x => x.Id).Select(flow =>
                $"FLOW|{flow.Id}|SRC:{flow.SourceReturnTransactionId}|ROR:{flow.ReturnOfReturnTransactionId}|REASON:{flow.ReasonCode}|SRC_TRACE:{flow.SourceReturnTransaction?.TraceNumber ?? flow.OriginalTransaction?.TraceNumber}|ROR_TRACE:{flow.ReturnOfReturnTransaction.TraceNumber}"));

            var contentText = string.Join(Environment.NewLine, lines);
            var content = Encoding.ASCII.GetBytes(contentText);
            var contentSha256 = Convert.ToHexString(SHA256.HashData(content)).ToLowerInvariant();

            var audit = new AchReturnOfReturnGeneratedFileAudit
            {
                FileName = fileName,
                ClearingHouseId = clearingHouseId,
                GeneratedAtUtc = request.GeneratedAtUtc,
                GeneratedFlowCount = flows.Count,
                ContentLength = content.Length,
                ContentSha256 = contentSha256,
                RequestedBy = request.RequestedBy,
                Source = request.Source,
                CreatedAtUtc = DateTime.UtcNow,
                Flows = flows.OrderBy(x => x.Id).Select(x => new AchReturnOfReturnGeneratedFileAuditFlow
                {
                    ReturnOfReturnFlowId = x.Id
                }).ToList()
            };

            context.AchReturnOfReturnGeneratedFileAudits.Add(audit);
            await context.SaveChangesAsync(cancellationToken);

            return new(true, fileName, contentText, content, flows.Count, flows.Select(x => (int)x.Id).ToArray(), Array.Empty<AchReturnOfReturnFileGenerationFailure>(), audit.Id, contentSha256);
        }
        catch (Exception ex)
        {
            failures.Add(new("FILE_GENERATION_FAILED", ex.Message));
            return new(false, null, null, null, 0, flows.Select(x => (int)x.Id).ToArray(), failures, null, null);
        }
    }

    public async Task<AchReturnOfReturnFileGenerationResult> GenerateNachaAsync(AchReturnOfReturnFileGenerationRequest request, CancellationToken cancellationToken)
    {
        var failures = new List<AchReturnOfReturnFileGenerationFailure>();
        if (request.ReturnOfReturnFlowIds is null || request.ReturnOfReturnFlowIds.Count == 0)
        {
            failures.Add(new("RETURN_OF_RETURN_FLOW_EMPTY", "Debe enviar al menos un flujo de devolución de devolución.", nameof(request.ReturnOfReturnFlowIds)));
            return new(false, null, null, null, 0, Array.Empty<int>(), failures, null, null);
        }

        var requestedIds = request.ReturnOfReturnFlowIds.Distinct().OrderBy(x => x).ToArray();
        var flows = await context.ReturnOfReturnFlows
            .Include(x => x.SourceReturnTransaction!).ThenInclude(x => x.AchCycle).ThenInclude(x => x.ClearingHouse)
            .Include(x => x.OriginalTransaction).ThenInclude(x => x!.AchCycle).ThenInclude(x => x.ClearingHouse)
            .Include(x => x.ReturnOfReturnTransaction).ThenInclude(x => x.AchCycle).ThenInclude(x => x.ClearingHouse)
            .Where(x => requestedIds.Contains((int)x.Id))
            .ToListAsync(cancellationToken);

        if (flows.Count != requestedIds.Length)
        {
            failures.Add(new("RETURN_OF_RETURN_FLOW_NOT_FOUND", "No se encontraron todos los flujos solicitados.", nameof(request.ReturnOfReturnFlowIds)));
            return new(false, null, null, null, 0, flows.Select(x => (int)x.Id).ToArray(), failures, null, null);
        }

        foreach (var flow in flows)
        {
            if (flow.SourceReturnTransaction is null)
            {
                var sourceTx = await context.AchTransactions
                    .Include(x => x.AchCycle).ThenInclude(x => x.ClearingHouse)
                    .FirstOrDefaultAsync(x => x.Id == flow.SourceReturnTransactionId, cancellationToken);
                if (sourceTx is not null)
                {
                    flow.SourceReturnTransaction = sourceTx;
                }
            }

            if (flow.ReturnOfReturnTransaction is null)
            {
                var rorTx = await context.AchTransactions
                    .Include(x => x.AchCycle).ThenInclude(x => x.ClearingHouse)
                    .FirstOrDefaultAsync(x => x.Id == flow.ReturnOfReturnTransactionId, cancellationToken);
                if (rorTx is not null)
                {
                    flow.ReturnOfReturnTransaction = rorTx;
                }
            }
        }

        if (flows.Any(x => (x.SourceReturnTransaction is null && x.OriginalTransaction is null) || x.ReturnOfReturnTransaction is null))
        {
            failures.Add(new("RETURN_OF_RETURN_FLOW_NOT_FOUND", "No se pudo resolver la transacción origen o la transacción de devolución de devolución para todos los flujos.", nameof(request.ReturnOfReturnFlowIds)));
            return new(false, null, null, null, 0, flows.Select(x => (int)x.Id).ToArray(), failures, null, null);
        }

        var clearingHouseIds = flows
            .Select(x => x.ReturnOfReturnTransaction?.AchCycle?.ClearingHouseId
                         ?? x.SourceReturnTransaction?.AchCycle?.ClearingHouseId
                         ?? x.OriginalTransaction?.AchCycle?.ClearingHouseId
                         ?? 0)
            .Distinct()
            .ToArray();
        if (clearingHouseIds.Any(x => x <= 0) || clearingHouseIds.Length != 1)
        {
            failures.Add(new("CLEARING_HOUSE_MISSING", "No se pudo resolver una cámara única y válida para la generación NACHA.", "ClearingHouseId"));
            return new(false, null, null, null, 0, requestedIds, failures, null, null);
        }

        var sourceValue = request.Source?.Trim();
        var productiveSourceValue = BuildProductiveSourceMarker(sourceValue);
        var candidateAudits = await context.AchReturnOfReturnGeneratedFileAudits
            .Include(x => x.Flows)
            .Where(x => x.Source == "nacha" || (x.Source != null && x.Source.StartsWith("nacha:")))
            .Where(x => x.GeneratedFlowCount == requestedIds.Length)
            .ToListAsync(cancellationToken);
        var duplicate = candidateAudits.Any(x => x.Flows.Select(f => (int)f.ReturnOfReturnFlowId).OrderBy(v => v).SequenceEqual(requestedIds));
        if (duplicate)
        {
            failures.Add(new("DUPLICATE_PRODUCTIVE_GENERATION", "Ya existe una generación NACHA productiva para el mismo conjunto de flujos."));
            return new(false, null, null, null, 0, requestedIds, failures, null, null);
        }

        var now = request.GeneratedAtUtc;
        var clearingHouseId = clearingHouseIds[0];
        var firstCycle = flows.First().ReturnOfReturnTransaction.AchCycle;
        if (string.Equals(firstCycle.ClearingHouse?.Code, "CENIT", StringComparison.OrdinalIgnoreCase))
        {
            return await GenerateCenitNachaAsync(flows, request, productiveSourceValue, cancellationToken);
        }
        var recordConfig = ResolveNachaRecordConfig(clearingHouseId, firstCycle.ClearingHouse);
        var originCode = NormalizeDigits(firstCycle.ClearingHouse?.OriginCode ?? recordConfig.Record1.ImmediateOrigin, 8);
        var provisionalFileName = $"RORNACHA_{clearingHouseId}_{now:yyyyMMddHHmmss}.ach";
        var entryLines = new List<string>();
        var addendaLines = new List<string>();
        var entryCodes = new List<(string txCode, decimal amount, string receivingDfi)>();

        foreach (var flow in flows.OrderBy(x => x.Id))
        {
            var tx = flow.ReturnOfReturnTransaction;
            var original = flow.SourceReturnTransaction!;
            var amount = tx.IsPrenotification ? 0m : tx.Amount;
            var newTrace = NormalizeDigits(tx.TraceNumber, 15);
            var originalTrace = NormalizeDigits(original.TraceNumber, 15);
            var receiving = NormalizeDigits(tx.ReceivingDFI, 8);
            var txCode = NormalizeDigits(tx.TransactionCode, 2);
            entryLines.Add(BuildType6(txCode, receiving, tx.DestinationAccountNumber, amount, newTrace));
            addendaLines.Add(BuildType7(flow.ReasonCode, originalTrace, newTrace, newTrace[^7..]));
            entryCodes.Add((txCode, amount, receiving));
        }

        var serviceClassCode = ResolveServiceClassCode(entryCodes.Select(x => x.txCode));
        var totalDebit = entryCodes.Where(x => IsDebit(x.txCode)).Sum(x => x.amount);
        var totalCredit = entryCodes.Where(x => !IsDebit(x.txCode)).Sum(x => x.amount);
        var hash = ComputeHash(entryCodes.Select(x => x.receivingDfi));
        var lines = new List<string>
        {
            BuildType1(now, originCode, recordConfig),
            BuildType5(now, serviceClassCode, recordConfig.Record5.CompanyIdentification, recordConfig.Record5.BatchNumberDefault, originCode, recordConfig)
        };
        for (var i = 0; i < entryLines.Count; i++) { lines.Add(entryLines[i]); lines.Add(addendaLines[i]); }
        lines.Add(BuildType8(entryLines.Count, addendaLines.Count, hash, totalDebit, totalCredit, serviceClassCode, recordConfig.Record89.CompanyIdentification, originCode, recordConfig.Record89.BatchNumber));
        var totalRecordsWithControl = lines.Count + 1;
        var blockCount = (int)Math.Ceiling(totalRecordsWithControl / 10m);
        var paddingNeeded = (blockCount * 10) - totalRecordsWithControl;
        lines.Add(BuildType9(1, totalRecordsWithControl, entryLines.Count + addendaLines.Count, hash, totalDebit, totalCredit));
        for (int i = 0; i < paddingNeeded; i++) lines.Add(new string('9', 106));

        var contentText = string.Concat(lines);
        var content = Encoding.ASCII.GetBytes(contentText);
        var contentSha256 = Convert.ToHexString(SHA256.HashData(content)).ToLowerInvariant();
        var (isFileNameResolved, fileNameError, fileName) = await ResolveProductiveExternalFileNameAsync(
            clearingHouseId,
            firstCycle.ClearingHouse,
            firstCycle,
            provisionalFileName,
            contentText,
            request,
            cancellationToken);
        if (!isFileNameResolved)
        {
            failures.Add(new("EXTERNAL_FILENAME_VALIDATION_FAILED", fileNameError ?? "No se pudo validar el nombre externo del archivo NACHA."));
            return new(false, null, null, null, 0, requestedIds, failures, null, null);
        }

        var audit = new AchReturnOfReturnGeneratedFileAudit
        {
            FileName = fileName,
            ClearingHouseId = clearingHouseId,
            GeneratedAtUtc = now,
            GeneratedFlowCount = flows.Count,
            ContentLength = content.Length,
            ContentSha256 = contentSha256,
            RequestedBy = request.RequestedBy,
            Source = productiveSourceValue,
            CreatedAtUtc = DateTime.UtcNow,
            Flows = flows.OrderBy(x => x.Id).Select(x => new AchReturnOfReturnGeneratedFileAuditFlow { ReturnOfReturnFlowId = x.Id }).ToList()
        };
        context.AchReturnOfReturnGeneratedFileAudits.Add(audit);
        await context.SaveChangesAsync(cancellationToken);
        return new(true, fileName, contentText, content, flows.Count, flows.Select(x => (int)x.Id).ToArray(), Array.Empty<AchReturnOfReturnFileGenerationFailure>(), audit.Id, contentSha256);
    }

    private async Task<AchReturnOfReturnFileGenerationResult> GenerateCenitNachaAsync(
        IReadOnlyList<ReturnOfReturnFlow> flows,
        AchReturnOfReturnFileGenerationRequest request,
        string productiveSourceValue,
        CancellationToken ct)
    {
        var flowIds = flows.Select(x => (int)x.Id).ToArray();
        if (_nachaFileBuilder is null)
            return Failure(flowIds, "CENIT_ROR_OPTION_C_REQUIRED", "INachaFileBuilder es requerido para ROR CENIT; no existe fallback hardcodeado.");
        if (flows.Any(x => x.Direction != "Out" || !x.ParentIncomingReturnStateEventId.HasValue))
            return Failure(flowIds, "CENIT_ROR_OUT_PARENT_REQUIRED", "ROR Out CENIT requiere un evento Return In padre.");

        var semanticEntries = new List<NachaReturnOutEntry>();
        BatchHeader? sourceBatch = null;
        foreach (var flow in flows.OrderBy(x => x.Id))
        {
            var parentEventId = flow.ParentIncomingReturnStateEventId!.Value;
            var parentEvent = await context.AchTransactionStateEvents.AsNoTracking().SingleAsync(x => x.Id == parentEventId, ct);
            var artifact = await context.IncomingNachaTransactionLinks
                .Include(x => x.EntryDetail).ThenInclude(x => x!.BatchHeader)
                .Include(x => x.AddendaRecord)
                .Where(x => x.AchTransactionId == parentEvent.AchTransactionId
                            && x.AddendaRecord != null
                            && x.AddendaRecord.ReturnReasonCode == parentEvent.ReasonCode)
                .OrderByDescending(x => x.LinkedAtUtc)
                .FirstOrDefaultAsync(ct);
            if (artifact?.EntryDetail?.BatchHeader is null || artifact.AddendaRecord is null)
                return Failure(flowIds, "PARENT_INCOMING_RETURN_RAW_NOT_FOUND", $"No existe evidencia raw para el flujo {flow.Id}.");
            if (!string.Equals(artifact.EntryDetail.BatchHeader.StandardEntryClassCode?.Trim(), "PPD", StringComparison.OrdinalIgnoreCase))
                return Failure(flowIds, CenitReturnOfReturn2026Layout.CcdScopeStatus, "El contrato específico vigente de ROR CENIT está definido para PPD.");

            sourceBatch ??= artifact.EntryDetail.BatchHeader;
            var ror = flow.ReturnOfReturnTransaction;
            var sourceReason = (parentEvent.ReasonCode ?? string.Empty).Trim().ToUpperInvariant();
            semanticEntries.Add(new NachaReturnOutEntry(
                ror.Id,
                artifact.EntryDetail.TransactionCode?.Trim() ?? string.Empty,
                artifact.EntryDetail.ReceivingParticipantEntityCode?.Trim() ?? string.Empty,
                artifact.EntryDetail.CheckDigit?.Trim() ?? string.Empty,
                artifact.EntryDetail.AccountNumber?.TrimEnd() ?? string.Empty,
                artifact.EntryDetail.Amount ?? 0m,
                artifact.EntryDetail.RecipIdNumber?.TrimEnd() ?? string.Empty,
                artifact.EntryDetail.RecipUserName?.TrimEnd() ?? string.Empty,
                artifact.EntryDetail.DiscreData?.TrimEnd() ?? string.Empty,
                ror.TraceNumber,
                flow.ReasonCode,
                artifact.AddendaRecord.OriginalTraceNumber?.Trim() ?? string.Empty,
                string.Empty,
                artifact.AddendaRecord.IdUserOrig?.Trim() ?? string.Empty,
                string.Empty,
                ror.TraceNumber,
                artifact.EntryDetail.SequenceNumber?.Trim() ?? string.Empty,
                (artifact.EntryDetail.BatchHeader.CompensationDate ?? parentEvent.OccurredAtUtc.DayOfYear.ToString("D3")).Trim(),
                sourceReason.TrimStart('R')));
        }

        var cycle = flows[0].ReturnOfReturnTransaction.AchCycle;
        var clearingHouse = cycle.ClearingHouse!;
        var participant = new string(flows[0].ReturnOfReturnTransaction.OriginatingDFI.Where(char.IsDigit).ToArray());
        participant = participant.Length >= 8 ? participant[..8] : participant.PadLeft(8, '0');
        var batch = new NachaReturnOutBatch(
            ResolveServiceClassCode(semanticEntries.Select(x => x.TransactionCode)).ToString("D3"),
            sourceBatch?.CompanyName?.TrimEnd() ?? flows[0].ReturnOfReturnTransaction.CompanyName,
            sourceBatch?.DiscretionaryData?.TrimEnd() ?? string.Empty,
            sourceBatch?.CompanyId?.TrimEnd() ?? flows[0].ReturnOfReturnTransaction.CompanyIdentification,
            "PPD",
            sourceBatch?.CompanyEntryDescription?.TrimEnd() ?? "ROR",
            request.GeneratedAtUtc.Date,
            request.GeneratedAtUtc.Date,
            request.GeneratedAtUtc.DayOfYear.ToString("D3"),
            participant,
            1,
            semanticEntries);
        var build = await _nachaFileBuilder.BuildReturnOutAsync(new NachaReturnOutBuildRequest(
            request.GeneratedAtUtc,
            "A",
            clearingHouse.OriginCode ?? participant,
            participant,
            clearingHouse.Name,
            flows[0].ReturnOfReturnTransaction.CompanyName,
            "ROR",
            [batch],
            PersistAudit: true,
            ClearingHouseCode: "CENIT",
            ClearingHouseName: clearingHouse.Name,
            NormativeVersion: CenitReturnOfReturn2026Layout.NormativeVersion,
            FlowTypeCode: CenitReturnOfReturn2026Layout.FlowTypeCode), ct);

        var provisionalName = $"RORNACHA_{clearingHouse.Id}_{request.GeneratedAtUtc:yyyyMMddHHmmss}.ach";
        var (resolved, error, fileName) = await ResolveProductiveExternalFileNameAsync(
            clearingHouse.Id, clearingHouse, cycle, provisionalName, build.Content, request, ct);
        if (!resolved) return Failure(flowIds, "EXTERNAL_FILENAME_VALIDATION_FAILED", error ?? "Nombre externo inválido.");

        var content = Encoding.ASCII.GetBytes(build.Content);
        var sha = Convert.ToHexString(SHA256.HashData(content)).ToLowerInvariant();
        var audit = new AchReturnOfReturnGeneratedFileAudit
        {
            FileName = fileName,
            ClearingHouseId = clearingHouse.Id,
            GeneratedAtUtc = request.GeneratedAtUtc,
            GeneratedFlowCount = flows.Count,
            ContentLength = content.Length,
            ContentSha256 = sha,
            RequestedBy = request.RequestedBy,
            Source = productiveSourceValue,
            CreatedAtUtc = DateTime.UtcNow,
            Flows = flows.OrderBy(x => x.Id).Select(x => new AchReturnOfReturnGeneratedFileAuditFlow { ReturnOfReturnFlowId = x.Id }).ToList()
        };
        context.AchReturnOfReturnGeneratedFileAudits.Add(audit);
        await context.SaveChangesAsync(ct);
        return new(true, fileName, build.Content, content, flows.Count, flowIds, [], audit.Id, sha);
    }

    private static AchReturnOfReturnFileGenerationResult Failure(IReadOnlyCollection<int> flowIds, string code, string message)
        => new(false, null, null, null, 0, flowIds, [new(code, message)], null, null);


    private void ValidateNachaRecords(int clearingHouseId, string? clearingHouseCode, NachaRecordFlow flow, NachaRailRecordConfig config, string content)
    {
        if (_nachaRecordFieldValidator is null) return;
        var result = _nachaRecordFieldValidator.Validate(new NachaRecordValidationContext(clearingHouseId, clearingHouseCode, flow, NachaRecordDirection.Outbound, config, content, true));
        foreach (var w in result.Issues.Where(i => i.Severity != NachaRecordValidationSeverity.Error))
            _logger.LogWarning("NACHA_RECORD_VALIDATION_{Severity}|Code={Code}|Message={Message}", w.Severity, w.Code, w.Message);
        if (result.HasErrors) throw new InvalidOperationException("NACHA record validation failed.");
    }

    private async Task<(bool IsResolved, string? Error, string FileName)> ResolveProductiveExternalFileNameAsync(
        int clearingHouseId,
        ClearingHouse? clearingHouse,
        AchCycle firstCycle,
        string provisionalFileName,
        string nachaContent,
        AchReturnOfReturnFileGenerationRequest request,
        CancellationToken cancellationToken)
    {
        if (_externalFileNamePolicy is null)
        {
            return (true, null, provisionalFileName);
        }

        var contextPolicy = new ExternalFileNameContext
        {
            ClearingHouseId = clearingHouseId,
            ClearingHouseCode = clearingHouse?.Code ?? string.Empty,
            ClearingHouseOriginCode = clearingHouse?.OriginCode,
            CycleId = firstCycle.Id,
            CycleName = firstCycle.CycleName,
            ProcessingDate = firstCycle.ProcessingDate,
            ExternalFileType = ExternalFileType.ReturnOfReturnOut,
            Flow = ExternalFileFlow.Originacion,
            Direction = ExternalFileDirection.Outbound,
            InternalFileName = provisionalFileName,
            NachaContent = nachaContent,
            RequestedBy = request.RequestedBy ?? "system"
        };

        var policyResult = await _externalFileNamePolicy.GenerateExternalNameAsync(contextPolicy, cancellationToken);
        if (policyResult.Validation.IsHardBlocked)
        {
            var details = string.Join(" | ", policyResult.Validation.Issues.Select(x => $"{x.RuleCode}:{x.Message}"));
            return (false, $"External filename validation failed. {details}", provisionalFileName);
        }

        if (policyResult.Validation.Issues.Count > 0)
        {
            _logger.LogWarning("ROR_PRODUCTIVE_FILENAME_POLICY_WARNING|CycleId={CycleId}|FileName={FileName}|Issues={Issues}",
                firstCycle.Id,
                policyResult.ExternalFileName,
                string.Join(" | ", policyResult.Validation.Issues.Select(x => $"{x.RuleCode}:{x.Message}")));
        }

        var resolved = string.IsNullOrWhiteSpace(policyResult.ExternalFileName)
            ? provisionalFileName
            : policyResult.ExternalFileName;

        return (true, null, resolved);
    }


    private NachaRailRecordConfig ResolveNachaRecordConfig(int clearingHouseId, ClearingHouse? clearingHouse)
    {
        if (_nachaRecordConfigProvider is not null)
        {
            return _nachaRecordConfigProvider.Resolve(clearingHouseId, clearingHouse?.Code, NachaRecordFlow.ReturnOfReturnOut, NachaRecordDirection.Outbound);
        }

        return new NachaRailRecordConfig(
            RailCode: clearingHouse?.Code ?? "UNKNOWN",
            ClearingHouseId: clearingHouseId,
            Flow: NachaRecordFlow.ReturnOfReturnOut,
            Direction: NachaRecordDirection.Outbound,
            IsCurrentLayout: true,
            IsProductiveApproved: false,
            Record1: new NachaRecord1Config("000101006", "000101006", "ACH COLOMBIA", "ACHINTERBANK ROR", "A", "0001", 106, 10, 1),
            Record5: new NachaRecord5Config(null, "DEV. DEV.", "BANCROR", "PPD", "RETORNO", "1", "00010100", "0000001"),
            Record7: new NachaRecord7Config("99", "CurrentLayout/TransactionReasonCode", "CurrentLayout/OriginalTrace15"),
            Record89: new NachaRecord89Config("BANCROR", "00010100", "0000001", "CurrentLayout/PadWithRecord9"));
    }

    private static string BuildProductiveSourceMarker(string? sourceValue)
    {
        if (string.IsNullOrWhiteSpace(sourceValue))
        {
            return "nacha";
        }

        const string marker = "nacha:";
        if (sourceValue.StartsWith(marker, StringComparison.OrdinalIgnoreCase))
        {
            var rest = sourceValue[marker.Length..];
            return string.IsNullOrWhiteSpace(rest)
                ? "nacha"
                : $"nacha:{rest}";
        }

        return $"nacha:{sourceValue}";
    }

    private static string BuildType1(DateTime now, string originCode, NachaRailRecordConfig recordConfig) => $"101  {originCode}{originCode}{now:yyMMdd}{now:HHmm}{recordConfig.Record1.FileIdModifier}094{recordConfig.Record1.BlockingFactor:00}{recordConfig.Record1.FormatCode}{recordConfig.Record1.ImmediateDestinationName.PadRight(23).Substring(0,23)}{recordConfig.Record1.ImmediateOriginName.PadRight(23).Substring(0,23)}{now:yyMMdd}{recordConfig.Record1.ReferenceCode.PadLeft(4,'0')}".PadRight(recordConfig.Record1.RecordSize, ' ');
    private static string BuildType5(DateTime now, int serviceClassCode, string originatorId, string batchNumber, string originatingDfi, NachaRailRecordConfig recordConfig) => $"5{serviceClassCode:000}ROR COMPANY      {originatorId}{recordConfig.Record5.CompanyName.PadRight(10).Substring(0,10)}{now:yyMMdd}{now:yyMMdd}   {recordConfig.Record5.OriginatorStatusCode}{originatingDfi}{batchNumber}".PadRight(recordConfig.Record1.RecordSize, ' ');
    private static string BuildType6(string txCode, string receivingDfi, string account, decimal amount, string trace) => $"6{txCode}{receivingDfi} {account.PadRight(17).Substring(0, 17)}{(long)(amount * 100):0000000000}               0{trace}".PadRight(106, ' ');
    private static string BuildType7(string reason, string originalTrace, string newTrace, string seq) => $"799{reason.PadRight(3).Substring(0, 3)}{originalTrace}{newTrace}{seq}".PadRight(106, ' ');
    private static string BuildType8(int entryCount, int addendaCount, long hash, decimal totalDebit, decimal totalCredit, int serviceClassCode, string originatorId, string originatingDfi, string batchNumber) => $"8{serviceClassCode:000}{entryCount + addendaCount:000000}{hash:0000000000}{(long)(totalDebit * 100):000000000000}{(long)(totalCredit * 100):000000000000}{originatorId.PadRight(10).Substring(0, 10)}      {originatingDfi}{batchNumber}".PadRight(106, ' ');
    private static string BuildType9(int batchCount, int totalRecords, int entryAddendaCount, long hash, decimal totalDebit, decimal totalCredit) => $"9{batchCount:000000}{totalRecords:000000}{entryAddendaCount:00000000}{hash:0000000000}{(long)(totalDebit * 100):000000000000}{(long)(totalCredit * 100):000000000000}".PadRight(106, ' ');
    private static string NormalizeDigits(string? value, int len) => new string((value ?? string.Empty).Where(char.IsDigit).ToArray()).PadLeft(len, '0')[^len..];
    private static int ResolveServiceClassCode(IEnumerable<string> txCodes) => txCodes.Any(IsDebit) && txCodes.Any(x => !IsDebit(x)) ? 200 : (txCodes.Any(IsDebit) ? 225 : 220);
    private static bool IsDebit(string txCode) => txCode is "26" or "27" or "28" or "36" or "37" or "38" or "55" or "56" or "57";
    private static long ComputeHash(IEnumerable<string> receiving) => receiving.Select(x => long.TryParse(NormalizeDigits(x, 8), out var n) ? n : 0).Sum() % 10_000_000_000L;
}

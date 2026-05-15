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
    ILogger<AchReturnOfReturnFileGenerationService>? logger = null) : IAchReturnOfReturnFileGenerationService
{
    private readonly IExternalFileNamePolicy? _externalFileNamePolicy = externalFileNamePolicy;
    private readonly ILogger<AchReturnOfReturnFileGenerationService> _logger = logger ?? NullLogger<AchReturnOfReturnFileGenerationService>.Instance;
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
            .Include(x => x.SourceReturnTransaction).ThenInclude(x => x.AchCycle)
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
            if (flow.SourceReturnTransaction is null)
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
            var sourceClearingHouseId = flow.SourceReturnTransaction?.AchCycle?.ClearingHouseId ?? 0;
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
            .Select(x => x.ReturnOfReturnTransaction.AchCycle?.ClearingHouseId ?? x.SourceReturnTransaction.AchCycle?.ClearingHouseId ?? 0)
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
                $"FLOW|{flow.Id}|SRC:{flow.SourceReturnTransactionId}|ROR:{flow.ReturnOfReturnTransactionId}|REASON:{flow.ReasonCode}|SRC_TRACE:{flow.SourceReturnTransaction.TraceNumber}|ROR_TRACE:{flow.ReturnOfReturnTransaction.TraceNumber}"));

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
            .Include(x => x.SourceReturnTransaction).ThenInclude(x => x.AchCycle).ThenInclude(x => x.ClearingHouse)
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
            flow.SourceReturnTransaction ??= await context.AchTransactions.Include(x => x.AchCycle).ThenInclude(x => x.ClearingHouse).FirstOrDefaultAsync(x => x.Id == flow.SourceReturnTransactionId, cancellationToken);
            flow.ReturnOfReturnTransaction ??= await context.AchTransactions.Include(x => x.AchCycle).ThenInclude(x => x.ClearingHouse).FirstOrDefaultAsync(x => x.Id == flow.ReturnOfReturnTransactionId, cancellationToken);
        }

        var clearingHouseIds = flows
            .Select(x => x.ReturnOfReturnTransaction?.AchCycle?.ClearingHouseId ?? x.SourceReturnTransaction?.AchCycle?.ClearingHouseId ?? 0)
            .Distinct()
            .ToArray();
        if (clearingHouseIds.Any(x => x <= 0) || clearingHouseIds.Length != 1)
        {
            failures.Add(new("CLEARING_HOUSE_MISSING", "No se pudo resolver una cámara única y válida para la generación NACHA.", "ClearingHouseId"));
            return new(false, null, null, null, 0, requestedIds, failures, null, null);
        }

        var sourceValue = request.Source?.Trim();
        var candidateAudits = await context.AchReturnOfReturnGeneratedFileAudits
            .Include(x => x.Flows)
            .Where(x => x.Source == "nacha")
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
        var originCode = NormalizeDigits(firstCycle.ClearingHouse?.OriginCode ?? "000101006", 8);
        var provisionalFileName = $"RORNACHA_{clearingHouseId}_{now:yyyyMMddHHmmss}.ach";
        var entryLines = new List<string>();
        var addendaLines = new List<string>();
        var entryCodes = new List<(string txCode, decimal amount, string receivingDfi)>();

        foreach (var flow in flows.OrderBy(x => x.Id))
        {
            var tx = flow.ReturnOfReturnTransaction;
            var original = flow.SourceReturnTransaction;
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
            BuildType1(now, originCode),
            BuildType5(now, serviceClassCode, "BANCROR", "0000001", originCode)
        };
        for (var i = 0; i < entryLines.Count; i++) { lines.Add(entryLines[i]); lines.Add(addendaLines[i]); }
        lines.Add(BuildType8(entryLines.Count, addendaLines.Count, hash, totalDebit, totalCredit, serviceClassCode, "BANCROR", originCode, "0000001"));
        var totalRecordsWithControl = lines.Count + 1;
        var blockCount = (int)Math.Ceiling(totalRecordsWithControl / 10m);
        var paddingNeeded = (blockCount * 10) - totalRecordsWithControl;
        lines.Add(BuildType9(1, totalRecordsWithControl, entryLines.Count + addendaLines.Count, hash, totalDebit, totalCredit));
        for (int i = 0; i < paddingNeeded; i++) lines.Add(new string('9', 106));

        var contentText = string.Concat(lines);
        var content = Encoding.ASCII.GetBytes(contentText);
        var contentSha256 = Convert.ToHexString(SHA256.HashData(content)).ToLowerInvariant();
        var fileName = await ResolveProductiveExternalFileNameAsync(
            clearingHouseId,
            firstCycle.ClearingHouse,
            firstCycle,
            provisionalFileName,
            contentText,
            request,
            cancellationToken);
        var audit = new AchReturnOfReturnGeneratedFileAudit
        {
            FileName = fileName,
            ClearingHouseId = clearingHouseId,
            GeneratedAtUtc = now,
            GeneratedFlowCount = flows.Count,
            ContentLength = content.Length,
            ContentSha256 = contentSha256,
            RequestedBy = request.RequestedBy,
            Source = sourceValue,
            CreatedAtUtc = DateTime.UtcNow,
            Flows = flows.OrderBy(x => x.Id).Select(x => new AchReturnOfReturnGeneratedFileAuditFlow { ReturnOfReturnFlowId = x.Id }).ToList()
        };
        context.AchReturnOfReturnGeneratedFileAudits.Add(audit);
        await context.SaveChangesAsync(cancellationToken);
        return new(true, fileName, contentText, content, flows.Count, flows.Select(x => (int)x.Id).ToArray(), Array.Empty<AchReturnOfReturnFileGenerationFailure>(), audit.Id, contentSha256);
    }

    private async Task<string> ResolveProductiveExternalFileNameAsync(
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
            return provisionalFileName;
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
            throw new InvalidOperationException($"Error Fatal ID: External filename validation failed. {details}");
        }

        if (policyResult.Validation.Issues.Count > 0)
        {
            _logger.LogWarning("ROR_PRODUCTIVE_FILENAME_POLICY_WARNING|CycleId={CycleId}|FileName={FileName}|Issues={Issues}",
                firstCycle.Id,
                policyResult.ExternalFileName,
                string.Join(" | ", policyResult.Validation.Issues.Select(x => $"{x.RuleCode}:{x.Message}")));
        }

        return string.IsNullOrWhiteSpace(policyResult.ExternalFileName)
            ? provisionalFileName
            : policyResult.ExternalFileName;
    }

    private static string BuildType1(DateTime now, string originCode) => $"101  {originCode}{originCode}{now:yyMMdd}{now:HHmm}A094101ACH COLOMBIA       ACHINTERBANK ROR  {now:yyMMdd}0001".PadRight(106, ' ');
    private static string BuildType5(DateTime now, int serviceClassCode, string originatorId, string batchNumber, string originatingDfi) => $"5{serviceClassCode:000}ROR COMPANY      {originatorId}DEV. DEV.  {now:yyMMdd}{now:yyMMdd}   1{originatingDfi}{batchNumber}".PadRight(106, ' ');
    private static string BuildType6(string txCode, string receivingDfi, string account, decimal amount, string trace) => $"6{txCode}{receivingDfi} {account.PadRight(17).Substring(0, 17)}{(long)(amount * 100):0000000000}               0{trace}".PadRight(106, ' ');
    private static string BuildType7(string reason, string originalTrace, string newTrace, string seq) => $"799{reason.PadRight(3).Substring(0, 3)}{originalTrace}{newTrace}{seq}".PadRight(106, ' ');
    private static string BuildType8(int entryCount, int addendaCount, long hash, decimal totalDebit, decimal totalCredit, int serviceClassCode, string originatorId, string originatingDfi, string batchNumber) => $"8{serviceClassCode:000}{entryCount + addendaCount:000000}{hash:0000000000}{(long)(totalDebit * 100):000000000000}{(long)(totalCredit * 100):000000000000}{originatorId.PadRight(10).Substring(0, 10)}      {originatingDfi}{batchNumber}".PadRight(106, ' ');
    private static string BuildType9(int batchCount, int totalRecords, int entryAddendaCount, long hash, decimal totalDebit, decimal totalCredit) => $"9{batchCount:000000}{totalRecords:000000}{entryAddendaCount:00000000}{hash:0000000000}{(long)(totalDebit * 100):000000000000}{(long)(totalCredit * 100):000000000000}".PadRight(106, ' ');
    private static string NormalizeDigits(string? value, int len) => new string((value ?? string.Empty).Where(char.IsDigit).ToArray()).PadLeft(len, '0')[^len..];
    private static int ResolveServiceClassCode(IEnumerable<string> txCodes) => txCodes.Any(IsDebit) && txCodes.Any(x => !IsDebit(x)) ? 200 : (txCodes.Any(IsDebit) ? 225 : 220);
    private static bool IsDebit(string txCode) => txCode is "26" or "27" or "28" or "36" or "37" or "38" or "55" or "56" or "57";
    private static long ComputeHash(IEnumerable<string> receiving) => receiving.Select(x => long.TryParse(NormalizeDigits(x, 8), out var n) ? n : 0).Sum() % 10_000_000_000L;
}

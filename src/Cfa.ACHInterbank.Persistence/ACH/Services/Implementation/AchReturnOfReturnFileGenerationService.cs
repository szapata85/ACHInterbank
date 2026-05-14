using System.Text;
using Cfa.ACHInterbank.Application.ACH.Interfaces;
using Cfa.ACHInterbank.Application.ACH.Models;
using Cfa.ACHInterbank.Persistence.DataBase;
using Microsoft.EntityFrameworkCore;

namespace Cfa.ACHInterbank.Persistence.ACH.Services.Implementation;

public class AchReturnOfReturnFileGenerationService(AchDbContext context) : IAchReturnOfReturnFileGenerationService
{
    public async Task<AchReturnOfReturnFileGenerationResult> GenerateAsync(AchReturnOfReturnFileGenerationRequest request, CancellationToken cancellationToken)
    {
        var failures = new List<AchReturnOfReturnFileGenerationFailure>();
        if (request.ReturnOfReturnFlowIds is null || request.ReturnOfReturnFlowIds.Count == 0)
        {
            failures.Add(new("RETURN_OF_RETURN_FLOW_EMPTY", "Debe enviar al menos un flujo de devolución de devolución.", nameof(request.ReturnOfReturnFlowIds)));
            return new(false, null, null, null, 0, Array.Empty<int>(), failures);
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
            return new(false, null, null, null, 0, Array.Empty<int>(), failures);
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
            return new(false, null, null, null, 0, flows.Select(x => (int)x.Id).ToArray(), failures);
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
            return new(false, null, null, null, 0, flows.Select(x => (int)x.Id).ToArray(), failures);
        }

        var clearingHouseIds = flows
            .Select(x => x.ReturnOfReturnTransaction.AchCycle?.ClearingHouseId ?? x.SourceReturnTransaction.AchCycle?.ClearingHouseId ?? 0)
            .Distinct()
            .ToArray();

        if (clearingHouseIds.Any(x => x <= 0) || clearingHouseIds.Length != 1)
        {
            failures.Add(new("CLEARING_HOUSE_MISSING", "No se pudo resolver una cámara única y válida para la generación del archivo.", "ClearingHouseId"));
            return new(false, null, null, null, 0, flows.Select(x => (int)x.Id).ToArray(), failures);
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
            return new(true, fileName, contentText, Encoding.ASCII.GetBytes(contentText), flows.Count, flows.Select(x => (int)x.Id).ToArray(), Array.Empty<AchReturnOfReturnFileGenerationFailure>());
        }
        catch (Exception ex)
        {
            failures.Add(new("FILE_GENERATION_FAILED", ex.Message));
            return new(false, null, null, null, 0, flows.Select(x => (int)x.Id).ToArray(), failures);
        }
    }
}

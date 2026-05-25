using Cfa.ACHInterbank.Application.ACH.Interfaces;
using Cfa.ACHInterbank.Application.ACH.Models;
using Cfa.ACHInterbank.Domain.Models.Configurations;

namespace Cfa.ACHInterbank.Persistence.ACH.Services.Implementation;

[Scoped]
public sealed class NachaSoapDryRunOperationExecutor : INachaSoapOperationExecutor
{
    private readonly INachaSoapRequestMapper _mapper;

    public NachaSoapDryRunOperationExecutor(INachaSoapRequestMapper mapper)
    {
        _mapper = mapper;
    }

    public Task<NachaSoapExecutionResult> ExecuteAsync(NachaSoapExecutionRequest request, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var mapped = _mapper.Map(request);
        if (!request.IsEnabled)
        {
            return Task.FromResult(BuildSkipped(mapped, "SOAP controlado deshabilitado para Fase 6B.5.1."));
        }

        if (!mapped.IsExecutable)
        {
            return Task.FromResult(new NachaSoapExecutionResult
            {
                Status = NachaSoapExecutionStatus.Rejected,
                MappedRequest = mapped,
                SoapWasInvoked = false,
                ProductiveExecution = false,
                Message = "Decision NACHA-M no ejecutable por gateway SOAP controlado.",
                Errors = mapped.Errors,
                Trace = BuildTrace(mapped, NachaSoapExecutionStatus.Rejected)
            });
        }

        if (!request.DryRun)
        {
            return Task.FromResult(new NachaSoapExecutionResult
            {
                Status = NachaSoapExecutionStatus.BlockedByNoGo,
                MappedRequest = mapped,
                SoapWasInvoked = false,
                ProductiveExecution = false,
                Message = "Ejecucion SOAP real bloqueada en Fase 6B.5.1. Productivo permanece NO-GO.",
                Trace = BuildTrace(mapped, NachaSoapExecutionStatus.BlockedByNoGo)
            });
        }

        return Task.FromResult(new NachaSoapExecutionResult
        {
            Status = NachaSoapExecutionStatus.DryRunCompleted,
            MappedRequest = mapped,
            SoapWasInvoked = false,
            ProductiveExecution = false,
            Message = "Dry-run completado. No se invoco SOAP real.",
            Trace = BuildTrace(mapped, NachaSoapExecutionStatus.DryRunCompleted)
        });
    }

    private static NachaSoapExecutionResult BuildSkipped(NachaSoapMappedRequest mapped, string message)
        => new()
        {
            Status = NachaSoapExecutionStatus.Skipped,
            MappedRequest = mapped,
            SoapWasInvoked = false,
            ProductiveExecution = false,
            Message = message,
            Trace = BuildTrace(mapped, NachaSoapExecutionStatus.Skipped)
        };

    private static Dictionary<string, string> BuildTrace(NachaSoapMappedRequest mapped, NachaSoapExecutionStatus status)
        => new(StringComparer.OrdinalIgnoreCase)
        {
            ["Phase"] = "6B.5",
            ["Status"] = status.ToString(),
            ["Operation"] = mapped.Operation.ToString(),
            ["SoapWasInvoked"] = "false",
            ["ProductiveExecution"] = "false",
            ["NoGoReason"] = "Productivo permanece NO-GO; no hay ejecucion SOAP real en 6B.5.1."
        };
}

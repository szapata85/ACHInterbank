#!/usr/bin/env python3
"""I06: checks de fuente / existencia de pruebas, NO dotnet build ni xUnit."""
from pathlib import Path
import sys
r=Path(__file__).resolve().parents[2]
s=r/'src/Cfa.ACHInterbank.Persistence/ACH/Services/Implementation'
execution=(s/'CenitCycleExecutionService.cs').read_text(encoding='utf-8-sig')
netting=(s/'CenitNettingService.cs').read_text(encoding='utf-8-sig')
liquidity=(s/'LiquidityOptimizationService.cs').read_text(encoding='utf-8-sig')
response=(s/'CenitChamberResponseService.cs').read_text(encoding='utf-8-sig')
operational=(r/'tests/Cfa.ACHInterbank.Tests/CenitOperationalGovernanceTests.cs').read_text(encoding='utf-8-sig')
lifecycle=(r/'tests/Cfa.ACHInterbank.Tests/CenitChamberResponseLifecycleTests.cs').read_text(encoding='utf-8-sig')
checks={
 'Quartz restringe ejecución CENIT': 'La ejecución dedicada CENIT solo aplica' in execution,
 'Quartz consulta un registro persistido por ciclo': '.FirstOrDefaultAsync(x => x.AchCycleId == cycle.Id, ct)' in execution,
 'Quartz idempotente si AwaitingOfficialResult': 'string.Equals(execution.Status, "AwaitingOfficialResult"' in execution,
 'Quartz conserva Completed': 'string.Equals(execution.Status, "Completed"' in execution,
 'Quartz conserva finalización registrada': 'execution.CompletedAtUtc.HasValue' in execution,
 'Quartz permite solo Running o Pending para entrar en espera': 'string.Equals(execution.Status, "Running"' in execution and 'string.Equals(execution.Status, "Pending"' in execution,
 'Quartz falla cerrado ante estado extraño': 'CENIT_CYCLE_EXECUTION_STATE_REQUIRES_RECONCILIATION' in execution,
 'Guarda antes de asignación Awaiting': execution.index('CENIT_CYCLE_EXECUTION_STATE_REQUIRES_RECONCILIATION') < execution.index('execution.Status = "AwaitingOfficialResult"'),
 'Quartz mantiene Completion null solo en tránsito válido': 'execution.CompletedAtUtc = null;' in execution,
 'Quartz no invoca servicios de simulación': 'OptimizeCycleAsync(' not in execution and 'CalculateAsync(' not in execution,
 'Quartz no crea rechazo operator': 'ReturnedByOperator' not in execution and 'ReturnReasonCode' not in execution,
 'Neteo usa saldos simulados': 'LiquiditySourceType = "Simulated"' in netting,
 'Neteo prohíbe sello oficial': 'IncludedInSettlement = false' in netting,
 'Liquidez contiene etiqueta simulada explícita': 'SimulatedInsufficientLiquidity' in liquidity,
 'Liquidez no escribe estado transacción': 'tx.State =' not in liquidity and 'tx.ReturnReasonCode =' not in liquidity,
 'Liquidez no mueve ciclo': 'ToCycleId = null' in liquidity,
 'Liquidez no publica reject externo': 'ReturnedByOperator' not in liquidity and 'AchTransactionStateEvents' not in liquidity,
 'Importación oficial correlaciona a archivo NACHA': 'x.ExportKind == "NACHA"' in response,
 'NACK individual requiere trace': 'CENIT_OPERATOR_REJECTION_TRACE_REQUIRED' in response,
 'NACK individual exige causal': 'CENIT_OPERATOR_REJECTION_TRACE_OR_REASON_REQUIRED' in response,
 'NACK crea estado solo sobre correlación Matched': 'correlation == CenitChamberCorrelationOutcome.Matched' in response and 'parsed.Type == CenitChamberResponseType.OperatorRejected' in response,
 'NACK oficial es origen explícito del evento': 'Source = AchStateEventSourceEnum.Operator' in response,
 'Replay por mismo origen y hash': 'sameSource.All(x => string.Equals(x.ContentSha256, contentHash' in response,
 'Prueba simulación saldo insuficiente': '[InlineData(0, "SimulatedInsufficientLiquidity")]' in operational,
 'Prueba simulación saldo suficiente': '[InlineData(1000, "SimulatedCandidate")]' in operational,
 'Prueba repetición de Quartz': 'I06_LiquiditySimulationAndQuartzRetry_NeverIssueOfficialReturn' in operational and 'Assert.Equal(first.Id, replay.Id)' in operational,
 'Prueba estado final preservado': 'I06_QuartzRetry_PreservesCompletedExecutionAndDoesNotRewriteTransactions' in operational,
 'Prueba estado desconocido falla cerrado': 'I06_QuartzRetry_UnknownExecutionStatusFailsClosedWithoutMutation' in operational,
 'Prueba no hay NACK antes del aviso oficial': 'Assert.Empty(await context.AchTransactionStateEvents.ToListAsync());' in lifecycle and 'I06_ProvisionalLiquidityShortfall_OnlyOfficialTransactionNackCreatesReturn' in lifecycle,
 'Prueba acuse oficial luego del shortfall': 'Assert.Equal(AchStateEventSourceEnum.Operator, official.Source)' in lifecycle,
 'Prueba replay oficial idempotente': 'Assert.True(replay.IsDuplicate)' in lifecycle and 'Assert.Single(await context.AchTransactionStateEvents.ToListAsync())' in lifecycle,
 'Regla de causa sintética interna DXX-LIQ no da rechazo': 'DXX-LIQ' in (s/'AchCauseCodePolicy.cs').read_text(encoding='utf-8-sig'),
}
for name, ok in checks.items(): print(f'{"PASS" if ok else "FAIL"}: {name}')
print(f'I06 STATIC: {sum(checks.values())}/{len(checks)}. No equivale a compilación, xUnit o verificación con CENIT/CUD.')
sys.exit(0 if all(checks.values()) else 1)

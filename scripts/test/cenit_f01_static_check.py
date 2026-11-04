#!/usr/bin/env python3
"""F01 regression guard (source only). Does not replace dotnet test / DB / Quartz."""
from pathlib import Path

root = Path(__file__).resolve().parents[2]
impl = root / 'src/Cfa.ACHInterbank.Persistence/ACH/Services/Implementation'
execution = (impl / 'CenitCycleExecutionService.cs').read_text()
netting = (impl / 'CenitNettingService.cs').read_text()
optimization = (impl / 'LiquidityOptimizationService.cs').read_text()
priority = (impl / 'TransactionPriorityPolicy.cs').read_text()
eligibility = (impl / 'AchPendingCycleEligibilityService.cs').read_text()
scheduler = (root / 'src/Cfa.ACHInterbank.Persistence/ACH/Quartz/Jobs/Implementation/AchCycleSchedulerHandler.cs').read_text()
tests = (root / 'tests/Cfa.ACHInterbank.Tests/CenitOperationalGovernanceTests.cs').read_text()

checks = {
    'Scheduler sigue invocando la ruta CENIT': 'executionService.StartExecutionAsync' in scheduler,
    'Ruta productiva espera respuesta oficial': 'AwaitingOfficialResult' in execution,
    'Ruta productiva NO ejecuta simulador': '_nettingService.CalculateAsync' not in execution and '_liquidityService.OptimizeCycleAsync' not in execution,
    'Ruta F01 NO consume cola antes de F03': 'item.Status = "Consumed"' not in execution,
    'Neteo simulado NO implica liquidación': 'IncludedInSettlement = false' in netting and 'IncludedInSettlement = true' not in netting,
    'Simulador NO modifica estado ni causal': 'tx.State =' not in optimization and 'tx.ReturnReasonCode' not in optimization,
    'Simulador NO reubica transacción/lote': 'tx.AchCycleId =' not in optimization and 'tx.AchBatchId =' not in optimization,
    'Simulador NO produce eventos ni colas': 'AchTransactionStateEvents.Add' not in optimization and 'CenitCycleQueues.Add' not in optimization,
    'Simulador etiqueta todas las decisiones': 'LiquidityModelUsed = "Simulated"' in optimization and 'SimulatedInsufficientLiquidity' in optimization,
    'Simulación no es evidencia operativa de procesamiento': 'item.DecisionReason != "SimulatedNettingOnly_AwaitingOfficialResult"' in eligibility,
    'Prioridades SEC y de devolución explícitas': all(k in priority for k in ['"CCD" => 10', '"PPD" => 9', '"CCD" => 8', '"PPD" => 7', '"CTX" => 6', '=> 5', '=> 4', '=> 3', '=> 2', '=> 1']),
    'Suite C# cubre límites, espera oficial y ACH': all(k in tests for k in ['CenitCycleExecution_QuartzPath_WaitsForOfficialResultWithoutConsumingQueue', 'LiquidityOptimization_Cycle4And5SimulatedShortfall_DoesNotReturnByOperator', 'CenitCycleExecution_RefusesAchColombiaCycle']),
}
for name, ok in checks.items():
    print(f'{"PASS" if ok else "FAIL"}: {name}')
if not all(checks.values()):
    raise SystemExit(1)
print(f'RESULTADO: {len(checks)}/{len(checks)} controles estáticos correctos. PRUEBAS .NET: NO EJECUTADAS.')

#!/usr/bin/env python3
"""I03 static source guards. These are not runtime, xUnit, DB or CUD evidence."""
from pathlib import Path
import sys
root = Path(__file__).resolve().parents[2]
services = root / 'src/Cfa.ACHInterbank.Persistence/ACH/Services/Implementation'
netting = (services/'CenitNettingService.cs').read_text(encoding='utf-8-sig')
execution = (services/'CenitCycleExecutionService.cs').read_text(encoding='utf-8-sig')
liquidity = (services/'LiquidityOptimizationService.cs').read_text(encoding='utf-8-sig')
controller = (root/'src/Cfa.ACHInterbank.Api/Controllers/CenitOperationsController.cs').read_text(encoding='utf-8-sig')
tests = (root/'tests/Cfa.ACHInterbank.Tests/CenitOperationalGovernanceTests.cs').read_text(encoding='utf-8-sig')
front = (root/'web/ach-interbank-ui/src/app/features/cenit/components/cenit-operation-page.component.ts').read_text(encoding='utf-8-sig')
front_tests = (root/'web/ach-interbank-ui/src/app/features/cenit/components/cenit-operation-page.component.spec.ts').read_text(encoding='utf-8-sig')
checks = {
 'Neteo verifica codigo CENIT en camara': 'string.Equals(cycle.ClearingHouse?.Code, "CENIT"' in netting,
 'Guardia CENIT precede retorno idempotente': netting.index('throw new InvalidOperationException("El neteo simulado') < netting.index('var existing = await _context.CenitNettingExecutions'),
 'Neteo siempre marca no liquidado': 'IncludedInSettlement = false' in netting and 'IncludedInSettlement = true' not in netting,
 'DecisionReason indica simulacion': 'DecisionReason = "SimulatedNettingOnly_AwaitingOfficialResult"' in netting,
 'Liquidez simulada separada de externa': 'ExternalLiquidity = null' in netting and 'LiquiditySourceType = "Simulated"' in netting,
 'Resultado de neteo solo guarda proyecciones': '_context.CenitNettingExecutions.Add(netting)' in netting and '_context.AchTransactions.Update' not in netting,
 'Quartz espera resultado externo': 'execution.Status = "AwaitingOfficialResult"' in execution,
 'Quartz no usa simulador': '_nettingService.CalculateAsync' not in execution and '_liquidityService.OptimizeCycleAsync' not in execution,
 'Simulador liquidez no cambia transacciones': 'tx.State =' not in liquidity and 'tx.ReturnReasonCode =' not in liquidity,
 'API identifica calculo interno': controller.count('calculationKind = "InternalSimulation"') == 2,
 'API no afirma settlement oficial': controller.count('representsOfficialSettlement = false') == 2,
 'API no incluye constancia externa': controller.count('officialEvidenceIncluded = false') == 2,
 'API filtra ejecucion mas reciente a CENIT': '.Where(x => x.AchCycle.ClearingHouse.Code == CenitClearingHouseCode)' in controller,
 'API filtra posiciones de otra camara': 'x.CenitNettingExecution.CenitCycleExecution.AchCycle.ClearingHouse.Code == CenitClearingHouseCode' in controller,
 'API documenta ausencia de CUD': 'NO prueba compensación ni liquidación CUD' in controller,
 'UX rotula posicion como simulada': 'Posición neta simulada (no liquidada)' in front,
 'UX advierte liquidacion no oficial': 'no confirma compensación ni liquidación CENIT/CUD' in front,
 'xUnit cubre saldo insuficiente': 'CenitNetting_SimulatedShortfall_NeverWritesOfficialSettlementOrLedger' in tests,
 'xUnit cubre posiciones de saldo suficiente e insuficiente': 'CenitNetting_PositiveAndNegativeInstitutionPositions_RemainSimulationOnly' in tests,
 'xUnit cubre camara ajena e historico': 'CenitNetting_RejectsOtherChamber_EvenWhenHistoricNettingExists' in tests,
 'xUnit cubre contrato API': 'CenitNetPositionsEndpoint_ExplicitlyStatesSimulation_NotOfficialEvidence' in tests,
 'Jasmine comprueba etiqueta UX': 'NettingView_ShouldLabelAmountsAsSimulatedAndNotSettled' in front_tests,
}
for key, value in checks.items():
 print(('PASS' if value else 'FAIL') + ': ' + key)
print(f'I03 SOURCE-CHECK: {sum(checks.values())}/{len(checks)}. NO ejecuta compilacion, tests C# ni CUD.')
sys.exit(0 if all(checks.values()) else 1)

#!/usr/bin/env python3
"""ACH-CR-001 I01: structural/source assertions. Not a C# compiler or a test runner."""
from pathlib import Path
import re
import sys

base = Path(__file__).resolve().parent.parent
paths = {
    'contract': base / 'src/Cfa.ACHInterbank.Application/ACH/Interfaces/IAchPendingCycleEligibilityService.cs',
    'result': base / 'src/Cfa.ACHInterbank.Application/ACH/Models/AchPendingCycleEligibilityResult.cs',
    'service': base / 'src/Cfa.ACHInterbank.Persistence/ACH/Services/Implementation/AchPendingCycleEligibilityService.cs',
    'tests': base / 'tests/Cfa.ACHInterbank.Tests/AchPendingCycleEligibilityServiceTests.cs',
}
checks = []
def check(name, ok):
    checks.append((name, ok))
    print(f'{"PASS" if ok else "FAIL"} {name}')

for name, path in paths.items():
    check(f'{name}: existe', path.is_file())
if any(not p.is_file() for p in paths.values()):
    sys.exit(1)
source = paths['service'].read_text(encoding='utf-8')
contract = paths['contract'].read_text(encoding='utf-8')
result = paths['result'].read_text(encoding='utf-8')
tests = paths['tests'].read_text(encoding='utf-8')
check('contract: método read-only explícito', 'Task<AchPendingCycleEligibilityResult> EvaluateAsync' in contract)
check('domain: sin nuevas dependencias', 'using Cfa.ACHInterbank.Persistence' not in contract + result)
check('persistence: DI convención Scoped', '[Scoped]' in source and 'class AchPendingCycleEligibilityService : IAchPendingCycleEligibilityService' in source)
check('persistence: calendario/política/ventana reutilizados', all(x in source for x in (
    'IOperationalCalendarService', 'IOperationalCycleWindowResolver', 'ICycleTransactionPolicy')))
check('persistence: mismo clearing house', 'cycle.ClearingHouseId == original.ClearingHouseId' in source)
check('persistence: misma fecha', 'cycle.ProcessingDate.Date == original.ProcessingDate.Date' in source)
check('persistence: after cutoff original', 'originalWindow.Status != OperationalCycleWindowStatus.After' in source)
check('persistence: control exportación', all(x in source for x in (
    'AchFileExportTransactions', 'AchFileExports', '"NACHA"', '"PUBLISH"')))
check('persistence: control de efectos', all(x in source for x in (
    'ContrapartidaDispatchItems', 'CenitCycleQueues', 'LiquidityOptimizationDecisions', 'CenitNettingDetails')))
check('persistence: solo lectura', 'AsNoTracking()' in source and not re.search(
    r'\b(SaveChanges|AddAsync|AddRange|ExecuteUpdate|ExecuteDelete|RemoveRange|RemoveAsync|UpdateAsync|BeginTransaction)\b', source))
check('persistence: sin bloqueo nuevo', 'SemaphoreSlim' not in source and 'Monitor.Enter' not in source and 'lock (' not in source)
check('tests: mínimo de 10 escenarios escritos', tests.count('[Fact]') >= 10)
check('tests: ejercicios sin ejecución ficticia', 'Fact' in tests and 'UseInMemoryDatabase' in tests)
# Scrutor: naming convention and registration from Persistence assembly.
di = (base / 'src/Cfa.ACHInterbank.Persistence/DependencyInjectionService.cs').read_text(encoding='utf-8')
scan = (base / 'src/Cfa.ACHInterbank.Application/Configuration/ScrutorDependencyInjectionExtensions.cs').read_text(encoding='utf-8')
check('DI: convención aplicada al ensamblado', 'AddConventionServicesFromAssembly(' in di and 'I{implementationType.Name}' in scan)
# Existing EF sets must match the used names (scope only).
ctx = (base / 'src/Cfa.ACHInterbank.Persistence/DataBase/AchDbContext.cs').read_text(encoding='utf-8')
check('DB: DbSets usados existen', all(x in ctx for x in (
    'AchTransactions', 'AchCycles', 'AchBatches', 'AchFileExportTransactions',
    'AchFileExports', 'ContrapartidaDispatchItems', 'CenitCycleQueues',
    'LiquidityOptimizationDecisions', 'CenitNettingDetails')))
if any(not ok for _, ok in checks):
    sys.exit(1)
print(f'PASS I01 ESTATICO: {len(checks)} controles; compilación, tests .NET y carreras runtime NO EJECUTADOS')

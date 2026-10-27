#!/usr/bin/env python3
"""I06-FIX01: comprobaciones estáticas de tres diagnósticos de compilación reportados.

No usa SDK .NET, ni invoca servicios externos. No sustituye compilación real.
"""
from pathlib import Path
import sys

root = Path(__file__).resolve().parent.parent
checks = []

def check(label, condition):
    checks.append((label, bool(condition)))
    print(('PASS ' if condition else 'FAIL ') + label)

def source(relative):
    return (root / relative).read_text(encoding='utf-8-sig')

contrapartida = source('src/Cfa.ACHInterbank.Persistence/ACH/Services/Implementation/ContrapartidaDispatchJobService.cs')
handler = source('src/Cfa.ACHInterbank.Persistence/ACH/Quartz/Jobs/Implementation/AchPendingCycleReassignmentHandler.cs')
admission = source('src/Cfa.ACHInterbank.Persistence/ACH/Services/Implementation/IncomingNachaAdmissionValidator.cs')
queue = source('src/Cfa.ACHInterbank.Domain/Models/ACH/IncomingNachaDispatchModels.cs')
window = source('src/Cfa.ACHInterbank.Application/ACH/Models/OperationalCycleWindowModels.cs')
correlation = source('src/Cfa.ACHInterbank.Persistence/ACH/Services/Implementation/ExternalFileNames/AchColInboundHeaderCorrelation.cs')
memory = source('docs/ach-cycle-reassignment/MEMORY_STATE.md')

check('CS1929: el campo de cola es int? real', 'public int? AchTransactionId { get; set; }' in queue)
check('CS1929: secuencia de transacciones int[]', 'var transactionIds = dispatchItems.Select(i => i.AchTransactionId).ToArray();' in contrapartida)
check('CS1929: existe guardia nullable', 'q.AchTransactionId.HasValue && transactionIds.Contains(q.AchTransactionId.Value)' in contrapartida)
check('CS1929: no queda Contains nullable original', 'transactionIds.Contains(q.AchTransactionId))' not in contrapartida)
check('CS0103: enum definido en namespace Application models', 'namespace Cfa.ACHInterbank.Application.ACH.Models;' in window and 'enum OperationalCycleWindowStatus' in window)
check('CS0103: handler importa namespace correcto', 'using Cfa.ACHInterbank.Application.ACH.Models;' in handler)
check('CS0103: handler sigue usando After', '.Status == OperationalCycleWindowStatus.After' in handler)
check('CS0165: correlación declara nullable previamente', 'string? correlationError = null;' in admission)
check('CS0165: usa variable ya inicializada', 'out _, out correlationError))' in admission)
check('CS0165: no queda out var local del condicional', 'out var correlationError' not in admission)
check('CS0165: contrato out string nullable', 'out string? errorCode)' in correlation)
check('CS0165: mensaje conserva fallback', 'correlationError ?? "ACHCOL_INBOUND_NAME_INVALID"' in admission)
check('Estado canónico sigue I06 y marca parche', 'INCREMENT_CURRENT: I06' in memory and 'PATCH_CURRENT: I06-FIX01' in memory)
check('Quartz sigue deshabilitado / runtime no probado', 'PRODUCTION_STATE: NOT_READY' in memory and 'RUNTIME_VALIDATION: NOT_RUN_ENVIRONMENT' in memory)

n = sum(x for _, x in checks)
print(f'I06-FIX01 ESTATICO: {n}/{len(checks)}. Build y tests .NET NO EJECUTADOS.')
sys.exit(0 if n == len(checks) else 1)

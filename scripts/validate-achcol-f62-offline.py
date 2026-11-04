#!/usr/bin/env python3
"""F6.2 static audit only; does NOT compile/run .NET or certify CFA/DB."""
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
code = (ROOT / 'src/Cfa.ACHInterbank.Persistence/ACH/Services/Implementation/IncomingNachaCommandCenterQueryService.cs').read_text(encoding='utf-8')
dto = (ROOT / 'src/Cfa.ACHInterbank.Application/ACH/Models/IncomingNachaCommandCenterDtos.cs').read_text(encoding='utf-8')
tests = (ROOT / 'tests/Cfa.ACHInterbank.Tests/IncomingNachaCommandCenterServiceTests.cs').read_text(encoding='utf-8')
openapi = (ROOT / 'tests/Cfa.ACHInterbank.Tests/OpenApiDocumentGenerationTests.cs').read_text(encoding='utf-8')
docs = ROOT / 'docs/ACHCOL_CFA_FILE_BASED_2026-10-24'
runbook = (docs / 'F6_2_OPERATIONS_RUNBOOK.md').read_text(encoding='utf-8')
memory = (docs / 'MEMORY_STATE.md').read_text(encoding='utf-8')
checks = {
    'DTO aditivo compatible con el constructor historico': 'public IncomingNachaAchColOperationalKpiDto AchColFileBased { get; init; }' in dto,
    'Reutiliza queueSnapshot (sin consulta nueva)': 'var achColItems = queueSnapshot' in code and '.Select(x => new { x.SourceKind,' in code,
    'Aisla el origen ACHCOL': 'x.SourceKind == IncomingNachaDispatchSourceKinds.AchColFileBased' in code,
    'Cuenta pendiente preflight y mapping': 'IncomingNachaFileBasedPreparationStage.InitiallyBlockedCode' in code and 'IncomingNachaFileBasedPreparationStage.MappingPendingCode' in code,
    'Cuenta rechazados': 'IncomingNachaFileBasedPreparationStage.RejectedCode' in code,
    'Detecta estados e intentos no autorizados': 'x.QueueStatus != IncomingNachaDispatchQueueStatus.Blocked' in code and 'x.AttemptCount > 0' in code,
    'Tests C# escritos (no ejecutados)': 'ShouldDistinguishBlockedAchColReasons' in tests and 'ShouldDetectUnsafeAchColQueueWithoutExposingDetails' in tests,
    'Contrato OpenAPI esperado actualizado': '"timeline", "achColFileBased"' in openapi,
    'Runbook SQL read-only y rollback por flag': 'SELECT "QueueStatus"' in runbook and 'SELECT [QueueStatus]' in runbook and 'AllowCycleIndependentInboundDispatch=false' in runbook,
    'Identidad del programa y Live no autorizado': 'program_id: ACHCOL-NACHA-CFA-EXTCYCLE-001' in memory and 'external_dispatch_allowed_without_authorization: false' in memory,
}
for key, ok in checks.items():
    print(('PASS' if ok else 'FAIL') + ' | ' + key)
if not all(checks.values()):
    raise SystemExit(1)
print(f'F6.2 STATIC AUDIT: {len(checks)}/{len(checks)} PASS; no C# build/xUnit/DB/CFA executed.')

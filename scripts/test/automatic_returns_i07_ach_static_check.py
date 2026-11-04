#!/usr/bin/env python3
"""I07-ACH: controles de fuente, NO reemplazan dotnet test ni homologación bancaria."""
from pathlib import Path
import re

ROOT = Path(__file__).resolve().parents[2]
SERVICE = ROOT / 'src/Cfa.ACHInterbank.Persistence/ACH/Services/Implementation/AchColombiaReturnReviewService.cs'
TESTS = ROOT / 'tests/Cfa.ACHInterbank.Tests/AchColombiaReturnRecoveryI07Tests.cs'
LEGACY_TEST = ROOT / 'tests/Cfa.ACHInterbank.Tests/AchColombiaAutomaticReturnI06Tests.cs'
GATE = ROOT / 'src/Cfa.ACHInterbank.Api/appsettings.json'
RUNNER = ROOT / 'src/Cfa.ACHInterbank.Persistence/ACH/Services/Implementation/AchColombiaAutomaticReturnFileRunner.cs'
CODE = SERVICE.read_text()
TEST = TESTS.read_text()
LEGACY = LEGACY_TEST.read_text()
SETTINGS = GATE.read_text()
RUN = RUNNER.read_text()
CHECKS = [
    ('consulta de evento original', 'context.AchTransactionStateEvents.AsNoTracking()' in CODE),
    ('clave de idempotencia original', 'outbound-return-v1:' in CODE),
    ('cámara restringida', 'e.ClearingHouseId == candidate.OriginalTransaction.AchCycle.ClearingHouseId' in CODE),
    ('estado fuente sin transporte', 'e.ToState == AchTransferStateEnum.ReturnedByEpr' in CODE),
    ('referencia de transacción', 'originalTransactionId' in CODE),
    ('archivo original', 'root.GetProperty("fileName")' in CODE),
    ('ciclo original', 'root.GetProperty("returnCycleId")' in CODE),
    ('causal original', 'root.GetProperty("returnReasonCode")' in CODE),
    ('registro original', 'root.GetProperty("recordCount")' in CODE),
    ('cantidad devoluciones', 'root.GetProperty("returnCount")' in CODE),
    ('huella auditada', 'root.GetProperty("contentSha256")' in CODE),
    ('huella calculada en los bytes, no metadato del builder', 'SHA256.HashData(artifact.Content)' in CODE),
    ('comparación de tiempo constante', 'CryptographicOperations.FixedTimeEquals' in CODE),
    ('evidencia faltante bloqueada', 'if (string.IsNullOrWhiteSpace(payloadJson)) return false;' in CODE),
    ('error de integridad no descarga', 'No se entregó ninguna descarga; requiere revisión operativa.' in CODE),
    ('estado explícito sin envío', 'ARCHIVO_GENERADO_SIN_ENVIO' in CODE),
    ('flag productivo inicialmente falso', re.search(r'"EnableAutomaticReturnFileGeneration"\s*:\s*false', SETTINGS) is not None),
    ('sin transporte en ejecutor automático', 'transportAdapter' not in RUN and 'DispatchAsync(' not in RUN),
    ('test alteración contenido', 'contenido-distinto' in TEST),
    ('test ausencia auditoría', 'sin-auditoria' in TEST),
    ('test metadatos', 'camara-distinta' in TEST and 'archivo-distinto' in TEST),
    ('test aislamiento CENIT', 'I07_RecoveryNeverExposesCenitReturnThroughAchEndpoint' in TEST),
    ('test positivo huella', 'I07_RecoveryVerifiesShaAgainstBytesNotArtifactClaimedHash' in TEST),
    ('fixture I06 ajustada a auditoría original', 'SeedGeneratedRecovery(db);' in LEGACY and 'AchTransactionStateEvents.Add' in LEGACY),
]
for title, ok in CHECKS:
    print(('PASS_STATIC' if ok else 'FAIL_STATIC') + ' | ' + title)
print(f'RESULTADO: {sum(bool(ok) for _,ok in CHECKS)}/{len(CHECKS)}')
raise SystemExit(0 if all(ok for _,ok in CHECKS) else 1)

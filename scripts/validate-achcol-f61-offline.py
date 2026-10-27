#!/usr/bin/env python3
"""F6.1 offline source audit. Does not compile C#, execute xUnit or contact CFA/DB."""
from __future__ import annotations

import json
import sys
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
SRC = ROOT / 'src'
TST = ROOT / 'tests/Cfa.ACHInterbank.Tests'
IMPL = SRC / 'Cfa.ACHInterbank.Persistence/ACH/Services/Implementation'
DOCS = ROOT / 'docs/ACHCOL_CFA_FILE_BASED_2026-10-24'
PG = SRC / 'Cfa.ACHInterbank.Persistence/DataBase/Migrations/Postgres'
SQL = SRC / 'Cfa.ACHInterbank.Persistence.Migrations.SqlServer/DataBase/Migrations/SqlServer'
checks: list[tuple[str, bool]] = []


def read(path: Path) -> str:
    if not path.is_file():
        raise FileNotFoundError(path.relative_to(ROOT))
    return path.read_text(encoding='utf-8-sig')


def check(name: str, condition: bool) -> None:
    checks.append((name, condition))
    print(f"{'PASS' if condition else 'FAIL'} | {name}")


sdk = json.loads(read(ROOT / 'global.json'))['sdk']['version']
check('SDK pin documentado', sdk == '10.0.402')
parser = read(IMPL / 'ExternalFileNames/AchColInboundFileNameParser.cs')
check('parser separado acepta nombres OUT/env y ciclo positivo',
      all(token in parser for token in ('CanonicalPlainFileName', 'ExternalCycleNumber', 'FileSequence', 'DigitalEnvelope', 'PlainNacha')))
planner = read(IMPL / 'IncomingNachaDispatchPlanner.cs')
check('planner ACHCOL usa EntryDetail y guarda cola Blocked',
      all(token in planner for token in ('PlanFileBasedAsync', 'EntryDetailId = candidate.Entry.EntryDetailID',
                                       'QueueStatus = IncomingNachaDispatchQueueStatus.Blocked', 'AchCycleId = null')))
config = read(SRC / 'Cfa.ACHInterbank.Persistence/Configuration/IncomingNachaDispatchQueueConfiguration.cs')
check('idempotencia UNIQUE y restriccion por origen',
      '.HasIndex(x => x.IdempotencyDispatchKey).IsUnique()' in config
      and 'CK_IncomingNachaDispatchQueue_SourceShape' in config)
check('migraciones paralelas F3.1/F3.2/F3.3', all((folder / name).exists() for folder, name in (
    (PG, '20261024213000_AchColFileBasedDispatchQueueF31.cs'),
    (SQL, '20261024224500_AchColFileBasedDispatchQueueF32.cs'),
    (PG, '20261024235500_AchColFileBasedQueuePlannerF33.cs'),
    (SQL, '20261024235500_AchColFileBasedQueuePlannerF33.cs'))))
pg_f33 = read(PG / '20261024235500_AchColFileBasedQueuePlannerF33.cs')
sql_f33 = read(SQL / '20261024235500_AchColFileBasedQueuePlannerF33.cs')
check('F3.3 preserva la columna de enlace opcional y downgrade protegido',
      all('IncomingNachaTransactionLinkId' in s and 'Down(' in s and 'SourceShape' in s
          for s in (pg_f33, sql_f33))
      and 'RAISE EXCEPTION' in pg_f33 and 'THROW 51033' in sql_f33)
preflight = read(IMPL / 'IncomingNachaFileBasedPreparationStage.cs')
check('F4.2 preflight no libera para envio',
      'queue.QueueStatus = IncomingNachaDispatchQueueStatus.Blocked;' in preflight
      and 'queue.NextAttemptAtUtc = null;' in preflight
      and 'MappingPendingCode' in preflight)
mapper = read(IMPL / 'ProcTransaccionesRequestMapper.cs')
check('F5.2/5.3 DryRun no entrega contrato transmisible',
      'PreviewAchColFileBasedAsync' in mapper
      and 'AchColProcTransaccionesDryRunContractCheck.Inspect(preview)' in mapper)
contract = read(IMPL / 'AchColProcTransaccionesDryRunContractCheck.cs')
check('F6.1: DryRun XML rechaza texto y atributos no autorizados',
      'ContainsUnexpectedContent(body)' in contract
      and 'ContainsUnexpectedContent(operation)' in contract
      and 'body.HasAttributes' in contract
      and 'envelope.Attributes().Any(x => !x.IsNamespaceDeclaration)' in contract)
model = read(SRC / 'Cfa.ACHInterbank.Application/ACH/Models/AchColProcTransaccionesDryRunPreview.cs')
check('CanTransmit sigue sellado a false', 'public bool CanTransmit => false;' in model)
tests = [
    'AchColInboundFileNameParserTests.cs', 'AchColInboundHeaderCorrelationTests.cs',
    'IncomingNachaAdmissionAchColFileBasedTests.cs', 'AchColInboundIngestionContextTests.cs',
    'IncomingNachaDispatchFileBasedSchemaF31Tests.cs', 'IncomingNachaDispatchFileBasedSqlServerF32Tests.cs',
    'IncomingNachaFileBasedDispatchPlannerF33Tests.cs', 'IncomingNachaFileBasedDispatchSchemaF33Tests.cs',
    'IncomingNachaFileBasedDispatchEligibilityF41Tests.cs', 'IncomingNachaFileBasedOrchestrationF42Tests.cs',
    'AchColProcTransaccionesFieldSourcesF51Tests.cs', 'AchColProcTransaccionesDryRunF52Tests.cs',
    'AchColProcTransaccionesContractReadinessF53Tests.cs',
]
check('tests de regresion F1–F5 presentes (no ejecutados)', all((TST / name).is_file() for name in tests))
check('documentacion incremental existe', all((DOCS / name).is_file() for name in
      ('MEMORY_STATE.md', 'PLAN.md', 'F5_3_IMPLEMENTATION.md')))
failed = [name for name, okay in checks if not okay]
print(f'F6.1 STATIC AUDIT: {len(checks)-len(failed)}/{len(checks)} PASS; no C# build/xUnit/DB/CFA executed.')
if failed:
    print('FAILED:', ', '.join(failed), file=sys.stderr)
    sys.exit(1)

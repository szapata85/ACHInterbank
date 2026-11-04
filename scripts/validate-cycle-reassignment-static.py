#!/usr/bin/env python3
"""ACH-CR-001 static structure validator. No .NET, DB, services or network use."""
from __future__ import annotations
import argparse
import re
import sys
from pathlib import Path
from zipfile import ZipFile

ROOT = 'ACHInterbank_ajustado/'
DOCS = 'docs/ach-cycle-reassignment/'
NEEDED = (
    'AGENTS.md', 'MEMORY.md', 'ACHInterbank.sln',
    'src/Cfa.ACHInterbank.Domain/Models/ACH/AchTransaction.cs',
    'src/Cfa.ACHInterbank.Domain/Models/ACH/AchCycle.cs',
    'src/Cfa.ACHInterbank.Domain/Models/ACH/AchBatch.cs',
    'src/Cfa.ACHInterbank.Domain/Models/ACH/AchTransactionStateEvent.cs',
    'src/Cfa.ACHInterbank.Domain/Models/ACH/AchFileExportTransaction.cs',
    'src/Cfa.ACHInterbank.Persistence/ACH/Services/Implementation/BatchResolver.cs',
    'src/Cfa.ACHInterbank.Persistence/ACH/Services/Implementation/RoutingStrategyService.cs',
    'src/Cfa.ACHInterbank.Persistence/ACH/Services/Implementation/NachaDataLoader.cs',
    'src/Cfa.ACHInterbank.Persistence/ACH/Services/Implementation/LiquidityOptimizationService.cs',
    'src/Cfa.ACHInterbank.Persistence/ACH/Quartz/Jobs/Implementation/AchCycleSchedulerHandler.cs',
    'src/Cfa.ACHInterbank.Persistence/ACH/Quartz/Jobs/Implementation/AchContrapartidasByCycleHandler.cs',
    'docs/normativa/md/ACH-Colombia-V35.md',
    'docs/normativa/md/CENIT-DSP-152-Anexo-2.md',
    'scripts/validate-cycle-reassignment-static.py',
)
DOC_FILES = (
    '00_START_HERE.md', '01_ARQUITECTURA.md', '02_PLAN_INCREMENTAL.md',
    '03_REGLAS_Y_ESCENARIOS.md', '04_VALIDACION_ESTATICA.md',
    '05_GUIA_DE_CHAT.md', '06_TRAZABILIDAD.md', 'MEMORY_STATE.md', 'CHANGELOG.md',
)

def validate(read, names):
    errors=[]
    for rel in (*NEEDED, *(DOCS+name for name in DOC_FILES)):
        if rel not in names:
            errors.append('Falta: '+rel)
    if errors:
        return errors
    state=read(DOCS+'MEMORY_STATE.md').decode('utf-8-sig')
    for token in ('INITIATIVE_ID: ACH-CR-001', 'CURRENT_ZIP:', 'INCREMENT_CURRENT:',
                  'NEXT_INCREMENT:', 'IMPLEMENTATION_STATE:', 'RUNTIME_VALIDATION:',
                  'RISK-CR-001', 'Siguiente acción exacta'):
        if token not in state:
            errors.append('MEMORY_STATE requiere '+token)
    if not re.search(r'INCREMENT_CURRENT:\s+I\d\d[a-z]?',state):
        errors.append('INCREMENT_CURRENT inválido')
    if not re.search(r'NEXT_INCREMENT:\s+(?:I\d\d[a-z]?|NONE)',state):
        errors.append('NEXT_INCREMENT inválido')
    arch=read(DOCS+'01_ARQUITECTURA.md').decode('utf-8-sig')
    for token in ('Clean Architecture', 'AchTransactionStateEvent', 'CENIT', 'ACH Colombia', 'idempotencia'):
        if token.lower() not in arch.lower():errors.append('ARQUITECTURA sin '+token)
    plan=read(DOCS+'02_PLAN_INCREMENTAL.md').decode('utf-8-sig')
    for inc in ('I00','I01','I02','I03','I04','I05','I06'):
        if inc not in plan:errors.append('PLAN sin '+inc)
    return errors

def main():
    p=argparse.ArgumentParser(description=__doc__)
    s=p.add_mutually_exclusive_group(required=True)
    s.add_argument('--directory',type=Path,help='Ruta a raíz del repositorio (AGENTS.md)')
    s.add_argument('--zip',type=Path,help='ZIP completo con carpeta ACHInterbank_ajustado')
    a=p.parse_args()
    if a.zip:
        with ZipFile(a.zip) as z:
            stored=z.namelist()
            duplicates=len(stored)-len(set(stored))
            bad_paths=[n for n in stored if n.startswith('/') or '..' in Path(n).parts]
            if duplicates or bad_paths:
                print('FAIL: rutas duplicadas o no seguras',duplicates,len(bad_paths));return 1
            names={n[len(ROOT):] for n in stored if n.startswith(ROOT)}
            errors=validate(lambda r:z.read(ROOT+r),names)
            # CRC of all archive entries; no execution/unpacking of source files.
            bad=z.testzip()
            if bad:errors.append('CRC inválido: '+bad)
    else:
        project=a.directory
        names={p.relative_to(project).as_posix() for p in project.rglob('*') if p.is_file()}
        def read(rel):return (project/rel).read_bytes()
        errors=validate(read,names)
    for e in errors:print('FAIL:',e)
    if errors:return 1
    print('PASS ACH-CR-001: estructura, documentación, memoria y ZIP (si aplica) verificadas; sin ejecución .NET/runtime.')
    return 0

if __name__=='__main__':sys.exit(main())

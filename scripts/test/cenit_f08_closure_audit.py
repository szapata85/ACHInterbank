#!/usr/bin/env python3
"""F08 documentary/integrity gate (static only; does NOT execute .NET tests)."""
import argparse
import hashlib
import json
import re
import sys
import zipfile
from pathlib import Path

p=argparse.ArgumentParser()
p.add_argument('--root', default=str(Path(__file__).resolve().parents[2]))
p.add_argument('--baseline-zip', default=None)
a=p.parse_args()
r=Path(a.root).resolve()
d=r/'docs/correcciones-cenit'
i=r/'docs/auditoria-estatica-cenit'
tests=[]
def test(name,passed):
    tests.append((name,bool(passed)))
    print(('PASS' if passed else 'FAIL')+' F08 '+name)

hc={f'HC-CENIT-{n:03}' for n in range(1,14)}
pt={f'PT-{n:02}' for n in range(1,23)}
audit=(i/'HALLAZGOS_CONSOLIDADOS_CENIT.md').read_text()
ptdoc=(i/'MATRIZ_PRUEBAS_POSTERIORES_CENIT.md').read_text()
mat=(d/'F08_MATRIZ_CIERRE_HALLAZGOS.md').read_text()
plan=(d/'F08_PLAN_PRUEBAS_Y_APROBACIONES.md').read_text()
verdict=(d/'F08_DICTAMEN_FINAL_CONDICIONADO.md').read_text()
check=(d/'F08_CHECKLIST_GO_NO_GO.md').read_text()

test('original I10 HC set is exactly 001-013', set(re.findall(r'HC-CENIT-\d{3}',audit))==hc)
test('F08 closing matrix HC set exactly 001-013',set(re.findall(r'HC-CENIT-\d{3}',mat))==hc)
test('F08 closing matrix contains 13 open rows',sum(bool(re.search(r'^\| HC-CENIT-\d{3} \|.*\*\*ABIERTO',s)) for s in mat.splitlines())==13)
test('original I10 posterior PT set exactly 01-22',set(re.findall(r'PT-\d{2}',ptdoc))==pt)
test('F08 plan PT set exactly 01-22',set(re.findall(r'PT-\d{2}',plan))==pt)
test('F08 plan has 22 explicitly pending rows',sum(bool(re.search(r'^\| PT-\d{2} \|.*\*\*PENDIENTE — NO EJECUTADA',s)) for s in plan.splitlines())==22)
test('F08 verdict explicitly denies homologation', 'NO SE ACREDITA CUMPLIMIENTO INTEGRAL' in verdict and 'NO SE ACREDITA' in verdict and '0 hallazgos cerrados' in verdict)
test('F08 official accounting excluded', 'CUD' in verdict and 'sin asientos CUD' in verdict)
test('F08 checklist blocks four technical gates',all(f'| G{n} |' in check and 'BLOQUEA' in check for n in range(4)))
version=json.loads((r/'global.json').read_text())['sdk']['version']
test('SDK requirement transcribed from actual global.json', version=='10.0.403' and version in plan and version in verdict)
test('historical 7 phase memories preserved',all((d/f'F{n:02}_MEMORY_STATE.md').is_file() for n in range(1,8)))
test('historical seven phase reports preserved',all((d/f'F{n:02}_INFORME_{"REGRESION" if n==7 else "CORRECCIONES"}.md').is_file() for n in range(1,8)))
test('historical original audit closing criteria preserved',(i/'DICTAMEN_ESTATICO_FINAL_CENIT.md').is_file() and (i/'MATRIZ_PRUEBAS_POSTERIORES_CENIT.md').is_file())
test('F07 suite remains available and not silently replaced', (r/'scripts/test/run_cenit_f07_dotnet_regression.sh').is_file() and (r/'tests/Cfa.ACHInterbank.Tests/CenitF07CrossPhaseRegressionTests.cs').is_file())

if a.baseline_zip:
    # Compare all non-documentary files: no production, test, migration, WSDL
    # or other historical baseline modifications are allowed in F08.
    archive=Path(a.baseline_zip)
    with zipfile.ZipFile(archive) as z:
        names=z.namelist()
        doc_prefix='ACHInterbank_ajustado/docs/correcciones-cenit/'
        unchanged=True
        changed=[]
        for name in names:
            local=r.parent/name
            if not local.is_file():
                unchanged=False; changed.append(name+':missing');continue
            if hashlib.sha256(z.read(name)).digest()!=hashlib.sha256(local.read_bytes()).digest():
                changed.append(name)
                if not name.startswith(doc_prefix):unchanged=False
        test('all F07 product/test/audit files identical bytes in F08',unchanged)
        test('all F07 entries exist after F08', len(changed)<len(names) and all((r.parent/n).is_file() for n in names))
        test('F08 changes confined to correction documentation',all(n.startswith(doc_prefix) for n in changed))

for n, ok in tests:
    if not ok:print('FAILED:',n,file=sys.stderr)
print(f'F08 RESULT: {sum(ok for _,ok in tests)}/{len(tests)} documentary/static checks pass; runtime tests NOT EXECUTED; 0 HC closed.')
sys.exit(0 if all(ok for _,ok in tests) else 1)

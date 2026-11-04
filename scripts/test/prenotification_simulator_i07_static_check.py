#!/usr/bin/env python3
"""I07: controles estáticos reproducibles, NO sustituyen build .NET/Angular/E2E."""
from pathlib import Path
import json
import re
import shutil
import subprocess
from html.parser import HTMLParser

root = Path(__file__).resolve().parents[2]
def source(path): return (root / path).read_text(encoding='utf-8-sig')
service = source('src/Cfa.ACHInterbank.Persistence/ACH/Services/Implementation/NachaInboundSimulationService.cs')
tacit = source('src/Cfa.ACHInterbank.Persistence/ACH/Services/Implementation/NachaInboundSimulationService.TacitAcceptance.cs')
component = source('web/ach-interbank-ui/src/app/features/uat/components/nacha-inbound-simulator/nacha-inbound-simulator.component.ts')
template = source('web/ach-interbank-ui/src/app/features/uat/components/nacha-inbound-simulator/nacha-inbound-simulator.component.html')
spec = source('web/ach-interbank-ui/src/app/features/uat/components/nacha-inbound-simulator/nacha-inbound-simulator.component.spec.ts')
cstests = source('tests/Cfa.ACHInterbank.Tests/NachaInboundSimulatorTests.I07.cs')
checks = {
    'Return-cycle exception narrowly scoped': 'scenarioType == NachaInboundSimulationType.IncomingPrenotificationReturn' in service,
    'Return-cycle active config still enforced': all(x in service for x in ['x.ClearingHouseCycleConfig.IsActive','x.ClearingHouse.IsActive','x.ClearingHouseCycleConfigId.HasValue']),
    'Cancelled cycle excluded for prenotification return': 'x.OperationalStatus != AchCycleOperationalStatus.Cancelled' in service,
    'Legacy scenarios retain transaction requirement': ': x.Transactions.Any(t =>' in service,
    'Return-cycle reports zero existing return transactions': 'TransactionCount = request.ScenarioType == NachaInboundSimulationType.IncomingPrenotificationReturn' in service,
    'Generator preview still validates return after original': 'RESPONSE_CYCLE_BEFORE_ORIGINAL' in source('src/Cfa.ACHInterbank.Persistence/ACH/Services/Implementation/NachaInboundSimulationService.Prenotifications.cs'),
    'C# tests both clearing houses': all(x in cstests for x in ['InlineData("ACHCOL", "ACH-CYCLE")', 'InlineData("CENIT", "CENIT-CYCLE")']),
    'C# test cancelled return cycle': 'I07_PrenotificationReturnCycle_DoesNotExposeCancelledCycle' in cstests,
    'SPA query version increments': 'const version = ++this.pnQueryVersion' in component,
    'SPA stale results rejected': 'version !== this.pnQueryVersion' in component,
    'SPA stale catalog responses rejected': 'version !== this.pnOptionsVersion' in component,
    'SPA close invalidates outstanding queries': 'this.pnQueryVersion++;' in component.split('closePrenotifications(): void')[1].split('changePnHouse(')[0],
    'SPA selected disabled during load': '[disabled]="pnBusy || pnLoading"' in template,
    'SPA stale search ignored': 'search !== this.pnSearch.trim()' in component,
    'SPA clear candidate results during refresh': 'this.pnCandidates = [];\n    this.pnTotal = 0;\n    const house' in component,
    'SPA tests stale camera selection': 'I07: la respuesta de una consulta antigua' in spec,
    'SPA tests closed dialog response': 'I07: ignora resultados tardíos' in spec,
    'SPA tests stale catalog': 'I07: no utiliza catálogos de otra prenotificación' in spec,
    'Tacit preview guard equals apply environment constraints': all(x in tacit for x in ['&& !_options.AllowExternalTransmission && !_options.AllowAutoImport', '&& _stateTransitionService is not null, items, problems']),
    'Tacit apply ignores virtual time': 'var now = DateTimeOffset.UtcNow;' in tacit,
    'No fictitious confirmation in SPA': 'no genera un archivo de aprobación' in template,
    'Authenticated blob download preserved': 'downloadFile(id: number)' in source('web/ach-interbank-ui/src/app/features/uat/services/nacha-inbound-simulator.service.ts'),
}
class Balanced(HTMLParser):
    void={'input','br','hr','img','link','meta','source','area'}
    def __init__(self): super().__init__(); self.stack=[]; self.errors=[]
    def handle_starttag(self, tag, attrs):
        if tag not in self.void: self.stack.append(tag)
    def handle_startendtag(self, tag, attrs): pass
    def handle_endtag(self, tag):
        if not self.stack or self.stack[-1] != tag: self.errors.append(tag)
        else: self.stack.pop()
b = Balanced(); b.feed(template)
checks['Angular HTML balanced'] = not b.errors and not b.stack
node = shutil.which('node')
if node:
    ts_script="""const ts=require('/opt/nvm/versions/node/v22.16.0/lib/node_modules/typescript');const fs=require('fs');let n=0;for(let f of process.argv.slice(1)){const s=ts.createSourceFile(f,fs.readFileSync(f,'utf8'),ts.ScriptTarget.Latest,true);n+=s.parseDiagnostics.length;}process.exit(n?1:0);"""
    paths = [str(root / p) for p in ['web/ach-interbank-ui/src/app/features/uat/components/nacha-inbound-simulator/nacha-inbound-simulator.component.ts','web/ach-interbank-ui/src/app/features/uat/components/nacha-inbound-simulator/nacha-inbound-simulator.component.spec.ts']]
    checks['Global TypeScript AST parser: source/spec'] = subprocess.run([node,'-e',ts_script,*paths], capture_output=True).returncode == 0
else: checks['Global TypeScript AST parser: source/spec'] = False
for title, passed in checks.items(): print(('PASS_STATIC' if passed else 'FAIL_STATIC'), title)
result = {'classification':'STATIC_ONLY_NOT_DOTNET_BUILD_ANGULAR_BUILD_OR_E2E','checks_total':len(checks),'passed':sum(checks.values()),'failed':[name for name, ok in checks.items() if not ok]}
print('SUMMARY_JSON '+json.dumps(result,ensure_ascii=False))
raise SystemExit(bool(result['failed']))

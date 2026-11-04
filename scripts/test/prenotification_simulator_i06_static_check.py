from pathlib import Path
from html.parser import HTMLParser
import re, subprocess, json
root=Path(__file__).resolve().parents[2]
base=root/'web/ach-interbank-ui/src/app/features/uat'
component=(base/'components/nacha-inbound-simulator/nacha-inbound-simulator.component.ts').read_text()
template=(base/'components/nacha-inbound-simulator/nacha-inbound-simulator.component.html').read_text()
scss=(base/'components/nacha-inbound-simulator/nacha-inbound-simulator.component.scss').read_text()
service=(base/'services/nacha-inbound-simulator.service.ts').read_text()
tests=(base/'components/nacha-inbound-simulator/nacha-inbound-simulator.component.spec.ts').read_text()
class Parser(HTMLParser):
    def __init__(self):
        super().__init__(); self.stack=[]; self.problems=[]
    def handle_starttag(self,tag,attrs):
        if tag not in ('input','br','hr','img','meta','link','source','area'):
            self.stack.append(tag)
    def handle_endtag(self,tag):
        if not self.stack or self.stack[-1]!=tag:
            self.problems.append((tag,self.stack[-3:]));return
        self.stack.pop()
    def handle_startendtag(self,tag,attrs): pass
p=Parser();p.feed(template)
checks={
    'HTML balanced tags':len(p.problems)==0 and len(p.stack)==0,
    'No alternate SPA / routes': 'router.navigate' not in component.split('openPrenotifications():')[1].split('loadFinancialInstitutions():')[0],
    'Existing simulation form preserved':'<form *ngIf="!prenotificationFlow" [formGroup]="form"' in template,
    'Three-step workflow':all(x in template for x in ('1. Buscar prenotificación enviada','2. Elegir resultado','3. Validar y ejecutar')),
    'Single prenotification payload':'originalPrenotificationIds: [candidate.originalPrenotificationId]' in component,
    'No fictitious NACHA approval':'no genera un archivo de aprobación' in template,
    'Prevent virtual-time apply':'!this.pnVirtualDateTime &&' in component,
    'Prevent apply without backend real deadline':'this.pnTacitPreview.items[0].eligibleAtRealTime' in component,
    'Contract returned and tacit':"export type PrenotificationOutcome = 'Returned' | 'NoReturnByDeadline';" in service,
    'Validates before action':'this.pnOutcome === \'Returned\'\n      ? this.pnReturnPreview?.businessEligible' in component,
    'Confirmation dialogue':'confirmPn(): void' in component and 'OperationalConfirmDialogComponent' in component,
    'No autoimport ':'Sin importación ni transmisión automática' in template,
    'Explicit NachaUpload handoff':'goToUpload()' in template,
    'User choice of official return cause':'code.appliesToPrenotification' in component,
    'Separates original cycle from return cycle':'cycle.cycleId !== candidate.originalCycleId' in component,
    'Server paginated search':'listOriginatedPrenotifications' in component and 'pageSize: this.pnPageSize' in component,
    'Read-only virtual preview':'previewTacitAcceptance(payload, virtual?.toISOString() ?? null)' in component,
    'Send only real clock to apply':'applyDueTacitAcceptance(request)' in component,
    'Authenticated blob download':'this.http.get(this.api.resolveUrl(`${this.basePath}/${id}/file`)' in service,
    'Unsafe window.open removed':'window.open(' not in component,
    'Blob object cleanup':'URL.revokeObjectURL(url)' in component,
    'Accessible selected state':'[attr.aria-pressed]' in template,
    'Empty and error states':all(x in template for x in ('pn-empty','pnError','pnPreviewError')),
    'Responsive mobile layouts':'@media (max-width: 600px)' in scss,
    'No CSS fixed color':'#' not in scss.split('/* I06:')[1],
    'Backend generation route exists': 'prenotifications/generate-return' in (root/'src/Cfa.ACHInterbank.Api/Controllers/NachaInboundSimulatorController.cs').read_text(),
    'Backend tacit routes exist':'prenotifications/tacit-acceptance/apply-due' in (root/'src/Cfa.ACHInterbank.Api/Controllers/NachaInboundSimulatorController.cs').read_text(),
    'New UI unit tests':tests.count("it('I06:")>=5,
    'No extra dependency': '"@angular/material"' in (root/'web/ach-interbank-ui/package.json').read_text(),
    'Existing history retained':'Simulaciones recientes' in template,
    'Legacy option only in history':"if (value === 'IncomingPrenotificationResponse'" in component and "{ value: 'IncomingPrenotificationResponse'" not in component,
}
# syntax-only validation via globally installed TypeScript -- not the Angular compiler
node='''const ts=require(process.env.TYPESCRIPT_MODULE || '/opt/nvm/versions/node/v22.16.0/lib/node_modules/typescript');const fs=require('fs'); let count=0; for(const p of process.argv.slice(1)){let sf=ts.createSourceFile(p,fs.readFileSync(p,'utf8'),ts.ScriptTarget.Latest,true);count+=sf.parseDiagnostics.length;} console.log(count);process.exit(count?1:0);'''
files=[str(base/'services/nacha-inbound-simulator.service.ts'),str(base/'components/nacha-inbound-simulator/nacha-inbound-simulator.component.ts'),str(base/'components/nacha-inbound-simulator/nacha-inbound-simulator.component.spec.ts')]
r=subprocess.run(['node','-e',node,*files],capture_output=True,text=True)
checks['TS syntax parser 3 files']=r.returncode==0
for name,passed in checks.items():print(('PASS_STATIC' if passed else 'FAIL_STATIC')+' '+name)
print('SUMMARY',sum(checks.values()),'/',len(checks))
raise SystemExit(0 if all(checks.values()) else 1)

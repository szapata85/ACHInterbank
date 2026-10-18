#!/usr/bin/env python3
from __future__ import annotations
import hashlib, json, re
from pathlib import Path

ROOT=Path(__file__).resolve().parents[1]
APP=ROOT/'web/ach-interbank-ui/src/app'
DELTA=ROOT/'docs/ai/navigation-menu/phases/NAV4_2I_COPY_DELTA.json'
INVENTORY=ROOT/'docs/ai/navigation-menu/phases/NAV4_2I_CONFIRMATION_COPY_INVENTORY.md'
BASELINE_SHA='6e291cbba272a76baaec6979f3c163b62e51cb8c7e24af41ff8fbbe8544d1f83'
EXPECTED_LINES=[40,45,48,50,177,148,293,35,74,203,205,126,127]
ASSERTIONS=0

def require(cond,msg):
    global ASSERTIONS; ASSERTIONS+=1
    if not cond: raise AssertionError(msg)

def sha(p:Path)->str: return hashlib.sha256(p.read_bytes()).hexdigest()
def txt(data:bytes)->str: return data.decode('utf-8').replace('\r\n','\n').replace('\r','\n')
def tree_hash(tree=None):
    h=hashlib.sha256()
    for p in sorted(x for x in APP.rglob('*') if x.is_file()):
        rel=p.relative_to(APP).as_posix(); data=(tree[rel] if tree is not None else p.read_bytes()); rb=rel.encode()
        h.update(len(rb).to_bytes(4,'big')); h.update(rb); h.update(len(data).to_bytes(8,'big')); h.update(data)
    return h.hexdigest()

def occurrences(tree, pattern):
    out=[]
    rx=re.compile(pattern,re.M|re.S)
    for rel in sorted(tree):
        if rel.endswith(('.ts','.html')) and not rel.endswith('.spec.ts'):
            out.extend((rel,m.group(0)) for m in rx.finditer(txt(tree[rel])))
    return out

# Navigation/security/seed invariants inherited from NAV4.2H.
protected={
 'web/ach-interbank-ui/src/app/core/services/navigation.service.ts':'4df488ac45465a5283a02afe795fc64c4e5995a3415556d324275cc5e1d7ea93',
 'web/ach-interbank-ui/src/app/core/services/menu-route-surface.service.ts':'3a32dc7921e3c805e32fa9921e371109813aae9ba3a3173bda6a5b6b7030df72',
 'web/ach-interbank-ui/src/app/core/services/navigation-naming.service.ts':'de6d4a402690956914a67f26466c7ea6c1cfac16eb717dbeb4d4b6f56e59db19',
 'web/ach-interbank-ui/src/app/core/services/navigation-naming.util.ts':'17e066e755f589ee818b6fa48fbcf3963ba14886a35cdf3187e5381e80368895',
 'src/Cfa.ACHInterbank.Application/Navigation/Queries/GetMenuForCurrentUserHandler.cs':'f111e416c181fc1912c297e37012cf7462f49d0434c1ef6ec9416c89b5ed1a61',
 'src/Cfa.ACHInterbank.Persistence/Navigation/Seeders/NavigationInformationArchitectureSeeder.cs':'28371549f4b9150fdf738bee11454c98117ba420d1abae0e9971e2c49aee9f4c',
 'src/Cfa.ACHInterbank.Persistence/Security/Seeders/FineGrainedGrantBackfillSeeder.cs':'e4f2b80b0b9e9c3aff636fb03e96b819019c07eeabc549bcfe4f789756775437',
 'src/Cfa.ACHInterbank.Persistence/Security/Seeders/LegacyFineGrainedRoleMigrationSeeder.cs':'e2366e75faab4ee31498d0cb84a93899a2a928698b01e4401a38ab1cc9c18105',
 'web/ach-interbank-ui/src/app/core/guards/permission.guard.ts':'7d9541b3899543ad40802b18cb4e734a9c14e59077b732108b31396213afa73a',
 'web/ach-interbank-ui/src/app/core/guards/role.guard.ts':'520fcea2357e5cfe2b19d3ad1d0b284ee84d21149b359025126308210d6de31e',
}
for rel,expected in protected.items(): require(sha(ROOT/rel)==expected,f'protected navigation/security/seed changed: {rel}')

D=json.loads(DELTA.read_text(encoding='utf-8')); changes=D.get('changes',[])
require(D.get('phase')=='NAV4.2I','wrong phase')
require(D.get('baseline')=='NAV4.2H','wrong baseline')
require(D.get('baseline_src_app_sha256')==BASELINE_SHA,'wrong NAV4.2H baseline hash')
require(len(changes)==13,'NAV4.2I delta must contain exactly 13 productive copy changes')
require(len(EXPECTED_LINES)==len(changes),'line map mismatch')
require(len({c['file'] for c in changes})==8,'NAV4.2I delta must cover exactly 8 productive files')
allowed={'file','old','new','action_context','origin'}
for c in changes:
    require(set(c)==allowed,f'unexpected delta fields: {set(c)-allowed}')
    require(c['file'].startswith('web/ach-interbank-ui/src/app/'),'delta outside src/app')
    require(c['file'].endswith(('.html','.ts')) and not c['file'].endswith('.spec.ts'),'non-production delta file')
    require(c['origin'] in {'SPA_CONFIRMATION','SPA_CONFIRMATION_COMPOSED'},'passthrough/dynamic source must not be changed')
    require(c['old']!=c['new'],'delta entry is not a real change')
    require(re.findall(r'\$\{[^}]+\}|\{\{[^}]+\}\}',c['old'])==re.findall(r'\$\{[^}]+\}|\{\{[^}]+\}\}',c['new']),'dynamic interpolation changed')

current={p.relative_to(APP).as_posix():p.read_bytes() for p in APP.rglob('*') if p.is_file()}
restored=dict(current)
for c,n in zip(changes,EXPECTED_LINES):
    rel=c['file'].split('web/ach-interbank-ui/src/app/',1)[1]
    raw=restored[rel].splitlines(keepends=True); require(1<=n<=len(raw),f'line outside file {rel}:{n}')
    rb=raw[n-1]
    if rb.endswith(b'\r\n'): ending,body=b'\r\n',rb[:-2]
    elif rb.endswith((b'\n',b'\r')): ending,body=rb[-1:],rb[:-1]
    else: ending,body=b'',rb
    line=body.decode('utf-8'); new=str(c['new']); old=str(c['old'])
    require(line.count(new)==1,f'documented new copy missing/ambiguous {rel}:{n}: {new!r}')
    baseline=line.replace(new,old,1)
    require(line.replace(new,'<NAV4.2I_COPY>',1)==baseline.replace(old,'<NAV4.2I_COPY>',1),f'non-copy token changed {rel}:{n}')
    raw[n-1]=baseline.encode()+ending; restored[rel]=b''.join(raw)

require(tree_hash(restored)==BASELINE_SHA,'reversing NAV4.2I delta does not reconstruct exact NAV4.2H src/app')

# Buttons/action labels are invariant.
button_patterns=[
 r'<button\b[^>]*>.*?</button>', r'<ui-boton\b[^>]*>',
 r'\b(?:confirmLabel|cancelLabel|confirmText|cancelText|textoConfirmar)\s*:\s*[^,}\n]+',
 r'\b(?:confirmLabel|cancelLabel|confirmText|cancelText)="[^"]*"'
]
for p in button_patterns: require(occurrences(current,p)==occurrences(restored,p),f'button/action label changed: {p}')

# Conditions, result handling, handlers/callbacks and API/service calls are invariant.
logic_patterns=[
 r'\bif\s*\(', r'\bfilter\s*\(', r'\.afterClosed\(\)', r'\.subscribe\s*\(',
 r'\b(?:api|service|this\.api|this\.service|policiesApi)\.[A-Za-z_$][\w$]*\s*\(',
 r'\b(?:confirm|window\.confirm)\s*\(', r'\b(?:confirmarAccion|confirmDeleteHoliday|disableUser|persistAndRefresh|executeNow|generateReturnOfReturnNachaFile)\b'
]
for p in logic_patterns: require(occurrences(current,p)==occurrences(restored,p),f'handler/condition/API invariant changed: {p}')

# Validators and prior validation copy remain byte-identical because exact replay reconstructs H; assert key surfaces directly.
for p in [r'Validators\.[A-Za-z]+',r'hasError\([^\)]*\)',r'<mat-error\b',r'formControlName="[^"]+"']:
    require(occurrences(current,p)==occurrences(restored,p),f'NAV4.2H validation surface changed: {p}')

# Backend/external passthrough and domain codes are invariant.
for p in [r'ProblemDetails',r'ValidationProblemDetails',r'error\.message',r'response\.error',r'\.errors\b',r'\bR\d{2}\b',r'\bD\d{2}\b']:
    require(occurrences(current,p)==occurrences(restored,p),f'backend/external passthrough or code changed: {p}')

# NAV4.2G feedback delta remains exactly present at documented lines.
g=json.loads((ROOT/'docs/ai/navigation-menu/phases/NAV4_2G_COPY_DELTA.json').read_text(encoding='utf-8'))
for c in g['changes']:
    rel=c['file'].split('web/ach-interbank-ui/src/app/',1)[1]
    ls=txt(current[rel]).splitlines(); n=int(c['line'])
    require(str(c['new']) in ls[n-1],f'NAV4.2G feedback regressed: {rel}:{n}')

# Earlier D/E/F deltas remain present when their delta format exposes source lines.
for phase in ['D','E','F']:
    pd=ROOT/f'docs/ai/navigation-menu/phases/NAV4_2{phase}_COPY_DELTA.json'
    if not pd.exists(): continue
    obj=json.loads(pd.read_text(encoding='utf-8'))
    for group in obj.get('changes',[]):
        rel=group.get('path','')
        if rel.startswith('web/ach-interbank-ui/src/app/'): rel=rel.split('web/ach-interbank-ui/src/app/',1)[1]
        for e in group.get('entries',[]):
            n=int(e.get('line',0) or 0)
            if rel in current and n:
                require(str(e['new']).strip() in txt(current[rel]).splitlines()[n-1],f'NAV4.2{phase} copy regressed: {rel}:{n}')

inv=INVENTORY.read_text(encoding='utf-8')
require('**Exact hooks inspected:** **44**' in inv,'inventory exact hook count missing')
require('`DRIFT` **11**' in inv,'inventory drift count missing')
require('**13** cambios de copy' in inv,'inventory change count missing')
for dec in ['DEC-NAV-4.2I-001','DEC-NAV-4.2I-002','DEC-NAV-4.2I-003']: require(dec in inv,f'missing decision {dec}')

print(f'PASS NAV4.2I static confirmation validation: {ASSERTIONS} assertions; 13 copy changes; 44 hooks; baseline NAV4.2H reconstructed exactly.')

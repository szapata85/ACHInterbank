#!/usr/bin/env python3
from __future__ import annotations
import hashlib, json, re
from collections import defaultdict
from pathlib import Path

ROOT=Path(__file__).resolve().parents[1]
APP_ROOT=ROOT/'web/ach-interbank-ui/src/app'
DELTA=ROOT/'docs/ai/navigation-menu/phases/NAV4_2H_COPY_DELTA.json'
INVENTORY=ROOT/'docs/ai/navigation-menu/phases/NAV4_2H_VALIDATION_MESSAGES_INVENTORY.md'
BASELINE_SHA='ba52a7a83c980ba302754ea3b776989cbf075a654b6e451230decd85751d4737'
EXPECTED_LINES=[153, 12, 204, 220, 224, 203, 211, 110, 118, 72, 89, 550, 38, 39, 255, 263, 274, 277, 328, 368, 208, 214, 219, 220, 226, 233, 238, 243, 248, 253, 258, 278, 85, 136, 155, 157, 158, 160, 214, 266, 269, 272, 282, 295, 298, 313, 366, 377, 437, 442, 447, 458, 471, 491, 544, 558, 559, 572, 589, 607, 617, 618, 1353, 1354, 1356, 1357, 1381, 1382, 1383, 1384, 1386, 1388, 1389, 1390, 293, 302, 37, 40, 43, 46, 49, 52, 55, 58, 4, 5, 6, 7, 8, 9, 10, 29]
ASSERTIONS=0

def require(cond,msg):
 global ASSERTIONS; ASSERTIONS+=1
 if not cond: raise AssertionError(msg)

def sha(p): return hashlib.sha256(p.read_bytes()).hexdigest()

def tree_hash(tree=None):
 h=hashlib.sha256()
 for p in sorted(x for x in APP_ROOT.rglob('*') if x.is_file()):
  rel=p.relative_to(APP_ROOT).as_posix(); data=(tree[rel] if tree is not None else p.read_bytes()); rb=rel.encode()
  h.update(len(rb).to_bytes(4,'big')); h.update(rb); h.update(len(data).to_bytes(8,'big')); h.update(data)
 return h.hexdigest()

def txt(data): return data.decode('utf-8').replace('\r\n','\n').replace('\r','\n')

def extracts(tree,patterns):
 out=[]
 for rel in sorted(tree):
  if not rel.endswith(('.ts','.html')) or rel.endswith('.spec.ts'): continue
  s=txt(tree[rel])
  for p in patterns:
   out += [f"{rel}:{m.group(0)}" for m in p.finditer(s)]
 return out

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
require(D.get('phase')=='NAV4.2H','wrong phase'); require(D.get('baseline')=='NAV4.2G','wrong baseline')
require(D.get('baseline_src_app_sha256')==BASELINE_SHA,'wrong NAV4.2G baseline hash')
require(len(changes)==92,'NAV4.2H delta must contain exactly 92 productive copy changes')
require(len(EXPECTED_LINES)==len(changes),'line map mismatch')
require(len({c['file'] for c in changes})==16,'NAV4.2H delta must cover exactly 16 productive files')
allowed={'file','old','new','field_context','validator_error_key','origin'}
for c in changes:
 require(set(c)==allowed,f'unexpected delta fields: {set(c)-allowed}')
 require(c['file'].startswith('web/ach-interbank-ui/src/app/'),'delta outside src/app')
 require(c['file'].endswith(('.html','.ts')) and not c['file'].endswith('.spec.ts'),'non-production delta file')
 require(c['origin'] in {'SPA_VALIDATION','SPA_VALIDATION_COMPOSED'},'passthrough must not be changed')
 require(re.findall(r'\$\{[^}]+\}',c['old'])==re.findall(r'\$\{[^}]+\}',c['new']),'dynamic interpolation changed')

current={p.relative_to(APP_ROOT).as_posix():p.read_bytes() for p in APP_ROOT.rglob('*') if p.is_file()}
restored=dict(current)
# Delta order is stable: sorted file, then source line. Hardcoded line map keeps the JSON contract limited to the six requested fields.
for c,n in zip(changes,EXPECTED_LINES):
 rel=c['file'].split('web/ach-interbank-ui/src/app/',1)[1]
 raw=restored[rel].splitlines(keepends=True); require(1<=n<=len(raw),f'line outside file {rel}:{n}')
 rb=raw[n-1]
 if rb.endswith(b'\r\n'): ending,body=b'\r\n',rb[:-2]
 elif rb.endswith((b'\n',b'\r')): ending,body=rb[-1:],rb[:-1]
 else: ending,body=b'',rb
 line=body.decode('utf-8'); new=str(c['new']); old=str(c['old'])
 require(line.count(new)==1,f'documented new copy missing/ambiguous {rel}:{n}: {new!r}')
 baseline_line=line.replace(new,old,1)
 require(line.replace(new,'<NAV4.2H_COPY>',1)==baseline_line.replace(old,'<NAV4.2H_COPY>',1),f'non-copy token changed {rel}:{n}')
 raw[n-1]=baseline_line.encode()+ending; restored[rel]=b''.join(raw)

require(tree_hash(restored)==BASELINE_SHA,'reversing NAV4.2H delta does not reconstruct exact NAV4.2G src/app')

# Structural invariants: validators, regex/limits, keys, conditions, precedence and bindings.
patterns=[
 re.compile(r'Validators\.(?:required|min|max|minLength|maxLength|pattern|email)(?:\([^\n;]*\))?'),
 re.compile(r'\b(?:setValidators|addValidators|setAsyncValidators|addAsyncValidators|setErrors)\s*\([^\n;]*'),
 re.compile(r'hasError\(\s*[\"\'][^\"\']+[\"\']\s*\)|getError\(\s*[\"\'][^\"\']+[\"\']\s*\)'),
 re.compile(r'(?:min|max|minlength|maxlength|pattern)=\"[^\"]*\"'),
 re.compile(r'(?:formControlName|\[formControl\]|\[formGroup\]|formGroupName)=\"[^\"]+\"'),
 re.compile(r'\*ngIf=\"[^\"]+\"'), re.compile(r'@(?>if|else if)\s*\([^\n]+'),
 re.compile(r'\b(?:touched|dirty|submitted|validationAttempted|searchAttempted)\b'),
]
require(extracts(current,patterns)==extracts(restored,patterns),'validator/key/condition/precedence/binding structure changed')
# Regex literals and numeric validator constants are byte-identical outside declared copy by exact restoration; assert representative definitions still present.
allcur='\n'.join(txt(v) for k,v in current.items() if k.endswith(('.ts','.html')))
allbase='\n'.join(txt(v) for k,v in restored.items() if k.endswith(('.ts','.html')))
for token in ['Validators.pattern','Validators.min','Validators.max','Validators.minLength','Validators.maxLength','positiveMoneyValidator','timeFormat','amountOverflow','passwordMismatch','invalidEmailDomain']:
 require(allcur.count(token)==allbase.count(token),f'validator token count changed: {token}')

# Backend/external validation passthrough invariants.
pass_patterns=[r"form.getError\('backend'\)",r'result\.errors',r'issue\.technicalMessage',r'analysis\.issues',r'preview\.message',r'validation\.message',r'validation\.suggestedAction',r'issue\.mensaje',r'result\.resumen',r'validationResult\?\.resumen',r'ProblemDetails',r'ValidationProblemDetails',r'error\.message',r'response\.error']
for pat in pass_patterns:
 require(len(re.findall(pat,allcur))==len(re.findall(pat,allbase)),f'backend/external validation passthrough changed: {pat}')
for codepat in [r'\bR\d{2}\b',r'\bD\d{2}\b',r'\bCFA\b',r'\bSOAP\b',r'\bCENIT\b',r'\bMFT\b',r'\bSFTP\b']:
 require(re.findall(codepat,allcur)==re.findall(codepat,allbase),f'external/domain codes or terms changed: {codepat}')

# NAV4.2G feedback delta remains verbatim at its documented locations.
g=json.loads((ROOT/'docs/ai/navigation-menu/phases/NAV4_2G_COPY_DELTA.json').read_text(encoding='utf-8'))
for c in g['changes']:
 rel=c['file'].split('web/ach-interbank-ui/src/app/',1)[1]; ls=txt(current[rel]).splitlines(); n=int(c['line'])
 require(str(c['new']) in ls[n-1],f'NAV4.2G feedback copy regressed: {rel}:{n}')
# Prior D/E/F productive copy remains present; H did not alter their line count or product surfaces.
for phase in ['D','E','F']:
 pd=ROOT/f'docs/ai/navigation-menu/phases/NAV4_2{phase}_COPY_DELTA.json'
 if not pd.exists(): continue
 obj=json.loads(pd.read_text(encoding='utf-8'))
 for group in obj.get('changes',[]):
  rel=group.get('path','')
  if rel.startswith('web/ach-interbank-ui/src/app/'):
   rel=rel.split('web/ach-interbank-ui/src/app/',1)[1]
  for c in group.get('entries',[]):
   n=int(c.get('line',0) or 0)
   if rel in current and n:
    ls=txt(current[rel]).splitlines(); require(str(c['new']).strip() in ls[n-1],f'NAV4.2{phase} copy regressed: {rel}:{n}')

inv=INVENTORY.read_text(encoding='utf-8')
require('Exact hooks inspected:** **321**' in inv,'inventory exact hook count missing')
require('`DRIFT`: **92**' in inv,'inventory drift count missing')
for dec in ['DEC-NAV-4.2H-001','DEC-NAV-4.2H-002','DEC-NAV-4.2H-003']: require(dec in inv,f'missing decision {dec}')
# Exact mat-error count is invariant.
mat=sum(txt(v).count('<mat-error') for k,v in current.items() if k.endswith(('.html','.ts')))
require(mat==224,f'expected 224 mat-error hooks, got {mat}')
print(f'PASS NAV4.2H static validation: {ASSERTIONS} assertions; 92 copy changes; 321 hooks; baseline NAV4.2G reconstructed exactly.')

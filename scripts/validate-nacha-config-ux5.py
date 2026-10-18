#!/usr/bin/env python3
import hashlib, json, sys
from pathlib import Path
ROOT=Path(__file__).resolve().parents[1]
checks=[]
def check(name, cond):
    checks.append((name,bool(cond)))
def text(rel): return (ROOT/rel).read_text(encoding='utf-8')
# Protected bytes from UX0
manifest=json.loads(text('docs/NACHA_CONFIG_UX_IMPLEMENTATION_PACKAGE_2026-10-11/NACHA_CONFIG_UX0_ENGINE_BASELINE.json'))
for item in manifest.get('files', manifest if isinstance(manifest,list) else []):
    if isinstance(item,str): rel=item; expected=None
    else:
        rel=item.get('path') or item.get('file'); expected=item.get('sha256') or item.get('hash')
    p=ROOT/rel
    ok=p.exists()
    if ok and expected:
        ok=hashlib.sha256(p.read_bytes()).hexdigest().lower()==expected.lower()
    check('protected:'+rel,ok)
svc=text('src/Cfa.ACHInterbank.Persistence/ACH/Services/Implementation/NachaConfigHistoryService.cs')
ctrl=text('src/Cfa.ACHInterbank.Api/Controllers/NachaConfigProfilesController.cs')
pub=text('src/Cfa.ACHInterbank.Persistence/ACH/Services/Implementation/NachaConfigPublicationService.cs')
dtos=text('src/Cfa.ACHInterbank.Application/ACH/Models/NachaConfigAdminDtos.cs')
changes=text('web/ach-interbank-ui/src/app/features/nacha-config-admin/workspace/nacha-config-changes-panel.component.ts')
changes_html=text('web/ach-interbank-ui/src/app/features/nacha-config-admin/workspace/nacha-config-changes-panel.component.html')
hist_html=text('web/ach-interbank-ui/src/app/features/nacha-config-admin/workspace/nacha-config-history-panel.component.html')
workspace=text('web/ach-interbank-ui/src/app/features/nacha-config-admin/pages/nacha-config-profile-workspace-page.component.html')
lifecycle=text('web/ach-interbank-ui/src/app/features/nacha-config-admin/workspace/nacha-config-lifecycle-actions.component.html')
checks_core={
'change review dto':'NachaConfigChangeReviewDto' in dtos,
'human history dto':all(x in dtos for x in ['ActionNameEs','ElementNameEs','SummaryEs','DetailEs']),
'changes endpoint':'perfiles/{id:int}/cambios' in ctrl and 'ConfigRead' in ctrl,
'base comparison':'SupersedesProfileId' in svc,
'chamber isolation':'COMPARISON_BASE_CONTEXT_MISMATCH' in svc,
'real diff position':'Posición inicial' in svc,
'real diff length':'Longitud' in svc,
'real diff source':'Origen del valor' in svc,
'real diff format':'Formato' in svc,
'real diff rules':'Validación ·' in svc,
'real diff state':'Estado' in svc,
'history persisted':'HistConfigChanges' in svc,
'history humanized':'HumanizeHistory' in svc and 'Ver detalle' in hist_html,
'publish validates':'ValidateBeforePublishAsync' in pub,
'publish concurrency':'EnsureExpectedRowVersion' in pub,
'publish snapshot':'SnapshotType = "PUBLISH"' in pub,
'publish audit':'ChangeType = "PUBLISH"' in pub,
'publish result state':'EstadoNombreEs = "Publicado"' in pub,
'guided validates first':'this.command.probar(this.perfil.id)' in changes,
'guided refreshes diff':'this.query.cambios(this.perfil.id)' in changes,
'guided current rowversion':'this.command.publicar(this.perfil.id, review.rowVersion)' in changes,
'guided confirm':'Publicar nueva versión' in changes,
'double click guard':'this.publishing' in changes and '[disabled]="!canPublish || publishing"' in changes_html,
'changes tab':'app-nacha-config-changes-panel' in workspace,
'history tab':'app-nacha-config-history-panel' in workspace,
'no direct publish lifecycle':'Publicar' not in lifecycle,
'no JSON in ux5 templates':'JSON' not in changes_html+hist_html and 'PropertyPath' not in changes_html+hist_html,
}
for n,c in checks_core.items(): check(n,c)
# No literal brand colors in new SCSS
for rel in ['web/ach-interbank-ui/src/app/features/nacha-config-admin/workspace/nacha-config-changes-panel.component.scss','web/ach-interbank-ui/src/app/features/nacha-config-admin/workspace/nacha-config-history-panel.component.scss']:
    s=text(rel)
    import re
    check('no hex:'+rel, re.search(r'#[0-9a-fA-F]{3,8}\b',s) is None)
# Focused tests present
specs=text('web/ach-interbank-ui/src/app/features/nacha-config-admin/workspace/nacha-config-changes-panel.component.spec.ts')+text('web/ach-interbank-ui/src/app/features/nacha-config-admin/workspace/nacha-config-history-panel.component.spec.ts')+text('tests/Cfa.ACHInterbank.Tests/NachaConfigAdminServicesHardeningTests.cs')
for token in ['Sin cambios reales','bloquea publicación','CONCURRENCY','HumanizePositionSourceAndRuleChanges','History_ShouldExposeHumanized','Publish_ShouldCreateSingleImmutablePublishSnapshot','Ux5Publish_ShouldFailClosed_OnStaleRowVersionWithoutSnapshotOrAudit']:
    check('test:'+token, token in specs)
failed=[n for n,ok in checks if not ok]
if failed:
    print('FAIL_STATIC_UX5',len(checks)-len(failed),'/',len(checks))
    print('\n'.join('FAIL '+x for x in failed)); sys.exit(1)
print(f'PASS_STATIC_UX5 {len(checks)}/{len(checks)}')

#!/usr/bin/env python3
"""Focused static guardrail for NACHA Config UX4."""
from __future__ import annotations
import hashlib, json, re, sys
from pathlib import Path
ROOT=Path(__file__).resolve().parents[1]
PKG=ROOT/'docs/NACHA_CONFIG_UX_IMPLEMENTATION_PACKAGE_2026-10-11'
MANIFEST=PKG/'NACHA_CONFIG_UX0_ENGINE_BASELINE.json'
errors=[]; checks=[]
def read(rel):
    p=ROOT/rel
    if not p.is_file(): errors.append(f'Falta archivo requerido: {rel}'); return ''
    return p.read_text(encoding='utf-8-sig',errors='ignore')
def req(text, token, label):
    if token not in text: errors.append(f'No se confirmó {label}: falta {token!r}')
    else: checks.append(label)
def forbid(text, pattern, label):
    if re.search(pattern,text,re.I|re.M): errors.append(f'Se detectó {label}: {pattern!r}')
    else: checks.append(f'sin {label}')

# Immutable engine/goldens.
if not MANIFEST.is_file(): errors.append('Falta baseline UX0')
else:
    data=json.loads(MANIFEST.read_text())
    protected=data.get('files',[])
    for item in protected:
        p=ROOT/item['path']
        if not p.is_file(): errors.append(f"Falta archivo protegido: {item['path']}"); continue
        if hashlib.sha256(p.read_bytes()).hexdigest()!=item['sha256']: errors.append(f"Hash protegido cambió: {item['path']}")
    if protected and not any('protegido cambió' in e for e in errors): checks.append(f'motor/goldens inmutables ({len(protected)} archivos)')

models=read('src/Cfa.ACHInterbank.Application/ACH/Models/NachaConfigAdminDtos.cs')
iface=read('src/Cfa.ACHInterbank.Application/ACH/Interfaces/INachaConfigOperationalTestService.cs')
service=read('src/Cfa.ACHInterbank.Persistence/ACH/Services/Implementation/NachaConfigOperationalTestService.cs')
controller=read('src/Cfa.ACHInterbank.Api/Controllers/NachaConfigProfilesController.cs')
backend_tests=read('tests/Cfa.ACHInterbank.Tests/NachaConfigOperationalTestServiceTests.cs')
contract_tests=read('tests/Cfa.ACHInterbank.Tests/NachaConfigOperationalTestContractTests.cs')
openapi=read('tests/Cfa.ACHInterbank.Tests/OpenApiDocumentGenerationTests.cs')
api=read('web/ach-interbank-ui/src/app/core/services/nacha-config-api.service.ts')
api_spec=read('web/ach-interbank-ui/src/app/core/services/nacha-config-api.service.spec.ts')
panel_ts=read('web/ach-interbank-ui/src/app/features/nacha-config-admin/workspace/nacha-config-test-panel.component.ts')
panel_html=read('web/ach-interbank-ui/src/app/features/nacha-config-admin/workspace/nacha-config-test-panel.component.html')
panel_scss=read('web/ach-interbank-ui/src/app/features/nacha-config-admin/workspace/nacha-config-test-panel.component.scss')
panel_spec=read('web/ach-interbank-ui/src/app/features/nacha-config-admin/workspace/nacha-config-test-panel.component.spec.ts')
workspace=read('web/ach-interbank-ui/src/app/features/nacha-config-admin/pages/nacha-config-profile-workspace-page.component.html')

for token,label in [
 ('class NachaConfigOperationalTestResultDto','contrato resultado UX4'),
 ('class NachaConfigRecordPreviewDto','contrato vista previa de registro'),
 ('class NachaConfigFieldPreviewDto','contrato vista previa de campo'),
 ('SourceSummaryEs','origen humanizado en contrato'),
 ('RelevantRulesEs','reglas relevantes humanizadas')]: req(models,token,label)
req(iface,'TestAsync','servicio de prueba operacional')
for token,label in [
 ('_validation.ValidateBeforePublishAsync','reutilización validación backend'),
 ('_preview.PreviewResolverAsync','reutilización resolver backend'),
 ('new NachaOfficialRecordRenderer()','reutilización renderer oficial'),
 ('ContentPreviewAvailable','degradación segura de preview de contenido'),
 ('TotalLength = variant.TotalLength','longitud desde layout persistido'),
 ('SourceSummary(field)','origen explicado por backend'),
 ('INVALID_FIELD_RULE_CONFIG','regla inválida humanizada'),
 ('FIELD_OVERLAP','superposición humanizada'),
 ('INVALID_RECORD_LENGTH','longitud inválida humanizada'),
 ('MISSING_SOURCE','origen obligatorio ausente humanizado')]: req(service,token,label)
forbid(service,r'\bSaveChanges(?:Async)?\s*\(','persistencia en prueba UX4')
req(controller,'HttpPost("perfiles/{id:int}/probar")','endpoint Probar configuración')
req(controller,'Policy = P1Policies.ConfigRead','endpoint de solo lectura')
req(controller,'NachaConfigOperationalTestResultDto','respuesta tipada UX4')
req(openapi,'("POST", "/nacha-config/perfiles/{id}/probar")','ruta UX4 inventariada en contrato')
req(api,'probar(id: number)','cliente Angular UX4')
req(api_spec,'contrato operativo UX4 sin mutarla','prueba HTTP Angular UX4')

for token,label in [
 ('Probar configuración','acción operativa'),
 ('Estructura válida','resumen correcto'),
 ('advertencias','conteo advertencias'),
 ('Información avanzada','detalle técnico relegado'),
 ('preview.totalLength','regla visual dinámica'),
 ('field.sourceSummaryEs','origen humanizado visible'),
 ('field.sampleValue','valor resultante visible'),
 ('field.relevantRulesEs','regla relevante visible'),
 ('selectedFieldIdChange','selección sincronizada')]: req(panel_html+panel_ts,token,label)
req(workspace,'<app-nacha-config-test-panel','panel UX4 integrado al workspace')
forbid(panel_ts+r'\n'+panel_html,r'["\'](?:ACH|CENIT|PPD|CCD|CTX|CONSTANTE|ENTIDAD|EXPRESION)["\']','catálogo/regla NACHA fija en Angular UX4')
forbid(panel_scss,r'#[0-9a-f]{3,8}\b','color hexadecimal fijo UX4')
req(panel_scss,'var(--mat-sys-primary)','branding dinámico Material UX4')

for token,label in [
 ('ShouldRenderFixedValueAndCalculation_WithOfficialRenderer','prueba valor fijo/cálculo/renderer'),
 ('ShouldHumanizeOverlapLengthMissingSourceAndInvalidRule','prueba errores estructurales y regla inválida'),
 ('ShouldKeepPublishedProfileReadOnly_AndNotPersistChanges','prueba publicado sin mutación'),
 ('ShouldKeepAchAndCenitSelectionContextsIsolated','prueba aislamiento ACH/CENIT')]: req(backend_tests,token,label)
for token,label in [
 ('prueba la configuración y muestra validación + vista previa proveniente del backend','prueba Angular preview válida'),
 ('humaniza errores y advertencias','prueba Angular errores/advertencias'),
 ('sincroniza selección de campo','prueba Angular selección sincronizada'),
 ('estado de error conservando la configuración sin cambios','prueba Angular error')]: req(panel_spec,token,label)
for token,label in [
 ('TestConfiguration_UsesReadPolicyAndStableRoute','prueba contrato ruta/policy'),
 ('OperationalTestDto_ExposesHumanizedValidationSelectionAndPreviewContract','prueba contrato DTO')]: req(contract_tests,token,label)

if errors:
    print('NACHA_CONFIG_UX4: FAIL')
    for e in errors: print('-',e)
    sys.exit(1)
print(f'NACHA_CONFIG_UX4: PASS_STATIC {len(checks)}/{len(checks)}')
print('- validación, resolución y renderer oficial reutilizados sin persistencia')
print('- preview dinámico/humanizado + branding Material + selección sincronizada presentes')
print('- motor/goldens conservan hashes UX0')

#!/usr/bin/env python3
"""UX6 closing guardrail for the NACHA-M configuration experience.

Static/syntactic guard only. It never substitutes Angular, Playwright or .NET execution.
"""
from __future__ import annotations

import hashlib
import json
import re
import sys
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
FEATURE = ROOT / 'web/ach-interbank-ui/src/app/features/nacha-config-admin'
PACKAGE = ROOT / 'docs/NACHA_CONFIG_UX_IMPLEMENTATION_PACKAGE_2026-10-11'
MANIFEST = PACKAGE / 'NACHA_CONFIG_UX0_ENGINE_BASELINE.json'
checks: list[tuple[str, bool]] = []


def check(name: str, condition: bool) -> None:
    checks.append((name, bool(condition)))


def text(rel: str) -> str:
    path = ROOT / rel
    if not path.is_file():
        return ''
    return path.read_text(encoding='utf-8-sig', errors='ignore')


# Protected engine/golden bytes.
if MANIFEST.is_file():
    manifest = json.loads(MANIFEST.read_text(encoding='utf-8'))
    protected = manifest.get('files', [])
    check('manifiesto UX0 con archivos protegidos', bool(protected))
    for item in protected:
        path = ROOT / item['path']
        check(
            'hash protegido:' + item['path'],
            path.is_file() and hashlib.sha256(path.read_bytes()).hexdigest().lower() == item['sha256'].lower(),
        )
else:
    check('manifiesto UX0 presente', False)

# Active Angular sources only (exclude tests/e2e fixtures).
active_files = [
    p for p in FEATURE.rglob('*')
    if p.is_file() and p.suffix in {'.ts', '.html', '.scss'} and not p.name.endswith('.spec.ts')
]
active = '\n'.join(p.read_text(encoding='utf-8-sig', errors='ignore') for p in active_files)
active_scss = '\n'.join(p.read_text(encoding='utf-8-sig', errors='ignore') for p in active_files if p.suffix == '.scss')

check('sin colores hexadecimales fijos en estilos del feature', re.search(r'#[0-9a-fA-F]{3,8}\b', active_scss) is None)
check('sin rgb/rgba fijos en estilos del feature', re.search(r'rgba?\s*\(', active_scss, re.I) is None)
check('sin nombres corporativos fijos ACH Colombia/CENIT en Angular activo', re.search(r'ACH\s+Colombia|\bCENIT\b', active, re.I) is None)
check('sin catálogos funcionales NACHA fijos en Angular activo', re.search(r"[\"'](?:PPD|CCD|CTX|CONSTANTE|EXPRESION|SQL_VIEW|SQL_PROCEDURE)[\"']", active) is None)
check('sin any en contratos/código principal del feature', re.search(r'\bas\s+any\b|:\s*any\b|<any>', active) is None)
check('sin select nativo en templates del feature', '<select' not in '\n'.join(p.read_text(encoding='utf-8-sig', errors='ignore') for p in active_files if p.suffix == '.html'))
check('sin catálogo de presentación Angular duplicado', not (FEATURE / 'presentation/nacha-config-presentation.catalog.ts').exists())
for obsolete in [
    'pages/nacha-config-records-page.component.ts',
    'pages/nacha-config-variants-fields-page.component.ts',
    'workspace/nacha-config-field-detail.component.ts',
]:
    check('componente obsoleto eliminado:' + obsolete, not (FEATURE / obsolete).exists())
check('componentes productivos razonablemente acotados', all(p.stat().st_size <= 25_000 for p in active_files if p.suffix == '.ts'))

routing = text('web/ach-interbank-ui/src/app/features/nacha-config-admin/nacha-config-admin-routing.module.ts')
redirect = text('web/ach-interbank-ui/src/app/features/nacha-config-admin/workspace/nacha-config-legacy-workspace-redirect.component.ts')
workspace_ts = text('web/ach-interbank-ui/src/app/features/nacha-config-admin/pages/nacha-config-profile-workspace-page.component.ts')
workspace_html = text('web/ach-interbank-ui/src/app/features/nacha-config-admin/pages/nacha-config-profile-workspace-page.component.html')
workspace_scss = text('web/ach-interbank-ui/src/app/features/nacha-config-admin/pages/nacha-config-profile-workspace-page.component.scss')
profiles_ts = text('web/ach-interbank-ui/src/app/features/nacha-config-admin/pages/nacha-config-profiles-page.component.ts')
profiles_html = text('web/ach-interbank-ui/src/app/features/nacha-config-admin/pages/nacha-config-profiles-page.component.html')
field_ts = text('web/ach-interbank-ui/src/app/features/nacha-config-admin/workspace/nacha-config-field-editor.component.ts')
field_html = text('web/ach-interbank-ui/src/app/features/nacha-config-admin/workspace/nacha-config-field-editor.component.html')
field_list_html = text('web/ach-interbank-ui/src/app/features/nacha-config-admin/workspace/nacha-config-field-list.component.html')
ruler_html = text('web/ach-interbank-ui/src/app/features/nacha-config-admin/workspace/nacha-config-position-ruler.component.html')
changes_ts = text('web/ach-interbank-ui/src/app/features/nacha-config-admin/workspace/nacha-config-changes-panel.component.ts')
history_ts = text('web/ach-interbank-ui/src/app/features/nacha-config-admin/workspace/nacha-config-history-panel.component.ts')
test_ts = text('web/ach-interbank-ui/src/app/features/nacha-config-admin/workspace/nacha-config-test-panel.component.ts')
user_message = text('web/ach-interbank-ui/src/app/features/nacha-config-admin/services/nacha-config-user-message.ts')

# Unified workspace/navigation/decomposition.
check('rutas antiguas usan redirect unificado', routing.count('component: NachaConfigLegacyWorkspaceRedirectComponent') >= 2)
check('redirect conserva profileId hacia /perfiles/:id', "['/nacha-config-admin/perfiles', profileId]" in redirect)
check('redirect presenta estado anunciable', 'role="status"' in redirect and 'aria-live="polite"' in redirect)
for selector in [
    '<app-nacha-config-context-bar', '<app-nacha-config-record-list', '<app-nacha-config-position-ruler',
    '<app-nacha-config-field-list', '<app-nacha-config-field-editor', '<app-nacha-config-profile-metadata',
    '<app-nacha-config-lifecycle-actions', '<app-nacha-config-test-panel', '<app-nacha-config-changes-panel',
    '<app-nacha-config-history-panel'
]:
    check('workspace descompuesto:' + selector, selector in workspace_html)
check('workspace principal no monolítico', len(workspace_ts.encode()) < 10_000)

# Contextual catalogs / no front-end authority.
check('workspace solicita catálogos por cámara y flujo', 'catalogosOperativos(perfil.camara, perfil.flujo)' in workspace_ts)
check('creación recarga catálogos al cambiar cámara', 'camaraCode.valueChanges' in profiles_ts and 'this.cargarCatalogos()' in profiles_ts)
check('creación recarga catálogos al cambiar flujo', 'flujoCode.valueChanges' in profiles_ts)
check('servicios se construyen desde respuesta de catálogo', 'this.catalogos.servicios.map' in profiles_ts)

# Material, a11y and responsive.
check('selectores de perfiles usan Angular Material', '<mat-select' in profiles_html and '<mat-form-field' in profiles_html)
check('workspace anuncia carga/error', 'aria-busy' in workspace_html and 'aria-live="assertive"' in workspace_html)
check('editor anuncia errores', 'role="alert"' in field_html and 'aria-live="assertive"' in field_html)
check('filas de campos tienen teclado y nombre accesible', '(keydown.enter)' in field_list_html and '(keydown.space)' in field_list_html and '[attr.aria-label]="rowAria(row)"' in field_list_html)
check('regla de posiciones no depende solo del color', 'overlap-indicator' in ruler_html and 'símbolo “!”' in ruler_html)
check('foco del workspace se mueve al título tras navegación', '#workspaceHeading' not in workspace_html and 'workspaceHeading' in workspace_ts and '.focus()' in workspace_ts)
# Above intentionally checks template ref using literal '#workspaceHeading' would be false; validate directly next.
checks.pop()
check('foco del workspace se mueve al título tras navegación', '#workspaceHeading' in workspace_html and 'workspaceHeading' in workspace_ts and '.focus()' in workspace_ts)
check('workspace tiene foco visible/responsividad móvil', ':focus-visible' in active_scss and '@media (max-width: 760px)' in workspace_scss)
check('diálogos controlan foco', "autoFocus: 'dialog'" in changes_ts and 'restoreFocus: true' in changes_ts)
check('mensajes backend centralizados y operativos', 'nachaConfigUserMessage' in user_message and 'La configuración se conserva sin cambios' in user_message)

# Technical content is explicitly segregated and functional labels stay Spanish.
check('PropertyPath/DSL/JSON solo en bloque técnico marcado', 'data-technical-value="true"' in field_html and 'propertyPath' in field_html and 'expressionDsl' in field_html and 'ruleConfigJson' in field_html)
check('sin fallback visible en inglés', 'Política de fallback' not in field_html and 'Política de respaldo' in field_html)

# Published protection + fail-closed publication.
command_service = text('src/Cfa.ACHInterbank.Persistence/ACH/Services/Implementation/NachaConfigProfileCommandService.cs')
publication = text('src/Cfa.ACHInterbank.Persistence/ACH/Services/Implementation/NachaConfigPublicationService.cs')
immutability = text('src/Cfa.ACHInterbank.Persistence/DataBase/NachaConfigImmutabilityGuard.cs')
check('Angular bloquea editor si perfil no editable', 'this.profileEditable && !!this.editor?.isEditable' in field_ts and 'if (!this.canEdit' in field_ts)
check('workspace muestra protección publicada', 'nacha-config-published-protection' in workspace_html and '*ngIf="!perfil.isEditable"' in workspace_html)
check('backend limita comandos a BORRADOR', 'EnsureProfileIsBorrador(profile)' in command_service and 'Solo se puede editar perfiles en estado BORRADOR' in command_service)
check('guard global protege perfiles ya publicados', 'WasPublished' in immutability and 'PUBLISH' in immutability)
check('publicación revalida antes de persistir', publication.count('ValidateBeforePublishAsync') >= 2)
check('publicación exige row version fresca', 'EnsureExpectedRowVersion' in publication)
check('publicación crea snapshot y auditoría PUBLISH', 'SnapshotType = "PUBLISH"' in publication and 'ChangeType = "PUBLISH"' in publication)
check('publicación solo acepta BORRADOR', '"BORRADOR"' in publication and 'Solo se puede publicar un perfil en estado BORRADOR' in publication)

# Read-only operations and ACH/CENIT isolation remain server-side.
operational = text('src/Cfa.ACHInterbank.Persistence/ACH/Services/Implementation/NachaConfigOperationalTestService.cs')
history = text('src/Cfa.ACHInterbank.Persistence/ACH/Services/Implementation/NachaConfigHistoryService.cs')
query = text('src/Cfa.ACHInterbank.Persistence/ACH/Services/Implementation/NachaConfigProfileQueryService.cs')
check('Probar no persiste', 'SaveChanges' not in operational and 'AsNoTracking' in operational)
check('Cambios/Historial no persisten', 'SaveChanges' not in history and 'AsNoTracking' in history)
check('comparación bloquea cruce de cámara', 'COMPARISON_BASE_CONTEXT_MISMATCH' in history and 'ClearingHouseId' in history)
check('catálogo de servicios filtra por cámara', 'ClearingHouseId == null' in query and 'clearingHouseId.Value' in query)
operational_tests = text('tests/Cfa.ACHInterbank.Tests/NachaConfigOperationalTestServiceTests.cs')
check('prueba operacional mantiene aislamiento de cámara', 'ShouldKeepAchAndCenitSelectionContextsIsolated' in operational_tests)

# Branding is consumed from the existing global service, including light/dark contrast.
branding = text('web/ach-interbank-ui/src/app/core/services/branding.service.ts')
branding_spec = text('web/ach-interbank-ui/src/app/core/services/branding.service.spec.ts')
official_e2e = text('web/ach-interbank-ui/e2e/nacha-config-official-routes.spec.ts')
check('feature consume tokens y no duplica contraste', 'var(--mat-sys-primary)' in active_scss and 'relativeLuminance' not in active)
check('servicio global calcula on-primary Material', "'--mat-sys-on-primary'" in branding and 'getContrastColor' in branding)
check('prueba unitaria cubre primario claro y oscuro', 'uses deterministic readable contrast for light and dark primary colors' in branding_spec)
check('Playwright cubre branding claro y oscuro', '#123456' in official_e2e and '#f5e9a8' in official_e2e and '--mat-sys-on-primary' in official_e2e)

# Full UX6 Playwright contract (execution may be NOT_RUN_ENVIRONMENT).
ux6_e2e = text('web/ach-interbank-ui/e2e/nacha-config-ux6.spec.ts')
for i in range(1, 15):
    check(f'Playwright UX6 flujo {i:02d} presente', f'UX6-{i:02d}' in ux6_e2e)
check('Playwright usa guard de idioma en workspace', ux6_e2e.count('assertNoFunctionalSpanglish') >= 3)
check('Playwright cubre consola y pageerror', "page.on('console'" in ux6_e2e and "page.on('pageerror'" in ux6_e2e)
check('Playwright cubre tres viewports', all(token in ux6_e2e for token in ['width: 390', 'width: 820', 'width: 1440']))
check('Playwright verifica cancelación sin PUT', 'state.putCount).toBe(0)' in ux6_e2e)
check('Playwright verifica PUBLICADO protegido', 'nacha-config-published-protection' in ux6_e2e)

failed = [name for name, ok in checks if not ok]
if failed:
    print(f'FAIL_STATIC_UX6 {len(checks) - len(failed)}/{len(checks)}')
    for name in failed:
        print('FAIL', name)
    sys.exit(1)

print(f'PASS_STATIC_UX6 {len(checks)}/{len(checks)}')
print('- accesibilidad, Material, branding dinámico, rutas y estados UX6 presentes')
print('- PUBLICADO/fail-closed/read-only/aislamiento backend conservados')
print('- 14 flujos Playwright UX6 declarados; este guard no afirma su ejecución')
print('- motor/goldens conservan hashes UX0')

#!/usr/bin/env python3
"""UX0 static baseline guardrail for NACHA Config UX.

Pins generation-critical NACHA-M source/golden bytes and verifies the current
administration contracts characterized by UX0. It does not validate business
behavior in place of the .NET/Angular/Playwright suites.
"""
from __future__ import annotations

import hashlib
import json
import sys
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
PACKAGE = ROOT / "docs/NACHA_CONFIG_UX_IMPLEMENTATION_PACKAGE_2026-10-11"
MANIFEST = PACKAGE / "NACHA_CONFIG_UX0_ENGINE_BASELINE.json"

errors: list[str] = []
checks: list[str] = []


def read(rel: str) -> str:
    path = ROOT / rel
    if not path.exists():
        errors.append(f"Falta archivo requerido: {rel}")
        return ""
    return path.read_text(encoding="utf-8-sig", errors="ignore")


def require(text: str, token: str, label: str) -> None:
    if token not in text:
        errors.append(f"No se confirmó {label}: falta {token!r}")
    else:
        checks.append(label)


if not MANIFEST.exists():
    errors.append(f"Falta manifiesto de hashes: {MANIFEST.relative_to(ROOT)}")
else:
    manifest = json.loads(MANIFEST.read_text(encoding="utf-8"))
    files = manifest.get("files", [])
    if not files:
        errors.append("El manifiesto UX0 no contiene archivos protegidos")
    for item in files:
        rel = item["path"]
        path = ROOT / rel
        if not path.is_file():
            errors.append(f"Falta archivo protegido: {rel}")
            continue
        actual = hashlib.sha256(path.read_bytes()).hexdigest()
        expected = item["sha256"]
        if actual != expected:
            errors.append(f"Hash NACHA-M cambió: {rel}: esperado {expected}, actual {actual}")
    if files and not errors:
        checks.append(f"hashes protegidos ({len(files)} archivos)")

routing = read("web/ach-interbank-ui/src/app/features/nacha-config-admin/nacha-config-admin-routing.module.ts")
profiles = read("web/ach-interbank-ui/src/app/features/nacha-config-admin/pages/nacha-config-profiles-page.component.ts")
workspace = read("web/ach-interbank-ui/src/app/features/nacha-config-admin/pages/nacha-config-profile-workspace-page.component.ts")
field_editor = read("web/ach-interbank-ui/src/app/features/nacha-config-admin/workspace/nacha-config-field-editor.component.ts")
metadata = read("web/ach-interbank-ui/src/app/features/nacha-config-admin/workspace/nacha-config-profile-metadata.component.ts")
query = read("src/Cfa.ACHInterbank.Persistence/ACH/Services/Implementation/NachaConfigProfileQueryService.cs")
branding = read("web/ach-interbank-ui/src/app/core/services/branding.service.ts")
tokens = read("web/ach-interbank-ui/src/styles/_tokens.scss")
button = read("web/ach-interbank-ui/src/app/shared/components/ui/ui-boton.component.ts")
route_e2e = read("web/ach-interbank-ui/e2e/nacha-config-official-routes.spec.ts")
profiles_spec = read("web/ach-interbank-ui/src/app/features/nacha-config-admin/pages/nacha-config-profiles-page.component.spec.ts")
workspace_spec = read("web/ach-interbank-ui/src/app/features/nacha-config-admin/pages/nacha-config-profile-workspace-page.component.spec.ts")
backend_spec = read("tests/Cfa.ACHInterbank.Tests/NachaConfigAdminServicesHardeningTests.cs")
branding_spec = read("web/ach-interbank-ui/src/app/core/services/branding.service.spec.ts")

require(routing, "path: 'perfiles/:id'", "ruta de detalle /perfiles/:id")
require(routing, "component: NachaConfigProfileWorkspacePageComponent", "componente de detalle de perfil")
require(profiles, "this.router.navigate(['/nacha-config-admin/perfiles', perfil.profileId])", "navegación de listado a perfil existente")

for token, label in [
    ("_context.CatConfigStatuses", "catálogo de estados desde persistencia"),
    ("_context.CatClearingHouses", "catálogo de cámaras desde persistencia"),
    ("_context.CatFlowTypes", "catálogo de flujos desde persistencia"),
    ("_context.CatDirections", "catálogo de direcciones desde persistencia"),
    ("_context.CatServiceClasses", "catálogo de servicios desde persistencia"),
]:
    require(query, token, label)

require(field_editor, "return this.canManage && this.profileEditable && !!this.editor?.isEditable;", "edición limitada a borradores editables")
require(field_editor, "if (!this.canEdit || this.saving || !this.editor)", "guard de guardado para perfil no editable")
require(metadata, "return this.puedeGestionar && !!this.perfil?.isEditable;", "metadatos protegidos cuando el perfil no es editable")

for token, label in [
    ("'--brand-primary'", "token dinámico de marca"),
    ("'--mat-sys-primary'", "token dinámico Material"),
    ("root.style.setProperty(RUNTIME_TOKENS.materialPrimary, primary)", "aplicación runtime de branding Material"),
]:
    require(branding, token, label)
require(tokens, "--color-primary: var(--brand-primary);", "puente de marca hacia tokens de la SPA")
require(button, "background: var(--color-primary)", "botón primario consume token dinámico")

for text, token, label in [
    (route_e2e, "OfficialRoutes_ShouldUseConfigProfilesAndAvoidLegacyEndpoints", "regresión E2E listado/rutas oficiales sin endpoint legado"),
    (route_e2e, "Workspace_ShouldInheritServerBrandingAtRuntime", "caracterización E2E de branding dinámico"),
    (profiles, "this.router.navigate(['/nacha-config-admin/perfiles', perfil.profileId])", "caracterización de navegación de perfil existente"),
    (profiles_spec, "mantiene el formulario deshabilitado mientras cargan los catálogos", "caracterización Angular de carga de catálogos"),
    (workspace_spec, "shows humanized record, field list and basic field detail in one workspace", "caracterización Angular del workspace editable"),
    (workspace_spec, "protects a published configuration against direct editing", "caracterización Angular de inmutabilidad publicada"),
    (backend_spec, "UpdateDraftAsync_ShouldReject_WhenProfileIsPublished", "caracterización backend de inmutabilidad publicada"),
    (backend_spec, "GetFilterCatalogsAsync_ShouldReturnConfiguredCatalogs", "caracterización backend de catálogos"),
    (branding_spec, "updates runtime design tokens and Material primary tokens from buttonColor without recompiling", "caracterización unitaria de branding runtime"),
]:
    require(text, token, label)

if errors:
    print("NACHA_CONFIG_UX0_BASELINE: FAIL")
    for error in errors:
        print(f"- {error}")
    sys.exit(1)

print("NACHA_CONFIG_UX0_BASELINE: PASS_STATIC")
print(f"- checks: {len(checks)}")
print("- motor/goldens: bytes sin cambios respecto del manifiesto UX0")
print("- rutas, catálogos, borrador/publicado y branding: contratos de caracterización presentes")

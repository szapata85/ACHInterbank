#!/usr/bin/env python3
"""Focused static guardrail for NACHA Config UX2.

Validates the unified Angular workspace contracts without running Angular/.NET.
It also re-checks the immutable NACHA-M engine/golden byte manifest captured in UX0.
"""
from __future__ import annotations

import hashlib
import json
import re
import sys
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
PACKAGE = ROOT / "docs/NACHA_CONFIG_UX_IMPLEMENTATION_PACKAGE_2026-10-11"
MANIFEST = PACKAGE / "NACHA_CONFIG_UX0_ENGINE_BASELINE.json"
WORKSPACE = ROOT / "web/ach-interbank-ui/src/app/features/nacha-config-admin/workspace"

errors: list[str] = []
checks: list[str] = []


def read(rel: str) -> str:
    path = ROOT / rel
    if not path.is_file():
        errors.append(f"Falta archivo requerido: {rel}")
        return ""
    return path.read_text(encoding="utf-8-sig", errors="ignore")


def require(text: str, token: str, label: str) -> None:
    if token not in text:
        errors.append(f"No se confirmó {label}: falta {token!r}")
    else:
        checks.append(label)


def forbid(text: str, pattern: str, label: str) -> None:
    if re.search(pattern, text, flags=re.IGNORECASE | re.MULTILINE):
        errors.append(f"Se detectó {label}: patrón {pattern!r}")
    else:
        checks.append(f"sin {label}")


if not MANIFEST.is_file():
    errors.append("Falta NACHA_CONFIG_UX0_ENGINE_BASELINE.json")
else:
    manifest = json.loads(MANIFEST.read_text(encoding="utf-8"))
    files = manifest.get("files", [])
    if not files:
        errors.append("El manifiesto UX0 no contiene archivos protegidos")
    for item in files:
        path = ROOT / item["path"]
        if not path.is_file():
            errors.append(f"Falta archivo protegido: {item['path']}")
            continue
        actual = hashlib.sha256(path.read_bytes()).hexdigest()
        if actual != item["sha256"]:
            errors.append(f"Hash protegido cambió: {item['path']}")
    if files and not any("Hash protegido" in e or "archivo protegido" in e for e in errors):
        checks.append(f"motor/goldens inmutables ({len(files)} archivos)")

routing = read("web/ach-interbank-ui/src/app/features/nacha-config-admin/nacha-config-admin-routing.module.ts")
page_ts = read("web/ach-interbank-ui/src/app/features/nacha-config-admin/pages/nacha-config-profile-workspace-page.component.ts")
page_html = read("web/ach-interbank-ui/src/app/features/nacha-config-admin/pages/nacha-config-profile-workspace-page.component.html")
page_scss = read("web/ach-interbank-ui/src/app/features/nacha-config-admin/pages/nacha-config-profile-workspace-page.component.scss")
context_html = read("web/ach-interbank-ui/src/app/features/nacha-config-admin/workspace/nacha-config-context-bar.component.html")
context_ts = read("web/ach-interbank-ui/src/app/features/nacha-config-admin/workspace/nacha-config-context-bar.component.ts")
record_html = read("web/ach-interbank-ui/src/app/features/nacha-config-admin/workspace/nacha-config-record-list.component.html")
record_ts = read("web/ach-interbank-ui/src/app/features/nacha-config-admin/workspace/nacha-config-record-list.component.ts")
field_list_html = read("web/ach-interbank-ui/src/app/features/nacha-config-admin/workspace/nacha-config-field-list.component.html")
field_editor_html = read("web/ach-interbank-ui/src/app/features/nacha-config-admin/workspace/nacha-config-field-editor.component.html")
legacy_redirect = read("web/ach-interbank-ui/src/app/features/nacha-config-admin/workspace/nacha-config-legacy-workspace-redirect.component.ts")
workspace_spec = read("web/ach-interbank-ui/src/app/features/nacha-config-admin/pages/nacha-config-profile-workspace-page.component.spec.ts")
redirect_spec = read("web/ach-interbank-ui/src/app/features/nacha-config-admin/workspace/nacha-config-legacy-workspace-redirect.component.spec.ts")
e2e = read("web/ach-interbank-ui/e2e/nacha-config-official-routes.spec.ts")

for token, label in [
    ("query.workspace(this.perfilId)", "consumo de NachaConfigWorkspaceDto"),
    ("query.catalogosOperativos(perfil.camara, perfil.flujo)", "catálogos operativos contextuales"),
    ("selectedRecordCode", "navegación por registro"),
    ("selectedFieldId", "navegación por campo"),
]:
    require(page_ts, token, label)

for token, label in [
    ("<mat-tab-group", "pestañas Material"),
    ("<app-nacha-config-context-bar", "barra de contexto"),
    ("<app-nacha-config-record-list", "lista de registros"),
    ("<app-nacha-config-field-list", "lista de campos"),
    ("<app-nacha-config-field-editor", "editor/detalle de campo"),
    ("*ngIf=\"!perfil.isEditable\"", "protección de publicado/no editable"),
    ("Cargando configuración", "estado de carga"),
    ("No fue posible cargar la configuración", "estado de error"),
]:
    require(page_html, token, label)

for token, label in [
    ("<mat-form-field", "form fields Material"),
    ("<mat-select", "selectores Material"),
    ("Cámara", "cámara visible"),
    ("Operación / flujo", "operación/flujo visible"),
    ("Dirección", "dirección visible"),
    ("Servicio", "servicio visible"),
    ("Versión", "versión visible"),
    ("Vigencia", "vigencia visible"),
    ("Estado", "estado visible"),
]:
    require(context_html, token, label)

require(context_ts, "catalogos?.servicios", "servicios provenientes del contrato dinámico")
require(record_ts, "record.recordNameEs", "registro humanizado desde backend")
require(field_list_html, "mat-table", "listado Material de campos")
require(field_editor_html, "Información avanzada", "detalle técnico relegado a expansión")
require(field_editor_html, "Dato o cálculo autorizado", "origen humanizado dirigido por backend")
require(field_editor_html, "Nombre funcional", "detalle funcional del campo en el editor")
require(legacy_redirect, "['/nacha-config-admin/perfiles', profileId]", "compatibilidad de rutas antiguas")
require(routing, "component: NachaConfigLegacyWorkspaceRedirectComponent", "records/variants redirigen al workspace")

for token, label in [
    ("inherits runtime branding token without rebuilding the SPA", "prueba DOM de branding runtime"),
    ("loads workspace and contextual catalogs from UX1 contracts", "prueba focal de contratos UX1"),
    ("protects a published configuration against direct editing", "prueba de protección publicada"),
]:
    require(workspace_spec, token, label)
require(redirect_spec, "redirects legacy routes to the unified workspace", "prueba de ruta legacy sin 404")
require(e2e, "Workspace_ShouldInheritServerBrandingAtRuntime", "regresión Playwright de branding dinámico")
require(e2e, "variants-fields?profileId=10", "regresión Playwright ruta variants-fields")
require(e2e, "records?profileId=10", "regresión Playwright ruta records")

production_workspace = "\n".join(
    path.read_text(encoding="utf-8-sig", errors="ignore")
    for path in WORKSPACE.glob("*.ts")
    if not path.name.endswith(".spec.ts")
) + "\n" + page_ts
production_styles = "\n".join(
    path.read_text(encoding="utf-8-sig", errors="ignore")
    for path in WORKSPACE.glob("*.scss")
) + "\n" + page_scss

forbid(production_workspace, r"nacha-config-presentation\.catalog", "catálogo funcional fijo de Angular en el workspace UX2")
forbid(production_styles, r"#[0-9a-f]{3,8}\b", "colores hexadecimales fijos en estilos UX2")
require(production_styles, "var(--mat-sys-primary)", "branding mediante token Material dinámico")

if errors:
    print("NACHA_CONFIG_UX2: FAIL")
    for error in errors:
        print(f"- {error}")
    sys.exit(1)

print(f"NACHA_CONFIG_UX2: PASS_STATIC {len(checks)}/{len(checks)}")
print("- workspace unificado, Material, catálogos UX1, protección y rutas: presentes")
print("- motor/goldens: bytes sin cambios respecto del manifiesto UX0")

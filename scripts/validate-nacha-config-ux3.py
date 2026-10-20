#!/usr/bin/env python3
"""Focused static guardrail for NACHA Config UX3.

Validates the operational field editor/read contracts, shared-source protection,
Angular humanized editor/ruler, and immutable UX0 engine/golden bytes without
requiring .NET or Angular dependencies.
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


# UX0 immutable engine/goldens.
if not MANIFEST.is_file():
    errors.append("Falta NACHA_CONFIG_UX0_ENGINE_BASELINE.json")
else:
    manifest = json.loads(MANIFEST.read_text(encoding="utf-8"))
    protected = manifest.get("files", [])
    for item in protected:
        path = ROOT / item["path"]
        if not path.is_file():
            errors.append(f"Falta archivo protegido: {item['path']}")
            continue
        actual = hashlib.sha256(path.read_bytes()).hexdigest()
        if actual != item["sha256"]:
            errors.append(f"Hash protegido cambió: {item['path']}")
    if protected and not any("protegido" in error for error in errors):
        checks.append(f"motor/goldens inmutables ({len(protected)} archivos)")

models = read("src/Cfa.ACHInterbank.Application/ACH/Models/NachaConfigAdminDtos.cs")
query_iface = read("src/Cfa.ACHInterbank.Application/ACH/Interfaces/INachaConfigProfileQueryService.cs")
command_iface = read("src/Cfa.ACHInterbank.Application/ACH/Interfaces/INachaConfigProfileCommandService.cs")
query_service = read("src/Cfa.ACHInterbank.Persistence/ACH/Services/Implementation/NachaConfigProfileQueryService.cs")
command_service = read("src/Cfa.ACHInterbank.Persistence/ACH/Services/Implementation/NachaConfigProfileCommandService.cs")
controller = read("src/Cfa.ACHInterbank.Api/Controllers/NachaConfigProfilesController.cs")
backend_tests = read("tests/Cfa.ACHInterbank.Tests/NachaConfigAdminServicesHardeningTests.cs")
openapi_tests = read("tests/Cfa.ACHInterbank.Tests/OpenApiDocumentGenerationTests.cs")

for token, label in [
    ("class NachaConfigFieldEditorDto", "read model tipado del editor"),
    ("class NachaConfigFieldEditorUpdateRequest", "comando tipado del editor"),
    ("NachaConfigFieldSourceEditDto", "contrato tipado de origen"),
    ("NachaConfigFieldRuleEditorUpdateDto", "contrato tipado de reglas"),
    ("ExpectedRowVersion", "concurrencia en contrato UX3"),
]:
    require(models, token, label)
require(query_iface, "GetFieldEditorAsync", "consulta de editor en interfaz")
require(command_iface, "UpdateFieldEditorAsync", "comando de editor en interfaz")

for token, label in [
    ("CatDataSourceTypes", "tipos de origen desde catálogo persistido"),
    ("SourceDefinitionId", "opciones de origen desde definiciones persistidas"),
    ("ClearingHouseId == profile.ClearingHouseId", "aislamiento contextual por cámara"),
    ("SourceReferenceCount", "conteo de uso de fuente compartida"),
    ("EditorSourceTypeLabel", "humanización server-side de origen"),
]:
    require(query_service, token, label)

for token, label in [
    ("EnsureExpectedRowVersion(profile, request.ExpectedRowVersion)", "control de concurrencia"),
    ("EnsureProfileIsBorrador(profile)", "inmutabilidad de publicado"),
    ("CountAsync(x => x.SourceDefinitionId == current.Id", "conteo antes de mutar fuente"),
    ("new CfgFieldSourceDefinition", "aislamiento mediante clon de fuente compartida"),
    ("field.SourceDefinition = isolated", "revinculación exclusiva del campo"),
    ("FIELD_EDITOR_UPDATE", "auditoría de edición UX3"),
    ("SOURCE_OPTION_NOT_AVAILABLE", "validación backend de opción contextual"),
    ("FORMAT_NOT_AVAILABLE", "validación backend de formato autorizado"),
    ("RULE_NOT_AVAILABLE", "validación backend de reglas existentes"),
    ("ApplyLegacyPropertyPathAsync", "protección compartida también en endpoint legado compatible"),
]:
    require(command_service, token, label)

require(controller, 'HttpGet("perfiles/{id:int}/fields/{fieldId:int}/editor")', "endpoint GET del editor")
require(controller, 'HttpPut("perfiles/{id:int}/fields/{fieldId:int}/editor")', "endpoint PUT del editor")
require(openapi_tests, '"/nacha-config/perfiles/{id}/fields/{fieldId}/editor"', "contrato OpenAPI del editor")

editor_ts = read("web/ach-interbank-ui/src/app/features/nacha-config-admin/workspace/nacha-config-field-editor.component.ts")
editor_html = read("web/ach-interbank-ui/src/app/features/nacha-config-admin/workspace/nacha-config-field-editor.component.html")
editor_scss = read("web/ach-interbank-ui/src/app/features/nacha-config-admin/workspace/nacha-config-field-editor.component.scss")
ruler_ts = read("web/ach-interbank-ui/src/app/features/nacha-config-admin/workspace/nacha-config-position-ruler.component.ts")
ruler_html = read("web/ach-interbank-ui/src/app/features/nacha-config-admin/workspace/nacha-config-position-ruler.component.html")
ruler_scss = read("web/ach-interbank-ui/src/app/features/nacha-config-admin/workspace/nacha-config-position-ruler.component.scss")
workspace_html = read("web/ach-interbank-ui/src/app/features/nacha-config-admin/pages/nacha-config-profile-workspace-page.component.html")
editor_spec = read("web/ach-interbank-ui/src/app/features/nacha-config-admin/workspace/nacha-config-field-editor.component.spec.ts")
ruler_spec = read("web/ach-interbank-ui/src/app/features/nacha-config-admin/workspace/nacha-config-position-ruler.component.spec.ts")
api_spec = read("web/ach-interbank-ui/src/app/core/services/nacha-config-api.service.spec.ts")
user_message = read("web/ach-interbank-ui/src/app/features/nacha-config-admin/services/nacha-config-user-message.ts")

for token, label in [
    ("ReactiveFormsModule", "Reactive Forms en editor"),
    ("actualizarEditorCampo", "API Angular tipada"),
    ("sourceType?.requiresDefinitionSelection", "compatibilidad de origen dirigida por backend"),
    ("sourceType?.usesFixedValue", "valor fijo dirigido por backend"),
    ("expectedRowVersion: this.editor.rowVersion", "token de concurrencia enviado"),
]:
    require(editor_ts, token, label)
require(user_message, "La configuración fue modificada por otro usuario. Recarga los cambios antes de continuar.", "mensaje de concurrencia UX3 centralizado")

for token, label in [
    ("Nombre funcional", "nombre funcional editable"),
    ("Posición inicial", "posición editable"),
    ("Longitud", "longitud editable"),
    ("Carácter de relleno", "relleno editable"),
    ("Alineación", "alineación dinámica"),
    ("Formato", "formato dinámico"),
    ("Origen del valor", "origen humanizado"),
    ("Valor fijo", "valor fijo"),
    ("Dato o cálculo autorizado", "dato/cálculo por opción autorizada"),
    ("Validaciones y comportamiento ante error", "reglas operativas"),
    ("Información avanzada", "información técnica avanzada"),
    ("Solo lectura", "avanzado de solo lectura"),
]:
    require(editor_html, token, label)

for token, label in [
    ("variant?.totalLength", "regla usa longitud del read model"),
    ("hasOverlap", "detección visual de superposición"),
    ("ariaLabel", "texto alternativo accesible"),
]:
    require(ruler_ts, token, label)
require(ruler_html, 'role="option"', "bloques seleccionables por teclado")
require(workspace_html, "<app-nacha-config-position-ruler", "regla integrada al workspace")
require(workspace_html, "<app-nacha-config-field-editor", "editor integrado al workspace")

# No Angular authority for source codes/rules, and no fixed branding colors.
forbid(editor_ts, r'["\'](?:CONSTANTE|ENTIDAD|EXPRESION|SQL_VIEW|SQL_PROCEDURE|REQUIRED|REGEX|RANGE|ENUM|CHECKSUM)["\']', "catálogo/regla funcional fija en Angular UX3")
forbid(editor_html, r'formControlName="(?:propertyPath|expressionDsl|ruleConfigJson|conditionDsl)"', "datos técnicos editables en Angular UX3")
forbid(editor_scss + "\n" + ruler_scss, r"#[0-9a-f]{3,8}\b", "colores hexadecimales fijos en UX3")
require(editor_scss + "\n" + ruler_scss, "var(--mat-sys-primary)", "branding dinámico por tokens Material")

for token, label in [
    ("GetFieldEditorAsync_ShouldExposeHumanizedSources_AndKeepClearingHousesIsolated", "prueba backend carga/aislamiento ACH-CENIT"),
    ("UpdateFieldEditorAsync_ShouldSaveOperationalSettings_Rules_AndIsolateSharedFixedSource", "prueba backend guardado y fuente compartida"),
    ("UpdateFieldEditorAsync_ShouldRelinkPersistedCalculation_WithoutMutatingSharedDefinition", "prueba backend cálculo autorizado"),
    ("UpdateFieldEditorAsync_ShouldFail_OnConcurrencyConflict", "prueba backend concurrencia"),
    ("UpdateFieldEditorAsync_ShouldProtectPublishedProfile", "prueba backend publicado protegido"),
]:
    require(backend_tests, token, label)
for token, label in [
    ("saves fixed value, format, alignment, padding and rules", "prueba Angular guardado completo"),
    ("changes to a persisted calculation by opaque source id", "prueba Angular cambio a cálculo"),
    ("protects a published configuration from direct save", "prueba Angular publicado"),
    ("shows the required concurrency message", "prueba Angular concurrencia"),
]:
    require(editor_spec, token, label)
require(ruler_spec, "marks overlap/inactive state", "prueba de regla visual")
require(api_spec, "debe consultar el editor operativo contextual del campo", "prueba contrato GET Angular")
require(api_spec, "debe actualizar el editor operativo con contrato tipado", "prueba contrato PUT Angular")

if errors:
    print("NACHA_CONFIG_UX3: FAIL")
    for error in errors:
        print(f"- {error}")
    sys.exit(1)

print(f"NACHA_CONFIG_UX3: PASS_STATIC {len(checks)}/{len(checks)}")
print("- editor tipado/humanizado, origen contextual, reglas, concurrencia y fuente compartida: presentes")
print("- regla visual por read model y branding dinámico: presentes")
print("- motor/goldens: bytes sin cambios respecto del manifiesto UX0")

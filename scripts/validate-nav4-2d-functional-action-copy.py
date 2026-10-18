#!/usr/bin/env python3
from __future__ import annotations

import hashlib
import json
import re
import subprocess
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
APP_ROOT = ROOT / "web/ach-interbank-ui/src/app"
ASSERTIONS = 0


def require(condition: bool, message: str) -> None:
    global ASSERTIONS
    ASSERTIONS += 1
    if not condition:
        raise AssertionError(message)


def read(path: str) -> str:
    return (ROOT / path).read_text(encoding="utf-8")


def sha(path: str) -> str:
    return hashlib.sha256((ROOT / path).read_bytes()).hexdigest()


# NAV4.1A/B/4.2A and NAV2/NAV3 protected surfaces remain byte-identical.
protected = {
    "web/ach-interbank-ui/src/app/core/services/navigation.service.ts": "4df488ac45465a5283a02afe795fc64c4e5995a3415556d324275cc5e1d7ea93",
    "web/ach-interbank-ui/src/app/core/services/menu-route-surface.service.ts": "3a32dc7921e3c805e32fa9921e371109813aae9ba3a3173bda6a5b6b7030df72",
    "web/ach-interbank-ui/src/app/core/services/navigation-naming.service.ts": "de6d4a402690956914a67f26466c7ea6c1cfac16eb717dbeb4d4b6f56e59db19",
    "web/ach-interbank-ui/src/app/core/services/navigation-naming.util.ts": "17e066e755f589ee818b6fa48fbcf3963ba14886a35cdf3187e5381e80368895",
    "src/Cfa.ACHInterbank.Application/Navigation/Queries/GetMenuForCurrentUserHandler.cs": "f111e416c181fc1912c297e37012cf7462f49d0434c1ef6ec9416c89b5ed1a61",
    "src/Cfa.ACHInterbank.Persistence/Navigation/Seeders/NavigationInformationArchitectureSeeder.cs": "28371549f4b9150fdf738bee11454c98117ba420d1abae0e9971e2c49aee9f4c",
    "src/Cfa.ACHInterbank.Persistence/Security/Seeders/FineGrainedGrantBackfillSeeder.cs": "e4f2b80b0b9e9c3aff636fb03e96b819019c07eeabc549bcfe4f789756775437",
    "src/Cfa.ACHInterbank.Persistence/Security/Seeders/LegacyFineGrainedRoleMigrationSeeder.cs": "e2366e75faab4ee31498d0cb84a93899a2a928698b01e4401a38ab1cc9c18105",
    "web/ach-interbank-ui/src/app/core/guards/permission.guard.ts": "7d9541b3899543ad40802b18cb4e734a9c14e59077b732108b31396213afa73a",
    "web/ach-interbank-ui/src/app/core/guards/role.guard.ts": "520fcea2357e5cfe2b19d3ad1d0b284ee84d21149b359025126308210d6de31e",
}
for path, expected in protected.items():
    require(sha(path) == expected, f"protected file changed: {path}")

fine = read("src/Cfa.ACHInterbank.Persistence/Security/Seeders/FineGrainedGrantBackfillSeeder.cs")
legacy = read("src/Cfa.ACHInterbank.Persistence/Security/Seeders/LegacyFineGrainedRoleMigrationSeeder.cs")
require("BackfillOrder = 119" in fine, "FineGrainedGrantBackfillSeeder must remain Order 119")
require("MigrationOrder = 120" in legacy, "LegacyFineGrainedRoleMigrationSeeder must remain Order 120")

# Strong no-logic-delta proof: reverting only the documented copy lines must recreate
# the exact complete NAV4.2C SPA source tree (including specs).
delta_path = ROOT / "docs/ai/navigation-menu/phases/NAV4_2D_COPY_DELTA.json"
delta = json.loads(delta_path.read_text(encoding="utf-8"))
require(delta.get("files") == 39, "NAV4.2D delta must cover exactly 39 SPA files")
require(delta.get("lines") == 49, "NAV4.2D delta must cover exactly 49 changed SPA lines")
changes = {item["path"]: item["entries"] for item in delta["changes"]}
require(len(changes) == 39, "NAV4.2D delta file list mismatch")


def restore_copy(path: Path, entries: list[dict[str, object]]) -> bytes:
    raw_lines = path.read_bytes().splitlines(keepends=True)
    for entry in entries:
        line_no = int(entry["line"])
        require(1 <= line_no <= len(raw_lines), f"delta line outside file: {path}:{line_no}")
        raw = raw_lines[line_no - 1]
        if raw.endswith(b"\r\n"):
            ending = b"\r\n"
            body = raw[:-2]
        elif raw.endswith(b"\n") or raw.endswith(b"\r"):
            ending = raw[-1:]
            body = raw[:-1]
        else:
            ending = b""
            body = raw
        current = body.decode("utf-8")
        require(current == entry["new"], f"unexpected NAV4.2D line content: {path}:{line_no}")
        raw_lines[line_no - 1] = str(entry["old"]).encode("utf-8") + ending
    return b"".join(raw_lines)

aggregate = hashlib.sha256()
for path in sorted(p for p in APP_ROOT.rglob("*") if p.is_file()):
    rel = path.relative_to(APP_ROOT).as_posix()
    data = restore_copy(path, changes[rel]) if rel in changes else path.read_bytes()
    rel_bytes = rel.encode("utf-8")
    aggregate.update(len(rel_bytes).to_bytes(4, "big"))
    aggregate.update(rel_bytes)
    aggregate.update(len(data).to_bytes(8, "big"))
    aggregate.update(data)
require(
    aggregate.hexdigest() == "95493edb9108bf6ff328adc33bf5e98adbbb9c9ea283efda5d890c3d3721004d",
    "SPA tree differs from NAV4.2C by more than the documented copy-only lines",
)

# Focal copy conventions and known drifts.
markers = {
    "web/ach-interbank-ui/src/app/layout/main-layout.component.html": [
        '(click)="logout()"', 'aria-label="Cerrar sesión"', 'matTooltip="Cerrar sesión"', '<span class="logout-label">Cerrar sesión</span>'
    ],
    "web/ach-interbank-ui/src/app/features/ach-colombia-mft-administration/ach-colombia-mft-administration.component.html": [
        '(click)="load()"', '<mat-icon>refresh</mat-icon>Actualizar', 'Probar conexión'
    ],
    "web/ach-interbank-ui/src/app/features/integrations/pages/contract-import-stepper.component.html": [
        '(click)="analyzeSelectedFile()"', 'Analizar archivo', '(click)="validateOperation()"', 'Validar operación',
        '(ngSubmit)="importDraft()"', 'Crear versión en borrador'
    ],
    "web/ach-interbank-ui/src/app/features/integrations/pages/mapping-editor-page.component.html": [
        '(click)="runValidation()"', "'Validar'", '(click)="runPreview()"', "'Vista previa'", '(click)="clone()"', "'Clonar configuración'"
    ],
    "web/ach-interbank-ui/src/app/features/admin/components/user-roles.component.html": [
        '(click)="save()"', "'Guardar perfiles de acceso'", '[disabled]="saving"'
    ],
    "web/ach-interbank-ui/src/app/features/nacha-security/components/nacha-certificate-manager.component.html": [
        "openUpload('CfaSigningAndDecryption')", 'Cargar certificado de firma y descifrado', 'Agregar certificado de cámara'
    ],
    "web/ach-interbank-ui/src/app/features/transactions/components/ach-return-of-return-management/ach-return-of-return-management.component.html": [
        '(click)="generateAudit()"', 'Generar archivo de auditoría', '(click)="generateNacha()"', 'Generar archivo NACHA-M productivo'
    ],
    "web/ach-interbank-ui/src/app/features/transactions/components/bulk-ingestion-upload/bulk-ingestion-upload.component.html": [
        '(click)="upload()"', "'Enviar archivo'"
    ],
    "web/ach-interbank-ui/src/app/features/nacha-security/components/manual-encrypt-operation.component.html": ["'Cifrar archivo'"],
    "web/ach-interbank-ui/src/app/features/nacha-security/components/manual-decrypt-operation.component.html": ["'Descifrar archivo'"],
}
for path, expected_markers in markers.items():
    source = read(path)
    for marker in expected_markers:
        require(marker in source, f"missing NAV4.2D marker {marker!r} in {path}")

# Generic action-copy drift eliminated from actual controls, while out-of-scope messages/snackbars may retain wording.
for path in [
    "web/ach-interbank-ui/src/app/features/ach-claims/ach-claims-detail.component.html",
    "web/ach-interbank-ui/src/app/features/admin/components/users-list.component.html",
    "web/ach-interbank-ui/src/app/features/scheduler/components/task-definitions.component.html",
    "web/ach-interbank-ui/src/app/features/reports/components/report-list-page.component.html",
    "web/ach-interbank-ui/src/app/features/reports/components/traceability-report.component.html",
    "web/ach-interbank-ui/src/app/features/reports/components/reconciliation-report.component.html",
    "web/ach-interbank-ui/src/app/features/integrations/pages/mapping-sets-page.component.html",
]:
    source = read(path)
    require("Intentar nuevamente</button>" not in source, f"legacy retry action remains in {path}")
    require("Volver a intentar</button>" not in source, f"legacy retry action remains in {path}")

for path in [
    "web/ach-interbank-ui/src/app/features/audit/components/audit-log.component.html",
    "web/ach-interbank-ui/src/app/features/auth-logs/components/auth-log.component.html",
    "web/ach-interbank-ui/src/app/features/navigation-logs/components/navigation-log.component.html",
    "web/ach-interbank-ui/src/app/features/ach-responses/pages/ach-response-list-page.component.html",
    "web/ach-interbank-ui/src/app/features/ach-responses/pages/ach-response-dashboard-page.component.html",
    "web/ach-interbank-ui/src/app/features/ach-responses/pages/ach-response-manual-review-page.component.html",
    "web/ach-interbank-ui/src/app/features/ach-responses/pages/ach-response-status-mappings-page.component.html",
]:
    require("Limpiar filtros" in read(path), f"clear-filters copy not aligned in {path}")

# Six inherited hybrid controls: four stay as-is, two are clarified from their real mutations.
hybrid_checks = {
    "web/ach-interbank-ui/src/app/features/ach-claims/ach-claims-list.component.html": ["Nueva reclamación", '(click)="openCreateDialog()"'],
    "web/ach-interbank-ui/src/app/features/incoming-nacha-command-center/pages/nacha-operational-dashboard.component.html": ["Actualizar información", '(click)="refresh()"'],
    "web/ach-interbank-ui/src/app/features/admin/components/user-roles.component.html": ["Guardar perfiles de acceso", '(click)="save()"'],
    "web/ach-interbank-ui/src/app/features/integrations/pages/mapping-editor-page.component.html": ["Clonar configuración", '(click)="clone()"'],
    "web/ach-interbank-ui/src/app/features/nacha-config-admin/pages/nacha-config-profiles-page.component.html": ["Crear borrador", '(accion)="crearBorrador()"'],
}
for path, hs in hybrid_checks.items():
    src = read(path)
    for marker in hs:
        require(marker in src, f"hybrid action changed unexpectedly: {marker!r} in {path}")

# Service methods/endpoints and behavior remain exactly where they were.
service_markers = {
    "web/ach-interbank-ui/src/app/features/admin/components/user-roles.component.ts": ["this.usersApi.assignRoles(this.user.id, Array.from(this.selectedRoleIds))", "this.router.navigate(['/users/list'])"],
    "web/ach-interbank-ui/src/app/features/admin/services/users-api.service.ts": ["assignRoles(id: string, roleIds: string[])"],
    "web/ach-interbank-ui/src/app/features/integrations/pages/mapping-editor-page.component.ts": ["this.api.clone(this.mappingSet.id", "this.router.navigate(['/integraciones/relacion-informacion', created.methodCode, created.id])"],
    "web/ach-interbank-ui/src/app/features/integrations/data-access/integration-mapping.api.ts": ["`api/integrations/mappingsets/${mappingSetId}/clone`"],
    "web/ach-interbank-ui/src/app/features/integrations/pages/contract-import-stepper.component.ts": ["this.store.analyzeArtifact();", "this.store.analyzeArtifact(", "this.store.importArtifactDraft("],
    "web/ach-interbank-ui/src/app/features/integrations/pages/soap-integration-settings.component.ts": ["this.loadSettings(true);", "this.api.getSettings()", "this.api.previewChange(request)"],
    "web/ach-interbank-ui/src/app/features/transactions/components/ach-return-of-return-management/ach-return-of-return-management.component.ts": ["this.returnsApi.generateReturnOfReturnAuditFile", "this.returnsApi.generateReturnOfReturnNachaFile"],
}
for path, expected_markers in service_markers.items():
    source = read(path)
    for marker in expected_markers:
        require(marker in source, f"service method/endpoint behavior changed: {marker!r} in {path}")

# Inventory must remain reproducible and exact.
inventory = read("docs/ai/navigation-menu/phases/NAV4_2D_FUNCTIONAL_ACTION_INVENTORY.md")
for marker in [
    "**Total inspeccionado: 582 definiciones de control.**",
    "**461** se caracterizaron como acciones funcionales",
    "`ALIGNED`: **289**", "`DRIFT`: **45**", "`TECHNICAL_TERM_LEGITIMATE`: **14**",
    "`CONTEXTUAL_ACTION`: **113**", "`OUT_OF_SCOPE`: **121**",
    "`CREATE` | 30", "`SAVE` | 31", "`RETRY` | 39", "`CLEAR_FILTERS` | 35", "`OTHER_FUNCTIONAL` | 191",
    "No logic delta",
]:
    require(marker in inventory, f"NAV4.2D inventory missing marker: {marker}")
require(len(re.findall(r"^\| \d+ \|", inventory, flags=re.MULTILINE)) == 627, "inventory must contain 45 drift rows + 582 complete rows")

# NAV4.2C navigation copy and NAV4.2B heading/metadata regressions must stay green.
for script, pass_marker in [
    ("scripts/validate-nav4-2c-navigation-copy.py", "PASS NAV4.2C static validation"),
    ("scripts/validate-nav4-2b-menu-backed-copy.py", "PASS NAV4.2B static validation"),
]:
    prior = subprocess.run(["python3", str(ROOT / script)], cwd=ROOT, capture_output=True, text=True)
    require(prior.returncode == 0, f"regression validator failed: {script}: {prior.stdout}\n{prior.stderr}")
    require(pass_marker in prior.stdout, f"regression validator did not report PASS: {script}")

print(f"PASS NAV4.2D static validation — {ASSERTIONS} assertions")

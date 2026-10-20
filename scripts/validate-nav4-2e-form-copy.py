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

# Strong copy-only proof: reverting exactly the documented NAV4.2E lines must recreate
# the complete NAV4.2D SPA app source tree byte-for-byte at aggregate level.
delta = json.loads(read("docs/ai/navigation-menu/phases/NAV4_2E_COPY_DELTA.json"))
require(delta.get("baseline") == "NAV4.2D", "NAV4.2E delta baseline must be NAV4.2D")
require(delta.get("files") == 12, "NAV4.2E delta must cover exactly 12 SPA production files")
require(delta.get("lines") == 36, "NAV4.2E delta must cover exactly 36 changed SPA lines")
require(delta.get("baseline_app_tree_sha256") == "4d83eac4561e9a8b4a57ce91e20980bce2f8e1a5f76c6b8cc8b6a0c8f8e1022f", "unexpected NAV4.2D app tree baseline")
changes = {item["path"]: item["entries"] for item in delta["changes"]}
require(len(changes) == 12, "NAV4.2E delta file list mismatch")
require(all(path.endswith(".html") for path in changes), "NAV4.2E production delta must be HTML copy only")


def normalize_copy_line(value: str) -> str:
    if "<" not in value and ">" not in value:
        return "<COPY_LINE>"
    value = re.sub(r'\b(?:aria-label|title|placeholder|etiqueta|ayuda)="[^"]*"', lambda m: m.group(0).split('=')[0] + '="<COPY>"', value)
    value = re.sub(r">[^<>]*<", "><COPY><", value)
    return value


def restore_copy(path: Path, entries: list[dict[str, object]]) -> bytes:
    raw_lines = path.read_bytes().splitlines(keepends=True)
    for entry in entries:
        line_no = int(entry["line"])
        require(1 <= line_no <= len(raw_lines), f"delta line outside file: {path}:{line_no}")
        raw = raw_lines[line_no - 1]
        if raw.endswith(b"\r\n"):
            ending, body = b"\r\n", raw[:-2]
        elif raw.endswith(b"\n") or raw.endswith(b"\r"):
            ending, body = raw[-1:], raw[:-1]
        else:
            ending, body = b"", raw
        current = body.decode("utf-8")
        require(current == entry["new"], f"unexpected NAV4.2E line content: {path}:{line_no}")
        require(normalize_copy_line(str(entry["old"])) == normalize_copy_line(str(entry["new"])), f"non-copy token changed at {path}:{line_no}")
        require("<mat-option" not in current and "<option" not in current, f"option label/value changed at {path}:{line_no}")
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
require(aggregate.hexdigest() == delta["baseline_app_tree_sha256"], "SPA tree differs from NAV4.2D by more than documented NAV4.2E copy lines")

# NAV4.2D actions must remain byte-identical at every line changed by NAV4.2D.
action_delta = json.loads(read("docs/ai/navigation-menu/phases/NAV4_2D_COPY_DELTA.json"))
for item in action_delta["changes"]:
    path = APP_ROOT / item["path"]
    lines = path.read_bytes().splitlines()
    for entry in item["entries"]:
        line_no = int(entry["line"])
        require(lines[line_no - 1].decode("utf-8") == entry["new"], f"NAV4.2D action copy regressed: {item['path']}:{line_no}")

# Focal final copy and eliminated field-copy drifts.
checks = {
    "features/operational-restrictions/operational-restrictions.component.html": [
        "<mat-label>Versión de política</mat-label>",
        'aria-label="Filtrar por versión de política"',
        '<mat-label>Ciclo versionado</mat-label>',
        "<mat-label>Tratamiento de tipos de cuenta</mat-label>",
        "<mat-label>Vigencia inicial</mat-label>",
        "<mat-label>Vigencia final</mat-label>",
    ],
    "features/payment-rail-capability-registry/pages/payment-rail-capability-registry-page.component.html": [
        "Capacidad (opcional)", "Fecha y hora de vigencia (UTC, opcional)"
    ],
    "features/ach-responses/pages/ach-response-list-page.component.html": [
        "<mat-label>Tipo de respuesta</mat-label>", "<mat-label>ID de transacción</mat-label>",
        "<mat-label>Estado de procesamiento</mat-label>", "<mat-label>ID de correlación</mat-label>",
        "<mat-label>Tamaño de página</mat-label>"
    ],
    "features/transactions/components/ach-return-of-return-management/ach-return-of-return-management.component.html": [
        "ID de la devolución de origen", "IDs de flujo (separados por comas)"
    ],
    "features/integrations/pages/mapping-editor-page.component.html": ["ID de transacción real", "ID de ciclo real"],
    "features/transactions/components/bulk-ingestion-upload/bulk-ingestion-upload.component.html": [
        "Si se omite, el sistema generará una referencia automática.", "ID del cliente (opcional)"
    ],
    "features/integrations/pages/soap-integration-settings.component.html": ["El sistema determina y aplica la seguridad del destino."],
    "features/incoming-nacha-command-center/pages/incoming-nacha-queue-page.component.html": ["ID de transacción ACH", "Tamaño de página"],
}
for path, markers in checks.items():
    source = read("web/ach-interbank-ui/src/app/" + path)
    for marker in markers:
        require(marker in source, f"missing NAV4.2E marker {marker!r} in {path}")

for legacy_marker in [
    "<mat-label>PolicyVersion</mat-label>", "<mat-label>ClearingHouseCycleConfig</mat-label>",
    "<mat-label>EffectiveFrom</mat-label>", "<mat-label>EffectiveTo</mat-label>",
    "<mat-label>CorrelationId</mat-label>", "<mat-label>FlowIds (separados por coma)</mat-label>",
    "<mat-label>Id transacción</mat-label>", "<mat-label>ID devolución origen</mat-label>",
]:
    require(all(legacy_marker not in read("web/ach-interbank-ui/src/app/" + p) for p in changes), f"legacy field-copy marker remains: {legacy_marker}")

# Secret fields remain exactly password/new-password or the same bound MFT secret; no secret field was added.
secret_markers = {
    "web/ach-interbank-ui/src/app/features/auth/reset-password.component.html": [
        '<input type="password" formControlName="newPassword" autocomplete="new-password" />',
        '<input type="password" formControlName="confirmPassword" autocomplete="new-password" />',
    ],
    "web/ach-interbank-ui/src/app/features/admin/components/user-form.component.html": [
        'type="password" formControlName="password" required autocomplete="new-password"'
    ],
    "web/ach-interbank-ui/src/app/features/ach-colombia-mft-administration/ach-colombia-mft-administration.component.html": [
        'type="password" [(ngModel)]="secret" [disabled]="!canManage || savingConnection" autocomplete="new-password"'
    ],
}
for path, markers in secret_markers.items():
    src = read(path)
    for marker in markers:
        require(marker in src, f"secret field contract changed: {path}: {marker}")

# Inventory must describe the exact scanner result.
inventory = read("docs/ai/navigation-menu/phases/NAV4_2E_FORM_FIELD_INVENTORY.md")
for marker in [
    "Hooks inspeccionados: **675**", "Fields físicos únicos representados: **517**",
    "`MAT_LABEL` **370**", "`HTML_LABEL` **143**", "`PLACEHOLDER` **47**", "`HINT` **75**", "`ACCESSIBLE_FIELD_NAME` **40**",
    "`ALIGNED` **355**", "`DRIFT` **36**", "`TECHNICAL_TERM_LEGITIMATE` **24**", "`DOMAIN_TERM_LEGITIMATE` **167**",
    "`CONTEXTUAL_FIELD` **58**", "`DYNAMIC_DATA` **35**", "`OUT_OF_SCOPE` **0**",
]:
    require(marker in inventory, f"inventory summary missing {marker}")

# NAV4.2C/B behavior remains valid; NAV4.2A protected hashes above remain exact.
for script, pass_marker in [
    ("scripts/validate-nav4-2c-navigation-copy.py", "PASS NAV4.2C static validation"),
    ("scripts/validate-nav4-2b-menu-backed-copy.py", "PASS NAV4.2B static validation"),
]:
    prior = subprocess.run(["python3", str(ROOT / script)], cwd=ROOT, capture_output=True, text=True)
    require(prior.returncode == 0, f"regression validator failed: {script}: {prior.stdout}\n{prior.stderr}")
    require(pass_marker in prior.stdout, f"regression validator did not report PASS: {script}")

print(f"PASS NAV4.2E static validation — {ASSERTIONS} assertions")

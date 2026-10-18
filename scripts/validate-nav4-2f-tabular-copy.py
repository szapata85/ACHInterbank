#!/usr/bin/env python3
from __future__ import annotations

import hashlib
import json
import re
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


# Frozen navigation/security surfaces from prior phases.
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

# Strong copy-only proof. Restoring only the documented lines must reconstruct NAV4.2E src/app byte-for-byte.
delta = json.loads(read("docs/ai/navigation-menu/phases/NAV4_2F_COPY_DELTA.json"))
require(delta.get("baseline") == "NAV4.2E", "NAV4.2F delta baseline must be NAV4.2E")
require(delta.get("baseline_app_tree_sha256") == "62230bdde11661f6d55559e6077972a65646578c9ae9c015002d7f563291e3cb", "unexpected NAV4.2E app tree baseline")
require(delta.get("files") == 26, "NAV4.2F delta must cover exactly 26 production files")
require(delta.get("lines") == 108, "NAV4.2F delta must cover exactly 108 changed production lines")
require(delta.get("tabular_hooks_changed") == 112, "NAV4.2F must document exactly 112 corrected tabular hooks")
changes = {item["path"]: item["entries"] for item in delta["changes"]}
require(len(changes) == 26, "NAV4.2F delta file list mismatch")
require(all(path.endswith((".html", ".ts")) for path in changes), "NAV4.2F production delta may only touch HTML/TS copy")


def normalize_allowed_copy(line: str) -> str:
    # TS display-only column labels.
    line = re.sub(r"(\bheaderName\s*:\s*)(['\"])(.*?)\2", r"\1'<COPY>'", line)
    line = re.sub(r"(\blabel\s*:\s*)(['\"])(.*?)\2", r"\1'<COPY>'", line)
    # Plain/template header text.
    line = re.sub(r"(<th\b[^>]*>)(.*?)(</th>)", r"\1<COPY>\3", line)
    # Direct tabular accessible names.
    line = re.sub(r'(\baria-label\s*=\s*)"[^"]*"', r'\1"<COPY>"', line)
    # Cell accessible prefix; preserve the bound expression after +.
    line = re.sub(r"(\[attr\.aria-label\]\s*=\s*\")'[^']*'(\s*\+[^\"]+\")", r"\1'<COPY>'\2", line)
    return line


def restore_copy(path: Path, entries: list[dict[str, object]]) -> bytes:
    raw_lines = path.read_bytes().splitlines(keepends=True)
    for entry in entries:
        n = int(entry["line"])
        require(1 <= n <= len(raw_lines), f"delta line outside file: {path}:{n}")
        raw = raw_lines[n - 1]
        if raw.endswith(b"\r\n"):
            ending, body = b"\r\n", raw[:-2]
        elif raw.endswith(b"\n") or raw.endswith(b"\r"):
            ending, body = raw[-1:], raw[:-1]
        else:
            ending, body = b"", raw
        current = body.decode("utf-8")
        require(current == entry["new"], f"unexpected NAV4.2F line content: {path}:{n}")
        require(normalize_allowed_copy(str(entry["old"])) == normalize_allowed_copy(str(entry["new"])), f"non-copy token changed: {path}:{n}")
        raw_lines[n - 1] = str(entry["old"]).encode("utf-8") + ending
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
require(aggregate.hexdigest() == delta["baseline_app_tree_sha256"], "SPA tree differs from NAV4.2E by more than documented NAV4.2F copy lines")

# Earlier copy increments remain present on their exact final lines.
for prior in ["NAV4_2D_COPY_DELTA.json", "NAV4_2E_COPY_DELTA.json"]:
    prior_delta = json.loads(read("docs/ai/navigation-menu/phases/" + prior))
    for item in prior_delta["changes"]:
        path = APP_ROOT / item["path"]
        lines = path.read_bytes().splitlines()
        for entry in item["entries"]:
            n = int(entry["line"])
            require(lines[n - 1].decode("utf-8") == entry["new"], f"prior copy regressed ({prior}): {item['path']}:{n}")

# Inventory is exact and records all required categories.
inv = read("docs/ai/navigation-menu/phases/NAV4_2F_TABULAR_COPY_INVENTORY.md")
for marker in [
    "Hooks tabulares inspeccionados: **630**",
    "Columnas únicas estáticamente representadas: **573**",
    "`HTML_TH` **44**", "`MATERIAL_HEADER` **206**", "`GRID_HEADER_NAME` **250**",
    "`COLUMN_DISPLAY_LABEL` **86**", "`SORT_ACCESSIBLE_NAME` **0**", "`TABLE_ACCESSIBLE_NAME` **16**",
    "`PAGINATION_ACCESSIBLE_COPY` **25**", "`OTHER_TABULAR_COPY` **3**",
    "`ALIGNED` **243**", "`DRIFT` **112**", "`TECHNICAL_TERM_LEGITIMATE` **8**",
    "`DOMAIN_TERM_LEGITIMATE` **54**", "`CONTEXTUAL_COLUMN` **210**", "`DYNAMIC_DATA` **3**", "`OUT_OF_SCOPE` **0**",
    "Drift residual tabular identificado después del incremento: **0**",
]:
    require(marker in inv, f"missing NAV4.2F inventory marker: {marker}")

# Dynamic header sources stay dynamic; no hardcoded replacement catalog was introduced.
require("headerName: column.label" in read("web/ach-interbank-ui/src/app/shared/components/table.component.ts"), "shared dynamic table header changed")
require("headerName: header" in read("web/ach-interbank-ui/src/app/features/cenit/components/cenit-regulatory-page.component.ts"), "CENIT regulatory dynamic headers changed")
require("headerName: header" in read("web/ach-interbank-ui/src/app/features/cenit/components/cenit-operation-page.component.ts"), "CENIT operation dynamic headers changed")

# Export coupling: no AG Grid CSV/Excel export uses these display headers. Report PDFs use backend APIs, not config.columns.
spa = "\n".join(p.read_text(encoding="utf-8", errors="ignore") for p in APP_ROOT.rglob("*.ts"))
require(not re.search(r"exportDataAs(?:Csv|Excel)|getDataAs(?:Csv|Excel)", spa), "grid header export coupling introduced")
report = read("web/ach-interbank-ui/src/app/features/reports/components/report-list-page.component.ts")
require("downloadSentTransactionsPdf" in report and "downloadAuditPdf" in report and "config.columns" not in report[report.index("private requestPdf"):report.index("private toNumber")], "report PDF contract unexpectedly coupled to display columns")

# Focal final copy examples and technical precision.
checks = {
    "features/transactions/components/transaction-list/transaction-list.component.ts": ["ID de operación", "Referencia heredada", "Gestión de devolución", "Cuenta de origen", "Cuenta de destino", "Institución de destino"],
    "features/nacha-operational/pages/ach-reconciliation-console.component.ts": ["ID de conciliación", "ID de correlación", "Número de rastreo", "Operación SOAP candidata"],
    "features/ach-responses/pages/ach-response-status-mappings-page.component.ts": ["ID del estado interno", "ID del estado del servicio externo", "Vigencia inicial", "Vigencia final"],
    "features/operational-restrictions/operational-restrictions.component.html": ["Restricciones operativas", "Paginación de restricciones operativas"],
    "features/navigation-logs/components/navigation-log.component.ts": ["User-Agent"],
    "features/transactions/components/ach-returns-management/ach-returns-management.component.ts": ["DFI de origen", "DFI de destino", "Código de transacción"],
}
for rel, markers in checks.items():
    source = read("web/ach-interbank-ui/src/app/" + rel)
    for marker in markers:
        require(marker in source, f"missing final tabular copy {marker!r}: {rel}")

# Structural counts did not change: same table/header mechanisms remain in place.
html = "\n".join(p.read_text(encoding="utf-8", errors="ignore") for p in APP_ROOT.rglob("*.html"))
ts = "\n".join(p.read_text(encoding="utf-8", errors="ignore") for p in APP_ROOT.rglob("*.ts"))
require(len(re.findall(r"<th\b", html, re.I)) == 250, "HTML table header hook count changed")
require(len(re.findall(r"\bheaderName\s*:", ts)) == 250, "grid headerName hook count changed")
require(len(re.findall(r"<mat-paginator\b", html, re.I)) == 12, "paginator count changed")

print(f"NAV4.2F validator PASS — {ASSERTIONS} assertions")

#!/usr/bin/env python3
from __future__ import annotations

import hashlib
import json
import re
from collections import defaultdict
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
APP_ROOT = ROOT / "web/ach-interbank-ui/src/app"
DELTA_PATH = ROOT / "docs/ai/navigation-menu/phases/NAV4_2G_COPY_DELTA.json"
BASELINE_SHA = "e260fe85e533bf326538f201343bf9b11a74236d62aa007628450121a86daa16"
ASSERTIONS = 0


def require(condition: bool, message: str) -> None:
    global ASSERTIONS
    ASSERTIONS += 1
    if not condition:
        raise AssertionError(message)


def sha(path: Path) -> str:
    return hashlib.sha256(path.read_bytes()).hexdigest()


def tree_hash(files: dict[str, bytes] | None = None) -> str:
    aggregate = hashlib.sha256()
    for path in sorted(p for p in APP_ROOT.rglob("*") if p.is_file()):
        rel = path.relative_to(APP_ROOT).as_posix()
        data = files[rel] if files is not None and rel in files else path.read_bytes()
        rel_bytes = rel.encode("utf-8")
        aggregate.update(len(rel_bytes).to_bytes(4, "big"))
        aggregate.update(rel_bytes)
        aggregate.update(len(data).to_bytes(8, "big"))
        aggregate.update(data)
    return aggregate.hexdigest()


def text_from_bytes(data: bytes) -> str:
    return data.decode("utf-8").replace("\r\n", "\n").replace("\r", "\n")


def normalized_extract(tree: dict[str, bytes], patterns: list[re.Pattern[str]]) -> list[str]:
    out: list[str] = []
    for rel in sorted(tree):
        if not rel.endswith((".ts", ".html")):
            continue
        text = text_from_bytes(tree[rel])
        for pattern in patterns:
            for match in pattern.finditer(text):
                out.append(f"{rel}:{match.group(0)}")
    return out


# Frozen navigation/security/seed surfaces from the already-approved prior phases.
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
for rel, expected in protected.items():
    require(sha(ROOT / rel) == expected, f"protected navigation/security/seed file changed: {rel}")

fine = (ROOT / "src/Cfa.ACHInterbank.Persistence/Security/Seeders/FineGrainedGrantBackfillSeeder.cs").read_text(encoding="utf-8")
legacy = (ROOT / "src/Cfa.ACHInterbank.Persistence/Security/Seeders/LegacyFineGrainedRoleMigrationSeeder.cs").read_text(encoding="utf-8")
require("BackfillOrder = 119" in fine, "FineGrainedGrantBackfillSeeder order changed")
require("MigrationOrder = 120" in legacy, "LegacyFineGrainedRoleMigrationSeeder order changed")

# Load and validate delta contract.
delta = json.loads(DELTA_PATH.read_text(encoding="utf-8"))
require(delta.get("phase") == "NAV4.2G", "wrong phase in NAV4.2G delta")
require(delta.get("baseline") == "NAV4.2F", "NAV4.2G must use NAV4.2F baseline")
require(delta.get("baseline_src_app_sha256") == BASELINE_SHA, "unexpected NAV4.2F src/app baseline hash")
changes = delta.get("changes")
require(isinstance(changes, list) and len(changes) == 50, "NAV4.2G delta must contain exactly 50 productive changed lines")
require(len({c["file"] for c in changes}) == 27, "NAV4.2G delta must cover exactly 27 productive files")
allowed_mechanisms = {
    "EMPTY_STATE", "GRID_EMPTY_STATE", "UI_ALERT", "SNACKBAR", "TOAST", "NOTIFICATION",
    "INLINE_STATUS", "INFO_BANNER", "SUCCESS_MESSAGE", "WARNING_MESSAGE", "ERROR_MESSAGE",
    "LOADING_MESSAGE", "OTHER_FEEDBACK",
}
allowed_origins = {"SPA_AUTHORED", "SPA_COMPOSED", "HYBRID"}
for item in changes:
    rel = item["file"]
    require(rel.startswith("web/ach-interbank-ui/src/app/"), f"productive delta outside src/app: {rel}")
    require(rel.endswith((".html", ".ts")) and not rel.endswith(".spec.ts"), f"non-production copy file in delta: {rel}")
    require(item["mechanism"] in allowed_mechanisms, f"unexpected mechanism in delta: {item['mechanism']}")
    require(item["origin"] in allowed_origins, f"backend/external passthrough must never be changed: {rel}:{item['line']}")
    require(item["classification"] == "DRIFT", f"delta contains non-drift item: {rel}:{item['line']}")
    require(not re.search(r"\b(BACKEND_PASSTHROUGH|EXTERNAL_PASSTHROUGH)\b", item["origin"]), f"passthrough modified: {rel}:{item['line']}")
    require(re.findall(r"\$\{[^}]+\}", item["old"]) == re.findall(r"\$\{[^}]+\}", item["new"]), f"dynamic interpolation changed: {rel}:{item['line']}")

# Reconstruct NAV4.2F in-memory by reversing only the declared copy delta.
current_tree = {p.relative_to(APP_ROOT).as_posix(): p.read_bytes() for p in APP_ROOT.rglob("*") if p.is_file()}
restored_tree = dict(current_tree)
by_file: dict[str, list[dict[str, object]]] = defaultdict(list)
for item in changes:
    by_file[item["file"].split("web/ach-interbank-ui/src/app/", 1)[1]].append(item)

for rel, entries in by_file.items():
    raw_lines = restored_tree[rel].splitlines(keepends=True)
    current_lines = current_tree[rel].splitlines(keepends=True)
    for entry in sorted(entries, key=lambda e: int(e["line"]), reverse=True):
        n = int(entry["line"])
        require(1 <= n <= len(raw_lines), f"delta line outside file: {rel}:{n}")
        raw = raw_lines[n - 1]
        if raw.endswith(b"\r\n"):
            ending, body = b"\r\n", raw[:-2]
        elif raw.endswith((b"\n", b"\r")):
            ending, body = raw[-1:], raw[:-1]
        else:
            ending, body = b"", raw
        line = body.decode("utf-8")
        new = str(entry["new"])
        old = str(entry["old"])
        require(line.count(new) == 1, f"documented new copy missing/ambiguous: {rel}:{n}: {new!r}")
        # Changed line must differ only in the declared copy segment. This is the NO LOGIC DELTA proof.
        baseline_line = line.replace(new, old, 1)
        current_normalized = line.replace(new, "<NAV4.2G_COPY>", 1)
        baseline_normalized = baseline_line.replace(old, "<NAV4.2G_COPY>", 1)
        require(current_normalized == baseline_normalized, f"non-copy token changed: {rel}:{n}")
        raw_lines[n - 1] = baseline_line.encode("utf-8") + ending
    restored_tree[rel] = b"".join(raw_lines)
    require(len(restored_tree[rel].splitlines()) == len(current_tree[rel].splitlines()), f"line count changed: {rel}")

# The exact NAV4.2F src/app bytes must be recoverable solely by reversing NAV4.2G delta.
aggregate = hashlib.sha256()
for path in sorted(p for p in APP_ROOT.rglob("*") if p.is_file()):
    rel = path.relative_to(APP_ROOT).as_posix()
    data = restored_tree[rel]
    rb = rel.encode("utf-8")
    aggregate.update(len(rb).to_bytes(4, "big")); aggregate.update(rb)
    aggregate.update(len(data).to_bytes(8, "big")); aggregate.update(data)
require(aggregate.hexdigest() == BASELINE_SHA, "reversing NAV4.2G delta does not reconstruct NAV4.2F src/app")
require(tree_hash() != BASELINE_SHA, "NAV4.2G productive delta unexpectedly empty")

# Out-of-scope display/logic surfaces must be byte-equivalent between current G and reconstructed F.
# Extract semantic snippets rather than whole lines so a feedback string sharing a line cannot create a false positive.
out_of_scope_patterns = {
    "mat-error": [re.compile(r"<mat-error\b.*?</mat-error>", re.I | re.S)],
    "validators": [
        re.compile(r"\bValidators\.[A-Za-z_$][\w$]*(?:\([^;\n]*?\))?"),
        re.compile(r"\b(?:setErrors|hasError|getError)\s*\([^\n;]*?\)"),
    ],
    "confirmations": [
        re.compile(r"\b(?:window\.)?confirm\s*\((?:[^()]|\([^()]*\))*\)", re.S),
        re.compile(r"\bprompt\s*\((?:[^()]|\([^()]*\))*\)", re.S),
    ],
    "fields_forms_NAV4_2E": [
        re.compile(r"<mat-label\b.*?</mat-label>", re.I | re.S),
        re.compile(r"<mat-hint\b.*?</mat-hint>", re.I | re.S),
        re.compile(r"\bplaceholder\s*=\s*(?:\"[^\"]*\"|'[^']*')", re.I),
        re.compile(r"\bformControlName\s*=\s*(?:\"[^\"]*\"|'[^']*')", re.I),
    ],
    "actions_NAV4_2D": [
        re.compile(r"<button\b[^>]*>.*?</button>", re.I | re.S),
        re.compile(r"\(click\)\s*=\s*(?:\"[^\"]*\"|'[^']*')", re.I),
    ],
    "tabular_NAV4_2F": [
        re.compile(r"<th\b.*?</th>", re.I | re.S),
        re.compile(r"<[^>]*mat-header-cell[^>]*>.*?</[^>]+>", re.I | re.S),
        re.compile(r"\bheaderName\s*:\s*(?:'[^']*'|\"[^\"]*\"|`[^`]*`)", re.S),
        re.compile(r"\bdisplayedColumns\s*=\s*\[[^\]]*\]", re.S),
        re.compile(r"<mat-paginator\b[^>]*>", re.I | re.S),
    ],
}
for label, patterns in out_of_scope_patterns.items():
    require(normalized_extract(current_tree, patterns) == normalized_extract(restored_tree, patterns), f"out-of-scope surface changed: {label}")

# Backend/ProblemDetails/body passthrough and external causal/code fragments remain byte-equivalent.
passthrough_patterns = [
    re.compile(r"^.*(?:error\??\.error|err\??\.error|response\??\.error|result\??\.(?:message|mensaje)|\.detail\b|\.title\b|\.errors\b|ProblemDetails|ValidationProblemDetails|body\??\.(?:message|detail|title|errors)).*$", re.I | re.M),
]
require(normalized_extract(current_tree, passthrough_patterns) == normalized_extract(restored_tree, passthrough_patterns), "backend/body/ProblemDetails passthrough changed")

for rel in sorted(current_tree):
    if not rel.endswith((".ts", ".html")): continue
    cur = text_from_bytes(current_tree[rel]); old = text_from_bytes(restored_tree[rel])
    require(re.findall(r"\b[RD]\d{2}\b", cur) == re.findall(r"\b[RD]\d{2}\b", old), f"Rxx/Dxx causal/code changed: {rel}")

# Confirm known passthrough shapes are still present. These assertions explicitly cover body string,
# ProblemDetails detail/title and ValidationProblemDetails errors when the source exposes them.
spa = "\n".join(text_from_bytes(data) for rel, data in current_tree.items() if rel.endswith((".ts", ".html")))
require(".detail" in spa, "ProblemDetails detail passthrough hooks disappeared")
require(".title" in spa, "ProblemDetails title passthrough hooks disappeared")
require(".errors" in spa, "ValidationProblemDetails errors passthrough hooks disappeared")
require(re.search(r"error\??\.error", spa) is not None, "backend error body passthrough hooks disappeared")

# Semantics singled out by NAV4.2G scope.
def read_app(rel: str) -> str:
    return (APP_ROOT / rel).read_text(encoding="utf-8")

file_exchange = read_app("features/ach-colombia-file-exchange/ach-colombia-file-exchange.component.ts")
require("La ejecución de envío terminó. Revisa el estado de las transferencias." in file_exchange, "outbound status copy missing")
require("La ejecución de recepción terminó. Revisa el estado de los archivos." in file_exchange, "inbound status copy missing")
require("Publicado en la frontera CFA. La recepción por ACH Colombia no está confirmada." in file_exchange, "external reception caveat regressed")

mft = read_app("features/ach-colombia-mft-administration/ach-colombia-mft-administration.component.ts")
require("Conexión exitosa." in mft, "real MFT test-connection success message changed")
require("Configuración guardada." in mft, "MFT save message changed")

bulk_root = APP_ROOT / "features/transactions/components"
bulk_upload = read_app("features/transactions/components/bulk-ingestion-upload/bulk-ingestion-upload.component.ts")
bulk_detail = "\n".join(p.read_text(encoding="utf-8") for p in bulk_root.glob("bulk-ingestion-*/*.ts") if not p.name.endswith(".spec.ts"))
require("registrado" in bulk_upload.lower() or "registrado" in bulk_detail.lower(), "bulk batch-created semantics disappeared")
require("Reintento" in bulk_detail and ("solicit" in bulk_detail or "registr" in bulk_detail), "bulk retry-request semantics disappeared")

nacha_upload = read_app("features/transactions/components/nacha-upload/nacha-upload.component.ts")
require("result.message || 'No fue posible cargar el archivo NACHA-M.'" in nacha_upload or "result.message ?? 'No fue posible cargar el archivo NACHA-M.'" in nacha_upload, "NACHA upload backend message/fallback contract changed")
require("La carga actual no está habilitada para reproceso controlado." in nacha_upload, "NACHA reprocess feedback missing")

ror = read_app("features/transactions/components/ach-return-of-return-management/ach-return-of-return-management.component.ts")
require("generad" in ror.lower(), "ROR local-generation wording disappeared")
# Do not claim external transmission for the local-generation success path.
for line in ror.splitlines():
    if "notifications.success" in line and "archivo" in line.lower():
        require("transmit" not in line.lower() and "enviad" not in line.lower(), "ROR success falsely claims external transmission")

envelope = read_app("features/nacha-security/components/digital-envelope-tool.component.ts")
require("Archivo cifrado: ${saved.fileName}" in envelope, "encrypt success must say file encrypted")
require("Archivo descifrado: ${saved.fileName}" in envelope, "decrypt success must say file decrypted")

soap = read_app("features/integrations/pages/soap-integration-settings.component.ts")
require("preview" in soap.lower(), "integration preview path disappeared")
# Copy-only proof already guarantees no SOAP call/handler change; ensure no preview success copy claims a test.
for line in soap.splitlines():
    if "notifications.success" in line and "preview" in line.lower():
        require("conex" not in line.lower() and "soap ejecut" not in line.lower(), "preview-change falsely claims SOAP/connectivity execution")

# Inventory and durable decisions are present; NAV4.2H remains unexecuted.
inv = (ROOT / "docs/ai/navigation-menu/phases/NAV4_2G_MESSAGES_EMPTY_STATES_INVENTORY.md").read_text(encoding="utf-8")
for marker in [
    "DEC-NAV-4.2G-001", "DEC-NAV-4.2G-002", "DEC-NAV-4.2G-003",
    "BACKEND_PASSTHROUGH", "EXTERNAL_PASSTHROUGH", "DYNAMIC_DATA",
    "NAV4.2H — mensajes de validación", "NAV4.2H no fue ejecutado",
]:
    require(marker in inv, f"inventory marker missing: {marker}")

print(f"NAV4.2G validator PASS — {ASSERTIONS} assertions; delta={len(changes)} lines/{len(by_file)} files; baseline={BASELINE_SHA}")

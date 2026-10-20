#!/usr/bin/env python3
from __future__ import annotations

import hashlib
import json
import re
from collections import Counter
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
APP = ROOT / 'web/ach-interbank-ui/src/app'
DELTA = ROOT / 'docs/ai/navigation-menu/phases/NAV4_2J_COPY_DELTA.json'
INVENTORY = ROOT / 'docs/ai/navigation-menu/phases/NAV4_2J_TOOLTIP_ARIA_COPY_INVENTORY.md'
BASELINE_SHA = '5885f1a7c83c3c979d1a5909555ab372aa29d7b01dba1472547e5d933273b77f'
EXPECTED_LINES = [1,142,142,184,184,252,42,129,146,146,199,142,20,29,44,6,19,114,116,171,181,259]
ASSERTIONS = 0


def require(condition: bool, message: str) -> None:
    global ASSERTIONS
    ASSERTIONS += 1
    if not condition:
        raise AssertionError(message)


def sha(path: Path) -> str:
    return hashlib.sha256(path.read_bytes()).hexdigest()


def text(data: bytes) -> str:
    return data.decode('utf-8').replace('\r\n', '\n').replace('\r', '\n')


def tree_hash(tree: dict[str, bytes] | None = None) -> str:
    h = hashlib.sha256()
    paths = sorted(p for p in APP.rglob('*') if p.is_file())
    for path in paths:
        rel = path.relative_to(APP).as_posix()
        data = tree[rel] if tree is not None else path.read_bytes()
        rb = rel.encode('utf-8')
        h.update(len(rb).to_bytes(4, 'big'))
        h.update(rb)
        h.update(len(data).to_bytes(8, 'big'))
        h.update(data)
    return h.hexdigest()


def occurrences(tree: dict[str, bytes], pattern: str) -> list[tuple[str, str]]:
    rx = re.compile(pattern, re.M | re.S)
    out: list[tuple[str, str]] = []
    for rel in sorted(tree):
        if rel.endswith(('.ts', '.html')) and not rel.endswith('.spec.ts'):
            out.extend((rel, m.group(0)) for m in rx.finditer(text(tree[rel])))
    return out


def capture_group(tree: dict[str, bytes], pattern: str, group: int = 1) -> list[tuple[str, str]]:
    rx = re.compile(pattern, re.M | re.S)
    out: list[tuple[str, str]] = []
    for rel in sorted(tree):
        if rel.endswith(('.ts', '.html')) and not rel.endswith('.spec.ts'):
            out.extend((rel, m.group(group)) for m in rx.finditer(text(tree[rel])))
    return out


def replace_accessible_copy_line(data: bytes, mechanism: str, new: str, old: str, rel: str, line_no: int) -> bytes:
    """Reverse one documented hook on its audited source line without normalizing line endings."""
    raw = data.splitlines(keepends=True)
    require(1 <= line_no <= len(raw), f'delta line outside file: {rel}:{line_no}')
    line = raw[line_no - 1]
    nb = new.encode('utf-8')
    ob = old.encode('utf-8')
    if mechanism == 'ARIA_LABEL':
        options = [
            (b'aria-label="' + nb + b'"', b'aria-label="' + ob + b'"'),
            (b'[attr.aria-label]="' + nb + b'"', b'[attr.aria-label]="' + ob + b'"'),
        ]
    elif mechanism == 'MAT_TOOLTIP':
        options = [
            (b'matTooltip="' + nb + b'"', b'matTooltip="' + ob + b'"'),
            (b'[matTooltip]="' + nb + b'"', b'[matTooltip]="' + ob + b'"'),
        ]
    elif mechanism == 'SR_ONLY':
        options = [(b'>' + nb + b'<', b'>' + ob + b'<')]
    else:
        raise AssertionError(f'unsupported NAV4.2J mechanism: {mechanism}')
    found = [(a, b) for a, b in options if line.count(a)]
    require(sum(line.count(a) for a, _ in found) == 1, f'documented new copy missing/ambiguous: {rel}:{line_no}: {new!r}')
    a, b = found[0]
    raw[line_no - 1] = line.replace(a, b, 1)
    return b''.join(raw)


# Security/navigation/seed invariants inherited from NAV4.2I.
protected = {
    'web/ach-interbank-ui/src/app/core/services/navigation.service.ts': '4df488ac45465a5283a02afe795fc64c4e5995a3415556d324275cc5e1d7ea93',
    'web/ach-interbank-ui/src/app/core/services/menu-route-surface.service.ts': '3a32dc7921e3c805e32fa9921e371109813aae9ba3a3173bda6a5b6b7030df72',
    'web/ach-interbank-ui/src/app/core/services/navigation-naming.service.ts': 'de6d4a402690956914a67f26466c7ea6c1cfac16eb717dbeb4d4b6f56e59db19',
    'web/ach-interbank-ui/src/app/core/services/navigation-naming.util.ts': '17e066e755f589ee818b6fa48fbcf3963ba14886a35cdf3187e5381e80368895',
    'src/Cfa.ACHInterbank.Application/Navigation/Queries/GetMenuForCurrentUserHandler.cs': 'f111e416c181fc1912c297e37012cf7462f49d0434c1ef6ec9416c89b5ed1a61',
    'src/Cfa.ACHInterbank.Persistence/Navigation/Seeders/NavigationInformationArchitectureSeeder.cs': '28371549f4b9150fdf738bee11454c98117ba420d1abae0e9971e2c49aee9f4c',
    'src/Cfa.ACHInterbank.Persistence/Security/Seeders/FineGrainedGrantBackfillSeeder.cs': 'e4f2b80b0b9e9c3aff636fb03e96b819019c07eeabc549bcfe4f789756775437',
    'src/Cfa.ACHInterbank.Persistence/Security/Seeders/LegacyFineGrainedRoleMigrationSeeder.cs': 'e2366e75faab4ee31498d0cb84a93899a2a928698b01e4401a38ab1cc9c18105',
    'web/ach-interbank-ui/src/app/core/guards/permission.guard.ts': '7d9541b3899543ad40802b18cb4e734a9c14e59077b732108b31396213afa73a',
    'web/ach-interbank-ui/src/app/core/guards/role.guard.ts': '520fcea2357e5cfe2b19d3ad1d0b284ee84d21149b359025126308210d6de31e',
    # Accessible navigation surfaces are explicitly out of NAV4.2J scope.
    'web/ach-interbank-ui/src/app/layout/main-layout.component.html': '166df4222c0cfdb99c547530b75cbb1db923e31bf91eac3c3992dbbb6c5226d4',
    'web/ach-interbank-ui/src/app/shared/components/ui/ui-migas-pan.component.ts': '48b667dcfe2c6ce5e5ded603752df9f2fc4cad7f0a1ab09540568fdd283de2c6',
    'web/ach-interbank-ui/src/app/shared/components/clearing-house-context-navigation.component.ts': '4d60c9740dcf164e33f323543e9f771e58aea3aac95af1a2b1441a6136ffc4c3',
    'web/ach-interbank-ui/src/app/features/navigation/components/navigation-menu.component.html': '25056104d925afd827df25eed0ab92e7c4815f9078961e8d6969dfe2546c2831',
}
for rel, expected in protected.items():
    require((ROOT / rel).is_file(), f'protected file missing: {rel}')
    require(sha(ROOT / rel) == expected, f'protected navigation/security/seed changed: {rel}')

# Delta contract.
delta = json.loads(DELTA.read_text(encoding='utf-8'))
changes = delta.get('changes', [])
require(delta.get('phase') == 'NAV4.2J', 'wrong phase')
require(delta.get('baseline') == 'NAV4.2I', 'wrong baseline')
require(delta.get('baseline_src_app_sha256') == BASELINE_SHA, 'wrong NAV4.2I baseline hash')
require(len(changes) == 22, 'NAV4.2J delta must contain exactly 22 productive copy changes')
require(len(EXPECTED_LINES) == len(changes), 'NAV4.2J audited line map mismatch')
require(len({c['file'] for c in changes}) == 11, 'NAV4.2J delta must cover exactly 11 productive files')
allowed = {'file', 'old', 'new', 'mechanism', 'control_context'}
for c in changes:
    require(set(c) == allowed, f'unexpected delta fields: {set(c) - allowed}')
    require(c['file'].startswith('web/ach-interbank-ui/src/app/'), 'delta outside src/app')
    require(c['file'].endswith(('.html', '.ts')) and not c['file'].endswith('.spec.ts'), 'non-production delta file')
    require(c['mechanism'] in {'ARIA_LABEL', 'MAT_TOOLTIP', 'SR_ONLY'}, 'non-accessible-copy delta mechanism')
    require(c['old'] != c['new'], 'delta entry is not a real change')
    # Dynamic fragments are invariants in composed copy.
    require(
        re.findall(r'\$\{[^}]+\}|\{\{[^}]+\}\}|\brow\.[A-Za-z_$][\w$]*|\b[A-Za-z_$][\w$]*\([^\n)]*\)', str(c['old']))
        == re.findall(r'\$\{[^}]+\}|\{\{[^}]+\}\}|\brow\.[A-Za-z_$][\w$]*|\b[A-Za-z_$][\w$]*\([^\n)]*\)', str(c['new'])),
        f'dynamic data/expression changed: {c["file"]}',
    )

# Reverse only documented accessible copy and require an exact NAV4.2I src/app reconstruction.
current = {p.relative_to(APP).as_posix(): p.read_bytes() for p in APP.rglob('*') if p.is_file()}
restored = dict(current)
for c, line_no in zip(changes, EXPECTED_LINES):
    rel = c['file'].split('web/ach-interbank-ui/src/app/', 1)[1]
    require(rel in restored, f'delta file missing from src/app: {rel}')
    restored[rel] = replace_accessible_copy_line(
        restored[rel], c['mechanism'], str(c['new']), str(c['old']), rel, line_no
    )

require(tree_hash(restored) == BASELINE_SHA, 'reversing NAV4.2J delta does not reconstruct exact NAV4.2I src/app')

# The exact source replay proves there is no undeclared productive delta. The following targeted
# comparisons make the user-requested invariants explicit and easier to diagnose.
invariant_patterns = {
    'handlers/callbacks': r'\((?:click|change|submit|keydown|keyup|keypress|focus|blur)\)\s*=\s*"[^"]*"|\.addEventListener\s*\([^)]*\)|\.afterClosed\(\)|\.subscribe\s*\(',
    'visibility/conditions': r'\*ngIf\s*=\s*"[^"]*"|@if\s*\([^)]*\)|\[hidden\]\s*=\s*"[^"]*"|\*ngFor\s*=\s*"[^"]*"',
    'disabled/readOnly': r'\[(?:disabled|readonly|readOnly)\]\s*=\s*"[^"]*"|\b(?:disabled|readonly)\b(?=\s|>|/)',
    'roles': r'\brole\s*=\s*["\'][^"\']*["\']|\[attr\.role\]\s*=\s*"[^"]*"',
    'tabindex': r'\btabindex\s*=\s*["\'][^"\']*["\']|\[attr\.tabindex\]\s*=\s*"[^"]*"',
    'aria states': r'(?:\[attr\.)?aria-(?:expanded|selected|checked|disabled|live|busy|current|modal)(?:\])?\s*=\s*["\'][^"\']*["\']',
    'aria relationships': r'(?:\[attr\.)?aria-(?:labelledby|describedby)(?:\])?\s*=\s*["\'][^"\']*["\']',
    'ids': r'\bid\s*=\s*["\'][^"\']*["\']|\[id\]\s*=\s*"[^"]*"',
    'tooltip config': r'\bmatTooltip(?:Position|ShowDelay|HideDelay|Disabled)\s*=\s*["\'][^"\']*["\']|\[matTooltip(?:Position|ShowDelay|HideDelay|Disabled)\]\s*=\s*"[^"]*"',
    'router/navigation': r'\brouterLink\s*=\s*"[^"]*"|\[routerLink\]\s*=\s*"[^"]*"|\.navigate(?:ByUrl)?\s*\(',
    'API/service calls': r'\b(?:api|service|this\.api|this\.service|[A-Za-z_$][\w$]*(?:Api|Service))\.[A-Za-z_$][\w$]*\s*\(',
    'validators/regex': r'Validators\.[A-Za-z_$][\w$]*(?:\([^)]*\))?|hasError\([^)]*\)|\.setValidators\([^)]*\)|/[^/\n]+/[gimsuy]*',
    'error keys/bindings': r'formControlName\s*=\s*"[^"]*"|\[formControl\]\s*=\s*"[^"]*"|<mat-error\b[^>]*>|app-error-message',
}
for label, pattern in invariant_patterns.items():
    require(occurrences(current, pattern) == occurrences(restored, pattern), f'{label} changed')

# Visible action copy is outside NAV4.2J. Opening attributes may contain the allowed ARIA copy,
# so compare only the rendered inner content plus common action-label configuration.
for pattern in [r'<button\b[^>]*>(.*?)</button>', r'<ui-boton\b[^>]*>(.*?)</ui-boton>']:
    require(capture_group(current, pattern) == capture_group(restored, pattern), f'visible button/action copy changed: {pattern}')
pattern = r'\b(?:confirmLabel|cancelLabel|confirmText|cancelText|textoConfirmar)\s*:\s*[^,}\n]+'
require(occurrences(current, pattern) == occurrences(restored, pattern), 'confirmation button config changed')

# Backend/external passthrough and domain codes remain invariant.
for pattern in [
    r'ProblemDetails', r'ValidationProblemDetails', r'error\.message', r'response\.error', r'\.errors\b',
    r'failureReasonDisplay', r'statusHelp', r'actionUnavailableReason\([^)]*\)', r'\bR\d{2}\b', r'\bD\d{2}\b',
]:
    require(occurrences(current, pattern) == occurrences(restored, pattern), f'backend/external passthrough or code changed: {pattern}')

# Previous phase deltas must remain present. Exact reconstruction to the trusted I tree is the primary
# proof; these checks add direct regression guards for the immediately preceding copy increments.
for phase in ['I', 'H', 'G']:
    p = ROOT / f'docs/ai/navigation-menu/phases/NAV4_2{phase}_COPY_DELTA.json'
    require(p.is_file(), f'previous delta missing: NAV4.2{phase}')
    obj = json.loads(p.read_text(encoding='utf-8'))
    for entry in obj.get('changes', []):
        if 'file' not in entry or 'new' not in entry:
            continue
        file_value = str(entry['file'])
        if not file_value.startswith('web/ach-interbank-ui/src/app/'):
            continue
        rel = file_value.split('web/ach-interbank-ui/src/app/', 1)[1]
        if rel not in current:
            continue
        # J may alter a different accessible string on a line containing a previous-phase string; presence is sufficient.
        require(str(entry['new']) in text(current[rel]), f'NAV4.2{phase} copy regressed: {rel}: {entry["new"]!r}')

# Inventory contract and exact audited distributions.
inv = INVENTORY.read_text(encoding='utf-8')
for needle in [
    '**Exact hooks inspected:** **392**.',
    '- `ARIA_LABEL`: **272**',
    '- `MAT_TOOLTIP`: **45**',
    '- `TITLE`: **62**',
    '- `SR_ONLY`: **8**',
    '- `ARIA_LABEL_RUNTIME`: **3**',
    '- `ARIA_LABEL_CONFIG`: **2**',
    '- `ALIGNED`: **111**',
    '- `DRIFT`: **22**',
    '- `TECHNICAL_TERM_LEGITIMATE`: **36**',
    '- `DOMAIN_TERM_LEGITIMATE`: **62**',
    '- `CONTEXTUAL_COPY`: **45**',
    '- `DYNAMIC_DATA`: **9**',
    '- `BACKEND_PASSTHROUGH`: **6**',
    '- `EXTERNAL_PASSTHROUGH`: **0**',
    '- `OUT_OF_SCOPE`: **101**',
    'controles icon-only sin nombre accesible útil detectado: **0**',
    '**22 cambios productivos de copy en 11 archivos**',
]:
    require(needle in inv, f'inventory contract missing: {needle}')
for dec in ['DEC-NAV-4.2J-001', 'DEC-NAV-4.2J-002', 'DEC-NAV-4.2J-003']:
    require(dec in inv, f'missing decision {dec}')

# Count hook rows by mechanism from the generated inventory, which is the canonical scan artifact.
row_mechanisms = re.findall(r'^\| `[^`]+:\d+` \| `[^`]+` \| `([^`]+)` \|', inv, re.M)
counts = Counter(row_mechanisms)
require(len(row_mechanisms) == 392, f'inventory hook table has {len(row_mechanisms)} rows, expected 392')
for mech, expected in {
    'ARIA_LABEL': 272, 'MAT_TOOLTIP': 45, 'TITLE': 62, 'SR_ONLY': 8,
    'ARIA_LABEL_RUNTIME': 3, 'ARIA_LABEL_CONFIG': 2,
}.items():
    require(counts[mech] == expected, f'inventory mechanism count mismatch {mech}: {counts[mech]} != {expected}')

print(
    f'PASS NAV4.2J static tooltip/ARIA copy validation: {ASSERTIONS} assertions; '
    '22 accessible-copy changes; 392 hooks; baseline NAV4.2I reconstructed exactly.'
)

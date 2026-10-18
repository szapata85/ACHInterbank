#!/usr/bin/env python3
from __future__ import annotations

import hashlib
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
ASSERTIONS = 0


def require(condition: bool, message: str) -> None:
    global ASSERTIONS
    ASSERTIONS += 1
    if not condition:
        raise AssertionError(message)


def read(path: str) -> str:
    return (ROOT / path).read_text(encoding="utf-8")


def file_sha256(path: str) -> str:
    return hashlib.sha256((ROOT / path).read_bytes()).hexdigest()


def aggregate_sha256(prefix: str) -> str:
    base = ROOT / prefix
    files = sorted(path for path in base.rglob("*") if path.is_file())
    digest = hashlib.sha256()
    for path in files:
        rel = path.relative_to(ROOT).as_posix()
        digest.update(rel.encode("utf-8"))
        digest.update(b"\0")
        digest.update(hashlib.sha256(path.read_bytes()).hexdigest().encode("ascii"))
        digest.update(b"\n")
    return digest.hexdigest()


service = read("web/ach-interbank-ui/src/app/core/services/navigation.service.ts")
spec = read("web/ach-interbank-ui/src/app/core/services/navigation.service.spec.ts")
phase_doc = read("docs/ai/navigation-menu/phases/PHASE_04_NAV4_SPA_COPY.md")

# NAV4.1A: backend tree is authoritative; Angular no longer filters/reconstructs IA by route/label.
require("api/navigation/menu" in service, "NavigationService must keep using the backend menu endpoint")
require("this.sortMenu(items ?? [])" in service, "NavigationService should only adapt backend ordering after fetch")
require("removeRetiredMenuItems" not in service, "frontend retired-route filtering must be removed")
require("normalizeRoute" not in service, "route normalization used for IA filtering must be removed")
require("LEGACY_NACHA_ROUTES" not in service, "legacy route hardcode must be removed from NavigationService")
require("RETIRED_FRONTEND_ROUTES" not in service, "retired frontend route hardcode must be removed from NavigationService")
require(".filter(" not in service, "NavigationService must not filter backend menu items")
require("Centro de integración CFA" not in service, "CFA label override must not exist in NavigationService")
require("children: []" not in service, "NavigationService must not manufacture empty children arrays")
require(".sort((a, b) => (a.order ?? 0) - (b.order ?? 0))" in service, "siblings must be sorted only by backend order")
require("children: this.sortMenu(item.children)" in service, "children must be preserved and ordered recursively")
require("item.children === undefined" in service, "missing children metadata must remain missing")
require("item.label" not in service, "NavigationService must not rewrite labels")
require("item.route" not in service, "NavigationService must not compare or rewrite routes")
require("item.exact" not in service, "NavigationService must not rewrite exact")
require("item.icon" not in service, "NavigationService must not rewrite icons")

# Focal regression coverage requested by NAV4.1A.
for marker in [
    "preserves backend labels routes hierarchy and children",
    "new backend child unknown to Angular",
    "preserves representative visible labels exactly",
    "preserva Conexiones y sus hijos exactamente desde el backend",
    "does not filter or rewrite backend items based on route values",
    "does not fabricate or substitute integration routes",
    "missing children metadata instead of manufacturing an empty children array",
    "Centro de integración CFA",
    "Configuración del intercambio de archivos",
    "Conexión entidad participante",
    "OfficialNavigation_ShouldExposeConfigProfilesWhenBackendReturnsIt",
    "Navigation_ShouldExposeSoapUatConsoleOnlyWhenBackendReturnsIt",
    "Navigation_ShouldExposeReconciliationConsoleOnlyWhenBackendReturnsIt",
]:
    require(marker in spec, f"missing focal NavigationService regression marker: {marker}")

require("NAV4.1A" in phase_doc, "NAV4 phase document must record NAV4.1A")
require("backend" in phase_doc.lower() and "fuente de verdad" in phase_doc.lower(), "NAV4.1A documentation must record backend source of truth")

# Protected NAV3/security/layout surfaces must remain byte-identical to NAV3.5B input.
protected_files = {
    "src/Cfa.ACHInterbank.Application/Navigation/Queries/GetMenuForCurrentUserHandler.cs": "f111e416c181fc1912c297e37012cf7462f49d0434c1ef6ec9416c89b5ed1a61",
    "src/Cfa.ACHInterbank.Application/Security/P0Policies.cs": "c080408f26c5a228a259f1aabd006463cc780778cba6574df48e114759fa28c1",
    "src/Cfa.ACHInterbank.Application/Security/P1Policies.cs": "a5eb51f927b62a187fe7b0c6c4405e2c13e2cab48d1c0dbfa059b83d13ae3808",
    "web/ach-interbank-ui/src/app/layout/main-layout.component.ts": "3b9bc7ddd4aa8628d11493ae53fff2829c197cf6d38cdda521fd8c775ca4a3af",
    "web/ach-interbank-ui/src/app/layout/main-layout.component.html": "725b993b8f24665ce1d8612e926f5a38384025f22ba6b89eaec0c13af39bfd66",
    "web/ach-interbank-ui/src/app/core/models/menu.model.ts": "9a0602ac6a2a358f7f7d8ad040df706d7fe100f98ca84419bb85a2073b371c77",
    "web/ach-interbank-ui/src/app/core/services/api.service.ts": "01e1680bb8f61c34bcaa05d82428aa6bc9fcb344862c2e4af22caf8b3e650a6e",
}
for path, expected in protected_files.items():
    require(file_sha256(path) == expected, f"protected file changed unexpectedly: {path}")

protected_groups = {
    "src/Cfa.ACHInterbank.Persistence/Navigation/Seeders": "512f43f6a12bfde3241a6801efcefa1123b9ddd5451fb3d284c56ff19dedfef5",
    "src/Cfa.ACHInterbank.Persistence/Security/Seeders": "5c2874626c8df6ed53bc86616580b96456fa57322a2eb00e0dfe2f7df8282c01",
    "web/ach-interbank-ui/src/app/core/guards": "c3e43798fddd8b4f0fc5aca24032f605be4598b485de3fe4f2af1a2c5dfcaf57",
}
for prefix, expected in protected_groups.items():
    require(aggregate_sha256(prefix) == expected, f"protected group changed unexpectedly: {prefix}")

print(f"PASS NAV4.1A static validation — {ASSERTIONS} assertions")

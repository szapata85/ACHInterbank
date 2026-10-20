#!/usr/bin/env python3
"""Static R2 ratchet for hidden DI construction and AppSettings globals.

This does not replace executable architecture tests. It provides a host-independent
structural gate for LOCAL_ADVANCEMENT environments that do not have dotnet.
"""
from __future__ import annotations

import re
import sys
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
SRC = ROOT / "src"
CRITICAL_ROOTS = [
    SRC / "Cfa.ACHInterbank.Api",
    SRC / "Cfa.ACHInterbank.Application",
    SRC / "Cfa.ACHInterbank.External",
]
PERSISTENCE_ROOT = SRC / "Cfa.ACHInterbank.Persistence"
PERSISTENCE_FALLBACK_BASELINE = 57

RUNTIME_FALLBACK = re.compile(
    r"\?\?\s*new\s+(?:[\w.]+\.)?\w+"
    r"(?:Service|Provider|Resolver|Policy|Validator|Builder|Factory|Guard|Strategy|Generator|Store|Auditor)\s*\("
)
PROHIBITED = {
    "AppSettings.Settings": re.compile(r"\bAppSettings\.Settings\b"),
    "direct LoggerManager construction": re.compile(r"new\s+LoggerManager\s*\("),
    "hidden SFTP session-factory construction": re.compile(r"new\s+SshNetAchColombiaSftpSessionFactory\s*\("),
}
API_OPERATIONAL_TIME_PROVIDER = re.compile(
    r"new\s+(?:Cfa\.ACHInterbank\.Persistence\.[\w.]+\.)?OperationalTimeSnapshotProvider\s*\("
)


def cs_files(root: Path):
    return sorted(root.rglob("*.cs"))


def rel(path: Path) -> str:
    return path.relative_to(ROOT).as_posix()


def matching(files, pattern: re.Pattern[str]):
    return [rel(path) for path in files if pattern.search(path.read_text(encoding="utf-8-sig", errors="ignore"))]


def main() -> int:
    all_source = cs_files(SRC)
    errors: list[str] = []

    for label, pattern in PROHIBITED.items():
        hits = matching(all_source, pattern)
        if hits:
            errors.append(f"{label}: {', '.join(hits)}")

    critical_files = [path for root in CRITICAL_ROOTS for path in cs_files(root)]
    critical_fallbacks = matching(critical_files, RUNTIME_FALLBACK)
    if critical_fallbacks:
        errors.append("critical hidden runtime-service fallbacks: " + ", ".join(critical_fallbacks))

    api_direct_provider = matching(cs_files(CRITICAL_ROOTS[0]), API_OPERATIONAL_TIME_PROVIDER)
    if api_direct_provider:
        errors.append("API direct OperationalTimeSnapshotProvider construction: " + ", ".join(api_direct_provider))

    persistence_fallback_count = sum(
        len(RUNTIME_FALLBACK.findall(path.read_text(encoding="utf-8-sig", errors="ignore")))
        for path in cs_files(PERSISTENCE_ROOT)
    )
    if persistence_fallback_count > PERSISTENCE_FALLBACK_BASELINE:
        errors.append(
            f"Persistence hidden runtime-service fallback count grew: "
            f"{persistence_fallback_count} > {PERSISTENCE_FALLBACK_BASELINE}"
        )

    print("R2 DI/CONFIG STATIC RATCHET")
    print(f"  AppSettings.Settings violations: {len(matching(all_source, PROHIBITED['AppSettings.Settings']))}")
    print(f"  critical hidden runtime-service fallbacks: {len(critical_fallbacks)}")
    print(f"  Persistence fallback debt count: {persistence_fallback_count}/{PERSISTENCE_FALLBACK_BASELINE}")
    print(f"  API direct operational-time provider constructions: {len(api_direct_provider)}")

    if errors:
        print("R2 DI/CONFIG STATIC RATCHET: FAIL", file=sys.stderr)
        for error in errors:
            print(f"  - {error}", file=sys.stderr)
        return 1

    print("R2 DI/CONFIG STATIC RATCHET: OK")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())

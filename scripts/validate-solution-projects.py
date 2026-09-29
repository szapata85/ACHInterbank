#!/usr/bin/env python3
"""Validate ACHInterbank.sln against local .csproj files and ProjectReference edges.

This is intentionally dependency-free so it can run before `dotnet restore`.
It catches the class of failure that produced NU1105 when a referenced project
exists on disk but is absent from the Visual Studio solution.
"""
from __future__ import annotations

import re
import sys
import xml.etree.ElementTree as ET
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
SLN = ROOT / "ACHInterbank.sln"
PROJECT_ROOTS = (ROOT / "src", ROOT / "tests")

PROJECT_RE = re.compile(
    r'^Project\("\{[^}]+\}"\) = "[^"]+", "([^"]+\.csproj)", "\{[^}]+\}"$',
    re.MULTILINE,
)


def norm(path: Path) -> str:
    return path.resolve().relative_to(ROOT.resolve()).as_posix().lower()


def sln_projects() -> set[str]:
    text = SLN.read_text(encoding="utf-8-sig")
    return {p.replace("\\", "/").lower() for p in PROJECT_RE.findall(text)}


def disk_project_map() -> dict[str, Path]:
    result: dict[str, Path] = {}
    for root in PROJECT_ROOTS:
        if root.exists():
            for project in root.rglob("*.csproj"):
                result[norm(project)] = project
    return result


def disk_projects() -> set[str]:
    return set(disk_project_map())


def project_references() -> tuple[list[str], list[str]]:
    missing_files: list[str] = []
    absent_from_solution: list[str] = []
    in_solution = sln_projects()
    project_map = disk_project_map()
    for rel in sorted(project_map):
        csproj = project_map[rel]
        try:
            tree = ET.parse(csproj)
        except ET.ParseError as exc:
            missing_files.append(f"INVALID XML: {rel}: {exc}")
            continue
        for node in tree.getroot().iter("ProjectReference"):
            include = node.attrib.get("Include")
            if not include:
                continue
            target = (csproj.parent / include.replace("\\", "/")).resolve()
            if not target.exists():
                missing_files.append(f"{rel} -> missing {target}")
                continue
            target_rel = norm(target)
            if target_rel not in in_solution:
                absent_from_solution.append(f"{rel} -> {target_rel}")
    return missing_files, absent_from_solution


def main() -> int:
    solution = sln_projects()
    disk = disk_projects()
    missing_from_solution = sorted(disk - solution)
    stale_in_solution = sorted(solution - disk)
    missing_ref_files, refs_absent_from_solution = project_references()

    failures = []
    if missing_from_solution:
        failures.append(("Projects on disk but absent from solution", missing_from_solution))
    if stale_in_solution:
        failures.append(("Solution projects missing on disk", stale_in_solution))
    if missing_ref_files:
        failures.append(("Broken ProjectReference paths / invalid project XML", missing_ref_files))
    if refs_absent_from_solution:
        failures.append(("ProjectReference targets absent from solution", refs_absent_from_solution))

    if failures:
        print("SOLUTION GRAPH: FAIL")
        for title, values in failures:
            print(f"\n{title}:")
            for value in values:
                print(f"  - {value}")
        return 1

    print("SOLUTION GRAPH: OK")
    print(f"  solution projects: {len(solution)}")
    print(f"  disk projects:     {len(disk)}")
    print("  project references: all targets exist and are loaded by the solution")
    return 0


if __name__ == "__main__":
    sys.exit(main())

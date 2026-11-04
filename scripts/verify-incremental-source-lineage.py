#!/usr/bin/env python3
"""Verify a full-source increment derives from its declared parent ZIP.

This offline gate verifies the exact parent archive SHA-256, confirms no parent
source file disappeared, and reports any changed/new files. It cannot tell
whether another, newer ZIP exists outside the artifacts supplied to this run.
"""

from __future__ import annotations

import argparse
import hashlib
import json
from pathlib import Path, PurePosixPath
from zipfile import ZipFile


def digest(path: Path) -> str:
    sha = hashlib.sha256()
    with path.open('rb') as file:
        for chunk in iter(lambda: file.read(1024 * 1024), b''):
            sha.update(chunk)
    return sha.hexdigest()


def main() -> int:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--parent-zip', type=Path, required=True)
    parser.add_argument('--project-root', type=Path, default=Path(__file__).resolve().parent.parent)
    parser.add_argument('--expected-parent-sha256', required=True)
    parser.add_argument('--evidence', type=Path, help='Optional JSON evidence output')
    args = parser.parse_args()
    parent = args.parent_zip.resolve()
    root = args.project_root.resolve()
    actual_parent = digest(parent)
    expected_parent = args.expected_parent_sha256.lower()
    parent_matches = actual_parent == expected_parent
    previous: dict[str, bytes] = {}
    bad_paths: list[str] = []

    with ZipFile(parent) as archive:
        infos = [info for info in archive.infolist() if not info.is_dir()]
        parent_dirs = {PurePosixPath(info.filename).parts[0] for info in infos}
        if len(parent_dirs) != 1:
            raise ValueError('Parent ZIP must have exactly one top-level project folder')
        prefix = next(iter(parent_dirs))
        for info in infos:
            path = PurePosixPath(info.filename)
            relative = PurePosixPath(*path.parts[1:])
            if not relative.parts or '..' in relative.parts or relative.is_absolute():
                bad_paths.append(info.filename)
                continue
            relative_str = relative.as_posix()
            if relative_str in previous:
                bad_paths.append(f'Duplicate path: {info.filename}')
                continue
            # SHA-256 of the parent ZIP protects ZIP metadata and every entry.
            # Exact content comparison detects changes without relying on timestamps.
            previous[relative_str] = archive.read(info)

    evidence_path = args.evidence.resolve() if args.evidence else None
    actual = {
        path.relative_to(root).as_posix(): path
        for path in root.rglob('*')
        if path.is_file() and path.resolve() != evidence_path
        and not any(segment in {'.git', 'bin', 'obj', 'node_modules', '__pycache__'}
                    for segment in path.relative_to(root).parts)
    }
    deleted = sorted(set(previous) - set(actual))
    added = sorted(set(actual) - set(previous))
    modified = sorted(
        name for name in previous.keys() & actual.keys()
        if actual[name].read_bytes() != previous[name]
    )
    result = {
        'gate': 'PARENT_ZIP_LINEAGE_AND_FULL_SOURCE_PRESERVATION',
        'parent_zip': parent.name,
        'expected_parent_sha256': expected_parent,
        'actual_parent_sha256': actual_parent,
        'parent_hash_matches': parent_matches,
        'parent_file_count': len(previous),
        'current_file_count': len(actual),
        'preserved_file_count': len(previous) - len(deleted),
        'deleted': deleted,
        'modified': modified,
        'added': added,
        'invalid_parent_paths': bad_paths,
        'pass': parent_matches and not deleted and not bad_paths
    }
    if args.evidence:
        args.evidence.parent.mkdir(parents=True, exist_ok=True)
        args.evidence.write_text(json.dumps(result, indent=2, ensure_ascii=False) + '\n', encoding='utf8')
    print(json.dumps(result, indent=2, ensure_ascii=False))
    return 0 if result['pass'] else 1


if __name__ == '__main__':
    raise SystemExit(main())

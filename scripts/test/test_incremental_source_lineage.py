"""Offline tests for the full-ZIP parent lineage gate; no Angular/.NET required."""

from __future__ import annotations

import hashlib
import json
import subprocess
import sys
import tempfile
import unittest
from pathlib import Path
from zipfile import ZipFile


SCRIPT = Path(__file__).resolve().parents[1] / 'verify-incremental-source-lineage.py'


class IncrementSourceLineageTests(unittest.TestCase):
    def setUp(self) -> None:
        self.temp = tempfile.TemporaryDirectory()
        self.addCleanup(self.temp.cleanup)
        directory = Path(self.temp.name)
        self.project = directory / 'project'
        self.project.mkdir()
        self.file = self.project / 'src' / 'entry.txt'
        self.file.parent.mkdir()
        self.file.write_text('initial source', encoding='utf8')
        self.parent = directory / 'parent.zip'
        with ZipFile(self.parent, 'w') as archive:
            archive.writestr('project/src/entry.txt', 'initial source')
        self.digest = hashlib.sha256(self.parent.read_bytes()).hexdigest()

    def check_gate(self, sha: str) -> tuple[int, dict]:
        result = subprocess.run(
            [sys.executable, str(SCRIPT), '--parent-zip', str(self.parent),
             '--project-root', str(self.project), '--expected-parent-sha256', sha],
            capture_output=True, text=True, check=False,
        )
        self.assertEqual(result.stderr, '')
        return result.returncode, json.loads(result.stdout)

    def test_valid_parent_with_intentional_file_change(self) -> None:
        self.file.write_text('improved source', encoding='utf8')
        code, evidence = self.check_gate(self.digest)
        self.assertEqual(code, 0)
        self.assertTrue(evidence['pass'])
        self.assertEqual(evidence['modified'], ['src/entry.txt'])
        self.assertEqual(evidence['deleted'], [])

    def test_rejects_wrong_parent_sha(self) -> None:
        code, evidence = self.check_gate('0' * 64)
        self.assertEqual(code, 1)
        self.assertFalse(evidence['parent_hash_matches'])
        self.assertFalse(evidence['pass'])

    def test_rejects_missing_original_file(self) -> None:
        self.file.unlink()
        code, evidence = self.check_gate(self.digest)
        self.assertEqual(code, 1)
        self.assertEqual(evidence['deleted'], ['src/entry.txt'])
        self.assertFalse(evidence['pass'])


if __name__ == '__main__':
    unittest.main()

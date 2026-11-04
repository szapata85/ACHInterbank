"""APN-I13: checks of source/test wiring; does not compile or execute C# or EF."""
from pathlib import Path
import sqlite3
import unittest

ROOT = Path(__file__).resolve().parents[3]
SCRIPTS = ROOT / "scripts/audit/prenotificaciones"
JOB = (ROOT / "src/Cfa.ACHInterbank.Persistence/ACH/Quartz/Jobs/Implementation/AchTacitAcceptanceJobHandler.cs").read_text()
SCAN = (ROOT / "src/Cfa.ACHInterbank.Persistence/ACH/Quartz/Jobs/Implementation/PrenotificationCandidateScan.cs").read_text()
TESTS = (ROOT / "tests/Cfa.ACHInterbank.Tests/PrenotificationCursorI13RelationalTests.cs").read_text()
RUNNER = (SCRIPTS / "run_apn_i13_dotnet.sh").read_text()

class StabilizationContracts(unittest.TestCase):
    def test_no_added_product_workflow(self):
        self.assertIn('IsOperationallySafe(', JOB)
        self.assertIn('PrenotificationCandidateScan.LoadPageAsync', JOB)
        self.assertIn('if (!transmitted) return false;', JOB)
        self.assertIn('if (!recalculatedDeadline.HasValue) return false;', JOB)

    def test_legacy_and_canonical_candidates_both_scanned(self):
        self.assertIn('canonical.Union(legacy)', SCAN)
        self.assertIn('!x.ResponseDeadlineAtUtc.HasValue', SCAN)
        self.assertIn('!db.PrenotificationValidationAttempts.Any', SCAN)

    def test_cursor_boundary_and_idempotency(self):
        self.assertIn('ParseCursor(string? value)', SCAN)
        self.assertIn('var afterId = pass == 0 ? cursor : 0;', JOB)
        self.assertIn('var maxId = pass == 0 ? int.MaxValue : cursor;', JOB)
        self.assertIn('IdempotencyKey: $"apn-tacit:{transactionId}"', JOB)

    def test_relational_tests_are_present_and_targeted(self):
        self.assertIn('UseSqlite(connection)', TESTS)
        self.assertIn('I13_CursorPersistsAcrossDbContexts_WithoutOverwritingBatchSize', TESTS)
        self.assertIn('I13_ExistingUniqueTaskParameterIndex_PreventsDuplicateCursor', TESTS)
        self.assertIn('Assert.ThrowsAsync<DbUpdateException>', TESTS)

    def test_runner_fails_closed_without_dotnet_sdk(self):
        self.assertIn('command -v dotnet', RUNNER)
        self.assertIn('exit 127', RUNNER)
        self.assertIn('PrenotificationCursorI13RelationalTests', RUNNER)
        self.assertIn('PrenotificationI10TerminalStateTests', RUNNER)

    def test_independent_cursor_parameter_key_uniqueness_in_sqlite(self):
        # A minimal SQL behavior example, NOT execution of EF Core migrations.
        with sqlite3.connect(':memory:') as db:
            db.execute('CREATE TABLE TaskParameters (TaskDefinitionId INTEGER, Key TEXT, Value TEXT, UNIQUE(TaskDefinitionId, Key))')
            db.execute('INSERT INTO TaskParameters VALUES (13, "BatchSize", "500")')
            db.execute('INSERT INTO TaskParameters VALUES (13, "ApnI12LastExaminedTransactionId", "1402")')
            db.execute('UPDATE TaskParameters SET Value="1501" WHERE TaskDefinitionId=13 AND Key="ApnI12LastExaminedTransactionId"')
            self.assertEqual([('BatchSize', '500'), ('ApnI12LastExaminedTransactionId', '1501')],
                             db.execute('SELECT Key, Value FROM TaskParameters ORDER BY Key DESC').fetchall())
            with self.assertRaises(sqlite3.IntegrityError):
                db.execute('INSERT INTO TaskParameters VALUES (13, "ApnI12LastExaminedTransactionId", "1502")')

if __name__ == '__main__':
    unittest.main(verbosity=2)

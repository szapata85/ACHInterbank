"""APN-I12: inspect production source contracts and model keyset fairness independently.

The Python examples do NOT execute EF Core, C#, Quartz, SQL or integration tests.
Run run_apn_i12_dotnet.sh on a .NET 10 capable host to test C# semantics.
"""
import unittest
from pathlib import Path

ROOT = Path(__file__).resolve().parents[3]
JOB = (ROOT / 'src/Cfa.ACHInterbank.Persistence/ACH/Quartz/Jobs/Implementation/AchTacitAcceptanceJobHandler.cs').read_text()
SCAN = (ROOT / 'src/Cfa.ACHInterbank.Persistence/ACH/Quartz/Jobs/Implementation/PrenotificationCandidateScan.cs').read_text()
TESTS = (ROOT / 'tests/Cfa.ACHInterbank.Tests/PrenotificationCandidateScanI12Tests.cs').read_text()


class StaticContracts(unittest.TestCase):
    def test_missing_persisted_deadline_can_be_revisited(self):
        self.assertIn('!x.ResponseDeadlineAtUtc.HasValue || x.ResponseDeadlineAtUtc.Value <= now', SCAN)
        self.assertIn('ResolvePrenotificationResponseDeadlineAsync(rule, sourceCycleId, ct)', JOB)
        self.assertIn('if (!recalculatedDeadline.HasValue) return false;', JOB)

    def test_no_premature_acceptance_when_date_is_missing(self):
        self.assertIn('IsOperationallySafe(', JOB)
        self.assertIn('if (!transmitted) return false;', JOB)
        self.assertIn('if (hasResponse) return false;', JOB)

    def test_distinct_keyset_order_prevents_offset_skipping(self):
        self.assertIn('canonical.Union(legacy)', SCAN)
        self.assertIn('.Where(id => id > afterId && id <= maxId)', SCAN)
        self.assertIn('.OrderBy(id => id)', SCAN)
        self.assertIn('.Take(pageSize)', SCAN)
        self.assertNotIn('.Skip(', SCAN)

    def test_bounded_scan_with_two_pass_wrap(self):
        self.assertIn('var scanLimit', JOB)
        self.assertIn('examined < scanLimit', JOB)
        self.assertIn('var afterId = pass == 0 ? cursor : 0;', JOB)
        self.assertIn('var maxId = pass == 0 ? int.MaxValue : cursor;', JOB)

    def test_cursor_persists_in_existing_config_no_schema(self):
        self.assertIn('ApnI12LastExaminedTransactionId', SCAN)
        self.assertIn('_db.TaskParameters', JOB)
        self.assertIn('parameter.Value = lastExamined.ToString', JOB)
        self.assertIn('await _db.SaveChangesAsync(cancellationToken)', JOB)

    def test_missing_cursor_fails_safe(self):
        self.assertIn('cursor > 0 ? cursor : 0', SCAN)
        self.assertIn('task.Id > 0', JOB)

    def test_diagnostics_expose_saturation_and_errors(self):
        self.assertIn('límite de exploración alcanzado', JOB)
        self.assertIn('Errores={Errors}', JOB)
        self.assertIn('CursorInicial={InitialCursor}', JOB)

    def test_old_first_page_limit_removed(self):
        self.assertNotIn('var canonicalIds =', JOB)
        self.assertNotIn('Take(batchSize * 4)', JOB)
        self.assertIn('PrenotificationCandidateScan.LoadPageAsync(', JOB)

    def test_regression_test_cases_authored(self):
        for case in ('I12_NullStoredDeadlineIsReevaluated',
                     'I12_KeysetPagesDoNotSkipLegacyOrCanonicalRows',
                     'I12_RotationWrapsOnce', 'I12_CursorParsing_FailsSafe'):
            self.assertIn(case, TESTS)


class IndependentCursorExamples(unittest.TestCase):
    @staticmethod
    def pick(ids, cursor, budget):
        """Independent mathematical model; NOT an invocation of the C# job."""
        ordered = sorted(set(ids))
        return ([x for x in ordered if x > cursor] +
                [x for x in ordered if 0 < x <= cursor])[:budget]

    def test_blocked_older_rows_do_not_starve_newer_over_successive_runs(self):
        ids = [100, 200, 300, 400, 500, 600]
        first = self.pick(ids, 0, 2)
        second = self.pick(ids, first[-1], 2)
        third = self.pick(ids, second[-1], 2)
        self.assertEqual([100, 200], first)
        self.assertEqual([300, 400], second)
        self.assertEqual([500, 600], third)

    def test_cursor_revisits_initial_segment_only_after_wrap(self):
        ids = [100, 200, 300, 400]
        self.assertEqual([400, 100, 200], self.pick(ids, 300, 3))

    def test_keyset_succeeds_when_previous_page_is_removed(self):
        ids = [100, 200, 300, 400]
        first = self.pick(ids, 0, 2)
        ids.remove(100)
        self.assertEqual([300, 400], self.pick(ids, first[-1], 2))


if __name__ == '__main__':
    unittest.main(verbosity=2)

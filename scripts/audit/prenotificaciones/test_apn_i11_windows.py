"""APN-I11: static source contracts + independent UTC/local schedule examples.

This DOES NOT execute C#, query SQL, validate external receipt completion, or
certify runtime operation. Use run_apn_i11_dotnet.sh in an SDK-backed environment.
"""
from pathlib import Path
from datetime import datetime, timedelta, timezone
from zoneinfo import ZoneInfo
import unittest

ROOT = Path(__file__).resolve().parents[3]
POLICY = (ROOT / 'src/Cfa.ACHInterbank.Persistence/ACH/Services/Implementation/ClearingHouseValidationPolicyService.cs').read_text()
JOB = (ROOT / 'src/Cfa.ACHInterbank.Persistence/ACH/Quartz/Jobs/Implementation/AchTacitAcceptanceJobHandler.cs').read_text()
WINDOW = (ROOT / 'src/Cfa.ACHInterbank.Application/ACH/Services/PrenotificationReceiptWindow.cs').read_text()
TESTS = (ROOT / 'tests/Cfa.ACHInterbank.Tests/PrenotificationReceiptWindowI11Tests.cs').read_text()
LIFECYCLE = (ROOT / 'tests/Cfa.ACHInterbank.Tests/ThirdPartyAccountValidationLifecycleTests.cs').read_text()
BOGOTA = ZoneInfo('America/Bogota')


class I11SourceContracts(unittest.TestCase):
    def test_ach_cycle_count_excludes_cancelled(self):
        self.assertIn('x.OperationalStatus != AchCycleOperationalStatus.Cancelled', POLICY)

    def test_cancelled_source_cannot_be_accepted(self):
        self.assertIn('sourceCycle.OperationalStatus == AchCycleOperationalStatus.Cancelled', POLICY)

    def test_operational_local_window_used_for_nacha_dates(self):
        self.assertIn('x.OperationalDate.Value >= inboundWindow.OperationalStartDate', JOB)
        self.assertIn('x.OperationalDate.Value < inboundWindow.OperationalEndExclusive', JOB)

    def test_unclassified_nacha_receipts_use_utc_window(self):
        self.assertIn('x.UploadedAtUtc >= inboundWindow.UtcUnclassifiedStartInclusive', JOB)
        self.assertIn('x.UploadedAtUtc < inboundWindow.UtcEndExclusive', JOB)

    def test_mft_operational_dates_are_local(self):
        self.assertIn('x.OperationalDate >= inboundWindow.OperationalStartDate', JOB)
        self.assertIn('x.OperationalDate < inboundWindow.OperationalEndExclusive', JOB)

    def test_cenit_unresolved_negative_can_be_in_later_return_cycle(self):
        self.assertIn('x.ReceivedAtUtc >= inboundWindow.UtcOperationalStartInclusive', JOB)
        self.assertIn('x.ReceivedAtUtc < inboundWindow.UtcEndExclusive', JOB)
        self.assertNotIn('x.AchCycleId == original.AchCycleId', JOB.split('var cenitResponseOutstanding =', 1)[1])

    def test_no_utc_date_shortcut(self):
        self.assertNotIn('recalculatedDeadline.Value.UtcDateTime.Date.AddDays(1)', JOB)
        self.assertIn('TimeZoneInfo.ConvertTime(responseDeadline, zone).Date', WINDOW)

    def test_no_false_positive_receipt_complete(self):
        self.assertNotIn('AllFilesReceived = true', JOB)
        self.assertNotIn('ReceiptComplete = true', JOB)

    def test_xunit_regression_cases_are_authored(self):
        for case in ('I11_AchColombiaCycleFive_UsesLocalDayBoundaryNotUtcMidnight',
                     'I11_CenitFinalOutputAt1915Local_IsWithinUtcWindowAfterMidnight',
                     'I11_AchColombiaLateCycle_CanCrossWeekendWithLocalDateBounds'):
            self.assertIn(case, TESTS)
        for case in ('I11_AchColombiaCancelledCycle_IsNotCountedAsNextReturnOpportunity',
                     'I11_CancelledSourceCycle_NeverProducesTacitDeadline',
                     'I11_CenitCancelledSession_DoesNotMeetFiveActiveCycleRequirement'):
            self.assertIn(case, LIFECYCLE)


class I11IndependentDateExamples(unittest.TestCase):
    @staticmethod
    def next_local_midnight_utc(deadline_utc):
        local = deadline_utc.astimezone(BOGOTA)
        midnight = datetime(local.year, local.month, local.day, tzinfo=BOGOTA) + timedelta(days=1)
        return midnight.astimezone(timezone.utc)

    def test_achcol_cycle_one_fourth_later_is_cycle_five(self):
        # Normative 7-cycle source schedule, local release for cycle 5 at 15:30.
        cutoffs = ['07:00', '09:00', '11:00', '12:30', '14:30', '16:00', '18:00']
        self.assertEqual(cutoffs[4], '14:30')
        deadline_utc = datetime(2026, 11, 3, 20, 30, tzinfo=timezone.utc)
        self.assertEqual(deadline_utc.astimezone(BOGOTA).strftime('%H:%M'), '15:30')
        self.assertEqual(self.next_local_midnight_utc(deadline_utc), datetime(2026, 11, 4, 5, tzinfo=timezone.utc))

    def test_cenit_final_release_after_utc_midnight_remains_same_operational_day(self):
        deadline_utc = datetime(2026, 11, 4, 0, 15, tzinfo=timezone.utc)
        self.assertEqual(deadline_utc.astimezone(BOGOTA).strftime('%Y-%m-%d %H:%M'), '2026-11-03 19:15')
        self.assertEqual(self.next_local_midnight_utc(deadline_utc), datetime(2026, 11, 4, 5, tzinfo=timezone.utc))

    def test_cross_day_sixth_cycle_can_reach_next_operational_day(self):
        deadline_utc = datetime(2026, 11, 9, 20, 30, tzinfo=timezone.utc)
        self.assertEqual(self.next_local_midnight_utc(deadline_utc), datetime(2026, 11, 10, 5, tzinfo=timezone.utc))

if __name__ == '__main__':
    unittest.main(verbosity=2)

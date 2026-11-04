#!/usr/bin/env python3
"""APN-I08: static checks + independent documentary schedule checks.
These tests do NOT execute C# / .NET / bank integration and cannot certify runtime behavior.
"""
from pathlib import Path
from datetime import datetime, timedelta
from zoneinfo import ZoneInfo
import unittest

ROOT = Path(__file__).resolve().parents[3]
SERVICE = ROOT / 'src/Cfa.ACHInterbank.Persistence/ACH/Services/Implementation/ClearingHouseValidationPolicyService.cs'
XUNIT = ROOT / 'tests/Cfa.ACHInterbank.Tests/ThirdPartyAccountValidationLifecycleTests.cs'

class I08Checks(unittest.TestCase):
    def test_cycle_window_must_be_complete(self):
        code = SERVICE.read_text(encoding='utf-8-sig')
        self.assertIn('.Take(window)', code)
        self.assertIn('futureCycles.Count != window', code)
        self.assertIn('deadlineCycle = futureCycles[^1]', code)
    def test_output_release_used_instead_of_cutoff(self):
        code = SERVICE.read_text(encoding='utf-8-sig')
        self.assertIn('deadlineCycle.OutputReleaseEndTime ?? deadlineCycle.OutputReleaseTime', code)
        self.assertIn('releaseDeadline <= deadlineCycle.CutoffTime', code)
        self.assertIn('ProcessingDate.Date.Add(releaseDeadline)', code)
        self.assertNotIn('ProcessingDate.Date.Add(deadlineCycle.CutoffTime)', code)
    def test_csharp_regression_cases_present(self):
        code = XUNIT.read_text(encoding='utf-8-sig')
        for title in (
            'ResponseDeadline_AchColombiaSevenCycles_WaitsForFourthLaterCycleOutput',
            'ResponseDeadline_AchColombiaMissingFutureCycles_FailsClosed',
            'ResponseDeadline_AchColombiaLaterCycleMissingReleaseMetadata_FailsClosed',
            'ResponseDeadline_AchColombiaLateCycle_ContinuesCountingAcrossOperationalDays',
            'ResponseDeadlines_AchUsesConfiguredCycleWindow_AndCenitUsesOperationalDayEnd',
        ):
            self.assertIn(title, code)
    def test_achcol_seven_cycles_documentary(self):
        cuts = ['07:00','09:00','11:00','12:30','14:30','16:00','18:00']
        self.assertEqual(len(cuts), 7)
        self.assertEqual(cuts[4], '14:30')
        catalog = (ROOT / 'src/Cfa.ACHInterbank.Application/ACH/Models/RegulatoryCycleScheduleCatalog.cs').read_text(encoding='utf-8-sig')
        self.assertIn('AchColombiaSevenCycleConfirmedVersion = "ACHCOL-7C-20261014"', catalog)
        self.assertIn('new TimeSpan(15, 30, 0)', catalog)  # Fifth-cycle release end
    def test_cenit_fifth_release_after_cutoff_documentary(self):
        cutoff = datetime(2026, 11, 3, 18, 45, tzinfo=ZoneInfo('America/Bogota'))
        release = datetime(2026, 11, 3, 19, 15, tzinfo=ZoneInfo('America/Bogota'))
        self.assertEqual(release-cutoff,timedelta(minutes=30))
    def test_bogota_conversions_documentary(self):
        bogota = ZoneInfo('America/Bogota')
        self.assertEqual(datetime(2026,11,3,15,30,tzinfo=bogota).astimezone(ZoneInfo('UTC')).hour,20)
        self.assertEqual(datetime(2026,11,3,19,15,tzinfo=bogota).astimezone(ZoneInfo('UTC')).date().isoformat(),'2026-11-04')

if __name__ == '__main__':
    unittest.main(verbosity=2)

"""Static source contract checks for APN-I09; NOT a .NET test runner."""
from pathlib import Path
import unittest

ROOT = Path(__file__).resolve().parents[3]
JOB = (ROOT / 'src/Cfa.ACHInterbank.Persistence/ACH/Quartz/Jobs/Implementation/AchTacitAcceptanceJobHandler.cs').read_text()
GATE = (ROOT / 'src/Cfa.ACHInterbank.Application/ACH/Services/PrenotificationTacitAcceptanceDecision.cs').read_text()
POLICY = (ROOT / 'src/Cfa.ACHInterbank.Persistence/ACH/Services/Implementation/ClearingHouseValidationPolicyService.cs').read_text()
TEST = (ROOT / 'tests/Cfa.ACHInterbank.Tests/PrenotificationTacitAcceptanceI09SafetyTests.cs').read_text()

class I09StaticSafety(unittest.TestCase):
    def test_guards_against_stale_deadline(self):
        self.assertIn('ResolvePrenotificationResponseDeadlineAsync(rule, sourceCycleId, ct)', JOB)
        self.assertIn('storedDeadlineAtUtc.Value > recalculatedDeadlineAtUtc.Value', GATE)

    def test_requires_proof_of_transmission(self):
        self.assertIn('AchFileExportTransactions.AsNoTracking()', JOB)
        self.assertIn('TransmittedAtUtc.HasValue', JOB)
        self.assertIn('if (!transmitted) return false', JOB)

    def test_blocks_pending_ingestion(self):
        self.assertIn('IncomingNachaFileIngestions.AsNoTracking()', JOB)
        self.assertIn('IncomingNachaIngestionStatus.PendienteResolucion', JOB)
        self.assertIn('inboundProcessingPending', GATE)

    def test_blocks_response_evidence(self):
        self.assertIn('AchResponses.AsNoTracking()', JOB)
        self.assertIn('NachaInboundSimulationEntries.AsNoTracking()', JOB)
        self.assertIn('hasAnyResponseEvidence', GATE)

    def test_serializable_and_idempotent(self):
        self.assertIn('IsolationLevel.Serializable', JOB)
        self.assertIn('IdempotencyKey: $"apn-tacit:{transactionId}"', JOB)

    def test_canonical_is_current(self):
        self.assertIn('validation.CurrentAttemptId == attempt.Id', JOB)
        self.assertIn('validation.Status == ThirdPartyAccountValidationStatus.PendingResponse', JOB)

    def test_cenit_requires_five_sessions(self):
        self.assertIn('operationalDayCycles.Count != 5', POLICY)
        self.assertIn('I09_CenitIncompleteDayCalendar_DoesNotAuthorizeEarlyTacitAcceptance',
                      (ROOT / 'tests/Cfa.ACHInterbank.Tests/ThirdPartyAccountValidationLifecycleTests.cs').read_text())

    def test_csharp_test_source(self):
        self.assertIn('I09_OldLaterDeadline_IsNotShortenedByPolicyRecalculation', TEST)
        self.assertIn('I09_CenitOperationalDay_WaitsUntilOutputIsAvailable', TEST)

if __name__ == '__main__':
    unittest.main()

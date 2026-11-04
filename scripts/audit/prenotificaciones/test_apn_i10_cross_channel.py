"""APN-I10 static source contracts (not compiled/running C# or validating a real chamber)."""
from pathlib import Path
import unittest

ROOT = Path(__file__).resolve().parents[3]
JOB = (ROOT / 'src/Cfa.ACHInterbank.Persistence/ACH/Quartz/Jobs/Implementation/AchTacitAcceptanceJobHandler.cs').read_text()
STATE = (ROOT / 'src/Cfa.ACHInterbank.Persistence/ACH/Services/Implementation/AchStateTransitionService.cs').read_text()
CENIT = (ROOT / 'src/Cfa.ACHInterbank.Persistence/ACH/Services/Implementation/CenitChamberResponseService.cs').read_text()
GUARD = (ROOT / 'src/Cfa.ACHInterbank.Persistence/ACH/Services/Implementation/IncomingPrenotificationReturnGuard.cs').read_text()
TESTS = (ROOT / 'tests/Cfa.ACHInterbank.Tests/PrenotificationI10TerminalStateTests.cs').read_text()

class I10SourceContractTests(unittest.TestCase):
    def test_mft_claim_before_nacha_ingestion_is_guarded(self):
        self.assertIn('AchManagedFileTransfers.AsNoTracking()', JOB)
        for value in ['Received','InProgress','RetryPending','Uncertain','Failed','Rejected']:
            self.assertIn('AchManagedFileTransferStatus.' + value, JOB)

    def test_ambiguous_nacha_ingestions_are_fail_closed(self):
        self.assertIn('!x.DetectedClearingHouseId.HasValue', JOB)
        self.assertIn('IncomingNachaIngestionStatus.Fallido', JOB)
        self.assertIn('IncomingNachaIngestionStatus.Bloqueado', JOB)

    def test_cenit_gateway_ledger_is_consulted(self):
        self.assertIn('CenitChamberResponses.AsNoTracking()', JOB)
        self.assertIn('CenitChamberResponseType.OperatorRejected', JOB)
        self.assertIn('CenitChamberResponseType.Nack', JOB)
        self.assertIn('x.AchFileExport!.Transactions.Any', JOB)

    def test_all_three_inbound_barriers_reach_decision(self):
        self.assertIn('inboundPending || mftInboundOutstanding || cenitResponseOutstanding', JOB)

    def test_terminal_prenotes_cannot_be_rewritten_via_canonical_service(self):
        self.assertIn('PRENOTE_TERMINAL_CHANGE_REQUIRES_MANUAL_REVIEW', STATE)
        self.assertIn('PRENOTE_ATTEMPT_ALREADY_TERMINAL_REQUIRES_MANUAL_REVIEW', STATE)

    def test_late_return_is_classified_for_manual_review(self):
        self.assertIn('PRENOTE_LATE_RETURN_REQUIRES_MANUAL_REVIEW', GUARD)
        self.assertIn('Result.Block(', GUARD)

    def test_cenit_no_longer_directly_mutates_prenotes(self):
        self.assertIn('else if (transaction.IsPrenotification)', CENIT)
        self.assertIn('stateTransitions.TransitionAsync(new AchStateTransitionRequest(', CENIT)
        self.assertIn('CENIT_PRENOTE_RETURN_REQUIRES_MANUAL_REVIEW', CENIT)
        self.assertIn('CENIT_PRENOTE_CANONICAL_SYNCHRONIZATION_REQUIRED', CENIT)

    def test_cenit_operator_response_atomicity(self):
        self.assertIn('BeginTransactionAsync(IsolationLevel.Serializable, ct)', CENIT)
        self.assertIn('receiptTransaction.CommitAsync(ct)', CENIT)
        self.assertIn('receiptTransaction.RollbackAsync(CancellationToken.None)', CENIT)

    def test_xunit_safety_cases_are_authored(self):
        for name in [
            'I10_CanonicalService_NeverRewritesTerminalPrenoteAutomatically',
            'I10_TerminalAttemptCannotBeOverwrittenEvenIfTransactionStillPending',
            'I10_CenitOperatorRejection_UsesCanonicalTransitionAndSynchronizesAttempt',
        ]:
            self.assertIn(name, TESTS)

    def test_no_made_up_positive_watermark(self):
        self.assertNotIn('ReceiptComplete = true', JOB)
        self.assertNotIn('AllFilesReceived = true', JOB)

if __name__ == '__main__':
    unittest.main()

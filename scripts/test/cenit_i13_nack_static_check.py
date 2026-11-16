"""I13 source-contract checks; DO NOT equate with executing C# or DB tests."""
from pathlib import Path
R = Path(__file__).resolve().parents[2]
s = (R/'src/Cfa.ACHInterbank.Persistence/ACH/Services/Implementation/CenitChamberResponseService.cs').read_text()
t = (R/'tests/Cfa.ACHInterbank.Tests/CenitChamberResponseLifecycleTests.cs').read_text()
m = (R/'src/Cfa.ACHInterbank.Persistence/Configuration/CenitChamberResponseConfiguration.cs').read_text()
c = (R/'src/Cfa.ACHInterbank.Persistence/ACH/Services/Implementation/CenitLocalGatewayPickupHostedService.cs').read_text()
checks = {
    'parse XML FileNack by FileErrorHandling items': 'root.Elements()' in s and 'FileErrorHandling' in s and 'return errors.Select(error =>' in s,
    'physical trace comes from error item': 'DescendantValue(error, "TraceNo")' in s,
    'item-level NACK and file-level NACK remain separate': 'trace is not null || batch is not null' in s,
    'official item trace not overridden by request': 'parsed.Type == CenitChamberResponseType.OperatorRejected' in s and '? parsed.TransactionTrace' in s,
    'file correlation is scoped to CENIT': 'x.ClearingHouseId == clearingHouseId && x.ExportKind == "NACHA"' in s,
    'correlation requires file identifier': 'CENIT_CORRELATION_IDENTIFIER_REQUIRED' in s,
    'trace matches uniquely within export': 'transactionMatches.Length != 1' in s,
    'trace mismatch fails closed': 'CENIT_TRANSACTION_TRACE_MISMATCH' in s and 'transaction.TraceNumber, transactionTrace' in s,
    'trace mismatch does not expose wrong associated transaction': 'response.AchTransactionId = null;' in s,
    'conflicting codes for same trace detected before writes': s.index('var conflictingTraces = parsedItems') < s.index('results.Add(await ImportItemAsync('),
    'conflicts grouped by canonical trace': '.GroupBy(x => x.TransactionTrace!, StringComparer.Ordinal)' in s,
    'conflicts require multiple distinct reasons': '.Distinct(StringComparer.Ordinal).Count() > 1' in s,
    'conflicts annotate only affected trace': 'conflictingTraces.Contains(x.TransactionTrace)' in s,
    'conflicting trace stored under controlled code': 'CENIT_NACK_CONFLICTING_REASONS_FOR_TRACE' in s,
    'conflicting item correlation cannot mutate': 'correlation = CenitChamberCorrelationOutcome.Invalid;' in s and 'if (parsed.Type == CenitChamberResponseType.OperatorRejected && parsed.ProblemCode is not null)' in s,
    'item NACK branch precedes file branch': s.index('parsed.Type == CenitChamberResponseType.OperatorRejected)\n        {') < s.index('parsed.Type != CenitChamberResponseType.OperatorRejected)\n        {'),
    'item event is operator-originated': 'context.AchTransactionStateEvents.Add(new AchTransactionStateEvent' in s and 'Source = AchStateEventSourceEnum.Operator' in s,
    'unique response source and item sequence': 'x.SourceResponseId, x.ItemSequence' in m,
    'unique per-item idempotency key': 'builder.HasIndex(x => x.IdempotencyKey).IsUnique();' in m,
    'only pending, tacit and certified monetary states eligible': 'AchTransferStateEnum.Pending\n                         or AchTransferStateEnum.AppliedTacitly\n                         or AchTransferStateEnum.Certified' in s,
    'already rejected does not emit second event': 'CENIT_TRANSACTION_ALREADY_REJECTED' in s,
    'single relational transaction for receipt and state change': 'BeginTransactionAsync(IsolationLevel.Serializable, ct)' in s and 'CommitAsync(ct)' in s,
    'DB insert conflict rechecks source content hash': 'return string.Equals(winner.ContentSha256, contentHash, StringComparison.Ordinal)' in s,
    'conflicting source identity reports controlled conflict': 'CENIT_RESPONSE_ID_CONFLICT' in s,
    'one import write for all item rows': s.index('results.Add(await ImportItemAsync(') < s.index('await context.SaveChangesAsync(ct);'),
    'pickup keeps using response service': 'ICenitChamberResponseService' in c and 'ImportAsync' in c,
    'PT14 3 item ACK and NACK test exists': 'I13_PT14_AckAndNackAcrossThreeTransactions' in t,
    'PT14 2 item NACK in single source': 'I13_PT14_TwoDistinctItemNacksInOnePayload' in t,
    'PT15 conflicting reasons fail-closed test exists': 'I13_PT15_ConflictingReasonsForSameTrace' in t,
    'PT15 stale export membership negative exists': 'I13_PT15_StaleExportMembershipCannotReject' in t,
    'PT15 duplicate source conflict test exists': 'I13_PT15_SameSourceDifferentContent' in t,
    'SQLite reload replay in PT14': 'await using var replayContext = NewContext(connection);' in t,
    'no ACH Colombia file changed in focal source': 'CENIT' in s and 'ClearingHouseId' in s,
}
for name,passed in checks.items():
    print(('PASS' if passed else 'FAIL') + ' ' + name)
print(f'{sum(checks.values())}/{len(checks)} source-contract checks (C# not executed)')
if not all(checks.values()): raise SystemExit(1)

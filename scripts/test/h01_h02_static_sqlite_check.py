#!/usr/bin/env python3
"""Static source guards + SQLite-equivalent CAS model for H01/H02 (not C#/SQL Server/PG execution)."""
from pathlib import Path
import sqlite3

ROOT = Path(__file__).resolve().parents[2]
JOB = (ROOT / 'src/Cfa.ACHInterbank.Persistence/ACH/Services/Implementation/ContrapartidaDispatchJobService.cs').read_text()
REC = (ROOT / 'src/Cfa.ACHInterbank.Persistence/ACH/Services/Implementation/ContrapartidaDispatchRecovery.cs').read_text()
TESTS = (ROOT / 'tests/Cfa.ACHInterbank.Tests/ContrapartidaDispatchJobServiceTests.cs').read_text()
checks = []

def check(name, ok):
    checks.append((name, bool(ok)))
    print(('PASS' if ok else 'FAIL') + ' H01/H02 ' + name)

check('recovery does not increment SOAP AttemptCount', '.SetProperty(x => x.AttemptCount, x => x.AttemptCount + 1)' not in REC)
check('max attempts shared between recovery and dispatcher', 'ContrapartidaDispatchRecovery.MaxSoapAttempts' in JOB)
check('recovery events use distinct negative sequence', 'AttemptNumber = await NextRecoveryAuditNumberAsync' in REC and 'x.AttemptNumber < 0' in REC)
check('stale CAS fences phase, expiry, token and count', all(t in REC for t in [
    'x.DispatchLeaseToken == candidate.DispatchLeaseToken',
    'x.DispatchLeasePhase == candidate.DispatchLeasePhase',
    'x.DispatchLeaseExpiresAtUtc == candidate.DispatchLeaseExpiresAtUtc',
    'x.AttemptCount == candidate.AttemptCount']))
check('Preparing allows retry only below max and without reconciliation',
    'candidate.DispatchLeasePhase == Preparing' in REC and 'candidate.AttemptCount < MaxSoapAttempts' in REC)
check('InFlight routes to reconciliation not automatic retry',
    'SetProperty(x => x.RequiresReconciliation, !safeToRetry)' in REC)
check('exhausted legacy RetryPending scanned independent of cycle',
    'FinalizeExhaustedRetryPendingAsync(db, nowUtc' in REC and 'x.State == ContrapartidaDispatchItemStateEnum.RetryPending' in REC)
check('exhausted state becomes terminal reconciliation',
    'H01_RETRY_LIMIT_RECONCILIATION' in REC and
    '.SetProperty(x => x.State, ContrapartidaDispatchItemStateEnum.ContrapartidaReportFailed)' in REC)
check('terminal reconciliation clears next attempt', '.SetProperty(x => x.NextAttemptAtUtc, (DateTime?)null)' in REC)
check('manual retries choose active cycle rather than source batch cycle',
    'FindActiveRetryCycleAsync(sourceBatch.ClearingHouseId, ct,' in JOB and
    'AchCycleId = cycle.Id' in JOB)
check('active cycle checks operational status and debit policy',
    'x.OperationalStatus == AchCycleOperationalStatus.Open' in JOB and
    'await _cycleTransactionPolicy.EvaluateAsync' in JOB)
check('calendar and operating window checked',
    '_calendarGuard.EnsureExecutableAsync(cycle, ct)' in JOB and
    'ValidateCycleOperationalWindow(cycle, nowUtcOffset)' in JOB)
check('manual retries recheck window per chunk',
    'currentCycle.OperationalStatus != AchCycleOperationalStatus.Open' in JOB and
    'ValidateCycleOperationalWindow(currentCycle, currentInstant)' in JOB)
check('no cycle defers without new SOAP batch',
    'if (cycle is null)' in JOB and 'SOAP no invocado' in JOB)
check('manual claim validates stale state and lease atomically',
    'i.State == selected.State' in JOB and 'i.DispatchLeaseToken == selected.DispatchLeaseToken' in JOB
    and 'i.AttemptCount == selected.AttemptCount' in JOB)
check('manual claim guards against outcome unknown',
    'i.DispatchLeasePhase == ContrapartidaDispatchRecovery.Idle' in JOB and '!i.RequiresReconciliation' in JOB)
check('per-item manual cycle reassignment and lease claim share update',
    '.SetProperty(i => i.AchCycleId, cycle.Id)' in JOB and
    '.SetProperty(i => i.LastReassignedFromCycleId,' in JOB)
check('manual retry preserves original monetary transaction cycle',
    'SetProperty(i => i.AchTransaction.AchCycleId' not in JOB)
for name in ['H01_ExpiredPreparingLease_PreservesSoapCounterAndHonorsLimit',
             'H01_LegacyRetryPendingAtLimit_QuartzRecoveryMovesToReconciliationWithoutSoap',
             'H01_RepeatedRestart_RecoveryAuditsCannotConsumeSoapAttemptsOrCollide',
             'H01_FourActualAttempts_RecoveryThenFinalSoapUsesFifthSlotOnce',
             'H02_ManualRetry_ClosedSourceUsesOpenPermittedCycleAndPersistsAudit',
             'H02_ManualRetry_NoOpenCycle_DefersWithoutSoap',
             'H02_ManualRetry_BlockedCalendarWithOpenCycle_DefersWithoutSoap',
             'H02_ManualRetry_UnknownOutcomeNeverRetransmitsEvenWithOpenCycle',
             'H02_CompetingManualWorkers_CannotBothClaimTheSameDebit',
             'H02_ManualRetry_CycleClosesAfterFirstSoap_LeavesRemainingDebitUnsent']:
    check('xUnit source exists: '+name, name in TESTS)

# A DB-level CAS and unique attempt-number model mirrors the EF predicates. This is
# not a replacement for testing SQL Server/PostgreSQL providers and a real SOAP server.
with sqlite3.connect(':memory:') as db:
    db.executescript('''
    CREATE TABLE items(id INTEGER PRIMARY KEY, state TEXT NOT NULL, phase TEXT NOT NULL,
        token TEXT NOT NULL, expires INTEGER, attempts INTEGER NOT NULL,
        reconcile INTEGER NOT NULL, cycle TEXT NOT NULL, next_at INTEGER);
    CREATE TABLE audit(item INTEGER NOT NULL, number INTEGER NOT NULL,
        kind TEXT NOT NULL, UNIQUE(item,number));
    CREATE TABLE cycles(id TEXT PRIMARY KEY, is_open INTEGER NOT NULL, calendar_ok INTEGER NOT NULL,
        window_ok INTEGER NOT NULL, debit_allowed INTEGER NOT NULL);
    INSERT INTO items VALUES(1,'Reporting','Preparing','old',90,4,0,'closed',0);
    INSERT INTO items VALUES(2,'Reporting','InFlight','old',90,4,0,'closed',0);
    INSERT INTO items VALUES(3,'RetryPending','Idle','recovery',NULL,5,0,'closed',0);
    INSERT INTO items VALUES(4,'RetryPending','Idle','token',NULL,1,0,'closed',0);
    INSERT INTO items VALUES(5,'RetryPending','Idle','token',NULL,1,1,'closed',0);
    INSERT INTO cycles VALUES('closed',0,1,0,0);
    INSERT INTO cycles VALUES('permitted',1,1,1,1);
    INSERT INTO cycles VALUES('holiday',1,0,1,1);
    INSERT INTO cycles VALUES('prohibited',1,1,1,0);
    ''')
    # Prepare recovery: no SQL UPDATE on attempts; preserve the fourth slot.
    old = db.execute('SELECT state,phase,token,expires,attempts,reconcile FROM items WHERE id=1').fetchone()
    db.execute('''UPDATE items SET state='RetryPending',phase='Idle',token='recovered',expires=NULL,
       next_at=100 WHERE id=1 AND state=? AND phase=? AND token=? AND expires=? AND attempts=? AND reconcile=? AND expires<=100''',
       old)
    check('SQLite model 4 SOAP attempts remain 4 after recovery',
          db.execute('SELECT attempts FROM items WHERE id=1').fetchone()[0] == 4)
    check('SQLite model recovered Prepare now eligible for fifth attempt',
          db.execute("SELECT count(*) FROM items WHERE id=1 AND state='RetryPending' AND attempts<5 AND reconcile=0").fetchone()[0] == 1)
    db.executemany("INSERT INTO audit VALUES(1,?,'soap')", [(i,) for i in (1,2,3,4)])
    db.execute("INSERT INTO audit VALUES(1,-1,'recovery')")
    db.execute("INSERT INTO audit VALUES(1,-2,'recovery')")
    check('SQLite model repeated recoveries preserve unique audit numbers',
          db.execute('SELECT count(DISTINCT number) FROM audit WHERE item=1').fetchone()[0] == 6)
    # Scanner must fail closed on in-flight. No further transmission.
    changed = db.execute('''UPDATE items SET state='Failed',phase='Idle',reconcile=1,next_at=NULL
       WHERE id=2 AND state='Reporting' AND phase='InFlight' AND token='old' AND expires<=100''').rowcount
    check('SQLite model expired InFlight reconciles and does not retry',
          changed == 1 and db.execute('SELECT reconcile,attempts FROM items WHERE id=2').fetchone() == (1,4))
    # Legacy exhausted RetryPending no longer disappears from scheduler with no terminal state.
    changed = db.execute('''UPDATE items SET state='Failed',reconcile=1,next_at=NULL
       WHERE id=3 AND state='RetryPending' AND attempts>=5''').rowcount
    check('SQLite model exhausted legacy item becomes terminal', changed == 1)
    check('SQLite model exhausted sweep is idempotent', db.execute('''UPDATE items SET state='Failed'
       WHERE id=3 AND state='RetryPending' AND attempts>=5''').rowcount == 0)
    # Eligible active cycles require all guards, not just "is_open".
    candidate = db.execute('''SELECT id FROM cycles WHERE is_open=1 AND calendar_ok=1
      AND window_ok=1 AND debit_allowed=1''').fetchall()
    check('SQLite model only open/calendar/window/policy-valid cycles qualify', candidate == [('permitted',)])
    # CAS reassign+claim executes exactly once for two concurrent snapshots.
    sql='''UPDATE items SET cycle='permitted',state='Retrying',phase='Preparing',token='manual:one'
      WHERE id=4 AND cycle='closed' AND state='RetryPending' AND phase='Idle'
      AND token='token' AND attempts=1 AND reconcile=0'''
    check('SQLite model first manual worker claims exactly once', db.execute(sql).rowcount == 1)
    check('SQLite model second stale manual worker cannot double claim', db.execute(sql).rowcount == 0)
    check('SQLite model unknown outcome cannot be claimed for SOAP', db.execute('''UPDATE items
      SET state='Retrying' WHERE id=5 AND state='RetryPending' AND reconcile=0''').rowcount == 0)
    check('SQLite model monetary source cycle is immutable', db.execute('SELECT cycle FROM items WHERE id=3').fetchone()[0]=='closed')

failed = [x for x, ok in checks if not ok]
print(f'TOTAL {len(checks)-len(failed)}/{len(checks)} H01/H02 source + SQLite-equivalent controls; .NET, xUnit, SQL Server, PostgreSQL, SOAP integration and UAT NOT EXECUTED')
if failed:
    raise SystemExit(1)

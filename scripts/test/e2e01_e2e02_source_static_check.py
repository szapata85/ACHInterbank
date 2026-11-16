#!/usr/bin/env python3
"""E2E-01/02 source controls and SQLite equivalent SQL-model tests.

NOT a .NET compilation/test, NOT a SQL Server/PostgreSQL runtime test.
"""
from pathlib import Path
import sqlite3
import tempfile
import threading

ROOT = Path(__file__).resolve().parents[2]
PERSIST = ROOT / 'src/Cfa.ACHInterbank.Persistence'
SERV = PERSIST / 'ACH/Services/Implementation'
TESTS = ROOT / 'tests/Cfa.ACHInterbank.Tests'
PASS = 0


def check(name, condition):
    global PASS
    if not condition:
        raise AssertionError(f'FAIL: {name}')
    PASS += 1
    print(f'{PASS:02} PASS SOURCE/SQLMODEL {name}')


def get(relative):
    return (ROOT / relative).read_text(encoding='utf-8-sig')


idempotency = (SERV / 'TransactionIdempotency.cs').read_text()
service = (SERV / 'AchTransactionService.cs').read_text()
policy = (SERV / 'TransactionPolicyService.cs').read_text()
persister = (SERV / 'TransactionPersister.cs').read_text()
ctx = (PERSIST / 'DataBase/AchDbContext.cs').read_text()
job = (SERV / 'ContrapartidaDispatchJobService.cs').read_text()
recovery = (SERV / 'ContrapartidaDispatchRecovery.cs').read_text()
quartz = get('src/Cfa.ACHInterbank.Api/Jobs/ContrapartidaDispatchJob.cs') if (ROOT / 'src/Cfa.ACHInterbank.Api/Jobs/ContrapartidaDispatchJob.cs').exists() else ''
pg = (PERSIST / 'DataBase/Migrations/Postgres/20261115170000_E2E01IdempotencyE2E02ContrapartidaLease.cs').read_text()
sql = get('src/Cfa.ACHInterbank.Persistence.Migrations.SqlServer/DataBase/Migrations/SqlServer/20261115170000_E2E01IdempotencyE2E02ContrapartidaLease.cs')
controller = next((p.read_text() for p in ROOT.rglob('TransactionsController.cs')), '')
check('hashes complete immutable instruction incl. amount and addendas', 'SHA256.HashData' in idempotency and 'request.Addendas' in idempotency and 'request.Amount.ToString' in idempotency)
check('idempotency key origin-scoped', 't.SourceInstitutionId == originId' in idempotency and 'TransactionExternalId == id' in idempotency)
check('legacy instructions fail closed', 'E2E01_LEGACY_RECONCILIATION' in idempotency)
check('changed instruction is conflict', 'E2E01_IDEMPOTENCY_CONFLICT' in idempotency)
check('historical duplicate detection', 'E2E01_HISTORICAL_DUPLICATES' in idempotency)
check('creation preflight before policy', service.index('TransactionIdempotency.ResolveOriginatingInstitutionAsync') < service.index('_transactionPolicyService.PreviewAsync'))
check('creation recheck inside EF transaction', service.index('await using var dbTransaction') < service.index('var alreadyCommitted = await TransactionIdempotency.FindAsync'))
check('unique collision resolves winner', 'RelationalDatabaseExceptionClassifier.IsUniqueViolation(ex)' in service and 'var winner = await TransactionIdempotency.FindAsync' in service)
check('persister stores fingerprint', 'RequestFingerprint = TransactionIdempotency.Fingerprint(request)' in persister)
check('policy not restricted to current cycle for duplicate', 'originIds' in policy and 't.SourceInstitutionId == originIds[0]' in policy)
check('EF filtered unique key', 'UX_AchTransactions_SourceInstitution_ExternalId' in get('src/Cfa.ACHInterbank.Persistence/Configuration/AchTransactionConfiguration.cs') and 'HasFilter(isPostgres' in ctx)
check('HTTP controlled 409 conflict', 'TransactionIdempotencyConflictException' in controller and 'Conflict' in controller)
check('PG real migration unique index', 'migrationBuilder.CreateIndex' in pg and 'unique: true' in pg and 'SourceInstitutionId' in pg)
check('SQL Server real migration unique index', 'migrationBuilder.CreateIndex' in sql and 'unique: true' in sql and 'SourceInstitutionId' in sql)
check('PG duplicates abort DDL without deletes', 'RAISE EXCEPTION' in pg and 'HAVING COUNT(*) > 1' in pg and 'DELETE' not in pg)
check('SQL Server duplicates abort DDL without deletes', 'THROW 52001' in sql and 'HAVING COUNT(*) > 1' in sql and 'DELETE' not in sql)
check('both providers persist lease and reconciliation', all(x in pg and x in sql for x in ('DispatchLeaseToken','DispatchLeaseExpiresAtUtc','DispatchLeasePhase','RequiresReconciliation')))
check('legacy in-flight fail closed in both providers', "'InFlight'" in pg and "InFlight" in sql and 'ReportingContrapartida' in pg and 'ReportingContrapartida' in sql)
check('recovery scans expired reporting and retrying', 'ReportingContrapartida' in recovery and 'Retrying' in recovery and 'DispatchLeaseExpiresAtUtc <= nowUtc' in recovery)
check('recovery claims with compare-and-swap', 'x.DispatchLeaseToken == candidate.DispatchLeaseToken' in recovery and '.ExecuteUpdateAsync(' in recovery)
check('pre-transport preparing recoverable', 'candidate.DispatchLeasePhase == Preparing' in recovery and 'RetryPending' in recovery)
check('unknown outcome means reconciliation', 'RequiresReconciliation, !safeToRetry' in recovery and 'OUTCOME_UNKNOWN' in recovery)
check('recovery audit attempt recorded', 'db.ContrapartidaDispatchAttempts.Add(' in recovery)
check('SOAP boundary persisted before I/O', 'ContrapartidaDispatchRecovery.PersistIntentBeforeTransportAsync(' in job and job.index('PersistIntentBeforeTransportAsync(') < job.index('var dispatchResult = await DispatchProcContrapartidasAsync('))
check('recovery independently scans Quartz even outside cycles', 'RecoverStaleAsync' in ''.join(p.read_text() for p in ROOT.rglob('ContrapartidaDispatchJobHandler.cs')) or 'RecoverStaleAsync' in ''.join(p.read_text() for p in ROOT.rglob('ContrapartidaDispatchQuartzJob.cs')) or 'RecoverStaleAsync' in ''.join(p.read_text() for p in ROOT.rglob('*Contrapartida*Job*.cs')))
check('scheduled and manual both acquire lease', job.count('.SetProperty(i => i.DispatchLeasePhase, ContrapartidaDispatchRecovery.Preparing)') == 2)
check('scheduled and manual both block reconciliation', job.count('&& !i.RequiresReconciliation') >= 2)
check('unknown live transport fails closed', 'liveTransportAttempted && status == IntegrationTransportStatus.Failed' in job and job.count('catalogResult is null || !catalogResult.IsKnownCode') == 2)
check('idempotency + SQL concurrency xUnit source exists', 'E2E01_RelationalUniqueIndex_SerializesConcurrentInsertsWithinOriginScope' in (TESTS / 'AchTransactionNachaTests.cs').read_text())
check('E2E02 restart and concurrent workers xUnit source exists', 'E2E02_TwoQuartzWorkers_CannotClaimSameExpiredDispatch' in (TESTS / 'ContrapartidaDispatchJobServiceTests.cs').read_text() and 'E2E02_ExpiredLease_SeparatesPreSendFromUnknownAfterRestart' in (TESTS / 'ContrapartidaDispatchJobServiceTests.cs').read_text())

# Pure SQLite equivalent of the uniqueness and CAS claim, not an implementation execution.
with tempfile.NamedTemporaryFile(suffix='.sqlite') as temp:
    path = temp.name
    con = sqlite3.connect(path)
    con.executescript('''CREATE TABLE Transactions (Origin INTEGER NOT NULL, ExternalId TEXT, Payload TEXT);
    CREATE UNIQUE INDEX UX ON Transactions(Origin,ExternalId) WHERE ExternalId IS NOT NULL AND ExternalId <> '';
    CREATE TABLE Item (Id INTEGER PRIMARY KEY, State TEXT, Token TEXT, Phase TEXT, Expire INTEGER, Recon INTEGER);''')
    con.execute("INSERT INTO Transactions VALUES (1,'A','X')")
    try:
        con.execute("INSERT INTO Transactions VALUES (1,'A','X')")
        unique_blocked=False
    except sqlite3.IntegrityError:
        unique_blocked=True
    check('SQLite equivalent rejects identical origin and key', unique_blocked)
    con.execute("INSERT INTO Transactions VALUES (2,'A','X')")
    check('SQLite equivalent allows independent originating scope', con.execute("SELECT COUNT(*) FROM Transactions WHERE ExternalId='A'").fetchone()[0] == 2)
    con.execute("INSERT INTO Item VALUES (1,'ReportingContrapartida','worker-old','InFlight',0,0)")
    updated = con.execute("UPDATE Item SET State='ContrapartidaReportFailed', Token='recovery-new',Recon=1 WHERE Id=1 AND State='ReportingContrapartida' AND Token='worker-old' AND Expire <= 1").rowcount
    old = con.execute("UPDATE Item SET Phase='InFlight' WHERE Id=1 AND State='ReportingContrapartida' AND Token='worker-old'").rowcount
    second = con.execute("UPDATE Item SET State='RetryPending' WHERE Id=1 AND State='ReportingContrapartida' AND Token='worker-old'").rowcount
    check('SQLite equivalent atomic recovery selects exactly one claimant', updated == 1 and second == 0)
    check('SQLite equivalent prevents old worker from transport boundary', old == 0)
    check('SQLite equivalent retains reconciliation flag', con.execute('SELECT Recon FROM Item WHERE Id=1').fetchone()[0] == 1)
    con.close()

print(f'TOTAL {PASS}/{PASS} PASS: SOURCE STATIC + SQLITE MODEL; .NET BUILD/XUNIT/PG/SQLSERVER/UAT NOT EXECUTED')

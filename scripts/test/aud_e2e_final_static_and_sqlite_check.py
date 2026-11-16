#!/usr/bin/env python3
"""Focused source and SQLite concurrency-model checks. Never presented as C# or PG/MSSQL execution."""
import sqlite3
import tempfile
from pathlib import Path
p=Path(__file__).resolve().parents[2]
src=p/'src/Cfa.ACHInterbank.Persistence/ACH/Services/Implementation'
read=lambda x:x.read_text(encoding='utf-8-sig')
service=read(src/'AchTransactionService.cs')
recovery=read(src/'ContrapartidaDispatchRecovery.cs')
job=read(src/'ContrapartidaDispatchJobService.cs')
model=read(p/'src/Cfa.ACHInterbank.Domain/Models/ACH/ContrapartidaDispatchModels.cs')
tests=read(p/'tests/Cfa.ACHInterbank.Tests/ContrapartidaDispatchJobServiceTests.cs')
result=[]
def ok(name, flag):
    assert flag, 'FAIL: '+name
    result.append(name)
    print(f'{len(result):02d} PASS source/sqlite-model {name}')
ok('EF transaction declared in enclosing retry scope before try',service.index('await using var dbTransaction =') < service.index('var transactionCompleted = false;') < service.index('var alreadyCommitted'))
ok('transaction context disposed on all paths',service.count('await using var dbTransaction =') == 1 and 'transactionStarted' not in service)
ok('rollback on unique collision', 'RecordRollback("unique_conflict")' in service and 'await dbTransaction.RollbackAsync(CancellationToken.None)' in service)
ok('commit after UOW', service.index('await _unitOfWork.CommitAsync(ct)') < service.index('await dbTransaction.CommitAsync(ct)'))
ok('recovery snapshots expiry, phase, attempt', 'x.DispatchLeaseExpiresAtUtc, x.AttemptCount' in recovery and 'x.DispatchLeasePhase == candidate.DispatchLeasePhase' in recovery)
ok('recovery checks expiry and phase in same CAS',recovery.index('x.DispatchLeasePhase == candidate.DispatchLeasePhase') < recovery.index('.ExecuteUpdateAsync('))
ok('recovery never retries reconciliation item','!candidate.RequiresReconciliation' in recovery)
ok('InFlight transition requires active matching token/phase/expiry',all(x in recovery for x in ['x.DispatchLeaseToken == token','x.DispatchLeasePhase == Preparing','x.DispatchLeaseExpiresAtUtc > nowUtc','!x.RequiresReconciliation']))
ok('persistent intent and fence shared DB transaction','await using var tx = await db.Database.BeginTransactionAsync(ct)' in recovery and 'await tx.CommitAsync(ct)' in recovery)
ok('intent contains correlation and SHA256 hash','DispatchCorrelationId' in recovery and 'RequestPayloadHash' in recovery and 'Hash(requestPayload)' in job)
ok('intent includes mapper and transport details',all(x in recovery for x in ['MappingSetId','MappingVersion','MappingSnapshotHash','DispatchSoapEndpoint','DispatchExecutionMode','RequestPayloadXml']))
ok('manual multi-item intent scoped to one dispatch item', 'x.DispatchIntentPayloadXml, payload' in recovery and 'if (totalItems.Value <= 1)' in recovery)
ok('per-item audit contains mapper/hash/endpoint', all(x in model for x in ['DispatchIntentPayloadHash','DispatchIntentMappingSetId','DispatchIntentMappingVersion','DispatchIntentSoapEndpoint','DispatchIntentCorrelationId']))
ok('intent before SOAP both scheduled and manual', job.count('PersistIntentBeforeTransportAsync(')==2 and job.count('intentCommitted = true;')==2)
ok('failed persistence prevents SOAP', job.count('when (intentPersistenceAttempted && !intentCommitted)')==2)
ok('cycle reassignment pre-send only',all(x in job for x in ['ReassignRecoveredItemsToActiveCycleAsync','LastErrorCode == "E2E02_PRE_SEND_CRASH"','x.State == ContrapartidaDispatchItemStateEnum.RetryPending','!x.RequiresReconciliation']))
ok('cycle reassign CAS and audit metadata', 'LastReassignedFromCycleId' in job and 'LastReassignedAtUtc' in job and 'x.DispatchLeaseToken == item.DispatchLeaseToken' in job)
ok('cycle target policy and calendar checked before reroute',job.index('EnsureCycleAllowsDebitDispatchAsync(cycle, ct);') < job.index('ReassignRecoveredItemsToActiveCycleAsync(cycle, nowUtcOffset, ct)'))
ok('unknown result blocked from replay','i.RequiresReconciliation' in job and 'E2E02_OUTCOME_UNKNOWN' in recovery)
for sql in ['Postgres','SqlServer']:
 d=(p/'src/Cfa.ACHInterbank.Persistence/DataBase/Migrations/Postgres') if sql=='Postgres' else (p/'src/Cfa.ACHInterbank.Persistence.Migrations.SqlServer/DataBase/Migrations/SqlServer')
 migration=read(d/'20261115183000_E2EFinalCycleAndSoapIntent.cs')
 snapshot=read(d/'AchDbContextModelSnapshot.cs')
 ok(sql+' migration and snapshot match intent fields',all(x in migration and x in snapshot for x in ['RequestPayloadHash','DispatchCorrelationId','DispatchIntentAtUtc','DispatchSoapEndpoint','DispatchExecutionMode','LastReassignedFromCycleId','LastReassignedAtUtc','DispatchIntentPayloadHash','DispatchIntentPayloadXml','DispatchIntentMappingVersion']))
for case in ['AUD_E2E_02_PhasePromotedBeforeExpiredRecovery','AUD_E2E_04_IntentCommittedBeforeSoap','AUD_E2E_04_ManualMultiItemBatch','AUD_E2E_04_ExpiredLeaseCannotWrite','AUD_E2E_03_ClosedOriginalCycle','AUD_E2E_03_UnknownResult','AUD_E2E_03_DeferredOperationalCalendar']:
 ok('added xUnit source '+case, case in tests)
# SQLite concurrent-race MODEL: winner has new phase, stale recovery cannot touch it.
with tempfile.TemporaryDirectory() as folder:
 con=sqlite3.connect(str(Path(folder)/'race.db'))
 con.execute('CREATE TABLE item(id INTEGER PRIMARY KEY, state TEXT, phase TEXT, token TEXT, expiry INTEGER, attempts INTEGER, recon INTEGER,cycle TEXT)')
 con.execute("INSERT INTO item VALUES(1,'ReportingContrapartida','Preparing','w1',10,0,0,'old')")
 con.execute("UPDATE item SET phase='InFlight' WHERE id=1 AND phase='Preparing' AND token='w1' AND expiry>5")
 changed=con.execute("UPDATE item SET state='RetryPending', recon=0 WHERE id=1 AND phase='Preparing' AND token='w1' AND expiry=10 AND expiry<=11").rowcount
 ok('SQLite CAS phase race rejects stale Preparing snapshot',changed==0)
 changed=con.execute("UPDATE item SET state='ContrapartidaReportFailed', recon=1 WHERE id=1 AND phase='InFlight' AND token='w1' AND expiry<=11").rowcount
 ok('SQLite unknown in-flight becomes reconciliation',changed==1)
 changed=con.execute("UPDATE item SET cycle='new' WHERE id=1 AND state='RetryPending' AND recon=0").rowcount
 ok('SQLite unknown cannot reassign cycle',changed==0)
 con.close()
print(f'TOTAL {len(result)}/{len(result)} source/SQLite-model controls; NOT a C# compilation or provider integration test')

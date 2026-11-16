#!/usr/bin/env python3
"""RES-H02-01/02 static source assertions + separate SQLite CAS model.

Does NOT compile C#, exercise EF providers, or invoke real SOAP.
"""
from pathlib import Path
import sqlite3, tempfile
import os

root=Path(__file__).resolve().parents[2]
job=(root/'src/Cfa.ACHInterbank.Persistence/ACH/Services/Implementation/ContrapartidaDispatchJobService.cs').read_text()
rec=(root/'src/Cfa.ACHInterbank.Persistence/ACH/Services/Implementation/ContrapartidaDispatchRecovery.cs').read_text()
test=(root/'tests/Cfa.ACHInterbank.Tests/ContrapartidaDispatchJobServiceTests.cs').read_text()
checks=[]
def check(name, value):
    checks.append(bool(value))
    print(('PASS' if value else 'FAIL')+' RES-H02 '+name)

scheduled=job.split('private async Task<ContrapartidaCycleDispatchResult> ProcessPendingAsync(',1)[1].split('public async Task<ContrapartidaBatchRetryResult> RetryBatchAsync(',1)[0]
manual=job.split('public async Task<ContrapartidaBatchRetryResult> RetryBatchAsync(',1)[1].split('private async Task<ProcContrapartidasDispatchExecutionResult> DispatchProcContrapartidasAsync(',1)[0]
helper=job.split('private async Task<bool> IsCycleAndClaimExecutableBeforeSoapAsync(',1)[1].split('private async Task<AchCycle?> FindActiveRetryCycleAsync(',1)[0]
check('scheduled claim CAS checks AchCycleId', '&& i.AchCycleId == cycle.Id && i.ClearingHouseId == cycle.ClearingHouseId' in scheduled.split('ExecuteUpdateAsync',1)[0])
check('scheduled claim requires Open DB status', 'i.AchCycle.OperationalStatus == AchCycleOperationalStatus.Open' in scheduled.split('ExecuteUpdateAsync',1)[0])
check('scheduled claim requires idle phase', 'i.DispatchLeasePhase == ContrapartidaDispatchRecovery.Idle' in scheduled.split('ExecuteUpdateAsync',1)[0])
check('scheduled claim readback checks matching lease/cycle/phase', '&& i.DispatchLeasePhase == ContrapartidaDispatchRecovery.Preparing' in scheduled)
check('scheduled CAS miss breaks without spin', 'Stop this invocation' in scheduled and 'break;' in scheduled.split('Stop this invocation',1)[1][:220])
check('scheduled pre-intent and post-intent checks', scheduled.count('IsCycleAndClaimExecutableBeforeSoapAsync(')==2)
check('manual pre-intent and post-intent checks', manual.count('IsCycleAndClaimExecutableBeforeSoapAsync(')==2)
check('both paths check Preparing and InFlight', all(s in scheduled and s in manual for s in ('ContrapartidaDispatchRecovery.Preparing','ContrapartidaDispatchRecovery.InFlight')))
check('fresh persisted cycle read', 'AsNoTracking()' in helper and 'SingleOrDefaultAsync(x => x.Id == cycleId' in helper)
check('fresh calendar policy and operational window', all(s in helper for s in ('ValidateCycleOperationalWindow(fresh, instant)','EnsureExecutableAsync(fresh, ct)','EnsureCycleAllowsDebitDispatchAsync(fresh, ct)')))
check('final CAS checks current persisted cycle and lease', all(s in helper for s in ('x.AchCycleId == cycleId', 'x.AchCycle.OperationalStatus == AchCycleOperationalStatus.Open','x.DispatchLeaseToken == token', 'x.DispatchLeasePhase == expectedPhase','x.DispatchLeaseExpiresAtUtc > instant.UtcDateTime','ExecuteUpdateAsync(')))
check('no SOAP on denied preflight', 'if (preSoapAborted)' in scheduled and 'if (preSoapAborted)' in manual)
check('abort CAS checks exact phase, token and cycle', all(s in rec for s in ('AbortUnsentCycleClaimAsync(', 'x.AchCycleId == cycleId','x.DispatchLeaseToken == token && x.DispatchLeasePhase == expectedPhase')))
check('abort restores RetryPending without consuming AttemptCount', 'SetProperty(x => x.State, ContrapartidaDispatchItemStateEnum.RetryPending)' in rec and 'SetProperty(x => x.DispatchLeasePhase, Idle)' in rec and '.SetProperty(x => x.AttemptCount,' not in rec)
check('aborted attempt is audited NotExecuted and negative', 'AttemptNumber = await NextRecoveryAuditNumberAsync(db, itemId, ct)' in rec and 'TransportStatus = IntegrationTransportStatus.NotExecuted' in rec)
check('unsent cycle code remains eligible for safe reroute', 'x.LastErrorCode == ContrapartidaDispatchRecovery.UnsentCycleCode' in job)
check('manual retry retains strict original cycle/lease CAS', 'i.AchCycleId == selected.AchCycleId' in manual and 'i.DispatchLeaseToken == selected.DispatchLeaseToken' in manual)
check('preflight abort cannot be treated as SOAP transport error', scheduled.count('catch (Exception) when (preSoapAbortAttempted)')==1 and manual.count('catch (Exception) when (preSoapAbortAttempted)')==1)
for name in ('RES_H02_ScheduledCycleClosesDuringMappingOrAfterIntent_NoSoapAndNoAttempt', 'RES_H02_ManualCycleClosesDuringPreparationOrAtFinalFence_SkipsDebit', 'RES_H02_CycleReassignedBetweenSelectionAndClaim_CasRejectsStaleWorker'):
    check('xUnit source '+name, name in test)

# SQLite DB model of cycle CAS and close-before-transport, NOT compiled EF Core.
f,dbpath=tempfile.mkstemp(prefix='res_h02_',suffix='.sqlite');os.close(f)
try:
    a=sqlite3.connect(dbpath);b=sqlite3.connect(dbpath)
    a.execute('create table cycles (id text primary key,status text)')
    a.execute('create table items (id integer primary key,cycle text, phase text, lease text, sent integer default 0)')
    a.executemany('insert into cycles values (?,?)',[('first','Open'),('second','Open')]);a.execute("insert into items values (1,'first','Idle','',0)");a.commit()
    selected=a.execute("select id from items where cycle='first' and phase='Idle'").fetchone()[0]
    b.execute("update items set cycle='second' where id=?",(selected,));b.commit()
    stale=a.execute("update items set phase='Preparing',lease='workerA' where id=? and cycle='first' and phase='Idle'",(selected,)).rowcount;a.commit()
    check('SQLite model rejects obsolete worker after reassignment',stale==0)
    fresh=b.execute("update items set phase='Preparing',lease='workerB' where id=? and cycle='second' and phase='Idle'",(selected,)).rowcount;b.commit()
    check('SQLite model permits exactly one owner for new cycle',fresh==1)
    collision=a.execute("update items set phase='Preparing',lease='workerC' where id=? and cycle='second' and phase='Idle'",(selected,)).rowcount;a.commit()
    check('SQLite model competing worker loses CAS',collision==0)
    b.execute("update items set phase='InFlight' where id=1 and phase='Preparing' and lease='workerB'");b.commit()
    a.execute("update cycles set status='Closed' where id='second'");a.commit()
    # Final-phase fence is atomic over cycle status and token.
    final=b.execute("update items set sent=sent where id=1 and phase='InFlight' and lease='workerB' and cycle=(select id from cycles where id='second' and status='Open')").rowcount;b.commit()
    check('SQLite final fence denies closed cycle',final==0)
    aborted=b.execute("update items set phase='Idle',lease='' where id=1 and phase='InFlight' and lease='workerB' and cycle='second'").rowcount;b.commit()
    check('SQLite model aborts only exact owned claim',aborted==1)
    check('SQLite model produces no monetarily sent item',b.execute('select sent from items').fetchone()[0]==0)
    a.close();b.close()
finally:
    os.unlink(dbpath)
print(f'TOTAL {sum(checks)}/{len(checks)} RES-H02 source/SQLite model; C#/.NET/SQL Server/PostgreSQL/SOAP not executed')
raise SystemExit(0 if all(checks) else 1)

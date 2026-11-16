#!/usr/bin/env python3
"""Focused source invariants + executable SQLite CAS MODEL; not .NET integration."""
from pathlib import Path
import sqlite3, tempfile, threading, json, uuid
ROOT = Path(__file__).resolve().parents[2]
SRC = ROOT / 'src'
F = {
 'cfa': SRC/'Cfa.ACHInterbank.Persistence/ACH/Services/Implementation/IncomingNachaPostProcessingPipeline.cs',
 'axon': SRC/'Cfa.ACHInterbank.Persistence/ACH/Services/Implementation/AxonNotificationClaimStore.cs',
 'notify': SRC/'Cfa.ACHInterbank.Application/ACH/Responses/Notification/Services/NotificarRespuestaAchUseCase.cs',
 'watch': SRC/'Cfa.ACHInterbank.Persistence/ACH/Services/Implementation/AxonNotificationRecoveryHostedService.cs',
 'parser': SRC/'Cfa.ACHInterbank.Persistence/ACH/Services/Implementation/ProcTransaccionesResponseParser.cs',
 'pre': SRC/'Cfa.ACHInterbank.Persistence/ACH/Services/Implementation/DifferentialPrenotificationResponseProcessor.cs',
}
S = {key: path.read_text(encoding='utf-8') for key,path in F.items()}
checks=[]
def check(name, result):
    checks.append({'name':name,'pass':bool(result)})
    print(('PASS ' if result else 'FAIL ')+name)

for name, snippets in {
 'CFA_SQL_fenced_finish':('internal async Task<bool> FinalizeClaimAsync','x.DispatchClaimToken == claimedToken','x.DispatchLeaseExpiresAtUtc > nowUtc','&& !x.RequiresReconciliation','BeginTransactionAsync','CommitAsync'),
 'CFA_prepared_request_fenced':('PersistPreparedRequestFencedAsync','RecordPreparedRequestAsync','MarkTransportIntentAsync'),
 'CFA_late_outcome_independent_context':('PreserveLateEvidenceAsync','new AchDbContext(options','NptLateOutcomeFenced'),
 'CFA_expired_lease_quarantine':('RecoverExpiredClaimsAsync','NPT_TRANSPORT_OUTCOME_UNKNOWN','NPT_PRETRANSPORT_REVIEW'),
 'CFA_exception_classification':('NPT_POST_TRANSPORT_UNCERTAIN','queue.RequiresReconciliation = true;','catch (InvalidOperationException ex)'),
 'Axon_fenced_finish':('TryFinalizeAsync','x.ClaimToken == token && x.LeaseExpiresAtUtc > nowUtc','BeginTransactionAsync','CommitAsync'),
 'Axon_preflight_fenced':('TryFinishPreflightAsync','AxonPreflightFenced','x.Version, nextVersion'),
 'Axon_version_concurrency':('x.Version, versionAfterFinalize','x.Version, versionAfterQuarantine'),
 'Axon_late_evidence_separated':('RecordLateEvidenceAsync','AxonLateOutcomeFenced','new AchDbContext('),
 'Axon_expired_watchdog':('QuarantineExpiredAsync','AxonLeaseExpired','RequiereRevisionManual'),
 'Axon_cancel_independent_token':('catch (OperationCanceledException)','CancellationToken.None','AXON_CANCELLED_OUTCOME_UNKNOWN'),
 'SOAP_reject_wrong_outer_operation':('Proc_TransaccionesResponse','WRONG_SOAP_OPERATION'),
 'SOAP_reject_spoofed_nested_codes':('resultElement.Descendants().Count(x => x.Name.LocalName == "RTAACH") != 1','MISSING_OR_AMBIGUOUS_RTAACH'),
 'SOAP_reject_faults':('SOAP_FAULT','DtdProcessing.Prohibit'),
 'Prenotification_physical_link_file_chamber':('headerScope.IncomingNachaFileIngestionId != link.IncomingNachaFileIngestionId','headerScope.ClearingHouseId != responseClearingHouseId'),
 'Prenotification_unique_final_link':('x.IsFinal','links.Count > 1','PrenotificationNachaMatch.Ambiguous'),
}.items():
    prefix=name.split('_')[0]
    subject=S[{'CFA':'cfa','Axon':'axon','SOAP':'parser','Prenotification':'pre'}[prefix]]
    if name == 'Axon_cancel_independent_token': subject=S['notify']
    check(name, all(snippet in subject for snippet in snippets))
check('Axon independent hosted worker registered', 'PeriodicTimer' in S['watch'] and 'QuarantineExpiredAsync' in S['watch']
    and 'AddHostedService<Cfa.ACHInterbank.Persistence.ACH.Services.Implementation.AxonNotificationRecoveryHostedService>'
    in (SRC/'Cfa.ACHInterbank.Persistence/DependencyInjectionService.cs').read_text())
check('Dedicated C# race/expiry tests authored', all((ROOT/'tests/Cfa.ACHInterbank.Tests'/f).exists()
    for f in ('NptFocalCfaFencingTests.cs','NptFocalAxonFencingTests.cs','NptFocalProcTransaccionesParserTests.cs')))

with tempfile.TemporaryDirectory(prefix='npt-focal-model-') as d:
    path=str(Path(d)/'control.sqlite')
    con=sqlite3.connect(path); con.execute('PRAGMA journal_mode=WAL')
    con.executescript('''
        CREATE TABLE money(id INTEGER PRIMARY KEY, status TEXT NOT NULL, token TEXT, lease INT, reconcile INT NOT NULL DEFAULT 0);
        INSERT INTO money VALUES(1,'Queued',NULL,NULL,0);
        CREATE TABLE attempt(id INTEGER PRIMARY KEY,status TEXT NOT NULL,token TEXT,lease INT);
        INSERT INTO attempt VALUES(7,'Pendiente',NULL,NULL);
        CREATE TABLE parent(id INTEGER PRIMARY KEY,status TEXT NOT NULL,version TEXT);
        INSERT INTO parent VALUES(7,'Homologada','v0');
        CREATE TABLE audit(id INTEGER PRIMARY KEY AUTOINCREMENT,code TEXT NOT NULL);
    '''); con.commit();con.close()
    lock=threading.Barrier(2)
    won=[]
    def claim_money(w):
        c=sqlite3.connect(path,timeout=15,isolation_level=None)
        lock.wait()
        affected=c.execute("UPDATE money SET status='Dispatching',token=?,lease=100 WHERE id=1 AND status='Queued' AND reconcile=0", (w,)).rowcount
        won.append(affected)
        c.close()
    workers=[threading.Thread(target=claim_money,args=(f'w{i}',)) for i in range(2)]
    for t in workers:t.start()
    for t in workers:t.join()
    c=sqlite3.connect(path,timeout=15)
    check('SQLite two CFA connections one claim',sorted(won)==[0,1])
    check('SQLite stale CFA token cannot finalize',c.execute("UPDATE money SET status='Confirmed' WHERE id=1 AND status='Dispatching' AND token='stale' AND lease>50 AND reconcile=0").rowcount==0)
    c.execute("UPDATE money SET status='Blocked',reconcile=1 WHERE id=1 AND status='Dispatching' AND lease<=101")
    check('SQLite quarantined CFA cannot be overwritten',c.execute("UPDATE money SET status='Confirmed' WHERE id=1 AND status='Dispatching' AND lease>50 AND reconcile=0").rowcount==0)
    check('SQLite quarantined CFA cannot be replayed',c.execute("UPDATE money SET status='Dispatching' WHERE id=1 AND status='Queued' AND reconcile=0").rowcount==0)
    # two Axon workers independently use same SQLite database; single CAS winner
    c.commit();c.close();lock=threading.Barrier(2);won.clear()
    def claim_axon(w):
        c=sqlite3.connect(path,timeout=15,isolation_level=None)
        lock.wait()
        won.append(c.execute("UPDATE attempt SET status='InFlight',token=?,lease=100 WHERE id=7 AND status='Pendiente'",(w,)).rowcount)
        c.close()
    workers=[threading.Thread(target=claim_axon,args=(f'ax{i}',)) for i in range(2)]
    for t in workers:t.start()
    for t in workers:t.join()
    c=sqlite3.connect(path,timeout=15)
    check('SQLite two Axon connections one claim',sorted(won)==[0,1])
    check('SQLite Axon preflight cannot override in-flight',c.execute("UPDATE attempt SET status='ErrorFuncional' WHERE id=7 AND status IN ('Pendiente','PendienteReintento')").rowcount==0)
    c.execute('BEGIN') if not c.in_transaction else None
    c.execute("UPDATE attempt SET status='RequiereRevisionManual' WHERE id=7 AND status='InFlight' AND lease<=101")
    c.execute("UPDATE parent SET status='RequiereRevisionManual', version='v1' WHERE id=7 AND status!='Notificada'")
    c.execute("INSERT INTO audit(code) VALUES('expired')");c.commit()
    check('SQLite Axon stale finish cannot override quarantine',c.execute("UPDATE attempt SET status='Exitosa' WHERE id=7 AND status='InFlight' AND lease>50").rowcount==0)
    check('SQLite Axon parent remains manual',c.execute("SELECT status FROM parent WHERE id=7").fetchone()[0]=='RequiereRevisionManual')
    c.close()

passes=sum(x['pass'] for x in checks)
print(f'TOTAL {passes}/{len(checks)} SOURCE + PYTHON_SQLITE_MODEL; .NET, SQLSERVER, POSTGRES, SOAP INTEGRATION NOT EXECUTED')
if __name__=='__main__':
    out=ROOT/'docs/cierre-focal-npt-2026-11-15/evidencias/VERIFICACION_FOCAL_FUENTE_MODELO.json'
    out.parent.mkdir(parents=True,exist_ok=True)
    out.write_text(json.dumps({'kind':'SOURCE_STATIC_AND_PYTHON_SQLITE_MODEL_NOT_DOTNET_TESTS','checks':checks,'pass':passes,'total':len(checks),'dotnet_executed':False},indent=2,ensure_ascii=False)+'\n')
    raise SystemExit(0 if passes==len(checks) else 1)

#!/usr/bin/env python3
"""NPT focal source checks + Python SQLite model. NOT dotnet/xUnit/provider execution."""
from pathlib import Path
import sqlite3
import json
import re
import sys

ROOT=Path(__file__).resolve().parents[2]
base=ROOT/'src'
paths={
 'pipeline':base/'Cfa.ACHInterbank.Persistence/ACH/Services/Implementation/IncomingNachaPostProcessingPipeline.cs',
 'parser':base/'Cfa.ACHInterbank.Persistence/ACH/Services/Implementation/ProcTransaccionesResponseParser.cs',
 'notifier':base/'Cfa.ACHInterbank.Application/ACH/Responses/Notification/Services/NotificarRespuestaAchUseCase.cs',
 'claim':base/'Cfa.ACHInterbank.Persistence/ACH/Services/Implementation/AxonNotificationClaimStore.cs',
 'transaction':base/'Cfa.ACHInterbank.Persistence/ACH/Services/Implementation/AchResponseTransactionalRunner.cs',
 'response':base/'Cfa.ACHInterbank.Application/ACH/Responses/Processing/Services/ProcesarRespuestaAchUseCase.cs',
 'prenota':base/'Cfa.ACHInterbank.Persistence/ACH/Services/Implementation/DifferentialPrenotificationResponseProcessor.cs',
 'readiness':base/'Cfa.ACHInterbank.Persistence/Integrations/Services/IntegrationMappingReadinessService.cs',
 'ingestion':base/'Cfa.ACHInterbank.Persistence/ACH/Services/Implementation/IncomingNachaIngestionAppService.cs',
 'nacha_parser':base/'Cfa.ACHInterbank.Persistence/ACH/Services/Implementation/NachaParserService.cs',
 'migration_pg':base/'Cfa.ACHInterbank.Persistence/DataBase/Migrations/Postgres/20261115210000_Npt03Npt04NachaDispatchClaim.cs',
 'migration_ms':base/'Cfa.ACHInterbank.Persistence.Migrations.SqlServer/DataBase/Migrations/SqlServer/20261115210000_Npt03Npt04NachaDispatchClaim.cs',
 'model':base/'Cfa.ACHInterbank.Domain/Models/ACH/IncomingNachaDispatchModels.cs',
 'axon_attempt':base/'Cfa.ACHInterbank.Domain/Models/ACH/AchResponseNotificationAttempt.cs',
 'di':base/'Cfa.ACHInterbank.Persistence/DependencyInjectionService.cs',
 'tests_ax':ROOT/'tests/Cfa.ACHInterbank.Tests/NotificarRespuestaAchUseCaseTests.cs',
 'tests_parser':ROOT/'tests/Cfa.ACHInterbank.Tests/ProcTransaccionesResponseParserTests.cs',
 'tests_soap':ROOT/'tests/Cfa.ACHInterbank.Tests/WscfaachSoapClientCharacterizationTests.cs',
}
s={k:v.read_text() for k,v in paths.items()}
results=[]
def check(name,ok):
    results.append([name, bool(ok)])
    print(('PASS' if ok else 'FAIL')+' NPT '+name)

check('complete solution', (ROOT/'ACHInterbank.sln').exists() and (ROOT/'tests/Cfa.ACHInterbank.Tests/Cfa.ACHInterbank.Tests.csproj').exists())
check('SQL conditional queue claim', all(x in s['pipeline'] for x in ['ExecuteUpdateAsync','x.QueueStatus == IncomingNachaDispatchQueueStatus.Queued','x.DispatchClaimToken','x.DispatchLeaseExpiresAtUtc','&& !x.RequiresReconciliation']))
check('atomic claim affected fence', 'if (affected != 1) return false;' in s['pipeline'])
check('fenced transport intent', 'NPT_CLAIM_FENCE_FAILED' in s['pipeline'] and 'MarkTransportIntentAsync(queue' in s['pipeline'])
check('pretransport/reconciliation distinction', all(x in s['pipeline'] for x in ['NPT_PRETRANSPORT_REVIEW','NPT_TRANSPORT_OUTCOME_UNKNOWN','RecoverExpiredClaimsAsync']))
check('no automatic retry for unknown bank outcome', 'var retryAllowed = false;' in s['pipeline'] and 'queue.RequiresReconciliation = true;' in s['pipeline'])
check('success requires parser and catalog', '&& parsed.IsSuccess' in s['pipeline'] and 'IsKnownCode: true' in s['pipeline'])
check('dry-run cannot claim real bank success', 'BankTransportNotExecuted' in s['pipeline'] and 'IntegrationTransportStatus.NotExecuted' in s['pipeline'])
check('planning failure captured', 'NPT_DISPATCH_PLANNING_FAILED' in s['pipeline'])
check('no invented NCTAORIG', 'PROC_TRANSACCIONES_NCTAORIG_NOT_HOMOLOGATED' in s['pipeline'] and 'PROC_TRANSACCIONES_NCTAORIG_SOURCE_INVALID' in s['readiness'])
check('SOAP 1.1 and 1.2 envelope enforced', 'http://schemas.xmlsoap.org/soap/envelope/' in s['parser'] and 'http://www.w3.org/2003/05/soap-envelope' in s['parser'])
check('SOAP operation enforced', 'contract + "Proc_TransaccionesResponse"' in s['parser'] and 'WRONG_SOAP_OPERATION' in s['parser'])
check('SOAP fault quarantined', 'SOAP_FAULT' in s['parser'] and 'MISSING_OR_AMBIGUOUS_RTAACH' in s['parser'])
check('no parser automatic timeout retry', 'var isRetryable = false;' in s['parser'])
check('response relational transaction', 'BeginTransactionAsync' in s['transaction'] and 'CommitAsync' in s['transaction'] and 'CreateExecutionStrategy()' in s['transaction'])
check('transaction registered for normal DI', 'IAchResponseTransactionalRunner' in s['di'] and 'AchResponseTransactionalRunner' in s['di'])
check('processing uses transaction runner', '_transactionalRunner.RunAsync' in s['response'] and 'ExecuteCoreAsync' in s['response'])
check('no premature prenotification match', 'if (prenotificationProcessing.Success)' in s['response'] and 'VinculoPrenotificacionNachaValidado' in s['response'])
check('prenota chamber before match', 'responseClearingHouseId' in s['prenota'] and 'x.AchCycle.ClearingHouseId == responseClearingHouseId' in s['prenota'])
check('prenota only validated match', 'NPT-12' in s['prenota'] and 'CorrelationStatus = AchResponseCorrelationStatus.Matched' in s['prenota'])
check('Axon uses ExecuteUpdate CAS', s['claim'].count('ExecuteUpdateAsync') >= 3 and 'x.ClaimToken == token' in s['claim'])
check('Axon claim DI', 'IAxonNotificationClaimStore' in s['di'] and 'AxonNotificationClaimStore' in s['di'])
check('Axon no claim store fails closed', 'AXON_CLAIM_STORE_UNAVAILABLE' in s['notifier'])
check('Axon transport intent before remote gateway', s['notifier'].index('MarkTransportIntentAsync(attempt.Id') < s['notifier'].index('_gateway.RegistrarRespuestaAsync(cmd'))
check('Axon cancel persists independently', 'catch (OperationCanceledException)' in s['notifier'] and 'await _unitOfWork.CommitAsync(CancellationToken.None)' in s['notifier'])
check('Axon in-flight leases quarantined', 'RequiereRevisionManual' in s['claim'] and 'QuarantineExpiredAsync' in s['claim'])
check('ax attempt persistence model', all(x in s['axon_attempt'] for x in ['ClaimToken','LeaseExpiresAtUtc','TransportStartedAtUtc']))
check('NACHA errors and warnings separated', 'ParserWarningsJson = "[]"' in s['ingestion'] and 'WarningCount = 0,' in s['nacha_parser'])
check('NACHA physical count protected', all(x in s['ingestion'] for x in ['parseResult.TotalEntries','parseResult.ValidEntries','parseResult.InvalidEntries']))
check('Postgres migration nullable claim and historical backfill', 'NPT_LEGACY_OUTCOME_UNKNOWN' in s['migration_pg'] and 'timestamp with time zone' in s['migration_pg'])
check('SQL Server migration nullable claim and historical backfill', 'NPT_LEGACY_OUTCOME_UNKNOWN' in s['migration_ms'] and 'datetime2' in s['migration_ms'])
check('both migration identifiers agree', '[Migration("20261115210000_Npt03Npt04NachaDispatchClaim")]' in s['migration_pg'] and '[Migration("20261115210000_Npt03Npt04NachaDispatchClaim")]' in s['migration_ms'])
check('xunit parser rejection source exists', 'Parse_RejectsFalseSuccessFromWrongEnvelopeOrOperation' in s['tests_parser'])
check('xunit physical HTTP counting source exists', 'Npt06_HttpGatewayTimeout_DoesNotDuplicatePhysicalPost' in s['tests_soap'])
check('xunit fake concurrent Axon source exists', 'Npt08_TwoConcurrentNotifications_WithAtomicClaimFake_CallGatewayOnce' in s['tests_ax'])
check('xunit cancel Axon source exists', 'Npt09_CancellationAfterAxonTransport_UsesIndependentTokenToQuarantine' in s['tests_ax'])
check('no new UAT', not (ROOT/'docs/cierre-npt-2026-11-15/UAT_RESULTS.md').exists())

# Cross-connection SQL state model: proves compare-and-set idea only, NOT EF generated SQL.
with sqlite3.connect(':memory:') as db:
    db.execute('CREATE TABLE queue(id INTEGER PRIMARY KEY,status TEXT NOT NULL,token TEXT,lease INTEGER,reconcile INTEGER NOT NULL)')
    db.execute('INSERT INTO queue VALUES(1,"Queued",NULL,NULL,0)')
    a=db.execute('UPDATE queue SET status="Dispatching",token="A",lease=100 WHERE id=1 AND status="Queued" AND reconcile=0').rowcount
    b=db.execute('UPDATE queue SET status="Dispatching",token="B",lease=100 WHERE id=1 AND status="Queued" AND reconcile=0').rowcount
    check('SQLite CAS model competing workers only first claims',a==1 and b==0)
    intent=db.execute('UPDATE queue SET lease=101 WHERE id=1 AND token="B" AND status="Dispatching"').rowcount
    check('SQLite CAS model stale token cannot mark transport',intent==0)
    db.execute('UPDATE queue SET status="Blocked",reconcile=1 WHERE id=1 AND status="Dispatching" AND lease<=200')
    replay=db.execute('UPDATE queue SET status="Dispatching" WHERE id=1 AND status="Queued" AND reconcile=0').rowcount
    check('SQLite CAS model expired lease never auto-replays',replay==0)
    db.execute('CREATE TABLE attempt(id INTEGER PRIMARY KEY,status TEXT NOT NULL,token TEXT)')
    db.execute('INSERT INTO attempt VALUES(1,"Pendiente",NULL)')
    a=db.execute('UPDATE attempt SET status="InFlight",token="A" WHERE id=1 AND status="Pendiente"').rowcount
    b=db.execute('UPDATE attempt SET status="InFlight",token="B" WHERE id=1 AND status="Pendiente"').rowcount
    check('SQLite Axon claim model at most one winner',a==1 and b==0)
    db.execute('CREATE TABLE pre(id INTEGER PRIMARY KEY,status TEXT)')
    db.execute('CREATE TABLE resp(id INTEGER PRIMARY KEY,status TEXT)')
    db.execute('INSERT INTO pre VALUES(1,"Pending")')
    db.commit()
    db.execute('BEGIN')
    db.execute('UPDATE pre SET status="Confirmed" WHERE id=1')
    db.execute('INSERT INTO resp VALUES(1,"Matched")')
    db.execute('ROLLBACK')
    check('SQLite transaction rollback model no partial pre/response', db.execute('SELECT status FROM pre WHERE id=1').fetchone()[0]=='Pending' and db.execute('SELECT COUNT(*) FROM resp').fetchone()[0]==0)

passes=sum(1 for _,ok in results if ok)
output={'kind':'STATIC_SOURCE_AND_PYTHON_SQLITE_MODEL_NOT_DOTNET_TESTS','total':len(results),'passes':passes,'fails':len(results)-passes,'checks':[{'name':n,'pass':v} for n,v in results],'dotnet':'NOT_RUN','sqlserver':'NOT_RUN','postgresql':'NOT_RUN','uat':'NOT_RUN'}
out=ROOT/'docs/cierre-npt-2026-11-15/evidencias/VERIFICACION_NPT_ESTATICA.json'
out.write_text(json.dumps(output,ensure_ascii=False,indent=2)+'\n')
print(f"{passes}/{len(results)} SOURCE+SQLITE MODEL CHECKS: " + ('PASS' if passes==len(results) else 'FAIL'))
sys.exit(0 if passes==len(results) else 1)

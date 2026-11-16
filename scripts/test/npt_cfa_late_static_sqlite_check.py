#!/usr/bin/env python3
"""NPT-CFA-LATE-01: source checks + independent SQLite concurrency *model*.
Not a .NET/EF Core execution nor SQL Server/PostgreSQL integration test.
"""
from __future__ import annotations
import concurrent.futures
import hashlib
import json
import sqlite3
import tempfile
from pathlib import Path

ROOT = Path(__file__).resolve().parents[2]
PIPELINE = ROOT / 'src/Cfa.ACHInterbank.Persistence/ACH/Services/Implementation/IncomingNachaPostProcessingPipeline.cs'
TESTS = ROOT / 'tests/Cfa.ACHInterbank.Tests/NptCfaLateEvidenceTests.cs'
src = PIPELINE.read_text(encoding='utf-8')
tests = TESTS.read_text(encoding='utf-8')
method = src.split('    private async Task PreserveLateEvidenceAsync(', 1)[1].split('    // The relational UPDATE is one statement', 1)[0]
checks = []

def check(name, condition):
    assert condition, f'FAILED: {name}'
    checks.append(name)
    print('PASS', name)

check('no insertion of duplicate execution', 'IncomingNachaIntegrationExecution.Add' not in method)
check('uses existing append-only processing events', 'IncomingNachaProcessingEvents.Add' in method)
check('semantic deterministic fingerprint', 'SHA256.HashData' in method and 'identityJson' in method and 'new Guid(fingerprint.AsSpan(0, 16))' in method)
check('stored XML plus correlation and response', all(x in method for x in ('responsePayloadXml = execution?.ResponsePayloadXml', 'requestPayloadXml = execution?.RequestPayloadXml', 'correlationId = execution?.CorrelationId', 'resultCode = execution?.ResultCode')))
check('no queue changes or monetary dispatch in evidence method', all(x not in method for x in ('ExecuteUpdateAsync', 'ProcTransaccionesAsync', 'SendAsync(', 'IncomingNachaDispatchQueue.Add', 'queue.QueueStatus =')))
check('duplicate only confirmed after reading DB evidence key', 'catch (DbUpdateException)' in method and 'storedKey.GetString()' in method and 'if (storedEvidence is null) throw;' in method)
check('C# regression exercises real persisted execution', 'setup.IncomingNachaIntegrationExecution.Add' in tests and 'collision.IncomingNachaIntegrationExecution.Add' in tests)
check('C# regression covers concurrency and rollback', 'Task.WhenAll(workers)' in tests and 'CREATE TRIGGER fail_late_evidence' in tests)
check('C# regression preserves prior nonempty SOAP evidence', 'EarlierSoapEvidence_IsNeverOverwrittenByAConflictingLateResponse' in tests)

with tempfile.TemporaryDirectory() as d:
    path = str(Path(d) / 'late.sqlite')
    def open_conn():
        c = sqlite3.connect(path, timeout=15, isolation_level=None)
        c.execute('PRAGMA busy_timeout=15000')
        c.execute('PRAGMA journal_mode=WAL')
        return c
    c = open_conn()
    c.executescript('''
        CREATE TABLE execution(id TEXT PRIMARY KEY, entry_detail_id INTEGER, attempt_number INTEGER,
            response_xml TEXT NOT NULL DEFAULT '', UNIQUE(entry_detail_id, attempt_number));
        CREATE TABLE events(id TEXT PRIMARY KEY, evidence_json TEXT NOT NULL);
        CREATE TABLE queue(id TEXT PRIMARY KEY, status TEXT NOT NULL, requires_reconciliation INTEGER NOT NULL);
        INSERT INTO execution(id, entry_detail_id, attempt_number) VALUES ('original', 4511, 1);
        INSERT INTO queue(id, status, requires_reconciliation) VALUES ('q', 'Blocked', 1);
    ''')
    try:
        c.execute('INSERT INTO execution(id, entry_detail_id, attempt_number) VALUES (?,?,?)', ('old-behavior',4511,1))
        raise AssertionError('Original duplicate would not fail')
    except sqlite3.IntegrityError:
        pass
    check('SQLite model reproduces original unique-index failure', c.execute('SELECT COUNT(*) FROM execution').fetchone()[0] == 1)

    def archive(xml, timestamp):
        identity = {'queueId':'q', 'claimToken':'t', 'executionId':'original', 'responsePayloadXml':xml,
                    'responseCode':'ACCEPTED-LATE'}
        identity_json = json.dumps(identity, sort_keys=True, separators=(',',':'))
        digest = hashlib.sha256(identity_json.encode()).hexdigest()
        evidence = {'evidenceKey':digest, 'correlationId':'CFA-LATE-CORR',
                    'responsePayloadXml':xml, 'requestPayloadXml':'ORIGINAL_REQUEST',
                    'resultCode':'ACCEPTED-LATE', 'finishedAtUtc':timestamp}
        stored_json = json.dumps(evidence, sort_keys=True)
        conn = open_conn()
        try:
            try:
                conn.execute('INSERT INTO events(id, evidence_json) VALUES (?,?)', (digest[:32], stored_json))
            except sqlite3.IntegrityError:
                old = conn.execute('SELECT evidence_json FROM events WHERE id=?', (digest[:32],)).fetchone()
                if old is None or json.loads(old[0]).get('evidenceKey') != digest:
                    raise
        finally:
            conn.close()

    with concurrent.futures.ThreadPoolExecutor(max_workers=4) as pool:
        list(pool.map(lambda i: archive('<Envelope>same</Envelope>', f't{i}'), range(12)))
    check('concurrent identical SOAP outcomes yield one event', c.execute('SELECT COUNT(*) FROM events').fetchone()[0] == 1)
    archive('<Envelope>different</Envelope>', 't13')
    check('different late response archived separately', c.execute('SELECT COUNT(*) FROM events').fetchone()[0] == 2)
    evidence = [json.loads(x[0]) for x in c.execute('SELECT evidence_json FROM events')]
    check('evidence keeps XML/correlation/request payload', all(x['correlationId']=='CFA-LATE-CORR' and x['requestPayloadXml']=='ORIGINAL_REQUEST' and x['responsePayloadXml'] for x in evidence))
    check('original attempt and quarantine untouched', c.execute('SELECT COUNT(*), MAX(response_xml) FROM execution').fetchone() == (1, '') and c.execute('SELECT status, requires_reconciliation FROM queue').fetchone() == ('Blocked',1))
    c.executescript('''
        CREATE TRIGGER deny_archive BEFORE INSERT ON events
        BEGIN SELECT RAISE(ABORT, 'forced rollback'); END;
    ''')
    try:
        archive('<Envelope>rollback</Envelope>', 't14')
        raise AssertionError('Injected rollback did not fail')
    except sqlite3.IntegrityError:
        pass
    check('failed event insertion rolls back, old evidence remains', c.execute('SELECT COUNT(*) FROM events').fetchone()[0] == 2)
    c.close()

print(f'RESULT {len(checks)}/{len(checks)} source checks + SQLite model; no .NET build/test was executed by this script')

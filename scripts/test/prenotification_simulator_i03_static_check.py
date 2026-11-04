#!/usr/bin/env python3
"""I03: comprobaciones estructurales, NO sustituyen compilacion .NET ni pruebas UAT."""
from pathlib import Path
import json
root = Path(__file__).resolve().parents[2]
paths = {
    'service': 'src/Cfa.ACHInterbank.Persistence/ACH/Services/Implementation/NachaInboundSimulationService.PrenotificationReturns.cs',
    'legacy': 'src/Cfa.ACHInterbank.Persistence/ACH/Services/Implementation/NachaInboundSimulationService.cs',
    'identity': 'src/Cfa.ACHInterbank.Application/ACH/Services/PrenotificationSimulationIdentity.cs',
    'api': 'src/Cfa.ACHInterbank.Api/Controllers/NachaInboundSimulatorController.cs',
    'iface': 'src/Cfa.ACHInterbank.Application/ACH/Interfaces/INachaInboundSimulationService.cs',
    'tests': 'tests/Cfa.ACHInterbank.Tests/PrenotificationSimulatorI03ContractTests.cs',
    'config': 'src/Cfa.ACHInterbank.Persistence/Configuration/NachaInboundSimulationConfiguration.cs',
}
text = {k: (root / v).read_text(encoding='utf-8') for k, v in paths.items()}
checks = {
    'new-endpoint': 'prenotifications/generate-return' in text['api'],
    'endpoint-write-role': 'P1Policies.NachaSimulatorGenerateDifferential' in text['api'].split('prenotifications/generate-return')[1][:250],
    'new-interface-contract': 'GeneratePrenotificationReturnAsync' in text['iface'],
    'no-return-not-file': 'NO_RETURN_HAS_NO_NACHA_FILE' in text['service'],
    'uat-only': '_options.IsUatLike()' in text['service'],
    'no-autoimport': '_options.AllowAutoImport' in text['service'],
    'no-transmission': '_options.AllowExternalTransmission' in text['service'],
    'shape-check': 'PrenotificationSimulationContract.Validate(request)' in text['service'],
    'reuse-preview': 'PreviewPrenotificationAsync(request, ct)' in text['service'],
    'serializable': 'IsolationLevel.Serializable' in text['service'],
    'atomic-reservation': 'SimulationId = reservationId' in text['service'] and 'SaveChangesAsync(ct)' in text['service'],
    'db-unique-key': 'HasIndex(x => x.SimulationId).IsUnique()' in text['config'],
    'idempotency-key-hash': 'ReservationId(' in text['identity'] and 'SHA256.HashData' in text['identity'],
    'payload-fingerprint': 'RequestHash(' in text['identity'],
    'replay-payload-check': 'MatchesStoredPayload(' in text['service'],
    'duplicate-original-check': 'PRENOTIFICATION_RETURN_ALREADY_GENERATED' in text['service'],
    'trace-original-uniqueness': 'PRENOTIFICATION_TRACE_NOT_UNIQUE' in text['service'],
    'official-builder': 'BuildOfficialReturnFileAsync(' in text['service'],
    'official-reason': 'ValidateReturnReasonAsync(' in text['service'],
    'published-profile': 'ResolvePublishedDifferentialProfileAsync(' in text['service'],
    'cenit-ror-exclusion': 'CenitReturnIn2026Layout.IsReturnOfReturnCause' in text['service'],
    'sequence-reservation': 'NextDailySequenceAsync(' in text['service'] and 'MapSequenceToFileId(' in text['service'],
    'zero-value-assert': 'e.Amount != 0' in text['service'],
    'manually-imported-only': 'AutoImported = false' in text['service'] and 'UploadRequired = true' in text['service'],
    'no-external-transmission': 'ExternalTransmission = false' in text['service'],
    'rollback-on-failure': 'RollbackAsync(CancellationToken.None)' in text['service'],
    'delete-file-on-rollback': 'Directory.Delete(outputDirectory' in text['service'],
    'write-evidence': 'WriteEvidenceAsync(' in text['service'],
    'old-generate-fail-closed': 'PRENOTIFICATION_RETURN_NOT_IMPLEMENTED' in text['legacy'],
    'enum-compatible': 'IncomingPrenotificationReturn = 10' in (root/'src/Cfa.ACHInterbank.Domain/Models/ACH/Enums/NachaInboundSimulationEnums.cs').read_text(),
    'unit-tests-added': '[Fact]' in text['tests'] and 'SameScopedKeySameGuid' in text['tests'],
}
for k, good in checks.items(): print(('PASS_STATIC' if good else 'FAIL_STATIC'), k)
result = {'classification':'STATIC_ONLY_NOT_DOTNET_BUILD_OR_TEST','checks_total':len(checks),'passed':sum(checks.values()),'failed':[k for k,v in checks.items() if not v]}
print(json.dumps(result, ensure_ascii=False))
raise SystemExit(0 if all(checks.values()) else 1)

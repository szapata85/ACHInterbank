#!/usr/bin/env python3
"""Static source guardrails only; NOT .NET/xUnit/SQL/HTTP execution."""
from pathlib import Path
import hashlib, json, sys
root=Path(__file__).resolve().parents[2]
base=Path('/mnt/data/ACHInterbank_HOTFIX_XSD_TRANSPORTE_NCTAORIG_H010203_2026-11-16.zip')
proc=root/'src/Cfa.ACHInterbank.Persistence/ACH/Services/Implementation/ProcTransaccionesRequestMapper.cs'
readiness=root/'src/Cfa.ACHInterbank.Persistence/Integrations/Services/IntegrationMappingReadinessService.cs'
provider=root/'src/Cfa.ACHInterbank.Persistence/Integrations/Providers/CurrentIntegrationSourceProviderFactory.cs'
pipeline=root/'src/Cfa.ACHInterbank.Persistence/ACH/Services/Implementation/IncomingNachaPostProcessingPipeline.cs'
files=[proc,readiness,provider,pipeline]
s=[x.read_text() for x in files]
tests=(root/'tests/Cfa.ACHInterbank.Tests/NachaDesagregadoIntegrationMappingTests.cs').read_text()
guarantees=(root/'tests/Cfa.ACHInterbank.Tests/TransactionIntegrationReadinessGuaranteeTests.cs').read_text()
safety=(root/'tests/Cfa.ACHInterbank.Tests/ProcTransaccionesOriginAccountSafetyTests.cs').read_text()
checks={
    'source provider allows ProcTransacciones': '[ProcContrapartidasMethod, ProcTransaccionesMethod]' in s[2].split('"transaction.sourceAccountNumber"')[1].split('async (context')[0],
    'NCTAORIG canonical mapping source': '"NCTAORIG" => "transaction.sourceAccountNumber"' in (root/'src/Cfa.ACHInterbank.Persistence/Integrations/Services/IntegrationMappingBootstrapper.cs').read_text(),
    'readiness requires exact canonical mapping': 'rule.SourceKind != IntegrationSourceKindEnum.Transaction' in s[1] and '"transaction.sourceAccountNumber"' in s[1],
    'readiness rejects fixed and default fallbacks': '!string.IsNullOrWhiteSpace(rule.FixedValue)' in s[1] and '!string.IsNullOrWhiteSpace(rule.DefaultValue)' in s[1],
    'independent provenance-only blocker removed': all('PROC_TRANSACCIONES_NCTAORIG_CONTRACT_NOT_HOMOLOGATED' not in v for v in s),
    'runtime rejects blank transaction source': 'string.IsNullOrWhiteSpace(transaction.SourceAccountNumber)' in s[1],
    'mapper rejects absent source': '!resolved.TryGetValue("NCTAORIG", out var account)' in s[0] and 'string.IsNullOrWhiteSpace(transactionSourceAccount)' in s[0],
    'mapper enforces exact ordinal equality': 'string.Equals(account, transactionSourceAccount, StringComparison.Ordinal)' in s[0],
    'mapper rejects receiver source and constant': 'source.SourceKind != IntegrationSourceKindEnum.Transaction' in s[0] and 'source.Fixed' in s[0] and 'source.Default' in s[0],
    'legacy mapping uses source account exactly': '"transaction.sourceaccountnumber" => transaction.SourceAccountNumber' in s[0],
    'Live preflight retains mandatory account': 'EnsureLiveOriginAccountPresent(requestXml)' in s[3] and 'string.IsNullOrWhiteSpace(account.Value)' in s[3],
    'Live preflight rejects xsi nil': 'account.Attribute(xsi + "nil")' in s[3],
    'Live preflight blocks DTD and resolvers': 'DtdProcessing = System.Xml.DtdProcessing.Prohibit' in s[3] and 'XmlResolver = null' in s[3],
    'no automatic blanket provenance denial': 'PROC_TRANSACCIONES_NCTAORIG_NOT_HOMOLOGATED' not in s[3],
    'ACH Colombia file-based F5 remains gated': 'PROC_TRANSACCIONES_ACHCOL_MAPPING_NOT_HOMOLOGATED_F51' in s[0],
    'XSD final transport still in CFA client': 'OfficialSoapContracts.ValidateTransportRequest' in (root/'src/Cfa.ACHInterbank.External/Connections/WscfaachSoapClient.cs').read_text(),
    'XSD final transport still in Axon client': 'OfficialSoapContracts.ValidateTransportRequest' in (root/'src/Cfa.ACHInterbank.External/Connections/WsAxonRespuestaTransaccionesSoapClient.cs').read_text(),
    'positive readiness expectation updated': 'ProcTransaccionesReadiness_CanonicalOriginSourcePassesWithValidPrerequisites' in tests and 'Assert.True(readiness.CanBuildPayload)' in tests,
    'transit negative expectations updated': 'ProcTransaccionesReadiness_MissingTransitCodeFailsForActualTransitProblem' in tests and 'receiver ? "BCORECEP" : "BCOORIG"' in tests,
    'mismatched source negative tests': 'ProcTransaccionesReadiness_RejectsRecipientOrEntryAccountAsOrigin' in guarantees,
    'missing source negative test': 'ProcTransacciones_RejectsMissingSourceAccount_EvenWhenEntryAccountIsPresent' in tests,
    'live nil and duplicate negative tests': "xsi:nil='true'" in safety and '<tem:NCTAORIG>123</tem:NCTAORIG><tem:NCTAORIG>123</tem:NCTAORIG>' in safety,
    'direct mapper test has original account': 'Assert.Equal(transaction.SourceAccountNumber, resolution.Contract.Parameters["NCTAORIG"])' in (root/'tests/Cfa.ACHInterbank.Tests/ProcTransaccionesRequestMapperTests.cs').read_text(),
}
for name, ok in checks.items(): print(('PASS' if ok else 'FAIL') + ' ' + name)
print(f'{sum(checks.values())}/{len(checks)} STATIC SOURCE CHECKS (not xUnit)')
out=root/'docs/npt-h02-h03-final/evidencias/focal_source_checks.json'
out.write_text(json.dumps({'total':len(checks),'passed':sum(checks.values()),'checks':checks,'execution':'Python static only; no CLR'},indent=2,ensure_ascii=False)+'\n')
sys.exit(0 if all(checks.values()) else 1)

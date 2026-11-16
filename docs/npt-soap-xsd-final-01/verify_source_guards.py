#!/usr/bin/env python3
"""Reproducible *static* regression of sensitive guards and official schema snapshots.
No .NET compilation, SOAP bank call, or homologation is implied.
"""
from pathlib import Path
import hashlib
import json
import sys

ROOT = Path(__file__).resolve().parents[2]
OUT = ROOT / 'docs/npt-soap-xsd-final-01/evidencias/source_guard_results.json'
BASE = ROOT / 'src/Cfa.ACHInterbank.Application/Integrations/Contracts/OfficialSoapSchemas'
checks = []

def check(name, predicate):
    checks.append({'name': name, 'passed': bool(predicate)})

def source(path):
    return (ROOT / path).read_text(encoding='utf-8')

# Fixed SHA256 from the exact two uploaded ZIPs; stays reproducible in this project.
EXPECTED_SHA256 = {
    'WSCFAACH/WSCFAACH.wsdl.xml': '46e66b4e84fb4d4082dfe5474cfc9a2095cc75373a4550bafdc3e122b02dc8ab',
    'WSCFAACH/xsd0.xml': '5a0e0b237c034db7efdffcea00623c68ef029720e32a0e482944893b2c8538e7',
    'WSCFAACH/xsd1.xml': 'affa235e732a8bf3ca3eb406dc435c4d72095d21c8e5906927c3812e3e8a5d19',
    'WSAxonRespuestaTransacciones/WSAxonRespuestaTransacciones.wsdl.xml': '0308cf9375989d6214a88120a887518406618ab195ccff9a78879f0ad5793062',
    'WSAxonRespuestaTransacciones/xsd0.xml': 'c6163208ddc3c471c72e87e77d259c0040da0188fed04e813e6838ae3ff70fd5',
    'WSAxonRespuestaTransacciones/xsd1.xml': 'affa235e732a8bf3ca3eb406dc435c4d72095d21c8e5906927c3812e3e8a5d19',
    'WSAxonRespuestaTransacciones/xsd2.xml': '5cf9892080047fe1944f0ff3df9217c269c32c47e4318a7eda886a6969c069b7',
}
for name, expected in EXPECTED_SHA256.items():
    copied = BASE / name
    check(f'original snapshot {name}', copied.is_file() and
        hashlib.sha256(copied.read_bytes()).hexdigest() == expected)

contract = source('src/Cfa.ACHInterbank.Application/Integrations/Contracts/OfficialSoapContracts.cs')
check('explicit local schema resources', 'GetManifestResourceStream(logicalName)' in contract)
check('no network resolver', 'XmlResolver = null' in contract and 'DtdProcessing = DtdProcessing.Prohibit' in contract)
check('import relink only locally', 'import.SchemaLocation = null;' in contract and 'import.Schema = local;' in contract)
check('schema compilation', 'set.Compile();' in contract)
check('schema validates actual operation', 'operation.Validate(schemas,' in contract)
check('WSDL action validation', 'SOAP_WSDL_ACTION_OR_LITERAL_MISMATCH' in contract)
check('WSDL local binding validation', 'SOAP_WSDL_SOAP11_BINDING_REQUIRED' in contract)

cfa = source('src/Cfa.ACHInterbank.Persistence/ACH/Services/Implementation/ProcTransaccionesResponseParser.cs')
check('CFA response XSD enforcement', 'OfficialSoapContracts.ValidateElement(resultElement' in cfa)
check('CFA retains no automatic replay', 'var isRetryable = false;' in cfa)
check('CFA SOAP Fault fail closed', 'SOAP_FAULT' in cfa)
check('CFA strict RTAACH', 'MISSING_OR_AMBIGUOUS_RTAACH' in cfa)
axon = source('src/Cfa.ACHInterbank.External/Connections/RespuestaTransaccionesAch/RegistrarRespuestaAchSoapResponseParser.cs')
check('Axon response XSD enforcement', 'Operation.RegistrarRespuestaResponse' in axon)
check('Axon requires qualified data contract boolean', 'result.Elements(data + "existeError")' in axon)
check('Axon DTD blocked', 'DtdProcessing = DtdProcessing.Prohibit' in axon)
check('Axon fault uncertain', 'SOAP_AXON_FAULT_RESULT_UNCERTAIN' in axon)

mapper = source('src/Cfa.ACHInterbank.Persistence/ACH/Services/Implementation/ProcTransaccionesRequestMapper.cs')
check('CFA request XSD preflight', 'ValidateGeneratedRequest(legacyXml);' in mapper
    and 'ValidateGeneratedRequest(xml);' in mapper)
check('NCTAORIG is transaction source only', 'source.SourceKind != IntegrationSourceKindEnum.Transaction' in mapper
    and '"transaction.sourceAccountNumber"' in mapper)
check('NCTAORIG never accepts default/fixed', '!string.IsNullOrWhiteSpace(source.Fixed)' in mapper
    and '!string.IsNullOrWhiteSpace(source.Default)' in mapper)
check('origin proven from exact transaction source, not receiver', '!string.Equals(account, transactionSourceAccount, StringComparison.Ordinal)' in mapper and 'entryDetails.accountNumber' not in mapper.split('private static void EnsureNctaOrigSource(',1)[1].split('public string BuildSoapBody',1)[0])

stage = source('src/Cfa.ACHInterbank.Persistence/ACH/Services/Implementation/IncomingNachaPostProcessingPipeline.cs')
check('Live NCTAORIG nonempty guard preserved', 'PROC_TRANSACCIONES_NCTAORIG_SOURCE_INVALID' in stage and 'EnsureLiveOriginAccountPresent(' in stage)
pre = source('src/Cfa.ACHInterbank.Persistence/ACH/Services/Implementation/AchColProcTransaccionesDryRunContractCheck.cs')
check('F5 blocked after local XSD validation', 'Operation.ProcTransaccionesRequest' in pre
    and 'public bool CanTransmit => false;' in pre
    and 'public bool HasApprovedAchColMapping => false;' in pre)
check('F5 external operational approval still blocked', 'ACHCOL_F53_EXTERNAL_CONTRACT_NOT_HOMOLOGATED' in pre)
axon_gw = source('src/Cfa.ACHInterbank.External/Connections/RespuestaTransaccionesAch/RespuestaTransaccionesAchGateway.cs')
check('Axon both request paths validated', axon_gw.count('ValidateAxonRequestBeforeTransport(') >= 3)
check('Axon retains cancellation token', 'cancellationToken' in axon_gw)
check('CFA SOAPAction exact pre-transport', 'WSCFAACH_SOAPACTION_XSD_MISMATCH' in source('src/Cfa.ACHInterbank.External/Connections/WscfaachSoapClient.cs'))
check('Axon SOAPAction exact pre-transport', 'WSAXON_SOAPACTION_XSD_MISMATCH' in source('src/Cfa.ACHInterbank.External/Connections/WsAxonRespuestaTransaccionesSoapClient.cs'))

report = {'type': 'source_and_original_bundle_static_guards', 'total': len(checks),
    'passed': sum(x['passed'] for x in checks), 'results': checks,
    'not_executed': ['dotnet build', 'xUnit', 'real SOAP transport', 'SQL Server/PostgreSQL']}
OUT.write_text(json.dumps(report, indent=2, ensure_ascii=False)+'\n', encoding='utf-8')
print(f"{report['passed']}/{report['total']} static source/bundle checks passed")
for x in checks:
    if not x['passed']: print('FAIL:', x['name'])
sys.exit(0 if report['passed']==report['total'] else 1)

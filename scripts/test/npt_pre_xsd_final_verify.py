#!/usr/bin/env python3
"""NPT_PRE_XSD_FINAL: static source checks and optional external WSDL metadata checks.
No C# compilation, xUnit, real EF Core/SQL, UAT or SOAP is run here.
Usage: python scripts/test/npt_pre_xsd_final_verify.py [WSCFAACH.wsdl.xml] [WSAxon.wsdl.xml]
"""
from pathlib import Path
from xml.etree import ElementTree as ET
import sys
root=Path(__file__).resolve().parents[2]
src=root/'src/Cfa.ACHInterbank.Persistence'
tst=root/'tests/Cfa.ACHInterbank.Tests'
load=lambda path:(src/path).read_text(encoding='utf-8-sig')
planner=load('ACH/Services/Implementation/IncomingNachaDispatchPlanner.cs')
preflight=load('ACH/Services/Implementation/IncomingNachaFileBasedPreparationStage.cs')
eligibility=load('ACH/Services/Implementation/IncomingNachaDispatchEligibilityPolicy.cs')
ingest=load('ACH/Services/Implementation/IncomingNachaIngestionAppService.cs')
bootstrap=load('Integrations/Services/IntegrationMappingBootstrapper.cs')
readiness=load('Integrations/Services/IntegrationMappingReadinessService.cs')
mapper=load('ACH/Services/Implementation/ProcTransaccionesRequestMapper.cs')
pipeline=load('ACH/Services/Implementation/IncomingNachaPostProcessingPipeline.cs')
corr=load('ACH/Services/Implementation/DifferentialPrenotificationResponseProcessor.cs')
checks=[]
def check(label, pass_):
    checks.append(bool(pass_));print(f'{len(checks):02} {"PASS" if pass_ else "FAIL"} {label}')

# NPT-01: still completely isolated from money-moving worker.
check('NPT01 planner keeps file-based blocked', 'SourceKind = IncomingNachaDispatchSourceKinds.AchColFileBased' in planner and 'QueueStatus = IncomingNachaDispatchQueueStatus.Blocked' in planner)
check('NPT01 no fabricated cycle for F5', 'AchCycleId = null' in planner and 'AchTransactionId = null' in planner)
check('NPT01 F4.2 checks no cycle or transaction or fake link',all(x in preflight for x in ('queue.AchCycleId is null','queue.AchTransactionId is null','queue.IncomingNachaTransactionLinkId is null')))
check('NPT01 F4.2 checks chamber and original file ownership',all(x in preflight for x in ('header.IncomingNachaFileIngestionId == ingestion.Id','header.ClearingHouseId == queue.ClearingHouseId')))
check('NPT01 F4.2 retains Blocked and no retry', 'queue.QueueStatus = IncomingNachaDispatchQueueStatus.Blocked;' in preflight and 'queue.NextAttemptAtUtc = null;' in preflight)
check('NPT01 eligibility rejects prenotification and return codes', 'ACHCOL_NON_MONETARY_OR_DIRECTION_MISMATCH' in eligibility and 'ACHCOL_CLASSIFICATION_NOT_ELIGIBLE' in eligibility)
check('NPT01 worker selects only cycle-bound', 'SourceKind == IncomingNachaDispatchSourceKinds.CycleBound' in pipeline)

# NPT-02: selected profile does not magically authorize physical differentials.
check('NPT02 partial 799 is detected not interpreted as original', "record.Length >= 3" in ingest and 'record[0] == \'7\'' in ingest and 'record.Substring(1, 2), "99"' in ingest)
check('NPT02 profile selection still requires homologated profile', 'RequireHomologated = isDifferentialCandidate' in ingest)
check('NPT02 selected differential blocked before any parser', ingest.index('if (isDifferentialCandidate && !isCenitReturnOfReturn)') < ingest.index('_parserService.ParseAndSaveDetailedAsync('))
check('NPT02 fail-closed event and returned error', all(x in ingest for x in ('DIFFERENTIAL_TABLE_DRIVEN_PARSER_NOT_ENABLED', 'DifferentialPhysicalContractGate', 'ParserErrorsJson = JsonSerializer.Serialize(new[] { blockedCode })')))
check('NPT02 CENIT official ROR separately recognized', 'isCenitReturnOfReturn = isCenit && IsCenitReturnOfReturnCandidate(records)' in ingest and 'if (isDifferentialCandidate && !isCenitReturnOfReturn)' in ingest)
check('NPT02 any selected profile must request published snapshot', 'RequirePublishedProfileSnapshot = profileResolution?.Profile is not null' in ingest)

# NPT-05: candidate source is from originating transaction but contract blocks Live.
check('NPT05 bootstrap no longer maps receiver account', '"NCTAORIG" => "transaction.sourceAccountNumber"' in bootstrap and '"NCTAORIG" => "entrydetails.accountnumber"' not in bootstrap)
check('NPT05 readiness only accepts origin-source shape', 'rule.SourceKind != IntegrationSourceKindEnum.Transaction' in readiness and '"transaction.sourceAccountNumber"' in readiness)
check('NPT05 readiness explicitly fails absent contract', 'PROC_TRANSACCIONES_NCTAORIG_CONTRACT_NOT_HOMOLOGATED' in readiness and 'return ["PROC_TRANSACCIONES_NCTAORIG_CONTRACT_NOT_HOMOLOGATED"];' in readiness)
check('NPT05 no default or fixed value', 'string.IsNullOrWhiteSpace(rule.FixedValue)' in readiness and 'string.IsNullOrWhiteSpace(rule.DefaultValue)' in readiness)
check('NPT05 Live origin account guard still active', 'PROC_TRANSACCIONES_NCTAORIG_NOT_HOMOLOGATED' in pipeline and 'EnsureLiveOriginAccountHomologated' in pipeline)
check('NPT05 file-based candidate cannot enter ResolveAsync SOAP', 'PROC_TRANSACCIONES_ACHCOL_MAPPING_NOT_HOMOLOGATED_F51' in mapper)

# NPT-11: chamber/file scope + verified original trace, ambiguity => no movement.
check('NPT11 chamber must be present', 'responseClearingHouseId is null' in corr)
check('NPT11 max two matches and ambiguity', '.Take(2)' in corr and 'PrenotificationNachaMatch.Ambiguous(' in corr)
check('NPT11 original file and chamber fixed', 'headerScope.IncomingNachaFileIngestionId != link.IncomingNachaFileIngestionId' in corr and 'originalIngestion.ResolvedClearingHouseId != responseClearingHouseId' in corr)
check('NPT11 addenda must belong to original link', 'link.AddendaRecordId != addenda?.AddendaID' in corr)
check('NPT11 reject wrong linked trace and cycle', 'Normalize(prenotification.TraceNumber)' in corr and 'header.AchCycleId != prenotification.AchCycleId' in corr)
check('NPT11 never Matched before identity checks', corr.index('DIFFERENTIAL_RESPONSE_IDENTITY_MISMATCH') < corr.index('response.CorrelationStatus = AchResponseCorrelationStatus.Matched'))
check('NPT11 only non monetary operation', 'if (operation.MovesMoney)' in corr and 'MonetaryMovementCreated: false' in corr)
check('NPT tests include positive, negative, deliberate blocking and Live guard', all((tst/f).exists() for f in ('IncomingNachaFileBasedOrchestrationF42Tests.cs','IncomingNachaIngestionAppServiceTests.cs','DifferentialPrenotificationResponseProcessorTests.cs','ProcTransaccionesOriginAccountSafetyTests.cs')) and 'Npt01_ForgedFileBasedQueueContext_StaysBlockedWithoutSoap' in (tst/'IncomingNachaFileBasedOrchestrationF42Tests.cs').read_text() and 'Npt11_InconsistentOriginalLink_NeverMatchesOrMovesMoney' in (tst/'DifferentialPrenotificationResponseProcessorTests.cs').read_text())

# WSDL provided externally, never copied into ZIP; verify only known WSDL metadata.
if len(sys.argv)==3:
    WSDL='{http://schemas.xmlsoap.org/wsdl/}'; SOAP='{http://schemas.xmlsoap.org/wsdl/soap/}'
    for path,operation,action,import_count in (
        (sys.argv[1],'Proc_Transacciones','http://tempuri.org/IWSCFAACH/Proc_Transacciones',2),
        (sys.argv[2],'RegistrarRespuestaTransaccion','http://tempuri.org/IWSAxonRespuestaTransacciones/RegistrarRespuestaTransaccion',3)):
        doc=ET.parse(path).getroot()
        soap_actions=[x.attrib.get('soapAction') for x in doc.iter(SOAP+'operation')]
        ops=[x.attrib.get('name') for x in doc.iter(WSDL+'operation')]
        imports=[x.attrib.get('schemaLocation') for x in doc.iter('{http://www.w3.org/2001/XMLSchema}import')]
        check(f'WSDL {operation} operation + SOAPAction exact', operation in ops and action in soap_actions)
        check(f'WSDL {operation} XSD import dependencies enumerated', len(imports)==import_count and all('xsd=xsd' in (x or '') for x in imports))
else:
    print('WSDL metadata: SKIPPED - supply the two unchanged external WSDL attachment paths.')
print(f'RESULT {sum(checks)}/{len(checks)} SOURCE/WSDL-METADATA only; not C# xUnit or SQL integration')
sys.exit(0 if all(checks) else 1)

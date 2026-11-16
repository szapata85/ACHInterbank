#!/usr/bin/env python3
"""NPT-MAP-01: XML/XSD + source guard static checks. Not C# execution or .NET tests."""
from pathlib import Path
import json
from lxml import etree as E
ROOT=Path(__file__).resolve().parents[2]
checks=[]
def ck(name, cond):
    if not cond: raise AssertionError(name)
    checks.append(name)
base=ROOT/'src'
resolver=(base/'Cfa.ACHInterbank.Persistence/Integrations/Services/ProcContrapartidasFunctionalMappingResolver.cs').read_text()
readiness=(base/'Cfa.ACHInterbank.Persistence/Integrations/Services/IntegrationMappingReadinessService.cs').read_text()
mapper=(base/'Cfa.ACHInterbank.Persistence/ACH/Services/Implementation/ProcTransaccionesRequestMapper.cs').read_text()
soap=(base/'Cfa.ACHInterbank.External/Connections/WscfaachSoapClient.cs').read_text()
csTests=(ROOT/'tests/Cfa.ACHInterbank.Tests/NptMap01MappingSafetyTests.cs').read_text()
for field in ('OFEMP','OFIDARCH','OFDIRECCIONIP'):
    ck('homologation-deny:'+field, f'"{field}"' in resolver and f'"{field}"' in readiness + resolver)
ck('no-technical-ip-readiness-exception','IsNonBlockingTechnicalConstant' not in readiness)
ck('shared-safety-guard','ProcContrapartidasFunctionalMappingResolver.AssessProtectedRule(' in readiness and 'AssessProtectedRule(parameter.ParameterPath, winner, sourcePath)' in resolver)
ck('protected-path-is-source-number', '"OFCTA" => "transaction.sourceAccountNumber"' in resolver and '"OFIDTX" => "transaction.reference"' in resolver)
for prop in ('rule.FixedValue is not null','rule.DefaultValue is not null','rule.TransformationCode','rule.FormatMask','rule.ConditionExpression','IntegrationSourceKindEnum.Transaction'):
    ck('protected-rule-safety:'+prop, prop in resolver)
ck('exact-account-reference','EnsureProtectedResolvedValues(debit, contract.OFCTA, contract.OFIDTX)' in resolver and 'StringComparison.Ordinal' in resolver)
ck('readiness-failure-rejects-payload','CanBuildPayload: false' in readiness and 'functionalErrors.Add(safetyError)' in readiness)
ck('force-protected-catalog-status','ProcContrapartidasFunctionalMappingResolver.IsProtectedParameter(x.ParameterPath)' in readiness)
ck('inactive-protected-catalog-fails-closed','PROC_CONTRAPARTIDAS_PROTECTED_CONTRACT_INVALID' in readiness)
ck('inactive-nacha-catalog-fails-closed','catalogo sin fuentes fisicas' in readiness)
ck('no-go-transport-before-client','if (string.Equals(action, "Proc_Contrapartidas", StringComparison.Ordinal))' in soap and soap.index('PROC_CONTRAPARTIDAS_SOURCE_NOT_HOMOLOGATED: envio SOAP bloqueado') < soap.index('CreateClient(ExternalHttpClientNames.WscfaachSoapNoRetry)'))
ck('xsd-before-no-go',soap.index('ValidateTransportRequest(')<soap.index('PROC_CONTRAPARTIDAS_SOURCE_NOT_HOMOLOGATED: envio SOAP bloqueado'))
for field,path in (('NORIG','batchHeaders.companyName'),('IDORIG','batchHeaders.companyId'),('NCTARECEP','entrydetails.accountnumber')):
    ck('physical-nacha-'+field, f'"{field}" => ' in mapper and path in mapper)
ck('nctaorig-not-recipient','transaction.sourceAccountNumber' in mapper and 'EnsureNctaOrigSource' in mapper)
ck('physical-nacha-runtime','EnsurePhysicalNachaValues(resolved, nachaSource.BatchHeader?.CompanyName' in mapper)
ck('physical-nacha-readiness','ProcTransaccionesRequestMapper.AssessPhysicalNachaRule' in readiness)
ck('safety-xunit-cases',all(x in csTests for x in ('UnaccreditedIdentifiers_CannotBeUnlockedWithAlternativeSources','ProtectedFields_OnlyPermitUnmodifiedTransactionSource','ResolvedIdentity_CannotDifferFromPersistedAccountOrReference','IncomingPhysicalSources_RejectOverridesAndReceiverOriginSwaps')))
# Actual negative/positive XSD data validation (synthetic, not C# serializer execution)
sch=ROOT/'src/Cfa.ACHInterbank.Application/Integrations/Contracts/OfficialSoapSchemas/WSCFAACH/xsd0.xml'
parser=E.XMLParser(load_dtd=False,resolve_entities=False,no_network=True)
schema=E.XMLSchema(E.parse(str(sch),parser))
ns='http://tempuri.org/'
def element(name, items):
    root=E.Element('{%s}%s'%(ns,name))
    for key,value in items:
        e=E.SubElement(root,'{%s}%s'%(ns,key));e.text=value
    return root
ck('xsd-positive-contra', schema.validate(element('Proc_Contrapartidas',[('OFMONDEB','24.50'),('OFIDARCH','12')])))
ck('xsd-negative-int', not schema.validate(element('Proc_Contrapartidas',[('OFIDARCH','not-an-int')])))
ck('xsd-negative-duplicate', not schema.validate(element('Proc_Contrapartidas',[('OFIDTX','1'),('OFIDTX','2')])))
try:
    E.fromstring(b'<Proc_Contrapartidas><OFCTA>',parser)
except E.XMLSyntaxError:
    checks.append('malformed-xml-rejected')
else: raise AssertionError('malformed-xml')
# The original WSDL/XSD byte-by-byte comparison is captured in CONTRATOS_SHA256.json.
sha=json.loads((Path(__file__).with_name('CONTRATOS_SHA256.json')).read_text())
ck('7-original-contract-files',len(sha)==7 and all(x['matches_project_resource'] for x in sha.values()))
matrix=Path(__file__).with_name('MATRIZ_TRAZABILIDAD_66_CAMPOS.md').read_text()
ck('66-traceability-rows',len([line for line in matrix.splitlines() if line.startswith('| ') and line.split('|',2)[1].strip().isdigit()])==66)
print(json.dumps({'outcome':'PASS','checks':len(checks),'validation':'offline static code + lxml XSD; NOT dotnet build/xUnit/HTTP runtime'},ensure_ascii=False,indent=2))

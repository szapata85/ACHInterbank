#!/usr/bin/env python3
"""Offline only: XSD shape and source guard characterization. NOT CLR/xUnit/HTTP."""
from pathlib import Path
from lxml import etree as E
import json, sys
root=Path(__file__).resolve().parents[2]
assets=root/'src/Cfa.ACHInterbank.Application/Integrations/Contracts/OfficialSoapSchemas'
checks=[]
def test(label, ok):
    checks.append({'test':label,'pass':bool(ok)})
    print(('PASS' if ok else 'FAIL'),label)
class OnlyLocal(E.Resolver):
    def resolve(self,url,pubid,ctx):
        if url=='http://localhost:7083/WSAxonRespuestaTransacciones.svc?xsd=xsd2':
            return self.resolve_filename(str(assets/'WSAxonRespuestaTransacciones/xsd2.xml'),ctx)
        if Path(url).is_absolute() and Path(url).resolve().is_relative_to(assets):
            return None
        raise ValueError('Remote XSD blocked: '+url)
parser=E.XMLParser(resolve_entities=False,no_network=True,load_dtd=False)
parser.resolvers.add(OnlyLocal())
cfa=E.XMLSchema(E.parse(str(assets/'WSCFAACH/xsd0.xml'),parser))
axon=E.XMLSchema(E.parse(str(assets/'WSAxonRespuestaTransacciones/xsd0.xml'),parser))
NS='http://tempuri.org/'
def el(name,child=''):
    return E.fromstring(f'<t:{name} xmlns:t="{NS}" xmlns:xsi="http://www.w3.org/2001/XMLSchema-instance">{child}</t:{name}>'.encode())
test('CFA valid canonical origin element',cfa.validate(el('Proc_Transacciones','<t:NCTAORIG>real-origin</t:NCTAORIG>')))
test('CFA absent origin still XSD-valid',cfa.validate(el('Proc_Transacciones')))
test('CFA nil origin still XSD-valid',cfa.validate(el('Proc_Transacciones','<t:NCTAORIG xsi:nil="true"/>')))
test('CFA invalid int rejected',not cfa.validate(el('Proc_Transacciones','<t:TIPTRAN>bad</t:TIPTRAN>')))
test('CFA duplicate rejected',not cfa.validate(el('Proc_Transacciones','<t:TIPTRAN>22</t:TIPTRAN><t:TIPTRAN>22</t:TIPTRAN>')))
test('CFA unqualified element rejected',not cfa.validate(el('Proc_Transacciones','<TIPTRAN>22</TIPTRAN>')))
test('Axon valid seven fields',axon.validate(el('RegistrarRespuestaTransaccion','<t:idCanal>1</t:idCanal><t:nombreCanal>ACH</t:nombreCanal><t:idTransaccion>REF</t:idTransaccion><t:idEstado>2</t:idEstado><t:causal>R01</t:causal><t:idTransaccionAxon>3</t:idTransaccionAxon><t:descripcionCausal>Motivo</t:descripcionCausal>')))
test('Axon invalid int rejected',not axon.validate(el('RegistrarRespuestaTransaccion','<t:idCanal>bad</t:idCanal>')))
test('Axon duplicate rejected',not axon.validate(el('RegistrarRespuestaTransaccion','<t:idCanal>1</t:idCanal><t:idCanal>2</t:idCanal>')))
app=(root/'src/Cfa.ACHInterbank.Application/Integrations/Contracts/OfficialSoapContracts.cs').read_text()
c=(root/'src/Cfa.ACHInterbank.External/Connections/WscfaachSoapClient.cs').read_text()
a=(root/'src/Cfa.ACHInterbank.External/Connections/WsAxonRespuestaTransaccionesSoapClient.cs').read_text()
s=(root/'src/Cfa.ACHInterbank.Persistence/ACH/Services/Implementation/IncomingNachaPostProcessingPipeline.cs').read_text()
readiness=(root/'src/Cfa.ACHInterbank.Persistence/Integrations/Services/IntegrationMappingReadinessService.cs').read_text()
test('Shared outbound XML operation validator', 'public static void ValidateTransportRequest(' in app and 'ValidateElement(operation, expected);' in app)
test('SOAP11 body single/last & no unexpected elements', 'bodies.Length != 1' in app and 'headers.Length > 1' in app and 'root.Elements().LastOrDefault() != bodies[0]' in app)
test('DTD and resolver disabled', 'DtdProcessing = DtdProcessing.Prohibit' in app and 'XmlResolver = null' in app)
test('CFA validates raw and params at last transport boundary', 'OfficialSoapContracts.ValidateTransportRequest(' in c and 'var envelope = EnsureEnvelope(requestXml);' in c and c.index('ValidateTransportRequest(')<c.index('using var client = _httpClientFactory.CreateClient('))
test('Axon validates raw and params at last transport boundary', 'OfficialSoapContracts.ValidateTransportRequest(' in a and a.index('ValidateTransportRequest(')<a.index('using var client = _httpClientFactory.CreateClient('))
test('CFA non-affected methods remain untouched', 'if (string.Equals(action, "Proc_Transacciones", StringComparison.Ordinal))' in c)
test('Live guard unconditional fail closed', 'EnsureLiveOriginAccountHomologated(string requestXml)' in s and '_ = System.Xml.Linq.XDocument.Parse(requestXml);\n        throw new InvalidOperationException(' in s)
test('Readiness still denies unhomologated source','return ["PROC_TRANSACCIONES_NCTAORIG_CONTRACT_NOT_HOMOLOGATED"]' in readiness)
tests=(root/'tests/Cfa.ACHInterbank.Tests/WsAxonRespuestaTransaccionesSoapClientCharacterizationTests.cs').read_text()
test('Axon previous invalid positive test updated', 'var body = ValidAxonBody;' in tests and 'NptH01_Axon_PublicXmlOverload_RejectsInvalidBodyBeforeHttp' in tests)
test('CFA no HTTP negative tests',(root/'tests/Cfa.ACHInterbank.Tests/WscfaachSoapClientCharacterizationTests.cs').read_text().count('Assert.Empty(server.Requests);')>=2)
test('Live missing/empty/nil tests', all(x in (root/'tests/Cfa.ACHInterbank.Tests/ProcTransaccionesOriginAccountSafetyTests.cs').read_text() for x in ("xsi:nil='true'",'Live_RejectsAbsentEmptyOrNilOriginAccountUntilHomologated')))
test('Contradictory readiness test corrected','ProcTransaccionesReadiness_CanonicalOriginSourceStillRequiresBankHomologation' in (root/'tests/Cfa.ACHInterbank.Tests/NachaDesagregadoIntegrationMappingTests.cs').read_text())
result={'kind':'lxml_xsd_plus_source_characterization_not_dotnet','total':len(checks),'passed':sum(t['pass'] for t in checks),'checks':checks}
(root/'docs/npt-transport-xsd-h010203/evidencias/offline_h010203.json').write_text(json.dumps(result,indent=2)+'\n')
print(f"{result['passed']}/{result['total']} PASS (not xUnit)")
sys.exit(0 if result['passed']==result['total'] else 1)

#!/usr/bin/env python3
"""Independent offline XMLSchema/WSDL checks (lxml, NOT dotnet/xUnit).
Runs on the 7 untouched bank-supplied XML files embedded in the Application project.
No connections, HTTP, DB, SOAP transport or UAT. Strict resolver only for supplied xsd2.
"""
import json
from pathlib import Path
from lxml import etree as E

ROOT = Path(__file__).resolve().parents[2]
SRC = ROOT / 'src' / 'Cfa.ACHInterbank.Application' / 'Integrations' / 'Contracts' / 'OfficialSoapSchemas'
XSD = 'http://www.w3.org/2001/XMLSchema'
WSDL = 'http://schemas.xmlsoap.org/wsdl/'
SOAP = 'http://schemas.xmlsoap.org/wsdl/soap/'
TN = 'http://tempuri.org/'
AX = 'http://schemas.datacontract.org/2004/07/WSCFAACH'
passed = []

class OnlyLocalXsdResolver(E.Resolver):
    def resolve(self, url, pubid, context):
        if url == 'http://localhost:7083/WSAxonRespuestaTransacciones.svc?xsd=xsd2':
            return self.resolve_filename(str(SRC / 'WSAxonRespuestaTransacciones/xsd2.xml'), context)
        if Path(url).is_absolute() and Path(url).resolve().is_relative_to(SRC):
            return None  # Already bundled absolute path, normal local IO.
        raise ValueError('External XSD location blocked: ' + url)

parser = E.XMLParser(resolve_entities=False, no_network=True, load_dtd=False, huge_tree=False)
parser.resolvers.add(OnlyLocalXsdResolver())

def verify(tag, test):
    assert test, tag
    passed.append(tag)

schemas = {}
for service, files in [('WSCFAACH', ('xsd0.xml','xsd1.xml')),
                       ('WSAxonRespuestaTransacciones', ('xsd0.xml','xsd1.xml','xsd2.xml'))]:
    for filename in files:
        doc = E.parse(str(SRC/service/filename), parser)
        schema = E.XMLSchema(doc)
        schemas[(service, filename)] = schema
        passed.append('compile:'+service+'/'+filename)

for service, op, binding, action, n in (
    ('WSCFAACH','Proc_Transacciones','IWSCFAACH','http://tempuri.org/IWSCFAACH/Proc_Transacciones',2),
    ('WSAxonRespuestaTransacciones','RegistrarRespuestaTransaccion','IWSAxonRespuestaTransacciones',
     'http://tempuri.org/IWSAxonRespuestaTransacciones/RegistrarRespuestaTransaccion',3)):
    w = E.parse(str(SRC/service/(service+'.wsdl.xml')), parser)
    defs = w.getroot()
    verify(service+':wsdl-tns', defs.get('targetNamespace') == TN)
    imports = defs.findall('.//{%s}import' % XSD)
    verify(service+':imports-count', len(imports) == n and all('?xsd=xsd' in (i.get('schemaLocation') or '') for i in imports))
    binds = defs.findall('.//{%s}binding' % WSDL)
    matched = [b for b in binds if b.get('type') == 'tns:'+binding]
    verify(service+':soap11', len(matched)==1 and matched[0].find('{%s}binding'%SOAP).get('transport') == 'http://schemas.xmlsoap.org/soap/http')
    ops = [x for x in matched[0].findall('{%s}operation'%WSDL) if x.get('name')==op]
    verify(service+':soapaction', len(ops)==1 and ops[0].find('{%s}operation'%SOAP).get('soapAction') == action)
    verify(service+':document-literal', ops[0].find('{%s}operation'%SOAP).get('style')=='document' and all(
        ops[0].find('{%s}%s/{%s}body'%(WSDL,x,SOAP)).get('use')=='literal' for x in ('input','output')))
    parts = [p.get('element') for p in defs.findall('.//{%s}message/{%s}part'%(WSDL,WSDL))]
    verify(service+':request-response-messages', 'tns:'+op in parts and 'tns:'+op+'Response' in parts)

cfa=schemas[('WSCFAACH','xsd0.xml')]
axon=schemas[('WSAxonRespuestaTransacciones','xsd0.xml')]

def elt(name, children='', ns=TN):
    return E.fromstring(('<t:%s xmlns:t="%s" xmlns:a="%s" xmlns:xsi="http://www.w3.org/2001/XMLSchema-instance">%s</t:%s>' % (name, ns, AX, children, name)).encode())

def valid(schema, root, expected, tag):
    observed=schema.validate(root)
    verify(tag, observed == expected)

valid(cfa,elt('Proc_Transacciones','<t:TIPTRAN>22</t:TIPTRAN><t:NCTAORIG>ORIG</t:NCTAORIG><t:FECEFEC>20261116</t:FECEFEC><t:NCTARECEP>RECEP</t:NCTARECEP><t:MONTO>1500.25</t:MONTO><t:IDTRAN>123456</t:IDTRAN>'),True,'CFA:request-valid')
valid(cfa,elt('Proc_Transacciones','<t:MONTO>10</t:MONTO><t:TIPTRAN>22</t:TIPTRAN>'),False,'CFA:order-reversed')
valid(cfa,elt('Proc_Transacciones','<t:TIPTRAN>22</t:TIPTRAN><t:TIPTRAN>22</t:TIPTRAN>'),False,'CFA:duplicate')
valid(cfa,elt('Proc_Transacciones','<t:MONTO>bad-double</t:MONTO>'),False,'CFA:invalid-double')
valid(cfa,elt('Proc_Transacciones','<t:FECEFEC>999999999999</t:FECEFEC>'),False,'CFA:invalid-int')
valid(cfa,elt('Proc_Transacciones','<t:IDTRAN>not-a-long</t:IDTRAN>'),False,'CFA:invalid-long')
valid(cfa,elt('Proc_Transacciones','<t:TIPTRAN xsi:nil="true"/>'),False,'CFA:nil-on-int')
valid(cfa,elt('Proc_Transacciones','<TIPTRAN>22</TIPTRAN>'),False,'CFA:unqualified-child')
valid(cfa,elt('Proc_TransaccionesResponse','<t:RTAACH>00</t:RTAACH><t:RTALOC>OK</t:RTALOC>'),True,'CFA:response-valid')
valid(cfa,elt('Proc_TransaccionesResponse','<t:RTALOC>OK</t:RTALOC><t:RTAACH>00</t:RTAACH>'),False,'CFA:response-order')
valid(cfa,elt('Proc_TransaccionesResponse','<t:RTAACH>00</t:RTAACH><t:RTAACH>00</t:RTAACH>'),False,'CFA:response-duplicate')
valid(cfa,elt('Proc_TransaccionesResponse',''),True,'CFA:xsd-allows-missing-code-security-does-not')
valid(axon,elt('RegistrarRespuestaTransaccion','<t:idCanal>1</t:idCanal><t:nombreCanal>ACH</t:nombreCanal><t:idTransaccion>1</t:idTransaccion><t:idEstado>2</t:idEstado><t:causal>R01</t:causal><t:idTransaccionAxon>3</t:idTransaccionAxon><t:descripcionCausal>C</t:descripcionCausal>'),True,'AXON:request-valid')
valid(axon,elt('RegistrarRespuestaTransaccion','<t:idCanal>bad</t:idCanal>'),False,'AXON:request-invalid-int')
valid(axon,elt('RegistrarRespuestaTransaccionResponse','<t:RegistrarRespuestaTransaccionResult><a:codigoError/><a:descripcionError/><a:existeError>false</a:existeError></t:RegistrarRespuestaTransaccionResult>'),True,'AXON:response-valid')
valid(axon,elt('RegistrarRespuestaTransaccionResponse','<t:RegistrarRespuestaTransaccionResult><a:existeError>0</a:existeError></t:RegistrarRespuestaTransaccionResult>'),True,'AXON:response-boolean-zero')
valid(axon,elt('RegistrarRespuestaTransaccionResponse','<t:RegistrarRespuestaTransaccionResult><a:existeError>maybe</a:existeError></t:RegistrarRespuestaTransaccionResult>'),False,'AXON:invalid-boolean')
valid(axon,elt('RegistrarRespuestaTransaccionResponse','<t:RegistrarRespuestaTransaccionResult><a:existeError>true</a:existeError><a:existeError>false</a:existeError></t:RegistrarRespuestaTransaccionResult>'),False,'AXON:duplicate-result-field')
valid(axon,elt('RegistrarRespuestaTransaccionResponse','<t:RegistrarRespuestaTransaccionResult><t:existeError>false</t:existeError></t:RegistrarRespuestaTransaccionResult>'),False,'AXON:wrong-datacontract-ns')
valid(axon,elt('RegistrarRespuestaTransaccionResponse','<t:RegistrarRespuestaTransaccionResult><a:existeError xsi:nil="true"/></t:RegistrarRespuestaTransaccionResult>'),False,'AXON:nil-boolean')
valid(axon,elt('RegistrarRespuestaTransaccionResponse',''),True,'AXON:xsd-allows-missing-result-security-does-not')

print(json.dumps({'outcome':'PASS','checks':len(passed),'checked':passed,'note':'lxml XMLSchema/WSDL offline, not dotnet, not SOAP integration'}, indent=2))

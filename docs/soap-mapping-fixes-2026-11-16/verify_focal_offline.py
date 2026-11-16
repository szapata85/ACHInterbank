#!/usr/bin/env python3
"""Pruebas offline focales SOAP (lxml + chequeos estáticos); NO ejecuta .NET, bancos ni UAT."""
from pathlib import Path
from lxml import etree as E
import json

ROOT = Path(__file__).resolve().parents[2]
SCHEMAS = ROOT / 'src/Cfa.ACHInterbank.Application/Integrations/Contracts/OfficialSoapSchemas'
XSD = '{http://www.w3.org/2001/XMLSchema}'
NS = 'http://tempuri.org/'
checks=[]
def check(name, yes):
    if not yes: raise AssertionError(name)
    checks.append(name)

class LocalSchemaOnly(E.Resolver):
    def resolve(self, url, pubid, context):
        if url == 'http://localhost:7083/WSAxonRespuestaTransacciones.svc?xsd=xsd2':
            return self.resolve_filename(str(SCHEMAS/'WSAxonRespuestaTransacciones/xsd2.xml'), context)
        if Path(url).is_absolute() and Path(url).resolve().is_relative_to(SCHEMAS):
            return None
        raise ValueError('schema remoto prohibido')
parser=E.XMLParser(resolve_entities=False,load_dtd=False,no_network=True)
parser.resolvers.add(LocalSchemaOnly())
cfa=E.XMLSchema(E.parse(str(SCHEMAS/'WSCFAACH/xsd0.xml'),parser))
axon=E.XMLSchema(E.parse(str(SCHEMAS/'WSAxonRespuestaTransacciones/xsd0.xml'),parser))

names=('Proc_Transacciones','Proc_TransaccionesResponse','Proc_Contrapartidas','Proc_ContrapartidasResponse')
xdoc=E.parse(str(SCHEMAS/'WSCFAACH/xsd0.xml'),parser)
adoc=E.parse(str(SCHEMAS/'WSAxonRespuestaTransacciones/xsd0.xml'),parser)
counts={}
for doc,operations in ((xdoc,names),(adoc,('RegistrarRespuestaTransaccion','RegistrarRespuestaTransaccionResponse'))):
    for op in operations:
        el=doc.getroot().find(f"{XSD}element[@name='{op}']")
        check('schema-operation:'+op,el is not None)
        fields=el.findall(f'{XSD}complexType/{XSD}sequence/{XSD}element')
        if op == 'RegistrarRespuestaTransaccionResponse':
            nested = E.parse(str(SCHEMAS/'WSAxonRespuestaTransacciones/xsd2.xml'),parser)
            responseType = nested.getroot().find(f"{XSD}complexType[@name='rtaRegistrarRespuestaTransaccion']")
            check('axon-response-type', responseType is not None)
            responseFields = responseType.findall(f'{XSD}sequence/{XSD}element')
            check('axon-response-all-optional', all(f.get('minOccurs')=='0' for f in responseFields))
            counts[op] = len(responseFields)
        else:
            counts[op]=len(fields)
        check('all-optional:'+op,all(f.get('minOccurs')=='0' for f in fields))
wsdl=E.parse(str(SCHEMAS/'WSCFAACH/WSCFAACH.wsdl.xml'),parser)
soap_ops=wsdl.xpath("//*[local-name()='binding' and @type='tns:IWSCFAACH']/*[local-name()='operation' and @name='Proc_Contrapartidas']/*[local-name()='operation']")
check('wsdl-contra-soapaction',len(soap_ops)==1 and soap_ops[0].get('soapAction')=='http://tempuri.org/IWSCFAACH/Proc_Contrapartidas')
check('66-fields',counts=={'Proc_Transacciones':27,'Proc_TransaccionesResponse':2,'Proc_Contrapartidas':22,'Proc_ContrapartidasResponse':5,'RegistrarRespuestaTransaccion':7,'RegistrarRespuestaTransaccionResponse':3})

def element(name,fields):
    root=E.Element('{%s}%s'%(NS,name),nsmap={'b':NS})
    for key,value in fields:
        e=E.SubElement(root,'{%s}%s'%(NS,key));e.text=value
    return root

def valid(schema, name, fields, expected):
    check(name+(':valid' if expected else ':rejected'),schema.validate(element(name,fields)) is expected)

contra=[('OFNIT','900000001'),('OFEMP','EMPRESA_REAL'),('OFCTA','123456789'),('OFDD','D'),
 ('OFFECHEFEC','20261116'),('OFMONDEB','25.75'),('OFMONCRE','0'),('OFIDARCH','8'),('OFIDLOT','12'),
 ('OFST','OO'),('OFIDTX','REF'),('OFIDREVER','0'),('OFIDEBAPLI','15'),('OFIDCAMCOMPE','1'),
 ('OFDIRECCIONIP','192.0.2.1'),('OFLIBRE',''),('OFLIBRE1','15'),('ANSIDLOTE','0'),('ANSST',''),
 ('ANCLC',''),('ANSIDTX',''),('ANSIDREVER','0')]
valid(cfa,'Proc_Contrapartidas',contra,True)
valid(cfa,'Proc_Contrapartidas',[],True)
valid(cfa,'Proc_Contrapartidas',[('OFIDARCH','no-int')],False)
valid(cfa,'Proc_Contrapartidas',[('OFMONDEB','no-decimal')],False)
valid(cfa,'Proc_Contrapartidas',[('OFIDLOT','1.5')],False)
valid(cfa,'Proc_Contrapartidas',[('OFCTA','1'),('OFNIT','2')],False)
valid(cfa,'Proc_ContrapartidasResponse',[('ANSIDLOTE','8'),('ANSST','OK'),('ANCLC','00'),('ANSIDTX','REF'),('ANSIDREVER','0')],True)
valid(cfa,'Proc_ContrapartidasResponse',[('ANSIDLOTE','invalid')],False)
valid(cfa,'Proc_Transacciones',[('NORIG','ORIG'),('IDORIG','ID')],True)
valid(cfa,'Proc_Transacciones',[('NCTARECEP','009888'),('MONTO','10')],True)
valid(cfa,'Proc_Transacciones',[('NCTARECEP','009888'),('MONTO','invalid')],False)
valid(cfa,'Proc_TransaccionesResponse',[('RTAACH','00'),('RTALOC','OK')],True)
valid(axon,'RegistrarRespuestaTransaccion',[
 ('idCanal','1'),('nombreCanal','ACH'),('idTransaccion','REF'),('idEstado','2'),('causal','R01'),
 ('idTransaccionAxon','42'),('descripcionCausal','FONDOS')],True)
valid(axon,'RegistrarRespuestaTransaccion',[('idEstado','invalid')],False)
axns='http://schemas.datacontract.org/2004/07/WSCFAACH'
response=E.Element('{%s}RegistrarRespuestaTransaccionResponse'%NS)
result=E.SubElement(response,'{%s}RegistrarRespuestaTransaccionResult'%NS)
for name,val in (('codigoError',''),('descripcionError',''),('existeError','false')):
    child=E.SubElement(result,'{%s}%s'%(axns,name));child.text=val
check('axon-response-false',axon.validate(response))
result[-1].text='mal';check('axon-response-invalid-bool',not axon.validate(response))

base=ROOT/'src'
bootstrap=(base/'Cfa.ACHInterbank.Persistence/Integrations/Services/IntegrationMappingBootstrapper.cs').read_text()
provider=(base/'Cfa.ACHInterbank.Persistence/Integrations/Providers/CurrentIntegrationSourceProviderFactory.cs').read_text()
functional=(base/'Cfa.ACHInterbank.Persistence/ACH/Services/Implementation/ProcTransaccionesFunctionalSourceResolver.cs').read_text()
resolver=(base/'Cfa.ACHInterbank.Persistence/Integrations/Services/ProcContrapartidasFunctionalMappingResolver.cs').read_text()
mapper=(base/'Cfa.ACHInterbank.Persistence/ACH/Services/Implementation/ProcTransaccionesRequestMapper.cs').read_text()
contraMapper=(base/'Cfa.ACHInterbank.Persistence/ACH/Services/Implementation/ProcContrapartidasRequestMapper.cs').read_text()
soap=(base/'Cfa.ACHInterbank.External/Connections/WscfaachSoapClient.cs').read_text()
for field,source in [('NORIG','batchHeaders.companyName'),('IDORIG','batchHeaders.companyId'),('NCTARECEP','entrydetails.accountnumber'),('NCTAORIG','transaction.sourceAccountNumber')]:
    check('mapping-inbound:'+field, f'"{field}" => "{source}"' in bootstrap)
    check('provider:'+field, source in provider)
check('physical-originator-batch', 'batch.CompanyName' in functional and 'batch.CompanyId' in functional)
check('physical-recipient-entry', 'entry.AccountNumber' in functional and 'ValidateMonetaryEntryCorrelation(transaction, entry)' in functional)
check('outbound-account-kept','"OFCTA", IntegrationSourceKindEnum.Transaction, "transaction.sourceAccountNumber"' in bootstrap)
check('outbound-fake-seed-removed',all(f'AddPathRule("{f}"' not in bootstrap for f in ('OFEMP','OFIDARCH','OFDIRECCIONIP')))
for f in ('OFEMP','OFIDARCH','OFDIRECCIONIP'):
    check('missing-field-blocked:'+f, f'"{f}"' in resolver and f'"{f}"' in bootstrap)
check('origin-optional-no-fabrication','string.IsNullOrWhiteSpace(transactionSourceAccount)' in mapper and 'return; // No fabricated source' in mapper)
check('xsd-contra-serializer','Operation.ProcContrapartidasRequest' in contraMapper)
check('xsd-contra-transport','Operation.ProcContrapartidasRequest' in soap)
check('axon-provider-unchanged','differentialResponse.codigoCausalExterna' in provider)
print(json.dumps({'outcome':'PASS','checks':len(checks),'contract_fields':counts,'note':'Offline: lxml/XMLSchema y aserciones estáticas. No es compilación .NET ni ejecución C#.'},ensure_ascii=False,indent=2))

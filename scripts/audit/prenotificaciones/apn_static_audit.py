#!/usr/bin/env python3
"""Read-only, offline checks for APN. No C# execution and no normative certification."""
from pathlib import Path
import json, hashlib, re, sys, datetime
ROOT=Path(__file__).resolve().parents[3]
ITEMS=[
 ('APN-I01','enum_validacion','src/Cfa.ACHInterbank.Domain/Models/ACH/Enums/ThirdPartyAccountValidationEnums.cs',r'PendingResponse\s*=\s*3.*TacitlyAccepted\s*=\s*5',re.S),
 ('APN-I01','intentos','src/Cfa.ACHInterbank.Domain/Models/ACH/PrenotificationValidationAttempt.cs',r'ResponseDeadlineAtUtc',0),
 ('APN-I01','transiciones','src/Cfa.ACHInterbank.Persistence/ACH/Services/Implementation/AchStateTransitionService.cs',r'AppliedTacitly.*ReturnedByEpr',re.S),
 ('APN-I02','catalogo_7','src/Cfa.ACHInterbank.Application/ACH/Models/RegulatoryCycleScheduleCatalog.cs',r'ACHCOL-7C-20261014',0),
 ('APN-I02','calendario_7','src/Cfa.ACHInterbank.Application/ACH/Models/RegulatoryCycleScheduleCatalog.cs',r'AchColombiaSevenCycleConfirmedPolicy',0),
 ('APN-I02','devoluciones_7','src/Cfa.ACHInterbank.Application/ACH/Models/RegulatoryCycleScheduleCatalog.cs',r'AchColombiaSevenCycleReturnWindows',0),
 ('APN-I03','catalogo_CENIT','src/Cfa.ACHInterbank.Application/ACH/Models/RegulatoryCycleScheduleCatalog.cs',r'CenitDsp152Cycles',0),
 ('APN-I03','regla_cenit','src/Cfa.ACHInterbank.Persistence/ACH/Services/Implementation/Seeders/RegulatoryCatalogSeeder.cs',r'PrenotificationResponseDeadlineMode\.OperationalDayEnd',0),
 ('APN-I04','ingesta','src/Cfa.ACHInterbank.Persistence/ACH/Services/Implementation/IncomingNachaPostParseProcessor.cs',r'_linker\.LinkAsync',0),
 ('APN-I04','resolucion','src/Cfa.ACHInterbank.Persistence/ACH/Services/Implementation/IncomingNachaPrenotificationResolver.cs',r'prenotification|Prenotification',re.I),
 ('APN-I04','guarda','src/Cfa.ACHInterbank.Persistence/ACH/Services/Implementation/IncomingPrenotificationReturnGuard.cs',r'class IncomingPrenotificationReturnGuard',0),
 ('APN-I05','job','src/Cfa.ACHInterbank.Persistence/ACH/Quartz/Jobs/Implementation/AchTacitAcceptanceJobHandler.cs',r'ExecuteAsync',0),
 ('APN-I05','job_deadline','src/Cfa.ACHInterbank.Persistence/ACH/Quartz/Jobs/Implementation/AchTacitAcceptanceJobHandler.cs',r'ResponseDeadlineAtUtc\.Value <= utcNow',0),
 ('APN-I05','conversor_local','src/Cfa.ACHInterbank.Persistence/ACH/Services/Implementation/ClearingHouseValidationPolicyService.cs',r'ConvertLocalToInstant',0),
 ('APN-I05','cantidad_4','src/Cfa.ACHInterbank.Persistence/ACH/Services/Implementation/Seeders/RegulatoryCatalogSeeder.cs',r'PrenotificationResponseWindowCycles\s*=\s*4',0),
 ('APN-I05','job_30_min','src/Cfa.ACHInterbank.Persistence/ACH/Services/Implementation/Seeders/TaskDefinitionSeeder.cs',r'Code = "AchTacitAcceptanceJob".*?N = 30',re.S),
 ('APN-I06','sincroniza_validacion','src/Cfa.ACHInterbank.Persistence/ACH/Services/Implementation/AchStateTransitionService.cs',r'SynchronizeCanonicalPrenotificationValidationAsync',0),
 ('APN-I06','sincroniza_tercero','src/Cfa.ACHInterbank.Persistence/ACH/Services/Implementation/AchStateTransitionService.cs',r'SynchronizePrenotificationThirdPartyAsync',0),
 ('APN-I06','terminales','src/Cfa.ACHInterbank.Persistence/ACH/Services/Implementation/ThirdPartyAccountValidationService.cs',r'IsTerminalAttempt',0),
 ('APN-I07','prueba_previa','tests/Cfa.ACHInterbank.Tests/PrenotificationSimulatorTacitAcceptanceI05Tests.cs',r'IsDue|Tacit',re.I),
]
RISKS=[
 ('APN-R01','src/Cfa.ACHInterbank.Persistence/ACH/Services/Implementation/ClearingHouseValidationPolicyService.cs',r'Add\(deadlineCycle\.CutoffTime\)',0),
 ('APN-R02','src/Cfa.ACHInterbank.Persistence/ACH/Services/Implementation/ClearingHouseValidationPolicyService.cs',r'\.Take\(window\).*?\.LastOrDefault\(\)',re.S),
 ('APN-R03','src/Cfa.ACHInterbank.Persistence/ACH/Quartz/Jobs/Implementation/AchTacitAcceptanceJobHandler.cs',r'&& x.Status == PrenotificationValidationAttemptStatus.AwaitingResponse',0),
 ('APN-R04','src/Cfa.ACHInterbank.Persistence/ACH/Services/Implementation/ThirdPartyAccountValidationService.cs',r'if \(IsTerminalAttempt\(attempt\)\)\s*\{\s*return false;',re.S),
]
def read(path):
    p=ROOT/path
    return p, p.read_text(encoding='utf-8-sig') if p.is_file() else None

def run():
    checks=[]; risks=[]
    for inc,key,path,pattern,flags in ITEMS:
        p,s=read(path); found=bool(s is not None and re.search(pattern,s,flags))
        checks.append(dict(increment=inc,key=key,source=path,result='FOUND_STATIC' if found else 'MISSING_OR_UNREADABLE',sha256=hashlib.sha256(p.read_bytes()).hexdigest() if s is not None else None))
    for key,path,pattern,flags in RISKS:
        _,s=read(path);risks.append(dict(id=key,source=path,result='PATTERN_PRESENT_FOR_REVIEW' if s is not None and re.search(pattern,s,flags) else 'PATTERN_NOT_FOUND_REVIEW_MANUALLY'))
    out={'tool':'APN read-only static checker','generated_utc':datetime.datetime.now(datetime.timezone.utc).isoformat(),'basis':'source code structure only; not runtime conformance','project_root':str(ROOT.name),'results':checks,'risks':risks,'summary':{'checks':len(checks),'found':sum(x['result']=='FOUND_STATIC' for x in checks),'missing':sum(x['result']!='FOUND_STATIC' for x in checks),'runtime_tests_executed':0}}
    target=ROOT/'docs/auditoria-prenotificaciones/evidencias/APN_EVIDENCIA_ESTATICA.json'
    target.parent.mkdir(parents=True,exist_ok=True);target.write_text(json.dumps(out,ensure_ascii=False,indent=2)+'\n',encoding='utf-8')
    print(json.dumps(out['summary'],ensure_ascii=False))
    return 0 if out['summary']['missing']==0 else 1
if __name__=='__main__': sys.exit(run())

#!/usr/bin/env python3
"""Checks I01 textual/structural invariants only; NOT C# compilation or behavior tests."""
from pathlib import Path
import re
import sys

root = Path(__file__).resolve().parents[2]
checks = []

def read(p): return (root / p).read_text(encoding="utf-8")
def check(name, ok):
    checks.append((name, bool(ok)))
    print(("PASS_STATIC" if ok else "FAIL_STATIC"), name)

enums = read("src/Cfa.ACHInterbank.Domain/Models/ACH/Enums/NachaInboundSimulationEnums.cs")
models = read("src/Cfa.ACHInterbank.Application/ACH/Models/NachaInboundSimulationModels.cs")
contract = read("src/Cfa.ACHInterbank.Application/ACH/Services/PrenotificationSimulationContract.cs")
service = read("src/Cfa.ACHInterbank.Persistence/ACH/Services/Implementation/NachaInboundSimulationService.cs")
controller = read("src/Cfa.ACHInterbank.Api/Controllers/NachaInboundSimulatorController.cs")
tests = read("tests/Cfa.ACHInterbank.Tests/PrenotificationSimulatorContractI01Tests.cs")
legacy_tests = read("tests/Cfa.ACHInterbank.Tests/NachaInboundSimulatorTests.cs")

check("legacy scenario retains number 3", "IncomingPrenotificationResponse = 3" in enums)
check("new scenario number is distinct 10", "IncomingPrenotificationReturn = 10" in enums)
check("typed outcome has only two semantic branches", "Returned = 1" in enums and "NoReturnByDeadline = 2" in enums)
check("contract includes original ids", "OriginalPrenotificationIds" in models)
check("contract separates original / response cycles", "OriginalCycleId" in models and "ResponseCycleId" in models)
check("contract includes chamber + bank", "ClearingHouseCode" in models and "RespondingFinancialInstitutionId" in models)
check("contract includes business date / reason / idempotency", all(x in models for x in ("BusinessDate", "ReasonCode", "IdempotencyKey")))
check("outcome JSON string converter explicit", models.count("JsonConverter(typeof(JsonStringEnumConverter))") >= 5)
check("contract validates duplicate IDs", "PRENOTIFICATION_IDS_DUPLICATED" in contract)
check("contract validates return fields", all(x in contract for x in ("RESPONSE_CYCLE_REQUIRED", "RETURN_REASON_REQUIRED", "IDEMPOTENCY_KEY_REQUIRED")))
check("no-return forbids false response attributes", "NO_RETURN_HAS_NO_REASON" in contract and "NO_RETURN_HAS_NO_RESPONSE_CYCLE" in contract)
check("new capability explicitly advertises not operational", all('false' in x for x in re.findall(r'new\(PrenotificationSimulationOutcome\.[^;]+?\)',contract,re.S)) if False else "NoReturnByDeadline, false, false, false" in contract and "Returned, true, true, false" in contract)
check("routes have read authorization", controller.count('[Authorize(Policy = P1Policies.NachaSimulatorRead)]') >= 2 and '"prenotifications/contract"' in controller and '"prenotifications/validate-contract"' in controller)
check("controller only calls pure validation/description", "PrenotificationSimulationContract.Validate(request)" in controller and "PrenotificationSimulationContract.Describe()" in controller)
check("service has fail-closed new scenario and explicit diagnostic", "PRENOTIFICATION_RETURN_NOT_IMPLEMENTED" in service)
check("guard precedes any downstream build in Preview", service.index('request.ScenarioType == NachaInboundSimulationType.IncomingPrenotificationReturn') < service.index('if (request.SimulationMode == NachaSimulationMode.IncomingTransactions\n            && RequiresTransactionReferences'))
check("generate always runs preview before building", service.index('var preview = await PreviewAsync') < service.index('var build = IsReturnScenario'))
check("pure C# contract test cases added", tests.count("[Fact]") >= 6 and tests.count("[InlineData(") >= 4)
check("fail-closed service test added", "I01_NewPrenotificationReturnMustFailClosedWithoutCreatingFileOrState" in legacy_tests)
check("contract documentation included", (root / "docs/implementacion-simulador-prenotificaciones/CONTRATO_PRENOTIFICACIONES_I01.md").exists())
failed = [name for name, result in checks if not result]
print(f"SUMMARY: {len(checks)-len(failed)}/{len(checks)} PASS_STATIC; {len(failed)} FAIL_STATIC; C# runtime NOT EXECUTED")
sys.exit(1 if failed else 0)

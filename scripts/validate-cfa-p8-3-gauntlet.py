#!/usr/bin/env python3
from __future__ import annotations

import json
import re
import shutil
import subprocess
import sys
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
ARTIFACT = ROOT / "artifacts/p8.3-gauntlet-validation.json"

checks: dict[str, dict[str, object]] = {}
failures: list[str] = []


def read(path: str) -> str:
    return (ROOT / path).read_text(encoding="utf-8")


def pass_check(name: str, evidence: object) -> None:
    checks[name] = {"status": "PASS", "evidence": evidence}


def fail_check(name: str, evidence: object) -> None:
    checks[name] = {"status": "FAIL", "evidence": evidence}
    failures.append(name)


def require_all(name: str, text: str, needles: list[str]) -> None:
    missing = [needle for needle in needles if needle not in text]
    if missing:
        fail_check(name, {"missing": missing})
    else:
        pass_check(name, {"required": needles})


def forbid_any(name: str, text: str, needles: list[str], *, ignore_case: bool = False) -> None:
    haystack = text.casefold() if ignore_case else text
    found = [needle for needle in needles if (needle.casefold() if ignore_case else needle) in haystack]
    if found:
        fail_check(name, {"forbidden_found": found})
    else:
        pass_check(name, {"forbidden": needles})


# Preserve and execute the P8.1/P8.2 ratchet first.
p81 = subprocess.run(
    [sys.executable, str(ROOT / "scripts/validate-cfa-p8-1-static.py")],
    cwd=ROOT,
    text=True,
    capture_output=True,
)
if p81.returncode == 0:
    pass_check("p8_1_p8_2_ratchet", "scripts/validate-cfa-p8-1-static.py returned 0")
else:
    fail_check("p8_1_p8_2_ratchet", (p81.stderr or p81.stdout)[-4000:])

writer = read("src/Cfa.ACHInterbank.Application/Integrations/Services/ContractDrivenSoapBodyWriter.cs")
resolver = read("src/Cfa.ACHInterbank.Application/Integrations/Services/IntegrationRequestResolver.cs")
value_resolver = read("src/Cfa.ACHInterbank.Application/Integrations/Services/IntegrationParameterValueResolver.cs")
source_registry = read("src/Cfa.ACHInterbank.Application/Integrations/Registries/IntegrationSourceProviderRegistry.cs")
analyzer = read("src/Cfa.ACHInterbank.Persistence/Integrations/Services/IntegrationContractArtifactAnalyzer.cs")
mapping_validation = read("src/Cfa.ACHInterbank.Persistence/Integrations/Services/IntegrationMappingValidationService.cs")
contract_service = read("src/Cfa.ACHInterbank.Persistence/Integrations/Services/IntegrationContractVersionService.cs")
execution_contract_resolver = read("src/Cfa.ACHInterbank.Persistence/Integrations/Services/IntegrationExecutionContractResolver.cs")
routes = read("web/ach-interbank-ui/src/app/features/integrations/integrations.routes.ts")
mapping_editor = read("web/ach-interbank-ui/src/app/features/integrations/pages/mapping-editor-page.component.ts")

require_all(
    "parameter_injection_and_output_guard",
    writer,
    [
        "RESOLVED_REQUEST_PARAMETER_NOT_IN_CONTRACT",
        "ValidateResolvedMetadata(binding, resolved)",
        "RESOLVED_REQUEST_OUTPUT_PARAMETER",
        "binding.Direction != IntegrationParameterDirectionEnum.Input",
    ],
)
require_all(
    "malicious_xml_name_guard",
    writer,
    [
        "XmlConvert.VerifyNCName(contract.OperationName)",
        "XmlConvert.VerifyNCName(binding.XmlElementName)",
        "new XElement(targetNamespace + binding.XmlElementName)",
        "RESOLVED_REQUEST_CONTRACT_MISMATCH",
    ],
)
require_all(
    "required_and_order_guard",
    writer,
    [
        "RESOLVED_REQUEST_PARAMETER_MISSING",
        "RESOLVED_REQUEST_REQUIRED_OMITTED",
        "RESOLVED_REQUEST_NULL_NOT_ALLOWED",
        "OrderBy(parameter => parameter.SortOrder)",
    ],
)
require_all(
    "type_boundary_guard",
    value_resolver,
    [
        "SOURCE_TYPE_INCOMPATIBLE",
        "SOURCE_VALUE_REQUIRED",
        "IntegrationTypeCompatibility.CanRepresent",
        "decimal.TryParse",
        "bool.TryParse",
    ],
)
require_all(
    "wsdl_xxe_dtd_guard",
    analyzer,
    [
        "DtdProcessing = DtdProcessing.Prohibit",
        "XmlResolver = null",
        "MaxCharactersFromEntities = 0",
        "ValidateNoExternalReferences(document, issues)",
        "EXTERNAL_REFERENCE_NOT_ALLOWED",
    ],
)
require_all(
    "cross_method_source_guard",
    source_registry + "\n" + mapping_validation,
    [
        "provider.Descriptor.SupportsMethod(methodCode)",
        "SOURCE_METHOD_INCOMPATIBLE",
        ".Where(x => x.MethodId == set.MethodId && x.IsActive)",
        "SOURCE_FIELD_NOT_FOUND",
    ],
)
require_all(
    "cross_version_guard",
    execution_contract_resolver + "\n" + mapping_validation + "\n" + contract_service,
    [
        "x.ContractVersionId == contract.Id",
        "RULE_PARAMETER_NOT_IN_CONTRACT",
        "mapping.ContractVersionId != contract.Id",
        "EnsureMappingBelongsToContract(mapping, contract)",
    ],
)
require_all(
    "concurrency_guard",
    contract_service,
    [
        "IsolationLevel.Serializable",
        "EnsureExpectedRevision(contract, request.ExpectedContractRevision)",
        "EnsureExpectedRevision(mapping, request.ExpectedMappingRevision)",
        "EnsureRevisionsStillMatchAsync",
        "DbUpdateConcurrencyException",
    ],
)
require_all(
    "activation_rollback_guard",
    contract_service,
    [
        "var validation = await _mappingValidationService.EvaluateAsync(mapping.Id",
        "if (!validation.IsValid)",
        "ContractVersion + MappingSet no están completos/compatibles",
        "var validation = await _mappingValidationService.EvaluateAsync(targetMapping.Id",
        "rollback rechazado sin modificar el estado vigente",
    ],
)
require_all(
    "contract_version_authority",
    execution_contract_resolver + "\n" + resolver,
    [
        "IntegrationContractVersionStatusEnum.Active",
        "x.ContractVersionId == contract.Id",
        "_contractResolver.ResolveActiveAsync(context.MethodCode",
        "contract.Parameters",
    ],
)
forbid_any(
    "no_common_engine_hardcoded_parameter_names",
    writer + "\n" + resolver,
    ["TREG", "TIPTRAN", "OFCTA", "idCanal", "descripcionCausal"],
)

# Frontend static UX/security guards.
feature_root = ROOT / "web/ach-interbank-ui/src/app/features/integrations"
style_files = sorted([*feature_root.rglob("*.scss"), *feature_root.rglob("*.css")])
color_pattern = re.compile(r"#[0-9a-f]{3,8}\b|\b(?:rgb|rgba|hsl|hsla)\s*\(", re.IGNORECASE)
color_hits: list[str] = []
for path in style_files:
    for number, line in enumerate(path.read_text(encoding="utf-8").splitlines(), start=1):
        if color_pattern.search(line):
            color_hits.append(f"{path.relative_to(ROOT).as_posix()}:{number}:{line.strip()}")
if color_hits:
    fail_check("no_brand_hex", color_hits)
else:
    pass_check("no_brand_hex", f"{len(style_files)} style files scanned")

click_pattern = re.compile(r"<(?:div|li)\b[^>]*(?:\(click\)|onclick\s*=)", re.IGNORECASE)
click_hits: list[str] = []
for path in feature_root.rglob("*.html"):
    for number, line in enumerate(path.read_text(encoding="utf-8").splitlines(), start=1):
        if click_pattern.search(line):
            click_hits.append(f"{path.relative_to(ROOT).as_posix()}:{number}:{line.strip()}")
if click_hits:
    fail_check("no_clickable_div_li", click_hits)
else:
    pass_check("no_clickable_div_li", "No clickable div/li found in integration templates")

for expected in [
    "path: ''",
    "path: 'contrato'",
    "path: 'relacion-informacion'",
    "path: 'relacion-informacion/:methodCode/:mappingSetId'",
    "path: 'conexion'",
]:
    if expected not in routes:
        fail_check("spa_routes", {"missing": expected})
        break
else:
    forbidden_routes = ["path: 'mappings'", "path: 'soap-settings'", "redirectTo"]
    hits = [value for value in forbidden_routes if value in routes]
    if hits:
        fail_check("spa_routes", {"forbidden": hits})
    else:
        pass_check("spa_routes", "Current Integraciones CFA routes preserved without obsolete redirects")

candidate = ROOT / "web/ach-interbank-ui/src/app/core/services/soap-integration-settings.service.ts"
retired_refs: list[str] = []
for path in (ROOT / "web/ach-interbank-ui/src/app").rglob("*.ts"):
    text = path.read_text(encoding="utf-8")
    if "SoapIntegrationSettingsService" in text or "soap-integration-settings.service" in text:
        retired_refs.append(path.relative_to(ROOT).as_posix())
if candidate.exists() or retired_refs:
    fail_check("retired_service_absent", {"exists": candidate.exists(), "references": sorted(retired_refs)})
else:
    pass_check("retired_service_absent", "SoapIntegrationSettingsService absent with no TS callers/imports")

# These two messages are rendered as primary editor errors, so technical nomenclature is forbidden there.
technical_primary_hits = []
for needle in [
    "No existe o no se pudo cargar el mapping set solicitado.",
    "El mapping set cargado pertenece a",
]:
    if needle in mapping_editor:
        technical_primary_hits.append(needle)
if technical_primary_hits:
    fail_check("technical_language_primary", technical_primary_hits)
else:
    visible_technical_hits: list[str] = []
    visible_pattern = re.compile(r"(?:>\s*(?:Live|DryRun|Source|Mapping set)\s*<|mapping set)", re.IGNORECASE)
    for path in feature_root.rglob("*.html"):
        for number, line in enumerate(path.read_text(encoding="utf-8").splitlines(), start=1):
            if visible_pattern.search(line):
                visible_technical_hits.append(f"{path.relative_to(ROOT).as_posix()}:{number}:{line.strip()}")
    if visible_technical_hits:
        fail_check("technical_language_primary", visible_technical_hits)
    else:
        pass_check("technical_language_primary", "Primary integration UI text uses functional language; raw technical labels remain secondary/data values only")

# Ensure low-level operating mode codes are not displayed as labels.
connection_html = read("web/ach-interbank-ui/src/app/features/integrations/pages/soap-integration-settings.component.html")
if ">Live<" in connection_html or ">DryRun<" in connection_html:
    fail_check("technical_operating_mode_labels", "Raw Live/DryRun label found")
else:
    pass_check("technical_operating_mode_labels", "Live/DryRun remain values while labels are functional Spanish")

writer_tests = read("tests/Cfa.ACHInterbank.Tests/IntegrationContractDrivenSoapBodyWriterTests.cs")
wsdl_tests = read("tests/Cfa.ACHInterbank.Tests/IntegrationContractArtifactImportTests.cs")
source_tests = read("tests/Cfa.ACHInterbank.Tests/IntegrationGenericEngineSourceResolutionTests.cs")
mapping_tests = read("tests/Cfa.ACHInterbank.Tests/IntegrationMappingEndToEndTests.cs")
activation_tests = read("tests/Cfa.ACHInterbank.Tests/IntegrationContractActivationRollbackTests.cs")
responsive_unit = read("web/ach-interbank-ui/src/app/features/integrations/integrations-responsive-a11y.spec.ts")
branding_unit = read("web/ach-interbank-ui/src/app/features/integrations/integrations-branding-tokens.spec.ts")
responsive_e2e = read("web/ach-interbank-ui/e2e/cfa-integrations-responsive-a11y.spec.ts")

require_all(
    "backend_gauntlet_regression_coverage",
    writer_tests + "\n" + wsdl_tests + "\n" + source_tests + "\n" + mapping_tests + "\n" + activation_tests,
    [
        "Write_ShouldFailWhenResolvedRequestContainsParameterOutsideContract",
        "WSCFAACH.Proc_Transacciones",
        "WSCFAACH.Proc_Contrapartidas",
        "WSAXON.RegistrarRespuestaTransaccion",
        "Write_ShouldRejectMaliciousResolvedElementNameBeforeXmlCreation",
        "Analyze_ShouldRejectDtdAndExternalEntity",
        "Analyze_ShouldRejectRemoteReferenceWithoutResolvingIt",
        "Analyze_ShouldRejectUnsupportedRestrictionFacetFailClosed",
        "Write_ShouldFailWhenRequiredInputIsMissing",
        "Type_ShouldRejectNonConvertibleResolvedValue",
        "MappingSet_Validation_ShouldRejectSourceCatalogFieldFromAnotherMethod",
        "MappingUpdate_ParameterFromAnotherContractVersion_ShouldFailClosed",
        "Activation_RevisionConflict_ShouldFailBeforeStateTransition",
        "Activation_IncompleteMapping_ShouldFailWithoutPartialState",
        "Rollback_WhenHistoricalMappingBecameIncompatible_ShouldFailWithoutPartialState",
        "Write_ShouldRespectContractFieldOrderForAllContractDrivenServices",
        "Write_ShouldRejectOutputParameterWhenInjectedIntoResolvedRequest",
    ],
)
require_all(
    "frontend_gauntlet_regression_coverage",
    responsive_unit + "\n" + branding_unit + "\n" + responsive_e2e,
    [
        "360x800",
        "390x844",
        "768x1024",
        "keyboard-only smoke",
        "focus trap",
        "restauración de foco",
        "branding",
        "contraste automatizado",
        "axe preparado",
        "no conserva colores hex/rgb/hsl hardcodeados",
    ],
)

coverage_files = {
    "writer": "tests/Cfa.ACHInterbank.Tests/IntegrationContractDrivenSoapBodyWriterTests.cs",
    "wsdl": "tests/Cfa.ACHInterbank.Tests/IntegrationContractArtifactImportTests.cs",
    "source_type": "tests/Cfa.ACHInterbank.Tests/IntegrationGenericEngineSourceResolutionTests.cs",
    "activation_rollback": "tests/Cfa.ACHInterbank.Tests/IntegrationContractActivationRollbackTests.cs",
    "responsive_unit": "web/ach-interbank-ui/src/app/features/integrations/integrations-responsive-a11y.spec.ts",
    "branding_unit": "web/ach-interbank-ui/src/app/features/integrations/integrations-branding-tokens.spec.ts",
    "responsive_e2e": "web/ach-interbank-ui/e2e/cfa-integrations-responsive-a11y.spec.ts",
}
missing_coverage = [path for path in coverage_files.values() if not (ROOT / path).is_file()]
if missing_coverage:
    fail_check("gauntlet_coverage_files", missing_coverage)
else:
    pass_check("gauntlet_coverage_files", coverage_files)

report = {
    "status": "FAIL_STATIC_GAUNTLET" if failures else "PASS_STATIC_GAUNTLET",
    "mode": "PACKAGE_SNAPSHOT",
    "environment": {
        "dotnet": shutil.which("dotnet"),
        "node": shutil.which("node"),
        "npm": shutil.which("npm"),
        "node_modules_present": (ROOT / "web/ach-interbank-ui/node_modules").is_dir(),
    },
    "checks": checks,
    "failures": failures,
}
ARTIFACT.write_text(json.dumps(report, indent=2, ensure_ascii=False) + "\n", encoding="utf-8")
print(json.dumps(report, indent=2, ensure_ascii=False))
raise SystemExit(1 if failures else 0)

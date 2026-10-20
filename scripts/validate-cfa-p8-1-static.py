#!/usr/bin/env python3
from __future__ import annotations

import json
import re
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]


def read(path: str) -> str:
    return (ROOT / path).read_text(encoding="utf-8")


def require(text: str, needle: str, label: str) -> None:
    if needle not in text:
        raise SystemExit(f"FAIL {label}: missing {needle!r}")


def forbid(text: str, needle: str, label: str) -> None:
    if needle in text:
        raise SystemExit(f"FAIL {label}: found forbidden {needle!r}")


def external_ts_references(candidate_path: str, symbols: list[str], *, tests: bool) -> list[str]:
    app_root = ROOT / "web/ach-interbank-ui/src/app"
    candidate = (ROOT / candidate_path).resolve()
    hits: list[str] = []
    patterns = [(symbol, re.compile(rf"\b{re.escape(symbol)}\b")) for symbol in symbols]
    for path in app_root.rglob("*.ts"):
        is_test = path.name.endswith(".spec.ts")
        if is_test != tests or path.resolve() == candidate:
            continue
        content = path.read_text(encoding="utf-8")
        for symbol, pattern in patterns:
            if pattern.search(content):
                hits.append(f"{path.relative_to(ROOT).as_posix()}::{symbol}")
    return sorted(hits)


def symbol_files(symbol: str, roots: list[str], tests: bool) -> list[str]:
    pattern = re.compile(rf"\b{re.escape(symbol)}\b")
    found: list[str] = []
    for root_name in roots:
        root = ROOT / root_name
        for suffix in ("*.cs", "*.ts"):
            for path in root.rglob(suffix):
                rel = path.relative_to(ROOT).as_posix()
                is_test = rel.startswith("tests/") or rel.endswith(".spec.ts")
                if is_test != tests:
                    continue
                if pattern.search(path.read_text(encoding="utf-8", errors="ignore")):
                    found.append(rel)
    return sorted(set(found))


proc_trans = read("src/Cfa.ACHInterbank.Persistence/ACH/Services/Implementation/ProcTransaccionesRequestMapper.cs")
proc_contra = read("src/Cfa.ACHInterbank.Persistence/ACH/Services/Implementation/ProcContrapartidasRequestMapper.cs")
registrar_gateway = read("src/Cfa.ACHInterbank.External/Connections/RespuestaTransaccionesAch/RespuestaTransaccionesAchGateway.cs")
persistence_di = read("src/Cfa.ACHInterbank.Persistence/DependencyInjectionService.cs")
contract_resolver = read("src/Cfa.ACHInterbank.Persistence/Integrations/Services/IntegrationExecutionContractResolver.cs")
request_resolver = read("src/Cfa.ACHInterbank.Application/Integrations/Services/IntegrationRequestResolver.cs")
writer = read("src/Cfa.ACHInterbank.Application/Integrations/Services/ContractDrivenSoapBodyWriter.cs")
routes = read("web/ach-interbank-ui/src/app/features/integrations/integrations.routes.ts")
connection_page = read("web/ach-interbank-ui/src/app/features/integrations/pages/soap-integration-settings.component.ts")

for label, text in (("Proc_Transacciones", proc_trans), ("Proc_Contrapartidas", proc_contra)):
    require(text, "UseContractDrivenRequest", label)
    require(text, "ResolveContractDrivenAsync", label)
    require(text, "ResolveLegacyAsync", label)
    require(text, "IIntegrationRequestResolver", label)
    require(text, "ISoapRequestBodyWriter", label)

require(registrar_gateway, "UseContractDrivenRequest", "RegistrarRespuestaTransaccion")
require(registrar_gateway, "BuildContractDrivenOperationAsync", "RegistrarRespuestaTransaccion")
require(registrar_gateway, "_requestMapper.Map(command)", "RegistrarRespuestaTransaccion legacy rollback")
require(registrar_gateway, "_soapRequestBodyWriter.Write", "RegistrarRespuestaTransaccion contract writer")

for needle in (
    "IIntegrationSourceProviderRegistry",
    "IntegrationSourceProviderRegistry",
    "IIntegrationTransformationRegistry",
    "IntegrationTransformationRegistry",
    "IIntegrationRequestResolver, IntegrationRequestResolver",
    "ISoapRequestBodyWriter, ContractDrivenSoapBodyWriter",
):
    require(persistence_di, needle, "contract engine DI")

for needle in (
    "_context.IntegrationContractVersions",
    "IntegrationContractVersionStatusEnum.Active",
    "x.ContractVersionId == contract.Id",
    "contract.OperationName",
    "contract.TargetNamespace",
    "contract.SoapAction",
):
    require(contract_resolver, needle, "ContractVersion authority")

for needle in (
    "_contractResolver.ResolveActiveAsync(context.MethodCode",
    "contract.Parameters",
):
    require(request_resolver, needle, "ContractVersion request resolution")

for needle in (
    "RESOLVED_REQUEST_PARAMETER_NOT_IN_CONTRACT",
    "RESOLVED_REQUEST_CONTRACT_HEADER_MISMATCH",
    "OrderBy(parameter => parameter.SortOrder)",
):
    require(writer, needle, "ContractDrivenSoapBodyWriter")

for path in ("path: ''", "path: 'contrato'", "path: 'relacion-informacion'", "path: 'relacion-informacion/:methodCode/:mappingSetId'", "path: 'conexion'"):
    require(routes, path, "SPA routes")
for obsolete in ("path: 'mappings'", "path: 'soap-settings'", "redirectTo"):
    forbid(routes, obsolete, "SPA routes")

candidate_path = "web/ach-interbank-ui/src/app/core/services/soap-integration-settings.service.ts"
candidate_symbols = [
    "SoapIntegrationSettingsService",
    "SoapInputParameterMapping",
    "SoapEndpointMethodMapping",
    "SoapIntegrationSettings",
]
candidate_file = ROOT / candidate_path
if candidate_file.exists():
    raise SystemExit(f"FAIL retired REMOVE_CANDIDATE still exists: {candidate_path}")
remove_candidate_refs = external_ts_references(candidate_path, candidate_symbols, tests=False)
remove_candidate_test_refs = external_ts_references(candidate_path, candidate_symbols, tests=True)
if remove_candidate_refs:
    raise SystemExit("FAIL retired REMOVE_CANDIDATE has production refs: " + ", ".join(remove_candidate_refs))
if remove_candidate_test_refs:
    raise SystemExit("FAIL retired REMOVE_CANDIDATE has test refs: " + ", ".join(remove_candidate_test_refs))
import_path_refs = []
for path in (ROOT / "web/ach-interbank-ui/src/app").rglob("*.ts"):
    content = path.read_text(encoding="utf-8")
    if "soap-integration-settings.service" in content:
        import_path_refs.append(path.relative_to(ROOT).as_posix())
if import_path_refs:
    raise SystemExit("FAIL retired REMOVE_CANDIDATE import/path refs remain: " + ", ".join(sorted(import_path_refs)))
require(connection_page, "IntegrationConnectionApi", "replacement connection API")
forbid(connection_page, "SoapIntegrationSettingsService", "replacement connection API")

report = {
    "status": "PASS_STATIC_ONLY",
    "contract_driven_paths": [
        "WSCFAACH.Proc_Transacciones",
        "WSCFAACH.Proc_Contrapartidas",
        "WSAXON.RegistrarRespuestaTransaccion",
    ],
    "remove_candidate": candidate_path,
    "remove_candidate_retired": not candidate_file.exists(),
    "remove_candidate_external_production_refs": remove_candidate_refs,
    "remove_candidate_external_test_refs": remove_candidate_test_refs,
    "remove_candidate_import_path_refs": sorted(import_path_refs),
    "candidate_reference_evidence": {
        symbol: {
            "production_files": symbol_files(symbol, ["src", "web/ach-interbank-ui/src/app"], tests=False),
            "test_files": symbol_files(symbol, ["tests", "web/ach-interbank-ui/src/app"], tests=True),
        }
        for symbol in (
            "IProcTransaccionesRequestMapper",
            "IProcContrapartidasRequestMapper",
            "RegistrarRespuestaAchSoapRequestMapper",
            "SoapParameterMapper",
            "IntegrationCatalogBootstrapper",
            "IntegrationMappingBootstrapper",
            "IntegrationMappingScenarioSeeder",
            "IntegrationMappingAdminService",
            "ProcTransaccionesRequestParityPolicy",
            "ProcContrapartidasRequestParityPolicy",
            "RegistrarRespuestaRequestParityPolicy",
            "RegistrarRespuestaSoapOperationAdapter",
            "ContractDrivenSoapBodyWriter",
            "IntegrationSourceProviderRegistry",
            "IntegrationTransformationRegistry",
        )
    },
}
output = ROOT / "artifacts/p8.1-static-validation.json"
output.write_text(json.dumps(report, indent=2, ensure_ascii=False) + "\n", encoding="utf-8")
print(json.dumps(report, indent=2, ensure_ascii=False))

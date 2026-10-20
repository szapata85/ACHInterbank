#!/usr/bin/env python3
from __future__ import annotations

import json
import re
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
SRC = ROOT / "src"
EXTERNAL_DI = SRC / "Cfa.ACHInterbank.External" / "DependencyInjectionService.cs"
RATCHET = ROOT / "tests" / "Cfa.ACHInterbank.Tests" / "RefactorArchitectureRatchetTests.cs"
APPSETTINGS = ROOT / "src" / "Cfa.ACHInterbank.Api" / "appsettings.json"

DIRECT_HTTP = re.compile(r"new\s+HttpClient\b")
RAW_HTTP_INJECTION = re.compile(r"\[FromServices\]\s+HttpClient\b")
BANNED_LOG_FRAGMENTS = (
    "Curl de la petición",
    "datos enviados al autorizador",
    "Respuesta del autorizador",
    "Este es la respuesta del servicio",
    "JsonConvert.SerializeObject(healthCheck)",
    "JsonConvert.SerializeObject(localServers)",
    "Link: {Link}",
    "Error en la creación del token con los datos",
)
REQUIRED_CLIENT_NAMES = (
    "ExternalHttpClientNames.HealthProbe",
    "ExternalHttpClientNames.GenericNoRetry",
    "ExternalHttpClientNames.WscfaachSoapNoRetry",
    "ExternalHttpClientNames.WsAxonSoapNoRetry",
)


def cs_files() -> list[Path]:
    return sorted(SRC.rglob("*.cs"))


def rel(path: Path) -> str:
    return path.relative_to(ROOT).as_posix()


def main() -> int:
    files = cs_files()
    direct_http = [rel(path) for path in files if DIRECT_HTTP.search(path.read_text(encoding="utf-8", errors="ignore"))]
    raw_http_injection = [rel(path) for path in files if RAW_HTTP_INJECTION.search(path.read_text(encoding="utf-8", errors="ignore"))]

    sensitive_logs: list[str] = []
    for path in files:
        text = path.read_text(encoding="utf-8", errors="ignore")
        if any(fragment in text for fragment in BANNED_LOG_FRAGMENTS):
            sensitive_logs.append(rel(path))

    di_text = EXTERNAL_DI.read_text(encoding="utf-8")
    missing_clients = [name for name in REQUIRED_CLIENT_NAMES if name not in di_text]
    old_global_soap_policy = "Resilience:Soap" in di_text
    no_retry_helper = di_text.split("private static void AddNoRetryHttpClient", 1)[1].split("private static bool IsValidHttpPolicy", 1)[0]
    non_idempotent_pipeline_absent = "AddResilienceHandler" not in no_retry_helper and "AddRetry" not in no_retry_helper

    ratchet_text = RATCHET.read_text(encoding="utf-8")
    application_manual_http_allowlist_removed = (
        '"manual HttpClient construction"' in ratchet_text
        and '@"new\\s+HttpClient\\b"' in ratchet_text
        and 'Helpers/DNS/Implemetations/CheckHealthAsync.cs' not in ratchet_text
    )

    config = json.loads(APPSETTINGS.read_text(encoding="utf-8"))
    policies = config["Resilience"]["ExternalHttp"]
    health_retry = int(policies["HealthProbe"]["MaxRetryAttempts"])
    generic_retry = int(policies["Generic"]["MaxRetryAttempts"])
    soap_retry = int(policies["Soap"]["MaxRetryAttempts"])
    generic_timeout = int(policies["Generic"]["TimeoutSeconds"])
    policy_ok = health_retry > 0 and generic_retry == 0 and soap_retry == 0 and generic_timeout == 100

    checks = {
        "direct HttpClient constructions": len(direct_http) == 0,
        "raw default HttpClient action injection": len(raw_http_injection) == 0,
        "required managed client registrations": len(missing_clients) == 0,
        "legacy global SOAP resilience block removed": not old_global_soap_policy,
        "no resilience replay/rejection pipeline on non-idempotent clients": non_idempotent_pipeline_absent,
        "secure logging banned fragments": len(sensitive_logs) == 0,
        "Application manual-HttpClient allowlist removed": application_manual_http_allowlist_removed,
        "operation-specific retry configuration": policy_ok,
    }

    print("R3 EXTERNAL RESILIENCE STATIC RATCHET")
    print(f"  direct HttpClient constructions: {len(direct_http)}")
    if direct_http:
        print("   - " + "\n   - ".join(direct_http))
    print(f"  raw default HttpClient action injections: {len(raw_http_injection)}")
    if raw_http_injection:
        print("   - " + "\n   - ".join(raw_http_injection))
    print(f"  missing managed client registrations: {len(missing_clients)}")
    if missing_clients:
        print("   - " + "\n   - ".join(missing_clients))
    print(f"  banned sensitive-log fragments: {len(sensitive_logs)}")
    if sensitive_logs:
        print("   - " + "\n   - ".join(sensitive_logs))
    print(f"  retry policy: health={health_retry}, generic={generic_retry}, soap={soap_retry}; genericTimeout={generic_timeout}s")

    failures = [name for name, ok in checks.items() if not ok]
    if failures:
        print("R3 EXTERNAL RESILIENCE STATIC RATCHET: FAIL")
        for failure in failures:
            print(f"  FAIL: {failure}")
        return 1

    print("R3 EXTERNAL RESILIENCE STATIC RATCHET: OK")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())

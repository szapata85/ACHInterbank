#!/usr/bin/env python3
"""SEC-I06A: inspección offline de fuentes y trazabilidad de escenarios.
No compila ni ejecuta .NET; no se considera evidencia de que pasen las pruebas xUnit.
Uso: python scripts/security/verify_sec_i06a.py --root .
     python scripts/security/verify_sec_i06a.py --zip /ruta/ACHInterbank_...zip
"""
import argparse
import hashlib
import json
from pathlib import Path
import re
import sys
import zipfile

MATRIX = "docs/security/SEC-2026-10-BROTLI-HARDENING/SEC-I06A_MATRIZ_PRUEBAS.json"
MEMORY = "docs/security/SEC-2026-10-BROTLI-HARDENING/MEMORY_STATE.md"
SEED = "src/Cfa.ACHInterbank.Persistence/Configuration/UserConfiguration.cs"
NEW_TESTS = (
    "tests/Cfa.ACHInterbank.Tests/Security/SecI06AHeadersRegressionTests.cs",
    "tests/Cfa.ACHInterbank.Tests/Security/SecI06ACredentialBoundaryTests.cs",
)


def main():
    parser = argparse.ArgumentParser()
    group = parser.add_mutually_exclusive_group(required=True)
    group.add_argument("--root", type=Path)
    group.add_argument("--zip", dest="archive", type=Path)
    args = parser.parse_args()
    z = zipfile.ZipFile(args.archive) if args.archive else None

    def content(p):
        if z:
            return z.read("ACHInterbank_ajustado/" + p)
        return (args.root / p).read_bytes()

    results = []
    def check(name, condition):
        results.append((name, bool(condition)))

    try:
        matrix = json.loads(content(MATRIX).decode("utf-8-sig"))
        scenarios = matrix.get("scenarios", [])
        ids = [x["id"] for x in scenarios]
        check("schema y escenario(s)", matrix.get("increment") == "SEC-I06A" and len(scenarios) >= 20)
        check("IDs únicos", len(ids) == len(set(ids)))
        check("estado honesto no ejecutado", all(x["status"] in ("WRITTEN_NOT_EXECUTED", "NOT_RUN_THIS_INCREMENT") for x in scenarios))
        check("archivos y métodos referenciados existen", all(
            re.search(r"\b"+re.escape(s["test_method"])+r"\s*\(", content(s["test_file"]).decode("utf-8-sig"))
            for s in scenarios))
        check("pruebas nuevas de seguridad", all("[Fact]" in content(path).decode() or "[Theory]" in content(path).decode() for path in NEW_TESTS))
        check("seed preservado en bytes", hashlib.sha256(content(SEED)).hexdigest() == matrix["seed_source_sha256"])
        memory = content(MEMORY).decode("utf-8-sig")
        check("flags de continuidad", all(flag in memory for flag in [
            "PRESERVE_ADMIN_SEED_CREDENTIAL: true", "GO_INCREMENT: YES", "GO_DEPLOY: NO", "GO_LIVE_MONETARY: NO"])
        )
        check("Quartz sigue separado", "original_ach_cr_001_quartz: DISABLED_KEEP_DISABLED" in memory)
        check("matriz sin aprobaciones ficticias", all(x["status"] == "NOT_RUN_ENVIRONMENT" for x in matrix["external_execution_gates"]))
    except Exception as exc:
        print(f"FAIL preparación de validación: {type(exc).__name__}: {exc}")
        return 2
    finally:
        if z: z.close()

    for label,passed in results:
        print(f"{'PASS_STATIC' if passed else 'FAIL_STATIC'}: {label}")
    print(f"Resultado: {sum(p for _,p in results)}/{len(results)} verificaciones offline")
    return 0 if all(p for _,p in results) else 1

if __name__ == "__main__":
    sys.exit(main())

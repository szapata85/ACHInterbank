# ACH Colombia Phase 8 controlled fixtures

These are non-production NACHA-M payload fixtures copied from the repository's existing ACH Colombia golden test data.

- `ACHCOL_PHASE8_OUT.payload`: source `tests/Cfa.ACHInterbank.Tests/TestData/Nacha/GoldenFiles/ACHColombia/Outgoing/ACH_COL_OUT_001.ach`.
- `ACHCOL_PHASE8_RET.payload`: source `tests/Cfa.ACHInterbank.Tests/TestData/Nacha/GoldenFiles/ACHColombia/Returns/ACH_COL_RET_001.RET`.

They are **not** production files, credentials, certificates, or pre-built `.env` envelopes. The Phase 8 harness intentionally does not commit private certificate material. Runtime digital-envelope generation/decryption must use ephemeral, authorized test certificates supplied by the controlled environment; the deterministic backend tests already exercise the envelope/file-exchange path without external ACH Colombia infrastructure.

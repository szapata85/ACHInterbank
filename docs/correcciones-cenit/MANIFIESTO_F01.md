# MANIFIESTO_F01 — comparación y reversibilidad

**Base:** `ACHInterbank_PROYECTO_COMPLETO_AUDITORIA_CENIT_I10_2026-11-02.zip`. SHA-256 `9c298cf0ae8f8215b4ab00a186c14b1bcc18be78c139b3d8584edec5fa54aeef`. **Archivos base:** 4602. **Entrega:** `ACHInterbank_PROYECTO_COMPLETO_CORRECCIONES_CENIT_F01_2026-11-02.zip`. CRC del ZIP base: OK. Comprobación de la entrega y hash SHA-256 externo: ver informe F01 y `.sha256` adjunto.

**Plan de diferencias:** 6 archivos base modificados deliberadamente (5 productivos + 1 prueba xUnit); archivo nuevo `scripts/test/cenit_f01_static_check.py`; documentos nuevos bajo `docs/correcciones-cenit/`. **Eliminaciones:** ninguna. **Reversión:** restaurar ZIP original o aplicar `F01_CAMBIOS.patch` en reversa y retirar nuevos. **Sin migraciones, tablas, WSDL, CUD ni modificación de fuentes ACH Colombia.**

| Archivo modificado | SHA-256 antes | SHA-256 después |
|---|---|---|
| `src/Cfa.ACHInterbank.Persistence/ACH/Services/Implementation/CenitCycleExecutionService.cs` | `319ab96ea56365b88a87b56518e0c3da714fc686195ea8070c83e36990385537` | `9c4275baf107fd8d80efed613ed755d975a86eae229e2381803f2f2d03ec9dde` |
| `src/Cfa.ACHInterbank.Persistence/ACH/Services/Implementation/CenitNettingService.cs` | `2b308a8cf017db4f6e4c278b91016f0421ccd2011686b51e80e180e18b938598` | `ad384fa00071007433b84a4b704e51ef052ce12e4eb43e612a1f8d3a6fbb2cb0` |
| `src/Cfa.ACHInterbank.Persistence/ACH/Services/Implementation/LiquidityOptimizationService.cs` | `ae190c63f453d0d23b8cf6c382654c6b60f74713d5f91212493b70c908a346ed` | `e7906122b7e29cf2788fc6ae7cd27f72a48c19aaeda723a4b56f545651689282` |
| `src/Cfa.ACHInterbank.Persistence/ACH/Services/Implementation/TransactionPriorityPolicy.cs` | `c389c502f50475a1066a33d949a4837e8be16d5a279356e80886d2a043880b93` | `af8763aea31768d31ced7c514999be1b80c855d358270652095d32f8cd83bf81` |
| `src/Cfa.ACHInterbank.Persistence/ACH/Services/Implementation/AchPendingCycleEligibilityService.cs` | `9c50c6de22b6ec9e5eae3b29acff7a43ef8a35f30937c1040441e9812ad94fcb` | `a6dffb0869928a06496b9e34e9d008c9df524561c32f0fb9108a591bceeeadce` |
| `tests/Cfa.ACHInterbank.Tests/CenitOperationalGovernanceTests.cs` | `c6db29b49d76b8fb20da030fce168c9f5c9ec646c8b55b4cf736eba83e78d016` | `a468c0a8c359fec49aa3113ce87621a27211172085afa34b9ef83768846ff0cd` |

**Diferencia reproducible:** `F01_CAMBIOS.patch` (unified diff de las seis rutas preexistentes). Las rutas inalteradas se verifican al empaquetar, byte a byte y mediante SHA-256. Los archivos nuevos están excluidos de los hashes de base por definición. El propio manifiesto no contiene su hash por autorreferencia. El ZIP resultante se verifica con `testzip` y su hash se registra en `.sha256` fuera del ZIP.

**Validación adicional ejecutada:** `patch --dry-run -p1` aplicado sobre los seis archivos originales extraídos del ZIP I10: PASS (seis archivos reconocidos, cero conflictos). Este ensayo confirma aplicabilidad del diff; la reversión se efectúa desde el proyecto F01 con `patch -R -p1` y eliminación de los archivos creados.

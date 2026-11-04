# MANIFIESTO F02 — Integridad y reversibilidad

Fecha 2026-11-02. Base: `ACHInterbank_PROYECTO_COMPLETO_CORRECCIONES_CENIT_F01_2026-11-02.zip` (SHA-256 `8beee25554db99142677d6c084cb08cb9cf864967e864cbc61dd6c72459084e1`). Raíz de ambos ZIP: `ACHInterbank_ajustado/`.

**Originales del ZIP F01:** 4614 entradas, 4614 archivos. **Modificados controlados:** 8 archivos (1 productivo, 1 de pruebas, 6 de documentación). **Nuevos F02:** 6 archivos. **Eliminados:** 0. **Originales intactos:** 4606. Ausencia de cambios accidentales en ACH Colombia: SHA-256 iguales en todo archivo no incluido en la lista de modificaciones. No hay migraciones/WSDL/CUD nuevos.

`F02_COMPARACION_SHA256.tsv` incluye SHA-256 anterior/actual y estado de cada archivo ya presente en F01 (sin autorreferencia al propio manifiesto/comparación). El SHA-256 del ZIP terminado se entrega en archivo externo `.zip.sha256`; no puede almacenarse idéntico en el ZIP cuyo contenido representa.

## Archivos originales modificados

| Ruta | SHA-256 F01 | SHA-256 F02 |
|---|---|---|
| `src/Cfa.ACHInterbank.Persistence/ACH/Services/Implementation/CenitChamberResponseService.cs` | `c77621429e2c21d7a9922d83fe6e59d699583cb8802333ed698220deb61e5740` | `bab5b5732d12a29d720ef3e7b0f5348baf2acabcb38106283f1973c2319e8c91` |
| `tests/Cfa.ACHInterbank.Tests/CenitChamberResponseLifecycleTests.cs` | `500a464f1667b558b2f241f90f33e8d00a7a88347ff82160c3d33f038877a773` | `ce2b61bde6cf73708630cf16116c1204cc5457803ae6d6647524f1b43503e8bd` |
| `docs/correcciones-cenit/MEMORY_STATE.md` | `2ea58c03013d9b3ab0005fd1a428ffb01780e510b5e2a16de4ed22e0801597cd` | `6e8fab8a8102b0a64b78490eab68bcabde6b74aa0a3a7dad353e373bce74de1c` |
| `docs/correcciones-cenit/PLAN_INCREMENTAL_CORRECCIONES.md` | `cba1ebacec6e9cfdef3564115a620f38c6b8a9aad09d7ddb7b50066d7064de42` | `ef6bd6eb6786efbf9622143b3566395243d96869b896dd6089b8dd39a6f01990` |
| `docs/correcciones-cenit/MATRIZ_HALLAZGOS_CORRECCIONES.md` | `93a425ea6eda14f40a85cecfa8cb2ac8fc35c56b2b4abab504a86e0895a6a5d2` | `b70a83d6a49e2b6a47011cc23894765e70f16863f7b7a4eca54c4d564d991738` |
| `docs/correcciones-cenit/REGISTRO_PRUEBAS.md` | `02afcaa27bfedf327ace4a959958216c5c1eeecc248b6b9b175a4e6c7be3f950` | `a9e75645ae0b3a762ef92eeb44642a086f57f0aa9b9d135ee95b2974e978f206` |
| `docs/correcciones-cenit/REGISTRO_CORRECCIONES.md` | `96abb6afd40015252bab8e6e2d24b014262be29c71f9e0ae197e9c10a5586b60` | `55fa0187aa0abaa4a9511ce8f087a8168445d5d1236b39ca05f7e2569934146c` |
| `docs/correcciones-cenit/ESTADO_FASES.md` | `084cfb9fd4be1b7d744dae89a9270a9629fbfc57588c4c27cd13240887eaae79` | `69cd1d7153048a0cb18c094a39dcf1065fc0890c81c958a90473135778f894f6` |

## Nuevos archivos F02

- `docs/correcciones-cenit/F02_CAMBIOS.patch`
- `docs/correcciones-cenit/F02_COMPARACION_SHA256.tsv`
- `docs/correcciones-cenit/F02_INFORME_CORRECCIONES.md`
- `docs/correcciones-cenit/F02_MEMORY_STATE.md`
- `docs/correcciones-cenit/MANIFIESTO_F02.md`
- `scripts/test/cenit_f02_static_check.py`

## Controles/recuperación

- CRC ZIP y comparación binaria por entrada original **se ejecutan tras comprimir**; ver `F02_VERIFICACION_INTEGRIDAD.txt` externo.
- Parche reversible únicamente para producción/pruebas: `F02_CAMBIOS.patch`, validado con `patch --dry-run -p1` contra ambos archivos originales extraídos de F01.
- Restituir el ZIP F01 para volver completamente al incremento anterior (incluyendo documentos); no hay eliminación de directorios/archivos.
- F02 con pruebas C# fuente pero sin `dotnet test` por falta de SDK; no declarar cierre de HC ni homologación.

# APN-I12 — Comparación con base APN-I11

**Base utilizada:** `ACHInterbank_COMPLETO_CORRECCION_PRENOTIFICACIONES_APN_I11_2026-11-03.zip`.

| Indicador | Resultado |
|---|---:|
| Archivos I11 originales | 5033 |
| Archivos I11 conservados sin alterar contenido | 5032 |
| Archivos I11 modificados | 1 |
| Archivos I11 eliminados | 0 |
| Archivos nuevos I12 (incluyendo este informe) | 20 |
| Archivos totales I12 | 5053 |

**Archivo productivo modificado:** `src/Cfa.ACHInterbank.Persistence/ACH/Quartz/Jobs/Implementation/AchTacitAcceptanceJobHandler.cs`.

**Clase productiva nueva:** `PrenotificationCandidateScan.cs`. No se modificó schema/migraciones, las políticas de cámaras ni los estados de prenotificación.

El manifiesto `APN_I12_FILE_DIFF_SHA256.csv` contiene hashes SHA-256 de archivos cambiados/nuevos (excluye auto-referencias de este informe y el mismo manifiesto). La verificación de CRC del ZIP y los hashes finales de los paquetes se entregan además fuera del ZIP como `APN_I12_INTEGRIDAD.md`.

**Resultado C# y UAT:** NO EJECUTADOS (la integridad del ZIP no equivale a compilación/homologación).

# Incremento I01 — baseline y primeros hallazgos

**Fecha de corte:** 2026-11-01. **Naturaleza:** inspección estática del ZIP, sin tocar código de producción.

## Integridad del insumo

- ZIP inicial: `ACHInterbank_PROYECTO_COMPLETO_SOAP_MAP_SAFE_001_I07A_INTEGRADO_2026-11-01.zip`.
- SHA-256 ZIP inicial: `aecbdfcfd7ecb75b19300eaf25cb956d215d10e6e0182f643766bb4681458ace`.
- ZIP de entrada validado con `ZipFile.testzip()` sin errores. 5.116 entradas; ~109,9 MB descomprimidos.
- Una muestra crítica (`RegulatoryCycleScheduleCatalog.cs`) se cotejó byte a byte contra el contenido del ZIP: idéntica a la copia de lectura.
- ACH Manual V35 (`ACH-Colombia-V35.md`), SHA-256: `dcac71b23bb047bfa280c4351c62415574961ca5c254d478e3b0ad8b86e59083`.
- Anexo de horarios (`Nuevos Ciclos de Proceso y Horarios.pdf`), SHA-256: `3f60bb2b596786c278055cd84fcb06712b81d93486f52bfae675556860a44f25`.
- `dotnet` no aparece disponible en el entorno de revisión; pruebas .NET, DB, SFTP y cámara no ejecutadas. **No bloquea** la siguiente revisión estática.

## Decisiones de baseline

- Cámara `ACHCOL`, siete ciclos para fechas desde `2026-10-14` **según configuración y decisión interna existente**; conservar cinco ciclos históricos. Confirmación externa de vigencia: **no documentada aquí**.
- El Manual ACH Colombia V35 (abril 2026) aún consigna cinco ciclos; el documento complementario presenta siete. El PDF tiene errata de prosa `cinco (7)` y el ciclo 4 `11:01pm`; la documentación interna usa 11:01 a. m. Se deja `CONFLICTO_FUENTE`, sin sustituir silenciosamente la fuente.
- Para ciclo 1 de siete ciclos: recepción 19:01 del día previo a 07:00, planilla 07:00–07:30, pago 07:30–07:45, liberación 07:45–08:00. Devoluciones desde ciclos 4,5,6,7 del día hábil anterior (documento complementario, página 1).

## Reglas detectadas (primer rastreo, **NO** auditoría concluida del ciclo 1)

| ID | Regla y fuente | Implementación localizada | Resultado estático |
|---|---|---|---|
| AC-BASE-01 | ACHCOL siete ciclos a partir 14-10-2026, decisión interna | `src/Cfa.ACHInterbank.Application/ACH/Models/RegulatoryCycleScheduleCatalog.cs` (`AchColombiaSevenCycleConfirmedPolicy`) y seeder `.../Seeders/ClearingHouseCycleConfigSeeder.cs` | `PARCIAL`: datos presentes; no DB desplegada comprobada |
| AC-C1-01 | C1 recepción 19:01–07:00 | `RegulatoryCycleScheduleCatalog.cs` (`Candidate(1, ...)` y `RequireActivatable`); prueba `tests/Cfa.ACHInterbank.Tests/RegulatoryCycleScheduleCatalogTests.cs` | `PARCIAL`: catálogo y test fuente; falta atravesar planificador real y bordes del día hábil |
| AC-C1-02 | C1 planilla, pago y liberación en ese orden | `RegulatoryCycleScheduleCatalog.cs` etapas de candidato; prueba de horarios en `RegulatoryCycleScheduleCatalogTests.cs` | `PARCIAL`: secuencia declarada, sin confirmar barreras financieras en flujo ejecutor |
| AC-C1-03 | C1 admite crédito, débito monetario y prenotificaciones | `RegulatoryCycleScheduleCatalog.cs` defaults de flags; `CycleTransactionPolicy.cs` lee `AllowsMonetaryCredit/Debit` y `AllowsCredit/DebitPrenotification` | `PARCIAL`: faltan callers, persistencia y bordes de horario |
| AC-C1-04 | C1 devoluciones solo del día hábil anterior, ciclos 4–7 | `RegulatoryCycleScheduleCatalog.cs` (`ReturnWindow(1, Previous(4)..Previous(7))`); prueba de matriz en `RegulatoryCycleScheduleCatalogTests.cs` | `NO_DEMOSTRADA` en ruta productiva: `ReturnWindows` se conserva en `RegulatoryCyclePolicyCandidate` pero **no** en `RegulatoryCyclePolicySnapshot`; `CycleTransactionPolicy.cs` solo evalúa `AllowsReturn` |
| AC-C1-05 | Evitar devolución de C1–C3 del día anterior y del propio C1 actual | `RegulatoryCycleReturnWindow.Allows`, prueba `BlocksFifthReturnSourceOutsideMovingWindow` para C1/C3 | `NO_DEMOSTRADA` en flujo productivo general; prueba de catálogo no prueba ejecución real |
| AC-C1-06 | Devolución de devolución (ROR) según norma ACHCOL | `RegulatoryCycleSchedule` trae `AllowsReturnOfReturn=true` por defecto; `CycleTransactionPolicy.cs` consume esa bandera | `CONFLICTO_FUENTE` / `NO_DEMOSTRADA`: primero localizar soporte normativo y flujo real antes de corregir |

## Hallazgos que pasan a I02

- **H01 (alta):** falta trazabilidad de la matriz de ventanas de devolución del catálogo al flujo general de devoluciones; **no equivale aún a defecto confirmado**. Búsqueda inicial de `ReturnWindows` bajo `src` solo encontró su declaración y carga en el catálogo.
- **H02 (media documental):** desalineación del Manual V35 / tabla siete ciclos y errata sobre inicio de C4; la documentación interna fijó una decisión, no acredita homologación externa.
- **H03 (alta por verificar):** horario y etapas de C1 constan en configuración, pero es necesario comprobar que la cadena planilla→pago→liberación sea efectivamente exigida en ejecución.
- **H04 (por verificar):** ROR ACHCOL habilitado por default, norma concreta aún sin verificar.

## Próximo paso exacto — I02

Recorrer C1 completo por `scheduler → resolver de fecha operativa → política vigente → ingreso/transacción → devolución → planilla → pago → liberación → respuesta/conciliación`, documentando cada regla con archivo y línea, sin introducir cambios de código hasta confirmar hallazgo. La ausencia de SDK o de conexiones bancarias **no detiene** el recorrido estático.

# F6.3 — Informe de verificación final *offline* y expediente de certificación

**Fecha:** 2026-10-25. **Programa:** `ACHCOL-NACHA-CFA-EXTCYCLE-001`. **Fuente:** ZIP consolidado F6.2, SHA-256 `85f79f594496404ea92f3d76558e875bf72149742892a7890d7cb76c4ba90b95`.

## Conclusión

**NO CERTIFICADO / NO GO para `Live`.** F6.3 reúne evidencia de revisión estática ejecutada y deja una ruta concreta para la validación ejecutable futura. Esto **no equivale** a haber compilado el software ni haber probado transferencias en un ambiente CFA. El alcance físico considera archivos NACHA-M `.OUT` y sobres `.OUT.env` y casos `H/008/.8`, `I/009/.9`, `H/008/.10` (consecutivo distinto de ciclo externo), sin exigir `AchCycleId`; los criterios funcionales siguen pendientes de ejecución real.

## Evidencias ejecutadas sobre el código F6.2

| Gate / comando desde raíz de `ACHInterbank_ajustado/` | Resultado | Alcance real |
| --- | --- | --- |
| `python3 scripts/validate-solution-projects.py` | **PASS: 7/7** | Proyectos y referencias, NO compilación |
| `python3 scripts/validate-achcol-f61-offline.py` | **PASS: 12/12** | Invariantes estáticas NACHA/cola/DryRun |
| `python3 scripts/validate-achcol-f62-offline.py` | **PASS: 10/10**, con ajuste F6.3 reejecutado | KPI, origen, bloqueo, runbook, guard sin vínculo mutable de tarea |
| `python3 scripts/validate-r5d-inbound-postprocessing.py` | **PASS** | Ratchet de separación del postprocesamiento |
| `python3 scripts/validate-nav3-4j-digital-envelope.py` | **PASS: 145 comprobaciones** | Verificador estático de sobre digital, no descifrado real |
| `python3 scripts/validate-r5c-inbound-persistence.py` | **FAIL** | SHA histórica R5B/R5C cambió; determinar si cambio posterior está autorizado, NO ignorar |
| `python3 scripts/validate-r7-returns-separation.py` | **INCONCLUSO** | Ejecución excede ventana local; invoca verificadores previos (incluye R5C), no es PASS |

**No disponibles aquí:** SDK `dotnet` versión `10.0.402`, Docker, instancias aisladas PostgreSQL/SQL Server, dependencias CFA XSD oficiales, conectividad CFA y autorización Live. No se realizaron llamadas monetarias.

## Gate técnico pendiente — ejecución en CI autorizado

1. Reproducir **exactamente el contenido de este ZIP** en un checkout controlado; el Git HEAD que trae el archivo es un snapshot histórico y por sí solo no garantiza que las modificaciones F1–F6 estén publicadas en ninguna rama remota. Antes de ejecutar CI, confirmar el hash del árbol que efectivamente prueba.
2. Con SDK **.NET 10.0.402**, ejecutar los pasos existentes de `.github/workflows/dotnet-ci.yml`:

   ```bash
   dotnet --info
   python3 scripts/validate-solution-projects.py
   dotnet restore ACHInterbank.sln
   dotnet build ACHInterbank.sln -c Release --no-restore --maxcpucount:1
   dotnet test tests/Cfa.ACHInterbank.Tests/Cfa.ACHInterbank.Tests.csproj -c Release --no-build --filter 'Category!=ClearingHouseMultiDb&Category!=OutgoingMonitorMultiDb&Category!=OutboundReturnMultiDb&Category!=ConfigImmutabilityMultiDb&Category!=ConfigImmutabilityOutOfBand' --logger 'trx;LogFileName=dotnet-tests.trx' --results-directory TestResults -- RunConfiguration.MaxCpuCount=1
   ```

   Conservar logs, exit codes, archivos TRX y versión SDK; corregir cualquier defecto demostrable antes del GO. Ejecutar además suites multidb según los workflows existentes `dotnet-ci.yml` y `financial-integrity-multidb.yml`, con servicios aislados y configuración explícita.
3. Revisar **R5C FAIL** y R7: resolver conflicto de hashes frente a cambios autorizados; no rebajar el ratchet para lograr verde. R7 necesita ejecución completa y evidencia antes de declarar regresión aprobada.
4. Aplicar migraciones incluidas F2.3 y F3.1–F3.3 sobre copias aisladas en **ambos proveedores**; validar nulabilidad/constraints/FK/índice único, snapshots y reversiones protegidas. **No** hacer `Down` destructivo en bases que contengan filas `AchColFileBased`.
5. Ejecutar los tests NACHA-M escritos para `.OUT` y `.OUT.env`, correlación encabezado–nombre, ciclos 8/9/10 independientes de `AchCycleId`, duplicados, concurrencia y la regresión de `CycleBound`, CENIT y RET. Comprobar la ausencia de llamadas SOAP desde `DryRun` y preflight; controlar la exposición de datos sensibles en evidencias.
6. Obtener los XSD oficiales importados por `WSCFAACH.wsdl` (`xsd0`/`xsd1`) y homologar los parámetros CFA que permanecen sin fuente contractual aprobada, incluidos `IDTRAN`, `IDLOTE`, `IDCAMCOMPE` y códigos institucionales. La inspección de XML parcial no reemplaza la validación contra XSD ni la homologación campo a campo.
7. Ejecutar UAT autorizado con CFA y conciliación/rollback; revisar consultas históricas de solo lectura del runbook F6.2 para asegurarse de que `unexpectedNonBlocked=0` y `withAttempts=0` en etapa no autorizada, dentro y fuera de ventana. **No** encender `Live` antes de autorización documental explícita.

## Matriz de aceptación pendiente

| Grupo | Casos | Estado F6.3 |
| --- | --- | --- |
| H/I/`.8`/`.9`/`.10` sin ciclos internos | AT-01/02/03/05/06/16/17/18 | **PENDIENTE E2E** (tests escritos; no ejecutados) |
| Segregación ACHCOL vs CENIT/RET | AT-04/15 | **PENDIENTE REGRESIÓN** |
| Validación y seguridad, archivos claros/cifrados | AT-07/08/09/10 y AT-21–28 | **PENDIENTE PRUEBAS REALES** |
| Despacho, contrato y Live | AT-11/12/19/20 | **BLOQUEADO** por homologación/autorización; DryRun no es Live |
| Reintentos inciertos, idempotencia y concurrencia | AT-13/14 | **PENDIENTE pruebas multinstancia/reconciliación** |

## Decisiones de cierre del incremento

- **DEC-086:** F6.3 cierra el **expediente de auditoría disponible**, no la certificación de entrega productiva. No transformar `IMPLEMENTED_UNVERIFIED` en `VERIFIED` sin evidencia ejecutada.
- **DEC-087:** Los validadores históricos revisarán contratos permanentes, no `next_microtask`; corregir el auditor F6.2 para mantener la regresión válida en futuras sesiones.
- **DEC-088:** No crear infraestructura adicional por falta de ambiente; usar CI existente y runbook F6.2 para completar pruebas en instalaciones autorizadas.
- **DEC-089:** El GO de CFA es **independiente** de la integridad del ZIP o de los controles estáticos: requiere compilación, pruebas, migraciones, XSD/UAT y aprobación explícita. `CanTransmit=false` y filas `Blocked` permanecen.

## Inventario de cambios de F6.3

- `scripts/validate-achcol-f62-offline.py`: elimina comparación con campo mutable `next_microtask`, conserva guard permanente de programa/Live.
- `docs/ACHCOL_CFA_FILE_BASED_2026-10-24/F6_3_VERIFICATION_REPORT.md`: evidencia y criterios pendientes.
- `MEMORY_STATE.md`, `PLAN.md`, `CHATGPT_MICRO_START.md`, `README_PAQUETE_UNIFICADO.md`: estado `1.20.0`, decisiones y continuidad.
- **Sin cambios** en código C#, modelos EF, migraciones, cliente SOAP, configuración Live ni otros `MEMORY_STATE.md` del proyecto.

**Nota:** `next_microtask=null` señala cierre de las microimplementaciones planificadas. La obligación siguiente es `CERTIFICATION_GATE_PENDING`, que permanece abierta hasta obtener pruebas y autorizaciones reales. Sin ese gate, el sistema no se considera preparado para operaciones monetarias.

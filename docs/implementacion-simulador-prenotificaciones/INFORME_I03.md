# I03 — Generación de devoluciones NACHA-M de prenotificaciones originadas

**Fecha de corte:** 2026-11-02  
**Estado:** código incorporado / pendiente de verificación .NET y homologación UAT.  
**Base:** ZIP completo I02; sin reescribir módulos heredados.

## Cambios realmente incorporados

1. Nuevo endpoint `POST /api/uat/nacha-inbound-simulator/prenotifications/generate-return`, autenticado por `TestTools.Access` (controlador) y política `NachaSimulatorGenerateDifferential` (acción).
2. Método `GeneratePrenotificationReturnAsync` de `INachaInboundSimulationService` implementado en una clase parcial de persistencia; acepta el contrato I01 y exige `Outcome=Returned`. `NoReturnByDeadline` sigue pendiente de I05 y **nunca** produce archivo de aprobación.
3. Revalidación I02 dentro de transacción serializable antes de generar: prenotificación saliente, cámara, banco, ciclo, intento activo, transmisión y plazo, sin respuestas existentes ni devoluciones simuladas para esos IDs.
4. Reserva transaccional y durable de idempotencia por `SimulationId` determinista (SHA-256 de cámara + banco + clave), aprovechando el índice **UNIQUE existente**; huella de contenido SHA-256 en `Notes`, sin registrar datos de cuenta. Reintento exitoso retorna el mismo archivo, pero clave reutilizada con diferente contenido se rechaza. La garantía se fundamenta en la BD; no en un `lock` de proceso.
5. Generador oficial existente `BuildOfficialReturnFileAsync` / `INachaFileBuilder.BuildReturnOutAsync`: causal válida en catálogo, perfil RETORNO/ENTRADA homologado, nuevos traces reservados, correlación de traces originales, importe 0, adenda del retorno y formato por cámara. CENIT R60–R74 se excluye del retorno ordinario.
6. El archivo y la evidencia se escriben en carpeta aislada por `SimulationId`, con limpieza de archivo en error, commit de la operación tras completarse el artefacto. La operación queda marcada `GeneratedOnly=true`, `AutoImported=false`, `UploadRequired=true`, `ExternalTransmission=false`. **El estado de la prenotificación original no cambia.**
7. La ruta genérica `/generate` del escenario nuevo sigue fail-closed; no se reinterpreta el enum legado `IncomingPrenotificationResponse=3`.
8. Pruebas Xunit nuevas para identidad, huella/cambio de payload y autorización de ruta. Pruebas estáticas de invariantes y no regresión I01/I02.

## Limitaciones explícitas

- La generación de archivo NACHA-M no pudo ejecutarse: `dotnet` no está instalado. **No hay comprobación de compilación**, pruebas Xunit ni fixtures producidos por el builder, ni pruebas SQL Server/PostgreSQL de concurrencia ni NachaUpload E2E. Por tanto **I03 no está homologado ni completamente cerrado**.
- Los controles estáticos inspeccionan símbolos y políticas, pero no demuestran comportamiento financiero o conformidad de bytes reales; en particular debe probarse la interacción de transacción EF con el generador oficial y su persistencia de auditoría.
- La escritura de archivo y el commit BD no constituyen una transacción distribuida. Se implementó limpieza best-effort en el `catch`; en caso de caída abrupta o error de filesystem podría quedar un archivo huérfano. Reconciliación de huérfanos y concurrencia real requieren pruebas y cierre I07.
- `NextDailySequenceAsync` del simulador legado calcula el consecutivo por conteo. La ejecución de I03 lo envuelve en transacción serializable, pero hace falta ensayo simultáneo en ambos motores de BD para homologar el comportamiento y resolver conflictos de serialización.
- La nueva API existe para UAT/local, pero solo funciona con `Enabled=true`, `DifferentialResponsesEnabled=true`, `RequirePublishedDifferentialProfile=true`, perfil de retorno homologado y catálogo/entidades/ciclos válidos. No habilitar en producción.
- La SPA de I06 aún no incorpora el nuevo endpoint ni la descarga JWT; NachaUpload y aceptación tácita corresponden a I04/I05.

## Comprobaciones ejecutadas

| Comando / evidencia | Resultado |
|---|---|
| `python scripts/validate-solution-projects.py` | PASS: 7 proyectos, referencias íntegras |
| `python scripts/test/prenotification_simulator_i01_static_check.py` | PASS_STATIC: 20/20 |
| `python scripts/test/prenotification_simulator_i02_static_check.py` | PASS_STATIC: 26/26 |
| `python scripts/test/prenotification_simulator_i03_static_check.py` | PASS_STATIC: 31/31 |
| `dotnet build ACHInterbank.sln -c Release` | NO_EJECUTADA_POR_AMBIENTE: SDK `dotnet` ausente |
| `dotnet test ... --filter FullyQualifiedName~PrenotificationSimulator` | NO_EJECUTADA_POR_AMBIENTE: SDK `dotnet` ausente |
| Ensayo ACHCOL/CENIT real + concurrencia + NachaUpload | NO_EJECUTADA_POR_AMBIENTE |

## Criterios para cerrar I03 después

- Compilar solución, corregir errores si los hubiera y ejecutar Xunit I01–I03.
- Verificar bytes de registros NACHA-M y causal/adenda para una prenotificación débito y crédito en ACHCOL/CENIT, incluido rechazo por perfil ausente.
- Probar simultaneidad (misma clave, claves distintas sobre mismo original, fallo de disco, fallo builder, reintento y rollback) en SQL Server y PostgreSQL.
- Comprobar que archivos, hash, evidencia, BD y traces están sincronizados, sin efectos secundarios monetarios; subsanar huérfanos.
- E2E NachaUpload, correlación, estados e idempotencia se cierran en I04–I07.

**Próximo incremento:** I04 — procesar mediante NachaUpload las devoluciones simuladas y verificar la correlación entre original, intento y respuesta, sin duplicar aplicación ni estados.

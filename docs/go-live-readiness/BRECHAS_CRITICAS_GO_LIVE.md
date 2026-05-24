# Brechas Criticas Go-Live - ACH Interbank

Fecha de generacion/revalidacion: 2026-05-18 / 2026-05-19
Version: 0.8 preliminar
Rama analizada: `fix/uat-operator-role-seed`
Estado: registro actualizado tras cierre controlado de DEF-UAT-015; requiere triage y aceptacion humana para go-live.

## Matriz

| ID | Severidad | Descripcion | Evidencia | Impacto | Riesgo | Accion correctiva | Responsable | Requiere codigo | Requiere validacion humana | Estado |
|---|---|---|---|---|---|---|---|---|---|---|
| G-01 | CRITICO | UAT real/anonimizado sin acta firmada. | `docs/uat/ACTA_UAT_DATOS_REALES_TEMPLATE.md` | Bloquea UAT formal y productivo. | Alto | Ejecutar UAT, completar evidencias y firmar acta. | Auditoria/Operaciones/Negocio | No | Si | Abierto |
| G-02 | CRITICO | CENIT neteo/liquidez/CUD sin E2E homologado. | `docs/uat/cenit-cycles-liquidity-cud-acceptance-checklist.md` | Riesgo operativo-financiero. | Critico | Ejecutar E2E con evidencia CUD o aceptar brecha formal. | Operaciones/Tesoreria | No inicialmente | Si | Abierto |
| G-03 | CRITICO | Sobre digital sin validacion externa oficial. | `docs/uat/digital-envelope-certificate-acceptance-checklist.md` | Rechazo externo/regulatorio. | Alto | Obtener vector/certificacion externa o waiver formal. | Seguridad/Operaciones | PENDIENTE VALIDAR | Si | Abierto |
| G-04 | ALTO | Naming externo ACH/CENIT/STA pendiente de cierre formal. | `docs/audits/external-filename-normative-matrix-ach-cenit-sta-current.md` | Rechazo de archivos o duplicidad. | Alto | Cerrar reglas por camara/flujo y firmar. | Compliance/Operaciones | PENDIENTE VALIDAR | Si | Abierto |
| G-05 | ALTO | `AchResponsesController` sin `[Authorize]` explicito o no evidenciado. | `src/Cfa.ACHInterbank.Api/Controllers/AchResponsesController.cs`, `tests/Cfa.ACHInterbank.Tests/AchResponsesControllerTests.cs` | Posible exposicion de flujo sensible. | Alto | Se agrego `[Authorize]` general y prueba de reflexion; falta validar policy granular si negocio/seguridad lo exige. | Seguridad/Tecnologia | Si | Si | Corregida - pendiente validar |
| G-06 | ALTO | SPA consume endpoints de interoperabilidad no encontrados en backend. | `web/ach-interbank-ui/src/app/features/nacha-security/services/interoperability-api.service.ts` | Pantalla rota o falsa cobertura UAT. | Alto | No se invento backend; se dejo TODO y prueba de contrato SPA. Requiere decision funcional: backend real, feature flag o retirar flujo. | Tecnologia/Seguridad | Si/PENDIENTE | Si | Parcial |
| G-07 | ALTO | Config productiva SPA apuntaba a `localhost`. | `web/ach-interbank-ui/src/environments/environment.prod.ts`, `web/ach-interbank-ui/nginx.conf` | Despliegue productivo no apto si usa host local quemado. | Alto | `apiBaseUrl` queda relativo (`''`) y Nginx proxya rutas API/Auth/Navigation hacia `achinterbank-api:8080`; build/test Angular OK. | Tecnologia/Operaciones | Si | Si | Corregida tecnicamente - pendiente validar UAT |
| G-08 | MEDIO | README raiz conserva plantilla generica o drift documental. | `README.md` | Confusion operativa y de release. | Medio | README reemplazado por contenido operativo real ACH Interbank. | Tecnologia | No/cambio doc | Si | Corregida |
| G-09 | ALTO | `.env` versionado si aplica. | `.env` existe en pre-check; `.gitignore` actualizado | Riesgo de secretos/datos sensibles. | Alto | Se agregaron reglas `.gitignore`; no se borro ni destrackeo `.env`. Revisar contenido y rotar si corresponde. | Seguridad/Operaciones | PENDIENTE | Si | Parcial |
| G-10 | ALTO | Credenciales de ejemplo en README/docker-compose. | `README.md`, `docker-compose.yml`, `.env.example`, `.env.test.example` | Riesgo de uso accidental. | Alto | Se reemplazaron defaults por placeholders locales/de demo y README advierte no usar secretos reales. | Seguridad/Operaciones | Si para compose | Si | Corregida - pendiente validar |
| G-11 | MEDIO | OpenBao no incluido en docker-compose principal si aplica al ambiente. | `docker-compose.yml`, `scripts/openbao`, `docs/architecture/openbao-integration-2026-04-22.md`, `docs/dev/docker-compose-openbao-uat-2026-04-22.md` | Secret provider puede no estar disponible. | Medio/Alto | Se documento que no se modifica compose principal; usar scripts/docs existentes o excepcion formal. | Seguridad/Operaciones | PENDIENTE | Si | Parcial - requiere decision humana |
| G-12 | MEDIO | Ruta absoluta `DocumentationFile` en csproj API. | `src/Cfa.ACHInterbank.Api/Cfa.ACHInterbank.Api.csproj`, `tests/Cfa.ACHInterbank.Tests/ProjectConfigurationPortabilityTests.cs` | Build/CI fragil fuera de host original. | Medio | Se cambio a ruta relativa MSBuild y se agrego prueba de portabilidad. | Tecnologia | Si | No | Corregida - pendiente build |
| G-13 | MEDIO | Falta de runbook operativo UAT/preproductivo consolidado. | Este documento crea `docs/operations/RUNBOOK_UAT_Y_PREPRODUCTIVO.md` | Operacion no repetible. | Medio | Revisar/aprobar runbook. | Operaciones | No | Si | Parcial |
| G-14 | ALTO | Falta de evidencia de backup/restore/rollback. | NO ENCONTRADO | Recuperacion no demostrada. | Alto | Documentar y ensayar backup/restore/rollback. | Operaciones/Tecnologia | No inicialmente | Si | Abierto |
| G-15 | MEDIO | Falta de evidencia de health checks completos. | `/health/live`, `/health/ready` solo DB | Monitoreo incompleto. | Medio | Agregar checks o documentar alcance y monitoreo externo. | Tecnologia/Operaciones | PENDIENTE | Si | Abierto |
| G-16 | MEDIO | Drift documental entre docs historicos y estado actual. | `docs/audits`, `README.md` | Decision de comite inconsistente. | Medio | Usar paquete comite y documentos current como fuente vigente. | Auditoria/Tecnologia | No | Si | Abierto |
| G-17 | MEDIO | Backend CI dotnet validado en GitHub Actions. | `.github/workflows/dotnet-ci.yml`; contexto reporta dotnet-ci remoto OK; backend build/test remoto OK | Mejora readiness backend; no reemplaza UAT ni evidencia runtime. | Medio | Adjuntar evidencia CI al paquete de release candidate. | Tecnologia/QA | No | Si | CI backend OK |
| G-18 | MEDIO | Angular CI de rama validado; build/test frontend local actual OK. | `.github/workflows/angular-ci.yml`; ultimo angular-ci de rama OK; `npm run build` OK y `npm test` OK con 147 specs | Mejora readiness frontend; no reemplaza UAT funcional. | Medio | Adjuntar evidencia CI/local al paquete de release candidate. | Tecnologia/QA | No | Si | CI Angular OK |
| G-19 | ALTO | Docker runtime levanta API/PostgreSQL/SPA y Nginx enruta SPA->API/Auth/Navigation same-origin. | `docs/uat/EVIDENCIA_TECNICA_UAT_RUNTIME.md`, `docs/go-live-readiness/DOCKER_RUNTIME_READINESS.md`; `:743/health/live` OK, `:743/health/ready` OK, `:743/api/ach/responses` 401, `:743/auth/login` 401 JSON, `:743/navigation/menu` 401 sin token | Habilita UAT tecnico E2E basico desde SPA; no reemplaza UAT funcional ni actas. | Medio | Ejecutar UAT tecnico con datos anonimizados, usuarios/roles y evidencias; mantener auth intacta. | Tecnologia/DevOps/Operaciones | No adicional | Si | Corregida tecnicamente - UAT pendiente |
| G-20 | ALTO | Warning NU1903 por vulnerabilidad alta en `System.Security.Cryptography.Xml` 10.0.0 durante build Docker. | `dotnet list ... --vulnerable` inicial; `Cfa.ACHInterbank.Application.csproj`; validacion posterior sin vulnerabilidades | Riesgo de seguridad pre-go-live mitigado tecnicamente. | Bajo/Medio residual | Se fijo `System.Security.Cryptography.Xml` 10.0.8 como referencia explicita y se ejecuto restore/build/test/list vulnerable. | Seguridad/Tecnologia | Si | Si | Corregida tecnicamente |
| G-21 | MEDIO | PostgreSQL no estaba publicado al host para validacion UAT tecnica local. | `docker-compose.yml`, `.env.example`, `docker compose port postgres 5432`, `Test-NetConnection localhost -Port 5432` | Dificultaba troubleshooting con DBeaver/pgAdmin y verificacion local controlada. | Medio | Publicado solo en loopback `127.0.0.1:${POSTGRES_HOST_PORT:-5432}:5432`; documentar que no aplica como patron productivo. | DevOps/Operaciones | Si | Si | Corregida tecnicamente - no productivo |
| G-22 | MEDIO | UAT tecnico autenticado basico fue reintentado con variables demo seguras presentes; login real, token, menu y endpoints read-only pasan. El cierre DEF-UAT-015 confirma roles `Admin` y `ACH.Operator` para `admin` en respuesta/JWT. | `docs/uat/EJECUCION_UAT_TECNICO_BASICO.md`, `docs/uat/EVIDENCIAS_UAT_TECNICO_BASICO.md`, `docs/uat/MATRIZ_DEFECTOS_UAT.md`, `UserRoleSeedTests` | Ya no bloquea el UAT tecnico autenticado basico; no reemplaza UAT funcional ni actas. | Bajo/Medio | Mantener evidencia runtime y no usar credenciales productivas. | Seguridad/Tecnologia/QA | Si, seed/migracion | Si | OK tecnico con observaciones |
| G-23 | MEDIO | Browser integrado no aporto evidencia visual automatizada en esta sesion; la navegacion quedo soportada por logs SPA y validaciones HTTP con token. | `docs/uat/EVIDENCIAS_UAT_TECNICO_BASICO.md`, DEF-UAT-014 | Riesgo bajo para UAT tecnico HTTP; requiere captura manual o herramienta habilitada si el acta exige evidencia visual. | Medio | Reintentar con navegador local/manual controlado o adjuntar capturas sanitizadas. | QA/DevOps | No | Si | Abierto |
| G-24 | ALTO | UAT funcional sintetico core API ejecutado parcialmente OK: datos maestros suficientes, transaccion sintetica `UAT-SINT-001` creada por API directa, persistida y duplicado rechazado de forma controlada; reintento HTTP por SPA Docker ya responde desde API. | `docs/uat/UAT_FUNCIONAL_SINTETICO.md`, `docs/uat/EVIDENCIAS_UAT_FUNCIONAL.md`, `docs/uat/ACTA_TECNICA_PRELIMINAR.md` | Reduce riesgo tecnico del core transaccional, pero no cierra UAT formal, evidencia visual ni actas. | Medio/Alto | Adjuntar evidencia visual sanitizada y cerrar defectos restantes de trazabilidad/idempotencia antes de UAT formal. | QA/Operaciones/Tecnologia | No inicialmente | Si | Parcialmente OK |
| G-25 | ALTO | SPA Docker no proxya rutas funcionales raiz y devolvia `index.html`; corregido en Nginx para `/financial-institutions`, `/ach-cycles`, `/clearing-houses`, `/company-entry-descriptions` y endpoints transaccionales confirmados. | DEF-UAT-016, `web/ach-interbank-ui/nginx.conf`, `docs/uat/EVIDENCIAS_UAT_FUNCIONAL.md` | Ya no bloquea el reintento HTTP del UAT funcional desde `http://localhost:743`; queda pendiente evidencia visual/acta formal. | Medio | Mantener inventario de endpoints SPA/API y revalidar en CI/runtime cuando se agreguen nuevos servicios raiz. | Tecnologia/DevOps | Si/config aplicada | Si | Cerrado tecnicamente |
| G-26 | MEDIO | Trazabilidad de nuevas transacciones revalidada: `UAT-SINT-TRACE-001` genero evento inicial `Pending -> Pending`, `Source=System`, `ReasonCode=CREATED`; `UAT-SINT-001` historica queda sin backfill. | DEF-UAT-017, `TransactionPersister.cs`, `AchTransactionNachaTests.cs`, `docs/uat/EVIDENCIAS_UAT_FUNCIONAL.md`; transaction ID `2`, `event_count=1` | Riesgo residual bajo acotado a registros historicos sin backfill; no bloquea nuevas transacciones sinteticas. | Bajo/Medio | Mantener evidencia y decidir si se requiere backfill historico formal; no necesario para cerrar DEF-UAT-017 en nuevas transacciones. | Tecnologia/QA/Auditoria | Si, aplicado bajo riesgo | Si | Cerrada funcionalmente para nuevas transacciones |
| G-27 | MEDIO | Idempotencia/deduplicacion transaccional queda formalizada documentalmente como contrato actual: duplicado responde 400 controlado; no hay `Idempotency-Key`, hash de payload ni replay. | DEF-UAT-018, `docs/go-live-readiness/CONTRATO_IDEMPOTENCIA_TRANSACCIONES.md`, `TransactionPolicyServiceTests.cs`, `TransactionsControllerTests.cs` | Riesgo residual acotado: clientes deben tratar 400 de duplicado como rechazo controlado no reintentable; evolucion a 409/key/replay requiere decision. | Bajo/Medio | Mantener 400 documentado para UAT actual; decidir en arquitectura si se migra a 409 Conflict o se introduce idempotency key/replay antes de preproductivo/productivo. | Arquitectura/Tecnologia | No en esta fase | Si | Cerrada documentalmente; decision evolutiva pendiente |
| G-28 | ALTO | NACHA-M layouts tecnicos existen y el proxy SPA Docker expone catalogos, pero el UAT integrado ACH Colombia/CENIT no genero archivo NACHA-M valido por prenotificacion previa ausente. DEF-UAT-021 ya evita falso exito 0 bytes con 422 controlado. | `docs/uat/UAT_NACHA_M_CAMPO_A_CAMPO.md`, `docs/uat/EVIDENCIAS_NACHA_M_UAT.md`, `docs/go-live-readiness/MATRIZ_NACHA_M_ACH_COLOMBIA.md`, `docs/go-live-readiness/MATRIZ_NACHA_M_CENIT.md` | Riesgo de rechazo externo o incumplimiento si no hay archivo no vacio validado campo-a-campo. | Alto | Crear prenotificaciones UAT validas sin bypass/backdating, generar archivo UAT no vacio y validar hash/totales/block count con firma/waiver. | Compliance/Operaciones/Tecnologia | PENDIENTE VALIDAR | Si | Abierto |
| G-29 | MEDIO | Rol `ACH.Operator` esperado para usuario demo `admin` fue asignado por seed/migracion controlada, manteniendo tambien `Admin`. | `RoleConfiguration`, `UserRoleConfiguration`, migracion `AddAdminOperatorRoleSeed`, login sanitizado/JWT con roles `Admin,ACH.Operator`, `/navigation/menu` y endpoints read-only 200 con Bearer | Cierra cobertura basica de roles UAT; persiste necesidad de matriz endpoint-rol formal para productivo. | Bajo | Mantener `admin` como usuario demo multirol solo para UAT controlado; evaluar usuario operador separado antes de preproductivo si seguridad lo exige. | Seguridad/Tecnologia/QA | Si | Si | Cerrado |
| G-30 | ALTO | Exportacion NACHA por `/NachaExport/{cycleId}` fue corregida para no responder 200 con archivo vacio cuando no se cumplen prerequisitos; ahora retorna 422 JSON controlado. | DEF-UAT-021; `docs/uat/evidencias/nacha-m-uat/*/attempt_4_controlled_422_response.txt` | Riesgo de falsa evidencia operativa reducido; sigue pendiente archivo valido exportable. | Alto | Reintentar cuando existan transacciones exportables con prenotificacion valida y confirmar archivo > 0 bytes. | Tecnologia/QA | Si | Si | Cerrado tecnico |
| G-31 | ALTO | `Proc_Contrapartidas` tiene guardrail `DryRun` por defecto en UAT/local; runtime valido `PROC_DRY_RUN` sin `SOAP request` ni transmision externa. | DEF-UAT-022; `docs/uat/EVIDENCIAS_SOAP_PROC_CONTRAPARTIDAS.md`, `docs/uat/evidencias/soap-proc-contrapartidas/runtime_dry_run_validation.md` | Riesgo de intento de conexion externa no autorizada mitigado para UAT/local; endpoint UAT/mock real sigue pendiente. | Alto | Mantener `DryRun/Disabled` en UAT; habilitar `Live` solo con endpoint UAT/mock aprobado y evidencia de homologacion. | Integracion/DevOps/Seguridad | EVIDENCIA OK UAT/LOCAL | Si | Cerrado tecnico |

## Decision Inicial

Con cualquier brecha CRITICA abierta, el estado permanece **NO-GO productivo**. UAT controlado puede avanzar si el ambiente esta disponible, los datos estan anonimizados y las brechas se comunican como restricciones. Backend CI/local y Angular CI de rama/local estan OK; Docker compose config/build/runtime estan OK para API, PostgreSQL, health checks, SPA estatica y proxy SPA->API/Auth/Navigation/funcional/NACHA. El UAT tecnico autenticado basico queda **OK con observaciones** y DEF-UAT-015 queda cerrado para el usuario demo `admin` multirol. El UAT funcional sintetico queda **PARCIALMENTE OK**. El UAT integrado NACHA/SOAP deja evidencia de transacciones por camara, export NACHA con 422 controlado por falta de prenotificacion y guardrail SOAP `DryRun` sin transmision externa. NACHA-M real UAT sigue bloqueado hasta prenotificacion valida y archivo no vacio. Productivo sigue **NO-GO**.

## Actualizacion 2026-05-23 - paquete final UAT SOAP end-to-end

Se consolida paquete firmable UAT SOAP end-to-end en `docs/uat/evidencias/soap-end-to-end-final/` con acta, matriz de escenarios, inventario, hashes, sanitizacion y reporte de no transmision externa.

Estado SOAP UAT/local:

- `Proc_Contrapartidas`: cerrado tecnico, sin fallback requerido, DryRun/no transmision.
- `Proc_Transacciones`: cerrado tecnico, usa NACHA-M desagregado, SOAP Envelope DryRun sanitizado, no transmision.
- `RegistrarRespuestaTransaccion`: cerrado tecnico UAT, no monetario, aprueba/rechaza prenotificaciones CFA pendientes, no invoca WSCFAACH.

Esta actualizacion habilita continuar UAT controlado, pero no reduce las brechas productivas criticas: homologacion externa, certificados/sobre digital, CENIT/CUD, backup/restore/rollback, UAT bancario formal y aprobaciones humanas siguen abiertas. Productivo permanece **NO-GO**.

## Actualizacion 2026-05-19 - Parametrizacion reglas por camara

Se implemento configuracion administrable de reglas de prenotificacion por camara/naturaleza/tipo para reducir hard-code normativo y preparar el cierre de DEF-UAT-020. Nuevas evidencias:

- `docs/auditoria-parametrizacion/`
- `docs/go-live-readiness/CONFIGURACION_REGLAS_CAMARA_PRENOTIFICACION.md`
- `docs/go-live-readiness/MATRIZ_REGLAS_PRENOTIFICACION_POR_CAMARA.md`

La brecha G-28/DEF-UAT-020 sigue abierta hasta generar archivo NACHA-M UAT no vacio con prenotificacion valida por camara. Productivo permanece **NO-GO**.
## Actualizacion 2026-05-20 - NACHA-M UAT no vacio

DEF-UAT-020 mejora de abierto bloqueado a **parcial tecnico**: se generaron archivos NACHA-M UAT no vacios por ACH Colombia y CENIT desde el sistema. Persisten brechas bloqueantes para productivo: transaccion debito monetaria post-prenotificacion madura por 3 dias habiles, validacion normativa campo-a-campo, homologacion/waiver y UAT formal con actas.

## Actualizacion 2026-05-20 - DEF-UAT-020 nomenclatura y NACHA-M UAT

Estado productivo: NO-GO.

Resultado del ciclo controlado:

| Camara | Archivo generado | SHA256 | ZZZ | Campo 7 registro 1 | Registros | Resultado |
|---|---|---|---:|---|---|---|
| ACH Colombia | docs/uat/evidencias/nacha-m-uat/ach-colombia/0001283.002.1 | E4DAEEE551596D067357953C552CD521871F635F6703D27700171EBC10A0026E | 002 | B | 1/5/6/7/8/9 | OK tecnico UAT |
| CENIT | docs/uat/evidencias/nacha-m-uat/cenit/0001283.001.1 | FD52F7834ADEC53C720E4A877B1D48A8AC15B149BEB7FAFB91EC57CF1B88FCD4 | 001 | A | 1/5/6/7/8/9 | OK tecnico UAT; homologacion normativa formal pendiente |

Evidencia comun:

- Patron aplicado: RRRRTTT.ZZZ.1.
- Originador: Cooperativa Financiera de Antioquia, unico FinancialInstitution.IsDefaultSource=true.
- RRRR=0001 y TTT=283 derivados de la configuracion de CFA.
- Mapeo validado: 001 -> A y 002 -> B en registro tipo 1 campo 7.
- Archivos generados por /NachaExport/{cycleId}; no fueron creados manualmente.
- Sin transmision externa a ACH Colombia o CENIT.
- Proc_Contrapartidas permanece en DryRun para UAT/local.

Observacion normativa:

- ACH Colombia se valida contra MAN-004 V32.
- CENIT se valida tecnicamente con ejemplos disponibles en el proyecto y queda pendiente homologacion normativa formal.

## Actualizacion 2026-05-20 - DEF-UAT-020 prenotificaciones CFA

Se genero evidencia UAT de prenotificaciones CFA por ACH Colombia y CENIT:

- ACH Colombia: `UAT-ACH-PRE-CFA-001`, TransactionId 256, archivo `0001283.004.1`, codigo NACHA `28`, SHA256 `E4695D004A35087B20485339E844F7C722E059C1DA58E732219370FAC0F9155A`.
- CENIT: `UAT-CEN-PRE-CFA-001`, TransactionId 257, archivo `0001283.002.1`, codigo NACHA `28`, SHA256 `B36BE4DB8A9EC2E3384A69A06CC0866BF24E05A2E6886B056498E361236A024C`.

Estado de brecha: **DEF-UAT-020 queda OK tecnico UAT para prenotificaciones CFA y permanece parcial normativo por homologacion formal CENIT/campo-a-campo externo**. Productivo sigue **NO-GO**.

## Actualizacion 2026-05-20 - Simulador NACHA-M de Entrada

| Brecha | Estado | Impacto | Accion requerida |
|---|---|---|---|
| Falta de archivos inbound sinteticos para ejecutar NachaUpload UAT | Cerrada tecnicamente para UAT/local | Permite preparar insumos de carga manual | Ejecutar carga real por NachaUpload y registrar resultados |
| Validacion real de procesamiento inbound | Abierta | Bloquea GO productivo | Cargar manualmente archivos generados y validar estados/auditoria |

El simulador queda deshabilitado por defecto fuera de Development/UAT, no transmite externamente y no importa automaticamente. Productivo continua **NO-GO**.

## Actualizacion 2026-05-20 - UX Configuracion SOAP

| Brecha | Estado | Impacto | Accion requerida |
|---|---|---|---|
| Saturacion visual en `/integraciones/soap-settings` | Cerrada tecnicamente frontend | Reduce riesgo operativo de edicion incorrecta de configuracion SOAP | Validacion visual UAT/manual en SPA Docker |
| Exposicion de secretos en UI SOAP | Controlada | La pantalla no muestra secretos completos ni certificados privados | Mantener gestion segura externa |
| Proc_Contrapartidas Live | NO habilitado por defecto | Conserva guardrail UAT/local | Live solo con autorizacion formal |

El cambio usa resumen/lista compacta y modales de detalle, edicion y prueba. No modifica backend, endpoints ni semantica funcional SOAP. Productivo permanece **NO-GO**.

## Actualizacion 2026-05-21 - Auditoria end-to-end SOAP

| Brecha | Estado | Impacto | Accion requerida |
|---|---|---|---|
| Fallback requerido de `Proc_Contrapartidas` si no hay mapping publicado | Cerrada tecnicamente | Ya no puede generar XML ni DryRun exitoso sin mapping funcional completo | Mantener mappings publicados requeridos y pruebas de no regresion |
| `Proc_Transacciones` no tiene guardrail DryRun especifico equivalente a Contrapartidas | Cerrada tecnicamente | UAT/local ya no transmite externamente con `ProcTransacciones:Mode=DryRun/Disabled` | Mantener modo no Live hasta autorizacion formal |
| `RegistrarRespuestaTransaccion` no consume `IntegrationMappingSet` | Cerrada tecnicamente | Trace parametrizado persistido antes del gateway | Mantener pruebas de no regresion no monetaria |

Clasificacion confirmada:

- `Proc_Contrapartidas`: `MonetaryDebitRequest`, mueve debitos originados por CFA.
- `Proc_Transacciones`: `MonetaryCreditRequest`, mueve creditos originados por otra entidad, CFA receptora.
- `RegistrarRespuestaTransaccion`: `DifferentialResponseNotification`, no mueve dinero ni afecta saldos.

Productivo permanece **NO-GO**.

## Actualizacion 2026-05-21 - garantia Transaction Integration Readiness

Se implemento una garantia automatizada para evitar falsos OK en integraciones SOAP:

- `GET /Transactions/{id}/integration-readiness` resuelve operacion esperada y readiness de mappings sin mutar estado ni invocar SOAP.
- Debito CFA -> `WSCFAACH / Proc_Contrapartidas / MonetaryDebitRequest`.
- Credito externo -> `WSCFAACH / Proc_Transacciones / MonetaryCreditRequest`.
- Respuesta diferencial -> `WSAXON / RegistrarRespuestaTransaccion / DifferentialResponseNotification`, `movesMoney=false`.
- Missing mapping queda `Failed`.
- Fallback requerido en `Proc_Contrapartidas` queda `Failed`, no `Partial` ni `Ok`.

Brechas persistentes:

- ejecutar acta UAT firmada con evidencias runtime representativas;
- sostener Productivo en NO-GO hasta homologacion externa formal.

Productivo permanece **NO-GO**.
## Actualizacion 2026-05-23 - SOAP/NACHA-M desagregado

- Cerrado tecnico UAT: `Proc_Transacciones` puede alimentarse desde NACHA-M desagregado (`NachaHeaders`, `BatchHeaders`, `EntryDetails`, `AddendaRecords`, `BatchControls`, `FileControls`) mediante catalogo controlado, mapping set y trace campo-a-campo.
- Cerrado tecnico UAT: `DEF-UAT-SOAP-MAP-004`, `RegistrarRespuestaTransaccion` aprueba/rechaza prenotificaciones CFA pendientes desde respuesta diferencial homologada, cruza NACHA-M desagregado y persiste trace/evento sin movimiento monetario.

## Actualizacion 2026-05-23 - DEF-UAT-SOAP-MAP-004

| Brecha | Estado | Evidencia | Observacion |
|---|---|---|---|
| Respuesta diferencial aprueba prenotificacion CFA pendiente | Cerrado tecnico UAT | `docs/uat/evidencias/soap-integrations/prenotification-responses/approved/` | Estado `Pending -> Certified`, sin movimiento monetario |
| Respuesta diferencial rechaza prenotificacion CFA pendiente | Cerrado tecnico UAT | `docs/uat/evidencias/soap-integrations/prenotification-responses/rejected/` | Estado `Pending -> ReturnedByEpr`, causal `R03` |
| Envelope formal Proc_Transacciones DryRun | Cerrado tecnico UAT | `docs/uat/evidencias/soap-integrations/mapping-trace/proc_transacciones/proc_transacciones_envelope_sanitizado.xml` | No transmision externa |

Productivo permanece **NO-GO** por homologacion externa y acta formal pendiente.
- Productivo: **NO-GO**.

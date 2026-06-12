# Scorecard Go-Live Readiness - ACH Interbank

Fecha de generacion/revalidacion: 2026-05-18 / 2026-05-19
Version: 0.8 preliminar
Rama analizada: `fix/uat-operator-role-seed`
Estado inicial: Candidato UAT controlado / NO-GO productivo.  
Uso: instrumento preliminar para comite; requiere evidencias y firmas.

## Criterios De Semaforo

| Puntaje | Clasificacion |
|---:|---|
| >= 90 | Candidato GO |
| 75-89 | GO condicionado |
| 60-74 | NO-GO con brechas altas |
| < 60 | NO-GO |

## Ponderacion

| Categoria | Peso | Estado inicial | Puntaje preliminar | Puntaje ponderado | Evidencia | Observacion |
|---|---:|---|---:|---:|---|---|
| Funcionalidad core | 20% | PARCIAL | 82 | 16.4 | Backend CI/local OK; Angular local OK; API/DB health OK; proxy SPA->API/Auth/Navigation/funcional/NACHA OK; transacciones sinteticas, evento inicial nuevo e idempotencia documental OK | Core API funcional sintetico y proxy Docker mejoran; UAT bancario formal sigue pendiente |
| UAT y evidencias | 20% | PARCIAL | 64 | 12.8 | Docs UAT tecnico/funcional actualizados; evidencia de transaccion, persistencia, idempotencia, proxy funcional/NACHA, logs, build, pruebas y acta preliminar final | Actas firmadas, evidencia visual SPA y homologaciones siguen pendientes |
| Seguridad | 15% | PARCIAL | 73 | 10.95 | `[Authorize]`, permisos, `dotnet list --vulnerable` sin hallazgos tras `System.Security.Cryptography.Xml` 10.0.8; `admin` evidencia `Admin` + `ACH.Operator` tras seed/migracion controlada | `.env` trackeado, custodia de secretos y certificados requieren decision; falta matriz endpoint-rol formal |
| Interoperabilidad externa | 15% | CRITICO | 40 | 6.00 | NACHA layouts tecnicos identificados; transacciones UAT ACH Colombia/CENIT creadas; export vacio corregido con 422; SOAP dry-run runtime validado sin transmision | NACHA-M real UAT sigue bloqueado por prenotificacion previa; validacion externa/homologacion sigue pendiente |
| Operacion y soporte | 10% | PARCIAL | 68 | 6.8 | Docker compose config/build/runtime, proxy SPA->API/Auth/Navigation/funcional/NACHA OK y PostgreSQL loopback 5432 para UAT local; runbooks/docs | Backup/restore/rollback pendientes |
| Observabilidad | 10% | PARCIAL | 68 | 6.8 | `/health/live` y `/health/ready` OK en Docker | Health cubre API/DB; faltan Quartz/externos y monitoreo |
| Documentacion y trazabilidad | 10% | PARCIAL | 83 | 8.3 | README, docs readiness, evidencia runtime, UAT funcional sintetico, matriz NACHA layouts, contrato idempotencia y acta preliminar final actualizados | Firmas/evidencias UAT formales pendientes; contrato idempotencia evolutivo queda pendiente si se exige 409/key/replay |
| **Total** | **100%** | **NO-GO con brechas altas** |  | **68.1** |  | **NO-GO productivo** |

## Estado Inicial

Resultado preliminar tras backend CI/local OK, Angular CI de rama y pruebas locales OK, validacion Docker runtime, proxy SPA->API/Auth/Navigation/funcional/NACHA OK, UAT tecnico autenticado basico OK con observaciones, UAT funcional sintetico parcial, cierre funcional de DEF-UAT-017, cierre documental de DEF-UAT-018, cierre tecnico de DEF-UAT-019, correccion NU1903, cierre DEF-UAT-015 para usuario demo multirol, cierre tecnico DEF-UAT-021 y cierre tecnico UAT/local DEF-UAT-022: **68.1 / 100 - NO-GO productivo con brechas altas**.

Clasificacion operacional: **Candidato UAT controlado**, no apto para productivo.

## Brechas Que Impiden Subir De Nivel

- UAT real/anonimizado sin acta firmada.
- Evidencias UAT incompletas.
- CENIT neteo/liquidez/CUD sin E2E homologado.
- Sobre digital/firma/certificados sin validacion externa oficial.
- Naming externo ACH/CENIT/STA sin cierre formal.
- Falta matriz endpoint-rol y validacion de politica granular para `AchResponsesController` aunque ya tiene `[Authorize]`.
- Endpoint mismatch de interoperabilidad SPA/backend.
- `environment.prod.ts` corregido a base relativa y Nginx proxya `/auth`, `/navigation`, `/api`, `/health`, `/openapi` y `/scalar`; falta UAT tecnico funcional con datos anonimizados.
- Angular CI remoto OK segun contexto; adjuntar evidencia al paquete RC.
- Backend CI remoto OK segun contexto; adjuntar evidencia al paquete RC.
- Docker compose config/build/runtime OK para API/PostgreSQL/SPA estatica y proxy SPA->API/Auth/Navigation; PostgreSQL publicado en loopback 5432 solo para UAT local.
- UAT tecnico autenticado basico queda OK con observaciones: login demo `admin`, token, roles `Admin` + `ACH.Operator`, menu y endpoints read-only pasan; falta adjuntar evidencia visual si el acta la exige.
- UAT funcional sintetico queda parcialmente OK: datos maestros suficientes, transaccion `UAT-SINT-001` creada, persistida, conciliacion basica consultada e idempotencia controlada.
- SPA Docker ya no devuelve `index.html` para las rutas funcionales reintentadas; falta evidencia visual/acta formal.
- Trazabilidad transaccional historica parcial: `UAT-SINT-001` no tiene evento inicial por ausencia de backfill; `UAT-SINT-TRACE-001` cierra DEF-UAT-017 para nuevas transacciones.
- Contrato de idempotencia actual cerrado documentalmente: duplicado controlado devuelve HTTP 400; queda decision evolutiva 409/idempotency key/replay si aplica.
- NU1903 de vulnerabilidad alta en `System.Security.Cryptography.Xml` 10.0.0 corregida con referencia explicita 10.0.8; mantener monitoreo de advisories.
- NACHA-M layouts tecnicos 1/5/6/7/8/9 presentes y proxied; intento real UAT ACH Colombia/CENIT queda bloqueado por prenotificacion previa ausente, ya sin falso exito 0 bytes.
- SOAP `Proc_Contrapartidas` tiene envelopes XML dry-run sanitizados y guardrail runtime `DryRun` por defecto; endpoint UAT/mock real sigue pendiente.
- Rol `ACH.Operator` visible para `admin` tras seed/migracion controlada; queda pendiente matriz endpoint-rol formal para productivo.
- Riesgo de `.env` versionado y defaults sensibles en compose/documentacion.
- Custodia de secretos definida para el ambiente si aplica.
- Backup/restore/rollback sin evidencia.

## Reglas De Decision

- UAT sin acta firmada = NO-GO productivo.
- CENIT CUD sin evidencia = NO-GO productivo si aplica al alcance.
- Sobre digital sin validacion externa = NO-GO productivo si aplica al flujo externo.
- Secretos/credenciales en Git = NO-GO hasta saneamiento o aceptacion formal de riesgo.
- SPA prod localhost = NO-GO despliegue productivo.
- Defecto bloqueante abierto = NO-GO productivo.
- Evidencia tecnica automatizada no reemplaza aprobacion humana.
- Backend CI OK no implica GO productivo.
- Docker runtime/proxy OK no implica GO productivo ni reemplaza UAT con actas.

## Proyeccion De Mejora

| Cierre requerido | Impacto esperado |
|---|---|
| Acta UAT y evidencias firmadas | Subir UAT/evidencias a 75+ |
| Cierre seguridad/configuracion | Subir seguridad a 75+ |

## Actualizacion 2026-05-20 - prenotificaciones CFA

Se agrego evidencia UAT tecnica de prenotificaciones CFA:

- Consulta read-only autenticada con estado funcional en espanol.
- NACHA-M no vacio de prenotificacion ACH Colombia: `0001283.004.1`, codigo `28`.
- NACHA-M no vacio de prenotificacion CENIT: `0001283.002.1`, codigo `28`.
- Nomenclatura `RRRRTTT.ZZZ.N`, campo 7 y hashes validados.

El avance mejora la evidencia tecnica de interoperabilidad NACHA-M, pero no cambia la decision productiva: CENIT requiere homologacion normativa formal y UAT bancario/formal con actas. Estado global se mantiene **NO-GO productivo / continuar UAT controlado**.
| Validacion externa sobre/naming | Subir interoperabilidad a 75+ |
| E2E CENIT/CUD | Subir funcionalidad/interoperabilidad |
| Backup/restore/rollback ensayado | Subir operacion |

## Actualizacion 2026-05-23 - UAT SOAP end-to-end final

Se agrega paquete formal UAT SOAP end-to-end:

- Acta: `docs/uat/ACTA_UAT_SOAP_END_TO_END_FORMAL.md`.
- Resumen ejecutivo: `docs/uat/RESUMEN_EJECUTIVO_UAT_SOAP_END_TO_END.md`.
- Matriz: `docs/uat/MATRIZ_ESCENARIOS_UAT_SOAP_END_TO_END_FINAL.md`.
- Inventario/hashes/sanitizacion/no transmision: `docs/uat/evidencias/soap-end-to-end-final/`.

Impacto sobre score: mejora evidencia UAT e interoperabilidad SOAP para continuar UAT controlado. No se incrementa score productivo automáticamente porque persisten brechas no tecnicas y externas: homologacion, certificados/sobre digital, CENIT/CUD, backup/restore/rollback, UAT bancario formal y aprobaciones.

Estado: **NO-GO productivo**.

## Actualizacion 2026-05-19

La parametrizacion de reglas por camara mejora readiness tecnico de NACHA-M, pero no eleva a GO productivo. El score no debe subir artificialmente hasta revalidar runtime con migracion/seed, crear prenotificaciones UAT validas y generar archivo NACHA-M no vacio por camara.

| Categoria | Impacto |
|---|---|
| Funcionalidad core | Mejora por control configurable de prerequisitos. |
| UAT y evidencias | Sigue parcial: falta archivo NACHA-M no vacio. |
| Seguridad/operacion | Sin cambio productivo; endpoints protegidos. |
| Readiness final | **NO-GO**. |
## Ajuste 2026-05-20

Se reconoce avance tecnico en DEF-UAT-020 por generacion de archivos NACHA-M UAT no vacios para ACH Colombia y CENIT, pero sin subir a estado GO: falta maduracion de prenotificacion para debito monetario, homologacion campo-a-campo, CENIT/CUD formal, certificados/sobre digital productivo y actas. Productivo permanece **NO-GO**.

## Actualizacion 2026-05-20 - DEF-UAT-020 nomenclatura y NACHA-M UAT

Estado productivo: NO-GO.

Resultado del ciclo controlado:

| Camara | Archivo generado | SHA256 | ZZZ | Campo 7 registro 1 | Registros | Resultado |
|---|---|---|---:|---|---|---|
| ACH Colombia | docs/uat/evidencias/nacha-m-uat/ach-colombia/0001283.002.1 | E4DAEEE551596D067357953C552CD521871F635F6703D27700171EBC10A0026E | 002 | B | 1/5/6/7/8/9 | OK tecnico UAT |
| CENIT | docs/uat/evidencias/nacha-m-uat/cenit/0001283.001.1 | FD52F7834ADEC53C720E4A877B1D48A8AC15B149BEB7FAFB91EC57CF1B88FCD4 | 001 | A | 1/5/6/7/8/9 | OK tecnico UAT; homologacion normativa formal pendiente |

Evidencia comun:

- Patron aplicado: RRRRTTT.ZZZ.N.
- Originador: Cooperativa Financiera de Antioquia, unico FinancialInstitution.IsDefaultSource=true.
- RRRR=0001 y TTT=283 derivados de la configuracion de CFA.
- Mapeo validado: 001 -> A y 002 -> B en registro tipo 1 campo 7.
- Archivos generados por /NachaExport/{cycleId}; no fueron creados manualmente.
- Sin transmision externa a ACH Colombia o CENIT.
- Proc_Contrapartidas permanece en DryRun para UAT/local.

Observacion normativa:

- ACH Colombia se valida contra MAN-004 V32.
- CENIT se valida tecnicamente con ejemplos disponibles en el proyecto y queda pendiente homologacion normativa formal.

## Actualizacion 2026-05-20 - Simulador NACHA-M Entrada

Impacto en scorecard: mejora capacidad UAT controlada, sin cambiar la decision productiva.

| Categoria | Estado | Observacion |
|---|---|---|
| UAT y evidencias | Mejora tecnica | Hay generador inbound para preparar archivos de carga manual |
| Seguridad/guardrails | OK tecnico | Simulador deshabilitado por defecto fuera de Development/UAT; sin transmision externa |
| Interoperabilidad externa | Pendiente | No reemplaza homologacion de ACH Colombia/CENIT |
| Operacion | Pendiente | Requiere ejecucion NachaUpload y acta de evidencias |

Productivo permanece **NO-GO**.

## Actualizacion Transaction Integration Readiness - 2026-05-21

Se agrega control tecnico verificable por pruebas para alinear transaccion, operacion esperada y readiness de mappings. El fallback requerido de `Proc_Contrapartidas` queda bloqueado antes de XML/DryRun/dispatch, pero no aumenta el score productivo por las brechas remanentes:

- `Proc_Transacciones` ya cuenta con guardrail UAT/local no transmisivo;
- `RegistrarRespuestaTransaccion` ya persiste trace campo-a-campo, pero requiere acta UAT firmada.

Decision final sin cambios: **NO-GO**.

## Actualizacion 2026-05-20 - UX Configuracion SOAP

Impacto en scorecard: mejora operabilidad frontend y reduce riesgo de error de usuario, sin elevar la decision productiva.

| Categoria | Estado | Observacion |
|---|---|---|
| Operacion y soporte | Mejora tecnica | Pantalla SOAP pasa a resumen/lista compacta con modales |
| Seguridad | Sin exposicion adicional | Secretos completos y certificados privados no se muestran |
| UAT y evidencias | Pendiente visual formal | Build/test Angular OK; falta captura/acta si el comite la exige |
| Readiness final | NO-GO | No sustituye homologacion externa ni UAT formal |

No se ajusta el score de forma artificial. Productivo permanece **NO-GO**.

## Actualizacion UX Integraciones - 2026-05-21

Se mejora la categoria UAT/evidencias y documentacion/trazabilidad con validacion visual obligatoria para `/integraciones/soap-settings` y `/integraciones/mappings`. El scorecard no se incrementa automaticamente hasta contar con evidencia DOM/screenshot y cierre de validaciones completas. Productivo permanece **NO-GO**.

## Actualizacion SOAP end-to-end - 2026-05-21

La auditoria mejora trazabilidad y claridad de integraciones, pero no incrementa readiness productivo porque hay brechas abiertas:

- trazabilidad parametrizada y sostenida de `Proc_Contrapartidas` debe mantenerse con mappings publicados requeridos.
- acta UAT formal pendiente para los tres flujos.
- homologacion externa formal pendiente.

Productivo permanece **NO-GO**.
## Actualizacion 2026-05-23

| Dimension | Estado | Observacion |
|---|---|---|
| SOAP `Proc_Transacciones` con NACHA-M desagregado | OK tecnico UAT | Mappings pueden tomar fuentes controladas del archivo cargado. |
| Trace campo-a-campo | OK tecnico UAT | Se conserva valor fuente sanitizado para evidencia. |
| Respuestas diferenciales sobre prenotificaciones CFA | OK tecnico UAT | Caso de uso aprueba/rechaza prenotificaciones pendientes, persiste trace/evento y no mueve dinero. |
| Envelope Proc_Transacciones DryRun | OK tecnico UAT | Evidencia formal `proc_transacciones_envelope_sanitizado.xml`, sin transmision externa. |
| Productivo | NO-GO | No se autoriza salida productiva. |

## Actualizacion 2026-05-24 - Regresion final SPA Angular

La SPA Angular queda OK tecnico UAT para las rutas auditadas:

- Playwright global historico: 23 rutas, P0=0, P1=0, P2=0.
- Playwright regresion final: 30 rutas, P0=0, P1=0, P2=0.
- Angular build: OK.
- Angular tests: OK, 214 SUCCESS.
- Reportes PDF priorizados: OK tecnico frontend, sin descarga vacia.

Impacto sobre score: mejora la evidencia UAT del frente SPA y reduce riesgo operativo frontend. No se incrementa artificialmente el score productivo porque persisten brechas externas y formales: homologacion, certificados/sobre digital, CENIT/CUD, backup/restore/rollback, UAT bancario formal y aprobaciones.

Estado: **NO-GO productivo / continuar UAT controlado**.

# Plan de Cierre de Brechas - ACH Interbank

Fecha de generacion/revalidacion: 2026-05-18 / 2026-05-19
Version: 0.8 preliminar
Rama analizada: `fix/uat-operator-role-seed`
Estado: plan de trabajo actualizado tras cierre controlado de DEF-UAT-015; incluye correcciones de bajo riesgo aplicadas y pendientes de validacion.

## Matriz De Acciones

| ID brecha | Accion | Tipo | Riesgo | Archivos a modificar en fase posterior | Responsable | Dependencia | Criterio de cierre | Estado |
|---|---|---|---|---|---|---|---|---|
| G-01 | Ejecutar UAT con datos anonimizados, completar acta e indice de evidencias. | Prueba UAT/Evidencia | Alto | `docs/uat/*` | Operaciones/Auditoria | Ambiente UAT y datos | Acta firmada sin bloqueantes | Pendiente |
| G-02 | Ejecutar E2E CENIT neteo/liquidez/CUD con evidencia homologada. | Validacion operacion | Critico | Evidencias externas y scorecard | Operaciones/Tesoreria | Soporte CUD | Evidencia y firma Tesoreria/Riesgo | Pendiente |
| G-03 | Obtener validacion externa de sobre digital/firma/certificados. | Validacion proveedor/camara | Alto | Docs seguridad/UAT | Seguridad | Contraparte externa | Evidencia oficial o waiver | Pendiente |
| G-04 | Cerrar naming externo por camara/flujo. | Validacion negocio/proveedor | Alto | Docs naming y pruebas si aplica | Compliance/Operaciones | Normativa/camara | Matriz firmada | Pendiente |
| G-05 | Agregar o validar autorizacion explicita en `AchResponsesController`. | Codigo bajo riesgo/Seguridad | Medio | `src/Cfa.ACHInterbank.Api/Controllers/AchResponsesController.cs`, tests | Tecnologia/Seguridad | Definir politica granular si aplica | `[Authorize]` general aplicado; prueba pasa | Corregida - pendiente validar |
| G-06 | Resolver mismatch interoperabilidad SPA/backend. | Codigo medio riesgo | Medio | SPA service o API controller/tests | Tecnologia/Seguridad | Decision funcional | Endpoint existe o pantalla deshabilitada/documentada | Parcial |
| G-07 | Parametrizar `environment.prod.ts` para no usar localhost. | Configuracion | Medio | `web/ach-interbank-ui/src/environments/*` o config runtime | Tecnologia/Operaciones | Estrategia deploy | Build prod consume URL aprobada o relativa | Corregida - pendiente build/deploy |
| G-08 | Sanear README raiz. | Documental | Bajo | `README.md` | Tecnologia | Aprobacion equipo | README apunta a docs actuales | Corregida |
| G-09 | Revisar `.env` versionado y rotar si hubo secreto real. | Seguridad/configuracion | Alto | `.env`, `.gitignore`, docs | Seguridad | Revision humana | Sin secretos reales versionados o riesgo aceptado | Parcial |
| G-10 | Remover credenciales por defecto de docs/compose o dejarlas como placeholders. | Configuracion/Seguridad | Medio | `docker-compose.yml`, `README.md`, `.env.example`, `.env.test.example` | Seguridad/Operaciones | Politica config | No hay valores sensibles utilizables | Corregida - pendiente validar |
| G-11 | Definir OpenBao para UAT o excepcion. | Configuracion/Seguridad | Medio | compose UAT/documentacion/scripts | Seguridad/Operaciones | Decision ambiente | Secret provider validado o waiver | Parcial - requiere decision |
| G-12 | Corregir ruta absoluta `DocumentationFile`. | Codigo bajo riesgo/config | Bajo | API csproj | Tecnologia | Politica build | Build local/CI sin ruta absoluta | Corregida - pendiente build |
| G-13 | Aprobar runbook UAT/preproductivo. | Documental/Operacion | Bajo | `docs/operations/RUNBOOK_UAT_Y_PREPRODUCTIVO.md` | Operaciones | Revision UAT | Runbook firmado | Parcial |
| G-14 | Documentar y ensayar backup/restore/rollback. | Operacion/Evidencia | Alto | Runbook, evidencia externa | Operaciones/Tecnologia | Ambiente UAT | Ensayo con evidencia | Pendiente |
| G-15 | Completar health checks o monitoreo alterno. | Codigo medio riesgo/Operacion | Medio | API health checks o docs monitoreo | Tecnologia/Operaciones | Alcance observabilidad | Checks o monitoreo aprobados | Pendiente |
| G-16 | Definir documentos current y archivar drift. | Documental | Bajo | README/docs/audits | Auditoria/Tecnologia | Decision comite | Fuente canonica publicada | Pendiente |
| G-17 | Adjuntar evidencia CI backend remota aprobada al paquete RC. | Evidencia/CI | Bajo | Docs readiness/paquete comite | Tecnologia/QA | GitHub Actions | dotnet-ci remoto OK adjunto | CI backend OK |
| G-18 | Adjuntar evidencia Angular CI remota/local aprobada al paquete RC. | Evidencia/CI | Bajo | Docs readiness/paquete comite | Tecnologia/QA | GitHub Actions/local | angular-ci de rama OK, `npm run build` OK y 147 specs OK adjuntos | CI Angular OK |
| G-19 | Resolver enrutamiento SPA->API en compose/UAT. | Configuracion/Operacion | Medio | `web/ach-interbank-ui/nginx.conf`, `web/ach-interbank-ui/src/environments/environment.prod.ts` | Tecnologia/DevOps/Operaciones | Decision de arquitectura de despliegue | `http://localhost:743/api/...` proxya a API; health/openapi/scalar por 743 no devuelven `index.html` | Corregida tecnicamente - UAT pendiente |
| G-20 | Revisar y actualizar dependencia vulnerable `System.Security.Cryptography.Xml`. | Seguridad/Codigo bajo riesgo | Medio | `src/Cfa.ACHInterbank.Application/Cfa.ACHInterbank.Application.csproj` | Seguridad/Tecnologia | Analisis advisory y compatibilidad .NET | `dotnet list ... --vulnerable` sin hallazgos y build/test OK | Corregida tecnicamente |
| G-21 | Mantener PostgreSQL publicado solo en loopback para UAT local o parametrizar puerto alterno. | Configuracion/Operacion | Bajo | `docker-compose.yml`, `.env.example`, docs | DevOps/Operaciones | Politica ambiente | `localhost:5432` OK en UAT local; no expuesto en productivo sin aprobacion | Corregida tecnicamente - no productivo |
| G-22 | Confirmar formalmente roles/claims del usuario demo tras cierre tecnico autenticado. | Prueba UAT/Evidencia | Medio | `docs/uat/EJECUCION_UAT_TECNICO_BASICO.md`, `docs/uat/MATRIZ_DEFECTOS_UAT.md` | Seguridad/Tecnologia/QA | UAT tecnico autenticado reejecutado con variables seguras | Login/JWT evidencia `Admin` y `ACH.Operator` sin imprimir token/password | Cerrado para UAT tecnico; mantener observaciones de evidencia visual |
| G-23 | Adjuntar evidencia visual sanitizada de navegacion SPA autenticada si el acta formal la exige. | Evidencia UAT | Bajo/Medio | `docs/uat/EVIDENCIAS_UAT_TECNICO_BASICO.md`, acta UAT | QA/DevOps | Browser local/manual o herramienta habilitada | Capturas sin datos sensibles de login post-redireccion, dashboard/menu y pantallas permitidas | Pendiente |
| G-24 | Reejecutar UAT funcional sintetico desde SPA Docker tras corregir proxy funcional. | Prueba UAT/Evidencia | Alto | `docs/uat/UAT_FUNCIONAL_SINTETICO.md`, `docs/uat/EVIDENCIAS_UAT_FUNCIONAL.md`, acta | QA/Operaciones/Tecnologia | Cierre G-25 | Transaccion sintetica y rutas funcionales reintentadas desde `http://localhost:743` con llamadas JSON, sin HTML fallback; falta evidencia visual y acta | Parcialmente OK |
| G-25 | Corregir proxy/ruteo de rutas funcionales raiz usadas por SPA. | Configuracion/Frontend runtime | Alto | `web/ach-interbank-ui/nginx.conf` | Tecnologia/DevOps | Inventario de endpoints SPA | `/financial-institutions`, `/ach-cycles`, `/clearing-houses`, `/transactions/company-entry-descriptions` y rutas transaccionales relacionadas devuelven JSON/401/403/404 controlado, nunca `index.html` | Corregida tecnicamente - validada HTTP |
| G-26 | Mantener evidencia de generacion de evento inicial de estado transaccional para nuevas transacciones. | QA/Auditoria | Bajo/Medio | Docs UAT/evidencia runtime | Tecnologia/QA/Auditoria | Cerrado con `UAT-SINT-TRACE-001` | `AchTransactionStateEvents` contiene evento inicial `Pending -> Pending` y trazabilidad API refleja lifecycle esperado en runtime | Cerrada funcionalmente |
| G-27 | Mantener contrato de idempotencia transaccional actual y decidir evolucion futura. | Arquitectura/API contract | Bajo/Medio | `docs/go-live-readiness/CONTRATO_IDEMPOTENCIA_TRANSACCIONES.md`, tests/docs API | Arquitectura/Tecnologia | Decision negocio/API para evolucion | Reintentos identicos tienen contrato documentado actual: 400 JSON controlado; 409, replay o `Idempotency-Key` quedan como decision evolutiva | Cerrada documentalmente |
| G-28 | Cerrar validacion formal NACHA-M 1/5/6/7/8/9 campo-a-campo. | Normativa/UAT/Evidencia | Alto | `docs/uat/UAT_NACHA_M_CAMPO_A_CAMPO.md`, `docs/go-live-readiness/MATRIZ_NACHA_M_ACH_COLOMBIA.md`, `docs/go-live-readiness/MATRIZ_NACHA_M_CENIT.md` | Compliance/Operaciones/Tecnologia | Normativa/camara, datos anonimizados y prerequisitos de prenotificacion/exportacion | Archivo controlado no vacio generado por sistema, hash/totales/block count validados y matriz firmada o waiver | Pendiente |
| G-29 | Definir asignacion de rol `ACH.Operator` para UAT. | Seguridad/Seed/Auth | Medio | `UserRoleConfiguration`, migracion `AddAdminOperatorRoleSeed`, `UserRoleSeedTests`, docs seguridad | Seguridad/Tecnologia/QA | Decision matriz rol-permiso | JWT/respuesta evidencia `ACH.Operator` para usuario demo aprobado y endpoints protegidos basicos responden 200 | Cerrado para UAT controlado |
| G-30 | Corregir respuesta de exportacion NACHA vacia. | Backend/API bajo-mediano riesgo | Alto | `NachaExportController`, `NachaSecurityOperationService`, tests de exportacion | Tecnologia/QA | Diagnostico DEF-UAT-021 | Si no hay archivo exportable, la API devuelve 422 JSON controlado con causa; si hay archivo, size > 0 y registros 1/5/6/8/9 | Cerrado tecnico |
| G-31 | Configurar `Proc_Contrapartidas` para UAT dry-run/mock. | DevOps/Integracion/Seguridad | Alto | `ProcContrapartidas:Mode`, `ContrapartidaDispatchJobService`, docs/evidencia | Integracion/DevOps/Seguridad | Politica de no transmision UAT/local | Job no intenta endpoint externo/productivo en UAT; dry-run queda auditado con `PROC_DRY_RUN` sin retry | Cerrado tecnico UAT/local |
| G-32 | Preparar prenotificacion UAT valida para NACHA-M no vacio. | UAT/Operaciones | Alto | Flujo de transaccion prenotificacion y ciclo futuro | Operaciones/QA/Arquitectura ACH | Default source CFA correcto, cuentas sinteticas y ciclo valido | Prenotificacion UAT registrada por API, plazo de 3 dias habiles cumplido sin backdating y transaccion posterior exportable | Pendiente |

## Priorizacion

1. Bloqueantes productivos: G-01, G-02, G-03, G-04, G-05, G-07, G-09, G-14.
2. Bloqueantes de UAT operativo: G-01, G-06, G-13, G-24 y ejecucion formal con datos anonimizados/evidencias.
3. Higiene documental/configuracion: G-08, G-10, G-11, G-12, G-15, G-16.

Estado especifico UAT tecnico autenticado: G-22 deja de estar bloqueada por variables y el rol `ACH.Operator` queda visible/asignado para `admin` tras cierre DEF-UAT-015. Persiste evidencia visual pendiente si el acta formal la exige, sin declarar GO productivo.
Estado especifico UAT funcional sintetico: G-24 queda parcialmente cubierto por API directa y reintento HTTP desde SPA Docker. G-25 queda corregida tecnicamente. G-26 queda cerrada funcionalmente para nuevas transacciones tras revalidacion con `UAT-SINT-TRACE-001`. G-27 queda cerrada documentalmente para el contrato actual; la decision tecnica/arquitectonica pendiente aplica solo a evolucion 409/`Idempotency-Key`/replay. G-28 queda abierto/bloqueado para cierre formal NACHA-M por falta de prenotificacion previa valida. G-30 queda cerrado tecnicamente con 422 controlado; G-31 queda cerrado tecnicamente para UAT/local con `DryRun`. G-32 queda pendiente antes de reintentar NACHA-M real UAT no vacio. G-29 queda cerrado para UAT controlado.

## Restriccion De Esta Fase

Esta actualizacion aplico solo cambios acotados de seguridad/configuracion/documentacion y pruebas de caracterizacion. No se tocaron reglas NACHA-M, CENIT, ROR, devoluciones, contabilidad ni conciliacion.

## Actualizacion 2026-05-19 - DEF-UAT-020

| Paso | Accion | Estado |
|---|---|---|
| 1 | Parametrizar reglas de prenotificacion por camara/naturaleza/tipo. | Completado tecnicamente. |
| 2 | Aplicar migracion `AddClearingHouseTransactionRules` en UAT/local controlado. | Pendiente runtime. |
| 3 | Validar seeds ACH Colombia/CENIT y banco origen default CFA/Cooperativa. | Pendiente runtime. |
| 4 | Crear prenotificaciones UAT validas sin bypass/backdating. | Pendiente. |
| 5 | Reintentar NACHA-M por ACH Colombia y CENIT. | Pendiente. |
| 6 | Validar registros 1/5/6/7/8/9, totales/hash/block count. | Pendiente. |
| 7 | Obtener homologacion/waiver/acta formal. | Pendiente. |

# Plan de Cierre de Brechas - ACH Interbank

Fecha de generacion/revalidacion: 2026-05-18 / 2026-05-19
Version: 0.5 preliminar
Rama analizada: `fix/spa-functional-root-routes-proxy`
Estado: plan de trabajo actualizado tras diagnostico de trazabilidad/idempotencia; incluye correcciones de bajo riesgo aplicadas y pendientes de validacion.

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
| G-18 | Adjuntar evidencia Angular CI remota aprobada al paquete RC. | Evidencia/CI | Bajo | Docs readiness/paquete comite | Tecnologia/QA | GitHub Actions | angular-ci remoto OK adjunto | CI Angular OK |
| G-19 | Resolver enrutamiento SPA->API en compose/UAT. | Configuracion/Operacion | Medio | `web/ach-interbank-ui/nginx.conf`, `web/ach-interbank-ui/src/environments/environment.prod.ts` | Tecnologia/DevOps/Operaciones | Decision de arquitectura de despliegue | `http://localhost:743/api/...` proxya a API; health/openapi/scalar por 743 no devuelven `index.html` | Corregida tecnicamente - UAT pendiente |
| G-20 | Revisar y actualizar dependencia vulnerable `System.Security.Cryptography.Xml`. | Seguridad/Codigo bajo riesgo | Medio | `.csproj` que referencia paquete transitivo/directo | Seguridad/Tecnologia | Analisis advisory y compatibilidad .NET | Build/test/CI OK con version segura | Nuevo - pendiente |
| G-21 | Mantener PostgreSQL publicado solo en loopback para UAT local o parametrizar puerto alterno. | Configuracion/Operacion | Bajo | `docker-compose.yml`, `.env.example`, docs | DevOps/Operaciones | Politica ambiente | `localhost:5432` OK en UAT local; no expuesto en productivo sin aprobacion | Corregida tecnicamente - no productivo |
| G-22 | Confirmar formalmente roles/claims del usuario demo tras cierre tecnico autenticado. | Prueba UAT/Evidencia | Medio | `docs/uat/EJECUCION_UAT_TECNICO_BASICO.md`, `docs/uat/MATRIZ_DEFECTOS_UAT.md` | Seguridad/Tecnologia/QA | UAT tecnico autenticado reejecutado con variables seguras | Decidir si `ACH.Operator` debe estar visible/asignado o si `Admin` cubre el alcance tecnico | OK tecnico con observaciones |
| G-23 | Adjuntar evidencia visual sanitizada de navegacion SPA autenticada si el acta formal la exige. | Evidencia UAT | Bajo/Medio | `docs/uat/EVIDENCIAS_UAT_TECNICO_BASICO.md`, acta UAT | QA/DevOps | Browser local/manual o herramienta habilitada | Capturas sin datos sensibles de login post-redireccion, dashboard/menu y pantallas permitidas | Pendiente |
| G-24 | Reejecutar UAT funcional sintetico desde SPA Docker tras corregir proxy funcional. | Prueba UAT/Evidencia | Alto | `docs/uat/UAT_FUNCIONAL_SINTETICO.md`, `docs/uat/EVIDENCIAS_UAT_FUNCIONAL.md`, acta | QA/Operaciones/Tecnologia | Cierre G-25 | Transaccion sintetica y rutas funcionales reintentadas desde `http://localhost:743` con llamadas JSON, sin HTML fallback; falta evidencia visual y acta | Parcialmente OK |
| G-25 | Corregir proxy/ruteo de rutas funcionales raiz usadas por SPA. | Configuracion/Frontend runtime | Alto | `web/ach-interbank-ui/nginx.conf` | Tecnologia/DevOps | Inventario de endpoints SPA | `/financial-institutions`, `/ach-cycles`, `/clearing-houses`, `/transactions/company-entry-descriptions` y rutas transaccionales relacionadas devuelven JSON/401/403/404 controlado, nunca `index.html` | Corregida tecnicamente - validada HTTP |
| G-26 | Mantener evidencia de generacion de evento inicial de estado transaccional para nuevas transacciones. | QA/Auditoria | Bajo/Medio | Docs UAT/evidencia runtime | Tecnologia/QA/Auditoria | Cerrado con `UAT-SINT-TRACE-001` | `AchTransactionStateEvents` contiene evento inicial `Pending -> Pending` y trazabilidad API refleja lifecycle esperado en runtime | Cerrada funcionalmente |
| G-27 | Formalizar contrato de idempotencia transaccional. | Arquitectura/API contract | Medio | Backend transacciones/tests/docs API | Arquitectura/Tecnologia | Decision negocio/API | Reintentos identicos tienen contrato firmado: 409, replay idempotente o 400 documentado; pruebas automatizadas y UAT actualizados | Pendiente decision |

## Priorizacion

1. Bloqueantes productivos: G-01, G-02, G-03, G-04, G-05, G-07, G-09, G-14.
2. Bloqueantes de UAT operativo: G-01, G-06, G-13, G-24 y ejecucion formal con datos anonimizados/evidencias.
3. Higiene documental/configuracion: G-08, G-10, G-11, G-12, G-15, G-16.

Estado especifico UAT tecnico autenticado: G-22 deja de estar bloqueada por variables. Quedan observaciones de roles (`ACH.Operator`) y evidencia visual, sin declarar GO productivo.
Estado especifico UAT funcional sintetico: G-24 queda parcialmente cubierto por API directa y reintento HTTP desde SPA Docker. G-25 queda corregida tecnicamente. G-26 queda cerrada funcionalmente para nuevas transacciones tras revalidacion con `UAT-SINT-TRACE-001`. G-27 requiere decision tecnica/arquitectonica antes de UAT formal.

## Restriccion De Esta Fase

Esta actualizacion aplico solo cambios acotados de seguridad/configuracion/documentacion y pruebas de caracterizacion. No se tocaron reglas NACHA-M, CENIT, ROR, devoluciones, contabilidad ni conciliacion.

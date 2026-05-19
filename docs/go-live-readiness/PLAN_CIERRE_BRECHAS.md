# Plan de Cierre de Brechas - ACH Interbank

Fecha de generacion: 2026-05-18  
Version: 0.1 preliminar  
Rama analizada: `ACH-Interbank-Postgresql`  
Estado: plan de trabajo actualizado; incluye correcciones de bajo riesgo aplicadas y pendientes de validacion.

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
| G-19 | Resolver enrutamiento SPA->API en compose/UAT. | Configuracion/Operacion | Medio | `web/ach-interbank-ui/nginx.conf`, `docker-compose.yml` o reverse proxy UAT | Tecnologia/DevOps/Operaciones | Decision de arquitectura de despliegue | `http://localhost:743/api/...` proxya a API o se define dominio/API base aprobada | Nuevo - pendiente |
| G-20 | Revisar y actualizar dependencia vulnerable `System.Security.Cryptography.Xml`. | Seguridad/Codigo bajo riesgo | Medio | `.csproj` que referencia paquete transitivo/directo | Seguridad/Tecnologia | Analisis advisory y compatibilidad .NET | Build/test/CI OK con version segura | Nuevo - pendiente |

## Priorizacion

1. Bloqueantes productivos: G-01, G-02, G-03, G-04, G-05, G-07, G-09, G-14.
2. Bloqueantes de UAT operativo: G-01, G-06, G-13, G-19.
3. Higiene documental/configuracion: G-08, G-10, G-11, G-12, G-15, G-16.

## Restriccion De Esta Fase

Esta actualizacion aplico solo cambios acotados de seguridad/configuracion/documentacion y pruebas de caracterizacion. No se tocaron reglas NACHA-M, CENIT, ROR, devoluciones, contabilidad ni conciliacion.

# Indice de Evidencias UAT - ACH Interbank

Fecha de generacion: 2026-05-18
Version: 0.1 preliminar
Rama analizada: `ACH-Interbank-Postgresql`
Estado: plantilla operativa; requiere validacion humana.

## Politica De Evidencia

Las evidencias con datos sensibles deben almacenarse fuera de Git en un repositorio documental seguro. En este archivo solo deben registrarse referencias, hashes SHA256, responsables y estado. No incluir PII, cuentas reales, tokens, passwords, llaves privadas, PFX ni certificados privados.

## Matriz

| ID evidencia | Escenario | Tipo | Descripcion | Ruta o referencia segura | Hash SHA256 | Responsable | Fecha | Estado | Observacion |
|---|---|---|---|---|---|---|---|---|---|
| EV-UAT-001 | UAT-REAL-001 | Captura SPA | Login y menu segun rol | PENDIENTE | PENDIENTE | Tecnologia | PENDIENTE | PENDIENTE | Sin datos sensibles |
| EV-UAT-002 | UAT-REAL-002 | Request/response | Validacion de permisos | PENDIENTE | PENDIENTE | Tecnologia | PENDIENTE | PENDIENTE | Redactar token |
| EV-UAT-003 | UAT-REAL-005 | Archivo NACHA-M | Archivo generado/cargado controlado | PENDIENTE | PENDIENTE | Operaciones | PENDIENTE | PENDIENTE | No versionar archivo real |
| EV-UAT-004 | UAT-REAL-006 | Registro BD | Persistencia registros 1/5/6/7/8/9 | PENDIENTE | PENDIENTE | Tecnologia | PENDIENTE | PENDIENTE | Extracto anonimizado |
| EV-UAT-005 | UAT-REAL-009 | Archivo de respuesta | Devolucion salida | PENDIENTE | PENDIENTE | Operaciones | PENDIENTE | PENDIENTE | |
| EV-UAT-006 | UAT-REAL-010 | Evento de auditoria | Devolucion entrada aplicada E2E | PENDIENTE | PENDIENTE | Auditoria | PENDIENTE | PENDIENTE | |
| EV-UAT-007 | UAT-REAL-011 | Evidencia de trazabilidad | Devolucion huerfana/no resuelta | PENDIENTE | PENDIENTE | Operaciones | PENDIENTE | PENDIENTE | |
| EV-UAT-008 | UAT-REAL-014 | Archivo NACHA-M | ROR generado | PENDIENTE | PENDIENTE | Operaciones | PENDIENTE | PENDIENTE | |
| EV-UAT-009 | UAT-REAL-017 | Evidencia conciliacion | Neteo CENIT | PENDIENTE | PENDIENTE | Operaciones/Tesoreria | PENDIENTE | PENDIENTE | |
| EV-UAT-010 | UAT-REAL-019 | Evidencia CUD | Soporte CUD o referencia operacional | Repositorio seguro externo | PENDIENTE | Tesoreria | PENDIENTE | PENDIENTE | No subir soporte real a Git |
| EV-UAT-011 | UAT-REAL-020 | Evidencia de conciliacion | Reporte conciliacion | PENDIENTE | PENDIENTE | Operaciones | PENDIENTE | PENDIENTE | |
| EV-UAT-012 | UAT-REAL-022 | Evidencia sobre digital | Cifrado/descifrado/fail-close | PENDIENTE | PENDIENTE | Seguridad | PENDIENTE | PENDIENTE | No incluir llaves |
| EV-UAT-013 | UAT-REAL-024 | Evidencia certificado/firma | Validacion certificado/firma | PENDIENTE | PENDIENTE | Seguridad | PENDIENTE | PENDIENTE | Certificado privado fuera de Git |
| EV-UAT-014 | UAT-REAL-025 | Evidencia mecanismo corporativo de secretos/secreto | SecretRef enmascarado y resolucion | PENDIENTE | PENDIENTE | Seguridad | PENDIENTE | PENDIENTE | No incluir token |
| EV-UAT-015 | UAT-REAL-027 | Evidencia de idempotencia | Reejecucion sin duplicidad | PENDIENTE | PENDIENTE | Tecnologia | PENDIENTE | PENDIENTE | |
| EV-UAT-016 | UAT-REAL-028 | Resultado dotnet test | Test backend no destructivo | PENDIENTE | PENDIENTE | Tecnologia | PENDIENTE | PENDIENTE | |
| EV-UAT-017 | UAT-REAL-028 | Resultado npm run build | Build SPA | PENDIENTE | PENDIENTE | Tecnologia | PENDIENTE | PENDIENTE | |
| EV-UAT-018 | UAT-REAL-032 | Resultado health checks | `/health/live` y `/health/ready` | PENDIENTE | PENDIENTE | Tecnologia | PENDIENTE | PENDIENTE | |
| EV-UAT-019 | UAT-REAL-033 | Captura SPA | Validacion SPA contra API | PENDIENTE | PENDIENTE | QA | PENDIENTE | PENDIENTE | |
| EV-UAT-020 | ACTA | Acta | Acta UAT firmada | PENDIENTE | PENDIENTE | Auditoria | PENDIENTE | PENDIENTE | |

## Tipos Permitidos

- Captura SPA.
- Request/response.
- Log backend.
- Registro BD.
- Archivo NACHA-M.
- Archivo de respuesta.
- Evento de auditoria.
- Evidencia de idempotencia.
- Evidencia de conciliacion.
- Evidencia CUD.
- Evidencia certificado/firma.
- Evidencia sobre digital.
- Evidencia mecanismo corporativo de secretos/secreto.
- Acta.
- Aprobacion por correo.
- Defecto.
- Reejecucion.
- Resultado dotnet test.
- Resultado npm run build.
- Resultado docker compose up.
- Resultado health checks.


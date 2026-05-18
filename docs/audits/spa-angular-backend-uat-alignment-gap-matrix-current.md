# Matriz de brechas SPA Angular ↔ Backend ↔ Normativa ↔ UAT — Estado vigente

## 1. Propósito
Documentar el estado de alineación del SPA Angular contra backend, normativa, matrices S1 y UAT punto 12.

Aclaraciones:
- No cambia estados GO/NO-GO.
- No declara SPA listo total.
- No habilita producción.
- Sirve como compuerta previa a 12D.

## 2. Resumen ejecutivo
- SPA alineado con backend: **Parcial**.
- SPA alineado con normativa: **Parcial**.
- SPA listo para UAT no técnica: **Parcial**.
- 12D puede iniciar: **Sí con restricciones**.
- GO productivo: **NO**.
- NO-GO productivo vigente.

## 3. Inventario SPA auditado
- Ubicación SPA: `web/ach-interbank-ui`.
- Angular aproximado: 21.x.
- Rutas relevantes: `dashboard`, `transactions`, `reports`, `cenit`, `nacha-security`, `incoming-nacha-command-center`, `audit-logs`, `auth-logs`.
- Servicios relevantes: reportes, CENIT, certificados, devoluciones ACH/ROR, transacciones, auditoría.

## 4. Matriz endpoints SPA ↔ backend

| Funcionalidad | SPA | Backend | Estado | Brecha | Riesgo | Recomendación |
|---|---|---|---|---|---|---|
| Reportes transaccionales | `reports` | `GET /api/reports/transactions/sent`, `GET /api/reports/transactions/received` | Implementado | Menor | Bajo | Mantener cobertura de contrato |
| Devoluciones/rechazos | `reports`, `transactions` | `GET /api/reports/returns`, `GET /api/reports/rejections`, `ach-returns/*` | Implementado | Menor | Bajo | Mantener pruebas de regresión |
| NACHA files | `reports` | `GET /api/reports/nacha-files` | Implementado | Menor | Bajo | Mantener |
| Ciclos | `reports`, `cenit` | `GET /api/reports/cycles`, `GET /api/cenit/queues` | Implementado | Menor | Bajo | Mantener |
| Conciliación | `reports` | `GET /api/reports/reconciliation` | Implementado | Semántica operativa | Medio | Reforzar mensaje no-contabilidad |
| Auditoría/histórico | `reports`, `audit-logs` | `GET /api/reports/audit`, `GET /api/reports/history` | Implementado | Menor | Bajo | Mantener |
| Accounting-review export | Expuesto en SPA (reportes) | `POST /api/reports/accounting-review/export` | Parcial | Implementado en UI; pendiente validación UAT operativa | P1 | Mantener frontera no-contable y validar con evidencias humanas |
| Trazabilidad ACH | Parcial en reportes/PDF | `/api/ach-traceability/*` | Parcial | Uso directo parcial/no confirmado | P1 | Confirmar consumo directo o documentar restricción |
| Certificate management | `nacha-security` | `nacha-security/certificates/management/*` | Implementado | Menor | Bajo | Mantener |
| Digital envelope certificates | `nacha-security` | `nacha-security/certificates/*` | Implementado | Menor | Bajo | Mantener masking |
| CENIT netting/liquidity | `cenit` | `/api/cenit/net-positions`, `/api/cenit/optimization-decisions` | Implementado | Riesgo semántico CUD | P2 | Etiquetado explícito de frontera CUD |
| CUD evidence boundary | Parcial en operación/reportes | Sin API CUD bancaria directa | Parcial | Flujo UI integral no encontrado | P1 | Operar como evidencia manual/documental |
| Naming externo | Parcial/manual | Matriz y validación por flujo/cámara | Parcial | No flujo UAT explícito en SPA | P1 | Checklist operativo + validación manual |
| UAT package/downloads | No encontrado | No aplica | No encontrado | No módulo UAT integral | P1 | Mantener ejecución 12B/12C fuera de SPA |

## 5. Matriz de casos UAT 12B vs SPA

| Caso UAT | Dominio | Ejecutable desde SPA | Evidencia descargable | Brecha | Restricción para 12D | Recomendación |
|---|---|---|---|---|---|---|
| Validar neteo por ciclo/participante/reproceso | S1-10 | Ejecutable parcialmente | Parcial (reportes) | No flujo UAT guiado integral | Ejecutar con checklist 12B | Guiar ruta exacta en sesión 12D |
| Liquidez simulada vs CUD real, evidencia CUD | S1-11 | Ejecutable parcialmente | Parcial | Sin módulo de evidencia/aprobación | Validación CUD manual obligatoria | Instrucción operativa explícita |
| Naming externo ACH/CENIT/ROR | S1-12 | Parcial/manual | Parcial | Validación naming no explícita en SPA | Validar contra matrices/checklists | Checklist naming obligatorio |
| Sobre digital/firma/certificados | S1-13 | Ejecutable parcialmente | Sí, según operación | Cierre humano fuera de SPA | Acta/evidencia manual obligatoria | Mantener compuertas 11D |
| Runbook/checklist/acta/defectos | S1-20 | Solo documental/manual | No desde SPA | No módulo UAT integral | Excel/PDF + acta obligatoria | Operar con paquete 12B/12C |

## 6. Riesgos semánticos
- Liquidez simulada no equivale a saldo real CUD.
- DXX-LIQ no equivale a rechazo oficial CUD.
- Accounting-review no equivale a contabilidad/asiento.
- Evidencia CUD no equivale a API CUD bancaria.
- UAT asistida no equivale a aprobación humana.
- SPA no debe declarar GO productivo.

## 7. Seguridad y datos sensibles
- `secretRefMasked` permitido como metadata enmascarada.
- No exponer `SecretRef` crudo.
- No exponer PFX/password/private key.
- Revisar token handling de frontend.
- `environment.prod.ts` con IP fija clasificado como P2.
- No mostrar saldos CUD reales sin autorización.

## 8. Roles y permisos

| Función | Rol UAT requerido | Rol SPA encontrado | Estado | Brecha | Recomendación |
|---|---|---|---|---|---|
| Operaciones | Operaciones ACH/CENIT | `Admin`, `ACH.Operator` + permisos | Parcial | Granularidad de negocio limitada | Mapear rol operativo explícito |
| Tesorería | Tesorería (S1-10/S1-11) | No explícito por dominio | Parcial | Falta separación fina | Definir permiso/rol por dominio |
| Seguridad | Seguridad (S1-13) | Parcial vía permisos NACHA security | Parcial | Falta mapeo formal con UAT | Definir matriz rol-permiso |
| Riesgo/Compliance | Riesgo/Compliance | No explícito por rol | Parcial | Cobertura no evidente en SPA | Definir perfil lectura/aprobación |
| Tecnología/QA | Tecnología/QA UAT | No explícito por rol UAT | Parcial | Falta perfil UAT transversal | Definir permisos de soporte UAT |
| Administrador | Administración global | `Admin` | Implementado | Menor | Mantener |

## 9. Brechas P0/P1/P2/P3

### P0
No encontradas en 13A. Condiciones que serían P0 si aparecen:
- SPA sugiere o permite GO productivo sin soporte formal.
- SPA trata liquidez simulada como saldo real CUD.
- SPA expone secretos/PFX/password/private key.
- SPA presenta accounting-review como asiento contable real.

### P1
- Falta módulo UAT integral (evidencias, defectos, aprobadores, scorecard UAT).
- Accounting-review export expuesto en SPA; pendiente validación UAT con usuarios y evidencias.
- Roles UAT finos no evidentes.
- Trazabilidad directa parcial/no confirmada.
- CUD evidence boundary sin flujo UI integral.

### P2
- Semántica CUD/liquidez reforzada en SPA; pendiente validación UAT operativa con usuarios.
- `environment.prod.ts` con IP fija.
- Falta ayuda contextual y links a guías 12B.

### P3
- Mejoras UX no bloqueantes.

## 10. Restricciones para iniciar 12D
12D puede iniciar solo si:
- se entrega PDF/Excel 12B/12C;
- se explica que SPA no cubre todo;
- evidencias/defectos/aprobadores se manejan en Excel/documental;
- CUD se valida como evidencia operacional/manual;
- GO productivo sigue NO;
- usuarios conocen rutas/pantallas disponibles;
- se registra toda brecha como defecto o restricción UAT.

## 11. Matriz readiness SPA para UAT

| Dominio | Backend listo | Docs/UAT listo | SPA listo | Estado | Riesgo | Restricción 12D | Recomendación |
|---|---|---|---|---|---|---|---|
| S1-10 Neteo CENIT | Sí | Sí | Parcial | Parcial | Medio | Validación guiada/manual parcial | Checklist por ciclo/participante |
| S1-11 Liquidez/CUD | Sí (boundary) | Sí | Parcial | Parcial | Alto | CUD manual/documental obligatorio | Reforzar semántica UI |
| S1-12 Naming externo | Parcial | Sí | Parcial | Parcial | Medio | Validación manual obligatoria | Flujo UAT de naming |
| S1-13 Sobre digital | Sí | Sí | Parcial-alto | Parcial | Medio | Cierre humano fuera de SPA | Mantener compuertas evidencia |
| S1-15 Reportes/auditoría | Sí | Sí | Sí | Implementado | Bajo | Ninguna crítica | Mantener |
| S1-20 UAT/runbooks/evidencia | Sí (docs) | Sí | No encontrado | No listo en SPA | Alto | Ejecutar 12B/12C documental | Evaluar módulo UAT |
| Accounting-review/no-contabilidad | Sí | Sí | Parcial | Parcial | Medio | Export no confirmado en SPA | Exponer endpoint en UI |
| Certificados/gobernanza | Sí | Sí | Sí | Implementado | Bajo | Mantener masking | Mantener |
| Trazabilidad | Sí | Sí | Parcial | Parcial | Medio | Uso directo parcial/no confirmado | Confirmar consumo y evidencia |
| Devoluciones/rechazos/ROR | Sí | Sí | Parcial-alto | Parcial | Medio | Validación UAT combinada | Mantener checklist operativo |

## 12. Plan de cierre
Commits propuestos a futuro (no implementados en este documento):
- `test(spa): add Angular contract/readiness characterization tests`
- `feat(spa): expose accounting-review export in UI`
- `feat(spa): add UAT evidence and defect tracking screens`
- `feat(spa): align CENIT liquidity labels with CUD boundary`
- `feat(spa): add UAT guide links in operational screens`
- `docs(uat): update operator guide with SPA execution paths`

Estado de cobertura documental:
- `docs(uat): update operator guide with SPA execution paths` → **cubierto** (guía operativa actualizada para ejecución híbrida SPA + Excel/PDF/manual).
- Esta cobertura **no cambia** readiness global SPA ni estado GO/NO-GO productivo.

## 13. Veredicto
- SPA readiness: **Parcial**.
- 12D: **Sí con restricciones**.
- 12E: **No debe ejecutarse como UAT 100% SPA-only**.
- GO UAT formal: **pendiente**.
- GO productivo: **NO**.
- NO-GO productivo vigente.

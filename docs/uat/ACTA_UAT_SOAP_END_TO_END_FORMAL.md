# Acta UAT SOAP End-to-End - ACH Interbank

## 1. Identificación

- Proyecto: ACH Interbank.
- Fecha/hora: 2026-05-23.
- Rama/commit: ver `docs/uat/evidencias/soap-end-to-end-final/precheck_runtime.md`.
- Ambiente: Docker/UAT/local.
- Ejecutor: QA UAT / auditor funcional asistido por Codex.
- API/runtime: `http://localhost:743`.
- Base de datos: PostgreSQL UAT/local en Docker.
- Decisión: Continuar UAT controlado. Productivo NO-GO.

## 2. Objetivo

Validar técnicamente los flujos SOAP end-to-end en UAT/local, con readiness, mapping trace, envelope/payload, no transmisión externa y guardrails no monetarios.

## 3. Alcance

- `Proc_Contrapartidas`.
- `Proc_Transacciones`.
- `RegistrarRespuestaTransaccion`.
- Prenotificación aprobada.
- Prenotificación rechazada.
- SPA `/integraciones/mappings`.
- Sanitización.
- No transmisión externa.

## 4. Fuera de alcance

- Producción.
- Transmisión real.
- Homologación externa con proveedores/cámaras.
- Uso de datos reales.
- Certificados productivos.
- Sobre digital productivo.
- CENIT/CUD productivo.
- Aprobación bancaria formal.

## 5. Ambiente

- Docker/UAT/local.
- API ACH Interbank.
- PostgreSQL.
- SPA ACH Interbank.
- Modos SOAP: DryRun/Disabled/Mock para UAT/local; Live no habilitado por esta acta.

## 6. Resumen ejecutivo

Los flujos SOAP end-to-end cuentan con evidencia técnica consolidada para UAT/local. `Proc_Contrapartidas` y `Proc_Transacciones` mantienen naturaleza monetaria con guardrails de readiness y DryRun. `RegistrarRespuestaTransaccion` mantiene naturaleza no monetaria y registra respuesta, causal, estado y trazabilidad sin mover dinero ni afectar saldos. La SPA de mappings está alineada a los catálogos controlados y no habilita SQL libre ni selección arbitraria de tablas.

## 7. Matriz de escenarios

La matriz formal se encuentra en:

- `docs/uat/MATRIZ_ESCENARIOS_UAT_SOAP_END_TO_END_FINAL.md`.

## 8. Resultados por operación

### Proc_Contrapartidas

- IntegrationKey: `WSCFAACH`.
- OperationKey: `Proc_Contrapartidas`.
- MappingPurpose: `MonetaryDebitRequest`.
- MappingDirection: `OutboundRequest`.
- Naturaleza: débito monetario originado por CFA.
- MovesMoney: true.
- Resultado: cerrado técnico UAT; sin fallback requerido; no transmisión externa.

### Proc_Transacciones

- IntegrationKey: `WSCFAACH`.
- OperationKey: `Proc_Transacciones`.
- MappingPurpose: `MonetaryCreditRequest`.
- MappingDirection: `OutboundRequest`.
- Naturaleza: crédito monetario originado por otra entidad financiera, CFA receptora/procesadora.
- MovesMoney: true.
- Resultado: cerrado técnico UAT; usa NACHA-M desagregado; SOAP Envelope DryRun sanitizado disponible; no transmisión externa.

### RegistrarRespuestaTransaccion

- IntegrationKey: `WSAXON`.
- OperationKey: `RegistrarRespuestaTransaccion`.
- MappingPurpose: `DifferentialResponseNotification`.
- MappingDirection: `InboundResponse`.
- Naturaleza: respuesta diferencial no monetaria.
- MovesMoney: false.
- Resultado: cerrado técnico UAT; aprueba/rechaza prenotificaciones CFA pendientes; no mueve dinero; no afecta saldos; no invoca WSCFAACH.

## 9. Evidencias anexas

- Inventario consolidado: `docs/uat/evidencias/soap-end-to-end-final/EVIDENCE_INVENTORY.md`.
- Inventario JSON: `docs/uat/evidencias/soap-end-to-end-final/evidence_inventory.json`.
- Hashes: `docs/uat/evidencias/soap-end-to-end-final/hashes/evidence_hashes.sha256`.
- Reporte no transmisión externa: `docs/uat/evidencias/soap-end-to-end-final/no_external_transmission_report.md`.
- Reporte sanitización: `docs/uat/evidencias/soap-end-to-end-final/security_sanitization_report.md`.

## 10. Defectos cerrados

- `DEF-UAT-SOAP-MAP-001`: cerrado técnico.
- `DEF-UAT-SOAP-MAP-002`: cerrado técnico.
- `DEF-UAT-SOAP-MAP-003`: cerrado técnico.
- `DEF-UAT-SOAP-MAP-004`: cerrado técnico UAT.
- `DEF-UAT-SOAP-MAP-005`: cerrado técnico.

## 11. Defectos abiertos o riesgos remanentes

No se identifican defectos técnicos bloqueantes nuevos para continuar UAT controlado. Permanecen riesgos no técnicos/productivos:

- Homologación externa.
- Certificados/sobre digital productivo.
- CENIT/CUD productivo si aplica.
- Backup/restore/rollback.
- UAT bancario formal.
- Aprobaciones formales.
- Seguridad operativa productiva.

## 12. Decisión

- Continuar UAT controlado.
- Productivo: **NO-GO**.

## 13. Firmas

| Rol | Nombre | Firma | Fecha |
|---|---|---|---|
| Tecnología |  |  |  |
| Operaciones |  |  |  |
| Seguridad |  |  |  |
| Auditoría |  |  |  |
| Negocio |  |  |  |

## 14. Anexos

Ver inventario consolidado de evidencias y matriz de escenarios citados en esta acta.

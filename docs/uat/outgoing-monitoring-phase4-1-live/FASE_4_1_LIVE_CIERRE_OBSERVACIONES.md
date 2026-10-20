# Fase 4.1 LIVE — cierre de observaciones

## Resultado

**CERRADA — OBSERVACIONES RESUELTAS.** El filtro de ciclo usa el catálogo real y el flujo de `Proc_Contrapartidas` fue comprobado por HTTP/SOAP real contra el WCF local, con persistencia, API y SPA Docker en SQL Server y PostgreSQL.

## Línea base

- Rama: `ACH-Interbank-Postgresql`.
- Commit base: `eb15a5938f82e927bbbe43f9acf86b6bb59cb640`.
- API/SPA Docker SQL Server: puertos 843/743.
- API/SPA Docker PostgreSQL aislado: puertos 844/744; PostgreSQL 5433.
- WCF local: `WSCFAACH.svc` en el puerto 7083.

## Cambios

- `Ciclo` se convirtió en un `mat-select` alimentado por `GET /api/ach-cycles`.
- La cámara seleccionada limita el catálogo y limpia ciclos incompatibles.
- El bootstrap repara únicamente mappings publicados por seed e incompatibles; mappings publicados por usuarios se preservan.
- El bootstrap SOAP incluye idempotentemente `Proc_Contrapartidas` en instalaciones nuevas.
- La suite `@ProcContrapartidasLive` requiere `RUN_PROC_CONTRAPARTIDAS_LIVE_TESTS=true` y ejecuta creación, despacho, error de transporte, reintento y verificación del monitor.

## Veredicto operativo

El WCF comprobado es una implementación local de desarrollo: recibe SOAP real y emite una respuesta real, pero sus llamadas descendentes al core están deshabilitadas. No se transmitieron archivos, no se usaron datos productivos y no se afectaron saldos. El estado Productivo continúa siendo **NO-GO**; este cierre aplica exclusivamente al monitor y al ambiente local controlado.

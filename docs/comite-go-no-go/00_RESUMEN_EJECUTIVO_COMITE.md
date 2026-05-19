# Resumen Ejecutivo Comite GO/NO-GO - ACH Interbank

Fecha: 2026-05-19
Estado recomendado: Continuar UAT controlado / NO-GO productivo
Scorecard vigente: 67.6 / 100

## Estado General

ACH Interbank se encuentra tecnicamente estabilizado para continuar pruebas controladas, pero no cuenta aun con evidencia suficiente para salida productiva. La plataforma tiene CI, runtime Docker, PostgreSQL, SPA, API, proxy, autenticacion y endpoints protegidos validados, y el UAT tecnico autenticado basico esta en estado OK con observaciones.

El UAT funcional sintetico avanzo y permanece parcialmente OK. Se cerraron defectos relevantes sobre proxy funcional, trazabilidad inicial e idempotencia documentada, pero persisten brechas de negocio, interoperabilidad externa, seguridad, operacion y aprobaciones formales.

## Que Se Logro

- dotnet-ci OK.
- angular-ci OK.
- Backend local OK.
- Angular local OK.
- Runtime Docker OK.
- PostgreSQL OK.
- SPA Docker OK.
- Proxy SPA -> API OK.
- Auth/login OK.
- Navigation/menu OK.
- Endpoints protegidos read-only OK.
- UAT tecnico autenticado basico OK con observaciones.
- UAT funcional sintetico parcialmente OK.
- DEF-UAT-011, DEF-UAT-012, DEF-UAT-016, DEF-UAT-017, DEF-UAT-018 y DEF-UAT-019 cerrados segun evidencia disponible.
- NU1903 corregido tecnicamente.

## Que Esta Validado

- La SPA Docker enruta correctamente al backend para rutas API y rutas funcionales raiz confirmadas.
- El login demo autorizado funciona sin documentar secretos.
- El token permite acceder a menu y endpoints protegidos.
- La nueva transaccion sintetica de trazabilidad genero evento inicial para nuevas transacciones.
- El duplicado sintetico fue rechazado de forma controlada, sin duplicar transaccion ni evento inicial.
- El contrato actual de deduplicacion/idempotencia fue documentado sin cambiar comportamiento productivo.

## Que Falta

- UAT funcional formal con actas.
- Evidencia visual y operativa completa.
- Correccion o decision formal sobre rol ACH.Operator no visible/asignado al usuario demo.
- Validacion NACHA-M campo-a-campo para registros 1/5/6/7/8/9 y homologacion o waiver.
- CENIT/CUD.
- Sobre digital, firma y certificados.
- OpenBao/secrets segun alcance.
- Backup, restore y rollback.
- Actas, aprobaciones formales y UAT bancario formal.

## Riesgos Principales

- Riesgo funcional por UAT sintetico aun parcial.
- Riesgo de interoperabilidad por NACHA-M, CENIT/CUD y homologacion pendientes.
- Riesgo de seguridad por secretos/certificados/OpenBao pendientes segun alcance.
- Riesgo operativo por backup/restore/rollback pendiente.
- Riesgo de auditoria por actas y evidencia visual/operativa pendientes.

## Decision Recomendada

Se recomienda continuar con UAT controlado. No se recomienda salida productiva en este momento.

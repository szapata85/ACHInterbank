# Resumen Ejecutivo Comite GO/NO-GO - ACH Interbank

Fecha de actualizacion: 2026-06-12
Estado recomendado: Continuar UAT controlado / NO-GO productivo
Scorecard vigente: 68.1 / 100

## Estado General

ACH Interbank se encuentra tecnicamente estabilizado para continuar pruebas controladas, pero no cuenta aun con evidencia suficiente para salida productiva. La plataforma tiene CI, runtime Docker, PostgreSQL, SPA, API, proxy, autenticacion y endpoints protegidos validados.

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
- DEF-UAT-015 cerrado para UAT controlado: `admin` evidencia `Admin` y `ACH.Operator`.
- DEF-UAT-016 cerrado.
- DEF-UAT-017 cerrado funcionalmente.
- DEF-UAT-018 cerrado documentalmente.
- NU1903 corregido.
- BatchResolver corregido.
- G3.5 naming dinamico inbound/outbound cerrado tecnicamente.
- G3.5.1 cleanup OpenBao/HashiCorp Vault cerrado tecnicamente; KeyVault separado.
- G3.5.2 migraciones deshabilitadas por defecto y escaneo residual cerrado.
- G3.6A inbound real con PostgreSQL/Quartz hasta `Proc_Transacciones` dry-run: 2/2.
- G3.6B outbound real hasta `Proc_Contrapartidas` dry-run: 2/2.

## Que Falta

- UAT funcional formal con actas.
- Evidencia visual y operativa completa.
- DEF-UAT-020: NACHA-M 1/5/6/7/8/9 pendiente de validacion campo-a-campo y homologacion/waiver.
- Homologacion externa/campo-a-campo firmada de NACHA-M.
- Endpoint SOAP UAT autorizado y ejecucion real controlada, si el alcance futuro lo exige.
- Causalidad NachaExport -> Proc_Contrapartidas; G3.6B solo demuestra correlacion por `AchCycleId`.
- CENIT/CUD.
- Sobre digital, firma y certificados.
- Custodia corporativa de secretos según alcance.
- Backup, restore y rollback.
- UAT bancario formal.

## Riesgos Principales

- Riesgo funcional por UAT sintetico aun parcial.
- Riesgo de interoperabilidad por NACHA-M, CENIT/CUD y homologacion pendientes.
- Riesgo de seguridad por custodia de secretos y certificados pendiente según alcance.
- Riesgo operativo por backup/restore/rollback pendiente.
- Riesgo de auditoria por actas y evidencia visual/operativa pendientes.

## Decision Recomendada

Se recomienda continuar con UAT controlado. Los cierres G3.5-G3.6 son tecnicos y no constituyen homologacion externa, movimiento monetario ni autorizacion productiva.

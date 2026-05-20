# Resumen Ejecutivo Comite GO/NO-GO - ACH Interbank

Fecha: 2026-05-19
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

## Que Falta

- UAT funcional formal con actas.
- Evidencia visual y operativa completa.
- DEF-UAT-020: NACHA-M 1/5/6/7/8/9 pendiente de validacion campo-a-campo y homologacion/waiver.
- UAT integrado NACHA-M ACH Colombia/CENIT bloqueado por prenotificacion previa ausente; DEF-UAT-021 ya evita archivo 0 bytes como falso exito.
- Proc_Contrapartidas cuenta con guardrail `DryRun` UAT/local validado; endpoint UAT/mock real sigue pendiente para homologacion.
- CENIT/CUD.
- Sobre digital, firma y certificados.
- OpenBao/secrets segun alcance.
- Backup, restore y rollback.
- UAT bancario formal.

## Riesgos Principales

- Riesgo funcional por UAT sintetico aun parcial.
- Riesgo de interoperabilidad por NACHA-M, CENIT/CUD y homologacion pendientes.
- Riesgo de seguridad por secretos/certificados/OpenBao pendientes segun alcance.
- Riesgo operativo por backup/restore/rollback pendiente.
- Riesgo de auditoria por actas y evidencia visual/operativa pendientes.

## Decision Recomendada

Se recomienda continuar con UAT controlado. No se recomienda salida productiva en este momento.

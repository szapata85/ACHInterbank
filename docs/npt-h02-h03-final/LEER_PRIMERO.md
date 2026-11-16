# NPT_H02_H03_FINAL — LEER PRIMERO (16-11-2026)

**HOTFIX SOLO H-02/H-03. NO-GO SOAP Live.** Fuente: `ACHInterbank_HOTFIX_XSD_TRANSPORTE_NCTAORIG_H010203_2026-11-16.zip`.

Regla definitiva: **`Transactions.SourceAccountNumber → Proc_Transacciones.NCTAORIG`**, sin cuenta receptora, constantes ni fallbacks. Se retiró exclusivamente el bloqueo de *homologación de procedencia* (`PROC_TRANSACCIONES_NCTAORIG_CONTRACT_NOT_HOMOLOGATED` y guard equivalente), no los controles de origen válido, XML, permisos, configuración, idempotencia ni F5 ACH Colombia file-based.

Informe y evidencias: `ACHInterbank_ajustado/docs/npt-h02-h03-final/INFORME_NPT_H02_H03_FINAL.md`.

**Validación real:** 23/23 comprobaciones focales estáticas; seis regresiones Python/SQLite con salida 0; validación XML Schema offline salida 0. Se intentaron `dotnet --info`, `restore`, `build`, `test`; todos **exit 127** por falta del SDK. **xUnit no ejecutado**; no SQL Server/PostgreSQL, UAT ni SOAP Live.

**Estado:** H-02 corregido en fuente, H-03 pruebas corregidas; ambas pendientes de compilación y xUnit en .NET 10. Sin nuevas migraciones, dependencias ni modificación de WSDL/XSD.

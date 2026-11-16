# NPT_H02_H03_FINAL — LEER PRIMERO (16-11-2026)

**HOTFIX SOLO H-02/H-03. NO-GO SOAP Live.** Fuente: `ACHInterbank_HOTFIX_XSD_TRANSPORTE_NCTAORIG_H010203_2026-11-16.zip`.

Regla definitiva: **`Transactions.SourceAccountNumber → Proc_Transacciones.NCTAORIG`**, sin cuenta receptora, constantes ni fallbacks. Se retiró exclusivamente el bloqueo de *homologación de procedencia* (`PROC_TRANSACCIONES_NCTAORIG_CONTRACT_NOT_HOMOLOGATED` y guard equivalente), no los controles de origen válido, XML, permisos, configuración, idempotencia ni F5 ACH Colombia file-based.

Informe y evidencias: `ACHInterbank_ajustado/docs/npt-h02-h03-final/INFORME_NPT_H02_H03_FINAL.md`.

**Validación real:** 23/23 comprobaciones focales estáticas; seis regresiones Python/SQLite con salida 0; validación XML Schema offline salida 0. Se intentaron `dotnet --info`, `restore`, `build`, `test`; todos **exit 127** por falta del SDK. **xUnit no ejecutado**; no SQL Server/PostgreSQL, UAT ni SOAP Live.

**Estado:** H-02 corregido en fuente, H-03 pruebas corregidas; ambas pendientes de compilación y xUnit en .NET 10. Sin nuevas migraciones, dependencias ni modificación de WSDL/XSD.

---

# LEER PRIMERO — H-01/H-02/H-03 · XSD en transporte (2026-11-16)

**Estado NO-GO Live.** ZIP íntegro actualizado sobre `ACHInterbank_NPT_SOAP_XSD_FINAL_01.zip`; leer `ACHInterbank_ajustado/docs/npt-transport-xsd-h010203/INFORME_H010203.md`. H-01: XSD final para clientes CFA/AXON XML directo, sin POST de XML inválido; H-02: guard Live cuenta originador `NCTAORIG` ausente, vacío y nil; H-03: readiness negativo coherente. XSD offline 38/38, fuente 33/33, focal 21/21, 7 regresiones Python exit 0; NPT general heredado 40/42. `dotnet` exit 127 sin SDK: no build/xUnit. No migraciones, no UAT, no Live. Dependencia restante: homologación oficial cuenta origen y autorización operativa F5 por separado.

---

# LEER PRIMERO — NPT_SOAP_XSD_FINAL_01 (2026-11-16)

**Última entrega del proyecto completo. Estado: NO-GO SOAP Live.** Dentro de `ACHInterbank_ajustado/docs/npt-soap-xsd-final-01/` están el informe, la matriz WSDL/XSD, los comandos, las evidencias y el plan. Los cinco XSD originales (2 CFA y 3 Axon) están embebidos y validados offline. Se añadió validación XML Schema a solicitudes y respuestas, controles exactos de `SOAPAction` y validación de fuente de `NCTAORIG`, manteniendo F5 sin autorización y el guard de Live. NPT-01/05 **PARCIAL**; NPT-10/14 **CORREGIDO TÉCNICAMENTE EN FUENTE, pendiente compilación**. Verificación `lxml` 38/38, verificación estática 33/33 y 7 regresiones históricas salida 0; verificador NPT anterior 40/42. `dotnet` ausente (127); xUnit NO ejecutados, sin UAT, sin SQL Server/PostgreSQL ni SOAP real. No se inventó homologación ni se autorizó Live.

---


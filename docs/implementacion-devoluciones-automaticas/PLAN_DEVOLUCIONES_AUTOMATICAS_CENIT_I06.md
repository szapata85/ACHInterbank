# PLAN Y ESTADO DE DEVOLUCIONES AUTOMÁTICAS CENIT — 2026-11-03 (I06)

| Incremento | Objetivo | Estado en código | Validación funcional/productiva |
|---|---|---|---|
| I01-CENIT | Modo independiente `DESACTIVADO` de inicio | INCORPORADO | Pendiente .NET/BD |
| I02-CENIT | Detección pasiva de evidencias reales | INCORPORADO | Pendiente .NET/BD |
| I03-CENIT | Preparación controlada, sin envío | INCORPORADO | Pendiente .NET/BD/concurrencia |
| I04-CENIT | Revisión, aprobación/descarte, excepciones | INCORPORADO | Pendiente .NET/BD/IAM |
| I05-CENIT | NACHA-M PPD/CCD local protegido, `NO_TRANSMITIR` | INCORPORADO | Pendiente .NET/golden files/BD |
| **I06-CENIT** | **Integración, acuses y conciliación** | **DIAGNÓSTICO DE SOLO LECTURA INCORPORADO; tramo financiero ABIERTO** | **Transporte real, acuses atribuibles, conciliación y homologación NO ejecutados** |

**Avance de código:** 5/6 incrementos completos en su alcance limitado, I06 parcialmente incorporado. **Queda 1 frente funcional CENIT por cerrar** dentro de I06; no se creó un incremento I07 artificial. La inclusión de diagnósticos no es certificación, y el paso a producción sigue NO-GO.

**ACH Colombia:** I01–I08 incorporados al código y sin cambios; I09-B obligatorio pendiente (compilación, ejecución, homologación); I09-A condicional. En conjunto: **2 frentes obligatorios pendientes** y **hasta 1 condicional**, sujetos a correcciones surgidas en pruebas reales.

## Requisitos de continuación de I06-CENIT (todos pendientes)

1. **Ambiente:** `dotnet build ACHInterbank.sln -c Release`; `dotnet test tests/Cfa.ACHInterbank.Tests/Cfa.ACHInterbank.Tests.csproj -c Release`; desplegar migraciones hasta I05 en SQL Server/PostgreSQL de prueba, verificar rollback, doble operador y concurrencia real.
2. **Contrato externo homologado:** acordar con CENIT nombre/id/modificador externo, calendario, ciclo/fecha valor y golden files PPD/CCD; no enviar `NO_TRANSMITIR`.
3. **Segregación y duplicados:** resolver bloqueo **transaccional** común con ruta manual CENIT y concurrencia entre generadores, sin tocar ACH Colombia sin causa demostrada.
4. **Seguridad:** cifrado por canal, custodia y rotación de llaves, tokens, logging, retención de archivos y trazabilidad de cada envío, sin exponer payload protegido.
5. **Transporte/acuse:** solamente tras los controles anteriores, crear identidad externa única, registro de intentos de entrega y respuesta idempotente **correlacionada al mismo archivo y huella**; no confundir ACK de transporte con liquidación, rechazo o aceptación del beneficiario.
6. **Conciliación:** evidencias de aceptación/rechazo CENIT, transacciones aplicadas, ajustes financieros, fecha valor, reversos y saldos; discrepancias permanecen abiertas hasta resolución humana.
7. **Normativa especial:** R06, R23, R34, R60–R69, CTX y prenotificaciones requieren controles separados, no heredar reglas ACH Colombia.
8. **Liberación:** pruebas de contrato y homologación del operador antes de cualquier GO productivo. Desplegar por cámara con banderas independientes y desactivadas por defecto; revisión de IAM y auditoría.

**Punto de parada:** entregar paquete I06 con lectura diagnóstica, evidencia y estado abierto. No ejecutar transmisión financiera ni comenzar un nuevo incremento sin nueva instrucción.

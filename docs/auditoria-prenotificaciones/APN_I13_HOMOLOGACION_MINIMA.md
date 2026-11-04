# APN-I13 — Lista mínima de homologación pendiente

Esta lista sustituye una nueva fase extensa de pruebas. **No se crea nueva arquitectura.**

1. **Compilación:** `dotnet test` limitado a las suites I09, I10, I12 e I13. Adjuntar TRX y errores reales.
2. **Ciclos:** verificar un caso ACH Colombia ciclo 1 (devolución hasta ciclo 5), uno que cruce día hábil y un caso CENIT hasta liberación 19:15. Casos de rechazo y ausencia de respuesta.
3. **Persistencia:** ejecutar las pruebas I13 SQLite y comprobar en el proveedor productivo que el `UNION`, `DateTimeOffset` y el parámetro cursor funcionan (SQL Server/PostgreSQL cuando corresponda).
4. **Carrera crítica:** una devolución que llega durante el job **no** puede dejar la cuenta aceptada tácitamente y rechazada a la vez. Verificar transición e idempotencia en base relacional.

Sin pruebas ejecutadas, el estado es **FUENTES ESTABILIZADAS / NO HOMOLOGADO**. No usar 60/60 Python como señal de autorización de producción.

**Externo:** una constancia auténtica de fin de recepción de la cámara/ciclo todavía no existe en el repositorio. El tema se gestiona con el operador, no mediante una señal ficticia en código.

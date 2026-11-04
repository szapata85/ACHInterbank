# Plan de continuidad desde I08-ACH

La implementación de fuente llegó hasta I08-ACH. La ruta CENIT continúa independiente. El siguiente incremento será **I09-ACH**: cierre por evidencia, no ampliación de arquitectura.

1. Ejecutar `dotnet restore ACHInterbank.sln`, `dotnet build ACHInterbank.sln --no-restore`, `dotnet test tests/Cfa.ACHInterbank.Tests/Cfa.ACHInterbank.Tests.csproj --no-build --filter "FullyQualifiedName~AchColombiaAutomaticReturnI08Tests|FullyQualifiedName~AchColombiaAutomaticReturnI06Tests|FullyQualifiedName~AchColombiaReturnRecoveryI07Tests"` (verificar filtros por nombre de clase partial), más pruebas de regresión ACH de I01–I07.
2. Con base de datos relacional efímera, comprobar que una reconstrucción distinta provoca rollback íntegro, sin dejar archivos, eventos, secuencias consumidas o estados inconsistentes; probar dos ejecuciones concurrentes. En I08 estas pruebas **no pudieron ejecutarse**.
3. Comparar NACHA-M generado, reconstruido y recuperado contra archivos de prueba (incluidos prenotificación/devolución de débito y crédito), bytes, encabezados, lotes, adendas, identificadores y SHA-256.
4. Aclarar semántica de `ReturnedByEpr` en los reportes: generación, transmisión, acuse, aceptación, compensación y liquidación deben distinguirse según evidencia. No alterar reportes sin un contrato de negocio aprobado y pruebas de regresión para ambas cámaras.
5. Confirmar con ACH Colombia el calendario de ciclos aplicable; validar migraciones I01/I03 SQL Server y PostgreSQL; obtener homologación, acuses y evidencias externas.
6. Solo después de alcanzar los criterios verificables definir si se solicita habilitar `AchCol:EnableAutomaticReturnFileGeneration`, que **permanece en false**, y transporte en un incremento por separado.

**Entregable:** ZIP acumulativo, memoria actualizada, informe, matriz de pruebas con estados PASS/FAIL/PENDIENTE y decisión GO/NO-GO. No cerrar CENIT con evidencia de ACH.

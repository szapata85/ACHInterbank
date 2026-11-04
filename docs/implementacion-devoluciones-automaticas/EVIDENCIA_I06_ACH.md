# Evidencias verificables I06-ACH

**Base examinada:** ZIP I05-ACH (4.833 entradas). **Alcance:** cambios exclusivamente ACH Colombia.

- Verificador de invariantes I06 (`scripts/test/automatic_returns_i06_ach_static_check.py`): 28/28 PASS_STATIC.
- Scripts históricos: I02 18/18; I03 25/25; I05 33/33. I04 33/35 por dos reglas que prohíben generar archivos, obsoletas desde I05. Ninguno representa pruebas de ejecución.
- Parser TypeScript `transpileModule`: cuatro archivos `.ts` afectados sin diagnósticos de sintaxis. No se ejecutó compilación del proyecto Angular ni navegador.
- Solución: seis proyectos fuente, referencias internas existentes. Sin SDK `dotnet`; xUnit I06 se entrega sin ejecutar.
- Seguridad: `appsettings.json` entrega puerta técnica `false`, `AUTOMATICO` solo para configuración ACH Colombia, endpoint de recuperación con `P0.ReturnsGenerateFile`, sin interfaz pública de generación autónoma.
- CENIT: sin modificación funcional, conforme a comparación de rutas del paquete anterior.
- El resultado de comparación de rutas, SHA256 y el `testzip` se registran en el manifiesto y el archivo de hash del ZIP.

**Límites**: sin SQL Server/PostgreSQL, conexión con cámara, certificación, ejecución xUnit, semántica financiera UAT, evidencia de concurrencia o equivalencia binaria del archivo reconstruido.

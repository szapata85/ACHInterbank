# EVIDENCIA I05-ACH — comprobación sin certificación de ejecución

**Base:** ZIP I04 ACH Colombia completo. **Corte:** 2026-11-02.

## Pruebas realmente ejecutadas

1. `python scripts/test/automatic_returns_i05_ach_static_check.py` → **33/33 PASS_STATIC**.
2. `python scripts/test/automatic_returns_i02_ach_static_check.py` → **18/18 PASS_STATIC**.
3. `python scripts/test/automatic_returns_i03_ach_static_check.py` → **25/25 PASS_STATIC**.
4. `python scripts/test/automatic_returns_i04_ach_static_check.py` → **33/35** sobre código I05. Fallan precisamente los requisitos históricos `sin generador NACHA` y `SPA no invoca creación archivo`, que se reemplazan por I05. Se mantiene script original como evidencia histórica.
5. Árbol de sintaxis TypeScript mediante `typescript.createSourceFile`: **4/4** archivos nuevos/modificados analizados sin errores de sintaxis; NO equivale a compilación de Angular.
6. `python scripts/validate-solution-projects.py` → **SOLUTION GRAPH OK: 7 proyectos y referencias**.
7. El manifiesto compara las entradas del ZIP I04 con I05 por contenido, registra rutas añadidas/modificadas y verifica integridad del ZIP.

## Pruebas incorporadas, NO EJECUTADAS

- `AchColombiaApprovedReturnGenerationI05Tests.cs`: generación de aprobada, bloqueo sin aprobación, modo desactivado, cámara CENIT, duplicado manual, ventana cambiada, R10, error de generador, falta de registro persistente, descartada, segundo intento.
- Jasmine SPA: confirmación antes de generar, no elegible bloqueada, respuesta de conflicto, contrato del endpoint y nombre de archivo de la política.

## Ambiente ausente, sin detener incremento

`dotnet` no disponible; no se pudieron ejecutar compilación ni xUnit. No hay `node_modules` Angular; no se pudo ejecutar build Angular, Karma ni navegador. No hay SQL Server/PostgreSQL de integración. Pruebas transaccionales reales y UAT/homologación ACH Colombia pendientes. **NO-GO.**

# Validaciones y evidencias por incremento

## Capas, en orden

1. **Integridad y estática real:** ZIP verificable, archivos presentes, sintaxis/formatos relevantes, verificador de diffs y reglas.
2. **Unitarias:** valores predeterminados, lectura/cambio de modo, reglas, causas, ventanas, permisos, excepciones, idempotencia.
3. **Integración local:** API → persistencia → generación actual de devoluciones, SQL Server y/o proveedor compatible cuando disponible, reintentos concurrentes.
4. **SPA:** compilación TypeScript/Angular, pruebas componente y revisión visual manual cuando haya navegador/herramientas.
5. **UAT controlado:** archivos de prueba por cámara, recepción/aplicación y evidencia de transmisión/acuses solo si existe ambiente autorizado.
6. **Homologación externa/producción:** exige evidencia de participantes/cámaras, permisos, transporte, conciliación y validación formal; no está incluida en I00 ni se presume por pasar tests.

## Comandos del proyecto (no afirmarlos ejecutados sin logs)

```cmd
python scripts/validate-solution-projects.py
dotnet build ACHInterbank.sln -c Release
dotnet test tests/Cfa.ACHInterbank.Tests/Cfa.ACHInterbank.Tests.csproj -c Release
cd webch-interbank-ui
npm ci
npm run build
```

Los comandos concretos de Karma/Playwright se deben obtener de los scripts **vigentes** de `package.json`, no inferirlos. No ejecutar conexiones productivas ni movimientos monetarios.

## Vocabulario de evidencia

- `PASS_REAL`: comando/caso ejecutado y resultado observado.
- `PASS_ESTATICO`: verificación de código/estructura realmente realizada; NO es compilación.
- `FALLA`: ejecución falló (con error reproducible).
- `NO_EJECUTADA_POR_AMBIENTE`: tool/SDK/network/DB/browser ausente; adjuntar comando para entorno adecuado.
- `PENDIENTE_HOMOLOGACION`: la cámara o certificación externa todavía no valida el flujo.

## Reporte de avance por fase

Cada informe debe tener: identificador, base SHA-256, objetivo, lista exacta de cambios, al menos prueba positiva y negativa pertinente, comandos y resultados ejecutados, pendientes por ambiente, regresiones de otras cámaras, riesgos, estado final, próximo único incremento, ZIP completo SHA-256.

**Regla:** NO bloquear cambios seguros por no contar con .NET/SQL/navegador en ChatGPT; NO convertir una ejecución pendiente en resultado aprobado. Mantener modo desactivado hasta reunir evidencia de activación.

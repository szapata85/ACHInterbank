# APN-I13 — Estabilización mínima y cierre técnico de fuentes

**Corte:** 2026-11-03. **Base única:** `ACHInterbank_COMPLETO_CORRECCION_PRENOTIFICACIONES_APN_I12_2026-11-03.zip`.

## Decisión de diseño: no agregar sobreingeniería

Se inspeccionaron `PrenotificationCandidateScan`, `AchTacitAcceptanceJobHandler`, la entidad `TaskParameter`, el índice único de `TaskParameters`, las pruebas I08–I12 y la configuración de la suite `net10.0`.

**No se identificó un defecto suficientemente demostrado que justifique cambiar el comportamiento productivo en I13.** El cursor de I12 no se reemplaza: mantiene una razón funcional concreta (evitar que registros antiguos bloqueados hagan que nunca se revisen los posteriores). Eliminarlo sin prueba relacional y de backlogs podría reintroducir ese riesgo. Se conserva también el límite de evaluaciones y la revisión individual de cada candidato.

**Cambios productivos:** ninguno. **Nuevos estados, tablas, servicios, migraciones, jobs o endpoints:** ninguno.

## Únicos cambios de I13

1. `tests/Cfa.ACHInterbank.Tests/PrenotificationCursorI13RelationalTests.cs`: dos pruebas xUnit nuevas que utilizan SQLite relacional para comprobar persistencia del cursor existente tras reconstruir el `AchDbContext`, aislamiento del parámetro `BatchSize` e índice único `(TaskDefinitionId, Key)`. **Están escritas, no ejecutadas.** SQLite es un smoke test y no equivale a SQL Server/PostgreSQL.
2. `scripts/audit/prenotificaciones/run_apn_i13_dotnet.sh`: ejecutor focalizado para suites del cursor, escaneo I12, seguridad I09 y estados terminales I10. Falla con código 127 si no hay SDK, sin falsos PASS.
3. `scripts/audit/prenotificaciones/test_apn_i13_stabilization.py`: seis verificaciones ejecutables de contratos estáticos y un ejemplo SQLite independiente. **No ejecuta C#/EF.**
4. Documentación, logs, memoria de continuidad y registro de integridad.

## Alcance de reglas conservadas

- ACH Colombia: 7 ciclos vigentes, ventana de cuatro ciclos posteriores válidos, no contar cancelados y esperar salida del último ciclo.
- CENIT: 5 sesiones, última liberación hasta 19:15 hora de Bogotá.
- Aceptación tácita: exige plazo recalculado, transmisión, estados canónicos, ausencia de devoluciones/respuestas y barreras NACHA/MFT/CENIT.
- Nunca interpretar `ACK`, `NoActivity`, ciclo cerrado o ausencia de archivos visibles como comprobante externo positivo de entrega completa.

## Ejecución y límites

```
cd ACHInterbank_ajustado
python scripts/audit/prenotificaciones/test_apn_i13_stabilization.py
bash scripts/audit/prenotificaciones/run_apn_i13_dotnet.sh
```

- Python I08–I13 y matriz: **60/60 PASS**; solo estáticas, documentales y ejemplos independientes.
- xUnit .NET 10: **NO_EJECUTADO** en este entorno (SDK ausente, salida 127).
- EF Core real, consultas `UNION`, `DateTimeOffset`, SQL Server/PostgreSQL, Quartz, concurrencia, archivos reales y UAT: **NO HOMOLOGADO**.
- Contrato externo de fin de recepción: **SIN PRODUCTOR VERIFICADO**. No se inventa confirmación.

## Criterio de cierre I13

**CERRADO COMO ESTABILIZACIÓN DE FUENTES Y ENTREGA DOCUMENTAL**, no como homologación operativa ni autorización de despliegue. No se recomienda crear otro incremento de arquitectura mientras no haya una prueba ejecutada que demuestre un defecto o una integración externa real que justificar.

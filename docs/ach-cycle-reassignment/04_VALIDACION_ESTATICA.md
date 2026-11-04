# ACH-CR-001 — Protocolo de validación estática y evidencia por incremento

## Restricción de entorno

La validación de este programa se realiza **sin ejecutar** `dotnet build`, `dotnet test`, Docker, servicios SOAP, Quartz productivo, MFT, SQL ni movimiento monetario si el entorno no permite esas acciones. **No bloquear** el avance documental o estático por la ausencia de herramientas; registrar exactamente `NOT_RUN_ENVIRONMENT`. Escribir tests de unidad está permitido, pero sin SDK no son tests ejecutados.

## Validación mínima de cada entrega

1. **Integridad del ZIP:** apertura, central directory, ausencia de duplicados de ruta, prueba CRC; conservar la carpeta raíz completa.
2. **Integridad del código:** comparar nombres/tamaños/CRC o hashes entre ZIP anterior y nuevo; listar únicamente las rutas cambiadas, agregadas o removidas; ninguna remoción sin justificación.
3. **Arquitectura:** Domain libre de Persistence/EF/Quartz; Application expone contratos sin infra; Quartz actúa de adaptador; resolver políticas/reloj/calendario existentes.
4. **Trazado:** identificar consumidores y llamadas afectadas (creación, exportación, MFT, dispatch, estado, compensación CENIT).
5. **Seguridad:** no reenvío ante incertidumbre; pruebas de predicado fail-closed; ninguna credencial/PII/log SOAP real.
6. **Calidad estática:** sintaxis C# revisada manualmente y dependencias/DI consistentes; XML de `.csproj`/`.props` consistente si se modifica; sin namespaces/archivos inexistentes.
7. **Pruebas focales:** registrar test files añadidos o modificados y casos CR-xx cubiertos por el diseño. No declarar resultados de test-run sin ejecución.
8. **Memoria + docs:** `MEMORY_STATE.md` tiene incremento, decisiones, hechos, riesgos, evidencia, siguiente paso; `CHANGELOG.md` actualizado; docs as-built reflejan cambios.
9. **Empaquetado:** ZIP de incremento `ACHInterbank_ACH_CR_Ixx_COMPLETO.zip` sin excluir código, configuraciones ni documentación previa.

## Herramienta incluida

Desde la **carpeta del proyecto descomprimida**:

```bash
python scripts/validate-cycle-reassignment-static.py --directory .
```

Desde cualquier carpeta, **sin extraer el ZIP**:

```bash
python scripts/validate-cycle-reassignment-static.py --zip ACHInterbank_ACH_CR_I00_COMPLETO.zip
```

El script valida estructura/documentación/presencia de componentes reutilizables. **No valida compilación ni semántica regulatoria**, y no pretende reemplazar análisis de cambio específico; tras I01 se debe ampliar solo cuando aparezca un riesgo estático reproducible.

Comando conservado del proyecto para cuando exista un ambiente autorizado:

```bash
python scripts/validate-solution-projects.py
```

`dotnet build`/`dotnet test` son **validaciones futuras**; no obligatorias para empaquetar evidencia estática en este ambiente.

## Evidencia I00 (se completará tras construcción del ZIP)

| Control | Evidencia |
|---|---|
| Inspección fuentes base | `AchTransaction`, `AchCycle`, `AchBatch`, `RoutingStrategyService`, `BatchResolver`, `CenitCycleQueueService`, `CenitCycleExecutionService`, `LiquidityOptimizationService`, `NachaDataLoader`, `AchFileExportTransaction`, `AchTransactionStateEvent`, handlers Quartz |
| Original | `ACHInterbank_ACHCOL_CFA_F6_3_UNIFICADO.zip` |
| Cambios en código producto | **0** |
| Cambios en normativa original | **0** |
| Migraciones nuevas | **0** |
| Pruebas runtime | `NOT_RUN_ENVIRONMENT` (SDK `.NET` no disponible en este entorno) |
| Externo / redes / BD | `NOT_RUN_NOT_REQUESTED` |
| Estado | `DOCS_ONLY_STATIC_BASELINE` |

Consultar `CHANGELOG.md` para conteos, verificación de integridad y SHA-256 exacto del ZIP. Ninguna afirmación `BUILD_PASSED`, `TESTS_PASSED`, `UAT_READY` o `PRODUCTION_READY` es válida para I00.

## Plantilla de evidencia para I01+

```markdown
### Ixx — Evidencia
- ZIP entrada: nombre + SHA-256:
- ZIP salida: nombre + SHA-256:
- Archivos nuevos/cambiados/eliminados:
- Motivo técnico por archivo:
- Reutilización comprobada:
- Validador estático + comando + salida (PASS/FAIL):
- Revisión de arquitecturas/dependencias:
- Casos CR-xx escritos / revisados / pendientes:
- Herramientas indisponibles: NOT_RUN_ENVIRONMENT (...):
- Riesgos no resueltos / evidencia faltante:
- Estado real y siguiente incremento:
```

## Política de errores

Si un validador estático falla, corregir el defecto en el mismo incremento o registrar `PARTIAL_STATIC_FAILED` con causa específica; no maquillar un fallo como aprobado. Si el SDK falta, no reescribir tests para que parezcan aprobados. Si los cambios requieren operaciones externas para confirmación, documentar hipótesis y marcar comportamiento `NOT_VERIFIED_RUNTIME`.


## Evidencia de I01 — 2026-10-25

Además del validador heredado, se agregó `scripts/validate-cycle-reassignment-i01-static.py`. Se ejecutaron ambos desde la raíz descomprimida: `PASS` para validador I00 y `PASS 19/19` para validador específico I01. Los nuevos tests xUnit tienen doce escenarios **escritos pero no ejecutados**; no existe `dotnet` en el entorno. Arquitectura revisada: Application contiene solo interfaz/modelo; Persistence realiza EF `AsNoTracking`; servicio sin `SaveChanges` ni locking. Ver `07_I01_IMPLEMENTACION_Y_EVIDENCIA.md`.

Pendientes técnicos: compilación real, tests unitarios, EF SQL Server/PostgreSQL, pruebas integradas de publicación y concurrencia.


## I02 — Validación estática (sin build)

```bash
python scripts/validate-cycle-reassignment-static.py
python scripts/validate-cycle-reassignment-i01-static.py
python scripts/validate-cycle-reassignment-i02-static.py
python scripts/validate-solution-projects.py
```

El tercer script comprueba estructura de contratos, DI por convención, secuencia de transacción relacional, uso de los servicios previos de elegibilidad y lotes, auditoría, tests escritos y documentación de riesgos. No constituye compilación C#, validación de SQL o prueba de carrera. Probar SQL Server/PostgreSQL posteriormente con proveedor real y escritor de exportaciones en paralelo. Ver `08_I02_IMPLEMENTACION_Y_EVIDENCIA.md`.

## Validación estática I03

`python3 scripts/validate-cycle-reassignment-i03-static.py` revisa presencia y composición del handler, catálogo, seeder Disabled, doble flag, selección `AsNoTracking`/keyset, límites, contrato I02, propagación de cancelación, observabilidad y nueve tests escritos. Ejecutar también scripts de I01/I02; contra el ZIP final: `python3 scripts/validate-cycle-reassignment-static.py --zip <ACHInterbank_ACH_CR_I03_COMPLETO.zip>`. Los validadores son verificaciones **estáticas**; no demuestran que C# compile ni que Quartz/EF se ejecute.

Es obligatorio registrar en entrega: cantidad de entradas del ZIP, diferencia vs I02 por path/CRC/tamaño, CRC total, hash SHA-256, conteo de controles y estado real de .NET (NOT_RUN_ENVIRONMENT si ausente).

## Validación estática I04 (2026-10-25)

```bash
python scripts/validate-cycle-reassignment-i04-static.py
python scripts/validate-cycle-reassignment-i03-static.py
python scripts/validate-cycle-reassignment-i02-static.py
python scripts/validate-cycle-reassignment-i01-static.py
python scripts/validate-solution-projects.py
python scripts/validate-cycle-reassignment-static.py --zip ACHInterbank_ACH_CR_I04_COMPLETO.zip
```

El validador I04 comprueba composición de guardias, uso de política/calendario existentes, límite de fecha valor, no activación Quartz, casos de prueba escritos y docs. El ZIP final será comparado por ruta+CRC contra I03. No demuestra compilación/ejecución de C#/SQL/EF/Quartz. Ver `10_I04_IMPLEMENTACION_Y_EVIDENCIA.md` y reporte externo I04 JSON.

## I05 — validación estática adicional

```bash
python scripts/validate-cycle-reassignment-i05-static.py
python scripts/validate-cycle-reassignment-i01-static.py
python scripts/validate-cycle-reassignment-i02-static.py
python scripts/validate-cycle-reassignment-i03-static.py
python scripts/validate-cycle-reassignment-i04-static.py
python scripts/validate-solution-projects.py
```

Comprobar `IsolationLevel.Serializable` tanto en I02 como en admisión NACHA, membresías exactas antes de entregar desde las tres rutas conocidas, guardia por ciclo fuente, regresiones CENIT, ausencia de nuevas migraciones, y Quartz deshabilitado. Compilación C#, SQL Server/PostgreSQL, 2 procesos simultáneos y aceptación ACH no corren en este ambiente: reportar `NOT_RUN_ENVIRONMENT`. Repetir con `--zip` al final para integridad.

## Cierre I06 — Reejecución y límites

Desde raíz del proyecto extraído:

```bash
python scripts/validate-cycle-reassignment-i01-static.py
python scripts/validate-cycle-reassignment-i02-static.py
python scripts/validate-cycle-reassignment-i03-static.py
python scripts/validate-cycle-reassignment-i04-static.py
python scripts/validate-cycle-reassignment-i05-static.py
python scripts/validate-cycle-reassignment-i06-static.py
python scripts/validate-solution-projects.py
```

Después del empaquetado, desde la ubicación que contenga el ZIP completo:

```bash
python scripts/validate-cycle-reassignment-static.py --zip ACHInterbank_ACH_CR_I06_COMPLETO.zip
```

Comprobar SHA-256 y CRC completo del ZIP, los archivos modificados/agregados/eliminados y la preservación ruta/CRC/tamaño de todo archivo no modificado frente a I05. `ACH_CR_I06_VALIDATION.json` y `.sha256` externos son la evidencia del paquete exacto, no documentación normativa ni prueba ejecutable.

**Límite de alcance:** las verificaciones de código fuente y grafo `.sln` no prueban compilación C#, coherencia de consultas SQL traducidas por EF, ni reglas operativas/financieras end-to-end. Mantener `NOT_RUN_ENVIRONMENT` hasta ejecutar herramientas adecuadas. No habilitar Quartz por resultados de estos scripts.

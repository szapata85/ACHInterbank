# I04 — Integración de devolución de prenotificación con NachaUpload

**Fecha:** 2026-11-02  
**Base de código:** ZIP I03; cambios acotados a la correlación y admisión del pipeline existente  
**Estado:** EN_PROGRESO: código y pruebas C# añadidos; sin compilación/homologación runtime por ausencia del SDK .NET.

## Objetivo y decisión arquitectónica

No crear un segundo importador ni actualizar manualmente `AchTransaction`, `PrenotificationValidationAttempt` o `CustomerThirdParty`. NachaUpload entra por `IncomingNachaIngestionAppService`, clasifica y vincula por `IncomingNachaPostParseProcessor`, y aplica resultados exclusivamente por `AchStateTransitionService`. Este último ya invoca `ThirdPartyAccountValidationService.ApplyTransactionResolutionAsync` y `SynchronizePrenotificationThirdPartyAsync`, con protección de intentos terminales y eventos idempotentes. I04 incorpora **una guarda antes de la transición**, no una nueva máquina de estados.

## Cambios de código incorporados

1. `IncomingPrenotificationReturnGuard.cs`: política pura, fail-closed para retornos de prenotificaciones. Comprueba referencia original de adenda tipo 99, vínculo exacto/manual final, importe cero en ambos lados, nuevo trace distinto del original, misma cámara, estado e identidad del intento y resultados terminales. Una devolución repetida con misma causal pasa al motor oficial para que su idempotencia reporte `WasDuplicate`; una devolución conflictiva se bloquea.
2. `IncomingNachaPostParseProcessor.cs`: invoca la guarda **antes** de resolver/aplicar una devolución, genera evento auditable `PrenotificacionReturnCorrelacionada` o `PrenotificacionReturnBloqueada`, y en caso de bloqueo marca revisión manual sin cambiar estado.
3. Reconocimiento de artefacto I03 por SHA-256, no por nombre del archivo. **Corrección real:** `IncomingNachaIngestionAppService.ComputeSha256` usa hexadecimal minúsculo, pero I03 genera `Convert.ToHexString` en mayúsculo; ahora se normaliza a mayúsculo antes de consultar. Se comprueban cámara, banco que responde, banco originador, causal e ID/trace original de la entrada en la simulación registrada. Si el mismo SHA identifica más de una simulación, se bloquea.
4. `ResolveReturnRouteAsync`: utiliza `IsPrenotification || Type==Prenotification` al construir la consulta de códigos de retorno, evitando categorizar como crédito/débito monetario una prenotificación que conserve `Type=Credit/Debit` por compatibilidad.
5. Política CENIT de retorno: recibe `TransactionTypeEnum.Prenotification` si `IsPrenotification=true`, para que el evaluador trate correctamente una devolución no monetaria.
6. Se preserva el importador, endpoint, autenticación y motor oficial de estados, y el generador I03 sigue `GeneratedOnly=true`, `AutoImported=false`. Ninguna ruta crea movimientos monetarios ni transmite externamente.

## Pruebas incorporadas

- `IncomingPrenotificationReturnGuardI04Tests.cs`: 12 casos de contrato (incluida selección ACHCOL/CENIT) para trazas, monto, identidad de banco, causa, intento ausente, duplicado exacto, conflicto terminal, hash ambiguo y no regresión de retorno ordinario.
- `IncomingNachaPostParseProcessorTests.cs`: dos casos de integración de orquestación con EF InMemory/mocks, que verifican bloqueo sin invocar transición y paso de correlación con causal sin resolver. **No equivalen a E2E real**.
- `scripts/test/prenotification_simulator_i04_static_check.py`: 31 invariantes de fuente; también se ejecutaron los scripts I01, I02, I03 y el validador de grafo de solución.

## Estado de ejecución

| Verificación | Resultado | Evidencia / comando |
|---|---|---|
| Validación de `.sln` | PASS | `python scripts/validate-solution-projects.py` → 7/7 |
| I01 | PASS_STATIC 20/20 | `python scripts/test/prenotification_simulator_i01_static_check.py` |
| I02 | PASS_STATIC 26/26 | `python scripts/test/prenotification_simulator_i02_static_check.py` |
| I03 | PASS_STATIC 31/31 | `python scripts/test/prenotification_simulator_i03_static_check.py` |
| I04 | PASS_STATIC 31/31 | `python scripts/test/prenotification_simulator_i04_static_check.py` |
| `dotnet build ACHInterbank.sln -c Release` | NO_EJECUTADA_POR_AMBIENTE | `dotnet` no instalado |
| Xunit I01–I04 | NO_EJECUTADA_POR_AMBIENTE | `dotnet` no instalado |
| NachaUpload E2E ACHCOL/CENIT con archivos reales | NO_EJECUTADA_POR_AMBIENTE | Sin .NET / motor BD de validación |
| Reupload concurrente con SQL Server/PostgreSQL | NO_EJECUTADA_POR_AMBIENTE | Sin .NET / motores de prueba |

## Riesgos y trabajo aún pendiente para cerrar I04/I07

- Comprobar compilación efectiva, contexto EF, fixture de NACHA-M (adenda y trace), idempotencia de `AchStateTransitionService`, eventos y persistencia en PostgreSQL y SQL Server. **No afirmar aprobación ni homologación.**
- Verificar si el parser/empaquetado de ACHCOL/CENIT conserva el hash del archivo físico, especialmente ante archivo protegido o desencriptado: una transformación de bytes puede alterar el hash respecto del archivo I03. No inferir procedencia por nombre ni degradar seguridad: en discrepancia exigir revisión hasta disponer de huella verificable por artefacto.
- Comprobar que el rechazo CENIT entre sesiones y la aplicación del catálogo de prenotificación cumplan con la versión normativa; no confiar únicamente en el chequeo estático.
- Revisar que `AchStateTransitionService` mantenga consistencia bajo transacciones y reintentos; en I07 confirmar que no quedan estados parcialmente aplicados si falla la persistencia.
- I05 implementará la ausencia de devolución sin emitir archivos y tendrá que coordinarse con intentos terminales que esta guarda rechaza. I06 incorporará interfaz y descarga con JWT.

## Regla de seguridad

Solo UAT/local; no mutaciones fuera del pipeline de estados oficial ni descargas/importaciones automáticas. Un intento ya aceptado tácitamente o rechazado con una causal distinta **requiere revisión manual**, no reescritura silenciosa. La falta de ambiente no detiene cambios de código seguros; sí impide declarar completada la verificación final.

# MEMORY STATE — DEVOLUCIONES AUTOMÁTICAS — I08-ACH

**Corte documental:** 2026-11-02. **Incremento vigente:** I08-ACH: verificación de artefacto antes de confirmar la generación autónoma. **Base:** `ACHInterbank_COMPLETO_DEVOLUCIONES_AUTOMATICAS_I07_ACHCOLOMBIA_2026-11-02.zip`. **Siguiente ZIP:** `ACHInterbank_COMPLETO_DEVOLUCIONES_AUTOMATICAS_I08_ACHCOLOMBIA_2026-11-02.zip`. **Producción: NO-GO.** **CENIT: sin cambios funcionales.**

## Cambios reales I08

1. Se preservan I01–I07. `AchCol:EnableAutomaticReturnFileGeneration` sigue `false` por defecto y no existe nueva ruta de envío.
2. En `AchColombiaReturnReviewService`, **solo para `GenerateAutomaticallyAsync`**, se requiere el servicio `IAchOutboundReturnArtifactService` incluso si el parámetro y la puerta técnica están habilitados. Ausencia de reconstructor: bloqueo antes de llamar al generador.
3. Después de invocar el generador NACHA-M, pero **antes** de confirmar el estado `ARCHIVO_GENERADO_SIN_ENVIO` y la transacción relacional, `VerifyAutomaticArtifactAsync` reconstruye el archivo registrado y verifica cámara, ciclo, nombre, transacción, cantidad y bytes con `CryptographicOperations.FixedTimeEquals`.
4. Diferencias binarias, metadatos incorrectos o reconstrucción inválida producen conflicto controlado. La transacción envolvente de BD relacional debe revertirse. **Esto NO está certificado en SQL Server/PostgreSQL.** InMemory no puede demostrar rollback.
5. No se modifica `GenerateApprovedFileAsync` (manual), el generador histórico `AchReturnsService`, reportes, modelo de datos, migraciones, SPA ni código funcional de CENIT.
6. Pruebas xUnit I08 agregadas para discrepancias de bytes/cámara/ciclo/transacción/registro, falta de servicio, error al reconstruir y aislamiento CENIT. Se ajustaron dos fixtures positivos I06 para entregar artefacto compatible.

## Verificaciones realmente ejecutadas

- `scripts/test/automatic_returns_i08_ach_static_check.py`: **25/25** controles de fuente.
- Regresión de scripts estáticos aplicables: I02 **18/18**, I03 **25/25**, I05 **33/33**, I06 **28/28**, I07 **24/24**.
- Script I04 histórico: **32/35** por condiciones previas a la existencia de generación y descarga; NO marcar aprobado.
- Grafo de solución: **7/7** referencias encontradas (no implica compilación).
- Verificación integral ZIP y aislamiento CENIT: consignados en evidencia I08.
- **No ejecutado:** dotnet build/test, ejecución xUnit, persistencia y rollback SQL Server/PostgreSQL, concurrencia real, comparación con NACHA-M en cámara, compilación Angular y homologación externa. No declarar PASS para lo anterior.

## Riesgos y bloqueos abiertos

- El validador I08 previene la confirmación de un archivo autónomo que no sea reconstruible en ese instante; **no corrige diferencias binarias de origen** ni demuestra que archivos anteriores sean recuperables. I07 sigue bloqueando descargas históricas si no coinciden con el hash de generación.
- La reconstrucción a partir de datos vivos podría diferir tras modificaciones posteriores; probar golden files completos y política de conservación de artefactos originales antes de GO.
- El generador asigna `ReturnedByEpr` **al generar**, no al recibir acuse. `AchReconciliationReportService` y `AchOperationalReconciliationService` pueden tratarlo como devolución en sus cifras. **No son prueba de compensación/liquidación.** Es una dependencia funcional abierta; se evitó un cambio transversal sin contrato claro.
- Validar calendario ACH Colombia con evidencia operativa: manual V35 vs anexo de siete ciclos. El sistema no debe habilitarse solo con esta documentación.
- Revisar migraciones I01/I03 y concurrencia en SQL Server/PostgreSQL; revisar errores y estados ante reinicios, consumo de secuencia y respuesta de cámara.
- La generación manual no incorpora el control pre-commit I08; la recuperación I07 conserva control hash. Alcance deliberado para no alterar funcionalidad histórica.

## Siguiente fase y punto de parada

**I08 termina incorporado al código y verificado estáticamente; NO-GO productivo.** El siguiente trabajo es **I09-ACH, cierre verificable y pruebas de certificación**, o correctivo puntual si las pruebas reales detectan diferencias. Antes de conectar transporte: ejecutar prueba SQL de rollback, golden file NACHA-M, reportes financieros, ventanas de siete/cinco ciclos, transporte/acuse/conciliación y aprobar homologación ACH. CENIT deberá iniciar su propia ruta sobre una base congelada, sin heredar reglas ACH.

**Continuidad de memorias:** no sobrescribir `/MEMORY.md` ni `/MEMORY_STATE.md` en la raíz; pertenecen a iniciativas preexistentes. Los estados históricos específicos I00–I07 permanecen en este directorio.

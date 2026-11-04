# EVIDENCIA — I03-CENIT

**Fecha:** 2026-11-03. **Naturaleza de resultados:** únicamente verificación estática y de integridad de artefactos; ninguna aprobación funcional ni homologación.

## Ejecutado

- Archivo original ZIP I02-CENIT: `zipfile.testzip()` sin errores, **4.886 entradas**; SHA-256 coincidente con `.zip.sha256` original: `bbb2862461466d54117a897b7a1b065cf396f8725ea396eabcec6577a93a2b69`.
- Lectura de memoria I02, estado I08-ACH, plan CENIT I01–I06, detector real, servicio de modos y mapeos de EF; contraste con normativa local DSP-152, Anexo A, manual NACHA-M.
- `python scripts/test/automatic_returns_i03_cenit_static_check.py`: **57/57**, aprobado como fuente.
- `python scripts/test/automatic_returns_i02_cenit_static_check.py`: **50/50**, aprobado como fuente.
- `python scripts/test/automatic_returns_i01_cenit_static_check.py`: **29/29**, aprobado como fuente.
- ACH scripts I02:18/18, I03:25/25, I05:33/33, I06:28/28, I07:24/24, I08:25/25, todos estáticos. I04-ACH: **32/35**, resultado histórico incompleto **no aprobado**.
- Sin modificación de ningún archivo funcional de ACH Colombia; los cuatro archivos modificados respecto al ZIP I02 son el `AchDbContext`, DI compartido y snapshots de ambos proveedores. Comparación SHA-256 e inventario se adjuntan en manifiesto.

## Pruebas incluidas, no ejecutadas

- `CenitReturnPreparationI03Tests`: apagado por defecto; persistencia para los otros dos modos sin envío; reserva única y auditoría; stale evidence; R06; respuesta posterior exitosa; aislamiento por cámara; Fecha Valor; cierre/corte; festivos; devolución existente; rutas y permisos.
- **Sin `dotnet build` ni `dotnet test`**: SDK .NET ausente en este contenedor. No inferir que los tests xUnit pasan.
- **Sin motor SQL Server/PostgreSQL**: no se ejecutaron migraciones, confirmación de `Serializable`, contienda entre dos instancias, Foreign Keys ni rollback.
- **Sin SPA Angular, navegador ni homologación bancaria**: no existen resultados UAT, certificación CENIT, tiempo real, NACHA-M ni envío.

## Comandos propuestos en ambiente de certificación

Desde la raíz `ACHInterbank_ajustado`:

```sh
python scripts/test/automatic_returns_i03_cenit_static_check.py
python scripts/test/automatic_returns_i02_cenit_static_check.py
python scripts/test/automatic_returns_i01_cenit_static_check.py
dotnet build ACHInterbank.sln --configuration Release
dotnet test tests/Cfa.ACHInterbank.Tests/Cfa.ACHInterbank.Tests.csproj --filter FullyQualifiedName~CenitReturnPreparationI03Tests --configuration Release
dotnet test tests/Cfa.ACHInterbank.Tests/Cfa.ACHInterbank.Tests.csproj --filter FullyQualifiedName~CenitPassiveReturnDetectionI02Tests --configuration Release
```

En SQL Server y PostgreSQL por separado: ejecutar migración I03 en instancia aislada, revalidar `CenitReturnPreparations` y ambos índices únicos/FK; realizar **dos POST concurrentes desde conexiones independientes** con el mismo original y la misma ejecución para verificar exactamente una fila y respuesta de conflicto, sin creación `AchReturnsGenerated`; confirmar auditoría y realizar rollback por falla provocada. Comprobar modo `DESACTIVADO`, cambio concurrente de modo, cambio de resultado del núcleo, ausencia de día hábil, cierre de ciclo y que ACH Colombia no cambia. Repetir controles autenticados 401/403/200/400/409. Registrar evidencia con ID de entorno, timestamps y resultados auténticos.

## Integridad final

El ZIP I03 y la huella `.sha256` externos se generan al cerrar el incremento. El manifiesto `MANIFIESTO_DEVOLUCIONES_AUTOMATICAS_I03_CENIT.json` lista archivos conservados, modificados y añadidos; el `INVENTARIO_CONSERVADOS_I03_CENIT.tsv` contiene SHA-256 de cada archivo idéntico. La ruta y el hash definitivo se suministran fuera del ZIP para evitar referencias autorrecursivas.

**NO-GO productivo.**

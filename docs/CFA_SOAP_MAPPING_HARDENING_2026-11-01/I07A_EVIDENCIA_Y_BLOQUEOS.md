# I07-A — Evidencia de preparación y pendientes

**Fecha:** 2026-11-01  
**Alcance:** revisión/empacado de código I01–I06 y artefactos de preparación; **sin pruebas UAT ni tráfico SOAP**.

## Estado de validaciones sobre la base I06

| Gate | Evidencia I07-A | Resultado |
|---|---|---|
| Conservación de archivos fuente I06 | Comparación ZIP de base vs. ZIP integral I07-A | `PASS_PRESERVATION`: 4.501 originales presentes; 4.497 byte idénticos, 4 Markdown actualizados, 5 archivos nuevos; sin código modificado |
| Solución .NET | `python scripts/validate-solution-projects.py` | `PASS_STATIC` (7/7, exit 0) |
| Ratchet P8.1 | `python scripts/validate-cfa-p8-1-static.py` | `PASS_STATIC_ONLY` (exit 0) |
| Ratchet P8.3 | `python scripts/validate-cfa-p8-3-gauntlet.py` | `NOT_RUN_ENVIRONMENT` (I06: timeout, no se finge PASS) |
| SDK .NET / build / xUnit | `dotnet --info` / build / test | `NOT_RUN_ENVIRONMENT` (.NET SDK no disponible; build/xUnit no ejecutados) |
| DB SQL Server/PostgreSQL | Tests dual-provider P8.4 | `NOT_RUN_ENVIRONMENT` |
| Banco/servidor SOAP oficial | Pruebas de homologación | `NOT_RUN_I07A` |
| UAT y aprobación del negocio | Acta oficial | `NOT_STARTED` |

## Condiciones de la evidencia

- `python scripts/validate-solution-projects.py` se ejecutó desde la raíz: 7 proyectos/7, exit 0.
- `python scripts/validate-cfa-p8-1-static.py` se ejecutó desde la raíz, con directorio `artifacts/` temporal: JSON `PASS_STATIC_ONLY`, exit 0; **no** prueba funcional ni gauntlet.
- El `.cmd` es **preparado para Windows**, no ejecutado en este host Linux. Los dos validadores Python son los que efectivamente pasaron.
- Comprobación de integridad de origen: se comparó cada archivo con el ZIP I06 (SHA-256 de contenido); 4.501/4.501 preservados, 4.497 idénticos y solo los cuatro Markdown de estado/índices cambiados.
- El directorio `artifacts/` generado por P8.1 es temporal y se excluye expresamente del ZIP final.

## Riesgos y dependencias de cara a I07-B

- **B-01 Funcional:** I02: `OFEMP`, `OFIDARCH`, `OFIDLOT`, `OFST`, `OFDIRECCIONIP` no homologados.
- **B-02 Funcional:** I03: `NCTAORIG` no tiene fuente origen fiable; `IDORIG`/`INFPAG` necesitan aceptación semántica del banco. El veto Live existente **no se retira**.
- **B-03 Ejecución:** .NET SDK, tests, concurrencia y estado persistido no demostrados en este artefacto.
- **B-04 Arquitectura/cutover:** P8.4 pendiente; `UseContractDrivenRequest` no se activa.
- **B-05 Configuración:** `appsettings.Development.json` configura `Mode=Live`/endpoints locales. Inspeccionar perfil/overrides reales; **no** asumir DryRun por archivo base.
- **B-06 Oficial:** faltan autorización, contrato bancario/XSD externo completo, entorno oficial y evidencia de recepción. No afirmar `UAT_READY`.

Este informe **no reutiliza** resultados de 2026-05 o del programa original como aprobación del ZIP actual. Las matrices históricas se conservan por trazabilidad, no se reescriben ni se ponen en PASS.

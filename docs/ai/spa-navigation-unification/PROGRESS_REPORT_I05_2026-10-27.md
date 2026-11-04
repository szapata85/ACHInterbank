# Informe de avance — I05 | ACH Interbank / Navegación SPA y MFT

**Fecha:** 2026-10-27  
**Programa:** `NAV-UX-MFT-20261027`  
**Incremento cerrado a nivel de fuente:** `I05_COMPACT_A11Y_AND_LINEAGE`  
**Estado:** `STATIC_PASS_RUNTIME_PENDING`  
**Línea base verificada:** `ACHInterbank_NAV_UX_MFT_2026-10-27_I04A_PROYECTO_COMPLETO.zip`  
**SHA-256 padre:** `94e6d0c03576b26a9ff34cd32a070679b6e49261f7facb2efe007aca8d2aabd0`

## 1. Hallazgo de continuidad

La entrega `I04A` **no incluyó nuevo código funcional**: al compararla contra `I04`, se conservaron los 4.470 archivos del ZIP anterior; solo se agregaron dos documentos y se modificaron cuatro archivos documentales. Presentarla como un avance funcional resultaba incorrecto. Esta entrega `I05` continúa expresamente **desde I04A** y realiza una corrección puntual nueva.

La línea base de seguridad documentada en `MEMORY.md` como SEC-I06A continúa preservada; el programa `NAV-UX-MFT` no debe confundirse con la numeración de incrementos de seguridad `SEC-I05/I06`.

## 2. Avance realizado en I05

| Frente | Trabajo de este incremento | Estado | Evidencia |
| --- | --- | --- | --- |
| UI menú compacto | `isItemExpanded` representa expansión realmente visible; conserva expansión interna para restauración | Implementado | `main-layout.component.ts`, `.html` |
| Accesibilidad | No anunciar `aria-expanded=true` cuando el subárbol se oculta al compactar escritorio | Implementado | `main-layout.component.spec.ts` nueva prueba |
| Validación offline | 3 nuevos invariantes de accesibilidad/plantilla/test (16 → 19) | **PASS 19/19** | `scripts/validate-spa-navigation-mft-unification.py` |
| Sintaxis TS | Transpilación del componente, test, rutas y servicio de navegación | **PASS 4/4** (solo sintaxis) | `typescript.transpileModule`, compilador global Node |
| Linaje de incrementos | Nueva herramienta que exige SHA-256 del padre y detecta archivos faltantes | **PASS** | `scripts/verify-incremental-source-lineage.py`; `LINEAGE_VERIFICATION_I05.json` |
| Pruebas del gate de linaje | Casos de padre correcto con modificaciones, SHA inválido y archivo perdido | **PASS 3/3** | `python -m unittest scripts/test/test_incremental_source_lineage.py -v` |
| Integración MFT | No se alteró ruta, seed ni permisos; se conserva el avance I02 | Sin cambios en I05 | Diff del árbol padre |

**Preservación del padre:** 4.472/4.472 archivos conservados, 0 eliminados. Los cuatro archivos de código/validación del párrafo anterior fueron modificados y el verificador de linaje fue añadido, además de esta documentación.

## 3. Gates no ejecutados: no son PASS

| Prueba / entorno | Estado | Motivo |
| --- | --- | --- |
| Angular compilación/AOT | `NOT_RUN_ENVIRONMENT` | No hay `node_modules` completo; caché npm no contiene `zone.js@0.16.3` |
| Angular Karma/unit en navegador | `NOT_RUN_ENVIRONMENT` | Faltan dependencias Angular y ejecutor Chrome preparado |
| Playwright E2E | `NOT_RUN_ENVIRONMENT` | No hay SPA/API de prueba desplegada ni dependencias completas |
| .NET build/xUnit | `NOT_RUN_ENVIRONMENT` | SDK .NET no instalado |
| Seeder MFT/idempotencia y respuesta `/api/navigation/menu` | `PENDING_DEPLOYMENT` | Sin backend/BD/usuarios QA autorizados |
| Matriz de permisos MFT de solo lectura/escritura/no autorizado | `PENDING_DEPLOYMENT` | Requiere backend y perfiles de QA |

**Release:** `NOT_CERTIFIED / GO_DEPLOY=NO`. Las validaciones de fuente no sustituyen pruebas de integración ni de permisos. No se cambió la configuración monetaria, las autorizaciones IAM ni los contratos de integración.

## 4. Próximo trabajo permitido

1. Continuar **desde el ZIP I05**, no desde I04/I04A ni desde otro programa con nombre de incremento similar. Verificar primero el hash indicado en el archivo SHA-256 externo.
2. En entorno adecuado, ejecutar las pruebas Angular del layout, pruebas E2E del menú, .NET de seeders y autorización MFT; incluir evidencia real.
3. Corregir únicamente fallos nuevos reproducibles. No rehacer la unificación I01 ni la configuración MFT I02 sin nueva evidencia.
4. Tras cada incremento, nuevo ZIP **completo** + documentación + memoria Markdown + informe de avance + hash externo. Si las pruebas no pueden ejecutarse, seguir con cambios implementables y conservar sus estados pendientes.

El porcentaje global no se estima porque no hay evidencia de funcionamiento extremo a extremo.

# Evidencia de validaciones disponibles — I05

Todas las ejecuciones detalladas a continuación corresponden al árbol de fuentes I05 derivado del ZIP I04A. **No son pruebas de integración ni certifican la operación bancaria.**

## 1. Validación estática del menú

Comando: `python scripts/validate-spa-navigation-mft-unification.py`  
Resultado: **PASS 19/19**. Comprueba el patrón único de despliegue, ruta y read-guard MFT conservados, seeder/copy MFT y nuevas invariantes de visualización en compacto.

## 2. Sintaxis TypeScript

Comando: `typescript.transpileModule` (TypeScript de instalación global, Node).  
Resultado: **PASS 4/4** con diagnósticos 0. Archivos: `main-layout.component.ts`, `main-layout.component.spec.ts`, `app-routing.module.ts`, `navigation.service.ts`.

**Límite:** no se ha realizado Angular AOT, validación de templates ni typecheck del proyecto al carecer del árbol completo de dependencias.

## 3. Tests del verificador de procedencia

Comando: `python -m unittest scripts/test/test_incremental_source_lineage.py -v`  
Resultado: **PASS 3/3**. Escenarios: acepta cambios intencionales manteniendo fuente, rechaza un SHA-256 de ZIP padre incorrecto y rechaza ausencia de un archivo original.

## 4. Verificación sobre ZIP padre real

Comando: `python scripts/verify-incremental-source-lineage.py --parent-zip <ZIP I04A> --expected-parent-sha256 <SHA del ZIP I04A> --evidence docs/ai/spa-navigation-unification/LINEAGE_VERIFICATION_I05.json`  
Resultado: **PASS**; SHA padre `94e6d0c03576b26a9ff34cd32a070679b6e49261f7facb2efe007aca8d2aabd0`; 4.472/4.472 archivos de entrada preservados y 0 eliminados. La evidencia JSON excluye de su inventario su propio archivo generado para evitar autorreferencia.

## 5. No ejecutado

No se ejecutaron Angular CLI build, Karma, Playwright, dotnet/xUnit ni el despliegue/API/BD MFT porque faltan dependencias instaladas, SDK o infraestructura QA. Permanecen `NOT_RUN_ENVIRONMENT` y `PENDING_DEPLOYMENT`. No se afirma que el menú MFT sea visible en producción.

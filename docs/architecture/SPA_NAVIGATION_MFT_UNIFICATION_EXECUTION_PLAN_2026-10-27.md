# Plan de ejecución — unificación del menú SPA y publicación explícita de MFT

**Programa:** `NAV-UX-MFT-20261027`  
**Línea base:** ZIP recibido `ACHInterbank_ACH_CR_I06_FIX01_SEC_I06A_TESTS_PREPARATION_COMPLETO.zip`  
**Modalidad:** ChatGPT en modo chat, incrementos acotados, entrada/salida ZIP completo de fuentes y documentación.  
**Estado actual:** Implementación en fuentes terminada; pruebas de runtime pendientes.

## 1. Objetivo y alcance

Unificar el comportamiento de apertura de grupos en el SPA Angular, hacer visible e identificable la administración del intercambio de archivos MFT en el menú autorizado, y entregar el proyecto con historia verificable por incremento. **No** cambiar reglas ACH/CENIT/NACHA-M, autorizaciones bancarias, contratos API, políticas IAM ni semántica operativa de MFT.

### Hallazgos comprobados en el código del ZIP

| Hallazgo | Evidencia | Riesgo | Tratamiento |
| --- | --- | --- | --- |
| Grupo estructural: toda la fila es un botón de expansión; grupo funcional: enlace principal + botón de expansión estrecho | `main-layout.component.html` (baseline) | La misma zona visual hace cosas distintas; usuarios móviles/teclado dudan | Botón de fila completa para **todos** los grupos; destino funcional en acción secundaria |
| El SPA consume `GET api/navigation/menu` y ordena los elementos; no crea un menú estático | `navigation.service.ts` | Poner un enlace codificado en Angular no garantiza autorización ni visibilidad | Mantener backend/seed como fuente de verdad |
| MFT ya existe como ruta `/administracion/mft-ach` | `app-routing.module.ts`, `AchColombiaMftAdministrationComponent` | Se interpreta como funcionalidad ausente cuando el problema puede ser nombre, despliegue o permisos | Nombre explícito, conservar ruta |
| Menú MFT sembrado bajo `/integraciones`, hijo #40 | `AchColombiaMftAdministrationMenuSeeder`, `NavigationInformationArchitectureSeeder` | Puede no encontrarse por copy genérico o faltar en base desplegada | Reconciliar etiqueta del seed, verificar reseed/deployment |
| Visibilidad MFT requiere `Config.Read` **o** `CanReadAch` | `MftAdministrationMenuAuthorizationSeeder`, `ConnectionsStructuralMenuAuthorizationSeeder`, Angular guard | Un usuario no autorizado no debe ver el enlace aunque exista | No ampliar grants, roles ni permisos |

## 2. Arquitectura elegida

1. **Un patrón de interacción para todas las ramas:** clic/toque/Enter/Espacio en la fila abre o cierra el submenú. Un grupo con pantalla propia expone además un icono/enlace «Abrir página de…», con nombre accesible. La flecha comunica estado de expansión; no se navega accidentalmente al expandir.
2. **Árbol canónico dinámico:** `MenuItem`/`MenuItemPermission`/`MenuItemRole` → seeders backend → `GetMenuForCurrentUserHandler` → `api/navigation/menu` → `NavigationService` → `MainLayoutComponent`.
3. **MFT único y no duplicado:** `Administración de parámetros → Conexiones → Administración de transferencias de archivos (MFT)`, ruta original `/administracion/mft-ach`, orden 40. Se evita introducir otra entrada bajo `Administración` o una raíz artificial.
4. **Responsabilidades separadas:** visibilidad del menú por backend; ruta angular con guard; API con policy; operaciones de escritura MFT restringidas por permisos de gestión existentes. No inferir que ver equivale a administrar.
5. **Compatibilidad:** no alterar IDs, árbol de dominio fuera de la etiqueta MFT, rutas, endpoints, seed order, jerarquías de roles o lógica de compensación.

Ver `SPA_NAVIGATION_MFT_ARCHITECTURE_2026-10-27.md` para el flujo y las decisiones.

## 3. Incrementos ejecutables desde ChatGPT — modo chat

| Incremento | Acción | Artefactos/cambios | Estado a fecha de entrega | Evidencia esperada |
| --- | --- | --- | --- | --- |
| I00 — Baseline | Diagnosticar SPA, seed, autorización MFT y pruebas preexistentes | Auditoría de archivos y decisiones | **ANALIZADO** | Mapa de rutas/controles |
| I01 — Comportamiento | Cambiar todas las ramas a un solo patrón de apertura, sin perder navegación de grupos funcionales; desktop compacto/móvil intactos | `main-layout.component.html`, `.scss` | **IMPLEMENTADO, runtime pendiente** | Angular unit + E2E interacción/accesibilidad |
| I02 — MFT | Exponer etiqueta explícita en seed canónico y seeder funcional; alinear título/breadcrumb y textos de pantalla, conservar permisos | Seeder canónico, seeder MFT, router, vistas | **IMPLEMENTADO, runtime pendiente** | Seed doble, menú visible sólo con capacidad autorizada |
| I03 — Regresión | Ampliar pruebas Angular/.NET, verificación estructural offline e inventario de estados | `main-layout.component.spec.ts`, tests C#, validador Python, documentación | **IMPLEMENTADO, 16 comprobaciones estáticas PASS** | Compilación y suites ejecutadas cuando el ambiente las permita |
| I04 — Entrega | Documentar arquitectura, decisiones, plan, memoria por incremento, guía de ejecución y empaquetar fuentes | `docs/ai/spa-navigation-unification`, manifest, ZIP | **IMPLEMENTADO** | ZIP íntegro + checksums externos |

**Regla de continuidad:** si una prueba no puede ejecutarse, anotar `NOT_RUN_ENVIRONMENT` y no bloquear el siguiente incremento de implementación. No declarar `CLOSED` o `PASS` de runtime sin ejecución real; realizar las pruebas al final del paquete en un entorno preparado.

## 4. Matriz de aceptación

- Todo grupo estructural y funcional expande/contrae al activar la **misma superficie principal**; flecha y `aria-expanded` reflejan el estado.
- Los grupos con página de aterrizaje mantienen su destino, pero no navegan cuando se activa su botón de expansión.
- Los submenús siguen accesibles por teclado y ratón/tacto, también con hasta tres niveles, incluso al pasar a escritorio compacto o móvil.
- El drawer móvil permanece abierto al expandir, cierra al navegar a una ruta y permite Escape/backdrop.
- El ítem MFT autorizado aparece **una sola vez** como hijo de «Conexiones» y llega a `/administracion/mft-ach`.
- El usuario con `Config.Read` o `CanReadAch` puede ver MFT si obtiene el árbol ancestral; el usuario sin ambas capacidades no lo ve ni pasa el guard SPA. Las operaciones modificadoras requieren sus permisos independientes.
- Ningún cambio a permisos, RolePermissions, perfiles, seeders de seguridad, endpoints o contratos externos.
- Identidad histórica de `NAV-MENU-IA-001` preservada; `NAV3.5C` sigue bloqueado.

## 5. Ejecución final de pruebas (sin detener implementación)

Desde la raíz del proyecto:

```bash
python scripts/validate-spa-navigation-mft-unification.py
cd web/ach-interbank-ui
npm ci
npm run build
npm test -- --watch=false --browsers=ChromeHeadless --include=src/app/layout/main-layout.component.spec.ts
npm run e2e -- --project=chromium e2e/navigation-and-login-icons-regression.spec.ts
cd ../..
dotnet test ACHInterbank.sln --filter 'FullyQualifiedName~NavigationMenuNav34NConnectionsNavigationTests|FullyQualifiedName~NavigationMenuNav34MMftAdministrationAuthorizationTests|FullyQualifiedName~NavigationMenuNav22ParametersInformationArchitectureTests'
```

La ejecución E2E requiere frontend/API y datos de prueba preparados conforme a su configuración Playwright. `npm ci` requiere disponibilidad del lockfile y paquetes. Los comandos .NET requieren SDK/restauración y acceso a dependencias. Registrar salida real por comando; cualquier imposibilidad se clasifica como `NOT_RUN_ENVIRONMENT`, nunca como PASS.

## 6. Publicación, rollback y control de riesgos

1. Respaldar base y menú actual (consulta de `MenuItems`, `MenuItemPermissions`, `MenuItemRoles`); confirmar que desplegará el ensamblado que ejecuta seeders.
2. Desplegar backend con reconciliación de copy MFT y frontend; **re-ejecutar seeders idempotentes** siguiendo el procedimiento del repositorio para materializar etiquetas en bases existentes.
3. Consultar con usuario autorizado `GET api/navigation/menu`; verificar la rama `Conexiones` y la ruta MFT. Confirmar `Config.Read`/`CanReadAch` sólo mediante claims autorizados y auditoría, sin entregar permisos de prueba.
4. Verificar usuario no autorizado y rechazo por URL directa; validar también con permisos sólo de lectura y sólo de gestión para detectar discrepancias.
5. Si hay regresión, revertir frontend/backend a la pareja de versiones anterior y re-ejecutar seed anterior según el procedimiento de rollback de datos; no asumir que revertir Angular revierte etiquetas sembradas en base.
6. No desactivar políticas ni conceder grants para «arreglar» visibilidad; investigar perfil/capacidad y si seed real se ejecutó.

## 7. Límites y decisiones pendientes

- Sin una base productiva ni sesión real no puede verificarse cuál de los factores (no desplegado, permisos o interacción) ocultaba MFT al usuario original. En **fuentes** la entrada ya existía; no afirmar que estaba ausente del backend de producción.
- La convención de menú preexistente mantiene MFT bajo **Conexiones**, no como una raíz separada ni duplicada.
- Cualquier revisión de IAM, concesión de acceso MFT o traslado de grupo a «Administración» debe autorizarse como un cambio posterior con pruebas de seguridad; no forma parte de este incremento.

## Implementación posterior I05 — avance efectivo sin reiniciar trabajo previo

I04A fue exclusivamente documental. I05 deriva del ZIP I04A (SHA-256 verificado) y corrige el estado de accesibilidad `aria-expanded` de ramas cuyos submenús están ocultos por la navegación de escritorio compacta. Se añadieron una prueba Angular de regresión (escrita, no ejecutada), tres invariantes estáticos y el script de control de linaje. Los gates de fuente pasaron (19/19); siguen pendientes build/unit/E2E Angular, xUnit, seed e inspección de permisos MFT. El siguiente paso es certificación de ejecución en el entorno permitido, sin rehacer I01/I02. Ver reporte I05 y memoria del programa.

## Incremento I06 — contraste hover/foco (implementado en fuente; pendiente navegador)

- Aislar botones `[mat-list-item]` frente a estilos de botones genéricos.
- Mantener CSS de texto/icono/fondo del sidebar coherente en `:hover` y `:focus-visible`.
- Añadir E2E para contrastes AA con temas y estados interactivos (prueba incorporada, no ejecutada).
- Entregar ZIP completo I06, memoria, avance, matriz de pruebas, linaje y SHA externo; no certificar ejecución sin evidencia.

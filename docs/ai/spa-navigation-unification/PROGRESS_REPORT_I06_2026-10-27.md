# Avance incremental I06 — Corrección de contraste hover del menú SPA

**Programa:** `NAV-UX-MFT-20261027`  
**Fecha de implementación:** 2026-10-27  
**Tipo de incremento:** **funcional (estilos CSS) + nueva regresión E2E**  
**Estado:** `SOURCE_IMPLEMENTED_STATIC_PASS_RUNTIME_PENDING`  
**Línea base de entrada:** `ACHInterbank_NAV_UX_MFT_2026-10-27_I05_PROYECTO_COMPLETO.zip`  
**SHA-256 entrada verificado:** `ce27bd3b3e062211fb8d11964fde30246a7627a77931a7d556ebaf4cc20da512`  
**ZIP de salida:** `ACHInterbank_NAV_UX_MFT_2026-10-27_I06_PROYECTO_COMPLETO.zip`

## Diagnóstico real

El sidebar (`main-layout.component.scss`) usa una paleta específica derivada de `--brand-sidebar-background` y `--brand-sidebar-on-background`. No obstante, la regla global `button:not(.mat-mdc-button-base):hover` (`src/styles/_components.scss`) se aplica también a elementos `button[mat-list-item]` que no necesariamente llevan la clase `.mat-mdc-button-base`. Esa regla les asigna un fondo de botón primario (`--btn-bg: var(--color-primary-hover)`) y un color de botón primario que **no pertenecen a la paleta del sidebar**. El texto `.nav-label` continuaba usando el color claro/oscuro del sidebar. La combinación podía hacer que la etiqueta fuera ilegible o casi invisible al pasar el mouse, sobre todo con colores corporativos personalizados de alto brillo.

**Alcance de evidencia:** conflicto identificado por inspección de selectores/estilos; **no** se obtuvo reproducción en un navegador conectado al backend, por lo que no se afirma haber observado el fallo en ejecución.

## Cambios de I06 — sin rehacer I01–I05

| Archivo | Cambio | Resultado |
| --- | --- | --- |
| `web/ach-interbank-ui/src/styles/_components.scss` | Excluir `button[mat-list-item]` de las seis reglas del botón genérico, incluyendo base, hover, active, foco y disabled | Evita que el color primario reemplace el fondo de los controles de navegación |
| `web/ach-interbank-ui/src/app/layout/main-layout.component.scss` | Contraste coherente de `color`, fondo y tokens Material para `:hover` y `:focus-visible` | Texto e iconos usan colores del sidebar para estados interactivos |
| `web/ach-interbank-ui/e2e/navigation-sidebar-hover-contrast.spec.ts` | E2E real de tema claro/oscuro, color primario claro, rama expandida, subopción hover/focus/active, contraste WCAG AA ≥4.5:1 | Prueba añadida; ejecución pendiente del entorno |
| `scripts/validate-spa-navigation-mft-unification.py` | Cuatro comprobaciones de fuente adicionales (19 → 23) | PASS 23/23 |

Se preservan rutas de navegación, permisos de MFT, políticas backend, estructura del menú, endpoints y reglas bancarias. No se reclasifica el MFT ni se duplica en otro lugar.

## Evidencia disponible

| Gate | Estado | Alcance |
| --- | --- | --- |
| Contrato estático navegación+hover | **PASS 23/23** | Script de invariantes; no renderiza el SPA |
| TypeScript transpilation/syntax | **PASS 4/4** | `main-layout.component.ts`, su spec, `branding.service.ts` y nuevo E2E; no typecheck semántico |
| Test de contraste Playwright | **NOT_RUN_ENVIRONMENT** | Requiere SPA/API local, usuarios QA y dependencias Angular/Playwright |
| Angular build/Unit | **NOT_RUN_ENVIRONMENT** | No hay dependencias completas (`node_modules`) |
| .NET y autorizaciones MFT | **NOT_RUN_ENVIRONMENT** | SDK .NET/DB/usuarios QA no preparados |
| Aprobación para producción | **NO** | E2E y autorización pendientes |

## Reproducción y QA final

1. Iniciar SPA y API locales autorizadas; configurar `ACH_USER`/`ACH_PASS` del entorno QA (sin registrar credenciales).
2. Compilar, ejecutar unitarias del layout y luego `npx playwright test e2e/navigation-sidebar-hover-contrast.spec.ts` desde `web/ach-interbank-ui`.
3. Inspeccionar textos e iconos de grupos y hojas con hover, foco, seleccionados, expandidos y niveles anidados; ejecutar con branding claro/oscuro y botón primario claro. Capturar imágenes de QA y comprobar ≥4.5:1.
4. Validar también menú compacto/móvil y que MFT aparezca únicamente para los roles autorizados.

**Siguiente incremento autorizado:** I07, únicamente para correcciones derivadas de ejecución real o QA. No marcar el menú como 100% estabilizado antes de dicha evidencia.

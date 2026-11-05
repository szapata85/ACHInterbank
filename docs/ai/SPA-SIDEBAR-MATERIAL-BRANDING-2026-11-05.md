# Corrección incremental del menú lateral global — ACH Interbank

**Fecha:** 2026-11-05  
**Base:** `ACHInterbank_MFT_ERROR_CONTROLADO_PROYECTO_COMPLETO_2026-11-05.zip`  
**Estado:** fuente implementada, validación estática ejecutada; compilación y pruebas en navegador pendientes.

## Hallazgo reportado

La captura de `/dashboard` muestra un menú lateral con jerarquía poco clara, iconos de interrogación, flecha de navegación separada del chevrón de grupo, etiquetas de contraste irregular y un orden inesperado (`Seguridad NACHA` antes de `Inicio`). La corrección debe abarcar el shell global, no una pantalla particular.

## Causa y separación de responsabilidades

1. **Material vs. estilos del SPA:** el anterior shell usaba botones y enlaces `mat-list-item` con varias capas de tokens y elementos internos. El layout se reemplazó por acciones `mat-button` de ancho completo, con color de texto/icono y estados definidos exclusivamente por los tokens del sidebar.
2. **Doble acción de grupo:** anteriormente los padres funcionales presentaban un botón de expansión y una flecha adicional a la derecha. Ahora cada fila de grupo *únicamente despliega*, y su enlace «Ver [nombre]» aparece dentro del submenú. Padres estructurales continúan sin enlace falso.
3. **Iconos con interrogación:** `UiIconComponent` usaba `help` para claves desconocidas. Añadimos símbolos válidos y el parámetro opcional `fallbackIcon`; el sidebar usa `folder` y `list` como alternativas neutras, sin reescribir el icono persistido. Fuera del sidebar se conserva el fallback histórico `help`.
4. **Orden y lenguaje persistidos:** `NavigationService` consume y ordena por el `order` recibido de `api/navigation/menu`. El backend (NavigationInformationArchitectureSeeder) define los nueve dominios raíz, empezando por `/dashboard`/`Inicio` orden 10 y `/nacha-security`/`Seguridad de archivos` orden 60. El screenshot con «Seguridad NACHA» al principio indica **posible desalineación del menú de SQL Server**, compatible con arranques `--no-seed`; no es prueba definitiva sin inspeccionar el árbol persistido. No se cambió ni se camufló desde Angular la fuente de verdad de orden/etiqueta/permisos.

## Cambios de UX

- Navegación limpia con una sola acción por fila, espaciamiento homogéneo, indicadores alineados y contraste definido por `--color-sidebar`, `--color-on-sidebar`, `--color-primary` y `--color-focus`.
- Estado activo diferencia la página seleccionada de sus ancestros; hover, focus y estados desplegados con colores de la identidad dinámica.
- Búsqueda accesible de opciones autorizadas (ignora mayúsculas y tildes), sin nuevas peticiones HTTP, sin añadir ni ocultar permanentemente permisos. Permite abrir/cerrar grupos filtrados y recuperar el estado original al limpiar.
- Modo compacto y menú móvil existentes conservados; Escape cierra el menú móvil y devuelve el foco al botón de apertura.
- Elementos que no tienen ícono configurado muestran un símbolo neutro, nunca un hueco.
- Ninguna ruta, API, autorización, seeder, esquema, algoritmo ACH/CENIT o contrato de negocio fue alterado.

## Verificación efectuada

- `python scripts/validate-sidebar-ux-static.py`: **31/31 PASS**.
- Análisis sintáctico con TypeScript de los cinco archivos TS afectados: **5/5 PASS** (no reemplaza el compilador Angular).
- Análisis estructural de tags HTML: **sin etiquetas desbalanceadas**.
- Especificaciones Jasmine y Playwright ampliadas para cubrir filtrado, expansión, enlaces de padres funcionales, fallback de iconos, contraste >= 4.5:1, compacto, móvil y Escape; **no ejecutadas** por ausencia de `node_modules` y acceso a las dependencias en este entorno.
- Se conserva el E2E previo de contraste del sidebar con dos variaciones de branding.

## Aceptación pendiente en el equipo de destino

1. Ejecutar desde el proyecto actualizado `npm ci`, `npm run build` y la suite focal de `MainLayoutComponent`, `UiIconComponent` y `navigation-sidebar-hover-contrast.spec.ts`.
2. Reconstruir Docker **sin `--no-build`**; validar `/dashboard` en 1366×768, 1440×900, 390×844 y con zoom 200%.
3. Con un menú autorizado real, comprobar que teclas Tab, Enter, Espacio y Escape funcionen en enlaces y grupos; que el foco sea visible y el grupo activo esté abierto.
4. Verificar contraste de textos y estados (normal, hover, focus, activo, abierto, deshabilitado) con sidebar claro y oscuro; objetivo WCAG AA 4.5:1 para texto normal, y estado visible no dependiente solo de color.
5. Verificar menús amplios, anidamiento de 3 niveles, cadenas largas, scrollbar vertical sin overflow horizontal y pantalla compacta.
6. Verificar permisos con usuarios de consulta/administración y con el permiso de herramientas de prueba ausente. La búsqueda no debe saltarse el filtro del backend.
7. Revisar la configuración **persistida** en `/navigation/menu-items` con usuario autorizado. Canonical raíz: Inicio 10, Operación 20, Clientes 30, Seguimiento y control 40, Administración de parámetros 50, Seguridad de archivos 60, Reportes 70, Administración 80, Herramientas de prueba 90. Cualquier ajuste debe hacerse por la administración autorizada o un proceso de reconciliación controlado con respaldo y evaluación de impacto; **no ejecutar siembra general ni SQL manual a ciegas**.

## Notas para Docker

`docker-ach.cmd up --no-seed --no-auto-migrations` reconstruye los contenedores, pero **omite la reconciliación del árbol de navegación de la base de datos**. Quitar `--no-build` sirve para los cambios CSS/HTML/TS. Resolver los rótulos/orden previos exige revisar el `MenuItem` persistido por otra vía. El presente incremento no modifica datos de menú.

**No atribuir a pruebas estáticas el estado de validado en producción o aceptado por usuario.**

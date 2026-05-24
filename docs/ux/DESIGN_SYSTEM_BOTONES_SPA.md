# Design system minimo de botones SPA

Fecha: 2026-05-23

## Regla base

Ningun boton de accion principal en la SPA debe quedar blanco/ilegible o sin contraste suficiente.

## Variantes

| Variante | Uso | Regla visual |
|---|---|---|
| Primario | Accion principal de la vista, por ejemplo `Buscar` | Fondo primario, texto blanco, borde primario, foco visible |
| Secundario | Accion auxiliar reversible, por ejemplo `Limpiar` | Fondo claro, texto oscuro, borde visible suficiente |
| Exportar | Acciones de documento/exportacion, por ejemplo `Exportar PDF` | Fondo verde institucional oscuro, texto blanco, borde visible |
| Peligro | Acciones destructivas si aplica | Fondo rojo/alerta, texto blanco, confirmacion si corresponde |
| Disabled | Accion temporalmente no disponible | Mantener legibilidad con opacidad controlada, cursor no permitido |
| Loading | Accion en proceso | Texto claro de progreso y doble click bloqueado |
| Icon-only | Acciones compactas | Debe tener `aria-label` y foco visible |

## Aplicacion en reportes

Las rutas `/reports/*` usan botones de filtros con variantes:

- `Buscar`: primario.
- `Limpiar`: secundario.
- `Exportar PDF`: exportar/documento.

La correccion se aplico en `ReportListPageComponent` sin cambiar comportamiento funcional ni endpoints.

Productivo: NO-GO.

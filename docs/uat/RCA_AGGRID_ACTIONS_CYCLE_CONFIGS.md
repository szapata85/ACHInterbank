# RCA - Acciones AG Grid en `/transactions/cycle-configs`

## 1. Resumen del incidente

Los botones de Acciones en la pantalla `/transactions/cycle-configs` no ejecutaban las acciones Editar, Clonar e Inactivar durante la prueba manual en la SPA servida por Docker/Nginx en `localhost:743`.

## 2. Impacto

La operación de administración de configuraciones de ciclos por cámara quedaba bloqueada desde la interfaz, aunque la consulta de datos y el backend funcionaban correctamente.

## 3. Alcance afectado

* Pantalla: `/transactions/cycle-configs`
* Componente: `CycleConfigManagementComponent`
* Funciones afectadas visualmente:
  * Editar
  * Clonar
  * Inactivar

## 4. Causa raíz

La SPA servida por Docker/Nginx en `localhost:743` estaba ejecutando una imagen vieja distinta al build local esperado.
El contenedor publicado no contenía el renderer robusto más reciente de la columna Acciones.

## 5. Evidencia del problema

Antes de la recreación completa de la imagen/contenedor, el DOM servido mostraba el renderer anterior:

* No existía `.cycle-config-actions`.
* No existía `data-testid="cycle-config-action-edit"`.
* No existía `data-testid="cycle-config-action-clone"`.
* No existía `data-testid="cycle-config-action-inactivate"`.
* La celda Acciones seguía renderizando botones dentro de un wrapper `<span>`.

## 6. Elementos descartados

Durante el diagnóstico se descartó que el problema estuviera en:

* Backend
* PostgreSQL
* Permisos
* `onCellClicked` de AG Grid
* `NgZone`
* Renderer robusto
* Selectores de Playwright
* API de configuración de ciclos

## 7. Corrección aplicada

Se forzó la recreación completa de la SPA servida por Docker:

```bash
docker compose stop achinterbank-spa
docker compose rm -f achinterbank-spa
docker image rm achinterbank-spa:local
docker compose build --no-cache --progress=plain achinterbank-spa
docker compose up -d achinterbank-spa
```

## 8. Validación posterior

Después de recrear imagen y contenedor:

* El DOM final contiene `.cycle-config-actions`.
* El DOM final contiene `data-testid` para Editar, Clonar e Inactivar.
* El DOM final contiene `aria-label` y `data-action`.
* Playwright confirmó:
  * Editar abre el formulario y carga el nombre del ciclo.
  * Clonar abre el formulario y deja el nombre terminado en `-V2`.
  * Inactivar abre el diálogo de confirmación sin confirmar la operación.

## 9. Conclusión

El incidente fue causado por una desalineación de despliegue/identidad de imagen Docker, no por un defecto funcional del código Angular ni del backend.

## 10. Acción preventiva recomendada

Para futuras validaciones locales con SPA Docker, cuando se sospeche que el contenedor no refleja el código actual, validar:

```bash
docker compose ps
docker ps --format "table {{.Names}}\t{{.Image}}\t{{.Ports}}\t{{.CreatedAt}}"
docker inspect achinterbank-spa --format "Container={{.Name}} Image={{.Image}} Created={{.Created}}"
docker image inspect achinterbank-spa:local --format "ImageId={{.Id}} Created={{.Created}}"
```

Y confirmar dentro del contenedor que el build contiene los cambios esperados:

```bash
docker exec -it achinterbank-spa sh -lc "grep -R 'cycle-config-action-edit' /usr/share/nginx/html || true"
docker exec -it achinterbank-spa sh -lc "grep -R 'cycle-config-actions' /usr/share/nginx/html || true"
```

## 11. Estado final

Cerrado.
No se requieren cambios adicionales de código para este incidente.

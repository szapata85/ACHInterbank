# Arquitectura objetivo + sistema visual enterprise para SPA Angular ACH Interbank

Fecha: 2026-04-16  
Ámbito: Toda la SPA Angular (backoffice bancario ACH/CENIT)

---

## 0) Principios de diseño obligatorios

1. **Operación crítica primero**: toda interacción de impacto transaccional debe ser segura, trazable y resistente a duplicidad.
2. **Consistencia por encima de velocidad local**: ningún módulo define su propio patrón visual o de interacción fuera del design system.
3. **Español 100%**: textos de usuario exclusivamente en español financiero homogéneo.
4. **Una sola estrategia de formularios**: formularios reactivos obligatorios.
5. **Una sola estrategia de grids**: AG-GRID exclusivo mediante wrapper corporativo.
6. **Escalabilidad mantenible**: separación de responsabilidades, contratos explícitos, bajo acoplamiento.

---

## 1) Arquitectura frontend objetivo (entregable A + 1)

## 1.1 Estructura de carpetas y capas

```text
src/app/
  core/
    auth/
    http/
      interceptores/
      errores/
    i18n/
    seguridad/
    trazabilidad/
    configuracion/
  layout/
    shell-principal/
    shell-autenticacion/
    page-shell/
  shared/
    utilidades/
    tipos/
    constantes/
    validadores/
  ui/
    design-system/
      boton/
      campo-texto/
      campo-moneda/
      selector-buscable/
      alerta/
      estado-vacio/
      estado-carga/
      migas-pan/
      modal/
      tarjeta/
      encabezado-pagina/
      grilla-empresarial/      <- wrapper AG-GRID único
    patrones/
      accion-asincrona/
      formulario-seccionado/
      filtros-grilla/
  features/
    transacciones/
      aplicacion/
      dominio/
      infraestructura/
      presentacion/
        paginas-contenedor/
        componentes-presentacionales/
    ciclos/
    integraciones/
    cenit/
    reportes/
    usuarios/
    branding/
```

## 1.2 Convención container vs presentational

- **Contenedor (página)**: orquesta caso de uso, ruteo, permisos, estado de carga/error, integración con servicios.
- **Presentacional (UI pura)**: recibe `@Input()`, emite `@Output()`, sin llamadas HTTP ni reglas de negocio.
- **Regla estricta**: ningún componente UI debe conocer endpoints o modelos de infraestructura.

## 1.3 Formularios reactivos como estándar único

- Prohibido `ngModel` en features de negocio.
- Cada formulario se compone por:
  - `FormGroup` tipado.
  - validadores sincrónicos y asíncronos centralizados en `shared/validadores`.
  - mapa de errores reusable (texto homogéneo y en español).
- Estados obligatorios de todo formulario:
  - inicial,
  - edición,
  - inválido,
  - enviando,
  - éxito,
  - error recuperable.

## 1.4 Servicios por dominio

Patrón por feature:

- `...-api.service.ts`: solo transporte HTTP, DTOs y mapping básico.
- `...-repository.service.ts`: adaptación entre API y modelo de dominio.
- `...-use-cases.service.ts`: reglas de orquestación de negocio para UI.
- `...-store.service.ts` (opcional): estado local de feature (filtros, selección, paginación, cache).

## 1.5 Manejo de errores estandarizado

- **Interceptores globales**:
  - autenticación,
  - trazabilidad de request,
  - error técnico,
  - loading global.
- **Catálogo único de errores** (`core/http/errores`):
  - códigos backend -> mensaje de negocio en español,
  - severidad,
  - acción sugerida.
- **Regla**: servicios de feature no deben duplicar `catchError` para casos ya cubiertos globalmente.

## 1.6 Convención de rutas

- Idioma único español y semántica de negocio.
- Formato:
  - `/inicio`
  - `/transacciones/listado`
  - `/transacciones/crear`
  - `/transacciones/lotes/seguimiento`
  - `/integraciones/mapeos`
- Convención:
  - minúsculas,
  - separador `-` solo cuando aplique,
  - sin mezcla inglés/español.

## 1.7 Convención de nombres

- **Archivos**: kebab-case en español funcional.
- **Clases TS**: PascalCase técnico, pero semántica de dominio en español cuando represente UI/negocio.
- **Constantes de textos UI**: centralizadas en catálogos por dominio (i18n monolingüe español).

## 1.8 Page Shell estándar

Toda página operativa debe componerse con esta secuencia:

1. `encabezado-pagina` (título + descripción + acciones primarias)
2. `migas-pan`
3. bloque de filtros (si aplica)
4. contenido principal (grilla/formulario/paneles)
5. estados (`cargando`, `vacío`, `error`, `sin-permisos`)

## 1.9 Patrón async con control anti doble submit

Crear patrón reusable `patron-accion-asincrona` con contrato:

- `ejecutar(accion, opciones)`
- bloqueo automático de CTA mientras la promesa/observable está activo.
- estrategia anti reentrada:
  - `exhaustMap` para submit de formularios,
  - `throttle` opcional para botones de navegación.
- salida estándar:
  - `procesando` (boolean),
  - `exito`,
  - `errorControlado`,
  - `errorTecnico`.

---

## 2) Design System global (entregable B + 2 + 5)

## 2.1 Fundaciones visuales

- **Paleta**: neutros fríos + azul institucional + semánticos de estado (éxito/advertencia/error).
- **Tipografía**: una sola familia sans de alta legibilidad; escala de 12/14/16/20/24.
- **Espaciado**: sistema 4-8-12-16-24-32.
- **Bordes/sombras**: suaves, sobrios, sin efectos decorativos innecesarios.

## 2.2 Botones (obligatorios)

Variantes:
- `primario`
- `secundario`
- `contorno`
- `fantasma`
- `peligro`
- `icono`
- `cargando`

Estados por variante:
- reposo, hover, foco visible, activo, deshabilitado, cargando.

Reglas:
- CTA principal único por bloque.
- Texto de acción verbal clara: “Guardar”, “Procesar lote”, “Aprobar”.
- En `cargando`: spinner + texto explícito (“Procesando…”).

## 2.3 Inputs

Componentes base:
- `campo-texto`
- `campo-numero`
- `campo-moneda`
- `campo-fecha`
- `campo-textarea`
- `campo-busqueda`

Contrato común:
- label,
- ayuda,
- validación,
- error,
- estado deshabilitado,
- estado lectura.

## 2.4 Selects

Componente único: `selector-buscable`.

Capacidades mínimas:
- búsqueda local/remota,
- limpiar selección,
- carga asíncrona,
- estado sin resultados,
- teclado accesible,
- plantillas para opción y seleccionado.

## 2.5 Otros componentes DS

- `tarjeta`
- `encabezado-pagina`
- `migas-pan`
- `alerta` (info/éxito/advertencia/error)
- `estado-vacio`
- `estado-carga`
- `modal-confirmacion`
- `panel-metricas`
- `etiqueta-estado` (chips)

## 2.6 Tono visual objetivo

- sobrio,
- profesional,
- ligero,
- consistente,
- alineado a plataforma financiera de operación diaria.

---

## 3) Estándar obligatorio AG-GRID (entregable C + 3)

## 3.1 Regla corporativa

- **AG-GRID exclusivo** para cualquier listado/tabla de negocio.
- Prohibido: tablas HTML para grids funcionales y mezcla de librerías.

## 3.2 Wrapper corporativo único: `grilla-empresarial`

Entradas mínimas:
- `columnas`
- `datos`
- `modoPaginacion` (cliente/servidor)
- `estadoCarga`
- `estadoError`
- `accionesFila`
- `filtroGlobal`

Salidas:
- `eventoPagina`
- `eventoOrden`
- `eventoFiltro`
- `eventoAccionFila`
- `eventoSeleccion`

## 3.3 Funcionalidad obligatoria

- ordenamiento,
- filtros por columna,
- búsqueda global,
- paginación,
- acciones por fila,
- exportación configurable (si política lo permite),
- estados loading/empty/error consistentes,
- persistencia de preferencias de columnas por usuario (ancho, orden, visibles).

## 3.4 Performance de grillas

- virtualización habilitada por defecto.
- para alto volumen: **server-side row model**.
- estrategias:
  - debounce de filtros,
  - caché de páginas,
  - renderizadores livianos,
  - evitar pipes costosos por celda.

## 3.5 UX de grillas

- barra superior estándar:
  - búsqueda,
  - filtros rápidos,
  - acciones masivas,
  - contador de resultados.
- columna de acciones fija a la derecha con permisos aplicados.

---

## 4) UX/UI objetivo de plataforma (entregable D + 4)

## 4.1 Modelo de experiencia

“**Backoffice bancario moderno de alta densidad controlada**”:
- lectura rápida,
- mínima ambigüedad,
- foco en operación,
- feedback continuo.

## 4.2 Dashboards operativos

Cada dashboard debe incluir:
- KPIs de operación diaria (volumen, éxito/fallo, pendientes, SLA),
- alertas prioritarias,
- accesos rápidos a acciones críticas,
- trazabilidad por cortes/ciclos.

## 4.3 Formularios complejos

- secciones colapsables por dominio,
- resumen lateral en tiempo real,
- validación incremental,
- mensajes de error accionables,
- confirmación explícita en acciones irreversibles.

## 4.4 Navegación y trazabilidad

- menú por dominios de negocio,
- migas de pan consistentes,
- cabecera de página uniforme,
- acceso rápido a histórico y detalle trazable.

---

## 5) Protección transaccional reusable (entregable E)

## 5.1 Patrón obligatorio

`directiva/boton-procesando` + `servicio-accion-asincrona`:

- bloquea doble click,
- muestra spinner + texto,
- impide abandonar formulario en proceso (cuando aplique),
- reintento controlado,
- registro de correlación para auditoría.

## 5.2 Matriz de criticidad

- Nivel 1 (alto riesgo): creación de transacción, envío de lote, reversos, aprobaciones.
- Nivel 2: edición de catálogos críticos.
- Nivel 3: acciones informativas.

Cada nivel define:
- confirmación requerida,
- timeout,
- política de reintento,
- mensajería de error.

---

## 6) Política obligatoria de idioma español (entregable F)

## 6.1 Regla

Toda cadena visible al usuario debe estar en español, sin excepciones.

Incluye:
- títulos,
- labels,
- botones,
- tablas,
- tooltips,
- placeholders,
- mensajes de validación/error,
- breadcrumbs,
- estados de carga/vacío.

## 6.2 Gobernanza de idioma

- diccionario único en `core/i18n/es.ts` (aunque sea monolingüe).
- linters/reglas de revisión para bloquear textos hardcodeados en inglés.
- checklist de PR: “0 cadenas en inglés visibles al usuario”.

## 6.3 Taxonomía financiera estandarizada

Ejemplo de términos únicos:
- “Transacción”, no “Operación”/“Transaction” mezclado sin criterio.
- “Lote”, “Ciclo”, “Cámara”, “Devolución”, “Rechazo”, “Trazabilidad”.

---

## 7) Lista de componentes base inicial (entregable 5)

1. `ui-boton`
2. `ui-campo-texto`
3. `ui-campo-moneda`
4. `ui-selector-buscable`
5. `ui-alerta`
6. `ui-estado-carga`
7. `ui-estado-vacio`
8. `ui-modal-confirmacion`
9. `ui-encabezado-pagina`
10. `ui-migas-pan`
11. `ui-grilla-empresarial` (wrapper AG-GRID)
12. `ui-panel-metricas`
13. `ui-etiqueta-estado`
14. `ui-barra-filtros`
15. `ui-resumen-transaccional`

---

## 8) Estrategia de migración (entregable 6)

## 8.1 Estrategia general

Migración incremental por vertical funcional, sin “big bang”.

Orden recomendado:
1. Transacciones (máxima criticidad)
2. Ciclos y CENIT operativo
3. Integraciones
4. Reportes
5. Catálogos/administración

## 8.2 Plan técnico por módulo

Por cada módulo:
1. congelar nuevos patrones legacy,
2. migrar layout a `page-shell` estándar,
3. migrar formularios a reactivos puros,
4. reemplazar tablas por `ui-grilla-empresarial`,
5. mover textos a diccionario español,
6. aplicar patrón async anti doble submit,
7. pruebas funcionales + visuales + accesibilidad mínima.

## 8.3 Criterio de salida por módulo

Un módulo se considera migrado si cumple:
- 100% español,
- 0 tablas HTML de negocio,
- 100% formularios reactivos,
- acciones críticas protegidas,
- uso exclusivo de componentes DS.

---

## 9) Roadmap por fases (entregable 7)

## Fase 0 (2 semanas): Gobierno y bases

- aprobar arquitectura objetivo,
- definir norma de idioma,
- cerrar catálogo DS mínimo,
- crear wrapper AG-GRID,
- crear patrón async reusable.

## Fase 1 (4-6 semanas): Núcleo transaccional

- migrar transacciones y lotes,
- unificar formularios y grids,
- introducir trazabilidad UX completa,
- erradicar inglés en dominios críticos.

## Fase 2 (4-6 semanas): Operación ACH/CENIT

- migrar ciclos y operación CENIT,
- paneles operativos + alertas,
- mejorar performance en grillas de alto volumen.

## Fase 3 (4-8 semanas): Integraciones/reportes/admin

- homologar módulos restantes,
- consolidar design system,
- cerrar deuda visual y semántica.

## Fase 4 (continuo): Excelencia operativa

- auditorías trimestrales UX/UI,
- monitoreo de performance frontend,
- control de regresión visual,
- hardening de accesibilidad y seguridad UX.

---

## 10) KPIs de éxito (operativos y técnicos)

- 0 textos en inglés visibles al usuario.
- 0 acciones críticas sin bloqueo anti doble submit.
- 100% grids de negocio en AG-GRID wrapper.
- reducción >= 40% de incidencias UI por inconsistencia.
- reducción >= 30% en tiempo de ejecución de tareas operativas clave.
- cumplimiento de checklist DoD enterprise en > 95% de PR.

---

## 11) Decisiones ejecutivas requeridas

1. Aprobar formalmente la regla de **español total**.
2. Prohibir nuevas tablas HTML en negocio.
3. Nombrar responsable de arquitectura frontend transversal.
4. Definir comité de diseño/UX con autoridad de estándar.
5. Condicionar despliegues de módulos críticos al cumplimiento de patrón anti duplicidad.

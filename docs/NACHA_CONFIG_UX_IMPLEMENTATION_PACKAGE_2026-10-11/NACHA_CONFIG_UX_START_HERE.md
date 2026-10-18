# NACHA-CONFIG-UX-001 — START HERE

**Estado:** PLANEADO  
**Fecha base:** 2026-10-11  
**Artefacto base revisado:** `ACHInterbank-R10-IMPLEMENTED-UNVERIFIED-2026-10-11`  
**Siguiente fase permitida:** `UX0`

Este documento es el contrato de ejecución para la mejora de experiencia de usuario de la parametrización NACHA-M. Su objetivo es impedir que un agente de implementación cambie el alcance, replantee el motor NACHA-M o introduzca decisiones no aprobadas.

## 1. Documentos canónicos y orden de lectura

Antes de modificar código, leer en este orden:

1. `NACHA_CONFIG_UX_START_HERE.md`
2. `NACHA_CONFIG_UX_STATE.md`
3. `NACHA_CONFIG_UX_ARCHITECTURE.md`
4. `NACHA_CONFIG_UX_IMPLEMENTATION_PLAN.md`

Después, leer en el repositorio:

1. `AGENTS.md`
2. `MEMORY.md`
3. Los archivos de código citados para la fase que se va a ejecutar.

No ejecutar dos fases en una sola intervención salvo autorización explícita.

## 2. Objetivo exacto

Transformar la administración de configuración NACHA-M en una experiencia entendible por un usuario operativo, conservando el motor table-driven existente y su autoridad por cámara, perfil, versión y publicación.

La experiencia objetivo debe permitir trabajar pensando en:

> Cámara → operación → dirección → servicio → estructura del archivo → campo → origen del valor → reglas → probar → revisar cambios → publicar.

El usuario operativo no debe necesitar conocer nombres de tablas, rutas de propiedades, identificadores internos, JSON, expresiones técnicas ni nombres de clases.

## 3. Decisiones no negociables

### 3.1 El motor NACHA-M no se reescribe

Queda fuera del alcance funcional de este programa alterar la semántica de:

- `NachaFileBuilder`
- `NachaFileGenerationCoordinator`
- `NachaConfigResolver`
- `NachaOfficialRecordRenderer`
- snapshots `PUBLISH`
- selección oficial por cámara, flujo, dirección, servicio, versión y vigencia
- reglas normativas ACH Colombia o CENIT
- golden files NACHA-M

Si una fase exige cambiar alguno de estos elementos para poder avanzar, **detener la implementación y documentar el bloqueo**. No improvisar una segunda ruta de generación.

### 3.2 La SPA es Angular Material

La experiencia nueva debe construirse con Angular Material 21 y Reactive Forms.

Preferir, cuando aplique:

- `mat-card`
- `mat-form-field`
- `mat-select`
- `mat-input`
- `mat-checkbox`
- `mat-slide-toggle`
- `mat-list` / `mat-nav-list`
- `mat-tabs`
- `mat-expansion-panel`
- `mat-chip`
- `mat-tooltip`
- `mat-dialog`
- `mat-progress-spinner`
- `mat-table` cuando el volumen sea apropiado

No crear controles visuales propios cuando Angular Material ya resuelve el caso de uso. No introducir otra librería de componentes.

### 3.3 Branding dinámico obligatorio

La parametrización NACHA-M debe respetar el branding existente en tiempo de ejecución.

Autoridad actual:

`BrandingSettings` → API → `BrandingService` → variables CSS / tokens de Angular Material.

Reglas:

- No agregar colores de marca fijos en SCSS del módulo NACHA-M.
- No agregar logos, nombres de entidad o imágenes de marca fijas.
- Usar `var(--color-primary)`, tokens semánticos existentes y los tokens Material ya puenteados por `BrandingService`.
- Estados de éxito, advertencia y error usan tokens semánticos, no el color corporativo.
- No recompilar temas para cambiar marca; la marca debe seguir cambiando en tiempo de ejecución.

### 3.4 Nada de catálogos funcionales fijos en Angular

La interfaz no será autoridad para:

- cámaras
- flujos
- direcciones
- servicios
- estados
- tipos de registro
- tipos de origen
- tipos de regla
- nombres funcionales de registros
- nombres funcionales de campos cuando ya exista nombre persistido

Estos valores deben provenir del servidor y, en última instancia, de las tablas/configuración del sistema.

El archivo actual `nacha-config-presentation.catalog.ts` es una deuda transitoria. No ampliar sus mapas. El objetivo es reducirlo o retirarlo a medida que la información funcional venga del servidor.

### 3.5 Español funcional en toda la experiencia

No usar Spanglish en texto visible al usuario.

Ejemplos:

- `Perfil` → preferir **Configuración** cuando se hable con Operaciones.
- `Layout` → **Estructura del archivo** o **Formato del registro**.
- `Field` → **Campo**.
- `Source` → **Origen del valor**.
- `Publish` → **Publicar**.
- `Snapshot` → **Versión guardada** o **Versión publicada**, según contexto.

Los códigos técnicos pueden existir en “Información técnica avanzada”, nunca como lenguaje principal.

### 3.6 Publicado sigue siendo inmutable

No permitir edición directa de una configuración publicada. La evolución continúa mediante borrador/clonación y publicación de una nueva versión.

### 3.7 La interfaz no replica reglas del motor

No calcular en Angular reglas que sean autoridad del servidor. La SPA puede:

- prevenir entradas claramente inválidas;
- calcular una posición final visual;
- mostrar una regla de 106 posiciones;
- advertir una superposición local para mejorar la experiencia.

Pero la decisión definitiva de validez y publicabilidad es del servidor.

### 3.8 No crear una segunda fuente de verdad

No duplicar en la SPA:

- posiciones oficiales;
- longitudes oficiales;
- nombres normativos;
- códigos de transacción;
- reglas por cámara;
- vigencias;
- selección de perfil.

La pantalla interpreta la configuración; no la reemplaza.

## 4. Alcance permitido

Se permite modificar o crear:

- read models orientados a Operaciones;
- endpoints de consulta para catálogos dependientes;
- comandos de edición de borradores;
- componentes Angular Material de la administración NACHA-M;
- vista previa estructural y de prueba sin efectos laterales;
- historial y comparación de cambios;
- pruebas unitarias, integración y Playwright;
- documentación de esta iniciativa.

Se permiten migraciones **solo si una fase demuestra que los catálogos existentes son insuficientes y existe decisión explícita registrada en `NACHA_CONFIG_UX_STATE.md`**. No crear tablas nuevas por comodidad.

## 5. Fases y disciplina

| Fase | Propósito | Puede modificar motor de generación |
|---|---|---:|
| UX0 | Congelar comportamiento y levantar línea base | No |
| UX1 | Contratos de lectura y catálogos dinámicos | No |
| UX2 | Ventana unificada Angular Material | No |
| UX3 | Editor operativo completo de campo/origen/reglas | No |
| UX4 | Probar configuración y vista previa | No, salvo extracción pura previamente aprobada |
| UX5 | Cambios, historial y publicación guiada | No |
| UX6 | Accesibilidad, branding, regresión y cierre | No |

Al finalizar cada fase:

1. Ejecutar sus pruebas focales.
2. Ejecutar pruebas regresivas indicadas.
3. Actualizar `NACHA_CONFIG_UX_STATE.md`.
4. Registrar evidencia concreta.
5. Avanzar `NEXT_ALLOWED_PHASE` solo si la fase cumple su salida.

## 6. Condiciones de parada

Detenerse y no improvisar cuando ocurra cualquiera de estos casos:

- una regla NACHA-M requiere interpretación normativa nueva;
- la experiencia necesita cambiar el motor oficial para mostrar una vista previa;
- un dato funcional no existe en tablas ni en un contrato del servidor;
- se propone codificar en Angular una lista de negocio porque “es más rápido”;
- se propone cambiar un perfil `PUBLICADO` directamente;
- una definición de origen es compartida por varios campos y editarla podría afectar campos no seleccionados;
- las pruebas indican diferencia de bytes en generación oficial no solicitada;
- se necesita una migración no prevista por la fase actual;
- se necesita cambiar branding global para resolver un problema local del módulo.

En estos casos, registrar el bloqueo y pedir decisión.

## 7. Definición global de terminado

El programa termina cuando un usuario de Operaciones puede, sin conocer código:

1. seleccionar una cámara;
2. ver únicamente opciones válidas para su contexto;
3. abrir la estructura del archivo;
4. seleccionar un registro y un campo por nombre entendible;
5. entender posiciones y longitud;
6. escoger el origen del valor de forma guiada;
7. administrar reglas autorizadas;
8. probar la configuración;
9. ver cambios antes de publicar;
10. publicar una nueva versión sin afectar versiones ya publicadas;
11. consultar quién cambió qué y cuándo;
12. realizar todo lo anterior con el branding configurado dinámicamente.

Y, simultáneamente, la generación oficial ACH Colombia/CENIT debe permanecer sin regresiones.

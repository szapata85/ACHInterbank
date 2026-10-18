# Arquitectura — Experiencia operativa de parametrización NACHA-M

**Programa:** `NACHA-CONFIG-UX-001`  
**Estado:** Diseño objetivo  
**Principio rector:** mejorar la experiencia sin crear una segunda autoridad de configuración.

## 1. Arquitectura existente que se conserva

El modelo persistido ya tiene una separación adecuada entre catálogos, configuración editable/publicable e historia.

```text
CatClearingHouse ─┐
CatFlowType ──────┤
CatDirection ─────┤
CatServiceClass ──┤
CatConfigStatus ──┘
                   ↓
               CfgProfile
                   │
          ┌────────┴─────────┐
          ↓                  ↓
 CfgProfileRecord     CfgLayoutVariant
                            │
                            ↓
                     CfgLayoutField
                       │          │
                       ↓          ↓
          CfgFieldSourceDefinition  CfgFieldRule

CfgProfile ──→ HistConfigSnapshot
CfgProfile ──→ HistConfigChange
```

La generación oficial permanece separada:

```text
PUBLISH snapshot
      ↓
NachaConfigResolver
      ↓
layout materializado
      ↓
NachaOfficialRecordRenderer
      ↓
NACHA-M
```

La nueva experiencia de usuario **no entra en esa cadena para sustituirla**.

## 2. Arquitectura objetivo de la administración

```text
┌─────────────────────────────────────────────────────────────┐
│ Angular SPA                                                 │
│                                                             │
│ Ventana Configuración de archivos NACHA-M                  │
│  ├─ Contexto                                                │
│  ├─ Estructura del archivo                                  │
│  ├─ Campos                                                  │
│  ├─ Editor operativo                                        │
│  ├─ Probar                                                  │
│  ├─ Cambios                                                 │
│  └─ Historial                                               │
│                                                             │
│ Angular Material + Reactive Forms + branding dinámico       │
└────────────────────────────┬────────────────────────────────┘
                             │ HTTP
                             ▼
┌─────────────────────────────────────────────────────────────┐
│ API                                                         │
│  - consultas orientadas a Operaciones                       │
│  - comandos de borrador                                     │
│  - validar / probar / publicar                              │
│  - historial humanizado                                     │
└────────────────────────────┬────────────────────────────────┘
                             │
                             ▼
┌─────────────────────────────────────────────────────────────┐
│ Application                                                 │
│  - contratos/read models de workspace                       │
│  - contratos de comandos                                    │
│  - sin conocimiento de Angular                              │
└────────────────────────────┬────────────────────────────────┘
                             │
                             ▼
┌─────────────────────────────────────────────────────────────┐
│ Persistence                                                 │
│  - consulta Cat* / Cfg* / Hist*                             │
│  - filtrado dependiente                                     │
│  - edición transaccional de borradores                      │
│  - materialización de historial                             │
└─────────────────────────────────────────────────────────────┘
```

## 3. Separación obligatoria de responsabilidades

### 3.1 SPA

La SPA se responsabiliza de:

- presentar información en lenguaje operativo;
- mantener contexto seleccionado;
- formularios y validaciones inmediatas de captura;
- navegación y estado visual;
- accesibilidad;
- vista visual de posiciones;
- solicitar validación/prueba al servidor;
- mostrar diferencias e historial ya interpretados.

La SPA **no** decide:

- cuál perfil oficial debe resolver;
- qué layout es normativamente correcto;
- qué valores están permitidos por cámara si el servidor no los autoriza;
- si una configuración es publicable;
- cómo generar bytes oficiales NACHA-M.

### 3.2 API/Application

Deben exponer contratos hechos para la experiencia, no reutilizar ciegamente DTO técnicos si eso obliga a Angular a interpretar estructuras internas.

Contratos objetivo sugeridos:

```text
NachaConfigWorkspaceContextDto
NachaConfigWorkspaceCatalogsDto
NachaConfigWorkspaceRecordDto
NachaConfigWorkspaceFieldDto
NachaConfigWorkspaceFieldSourceDto
NachaConfigWorkspaceRuleDto
NachaConfigPendingChangesDto
NachaConfigHistoryDetailDto
NachaConfigStructuralPreviewDto
```

Los nombres son orientativos; lo obligatorio es la responsabilidad.

### 3.3 Persistence

Persistence traduce tablas actuales a modelos de lectura entendibles y ejecuta cambios de borrador de forma transaccional.

No se debe mover semántica de negocio a controladores.

## 4. Flujo dinámico de catálogos

### 4.1 Selección inicial

```text
GET catálogos base
     ↓
Cámara
     ↓
GET opciones válidas para la cámara
     ↓
Flujo / Dirección / Servicio
     ↓
GET perfiles/configuraciones aplicables
```

Ejemplo mínimo obligatorio:

```text
CatServiceClass.ClearingHouseId
             ↓
Cámara seleccionada
             ↓
solo servicios de esa cámara + servicios globales válidos
```

No cargar todos los servicios y esperar que el usuario descubra después una combinación inválida.

### 4.2 Estructura y campos

Los nombres visibles se obtienen, en orden de preferencia, de:

1. nombre funcional persistido (`NameEs` / `FieldNameEs`);
2. catálogo persistido correspondiente (`CatRecordCode.NameEs`, `CatDataSourceType.NameEs`, `CatRuleType.NameEs`);
3. fallback genérico del servidor únicamente para diagnóstico.

El frontend no mantiene mapas paralelos de nombres oficiales.

## 5. Origen del valor

La experiencia operacional debe presentar categorías como:

- Dato del sistema
- Valor fijo
- Calculado por el sistema
- Catálogo autorizado
- Configuración avanzada

La persistencia actual continúa utilizando `CfgFieldSourceDefinition`.

### 5.1 Regla de seguridad por referencias compartidas

Antes de cambiar una fuente:

```text
¿SourceDefinitionId es usado por más de un CfgLayoutField?
       │
  ┌────┴────┐
  │         │
 no        sí
  │         │
actualizar  crear nueva definición
           + revincular solo el campo actual
```

Nunca modificar una definición compartida de forma que cambien otros campos sin intención explícita.

### 5.2 Catálogo de opciones de origen

Primera estrategia aprobada: **reutilizar las tablas/configuraciones existentes**.

Para un `FieldCode`, el servidor puede construir opciones aprobadas a partir de fuentes ya persistidas para ese campo y de `CatDataSourceType`, eliminando duplicados y proporcionando nombres funcionales.

Si UX3 demuestra que esto no alcanza para una administración segura, se detiene la fase y se documenta una propuesta de catálogo persistido. No se compensa la falta con una lista fija en Angular.

## 6. Edición del campo

El modelo visual debe cubrir como mínimo:

```text
Ubicación
- posición inicial
- longitud
- posición final calculada

Origen del valor
- tipo de origen
- opción autorizada / valor fijo / cálculo

Presentación
- alineación
- carácter de relleno
- formato

Estado
- habilitado

Reglas
- tipo
- mensaje al usuario
- severidad
- habilitada
```

Los campos técnicos de bajo nivel se muestran únicamente en “Información técnica avanzada”.

## 7. Vista previa y prueba

Se separan dos conceptos.

### 7.1 Vista estructural

Siempre disponible para un borrador cargado.

- regla de 106 posiciones;
- bloques por campo;
- inicio/fin/longitud;
- campos deshabilitados claramente diferenciados;
- superposiciones destacadas;
- no genera movimiento, archivo productivo ni publicación.

Puede construirse a partir del read model del servidor.

### 7.2 Prueba de renderizado

No se implementará un renderizador duplicado en TypeScript.

Opciones autorizadas, en orden:

1. reutilizar una ruta pura existente del servidor si puede renderizar sin persistencia ni publicación;
2. extraer una función pura desde el renderer oficial, con pruebas de equivalencia, si es necesario y aprobado;
3. si ninguna es segura, mantener en esta iniciativa únicamente vista estructural + validación del servidor y registrar la prueba de bytes como fase separada.

Nunca copiar reglas de renderizado al frontend.

## 8. Cambios e historial

`HistConfigChange` ya almacena `BeforeJson` y `AfterJson`; la SPA actual no recibe una interpretación útil.

Arquitectura objetivo:

```text
HistConfigChange
   ↓
servidor interpreta entidad/campo
   ↓
ChangeDetailDto
   - sección
   - elemento
   - atributo
   - valor anterior
   - valor nuevo
   - usuario
   - fecha
   ↓
Angular muestra texto operativo
```

No enviar JSON crudo a Operaciones como experiencia principal.

## 9. Publicación guiada

```text
Guardar borrador
      ↓
Validar
      ↓
Errores bloqueantes = 0
      ↓
Mostrar cambios pendientes
      ↓
Confirmación explícita
      ↓
Publicar con RowVersion
      ↓
Crear/confirmar PUBLISH
      ↓
Recargar como solo lectura
```

Si el `RowVersion` cambió, mostrar conflicto de concurrencia y recargar; nunca sobrescribir silenciosamente.

## 10. Arquitectura visual Angular Material

La nueva ventana se divide así:

```text
┌──────────────────────────────────────────────────────────────┐
│ Configuración de archivos NACHA-M                           │
│ Cámara | Operación | Dirección | Servicio | Versión | Estado│
├──────────────────────────────────────────────────────────────┤
│ Configuración | Probar | Cambios | Historial                │
├─────────────────┬──────────────────────────┬─────────────────┤
│ Estructura      │ Campos                   │ Detalle         │
│ 1 Encabezado    │ Código transacción       │ Número cuenta   │
│ 5 Encab. lote   │ Entidad receptora        │ Pos. 13–29      │
│ 6 Detalle       │ Número de cuenta   ←     │ Origen          │
│ 7 Adicional     │ Valor                    │ Reglas          │
│ 8 Control lote  │ ...                      │ [Guardar]       │
│ 9 Control arch. │                          │                 │
├─────────────────┴──────────────────────────┴─────────────────┤
│ Regla visual de posiciones 1 … 106                           │
└──────────────────────────────────────────────────────────────┘
```

En pantallas pequeñas se transforma en navegación por pasos/paneles; no se obliga a mantener tres columnas comprimidas.

## 11. Branding dinámico

Flujo existente que se conserva:

```text
BrandingSettings
      ↓
BrandingSettingsService/API
      ↓
BrandingService (Angular)
      ↓
--brand-primary
--brand-on-primary
--brand-primary-hover
--brand-public-background
--brand-private-background
--brand-sidebar-background
--mat-sys-primary
--mat-sys-on-primary
      ↓
Angular Material + componentes compartidos
```

Reglas del módulo:

- sin hexadecimales de marca en SCSS del feature;
- sin `!important` para imponer color corporativo salvo defecto comprobado y documentado;
- foco y selección usan tokens del sistema;
- los contrastes se dejan al mecanismo existente de `BrandingService`;
- la vista previa NACHA usa color de marca solo para selección/foco, no para codificar semántica de error/éxito.

## 12. Compatibilidad de rutas

Rutas actuales:

- `/nacha-config-admin/perfiles`
- `/nacha-config-admin/records`
- `/nacha-config-admin/variants-fields`
- `/nacha-config-admin/perfiles/:id`

Objetivo recomendado:

- conservar `/nacha-config-admin/perfiles` como entrada/listado;
- usar `/nacha-config-admin/perfiles/:id` como ventana unificada;
- redirigir `records` y `variants-fields` a la ventana unificada conservando `profileId`, `recordCode`, `variantId`, `fieldId` cuando estén presentes;
- cubrir todas las rutas con Playwright para impedir regresiones 404.

## 13. Cambios de esquema

**No hay migración aprobada de inicio.**

Primero se aprovechan:

- nombres persistidos;
- catálogos `Cat*`;
- relaciones existentes;
- definiciones de origen existentes;
- historial actual.

Una nueva tabla solo puede proponerse cuando exista una necesidad demostrada y no pueda resolverse limpiamente con las autoridades actuales.

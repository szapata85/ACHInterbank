# Plan de implementación — Experiencia operativa de parametrización NACHA-M

**Programa:** `NACHA-CONFIG-UX-001`  
**Estado inicial:** PLANEADO  
**Siguiente fase permitida:** UX0

## 1. Resultado esperado

Entregar una ventana de parametrización NACHA-M amigable para Operaciones, completamente integrada a Angular Material, alimentada por las tablas del sistema y compatible con el branding dinámico existente.

No se busca “maquillar” las pantallas actuales. Se busca traducir correctamente el modelo table-driven a una experiencia humana sin duplicar autoridad en la SPA.

## 2. Estado actual confirmado

### 2.1 Fortalezas que se conservan

- Perfiles por cámara, flujo, dirección, servicio, versión y vigencia.
- Publicación inmutable mediante snapshots.
- Catálogos persistidos para cámara, flujo, dirección, servicio, registro, estado, origen y regla.
- Validación de configuración.
- Historial de cambios.
- Control de concurrencia por `RowVersion`.
- Permisos `Config.Read` / `Config.Manage` y equivalentes ACH.
- Pruebas Playwright existentes del módulo.
- Branding dinámico ya centralizado.

### 2.2 Brechas actuales que este plan corrige

1. La pantalla usa lenguaje todavía técnico: perfil, variante, ruta técnica, estrategia fuente, prioridad.
2. Angular mantiene mapas funcionales fijos en `nacha-config-presentation.catalog.ts`.
3. El selector de servicios no está condicionado por la cámara.
4. El editor de campo no permite administrar toda la información ya persistida.
5. El usuario puede ver `PropertyPath`, códigos y nombres técnicos como parte normal del flujo.
6. No existe una sola ventana contextual.
7. No hay regla visual de 106 posiciones.
8. “Probar” actualmente valida resolución, pero no ofrece suficiente retroalimentación visual.
9. El historial no presenta antes/después de forma humanizada.
10. La publicación no muestra una revisión completa de cambios en lenguaje operativo.
11. Existe evidencia visual de navegación 404 en detalle que debe quedar cerrada mediante pruebas de rutas.

## 3. Experiencia objetivo

### 3.1 Entrada

Título:

**Configuración de archivos NACHA-M**

Contexto:

- Cámara
- Operación
- Dirección
- Servicio
- Versión
- Vigencia
- Estado

Mensaje para borrador:

> Puedes realizar cambios sin afectar la configuración actualmente publicada.

### 3.2 Área de trabajo

Pestañas:

1. **Configuración**
2. **Probar**
3. **Cambios**
4. **Historial**

Dentro de Configuración:

- Estructura del archivo.
- Lista de campos del registro.
- Detalle/editor del campo.
- Regla visual de posiciones.

### 3.3 Lenguaje

Ejemplos visibles:

- “Detalle de la transacción”, no “R6_BASE”.
- “Origen del valor”, no “Source definition”.
- “Número de cuenta”, no “R6_ACCOUNTNUMBER”.
- “Calculado por el sistema”, no una expresión interna.
- “Error bloqueante”, no un código técnico como etiqueta principal.

Los códigos pueden mostrarse bajo “Información técnica avanzada”.

---

# FASE UX0 — Congelación y línea base

## Objetivo

Demostrar el comportamiento actual antes de cambiar la experiencia y crear guardrails que impidan alterar el motor NACHA-M por accidente.

## Cambios permitidos

- pruebas;
- scripts de validación;
- documentación;
- correcciones de ruta estrictamente necesarias para que una ruta válida no termine en 404, siempre que no cambien reglas NACHA-M.

## Trabajo

1. Caracterizar rutas actuales de administración.
2. Caracterizar carga de catálogos.
3. Caracterizar detalle de perfil y edición de borrador.
4. Confirmar que perfiles publicados no son editables.
5. Confirmar branding dinámico en la pantalla actual.
6. Capturar hash/evidencia de archivos clave del motor para detectar cambios accidentales durante UX1–UX6.
7. Agregar prueba de navegación que abra un perfil existente desde el listado y confirme que `/perfiles/:id` no responde con pantalla 404.
8. Registrar línea base de generación oficial con las pruebas existentes; no modificar goldens.

## Archivos de referencia

- `web/ach-interbank-ui/src/app/features/nacha-config-admin/**`
- `web/ach-interbank-ui/e2e/nacha-config-*.spec.ts`
- `src/Cfa.ACHInterbank.Persistence/ACH/Services/Implementation/NachaConfigProfileQueryService.cs`
- `src/Cfa.ACHInterbank.Persistence/ACH/Services/Implementation/NachaConfigProfileCommandService.cs`
- motor NACHA citado en START_HERE, solo para hash/caracterización.

## Salida obligatoria

- pruebas de línea base en verde;
- evidencia de rutas;
- evidencia de branding;
- ningún cambio funcional del motor;
- `NACHA_CONFIG_UX_STATE.md` actualizado a `UX0=CLOSED`, `NEXT_ALLOWED_PHASE=UX1`.

---

# FASE UX1 — Contratos de lectura y catálogos dinámicos

## Objetivo

Hacer que la SPA reciba información ya humanizada y contextual desde el servidor, eliminando gradualmente la necesidad de mapas fijos en Angular.

## Backend

### 1. Crear un read model de workspace

Crear un contrato orientado al usuario, por ejemplo:

```text
NachaConfigWorkspaceDto
- profileId
- displayName
- statusCode
- statusName
- clearingHouse
- flow
- direction
- service
- version
- effectiveFrom / effectiveTo
- isEditable
- records[]
- variants[]
- fields[]
```

Cada elemento debe contener:

- código técnico para trazabilidad;
- nombre funcional en español;
- información necesaria para la UI;
- indicadores de edición/estado.

No enviar a Angular la responsabilidad de traducir códigos conocidos.

### 2. Catálogos dependientes

Ampliar el contrato de catálogos para aceptar contexto.

Ejemplo:

```http
GET /nacha-config/catalogos-operativos?camaraCode=CENIT
```

Resultado:

- cámaras activas;
- flujos activos;
- direcciones válidas;
- servicios válidos para la cámara;
- estados;
- tipos de registro;
- tipos de origen;
- tipos de regla.

Regla para servicios:

```text
CatServiceClass.IsActive = true
AND (
  ClearingHouseId = cámara seleccionada
  OR ClearingHouseId IS NULL cuando el servicio sea global
)
```

No asumir que toda clase global es válida sin validar el significado actual de los datos.

### 3. Nombres funcionales

Usar directamente:

- `CatClearingHouse.Name`
- `CatFlowType.NameEs`
- `CatDirection.NameEs`
- `CatServiceClass.NameEs`
- `CatRecordCode.NameEs`
- `CatDataSourceType.NameEs`
- `CatRuleType.NameEs`
- `CfgProfile.NameEs`
- `CfgLayoutVariant.NameEs`
- `CfgLayoutField.FieldNameEs`

Si los datos oficiales contienen nombres técnicos, corregirlos mediante seeding/backfill controlado solo si es necesario y con pruebas. No compensar permanentemente con diccionarios Angular.

## Frontend

1. Consumir el nuevo read model.
2. Reemplazar el uso funcional de mapas de `nacha-config-presentation.catalog.ts` por nombres recibidos.
3. Mantener temporalmente fallback técnico solo para diagnóstico, no como camino feliz.
4. No cambiar todavía el layout completo de la página.

## Pruebas

- servicio por cámara;
- nombres funcionales desde servidor;
- catálogos vacíos manejados sin romper UI;
- CENIT y ACH Colombia aislados;
- pruebas de permisos;
- contrato sin Spanglish visible.

## Salida obligatoria

- Angular deja de ser autoridad de los catálogos principales;
- servicio dependiente por cámara funcionando;
- no hay regresión de selección oficial;
- `NEXT_ALLOWED_PHASE=UX2`.

---

# FASE UX2 — Ventana unificada Angular Material

## Objetivo

Reemplazar la navegación fragmentada por una única ventana operacional.

## Componentes sugeridos

Crear bajo `features/nacha-config-admin/workspace/` componentes pequeños y enfocados:

```text
nacha-config-workspace-page
nacha-config-context-bar
nacha-config-record-list
nacha-config-field-list
nacha-config-field-detail
nacha-config-position-ruler
nacha-config-validation-summary
```

No crear un componente de más de cientos de líneas que concentre toda la lógica.

## Angular Material obligatorio

Usar:

- `mat-card` para bloques;
- `mat-form-field` + `mat-select` para contexto;
- `mat-tab-group` para Configuración / Probar / Cambios / Historial;
- `mat-nav-list` o `mat-list` para registros;
- `mat-table` o lista Material para campos;
- `mat-expansion-panel` para información técnica avanzada;
- `mat-chip` para estado;
- `mat-tooltip` para ayudas breves;
- `mat-dialog` para confirmaciones.

No recrear botones o selectores con CSS propio cuando exista equivalente Material.

## Branding

El módulo no define color corporativo. Todo énfasis principal utiliza los tokens ya administrados por `BrandingService`.

Añadir prueba automática que cambie el color de branding y compruebe visual/DOM que el workspace hereda el nuevo token sin reconstruir la SPA.

## Rutas

- `/perfiles/:id` se convierte en la ventana principal.
- `records` y `variants-fields` conservan compatibilidad, pero deben llevar al workspace preservando contexto.
- rutas válidas nunca deben caer en 404.

## Diseño adaptable

Desktop: hasta tres zonas de trabajo.

Tablet/móvil: navegación secuencial; no tres columnas minúsculas.

## Salida obligatoria

- flujo principal navegable con mouse y teclado;
- Material visible y consistente;
- branding dinámico probado;
- rutas antiguas compatibles;
- `NEXT_ALLOWED_PHASE=UX3`.

---

# FASE UX3 — Editor operativo completo

## Objetivo

Permitir que el usuario administre un borrador sin escribir rutas internas ni conocer el modelo de base de datos.

## 1. Contrato de edición de campo

Ampliar la edición para cubrir, como mínimo:

- nombre funcional;
- posición inicial;
- longitud;
- habilitado;
- alineación;
- carácter de relleno;
- formato;
- origen del valor;
- datos propios del origen.

El contrato debe ser tipado. No continuar ampliando `Record<string, unknown>` como contrato normal de comandos.

## 2. Origen del valor

La UI presenta opciones humanas.

Ejemplos:

- Dato del sistema
- Valor fijo
- Calculado por el sistema
- Catálogo autorizado
- Avanzado

El servidor traduce la intención a `CfgFieldSourceDefinition`.

### Protección de fuente compartida

Antes de mutar `SourceDefinition`:

1. contar referencias desde `CfgLayoutField`;
2. si solo hay una, se puede actualizar;
3. si hay varias y la definición cambia, crear una nueva definición y revincular solo el campo actual;
4. todo en una transacción;
5. auditar el cambio.

Agregar pruebas específicas.

## 3. Reglas

Presentar `CatRuleType.NameEs` y mensajes claros.

La experiencia debe permitir administrar reglas que ya formen parte del modelo autorizado sin exponer JSON/DSL en la vista normal.

Información técnica avanzada puede mostrar:

- código de regla;
- configuración interna;
- condición;
- identificadores.

No hacer editables esos datos para el rol operativo salvo que exista una necesidad y permiso explícitos.

## 4. Regla visual de posiciones

Crear `nacha-config-position-ruler`:

- 1 a 106;
- bloques por campo;
- selección sincronizada con lista/editor;
- posición inicial/final;
- campo deshabilitado;
- superposición;
- accesible por teclado y texto alternativo.

La regla visual usa datos del read model, no una estructura oficial fija.

## 5. Concurrencia

Todos los comandos conservan `ExpectedRowVersion`.

Ante conflicto:

> La configuración fue modificada por otro usuario. Recarga los cambios antes de continuar.

No sobrescribir.

## Salida obligatoria

Un operador puede cambiar posición, longitud, formato/origen autorizado y reglas de un campo de borrador sin editar rutas técnicas.

`NEXT_ALLOWED_PHASE=UX4`.

---

# FASE UX4 — Probar configuración y vista previa

## Objetivo

Dar confianza antes de publicar sin crear efectos laterales.

## 1. Validación

Reutilizar `INachaConfigValidationService`.

Mostrar:

- Apto para publicar / Requiere correcciones;
- errores bloqueantes;
- advertencias;
- ubicación del problema: registro/campo;
- mensaje humano;
- acción sugerida cuando sea posible.

El código técnico queda en detalle avanzado.

## 2. Vista estructural

Siempre disponible:

- 106 posiciones;
- campos y límites;
- origen resumido;
- formato;
- validaciones relevantes.

## 3. Prueba de selección

Aprovechar `NachaConfigPreviewService` para responder, en lenguaje operacional:

> Para CENIT / Operaciones originales / Salida / PPD / fecha X se resolverá la configuración Y.

La traza técnica actual se puede conservar en sección avanzada.

## 4. Prueba de contenido

No duplicar renderer.

Implementar únicamente si puede reutilizarse el servidor sin tocar la autoridad productiva. Si se requiere refactor del renderer, detener y obtener aprobación de diseño antes de ejecutar.

Si se implementa, debe ser:

- sin persistencia de archivo productivo;
- sin despacho;
- sin MFT;
- sin SOAP;
- sin movimiento monetario;
- claramente marcado “Vista previa”.

## Salida obligatoria

El usuario puede validar y entender visualmente qué va a publicar.

`NEXT_ALLOWED_PHASE=UX5`.

---

# FASE UX5 — Cambios, historial y publicación guiada

## Objetivo

Que publicar sea una decisión informada y auditable.

## 1. Cambios pendientes

Crear endpoint/read model que traduzca `HistConfigChange` o compare el borrador con su base.

Mostrar ejemplos:

```text
Número de cuenta
Posición: 13–29 → 13–30
Longitud: 17 → 18

Valor de la transacción
Formato: sin formato especial → numérico con relleno a la izquierda
```

No mostrar JSON como interfaz principal.

## 2. Historial

Mostrar:

- fecha;
- usuario;
- acción;
- elemento afectado;
- resumen;
- “Ver detalle”.

## 3. Publicación

Secuencia obligatoria:

1. guardar;
2. validar;
3. si hay bloqueantes, impedir publicar;
4. mostrar cambios;
5. confirmación explícita;
6. publicar con `RowVersion` actual;
7. recargar como solo lectura.

No introducir edición post-publicación.

## Salida obligatoria

Publicación trazable, entendible y segura.

`NEXT_ALLOWED_PHASE=UX6`.

---

# FASE UX6 — Cierre de experiencia, accesibilidad y regresión

## Objetivo

Certificar que la nueva experiencia es consistente con el resto de la SPA y no degrada el motor.

## 1. Accesibilidad

Comprobar:

- navegación por teclado;
- foco visible;
- etiquetas asociadas;
- mensajes de error anunciables;
- contraste;
- no usar solo color para transmitir estado;
- diálogos con foco controlado.

## 2. Branding

Pruebas con al menos dos configuraciones de marca:

- color principal claro;
- color principal oscuro.

Verificar contraste calculado por el mecanismo existente.

No duplicar lógica de contraste dentro del feature.

## 3. Idioma

Extender/usar `assertNoFunctionalSpanglish` para todas las pestañas y diálogos del workspace.

## 4. Pruebas Playwright

Mínimo:

1. abrir perfil existente desde listado;
2. cambiar cámara y confirmar filtrado de servicio;
3. abrir registro/campo por nombre humano;
4. editar borrador y cancelar sin PUT;
5. guardar campo y persistir;
6. conflicto de concurrencia;
7. perfil publicado bloquea edición;
8. validar y localizar error;
9. abrir cambios;
10. abrir historial;
11. probar branding dinámico;
12. móvil/tablet/desktop;
13. rutas antiguas redirigen correctamente;
14. cero errores de consola/página en flujo feliz.

## 5. Regresión backend

Ejecutar:

```text
dotnet build ACHInterbank.sln -c Release
dotnet test tests/Cfa.ACHInterbank.Tests/Cfa.ACHInterbank.Tests.csproj -c Release
```

Además de las pruebas focales NACHA config, ejecutar las pruebas de perfiles publicados, snapshots y generación oficial relevantes.

Frontend:

```text
npm ci
npm run build
npm test -- --watch=false
npm run e2e -- <suite focal>
```

Usar las versiones de Node definidas por CI del repositorio.

## 6. Guardrail estático recomendado

Agregar un validador del feature que falle si:

- se agregan mapas de cámara/servicio/registro/campo en Angular;
- se agregan colores hexadecimales de marca en SCSS del workspace;
- se usa `any` en los nuevos contratos principales;
- se muestra `PropertyPath`, JSON o DSL fuera de “Información técnica avanzada”;
- se modifica el conjunto protegido de archivos del motor sin autorización explícita.

## Salida final

Solo declarar el programa cerrado cuando:

- todas las fases estén cerradas;
- build y tests estén ejecutados en el artefacto exacto;
- Angular build/test/E2E estén en verde;
- no exista regresión NACHA-M;
- branding dinámico esté probado;
- Operaciones pueda completar el recorrido sin conceptos técnicos obligatorios.

---

# 4. Archivos actuales que probablemente se modificarán

## SPA

- `web/ach-interbank-ui/src/app/features/nacha-config-admin/**`
- `web/ach-interbank-ui/src/app/core/models/nacha-config.model.ts`
- `web/ach-interbank-ui/src/app/core/services/nacha-config-api.service.ts`
- pruebas `*.spec.ts`
- `web/ach-interbank-ui/e2e/nacha-config-*.spec.ts`

El branding global (`BrandingService`, tokens) se consume; no debe reescribirse salvo defecto demostrado fuera del alcance.

## Application/API

- `src/Cfa.ACHInterbank.Application/ACH/Models/NachaConfigAdminDtos.cs` o nuevos read models específicos.
- nuevas interfaces de consulta si evitan sobrecargar servicios actuales.
- `src/Cfa.ACHInterbank.Api/Controllers/NachaConfigProfilesController.cs` o un controlador de workspace si mejora cohesión.

## Persistence

- `NachaConfigProfileQueryService` o un nuevo servicio de read model.
- `NachaConfigProfileCommandService` solo para ampliar comandos de borrador de forma cohesionada; si crece demasiado, separar por responsabilidad.
- `NachaConfigHistoryService` para detalle humanizado.
- `NachaConfigPreviewService` para prueba/lectura, sin despachos.

## Archivos protegidos por defecto

No modificar sin decisión explícita:

- `NachaFileBuilder.cs`
- `NachaFileGenerationCoordinator.cs`
- `NachaConfigResolver.cs`
- `NachaOfficialRecordRenderer.cs`
- seeders normativos oficiales salvo corrección de nombres funcionales demostrada y aislada;
- goldens NACHA-M;
- normativa.

# 5. Criterios de aceptación de negocio

La implementación se considera excelente para Operaciones cuando:

- el usuario entiende qué configura sin manual técnico;
- las listas se adaptan al contexto;
- los nombres vienen del sistema;
- el sistema evita combinaciones inválidas tempranamente;
- los detalles técnicos existen pero no estorban;
- se puede ver la estructura de 106 posiciones;
- se puede validar antes de publicar;
- se puede saber exactamente qué cambió;
- se puede saber quién lo cambió;
- el perfil publicado nunca cambia por una edición de borrador;
- el aspecto visual se adapta a la marca configurada;
- la UI no contiene Spanglish funcional;
- ACH Colombia y CENIT continúan generándose con su autoridad table-driven actual.

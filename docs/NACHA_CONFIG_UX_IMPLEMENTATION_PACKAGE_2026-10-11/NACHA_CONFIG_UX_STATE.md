# NACHA-CONFIG-UX-001 — Memory State

**Programa:** `NACHA-CONFIG-UX-001`  
**Estado:** `CLOSED`  
**Fase actual:** `UX6 (CLOSED)`  
**NEXT_ALLOWED_PHASE:** `NONE`  
**Fecha:** 2026-10-11

Este archivo es memoria operativa de la iniciativa de experiencia de usuario para la parametrización NACHA-M. No sustituye `MEMORY.md` del proyecto. Al ejecutar fases reales, las decisiones durables confirmadas deben reflejarse también en la memoria raíz del repositorio.

## Baseline aceptado

- BASE-UX-001: La solución R10 conserva generación NACHA-M table-driven y selección por cámara/perfil/flujo/dirección/servicio/versión/vigencia.
- BASE-UX-002: La autoridad productiva ordinaria continúa siendo el snapshot `PUBLISH`; perfiles publicados no se editan directamente.
- BASE-UX-003: La SPA usa Angular 21.2.x y Angular Material 21.2.x.
- BASE-UX-004: El branding actual se obtiene del servidor mediante `BrandingService` y modifica en tiempo de ejecución tokens como `--brand-primary`, `--mat-sys-primary`, fondos y contraste.
- BASE-UX-005: Existen tablas/catálogos `CatClearingHouse`, `CatFlowType`, `CatDirection`, `CatServiceClass`, `CatRecordCode`, `CatConfigStatus`, `CatDataSourceType` y `CatRuleType`.
- BASE-UX-006: El modelo de configuración incluye `CfgProfile`, `CfgProfileRecord`, `CfgLayoutVariant`, `CfgLayoutField`, `CfgFieldSourceDefinition`, `CfgFieldRule`, `CfgRuleSet`, `HistConfigSnapshot` y `HistConfigChange`.
- BASE-UX-007: La pantalla actual ya permite borrador, validación, publicación, clonación y algunas ediciones, pero expone conceptos técnicos y no administra toda la riqueza de `CfgLayoutField`/`CfgFieldSourceDefinition`/`CfgFieldRule`.
- BASE-UX-008: El endpoint actual de catálogos devuelve todas las clases de servicio activas y no las filtra por cámara, aunque `CatServiceClass` dispone de `ClearingHouseId` opcional.
- BASE-UX-009: `nacha-config-presentation.catalog.ts` mantiene traducciones funcionales fijas en frontend para perfiles, variantes, campos, flujos, direcciones, servicios, estados y tipos de origen.
- BASE-UX-010: El servicio actual de vista previa (`NachaConfigPreviewService`) valida resolución de perfil/layout; todavía no genera una vista previa operativa completa del archivo.
- BASE-UX-011: El historial expuesto actualmente entrega metadatos del cambio, pero no una comparación humanizada antes/después.
- BASE-UX-012: Las capturas funcionales actuales muestran una pantalla administrativa útil pero densa; la captura de detalle disponible evidencia además una navegación 404 que debe quedar cubierta por regresión de rutas.

## Decisiones durables

- DEC-UX-001: No reescribir ni sustituir el motor NACHA-M durante esta iniciativa.
- DEC-UX-002: La nueva experiencia se implementa con Angular Material y Reactive Forms; no se introduce otro sistema de componentes.
- DEC-UX-003: El branding dinámico existente es obligatorio. No se fijan colores de marca, logos ni nombres institucionales dentro del módulo.
- DEC-UX-004: La SPA no será autoridad de catálogos funcionales. Las opciones deben provenir del servidor y de las tablas/configuraciones persistidas.
- DEC-UX-005: Todo texto principal visible al usuario operativo estará en español claro. Códigos y detalles técnicos se relegan a una sección avanzada.
- DEC-UX-006: La experiencia objetivo es una sola ventana de trabajo contextual, no tres mantenimientos independientes de “perfiles / registros / variantes”.
- DEC-UX-007: Se conservarán rutas antiguas mientras existan consumidores; podrán redirigir o abrir la nueva ventana con el contexto equivalente.
- DEC-UX-008: Las opciones dependientes se filtran antes de que el usuario seleccione combinaciones imposibles. Ejemplo mínimo: servicios por cámara.
- DEC-UX-009: No se crea una tabla nueva de catálogo en UX0/UX1. Primero se explotarán catálogos existentes y configuraciones persistidas. Si son insuficientes para ofrecer una selección segura y humanizada de orígenes, la fase debe detenerse y registrar una propuesta de esquema antes de migrar.
- DEC-UX-010: Una edición de origen nunca puede modificar accidentalmente una `CfgFieldSourceDefinition` compartida por otros campos. La fase UX3 deberá detectar uso compartido y clonar/revincular de forma transaccional cuando corresponda.
- DEC-UX-011: La vista previa no se implementará con un renderizador paralelo en Angular. Debe reutilizar el servidor y la misma semántica de renderizado o limitarse a una vista estructural hasta que exista una extracción pura aprobada.
- DEC-UX-012: Publicación continúa fail-closed: validar primero, mostrar cambios, confirmar y publicar con control de concurrencia.

## Brechas abiertas

- Ninguna dentro de `NACHA-CONFIG-UX-001`. Los gates ejecutables no disponibles en este host se registran como `NOT_RUN_ENVIRONMENT`; no se convierten en nuevas brechas funcionales ni en PASS.

## Elementos que no se deben reabrir

- INV-UX-001: Perfiles `PUBLICADO` son inmutables.
- INV-UX-002: `PUBLISH` sigue siendo autoridad productiva.
- INV-UX-003: ACH Colombia y CENIT permanecen aislados por configuración.
- INV-UX-004: Longitud/posición/reglas oficiales no se codifican en componentes Angular.
- INV-UX-005: No hay fallback al generador heredado para solucionar problemas de interfaz.
- INV-UX-006: No se cambian golden files sin cambio normativo explícito y evidencia.

## Evidencia inicial relevante

- EVID-UX-001: `web/ach-interbank-ui/package.json` contiene Angular/Material 21.2.x.
- EVID-UX-002: `BrandingService` aplica tokens Material y variables CSS en tiempo de ejecución.
- EVID-UX-003: `styles/_tokens.scss` define la capa de tokens consumida por la SPA.
- EVID-UX-004: `NachaConfigProfileQueryService.GetFilterCatalogsAsync` consulta tablas reales, pero `Servicios` no filtra por `ClearingHouseId`.
- EVID-UX-005: `NachaConfigLayoutFieldEditDto` solo expone `FieldNameEs`, `StartPosition`, `Length`, `PropertyPath`, `IsEnabled` y `ExpectedRowVersion`.
- EVID-UX-006: `NachaConfigLayoutFieldDto` ya lee relleno, alineación, formato, origen, constante, entidad, expresión, catálogo externo, fallback y reglas; por tanto existe información persistida que la UI aún no aprovecha plenamente.
- EVID-UX-007: Existen pruebas Playwright NACHA Config y un verificador explícito de ausencia de Spanglish funcional.

## Fases

| Fase | Estado | Salida requerida |
|---|---|---|
| UX0 | CLOSED | Línea base y pruebas de caracterización |
| UX1 | CLOSED | Contratos de lectura y catálogos dependientes |
| UX2 | CLOSED | Ventana unificada Angular Material |
| UX3 | CLOSED | Editor operativo completo |
| UX4 | CLOSED | Probar configuración / vista previa |
| UX5 | CLOSED | Cambios, historial y publicación guiada |
| UX6 | CLOSED | Accesibilidad, branding, regresión y cierre |


## Evidencia UX0 — 2026-10-11

- EVID-UX0-001: `scripts/validate-nacha-config-ux0-baseline.py` ejecutó `PASS_STATIC` con 25 checks. El manifiesto `NACHA_CONFIG_UX0_ENGINE_BASELINE.json` fija 11 archivos protegidos: cinco fuentes críticas del motor/snapshot y seis golden files ACH Colombia/CENIT. Todos conservan exactamente los bytes capturados al inicio de UX0.
- EVID-UX0-002: Se agregó regresión Playwright `ProfilesList_ShouldOpenExistingProfileWithout404`, que abre un perfil existente desde el listado y exige `/nacha-config-admin/perfiles/:id` con el workspace visible y sin pantalla 404. La ruta productiva ya existía; UX0 no necesitó corrección funcional de routing.
- EVID-UX0-003: La carga de catálogos queda caracterizada tanto en Angular como en backend; `GetFilterCatalogsAsync` continúa leyendo catálogos persistidos. La brecha de servicios no filtrados por cámara permanece para UX1, sin cambio en UX0.
- EVID-UX0-004: La edición de borrador y la inmutabilidad de publicado quedan caracterizadas en Angular y backend. Se agregó un guard test explícito que confirma que un perfil `PUBLICADO` no invoca `editarBorrador`.
- EVID-UX0-005: Se agregó caracterización Playwright de branding en NACHA Config usando una respuesta de `api/users/branding`; el contrato verifica actualización runtime de `--brand-primary` y `--mat-sys-primary`. `BrandingService`, el puente de tokens y el componente de botón continúan siendo la autoridad visual existente.
- EVID-UX0-006: Pruebas Angular/Playwright/.NET focales no pudieron ejecutarse en este host: el ZIP no incluye `node_modules`, la instalación offline falla `ENOTCACHED`, la instalación normal agotó el tiempo disponible, no hay runner local de Playwright utilizable y `dotnet` no está instalado. Esto se registra como `NOT_RUN_ENVIRONMENT`, no como PASS.
- EVID-UX0-007: No hubo cambios productivos del motor NACHA-M, goldens, catálogos, esquema o reglas. Los únicos cambios de UX0 son pruebas, guardrail/manifiesto y documentación de línea base.
- EVID-UX0-008: Los tres specs TypeScript modificados pasaron transpilación sintáctica con el compilador TypeScript 5.8.3 disponible en el host; la ejecución Angular/Playwright completa sigue `NOT_RUN_ENVIRONMENT` por dependencias faltantes.

### Brechas después de UX0

- GAP-UX-012 queda cubierta por regresión automatizada de ruta/listado; su ejecución Playwright queda pendiente de un entorno con dependencias frontend.
- GAP-UX-001 a GAP-UX-011 permanecen abiertas y no fueron consumidas por UX0.

## Evidencia UX1 — 2026-10-11

- EVID-UX1-001: Se agregó `NachaConfigWorkspaceDto` y `GET /nacha-config/perfiles/{id}/workspace`. El contrato conserva códigos técnicos para trazabilidad y añade nombres funcionales entregados por servidor para estado, cámara, flujo, dirección, servicio, registro, estrategia de origen, tipo de origen, tipo de regla y severidad; también expone `IsEditable` desde el estado persistido.
- EVID-UX1-002: Se agregó `GET /nacha-config/catalogos-operativos?camaraCode=&flujoCode=`. Las cámaras, flujos, direcciones y servicios activos salen de las tablas existentes; al seleccionar cámara, servicios se limitan a los específicos de esa cámara más los globales con `ClearingHouseId = null`; un código de cámara desconocido falla cerrado con servicios vacíos. Al seleccionar un flujo con `DirectionDefaultId`, se devuelve únicamente esa dirección activa; un flujo desconocido falla cerrado con direcciones vacías.
- EVID-UX1-003: `Registros`, `TiposOrigen` y `TiposRegla` se incorporaron al contrato de catálogos usando `CatRecordCode.NameEs`, `CatDataSourceType.NameEs` y `CatRuleType.NameEs`. No se creó tabla, migración ni backfill; los servicios oficiales PPD/CCD/CTX ya usan la semántica global existente con `ClearingHouseId = null`.
- EVID-UX1-004: La SPA conserva las rutas y endpoints anteriores y ahora consume el workspace humanizado en detalle, registros y variantes/campos. La creación de perfiles recarga catálogos según cámara y flujo mediante Reactive Forms, invalida selecciones que ya no son válidas y no usa mapas fijos de cámara/flujo/dirección/servicio/estado/origen como autoridad del flujo principal. `nacha-config-presentation.catalog.ts` permanece por compatibilidad, pero sus mapas funcionales principales ya no participan en esas pantallas.
- EVID-UX1-005: Se agregaron pruebas focales de backend para aislamiento ACH Colombia/CENIT, servicios globales, flujo→dirección, nombres persistidos, códigos técnicos invariantes y editabilidad; pruebas Angular para parámetros contextuales, workspace y catálogos vacíos; y actualización del contrato OpenAPI para los dos GET nuevos y los DTO enriquecidos.
- EVID-UX1-006: Validaciones ejecutadas en este host: `PASS_STATIC_UX1 23/23` y transpilación sintáctica TypeScript `PASS_TS_SYNTAX_UX1 12/12`. La validación estática confirma además que los archivos protegidos del motor/goldens no forman parte del conjunto de cambios UX1, que no hay migraciones/esquema ni archivos de branding modificados y que los mapas fijos funcionales principales no son consumidos por las páginas productivas modificadas.
- EVID-UX1-007: Las suites .NET focales/OpenAPI quedan `NOT_RUN_ENVIRONMENT` porque `dotnet` no está instalado. Las suites Angular/Jasmine quedan `NOT_RUN_ENVIRONMENT` porque el proyecto no incluye `node_modules`. En UX1 no se intentó reinstalar esas dependencias, conforme a la instrucción operativa.
- EVID-UX1-008: UX1 queda `CLOSED`; la siguiente fase permitida es UX2. No se avanzó a UX2.

### Brechas cerradas en UX1

- GAP-UX-001: Cerrada como dependencia funcional de la experiencia actual: los catálogos y nombres principales consumidos por las páginas modificadas provienen del backend/persistencia; el archivo de presentación fijo queda solo como compatibilidad/fallback no autoritativo.
- GAP-UX-002: Cerrada mediante el catálogo operativo contextual: servicios específicos por cámara más servicios globales. El endpoint legado `catalogos-filtro` se conserva para compatibilidad.

## Evidencia UX2 — 2026-10-11

- EVID-UX2-001: `/nacha-config-admin/perfiles/:id` quedó como ventana unificada operacional. La página consume `NachaConfigWorkspaceDto` y, una vez conocido cámara/flujo del perfil, solicita `catalogos-operativos` con ambos parámetros; no usa `nacha-config-presentation.catalog.ts` como autoridad.
- EVID-UX2-002: La barra de contexto Angular Material muestra Cámara, Operación/flujo, Dirección y Servicio mediante `mat-form-field` + `mat-select`, y muestra Versión, Vigencia y Estado desde el workspace. Las opciones visibles se forman con el contrato dinámico; no se agregaron listas fijas de cámaras, servicios o registros.
- EVID-UX2-003: La configuración se navega como estructura del archivo → registro humanizado → campos → detalle básico. Registros usan `recordNameEs`; campos usan `fieldNameEs` y nombres de origen del backend. Los códigos/rutas técnicas se relegan a un `mat-expansion-panel`.
- EVID-UX2-004: El workspace fue dividido en componentes cohesionados bajo `features/nacha-config-admin/workspace/`; el contenedor principal quedó en 157 líneas y ningún componente productivo UX2 supera 157 líneas TypeScript. Se usan `mat-card`, `mat-tabs`, `mat-list`, `mat-table`, `mat-chip`, `mat-tooltip` y `mat-dialog` donde corresponde.
- EVID-UX2-005: Perfiles no editables —incluido `PUBLICADO`— muestran protección explícita y no presentan acción de guardado directo. La clonación como borrador se mantiene disponible con permiso; validación/publicación continúan delegadas al backend. No se implementó edición avanzada de campos/reglas.
- EVID-UX2-006: Las rutas legacy `records` y `variants-fields` ahora redirigen al workspace y preservan `profileId`/query context; si llegan sin perfil seleccionan el primer perfil disponible o vuelven al listado. Se agregaron regresiones unitarias y Playwright para evitar 404.
- EVID-UX2-007: Branding del workspace usa exclusivamente tokens runtime (`var(--mat-sys-primary)`, superficies y contraste Material). Se agregó prueba DOM que cambia `--mat-sys-primary` sin reconstrucción y regresión Playwright que responde `api/users/branding` con un color distinto y verifica el token/computed style. No se fijaron colores hexadecimales en estilos productivos UX2.
- EVID-UX2-008: Validaciones ejecutadas en este host: `scripts/validate-nacha-config-ux2.py` → `PASS_STATIC 40/40`; transpilación sintáctica TypeScript de 14 archivos focales → `PASS_TS_SYNTAX_UX2 14/14`; comparación de migraciones contra la base de trabajo → `PASS_MIGRATIONS_UNCHANGED`. El guardrail UX2 verificó además los 11 hashes protegidos del motor/goldens contra el manifiesto UX0.
- EVID-UX2-009: Angular/Jasmine/Playwright quedan `NOT_RUN_ENVIRONMENT` porque `web/ach-interbank-ui/node_modules` sigue ausente. .NET queda `NOT_RUN_ENVIRONMENT` porque `dotnet` no está instalado. Conforme a la instrucción de fase no se intentó reinstalar dependencias.
- EVID-UX2-010: UX2 queda `CLOSED`; `NEXT_ALLOWED_PHASE=UX3`. No se modificaron motor table-driven NACHA-M, goldens, snapshots publicados, esquema, migraciones ni bytes oficiales, y no se avanzó a UX3.

### Brechas cerradas en UX2

- GAP-UX-005: Cerrada para la ventana principal: nombres operativos humanizados son el lenguaje primario y los códigos/rutas técnicas quedan en información avanzada.
- GAP-UX-006: Cerrada mediante la navegación unificada contexto → estructura → registro → campo en `/perfiles/:id`.
- GAP-UX-012: Cerrada mediante redirección compatible de rutas antiguas y regresiones focales de navegación sin 404.


## Evidencia UX3 — 2026-10-11

- EVID-UX3-001: Se agregó `GET /nacha-config/perfiles/{id}/fields/{fieldId}/editor` con `NachaConfigFieldEditorDto`. El read model entrega el campo, reglas existentes, tipos de origen persistidos, opciones de origen ya configuradas en la misma cámara, formatos ya usados en la misma cámara, alineaciones y comportamientos ante error. Los tipos sin una definición contextual compatible no se ofrecen, salvo `CONSTANTE`, que puede configurarse como valor fijo.
- EVID-UX3-002: Se agregó `PUT /nacha-config/perfiles/{id}/fields/{fieldId}/editor` con `NachaConfigFieldEditorUpdateRequest` tipado. El comando actualiza nombre funcional, posición, longitud, habilitación, relleno, alineación, formato, origen y reglas existentes; conserva `ExpectedRowVersion`, exige perfil `BORRADOR`, audita `FIELD_EDITOR_UPDATE` y deja al backend como autoridad de validación.
- EVID-UX3-003: Los orígenes no constantes se seleccionan mediante `SourceDefinitionId` opaco entre definiciones persistidas y usadas por la misma cámara; la UI no permite escribir `PropertyPath`, SQL, JSON ni DSL. Para valor fijo, el backend cuenta referencias de `CfgFieldSourceDefinition`: si la definición es compartida, crea una definición aislada y revincula solo el campo actual dentro de la misma transacción. El endpoint legado de edición de `PropertyPath` recibió la misma protección de fuente compartida para mantener compatibilidad sin mutaciones laterales.
- EVID-UX3-004: El editor Angular Material usa Reactive Forms y presenta en español nombre, posición, longitud, estado, relleno, alineación, formato, origen, valor fijo/dato/cálculo autorizado y reglas. `CatRuleType.NameEs` se usa como nombre visible; código de regla, condición, configuración interna, rutas/entidad/SQL/expresión/fallback quedan en `Información avanzada` de solo lectura. Los códigos de tipos de origen no se codifican como catálogo funcional en Angular; la visibilidad de controles depende de flags del backend.
- EVID-UX3-005: Se incorporó `nacha-config-position-ruler`. La longitud sale de `LayoutVariant.TotalLength` del read model; los bloques se construyen con posiciones/longitudes persistidas, sincronizan selección con el editor, marcan campo inactivo y superposición, y usan botones con `aria-label` descriptivo. No se introdujo una estructura oficial fija ni colores de marca; los estilos usan tokens Material dinámicos.
- EVID-UX3-006: Se agregaron pruebas focales de backend para carga humanizada y aislamiento ACH/CENIT, guardado completo, valor fijo, formato/alineación/relleno, reglas, clonación segura de fuente compartida, revinculación a cálculo persistido sin mutarlo, conflicto de concurrencia y perfil publicado protegido. Se actualizaron pruebas contractuales OpenAPI para el GET del editor y se reservó el PUT tipado; en Angular se agregaron pruebas del editor, cálculo por identificador opaco, publicación protegida, conflicto de concurrencia, regla visual y contratos HTTP.
- EVID-UX3-007: Validaciones ejecutadas en este host: `scripts/validate-nacha-config-ux3.py` → `PASS_STATIC 66/66`; transpilación sintáctica TypeScript focal → `PASS_TS_SYNTAX_UX3 12/12`; comparación de 170 archivos de migraciones contra el checkpoint UX2 → `PASS_MIGRATIONS_UNCHANGED`. El guardrail UX3 verificó también los 11 hashes protegidos del motor/goldens contra el manifiesto UX0.
- EVID-UX3-008: Angular/Jasmine/Playwright no se ejecutó porque `web/ach-interbank-ui/node_modules` sigue ausente (`NOT_RUN_ENVIRONMENT`). Las suites .NET, incluidas las pruebas focales y OpenAPI, no se ejecutaron porque `dotnet` no está instalado (`NOT_RUN_ENVIRONMENT`). Conforme a la instrucción de fase no se reinstalaron dependencias ni se esperó UAT.
- EVID-UX3-009: No se crearon tablas ni migraciones y no se modificaron motor table-driven NACHA-M, goldens, snapshots publicados, esquema, bytes oficiales ni branding. UX3 queda `CLOSED`; `NEXT_ALLOWED_PHASE=UX4`. No se avanzó a UX4.

### Brechas cerradas en UX3

- GAP-UX-003: Cerrada mediante el editor tipado de nombre/posición/longitud/habilitación/relleno/alineación/formato y origen guiado sin rutas técnicas editables.
- GAP-UX-004: Cerrada para las reglas que ya forman parte del modelo autorizado: se administran activación, mensaje operativo y comportamiento ante error usando el nombre persistido del tipo; JSON/DSL/condición permanecen solo lectura.
- GAP-UX-007: Cerrada mediante `nacha-config-position-ruler` basado en el read model, con selección sincronizada, estados de inactividad/superposición y accesibilidad por botón/teclado.


## Evidencia UX4 — 2026-10-11

- EVID-UX4-001: Se agregó `POST /nacha-config/perfiles/{id}/probar` protegido con `P1.ConfigRead` y `INachaConfigOperationalTestService`. La operación es de solo lectura: carga el perfil con `AsNoTracking`, reutiliza `INachaConfigValidationService` y `INachaConfigPreviewService`, y no llama `SaveChanges`, publicación, despacho, MFT, SOAP ni movimiento monetario.
- EVID-UX4-002: La prueba reutiliza `NachaOfficialRecordRenderer`, el renderer determinístico existente del servidor, para una vista previa de contenido sintético cuando la estructura permite render seguro. No se creó renderer paralelo en Angular ni se modificó el renderer oficial. Si posiciones/longitudes o reglas impiden renderizar, la respuesta conserva siempre la vista estructural y explica que el ejemplo de contenido no está disponible.
- EVID-UX4-003: `NachaConfigOperationalTestResultDto` entrega resultado `Apto para publicar` / `Requiere correcciones`, conteos de errores/advertencias, incidencias humanizadas con ubicación registro/campo y acción sugerida, prueba contextual de selección, y registros/campos con longitud, posiciones, origen operativo, formato, obligatoriedad, reglas relevantes y valor sintético resultante. Códigos/trazas/mensajes técnicos quedan separados para `Información avanzada`.
- EVID-UX4-004: La pestaña `Probar` de la ventana unificada incorpora la acción `Probar configuración`, estados inicial/carga/error, resumen de validación, prueba de selección y vista previa navegable por registro/variante. La regla visual usa `preview.totalLength` recibido del backend —106 cuando el layout persistido así lo define— y bloques derivados únicamente de posiciones/longitudes recibidas; no contiene layouts, posiciones ni catálogos NACHA fijos en Angular.
- EVID-UX4-005: La selección de campo se sincroniza entre vista previa y workspace mediante los mismos `selectedRecordCode`/`selectedFieldId`. El detalle muestra nombre, posiciones, valor de ejemplo, origen, formato, obligatoriedad y reglas relevantes en lenguaje operativo. La traza de resolución y códigos técnicos se presentan únicamente en paneles `Información avanzada`.
- EVID-UX4-006: Se agregaron pruebas focales de backend para configuración válida, valor fijo, cálculo mediante renderer oficial, humanización de superposición/longitud inválida/origen faltante/regla inválida, aislamiento ACH/CENIT y perfil `PUBLICADO` sin cambios persistidos. Se agregó validación de contrato de ruta/policy/DTO y prueba HTTP Angular del endpoint; el componente Angular cubre éxito, errores/advertencias, selección sincronizada y error sin mutación.
- EVID-UX4-007: Validaciones ejecutadas en este host: `scripts/validate-nacha-config-ux4.py` → `PASS_STATIC 47/47`; transpilación sintáctica TypeScript focal → `PASS_TS_SYNTAX_UX4 9/9`; comparación del directorio de migraciones contra el checkpoint UX3 → `PASS_MIGRATIONS_UNCHANGED 92/92`. El guardrail UX4 verificó además los 11 hashes protegidos del motor/goldens contra el manifiesto UX0.
- EVID-UX4-008: Angular/Jasmine/Playwright quedan `NOT_RUN_ENVIRONMENT` porque `web/ach-interbank-ui/node_modules` sigue ausente. Las suites .NET, incluidas las nuevas pruebas focales y contractuales, quedan `NOT_RUN_ENVIRONMENT` porque `dotnet` no está instalado. Conforme a la instrucción de fase no se reinstalaron dependencias ni se esperó UAT.
- EVID-UX4-009: No se crearon tablas ni migraciones y no se modificaron motor table-driven NACHA-M, goldens, snapshots publicados, esquema, bytes oficiales ni branding. UX4 queda `CLOSED`; `NEXT_ALLOWED_PHASE=UX5`. No se avanzó a UX5.

### Brechas cerradas en UX4

- GAP-UX-010: Cerrada mediante prueba integral de configuración y vista previa estructural/contenido sintético basada en el backend, con degradación segura a estructura cuando el renderer no puede producir un ejemplo.


## Evidencia UX5 — 2026-10-11

- EVID-UX5-001: Se agregó `GET /nacha-config/perfiles/{id}/cambios` y `NachaConfigChangeReviewDto`. El read model compara el borrador con `SupersedesProfileId` usando el grafo persistido y devuelve solo diferencias reales humanizadas de perfil, registros, variantes, campos y reglas; incluye contexto operativo, vigencia, versión actual, siguiente versión y `RowVersion` fresco.
- EVID-UX5-002: La comparación usa claves funcionales persistidas para alinear elementos clonados y describe cambios de posición, longitud, estado, origen, formato, alineación, relleno y validaciones sin exponer `PropertyPath`, JSON ni DSL en la interfaz principal. Una base ausente o perteneciente a otra cámara falla de forma cerrada, preservando aislamiento ACH/CENIT.
- EVID-UX5-003: `NachaConfigHistoryService` continúa leyendo `HistConfigChange` y ahora entrega acción, elemento, versión, resumen y detalle en lenguaje operativo. Los cambios del editor de campo traducen la auditoría persistida a diferencias entendibles; la SPA ofrece `Ver detalle` sin mostrar el JSON de auditoría.
- EVID-UX5-004: La pestaña `Cambios` implementa la secuencia guiada: validar con `Probar configuración`, recargar la revisión desde servidor, mostrar únicamente cambios reales, solicitar confirmación explícita con contexto/versión/vigencia y publicar con el `RowVersion` recién leído. El estado `publishing` permanece activo durante validación, confirmación y publicación para evitar doble acción desde la UI.
- EVID-UX5-005: La publicación del backend conserva el mecanismo existente fail-closed: vuelve a validar dentro de la transacción, verifica concurrencia, exige `BORRADOR`, crea un único snapshot inmutable `PUBLISH`, registra `HistConfigChange` y cambia a `PUBLICADO`. La respuesta ahora expone también estado y nombre de estado para confirmar la nueva versión en la experiencia operativa.
- EVID-UX5-006: La publicación directa se retiró de las acciones generales del workspace; queda centralizada en la revisión de `Cambios`. Tras publicar, la SPA emite recarga del workspace, que vuelve como configuración protegida/solo lectura. No se agregó despacho de archivos, MFT, SOAP ni otro efecto operativo.
- EVID-UX5-007: Se agregaron pruebas focales backend para diff sin cambios, cambios de posición/origen/reglas, aislamiento de cámara, historial humanizado y publicación única con snapshot/auditoría e intento de segunda publicación rechazado. En Angular se cubren diff vacío, uno/varios cambios, bloqueo por validación, confirmación, uso de `RowVersion` fresco, publicación y prevención de doble acción; se ampliaron pruebas de contrato HTTP/OpenAPI.
- EVID-UX5-008: Validaciones ejecutadas en este host: `scripts/validate-nacha-config-ux5.py` → `PASS_STATIC_UX5 47/47`; transpilación sintáctica TypeScript focal con TypeScript 5.8.3 → `PASS_TS_SYNTAX_UX5 11/11`; comparación de migraciones contra UX4 → `PASS_MIGRATIONS_UNCHANGED` para PostgreSQL `92/92` y SQL Server `79/79`. El guardrail UX5 verificó además los 11 archivos protegidos del motor/goldens contra el manifiesto UX0.
- EVID-UX5-009: Angular/Jasmine/Playwright quedan `NOT_RUN_ENVIRONMENT` porque `web/ach-interbank-ui/node_modules` sigue ausente. Las suites .NET y generación OpenAPI ejecutable quedan `NOT_RUN_ENVIRONMENT` porque `dotnet` no está instalado. Conforme a la instrucción de fase no se reinstalaron dependencias ni se esperó UAT.
- EVID-UX5-010: No se crearon tablas ni migraciones y no se modificaron motor table-driven NACHA-M, goldens, snapshots publicados, esquema, bytes oficiales ni mecanismo de branding. UX5 queda `CLOSED`; `NEXT_ALLOWED_PHASE=UX6`. No se avanzó a UX6.

### Brechas cerradas en UX5

- GAP-UX-008: Cerrada mediante comparación persistida y humanizada antes de publicar.
- GAP-UX-009: Cerrada mediante historial operativo con resumen y detalle entendibles desde `HistConfigChange`.

## Evidencia UX6 — 2026-10-11

- EVID-UX6-001: El módulo NACHA Config quedó endurecido sin agregar funciones nuevas: los selectores operativos consumen catálogos del servidor y conservan filtrado contextual; no permanecen diccionarios funcionales Angular para cámaras, servicios, registros o campos. Se retiraron las vistas legacy monolíticas/supersedidas y el workspace activo continúa compuesto por contexto, registros, regla de posiciones, campos, editor, metadatos, ciclo de vida, Probar, Cambios e Historial.
- EVID-UX6-002: Accesibilidad y consistencia Material fueron revisadas en el workspace: estados de carga/error/vacío usan regiones anunciables cuando aplica, las filas/campos interactivos tienen semántica y navegación de teclado, existe foco visible, el encabezado recibe foco controlado al abrir un perfil, los diálogos restauran foco y la regla de posiciones añade indicador textual/visual de solapamiento para no depender solo del color.
- EVID-UX6-003: Branding permanece 100 % dinámico. Los estilos del módulo consumen tokens existentes y no fijan colores, logos ni nombres corporativos. La regresión UX6 cubre configuraciones de primario oscuro y claro y valida el token de contraste calculado por el `BrandingService` global; no se creó un mecanismo paralelo de contraste en NACHA Config. GAP-UX-011 queda cerrado.
- EVID-UX6-004: Los errores de backend se traducen mediante un helper focal en mensajes claros para Operaciones, incluyendo concurrencia, estado protegido, validación, contexto incompatible, autorización, no encontrado, indisponibilidad y conectividad. Las operaciones fallidas informan que la configuración se conserva sin cambios cuando corresponde.
- EVID-UX6-005: La protección de `PUBLICADO` y la publicación fail-closed se preservan en UI y backend. Edición directa requiere perfil editable/BORRADOR; publicar vuelve a validar, usa `RowVersion` fresco y mantiene snapshot/auditoría. `Probar`, `Cambios` e `Historial` continúan como lecturas sin persistencia propia. El aislamiento ACH/CENIT sigue validado por contexto/cámara y por la revisión de comparación.
- EVID-UX6-006: Las rutas antiguas `/records` y `/variants-fields` continúan aceptadas y redirigen al workspace unificado preservando el contexto de consulta. Se actualizó la regresión de rutas para exigir esa semántica y evitar reintroducir las pantallas retiradas.
- EVID-UX6-007: Se agregó `e2e/nacha-config-ux6.spec.ts` con los 14 flujos mínimos definidos por el plan: apertura desde listado, filtros contextuales, nombres humanos, cancelar sin PUT, guardar/persistir, concurrencia, protección de publicado, validación, Cambios no mutante, Historial no mutante, branding oscuro/claro, tres anchos responsivos, rutas legacy y ausencia de errores de consola/página con verificación de idioma. Estos flujos están declarados pero no se reportan como ejecutados en este host.
- EVID-UX6-008: Todos los validadores estáticos disponibles UX0–UX6 fueron ejecutados: UX0 `PASS_STATIC` 26 checks; UX2 `PASS_STATIC 40/40`; UX3 `PASS_STATIC 66/66`; UX4 `PASS_STATIC 47/47`; UX5 `PASS_STATIC_UX5 47/47`; UX6 `PASS_STATIC_UX6 88/88`. El artefacto no contiene un script `validate-nacha-config-ux1.py`; no se declara un PASS inexistente para UX1. Los guardrails UX0/UX2/UX3 se ajustaron únicamente para reconocer la arquitectura consolidada posterior, sin reabrir funcionalidad de fases cerradas.
- EVID-UX6-009: La regresión sintáctica focal de los 19 TypeScript modificados/nuevos pasó con TypeScript 5.8.3 (`PASS_TS_SYNTAX 19/19`). Es una comprobación de sintaxis/transpilación y no sustituye build Angular, Jasmine ni Playwright.
- EVID-UX6-010: Migraciones permanecen sin cambios frente al checkpoint UX5: PostgreSQL `92/92` y proyecto de migraciones SQL Server `79/79`, con cero archivos modificados/agregados/eliminados. Todo `src/**/*.cs` también permanece idéntico al checkpoint (`1543` archivos, cero cambios), por lo que UX6 no modificó backend ni motor.
- EVID-UX6-011: Los 11 hashes protegidos de motor/goldens definidos por `NACHA_CONFIG_UX0_ENGINE_BASELINE.json` pasan `11/11`. No se tocaron goldens, snapshots publicados, bytes oficiales ni archivos protegidos del motor NACHA-M.
- EVID-UX6-012: `npm run build`, Jasmine y Playwright quedan `NOT_RUN_ENVIRONMENT` porque `web/ach-interbank-ui/node_modules` no está presente; no se reinstaló. `dotnet build` y `dotnet test` quedan `NOT_RUN_ENVIRONMENT` porque `dotnet` no está disponible; no se instaló. No se declara PASS para esas suites y no se esperó UAT ni pruebas operativas.
- EVID-UX6-013: `MEMORY.md` queda sin cambios porque UX6 no introdujo una decisión durable nueva; tampoco fue necesario modificar `NACHA_CONFIG_UX_ARCHITECTURE.md`.
- EVID-UX6-014: `NACHA-CONFIG-UX-001` queda `CLOSED`, UX6 queda `CLOSED` y `NEXT_ALLOWED_PHASE=NONE`. El cierre es del programa de implementación UX y no equivale a UAT, homologación externa, release readiness ni aprobación productiva.

### Brechas cerradas en UX6

- GAP-UX-011: Cerrada mediante consumo exclusivo de tokens/branding dinámico existente, foco/semántica accesible y regresión de contraste para primarios claros y oscuros.

## Regla para actualizar este archivo

Cada fase debe agregar únicamente:

- evidencias verificadas;
- decisiones nuevas realmente tomadas;
- brechas cerradas o abiertas;
- estado de la fase;
- siguiente fase permitida.

No copiar logs completos ni registrar suposiciones como hechos.

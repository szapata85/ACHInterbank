# Preanálisis dirigido — Integraciones con Angular Material

## Alcance y estado inicial

El análisis cubre exclusivamente `/integraciones/mappings` y `/integraciones/soap-settings`. La rama inicial es `ACH-Interbank-Postgresql`, el commit base es `618f09cfd14811ed0ec6266db22cf8c09845e600` y el worktree estaba limpio. API, SPA y SQL Server estaban activos y saludables; `health/live`, `health/ready` y la SPA respondían HTTP 200.

La línea base LIVE local se ejecutó de forma no mutante. Las dos rutas cargaron sin errores HTTP o JavaScript. Los endpoints visibles antes y después de abrir/cancelar edición, navegar a mappings, volver y recargar permanecieron idénticos. No se detectaron actualizaciones administrativas ni invocaciones de `Proc_Contrapartidas`, `Proc_Transacciones` o `RegistrarRespuestaTransaccion`.

## Componentes y relación técnica

| Ruta | Componentes | Responsabilidad |
| --- | --- | --- |
| `/integraciones/mappings` | `IntegrationWorkspaceComponent`, `MappingSetsPageComponent`, `MappingEditorPageComponent` | Catálogo, versiones y reglas funcionales por método SOAP |
| `/integraciones/soap-settings` | `IntegrationWorkspaceComponent`, `SoapIntegrationSettingsComponent` | Endpoint, SOAP Action, modo y habilitación técnica |

Ambas vistas comparten únicamente el contenedor y navegación. Mappings recibe el método por query string; no recibe ni modifica el DTO editable de settings.

## Servicios, modelos y endpoints

- `IntegrationMappingAdminService`: `IntegrationMethod`, parámetros, catálogo de fuentes, transformaciones, `IntegrationMappingSet`, reglas, validación, vista previa e historial.
- Lecturas: `GET api/integrations/methods`, `methods/{id}/parameters`, `source-catalog`, `transformations`, `mappingsets`, `mappingsets/{id}`, `mappingsets/{id}/history`.
- Escrituras explícitas: `POST mappingsets`, `PUT mappingsets/{id}`, `PUT mappingsets/{id}/rules`, `POST validate`, `preview`, `publish`, `clone`.
- `SoapIntegrationSettingsService`: agregado `SoapIntegrationSettings` con métodos de ambos clientes y parámetros de contrato.
- Settings: `GET api/users/soap-integrations` y `PUT api/users/soap-integrations`.
- El backend actual no ofrece timeout, autenticación ni prueba de conexión; no se agregarán controles ficticios.

## Formularios y permisos

- Mappings usa formularios reactivos para borradores y reglas. Lectura requiere `CanReadAch`; mutación requiere `CanManageAch`.
- Settings usa formularios reactivos por método. El backend protege lectura/escritura con `P1.ConfigRead` y `P1.ConfigManage`; la UI debe aplicar el patrón equivalente `Config.Manage`/`CanManageAch`.
- La ruta padre permite roles `Admin` y `ACH.Operator` con `CanReadAch` o `CanManageAch`.

## Defectos confirmados

1. Settings construye y envía el agregado completo desde todos los `FormArray` al guardar un solo método. Un valor obsoleto o accidentalmente mutado puede sobrescribir otro servicio.
2. `SoapIntegrationSettingsService.GetAsync` normaliza y persiste JSON durante una consulta. Abrir o recargar la pantalla puede escribir en base de datos.
3. El singleton Angular publica objetos recibidos sin una frontera inmutable explícita.
4. El botón Guardar no exige cambios reales y la UI no aplica permiso de escritura.
5. En mappings, abrir “Editar relación” clona inmediatamente una versión publicada; existe persistencia antes de Guardar.
6. Los filtros de mappings solo cubren estados predefinidos: faltan búsqueda y obligatoriedad.
7. Ambas vistas usan principalmente controles, tablas y paneles personalizados; faltan los patrones Material solicitados, `mat-error` y estados accesibles consistentes.
8. La captura inicial de mappings evidenció un estado transitorio invasivo y la versión móvil requiere scroll interno y mejor jerarquía.

## Causa del antecedente de endpoints

El copiado no se reprodujo en la secuencia segura actual, pero el riesgo es verificable: el frontend edita formularios pertenecientes a un agregado y el único `PUT` reemplaza ambos clientes completos. No hay identidad de fila persistente más allá de `methodName`; por ello cualquier referencia o valor obsoleto incluido en el payload puede reemplazar el endpoint correcto. El `GET` con escritura agrava la trazabilidad porque una navegación puede producir persistencia de normalización sin acción de Guardar.

La corrección conservará el contrato agregado: se mantendrá una instantánea canónica profunda, se creará una copia editable aislada solo para el servicio seleccionado y, al guardar, se sustituirá exclusivamente ese método dentro de una copia del agregado persistido. El `GET` dejará de escribir.

## Archivos previstos

- Componentes TS/HTML/SCSS y pruebas de settings.
- Página TS/HTML/SCSS y pruebas de matriz.
- Editor avanzado TS/HTML/SCSS y pruebas solo cuando sea necesario para coherencia Material y permisos.
- Servicio Angular de settings para copias defensivas.
- Servicio backend de settings y pruebas dirigidas para consulta sin escritura.
- Spec Playwright focalizado y evidencias de los tres viewports.

## Estrategia de implementación y pruebas

1. Incorporar imports Material locales sin cambiar paquetes ni tema.
2. Aislar edición, detectar cambios reales, impedir doble envío y aplicar permisos.
3. Diferir clonación/creación de mappings hasta el Guardar explícito.
4. Añadir pestañas por servicio, búsqueda, filtros, tabla Material y diálogos/paneles accesibles.
5. Ejecutar pruebas unitarias focalizadas y build de SPA; después suite Angular y backend dirigida/completa.
6. Reconstruir solo API/SPA si es necesario y ejecutar Playwright LIVE con interceptores que fallen ante procedimientos SOAP operativos.

## Riesgos y reversión

- El endpoint de settings reemplaza el agregado completo: la instantánea canónica y las pruebas de endpoints cruzados son obligatorias.
- Clonar una versión publicada crea auditoría y no tiene borrado expuesto: la prueba LIVE no abrirá guardado de mappings salvo un escenario reversible ya existente.
- Para una persistencia reversible en settings se capturará el agregado original autenticado, se cambiará únicamente un valor técnico inocuo sin tocar endpoint ni modo, y se restaurará en `finally`; después se verificará igualdad exacta.
- No se ejecutará SOAP, no se cambiará DryRun/Live y no se imprimirán credenciales o secretos.

## Criterios de aceptación

- Solo Guardar produce escritura; abrir, cancelar, seleccionar o navegar son de solo lectura.
- Los endpoints de servicios distintos no comparten referencias ni se sobrescriben.
- Mappings y settings usan Material, Reactive Forms, `mat-error`, estados explícitos y permisos.
- Desktop, tablet y móvil son utilizables sin desbordamiento del body.
- Unitarias, build y Playwright LIVE pasan; los valores alterados se restauran y Git queda sin commit.

## Compuerta

`APTO CON AJUSTES`: el backend admite las capacidades requeridas y no hay conflicto local. Los ajustes obligatorios son eliminar la escritura del `GET`, aislar el método editable aunque el contrato de guardado siga siendo agregado y diferir la clonación de mappings hasta Guardar.

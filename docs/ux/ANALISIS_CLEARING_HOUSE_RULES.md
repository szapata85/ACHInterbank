# Diagnóstico de `/transactions/clearing-house-rules`

## 1. Propósito encontrado

**Decisión: MIGRAR A OTRO MÓDULO. Confianza: ALTA.**

La implementación entiende una “regla de cámara compensadora” como una política versionada por cámara, naturaleza y tipo de transacción que determina si una transacción monetaria requiere prenotificación y validación de identificación antes de su exportación NACHA-M.

La función es válida y tiene un consumidor productivo, por lo que no debe retirarse. Sin embargo, el CRUD actual no es una fuente de verdad estable: los cuatro registros base son reescritos por el seeder al inicializar la API, existen dos catálogos globales con reglas equivalentes y varios campos editables son redundantes o no influyen en el procesamiento. La administración debe migrar al módulo **Cámaras compensadoras**, conservando temporalmente la tabla y el servicio productivo.

## 2. Arquitectura implicada

Flujo administrativo:

1. Ruta `clearing-house-rules` protegida con `CanManageAch`: `web/ach-interbank-ui/src/app/features/transactions/transactions-routing.module.ts:69`.
2. `ClearingHouseTransactionRulesComponent`: formulario, filtros, grilla, vista previa, creación, edición y activación/inactivación; `clearing-house-transaction-rules.component.ts:31`.
3. `ClearingHouseTransactionRulesApiService`: `api/clearing-house-transaction-rules`; `clearing-house-transaction-rules-api.service.ts:15`.
4. `ClearingHouseTransactionRulesController`: GET/GET por ID con `P1.ConfigRead`; POST/PUT/PATCH con `P1.ConfigManage`; `ClearingHouseTransactionRulesController.cs:12`.
5. `IClearingHouseTransactionRuleService` y `ClearingHouseTransactionRuleService`: lectura y escrituras administrativas, validación de vigencias solapadas y persistencia EF Core.
6. `ClearingHouseTransactionRule`: entidad de dominio; `ClearingHouseTransactionRule.cs:7`.
7. `ClearingHouseTransactionRuleConfiguration`: tabla, conversiones de enum, índice de búsqueda y FK restrictiva hacia `ClearingHouses`.

Flujo productivo:

1. `NachaTransactionValidationService.ValidateTransactionsForSendAsync` llama a `ValidateForNachaExportAsync`; `NachaTransactionValidationService.cs:45`.
2. `NachaFileBuilder` vuelve a aplicar la misma política antes de construir el archivo; `NachaFileBuilder.cs:3909`.
3. `TransactionPrerequisitePolicyService.ResolveRuleAsync` selecciona una regla activa y vigente por cámara, naturaleza, tipo y alcance; `TransactionPrerequisitePolicyService.cs:117`.
4. La ausencia de regla produce `NACHA_EXPORT_RULE_NOT_CONFIGURED`; la ausencia de prenotificación obligatoria produce `NACHA_EXPORT_PREREQUISITE_FAILED`.
5. La espera de tres días hábiles se calcula con el calendario de festivos; `TransactionPrerequisitePolicyService.cs:108,155`.

Clasificación de referencias:

| Artefacto | Clasificación |
| --- | --- |
| Componente, API Angular y controlador CRUD | Escritura administrativa |
| Endpoint `transaction-prerequisite-policy/preview` | Lectura únicamente para mostrar información |
| `TransactionPrerequisitePolicyService` | Validación y lectura productiva |
| `NachaTransactionValidationService` y `NachaFileBuilder` | Consumidores productivos |
| `RegulatoryCatalogSeeder` | Seed y sobrescritura de configuración base |
| `AchPrenotificationPolicies` y `AchTransactionTypePolicies` | Catálogos de lectura, sin consumidor productivo encontrado |
| Pruebas de política, seeder y navegación | Prueba |

## 3. Modelo de datos

Tabla principal: `ClearingHouseTransactionRules`.

Campos reales: `ClearingHouseId`, `TransactionNature`, `TransactionType`, `RequiresPrenotification`, `PrenotificationMode`, `RequiresReceiverIdentificationValidation`, `ReceiverIdentificationValidationMode`, `AppliesToNachaExport`, `AppliesToMonetaryTransactions`, `EffectiveFrom`, `EffectiveTo`, `IsActive`, `NormativeSource`, `NormativeReference`, `Notes`, `CreatedAt` y `UpdatedAt`.

Hallazgos:

- `ClearingHouseId` tiene FK restrictiva a `ClearingHouses`.
- No existe prioridad; si hubiera más de una regla coincidente, gana la de `EffectiveFrom` más reciente.
- El servicio impide solapamientos activos para cámara/naturaleza/tipo, pero la comprobación asume siempre alcances NACHA y monetario verdaderos.
- `TransactionNature` es derivable de `TransactionType` en `ResolveNature`, pero ambos se editan independientemente y el backend no valida su coherencia.
- `RequiresPrenotification` duplica semántica con `PrenotificationMode`; el consumidor exige que ambos indiquen obligatoriedad.
- Los campos de validación de identificación solo se devuelven en la vista previa; no intervienen en la decisión productiva encontrada.
- El flujo productivo solo consulta reglas con `AppliesToNachaExport=true` y `AppliesToMonetaryTransactions=true`; las variantes falsas editables no tienen consumidor productivo.
- No existe eliminación en el API; solo creación, actualización, activación e inactivación.

Estado local de solo lectura:

- 4 registros, todos activos, sin solapamientos duplicados.
- ACH Colombia: débito obligatorio y crédito opcional.
- CENIT: débito obligatorio y crédito opcional.
- Todos vigentes desde `2025-01-01`, sin fecha final.
- Fuentes sembradas: `MAN-004 ACH Colombia V32` y `CENIT DSP-152 Anexo 2`.

El `RegulatoryCatalogSeeder.BuildClearingHouseTransactionRules` define esas cuatro filas (`RegulatoryCatalogSeeder.cs:463`). `UpsertClearingHouseTransactionRulesAsync` vuelve a copiar modos, banderas, estado, fuente, referencia y notas sobre filas existentes (`:259-281`). `DbInitializer` ejecuta los seeders con `AuditEnabled=false` (`DbInitializer.cs:17,23`). Por ello, editar o inactivar una fila base desde la SPA no es durable frente a una inicialización posterior.

## 4. Consumidores reales

Consumidor confirmado: exportación NACHA-M.

- `TransactionPrerequisitePolicyService.ValidateForNachaExportAsync` resuelve la cámara desde el ciclo, busca la regla vigente y bloquea la transacción si no hay regla o prenotificación obligatoria.
- `NachaTransactionValidationService` convierte el rechazo en `NachaGenerationException`.
- `NachaFileBuilder` impide continuar la construcción del archivo cuando la política falla.
- `TransactionPrerequisitePolicyServiceTests` demuestra: débito ACH Colombia sin prenota falla, crédito sin prenota pasa y CENIT sin regla configurada falla.

No se encontró lectura de estas reglas en procesamiento entrante, devoluciones, respuestas diferenciales, conciliación, liquidación, Quartz ni dispatch SOAP.

## 5. Evidencia normativa

### ACH Colombia

Fuente local: `docs/normativa/md/ACH-Colombia-V32.md`.

- §2.10.2 y §2.10.3: la prenotificación de crédito y la validación de identificación son opcionales (`:4947-5008`).
- §2.10.3.2: si se realiza prenotificación crédito, debe hacerse mínimo tres días hábiles antes de la primera transacción (`:5019-5038`).
- §2.11.4: la prenotificación débito es obligatoria y previa, una vez por usuario/autorización (`:5483-5600`).
- §2.11.4.2: exige mínimo tres días hábiles antes de la primera transacción débito (`:5611-5616`).
- §2.11.7: el receptor debe validar cuenta e identificación para prenotificaciones y débitos (`:5867-5914`).

El documento extraído se llama V32, pero los encabezados de las páginas citadas muestran “VERSIÓN 31 / Agosto de 2024”; la versión documental debe verificarse antes de publicar nuevas reglas.

### CENIT

Fuente local: `docs/normativa/md/CENIT-DSP-152-Anexo-2.md`.

- §4.7: antes de una entrada débito el originador debe enviar obligatoriamente una prenotificación con adenda; el receptor debe validar los campos recibidos (`:472-481`).
- §4.7: para crédito la prenotificación es potestativa y expresamente no obligatoria (`:484-489`).
- No se encontró en §4.7 un plazo de tres días hábiles. El código aplica el mismo plazo de tres días a toda regla obligatoria, incluida CENIT, sin distinguir fuente.

Conclusión normativa: la distinción débito obligatorio/crédito opcional está demostrada para ambas cámaras. La administración versionada puede ser válida ante cambios normativos, pero no es correcto exponer valores base sin control documental, aprobación, coherencia y auditoría.

## 6. Duplicidades y fuente de verdad

1. `AchPrenotificationPolicies`: repite Débito requerido/bloqueante y Crédito opcional; solo se encontró lectura mediante `AchRegulatoryCatalogService.GetPrenotificationPoliciesAsync`, no en procesamiento productivo.
2. `AchTransactionTypePolicies.RequiresPrenotification`: vuelve a declarar Débito `true` y Crédito `false`; tampoco es la política leída por el exportador.
3. `RequiresPrenotification` y `PrenotificationMode` duplican la misma decisión dentro de la propia entidad.
4. `TransactionNature` y `TransactionType` duplican una clasificación que el consumidor deriva del tipo.
5. `RegulatoryCatalogSeeder` actúa como fuente de verdad efectiva para las cuatro filas base y contradice la expectativa de administración persistente desde la SPA.
6. El fallback legado de validación de prenotas permanece en `NachaTransactionValidationService` y `NachaFileBuilder` cuando el servicio de política no está disponible.

No se encontraron campos equivalentes en configuración de ciclos, calendarios, `OnlyBusinessDays`, scheduler ni perfiles NACHA-M. Esos módulos resuelven horarios, días de ejecución o estructura de archivos, no prerrequisitos transaccionales.

El módulo propietario recomendado es **Cámaras compensadoras**: la política está indexada y normada por cámara, y el repositorio ya ubica allí configuraciones hijas como `/clearing-houses/:id/cycles` (`app-routing.module.ts:136-142`). Los perfiles NACHA-M deben seguir siendo la fuente de verdad del layout, no de esta política funcional.

## 7. Matriz de evidencia

| Regla o campo | Uso aparente | Fuente normativa | Consumidor productivo | Duplicidad | Clasificación |
| --- | --- | --- | --- | --- | --- |
| Cámara | Diferencia ACH Colombia/CENIT | Ambas normas | Sí, resolución por ciclo | No | NORMATIVA Y PARAMETRIZABLE |
| Naturaleza + tipo | Selección débito/crédito | Ambas normas | Sí | Mutuamente redundantes | DUPLICADA |
| Prenotificación obligatoria/opcional | Bloquea o permite exportar | ACH §§2.10.3/2.11.4; CENIT §4.7 | Sí | Dos campos y dos catálogos globales | NORMATIVA Y PARAMETRIZABLE |
| Validación de identificación | Informa obligación del receptor | ACH §2.11.7; CENIT §4.7 | No; solo preview | Booleano + modo | SIN CONSUMIDOR |
| Aplica a export NACHA-M | Define alcance técnico | No encontrada como regla editable | Solo se consume `true` | No | CONFIGURACIÓN TÉCNICA |
| Aplica a monetarias | Define alcance técnico | Implícita en tipo de entrada | Solo se consume `true` | `AchTransactionTypePolicies.IsMonetary` | CONFIGURACIÓN TÉCNICA |
| Vigencia y estado | Versionado operativo | Necesidad de gobernar versiones | Sí | Seeder reactiva filas base | OPERATIVA Y PARAMETRIZABLE |
| Fuente y referencia normativa | Trazabilidad | Documentos citados | Solo preview/administración | Seeder las sobrescribe | NORMATIVA Y PARAMETRIZABLE |
| Notas | Explicación administrativa | No obligatoria | No | No | SIN CONSUMIDOR |
| Espera de 3 días hábiles | Bloqueo temporal hardcoded | ACH §§2.10.3.2/2.11.4.2 | Sí, para toda regla obligatoria | Calendario de festivos solo aporta días | SIN EVIDENCIA para aplicación genérica a CENIT |

## 8. Decisión

**MIGRAR A OTRO MÓDULO: Cámaras compensadoras.**

Fuente de verdad propuesta: una única política transaccional versionada por cámara, consumida por `ITransactionPrerequisitePolicyService`. La tabla actual puede conservarse durante la transición para no romper exportaciones, pero debe dejar de competir con catálogos globales y con un seeder que sobrescribe decisiones administrativas.

La ruta actual debe mantenerse temporalmente en modo compatible o de solo lectura. Solo debe retirarse después de migrar la administración, reconciliar datos y demostrar que `NachaTransactionValidationService` y `NachaFileBuilder` resuelven exactamente la misma política.

## 9. Riesgos

### Riesgo de conservar la pantalla sin cambios

- Una modificación normativa puede revertirse silenciosamente al reiniciar o ejecutar mantenimiento.
- Una combinación incoherente naturaleza/tipo o booleano/modo puede crear una regla que nunca se resuelva.
- Inactivar una fila base puede bloquear todas las exportaciones correspondientes hasta el siguiente seed; luego reaparecería activa sin auditoría de esa restauración.
- Los campos de alcance falsos generan configuraciones aparentemente válidas pero sin efecto productivo.
- La validación de identificación se presenta como configurable aunque no cambia la decisión productiva.
- CENIT hereda un plazo fijo de tres días que no está demostrado por la sección normativa citada.
- La ruta exige `CanManageAch`, el menú incluye `CanReadAch` y el API usa `P1.ConfigRead/ConfigManage`; existe deriva de permisos y de expectativas de solo lectura/escritura.

### Riesgo de retirar la función

- La exportación NACHA-M fallaría con `NACHA_EXPORT_RULE_NOT_CONFIGURED`.
- Débitos sin prenotificación podrían quedar bloqueados o, si se reactivara el fallback legado, depender de una regla menos específica por cámara.
- Se perdería la distinción normativa ACH Colombia/CENIT y el versionado por vigencia.

## 10. Plan de implementación posterior

1. Declarar `ITransactionPrerequisitePolicyService` y su almacén versionado como fuente de verdad única.
2. Congelar la ruta actual para nuevas escrituras mientras se realiza la transición.
3. Incorporar la administración como sección hija de cada cámara compensadora.
4. Mantener temporalmente los endpoints y la tabla actuales por compatibilidad.
5. Cambiar el seeder de sobrescritura a bootstrap controlado, idempotente y auditable.
6. Reconciliar las cuatro filas locales con las referencias normativas verificadas.
7. Derivar `TransactionNature` de `TransactionType` o validar obligatoriamente su coherencia.
8. Sustituir booleano + modo por una única decisión canónica.
9. Retirar de la UI los alcances técnicos sin consumidor o implementar consumidores explícitos antes de exponerlos.
10. Implementar el efecto real de validación de identificación o dejarlo como metadato normativo no editable.
11. Modelar el plazo previo por cámara y versión; no aplicar un valor global sin evidencia.
12. Consolidar o retirar `AchPrenotificationPolicies` y `AchTransactionTypePolicies.RequiresPrenotification` después de verificar sus clientes de lectura.
13. Alinear permisos de ruta, menú, lectura y administración.
14. Agregar pruebas de compatibilidad, vigencia, concurrencia, auditoría y exportación para ambas cámaras.
15. Retirar la ruta y el menú antiguos únicamente después de la migración de datos y una verificación end-to-end sin diferencias.

## Evidencia de runtime

El 28 de julio de 2026, SPA y API locales respondieron HTTP 200. La pantalla cargó cuatro filas mediante `GET /api/clearing-house-transaction-rules`, sin errores de consola ni respuestas HTTP fallidas. Se abrió y canceló el formulario de creación: no se generaron POST, PUT, PATCH ni DELETE. No se modificaron datos.

## 11. Consolidación backend — JOB 1

### Modelo y fuente de verdad

La fuente de verdad productiva continúa siendo `ClearingHouseTransactionRules`, resuelta exclusivamente por `ITransactionPrerequisitePolicyService` para la fecha efectiva, cámara y tipo de transacción. `TransactionNature` y `RequiresPrenotification` se conservan por compatibilidad, pero se derivan respectivamente de `TransactionType` y `PrenotificationMode`; los contratos antiguos rechazan combinaciones incoherentes. Los alcances técnicos se fijan en `true`.

Se agregó `PrenotificationLeadBusinessDays` nullable. ACH Colombia débito usa `Mandatory` y `3`; ACH Colombia crédito usa `Optional` y `null`; CENIT débito usa `Mandatory` y `null`; CENIT crédito usa `Optional` y `null`. Un valor configurado se calcula con `IBankHoliday`; `Mandatory` sin plazo exige prenotificación sin inventar antigüedad.

### Versionado y API

Una decisión funcional nueva crea una versión y cierra la anterior el día previo. El servicio rechaza fechas inválidas, fechas iniciales duplicadas y solapamientos activos; permite versiones futuras y resuelve como máximo una versión por fecha. La edición in-place queda limitada a fuente, referencia y notas. No existe eliminación física.

La API canónica es `api/clearing-houses/{clearingHouseId}/transaction-policies` y ofrece listado de versiones, vigente, detalle, creación de versión, metadatos, cierre, activación y preview. Lecturas usan `P1.ConfigRead`; escrituras usan `P1.ConfigManage`, que conservan compatibilidad con `CanReadAch` y `CanManageAch`. El endpoint anterior permanece como adaptador sobre el mismo servicio.

### Seed, duplicidades y consumidor

`RegulatoryCatalogSeeder` identifica `CENIT` y los códigos estables de ACH Colombia (`ACHCOL`, con compatibilidad para `ACH`) sin depender de IDs ni nombres, y solo inserta políticas faltantes. Una segunda ejecución, el bootstrap o `/seed` no reactivan ni sobrescriben decisiones administrativas existentes.

- `RequiresPrenotification`: derivado y conservado por compatibilidad.
- `TransactionNature`: derivada/validada y conservada por compatibilidad.
- `AchPrenotificationPolicies`: catálogo de lectura administrativa, sin consumidor productivo; pendiente de retiro.
- `AchTransactionTypePolicies.RequiresPrenotification`: catálogo de lectura administrativa, sin consumidor productivo; pendiente de retiro.
- Campos de validación de identificación: informativos/compatibles, pendientes de consumidor real.
- Fallback en `NachaTransactionValidationService` y duplicado privado en `NachaFileBuilder`: eliminados.

`NachaFileBuilder` delega la validación en `INachaTransactionValidationService`; este exige `ITransactionPrerequisitePolicyService`. La falta de política produce error funcional controlado y la falta de la dependencia impide construir el servicio, sin activar reglas genéricas.

### Persistencia, migraciones y pruebas

Las migraciones `CanonicalClearingHouseTransactionPolicies` de PostgreSQL y SQL Server agregan la columna nullable, asignan `3` solo a versiones ACHCOL débito obligatorias y dejan CENIT en `null`. No eliminan columnas ni tablas compatibles. Se validaron scripts forward y rollback de ambos proveedores.

Las pruebas focalizadas cubren resolución por cámara/tipo/fecha, los siete casos ACH/CENIT, plazo nullable, versionado, cierre, solapamiento, coherencia de compatibilidad, seed insert-only, ausencia de fallback, API anidada y permisos.

### Riesgos residuales

- Los dos catálogos globales duplicados continúan disponibles para clientes administrativos hasta que se verifique y ejecute su retiro.
- La validación de identificación sigue siendo metadato sin decisión productiva.
- La auditoría conserva sellos de creación/actualización; una bitácora de aprobación normativa con identidad y motivo requiere un trabajo posterior.
- La ruta Angular antigua permanece hasta el JOB 2 y debe migrarse antes de retirar el adaptador HTTP.

## 12. Migración frontend — JOB 2

La administración se trasladó a `/clearing-houses/:id/transaction-policies`, como pantalla hija de Cámaras compensadoras. El listado agrega el acceso contextual **Políticas transaccionales**; la cámara se obtiene exclusivamente de la ruta y no forma parte del formulario.

La pantalla `TransactionPoliciesComponent` usa Angular Material, Reactive Forms tipados y el servicio `TransactionPoliciesService` sobre `api/clearing-houses/{clearingHouseId}/transaction-policies`. Muestra contexto de cámara, resumen de débito/crédito, historial versionado y acciones de crear versión, metadatos, cierre, activación y preview. El plazo nullable se presenta como **Sin plazo mínimo documentado** cuando corresponde a CENIT.

La ruta histórica `/transactions/clearing-house-rules` redirige a `/clearing-houses`. Se retiró su registro del seed actual y el cliente filtra la ruta persistida heredada para impedir que aparezca durante la transición. Lectura usa `Config.Read`/`CanReadAch`; escritura usa `Config.Manage`/`CanManageAch`.

Se validaron build Angular y pruebas focalizadas. El spec Playwright de runtime real queda preparado en `e2e/clearing-house-transaction-policies-live.spec.ts`; su ejecución se omitió en esta sesión porque no había una credencial E2E local expuesta.

## 13. Cierre de UX y runtime

### Causa de la carga y Docker detectado

El spinner local no provenía de una petición pendiente: el contenedor SPA conservaba el chunk lazy anterior `902.5b79a6ffecef1a55.js`, mientras el código fuente ya tenía `finalize` dentro del `forkJoin` por identificador. El runtime activo era el proyecto Compose `achinterbank`, levantado únicamente con `docker-compose.yml`, API y SPA en 843/743 y SQL Server en el contenedor `achinterbank-sqlserver-1`. La reconstrucción y recreación real sustituyó los contenedores; no se reinició sobre la misma imagen ni se eliminó `achinterbank_ach_sqlserver_data`.

El diagnóstico autenticado encontró además dos defectos independientes: la API serializa `PrenotificationRequirementMode` como enum numérico y el cliente lo trataba como texto, por lo que mostraba `No aplica`; y los nuevos `mat-icon` usaban el font-set legacy aunque la aplicación empaqueta Material Symbols. El adaptador HTTP ahora normaliza 1/2/3 a `Mandatory`/`Optional`/`NotApplicable`, envía el enum numérico al API y la fuente local de símbolos queda registrada globalmente.

### Arquitectura frontend y rediseño

`/clearing-houses` quedó como centro administrativo Material con métricas, filtros tipados por nombre/código y estado, tabla ordenable y paginada, tarjetas móviles, chips textuales, menú contextual, estados de carga/vacío/sin resultados/error y editor Material con validaciones específicas. Crear, editar y cambiar estado respetan los permisos existentes y usan snackbars y diálogos; no existen `window.prompt`, `window.confirm` ni `window.alert`.

`ClearingHouseContextNavigationComponent` concentra nombre, código, estado, regreso al listado y subnavegación a rutas comprobadas: políticas, ciclos y fechas especiales. Los enlaces se filtran con permisos reales y se reutilizan en políticas y ciclos. En móvil el tab-nav es desplazable y mantiene las acciones dentro del viewport. No se creó una opción principal para políticas ni se restauró `Reglas por cámara`.

La pantalla de políticas mantiene un único `h1` en el shell y usa un `h2` funcional en el contenido. Incluye resumen vigente de débitos/créditos, impacto NACHA-M, historial Material con estados vigentes/futuros/históricos/inactivos, vista móvil por tarjetas, formulario reactivo tipado, preview funcional y diálogos para crear, editar metadatos, cerrar y activar. El plazo opcional se limpia, deshabilita y envía como `null`; el obligatorio admite entero no negativo o vacío. Las fechas normativas se muestran en UTC para conservar el día efectivo.

ACH Colombia presenta débito obligatorio con 3 días hábiles y crédito opcional sin plazo. CENIT presenta débito obligatorio con `Sin plazo mínimo documentado` y crédito opcional; no se le atribuyen tres días.

### Permisos, responsive y accesibilidad

Las lecturas aceptan `Config.Read`, `Config.Manage`, `CanReadAch` o `CanManageAch`; las escrituras requieren `Config.Manage` o `CanManageAch`. Ciclos y fechas especiales mantienen sus permisos dedicados. El Administrador ACH del runtime aislado visualizó la acción contextual de políticas y las operaciones administrativas.

Se validaron 1440×900, tablet y 390×844. La tabla cambia a tarjetas, los filtros se apilan, la navegación secundaria permanece utilizable y no existe overflow horizontal. El flujo conserva un solo encabezado principal, jerarquía semántica, foco Material, etiquetas ARIA, tooltips, regiones `status`/`alert`, texto además de color y foco gestionado por los diálogos.

### Pruebas y evidencia

- Pruebas focalizadas Angular: 23/23 exitosas, incluidas normalización del enum API, carga, error, ID inválido/cambiante, nullable, permisos, formularios y navegación.
- Build Angular: exitoso; chunk final de políticas `798.8f45db0dbde9e6ac.js`, cámaras `264.4026509d7222130f.js` y contexto compartido `911.a46e968941784aaf.js`.
- Suite Angular completa: 579/579 exitosa en la repetición final, sin omitidas.
- Build .NET Release: exitoso, 0 warnings y 0 errores. Suite backend multi-base: 2005 aprobadas, 0 fallidas y 5 omisiones históricas de 2010; TRX en `artifacts/clearing-house-ux-final/clearing-houses-final-multidb.trx`.
- Playwright autenticado: 1/1 escenario integral exitoso sobre PostgreSQL aislado en 1743/1843, con credenciales efímeras, navegación por teclado en filtros/formulario y sin errores de página/consola, 4xx/5xx inesperados, spinner permanente, loop ni overflow.
- Capturas: `artifacts/clearing-house-ux-final/clearing-houses-desktop.png`, `clearing-houses-mobile.png`, `transaction-policies-achcol-desktop.png`, `transaction-policies-cenit-desktop.png`, `transaction-policies-tablet.png` y `transaction-policies-mobile.png`.
- Logs del runtime aislado: `artifacts/clearing-house-ux-final/isolated-compose.log` y `isolated-compose-ps.log`.

El runtime aislado y su volumen PostgreSQL fueron eliminados al terminar. El runtime principal quedó nuevamente construido y recreado con SQL Server, API y SPA saludables; sus rutas directas devuelven 200 y los endpoints canónicos rechazan sin credencial en tiempo finito con 401.

### Riesgos residuales

- Una primera ejecución final de la suite Angular completa presentó ocho fallos transitorios; la repetición inmediata con el mismo código cerró 579/579. La suite tiene inestabilidad intermitente comprobada fuera de los specs focalizados.
- La validación autenticada se realizó en el runtime desechable permitido porque no había contraseña del administrador persistente expuesta. El Docker principal usa los mismos bundles finales y se verificó por health, rutas, hashes y literales, pero no se modificó la credencial local para repetir allí el login.
- Los catálogos globales duplicados y la validación de identificación sin consumidor, ya documentados en el cierre backend, permanecen como compatibilidad y no se alteraron en este trabajo de UX.

## 14. Cierre real del workspace de cámara

### Brechas iniciales y resultado

La inspección inicial confirmó que Ciclos mezclaba selectores y grilla empresariales, clases `panel`/`btn`, inputs nativos, confirmación legacy y una cámara editable pese a estar en la ruta. Fechas especiales mantenía select nativo, grilla legacy, `document.createElement`, `confirm()` y textos con codificación incorrecta. Políticas daba protagonismo a fuente y referencia normativa. El listado de cámaras excedía visualmente el área útil a 1024×768 por conservar la tabla de escritorio en un layout con sidebar.

Ciclos y Fechas especiales quedaron reconstruidos como pantallas Angular Material declarativas. La cámara procede exclusivamente de `:id`, se recarga ante cambios del parámetro y los estados de ID inválido, inexistencia, carga, error y permisos se presentan dentro de la pantalla. No quedan selectores de cámara ni componentes legacy visibles.

### Ciclos y Fechas especiales

`/clearing-houses/:id/cycles` usa cards, formularios reactivos tipados, form-fields, selects, datepicker, tabla ordenable y paginada, chips, menú, diálogos, snackbars, spinner, iconos, tooltips y botones Material. El resumen distingue total, vigentes, futuras, inactivas y próxima ventana. El formulario valida nombre, horas, orden de ventana, cutoff, vigencia, duplicidad y solapamiento; las operaciones conservan creación, nueva versión, historial e inactivación sin eliminación física.

`/clearing-houses/:id/special-dates` usa el mismo lenguaje Material, filtros por año/estado/descripción, tabla y tarjetas móviles. El formulario tipado valida fecha obligatoria, fin de semana, festivo bancario, duplicidad, descripción y longitud máxima sin convertir el día por UTC. Crear, editar, activar y desactivar usan diálogos y snackbars. El contrato corregido es `/api/clearing-house-special-dates`; la ruta anterior omitía `/api` y Docker devolvía el `index.html` de la SPA.

La ruta `/catalogs/clearing-house-special-dates?clearingHouseId=:id` redirige al contexto de la cámara; sin ID redirige a `/clearing-houses`. `/transactions/clearing-house-rules` continúa redirigiendo al listado. No existen dos interfaces administrativas ni nuevas opciones principales en el menú lateral.

### Políticas y trazabilidad normativa

Políticas responde primero a la decisión previa a exportación NACHA-M bajo el título funcional **Reglas de prenotificación**. Las tarjetas vigentes muestran obligatoriedad, plazo, vigencia, estado y consecuencia operativa. ACH Colombia muestra débito obligatorio con 3 días hábiles y crédito opcional. CENIT muestra débito obligatorio con **Sin plazo mínimo documentado** y crédito opcional; no se le atribuyen tres días.

Fuente, referencia y notas se conservaron en entidad, DTO, API, auditoría y formulario, pero se retiraron de las tarjetas principales y de las columnas por defecto del historial. Se consultan mediante **Ver detalle normativo** y se editan dentro de **Trazabilidad normativa**. El preview se presenta como **Comprobar regla para una fecha** sin JSON técnico.

### Navegación, permisos y responsive

`ClearingHouseContextNavigationComponent` concentra regreso, nombre, código, estado y tabs Material para Políticas transaccionales, Ciclos y Fechas especiales. Conserva indicador activo, foco de teclado y desplazamiento interno de tabs en móvil. Las lecturas de políticas aceptan `Config.Read`, `Config.Manage`, `CanReadAch` o `CanManageAch`; sus escrituras, `Config.Manage` o `CanManageAch`. Ciclos conserva `ClearingHouses.View`/`ClearingHouses.ManageCycles`; Fechas especiales, `ClearingHouses.View`/`ClearingHouses.ManageSpecialDates`.

Playwright midió Cámaras, Políticas ACHCOL/CENIT, Ciclos ACHCOL/CENIT y Fechas ACHCOL/CENIT en 1440×900, 1024×768, 768×1024 y 390×844. En las 28 combinaciones `documentElement.scrollWidth` fue exactamente igual a `window.innerWidth`: 1440, 1024, 768 y 390 respectivamente. Las tablas anchas quedan contenidas en desktop/tablet y son sustituidas por tarjetas en móvil; no se aplicó `overflow-x: hidden` global.

### Pruebas, Playwright y Docker

- Specs focalizados: Ciclos/Fechas/Políticas 27/27 y navegación contextual 3/3.
- Build Angular final: exitoso en 136,5 s.
- Suite Angular completa: 593/593, 0 fallidas y 0 omitidas en dos ejecuciones completas consecutivas; la final tomó 85,7 s.
- Build .NET Release: exitoso. El build de API dentro de Docker confirmó 0 warnings y 0 errores.
- Suite backend: 2003 aprobadas y 5 omisiones históricas; dos pruebas `ClearingHouseMultiDbTests` no se habilitaron porque exigen crear y eliminar bases temporales, incompatible con la prohibición de eliminar bases de esta sesión.
- Playwright autenticado: 1/1 escenario integral, 50,4 s, sobre PostgreSQL aislado en 1743/1843; administrador y lector real con `CanReadAch`/`ClearingHouses.View` fueron efímeros. No hubo `pageerror`, `console.error`, HTTP inesperados, loops, spinner permanente, mojibake ni overflow.
- Evidencia visual y mediciones: `artifacts/clearing-house-workspace-final/`.
- Docker principal: API y SPA reconstruidas y recreadas; live, ready y SPA respondieron 200. Se sirvieron `main.59bc00ca6d6db8f5.js`, `798.f87f86caf4dbc311.js`, `737.809d65d916ccb423.js` y `242.f8cdfeb0f166e4d3.js`. SQL Server conservó el mismo contenedor y el volumen `achinterbank_ach_sqlserver_data`.

### Riesgos residuales comprobados

- Las dos pruebas multi-base señaladas requieren un job que autorice crear y eliminar bases temporales; no es posible ejecutarlas bajo la restricción de esta sesión.
- La autenticación funcional completa se validó en el runtime aislado. El Docker principal usa las mismas fuentes e imágenes finales y fue validado por health, rutas, bundles y logs, sin modificar credenciales persistentes.

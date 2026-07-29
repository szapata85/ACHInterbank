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

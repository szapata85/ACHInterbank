# Plan de implementación por fases — CFA SOAP UX + Mapping dinámico

## Reglas de ejecución

- una fase a la vez;
- no avanzar con tests focales fallando;
- actualizar `MEMORY_STATE.md` al cierre de cada fase;
- cambios de comportamiento SOAP requieren caracterización previa;
- no eliminar legacy hasta P8;
- no esperar UAT externa para completar fases locales, pero no declarar homologación externa.

---

# P0 — Baseline y caracterización

## Objetivo

Congelar comportamiento actual de los tres servicios y de la SPA antes del refactor.

## Trabajo

1. Registrar commit/base real si existe `.git`.
2. Inventariar callers de:
   - `ProcTransaccionesRequestMapper`;
   - `ProcContrapartidasRequestMapper`;
   - `RegistrarRespuestaAchSoapRequestMapper`;
   - `WscfaachSoapClient`;
   - `WsAxonRespuestaTransaccionesSoapClient`.
3. Capturar catálogos actuales de parámetros y fuentes.
4. Agregar/confirmar tests de XML contractual de los tres requests actuales.
5. Capturar estado de frontend:
   - rutas;
   - componentes;
   - tamaños;
   - Material usage;
   - branding tokens;
   - tests existentes.
6. Crear fixtures controlados para request parity sin CFA externo.
7. No cambiar producción lógica.

## Evidencia requerida

- build/tests baseline o `NOT_RUN_ENVIRONMENT`;
- snapshots/goldens sanitizados de XML de request;
- lista de archivos/callers;
- `MEMORY_STATE.md` actualizado.

## Salida

`NEXT_ALLOWED_PHASE=P1`.

---

# P1 — Versionado de contrato y migraciones aditivas

## Objetivo

Introducir ContractVersion sin cambiar runtime.

## Backend

1. Crear `IntegrationContractVersion`.
2. Agregar ContractVersionId a parameters/mapping sets.
3. Agregar metadata XML y Revision.
4. Crear migraciones PostgreSQL y SQL Server.
5. Crear baseline contract version para cada uno de los tres métodos.
6. Backfill de parámetros/mappings existentes conservando IDs.
7. Extender queries para leer parámetros por versión.
8. Crear history/snapshot si se aprueba según documento de datos.
9. Runtime continúa usando camino legacy.

## Tests

- migración limpia;
- upgrade desde snapshot compatible;
- mappings actuales conservan reglas;
- exactitud de siete parámetros RegistrarRespuesta;
- constraints de versión.

## Salida

Persistencia versionada, runtime sin cambios. `NEXT_ALLOWED_PHASE=P2`.

---

# P2 — Motor genérico de contrato/resolución/serialización

## Objetivo

Construir motor nuevo detrás de interfaces sin activarlo en runtime productivo.

## Trabajo

1. Crear `IntegrationExecutionContractResolver`.
2. Crear `IntegrationRequestResolver`.
3. Crear `IntegrationSourceProviderRegistry`.
4. Implementar providers para fuentes actualmente usadas.
5. Crear `IntegrationTransformationRegistry` con transformaciones actuales.
6. Crear `ContractDrivenSoapBodyWriter`.
7. Crear `ResolvedIntegrationRequest` inmutable.
8. Crear feature flags por servicio.
9. No retirar typed mappers.

## Tests

- required/optional;
- order;
- escaping XML;
- type conversion;
- missing source;
- transformation validation;
- no arbitrary field path;
- registry duplicate detection.

## Salida

Motor nuevo disponible, todavía sin cutover. `NEXT_ALLOWED_PHASE=P3`.

---

# P3 — Paridad y cutover progresivo de los tres servicios

## Objetivo

Hacer que cada servicio pueda construir el mismo request mediante motor genérico.

## Secuencia

### P3A `Proc_Transacciones`

- adaptar contexto actual;
- convertir builders derivados en providers/strategies donde aplique;
- comparar XML legacy vs nuevo;
- activar feature flag solo después de paridad.

### P3B `Proc_Contrapartidas`

- preservar validaciones funcionales;
- producir parameter bag genérico;
- comparar contra `ProcContrapartidasRequestContract`/XML actual;
- activar flag.

### P3C `RegistrarRespuestaTransaccion`

- implementar `DifferentialResponseSourceValueProvider`;
- conservar command validator;
- comparar exactamente los siete parámetros baseline;
- activar flag.

## Tests obligatorios

- XML parity por servicio;
- no cambios de SoapAction/namespace/order;
- response parser unchanged;
- idempotencia/regresión externa existente;
- no retries HTTP introducidos.

## Salida

Los tres servicios pueden usar runtime dirigido por contrato. `NEXT_ALLOWED_PHASE=P4`.

---

# P4 — Administración de versiones de contrato y campo nuevo sin código

## Objetivo

Habilitar el caso principal: agregar un parámetro CFA escalar y mapearlo sin modificar el cliente SOAP.

## Backend

1. Contract query/command services.
2. Clone contract.
3. Alta manual aprobada de parámetro.
4. Import/analyze WSDL seguro.
5. Contract diff.
6. Clasificación `MappingRequired/RequiresDevelopment`.
7. Clone mapping across versions por ParameterPath.
8. Validación de contract+mapping.
9. Activación atómica.
10. Rollback.

## Scenario test obligatorio

Crear un parámetro ficticio controlado, por ejemplo `TIPOCLIENTE_TEST`, solo en test fixture:

1. crear ContractVersion v2;
2. agregar parámetro required scalar;
3. clonar mapping v1;
4. mapping queda incompleto;
5. mapear a fuente existente;
6. validar;
7. generar XML con nuevo elemento;
8. no modificar writer/soap client;
9. rollback a v1.

No dejar el parámetro ficticio en seeds productivos.

## Salida

No-code target parameter flow probado. `NEXT_ALLOWED_PHASE=P5`.

---

# P5 — Foundation SPA y centro de integración CFA

## Objetivo

Reemplazar navegación técnica por arquitectura funcional y preparar componentes pequeños.

## Trabajo frontend

1. Crear lazy standalone routes si es compatible con routing actual.
2. Crear shell `Centro de integración CFA`.
3. Crear cards de los tres servicios desde read model del servidor.
4. Crear feature stores con signals.
5. Separar data-access de presentation.
6. Centralizar textos genéricos en español.
7. No hardcodear nombres técnicos como títulos principales.
8. Migrar dialogs artesanales prioritarios a MatDialog.
9. Conservar rutas legacy con redirects durante transición.

## Branding

- retirar hex directos del módulo;
- usar tokens globales;
- alinear tipografía con tema Material/global.

## Tests

- route tests;
- service cards;
- permissions;
- loading/error/empty;
- branding claro/oscuro.

## Salida

Nueva shell disponible, editor antiguo aún puede coexistir. `NEXT_ALLOWED_PHASE=P6`.

---

# P6 — Workspace guiado de relación de información

## Objetivo

Hacer que un usuario no técnico configure mappings por campo.

## Trabajo

1. `mapping-workspace` basado en read model backend.
2. Lista de datos requeridos CFA.
3. Estados: completo, pendiente, inválido, no aplica.
4. Selector de origen agrupado y buscable.
5. Tratamientos compatibles filtrados por backend metadata.
6. Vista previa por campo.
7. Sección técnica colapsada.
8. Guardado por parámetro con Revision.
9. Concurrency conflict UX.
10. resumen de cobertura.
11. validación general.
12. publish dialog humanizado.

## Nuevo campo UX

El escenario P4 debe aparecer como:

```text
1 dato nuevo pendiente
Tipo de cliente
[Configurar]
```

sin exponer rutas técnicas salvo detalle avanzado.

## Tests

- Material Harnesses;
- Reactive Forms;
- keyboard;
- validation messages;
- concurrency;
- publish disabled/enabled.

## Salida

Mapping usable funcionalmente. `NEXT_ALLOWED_PHASE=P7`.

---

# P7 — Contratos, conexión avanzada, responsive y accesibilidad

## Objetivo

Completar administración y hardening de UX.

## Trabajo

1. UI de versiones de contrato.
2. Importación WSDL con stepper.
3. Diff humanizado.
4. edición de metadata funcional.
5. conexión técnica en sección avanzada.
6. confirmación de Live/enviar.
7. responsive compacto/mediano/amplio.
8. WCAG AA.
9. navegación teclado.
10. foco de dialogs/drawers.
11. `aria-live` de acciones.
12. tests con branding extremos.
13. viewport matrix completa.
14. textos largos y códigos técnicos.

## Salida

Experiencia completa y adaptable. `NEXT_ALLOWED_PHASE=P8`.

---

# P8 — Consolidación, eliminación de legacy y gauntlet

## Objetivo

Cerrar el programa con una sola arquitectura activa.

## Trabajo

1. Confirmar los tres feature flags en motor nuevo.
2. Retirar typed request mappers solo si no tienen callers.
3. Retirar hardcoded contract parameter seeds usados como autoridad.
4. Deprecar/eliminar input parameter mappings legacy de settings si no tienen consumers.
5. Retirar componentes SPA reemplazados.
6. Mantener redirects de rutas si todavía son necesarios.
7. Revisar docs y memory.
8. Ejecutar suite completa.

## Gauntlet

Intentar romper:

- parameter injection no registrado;
- XML element name malicioso;
- WSDL XXE/DTD;
- required missing;
- type mismatch;
- mapping a source de otro method;
- mapping a parameter de otro contract version;
- concurrent update;
- activation con mapping incompleto;
- rollback sin mapping compatible;
- field order incorrecto;
- output parameter enviado como input;
- mobile overflow;
- keyboard trap;
- branding sin contraste;
- technical code expuesto como lenguaje principal.

## Salida

`program_status=COMPLETE`, `NEXT_ALLOWED_PHASE=NONE` solo si gates aplicables están cerrados.

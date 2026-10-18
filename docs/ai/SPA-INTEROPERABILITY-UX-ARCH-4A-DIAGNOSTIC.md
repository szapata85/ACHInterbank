# 4A — Diagnóstico consolidado SPA / Interoperabilidad / UX / Arquitectura

## 1. Propósito y baseline

Este documento consolida exclusivamente los resultados ya cerrados en 4A.1, 4A.2, 4A.3A y 4A.3B y los convierte en un backlog ejecutable para 4B, 4C y 4D. No reabre auditorías ni agrega hallazgos nuevos.

**Baseline de código:** `ACHInterbank-v22-achcol-claims-3E1.zip`  
**Resultado documental:** `docs/ai/SPA-INTEROPERABILITY-UX-ARCH-4A-DIAGNOSTIC.md`  
**Target:** `ACHInterbank-v23-spa-diagnostic-4A.zip`

Los incrementales 4A.1, 4A.2, 4A.3A y 4A.3B fueron diagnósticos sin modificación de código.

---

## 2. Decisiones cerradas de 4A

| Área | Decisión / clasificación cerrada | Consecuencia de implementación |
|---|---|---|
| SPA ↔ Backend | `UAT-INTEROPERABILITY-CONTRACT = REMOVE_STALE_FRONTEND` | No crear endpoints `nacha-security/interoperability/*`; retirar service/spec residual en 4C.2. |
| Alias | `ALIASES_BACKEND_CONTRACT` confirmado como gap funcional | 4B debe comenzar con un diagnóstico corto de domain/persistence/permisos/DTO y, si existe soporte real, implementar backend + seguridad. |
| Foundation visual | `MIXED_VISUAL_FOUNDATION` | 4C.1 debe establecer una foundation única antes de migrar pantallas. |
| Arquitectura frontend | `ARCHITECTURE_MIXED` | 4C debe reducir acoplamiento y duplicación sin reescribir toda la SPA. |
| Lenguaje / UX | `UX_LANGUAGE_MIXED` | 4C.4 debe hacer humanización transversal, sin rehacer lógica de negocio. |

### 2.1 Interoperabilidad NACHA Security

El contrato residual conocido es:

- `GET nacha-security/interoperability/status`
- `POST nacha-security/interoperability/run-harness`
- `GET nacha-security/interoperability/reports/{reportId}`

No existe backend equivalente, página activa ni ruta Angular que consuma ese servicio. La decisión final permanece **REMOVE_STALE_FRONTEND**. El trabajo posterior es eliminación de código stale, no implementación backend.

### 2.2 Alias

La SPA activa consume:

- `GET api/aliases`
- `GET api/aliases/{id}`
- `POST api/aliases`
- `PUT api/aliases/{id}`

Rutas activas:

- `/aliases`
- `/aliases/new`
- `/aliases/:id/edit`

Permisos frontend existentes:

- `CanReadAliases`
- `CanManageAliases`

Este es el **gap funcional confirmado** que debe tratar 4B, condicionado a que el diagnóstico inicial de 4B demuestre domain/persistence y contrato implementable sin inventar un modelo nuevo.

---

## 3. Priorización consolidada

No se identificaron P0 visuales, arquitectónicos ni de UX. Tampoco se introduce un P0 nuevo en esta consolidación.

| Prioridad | Tipo | Hallazgo consolidado | Incremento destino |
|---|---|---|---|
| P1 | `FUNCTIONAL_GAP` | Alias SPA activa consume CRUD sin backend equivalente. | 4B |
| P1 | `VISUAL_DEBT` | Material, `ui-*`, AG Grid y estilos históricos compiten como foundation. | 4C.1 / 4C.3 |
| P1 | `VISUAL_DEBT` | Branding dinámico no gobierna completamente el theme Material ni todos los componentes compartidos. | 4C.1 |
| P1 | `ARCHITECTURAL_DEBT` | Cross-feature imports, dependencia `shared → feature`, servicios de feature en `core`, primitives y helpers duplicados. | 4C.1 / 4C.2 / 4C.3 |
| P1 | `ARCHITECTURAL_DEBT` | Componentes grandes concentran demasiadas responsabilidades en features relevantes. | 4C.2 / 4C.3 |
| P1 | `UX_DEBT` | Spanglish, enums técnicos, identificadores internos, errores backend poco humanizados y formularios técnicos. | 4C.4 |
| P2 | `STALE_CODE` | `InteroperabilityApiService` y spec residual sin consumidor activo. | 4C.2 |
| P2 | `ARCHITECTURAL_DEBT` | `PagedResponse<T>`, helpers de formato/error y patrones de estado/cleanup duplicados. | 4C.1 / 4C.3 |

---

## 4. 4B — Backend + seguridad para Alias

### Objetivo

Cerrar el gap `ALIASES_BACKEND_CONTRACT` sin inventar una capa de dominio que no exista en el repositorio.

### Gate obligatorio al inicio de 4B

Antes de implementar, realizar un diagnóstico corto y dirigido para confirmar:

1. entidad/modelo Alias existente o equivalente funcional;
2. persistence/repository/DbSet/configuración/migraciones existentes o claramente extensibles;
3. permisos backend existentes o correspondencia con `CanReadAliases` / `CanManageAliases`;
4. DTOs que la SPA ya usa para búsqueda, detalle, alta y edición;
5. semántica del CRUD esperado, incluyendo identidad, unicidad, filtros y estados si los hay.

**Regla de decisión:** si domain/persistence no existe o no puede identificarse con claridad, 4B debe detener la implementación y devolver `NEEDS_BUSINESS_OR_DOMAIN_CLARIFICATION`; no debe inventar un agregado Alias nuevo por inferencia.

### Si el gate es positivo

Cambios permitidos:

- implementar los cuatro endpoints ya consumidos por la SPA;
- agregar autorización coherente con permisos existentes;
- mapear DTOs frontend/backend de forma explícita;
- reutilizar patrones existentes de controller/service/persistence;
- agregar pruebas unitarias/integración del contrato Alias;
- documentar únicamente el contrato necesario.

Cambios prohibidos:

- crear endpoints de interoperabilidad NACHA Security;
- rediseñar Alias UI;
- ampliar el dominio Alias más allá de lo exigido por el contrato activo;
- mezclar trabajo visual/arquitectónico de 4C.

### Criterio de terminado de 4B

- los cuatro endpoints Alias existen y responden con verbos/rutas esperados;
- autorización backend está aplicada;
- DTOs son compatibles con la SPA activa;
- tests del contrato pasan;
- no se creó ningún endpoint `nacha-security/interoperability/*`.

---

## 5. 4C.1 — Visual foundation + branding + shared primitives

### Objetivo

Crear una foundation compartida y predecible sobre Angular Material, integrando branding dinámico y reduciendo duplicación transversal antes de migrar features.

### Features / áreas principales

- theme global;
- login;
- shell/layout;
- sidenav/navigation;
- page headers;
- botones;
- dialogs/confirmaciones;
- tablas;
- loaders;
- empty states;
- estados semánticos;
- shared/core transversal.

### Cambios permitidos

- definir Angular Material como foundation efectiva;
- conectar branding dinámico con los tokens reales del theme Material;
- consolidar tokens globales de color/surface/border/focus/estado;
- consolidar primitives duplicados (`page-header`, tablas, dialogs, confirmaciones, loaders, empty states);
- eliminar o acotar reglas globales HTML/CSS que compiten con Material;
- romper la dependencia `shared → feature`;
- mover fuera de `core` servicios que sean exclusivamente de una feature cuando sea necesario para sanear la foundation;
- consolidar `PagedResponse<T>` y helpers verdaderamente transversales si forman parte del contrato shared.

### Cambios prohibidos

- migrar todos los features de negocio;
- reescribir lógica funcional;
- corregir en masa textos UX;
- implementar Alias backend;
- tocar interoperabilidad salvo dependencias estrictas de shared, dejando su retiro funcional para 4C.2.

### Criterio de terminado

- un solo mecanismo de theme/branding gobierna Material y foundation compartida;
- login/shell/navigation usan la misma fuente de tokens;
- primitives shared seleccionados tienen una implementación canónica;
- `shared` no depende de `features/*`;
- estilos globales legacy quedan eliminados o claramente acotados;
- existe base estable para migrar features sin recrear estilos locales.

---

## 6. 4C.2 — NACHA Security + retiro interoperabilidad stale

### Objetivo

Alinear NACHA Security con la foundation de 4C.1, reducir deuda estructural del módulo y retirar código de interoperabilidad sin consumidor.

### Features principales

- `nacha-security`;
- dependencias transversales que el módulo toma hoy desde `ach-cycles` / `transactions`;
- service/spec residual de interoperabilidad.

### Cambios permitidos

- eliminar `InteroperabilityApiService` residual;
- eliminar su spec stale;
- confirmar que no quede ruta, menú, navegación o referencia muerta;
- conservar y migrar las funcionalidades NACHA Security válidas a la foundation 4C.1;
- aplicar branding y shared primitives;
- reducir cross-feature imports mediante contratos compartidos reales;
- dividir componentes grandes solo cuando reduzca responsabilidades claras;
- humanizar estados técnicos principales cuando sea necesario para dejar el módulo coherente, sin sustituir el barrido transversal de 4C.4.

### Cambios prohibidos

- crear backend de interoperability;
- crear páginas nuevas para justificar el service retirado;
- rediseñar el dominio NACHA Security;
- introducir una segunda foundation visual.

### Criterio de terminado

- no existe `nacha-security/interoperability/*` en frontend activo ni tests residuales;
- no quedan ruta/menu muerto relacionados;
- funcionalidades válidas de NACHA Security siguen operativas;
- pantallas del módulo usan foundation 4C.1 y branding;
- acoplamientos feature→feature relevantes del módulo se reducen o justifican explícitamente.

---

## 7. 4C.3 — Migración gradual de features con mixed/legacy UI + arquitectura

### Objetivo

Migrar por lotes el resto de la SPA hacia la foundation de 4C.1, mejorando aislamiento de features y reduciendo deuda estructural sin exigir reescritura total.

### Lote 1 — Operación crítica

1. `transactions`
2. `clearing-houses/cycles`
3. `ach-responses`
4. `nacha-operational`
5. `incoming-nacha-command-center`
6. `returns`

### Lote 2 — Configuración / administración

1. `nacha-config-admin`
2. `integrations`
3. `catalogs`
4. `navigation`
5. `users/admin`
6. `scheduler`
7. `operational-restrictions`

### Cambios permitidos

- migrar estructuras visuales mixed/legacy hacia la foundation compartida;
- aplicar branding/tokens compartidos;
- reducir imports feature→feature;
- mover contratos realmente transversales a shared/core apropiado;
- consolidar helpers compartidos;
- dividir componentes excesivamente grandes cuando haya beneficio verificable;
- unificar el solapamiento entre incoming command center y NACHA operational sin reescritura total.

### Cambios prohibidos

- reescritura masiva de features;
- cambiar lógica de negocio sin necesidad de migración;
- introducir NgRx por obligación;
- rehacer contratos backend;
- hacer el barrido de lenguaje completo antes de 4C.4.

### Criterio de terminado

- features priorizados usan foundation 4C.1;
- no crean nuevas variantes de primitives compartidos;
- cross-feature coupling disminuye en los pares prioritarios;
- componentes divididos mantienen comportamiento;
- navegación/rutas/permisos existentes continúan operativos.

---

## 8. 4C.4 — Humanización y UX transversal

### Objetivo

Hacer que la SPA se comunique en español natural, consistente y orientado a operación, sin modificar reglas de negocio.

### Cambios permitidos

- eliminar spanglish visible;
- humanizar enums y estados;
- humanizar errores y separar detalle técnico para soporte;
- sustituir labels técnicos por términos funcionales;
- consolidar terminología;
- mejorar estados vacíos;
- homogeneizar feedback de procesamiento/éxito/error;
- mejorar confirmaciones;
- orientar formularios técnicos;
- corregir riesgos obvios de accesibilidad y responsive detectados estáticamente.

### Terminología objetivo ya acordada

| Uso actual | Término objetivo |
|---|---|
| Mapping | Homologación |
| Payload JSON | Contenido JSON del lote |
| operationId | Identificador de operación |
| status | Estado |
| CorrelationId | Identificador de correlación |
| Return of Return | Devolución de devolución |
| Prenote | Prenotificación |
| FlowIds | Identificadores de flujo |
| Trace | Rastreo |
| Dashboard | Panel |

### Cambios prohibidos

- refactors arquitectónicos no requeridos por UX;
- migración Material masiva;
- rediseño de dominio;
- traducción de nombres internos de código que no son visibles;
- ocultar códigos de negocio válidos como Rxx/DEV14/SOL02 cuando el usuario los necesita, siempre que tengan contexto funcional.

### Criterio de terminado

- no quedan spanglish P1 conocido en las áreas diagnosticadas;
- estados/enums visibles están humanizados;
- identificadores internos tienen labels funcionales;
- errores principales no exponen mensajes backend crudos como mensaje primario;
- estados vacíos y feedback indican claramente situación y próxima acción cuando aplica;
- formularios técnicos priorizados tienen contexto suficiente para operar sin conocer nombres internos de implementación.

---

## 9. 4D — Regresión + validación visual + documentación + checkpoint

### Objetivo

Validar de forma integrada los cambios de 4B y 4C y dejar un checkpoint verificable. Completar 4D **no equivale por sí solo a declarar release-ready**.

### Validaciones mínimas

- build backend;
- build SPA;
- unit tests;
- E2E/smoke tests disponibles;
- rutas principales y deep links;
- ausencia de errores relevantes en consola;
- revisión de requests 404/500 no esperados;
- permisos de UI y backend en flujos modificados;
- branding dinámico después de refresh y en login/shell;
- visual desktop;
- visual laptop;
- visual tablet;
- visual móvil;
- textos visibles;
- ausencia de spanglish priorizado;
- ausencia de enums técnicos visibles no humanizados;
- contratos SPA/backend, incluido Alias;
- ausencia de endpoints/referencias interoperability stale;
- documentación técnica/operativa necesaria actualizada.

### Salida de 4D

Checkpoint con:

- resultados de builds/tests;
- matriz de rutas/smoke;
- evidencia visual básica por viewport;
- issues abiertos clasificados;
- confirmación de contratos;
- decisión explícita de siguiente paso. La etiqueta `release-ready` requiere evaluación independiente de los issues abiertos y criterios de release.

---

## 10. Qué NO debemos tocar en esta secuencia

1. No crear endpoints backend `nacha-security/interoperability/*`.
2. No crear una nueva página de interoperabilidad para conservar código stale.
3. No inventar domain/persistence Alias si el gate de 4B no encuentra soporte real.
4. No reescribir toda la SPA ni imponer una nueva arquitectura completa.
5. No introducir otra librería visual o un segundo sistema de theme.
6. No cambiar reglas de negocio como parte de migraciones visuales o UX.
7. No hacer una traducción literal indiscriminada de códigos operativos válidos.
8. No declarar release-ready únicamente porque 4D finalice.

---

## 11. Orden de ejecución y dependencias

```text
4A
 |
 +--> 4B Alias backend
 |
 +--> 4C.1 Foundation
       |
       +--> 4C.2 NACHA Security
       |
       +--> 4C.3 Features
                |
                +--> 4C.4 UX
                         |
                         +--> 4D
```

Reglas de secuencia:

- 4B puede ejecutarse en paralelo con 4C.1 una vez cerrado 4A.4.
- 4C.2 y 4C.3 deben consumir la foundation de 4C.1; no deben recrearla.
- 4C.4 debe ejecutarse después de la migración principal de features para evitar corregir dos veces los mismos textos/estados.
- 4D requiere que **4B y 4C.4 estén completos**, porque valida tanto el contrato Alias como la SPA consolidada.

---

## 12. Backlog ejecutable resumido

| ID | Incremento | Tipo | Prioridad | Entregable |
|---|---|---|---|---|
| 4B-01 | Diagnóstico corto Alias domain/persistence/permisos/DTO | `FUNCTIONAL_GAP` | P1 | Gate de implementación documentado |
| 4B-02 | Implementar contrato Alias si gate positivo | `FUNCTIONAL_GAP` | P1 | CRUD backend + seguridad + tests |
| 4C1-01 | Unificar Material theme + branding dinámico | `VISUAL_DEBT` | P1 | Theme/tokens canónicos |
| 4C1-02 | Consolidar shell/login/navigation/shared primitives | `VISUAL_DEBT` | P1 | Foundation compartida |
| 4C1-03 | Corregir fronteras shared/core transversales | `ARCHITECTURAL_DEBT` | P1 | `shared` feature-agnostic + contracts comunes |
| 4C2-01 | Retirar interoperability service/spec residual | `STALE_CODE` | P2 | Cero referencias activas/muertas |
| 4C2-02 | Migrar NACHA Security a foundation y reducir coupling | `VISUAL_DEBT` / `ARCHITECTURAL_DEBT` | P1 | Módulo coherente |
| 4C3-01 | Migrar lote operación crítica | `VISUAL_DEBT` / `ARCHITECTURAL_DEBT` | P1 | Features operativas alineadas |
| 4C3-02 | Migrar lote configuración/administración | `VISUAL_DEBT` / `ARCHITECTURAL_DEBT` | P1/P2 | Features administrativas alineadas |
| 4C4-01 | Barrido de español/terminología/errores/formularios | `UX_DEBT` | P1 | UX humanizada |
| 4C4-02 | Empty states, feedback, confirmaciones, A11Y/responsive obvio | `UX_DEBT` | P1/P2 | Experiencia operativa consistente |
| 4D-01 | Regresión funcional/técnica/visual | Validación | P1 | Evidencia de checkpoint |
| 4D-02 | Documentación y cierre de issues | Validación | P1 | Estado final verificable |

---

## 13. Criterio de cierre de la fase 4A

4A queda cerrado cuando este documento sea la referencia única para ejecutar 4B y 4C sin reabrir los diagnósticos previos. Cualquier hallazgo nuevo durante implementación debe registrarse como evidencia del incremental correspondiente y no reinterpretarse retroactivamente como parte de 4A salvo que invalide una decisión cerrada de forma demostrable.

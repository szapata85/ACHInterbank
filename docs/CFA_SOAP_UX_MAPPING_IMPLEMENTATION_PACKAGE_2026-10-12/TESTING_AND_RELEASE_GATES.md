# Estrategia de pruebas y gates — CFA-SOAP-UX-MAP-001

## 1. Regla

Ninguna fase se cierra solo por compilar. Cada fase requiere evidencia del comportamiento que modifica.

No declarar PASS para comandos no ejecutados.

## 2. Baseline backend

Comandos canónicos cuando el entorno lo permita:

```powershell
python scripts/validate-solution-projects.py
dotnet build ACHInterbank.sln -c Release
dotnet test tests/Cfa.ACHInterbank.Tests/Cfa.ACHInterbank.Tests.csproj -c Release --no-build
```

## 3. Baseline frontend

```powershell
cd web/ach-interbank-ui
npm ci
npm run build
npm test -- --watch=false --browsers=ChromeHeadless
npm run e2e -- --project=chromium --workers=1
```

No reinstalar dependencias automáticamente si una fase/política del repositorio lo prohíbe; registrar `NOT_RUN_ENVIRONMENT`.

## 4. Contract version tests

Cubrir:

- create/clone version;
- duplicate label;
- only one active;
- active immutable;
- retired immutable;
- parameter uniqueness inside version;
- parameters repeated safely across versions;
- mapping set belongs to same version;
- revision concurrency;
- history snapshot/hash.

## 5. WSDL import security tests

Fixtures:

- valid simple WSDL;
- multiple operations;
- missing selected operation;
- invalid XML;
- DTD;
- external entity;
- oversized document;
- unsupported complex type;
- collection/unbounded;
- namespace change;
- required field addition;
- optional field addition;
- type change;
- removed field.

Assertions:

- external entities never resolved;
- parser does not perform network I/O;
- unsupported structures never become Active silently.

## 6. Request resolver tests

Matriz:

- fixed value;
- default;
- source field;
- nullable source;
- missing required;
- optional missing;
- transformation;
- invalid mask;
- type conversion;
- source provider missing;
- source catalog field from another method;
- parameter from another contract;
- disabled rule;
- priority conflict.

## 7. SOAP writer tests

- exact operation name;
- namespace;
- XML escaping;
- parameter order;
- omission optional;
- nillable behavior;
- decimal invariant;
- date formatting input already normalized;
- no output parameters in request;
- no element not present in contract.

## 8. Paridad de los tres servicios

Crear goldens sanitizados o assertions semánticas para:

### `Proc_Transacciones`

Legacy mapper vs contract engine con el contrato baseline.

### `Proc_Contrapartidas`

`ProcContrapartidasRequestContract`/BuildSoapBody vs contract engine.

### `RegistrarRespuestaTransaccion`

Mapper fijo de siete campos vs contract engine.

Gate P3:

```text
Baseline input -> mismo operation/namespace/elementos/orden/valores
```

Whitespace cosmético del XML puede normalizarse; semántica y orden contractual no.

## 9. Scenario de campo nuevo

Test de integración obligatorio:

```text
Active contract v1
→ Draft v2
→ + required scalar parameter
→ clone mapping
→ validation fails pending field
→ map to existing source
→ validation passes
→ activate
→ generated request contains field
→ writer/client source code unchanged by scenario
→ rollback v1
```

Este test es la prueba principal del objetivo “nuevo campo sin desarrollo”.

## 10. Data migration tests

Ambos providers:

- DB vacía → latest migration;
- snapshot anterior → upgrade;
- baseline contracts creados;
- existing parameter IDs preserved;
- rules preserved;
- mapping sets linked;
- registrar seven params preserved;
- no duplicate active contract.

## 11. Regression financiera

Ejecutar suites afectadas existentes, especialmente:

- IntegrationMappingEndToEndTests;
- ProcTransaccionesRequestMapperTests;
- ProcContrapartidasFallbackClosureTests;
- RegistrarRespuestaAchContractsTests;
- WscfaachSoapClientCharacterizationTests;
- WsAxonRespuestaTransaccionesSoapClientCharacterizationTests;
- NachaSoapResilienceAndIdempotencyTests;
- SoapArchitectureDiagnosticTests;
- readiness/settings tests.

No modificar expectativas para acomodar un XML incorrecto.

## 12. Frontend unit/component tests

Usar Angular Material Harnesses donde exista harness.

Cubrir:

- service cards;
- mapping field list;
- source picker;
- transformation editor;
- validation summary;
- technical details expansion;
- contract diff;
- import stepper;
- publish/activate dialogs;
- connection settings;
- concurrency conflict;
- permissions/read-only.

## 13. Playwright

Flujos mínimos:

1. abrir Centro de integración CFA;
2. abrir Créditos recibidos;
3. ver nombres funcionales y technical secondary;
4. filtrar datos pendientes;
5. editar mapping y guardar;
6. validar;
7. previsualizar;
8. cancelar publicación sin mutación;
9. publicar;
10. abrir contrato nuevo;
11. identificar campo agregado;
12. mapearlo;
13. diff;
14. activar;
15. rollback simulado/local;
16. permiso solo lectura;
17. conflicto de concurrencia;
18. conexión avanzada;
19. confirmación de Live;
20. rutas legacy redirect.

## 14. Responsive E2E

Ejecutar los flujos esenciales en:

```text
360x800
390x844
768x1024
1024x768
1366x768
1440x900
1920x1080
```

Assertions:

- `document.documentElement.scrollWidth <= clientWidth` para shell/tarea primaria salvo componentes técnicos explícitamente scrollables;
- acciones visibles;
- dialog dentro del viewport;
- drawer usable;
- no truncar botones críticos.

## 15. Accesibilidad

Recomendado agregar `@axe-core/playwright` como dev dependency durante P7.

Gates:

- cero violaciones críticas/serias en rutas principales;
- keyboard-only smoke;
- focus restoration dialogs;
- logical heading structure;
- labels de form;
- no color-only state;
- contrast tests con branding claro/oscuro;
- zoom 200 % manual o automatizado equivalente documentado.

## 16. Branding

Fixtures visuales mínimas:

- primary oscuro;
- primary claro;
- sidebar oscuro;
- background claro;
- nombres de entidad largos.

Verificar que la feature no contiene hex de marca nuevos mediante script estático.

Guardrail sugerido:

```text
features/integrations/**/*.scss
  reject: #[0-9a-fA-F]{3,8}
  allowlist: none unless documented semantic/local exception
```

## 17. Static architecture guards

Script recomendado `scripts/validate-cfa-soap-ux-architecture.py`:

- no nuevos hardcoded parameter arrays para los tres métodos fuera de test/baseline migration;
- no `new XElement(... "OF...")` por campo en runtime nuevo;
- no fixed seven-parameter assertion en runtime nuevo después de P8;
- IntegrationMappingSet tiene ContractVersionId;
- no clickable `li/div` en templates del feature sin keyboard semantics;
- no términos `Mapping set`, `Source`, `Live`, `DryRun` como textos visibles principales;
- no brand hex en feature SCSS;
- rutas nuevas existen;
- old routes redirect.

## 18. Gates por fase

| Fase | Gate mínimo |
|---|---|
| P0 | baseline + request fixtures |
| P1 | migrations + data preservation |
| P2 | unit tests resolver/writer/providers |
| P3 | XML parity 3 servicios + regression |
| P4 | no-code new-field scenario |
| P5 | Angular build/unit + architecture guards |
| P6 | mapping UX + harness + publish flow |
| P7 | Playwright responsive + accessibility + branding |
| P8 | full backend/frontend regression + gauntlet |

## 19. Estados de cierre

- `DEVELOPMENT_COMPLETE`: implementación y focal tests.
- `VERIFIED`: build/regresión/migraciones completas en entorno adecuado.
- `GAUNTLET_PASSED`: pruebas adversariales del programa.
- `UAT_READY`: artefacto exacto preparado para UAT.
- `USER_ACCEPTED`: solo aceptación explícita del negocio.

El programa documental no autoriza saltar estados.

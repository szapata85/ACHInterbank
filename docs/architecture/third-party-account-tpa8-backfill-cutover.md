# TPA8 — Backfill y cutover canónico

## TPA8A — Backfill seguro legacy -> master canónico

### Baseline y alcance

TPA8A parte exclusivamente de `ACHInterbank-R15-TPA7C-PARITY-CLOSURE.zip`.

Este incremento implementa **sólo backfill** desde `CustomerThirdParty` hacia el master canónico existente:

```text
Customer
  -> ThirdParty
  -> ThirdPartyAccount
```

y reconstruye `ThirdPartyAccountValidation` / `PrenotificationValidationAttempt` únicamente cuando la evidencia histórica enlazada permite determinar el contexto sin heurísticas.

TPA8A **no ejecuta TPA8B ni TPA9**. No cambia autoridad de lectura, no modifica el selector TPA7, no elimina `CustomerThirdParty`, no retira escrituras legacy y no agrega migraciones.

### Entrada operacional

Se agrega el comando administrativo:

```text
POST /api/third-party-accounts/backfill
Authorization policy: CanManageAch
```

Contrato de lote:

- `DryRun` (default `true`);
- `AfterLegacyThirdPartyId` como checkpoint exclusivo para reanudación;
- `BatchSize` entre 1 y 1000.

La respuesta expone como mínimo métricas de master `Created`, `Reused`, `SkippedIncomplete`, `Ambiguous`, `Failed`, más métricas independientes para historial de validación. Los issues contienen únicamente `LegacyThirdPartyId`, categoría y código técnico; no incluyen documentos ni números de cuenta.

### Orden determinista y reanudación

El proceso usa paginación por clave sobre `CustomerThirdParty.Id`:

```text
WHERE Id > AfterLegacyThirdPartyId
ORDER BY Id
TAKE BatchSize + 1
```

La respuesta entrega `LastProcessedLegacyThirdPartyId` y `HasMore`. Repetir el mismo lote no crea duplicados: el master reutiliza las claves naturales y el servicio de onboarding TPA1/TPA2; el historial usa la transacción prenotificación enlazada y una idempotency key determinista.

Para una interrupción después de crear el contexto de validación pero antes del intento, el contexto se persiste primero en `NotStarted`. El resultado histórico y el intento se guardan juntos en la siguiente escritura. Una ejecución posterior puede continuar desde ese estado neutral. Si ya existe el intento y falta únicamente `CurrentAttemptId`, TPA8A repara ese puntero sólo cuando no existe conflicto.

### Reglas de backfill del master

TPA8A no deriva identidad ni tipo de cuenta desde texto libre.

1. Normaliza documento/cuenta con las mismas funciones del modelo canónico (`ThirdParty.NormalizeDocumentNumber`, `ThirdPartyAccount.NormalizeAccountNumber`).
2. Reutiliza una cuenta canónica existente cuando existe una única coincidencia compatible con Customer, institución, cuenta normalizada y documento del tercero.
3. Si la cuenta no existe, `AccountType` sólo se obtiene de una `AchTransaction` enlazada que:
   - sea prenotificación;
   - coincida con institución destino, cuenta normalizada y documento receptor del legacy;
   - tenga exactamente una semántica prenotificación conocida en `NachaTransactionCodeTaxonomy`.
4. La identidad `ThirdParty` se obtiene de una única identidad canónica ya existente o de un único `Customer` receptor persistido con `DocumentType`, `PersonType` y nombre disponible.
5. La creación/reutilización final pasa por `IThirdPartyOnboardingService` con `ThirdPartyAccountOriginSource.Backfill`, preservando normalización, clave natural y arbitraje de conflictos únicos de TPA1/TPA2.

Si falta cualquiera de esas pruebas, el registro se clasifica como `SKIPPED_INCOMPLETE`; si existen varias identidades/cuentas o evidencia contradictoria, se clasifica como `AMBIGUOUS`. No se elige una opción por orden, no se usa un AccountType por defecto y no se fabrica un nombre.

### Dry-run

`DryRun=true` ejecuta las mismas resoluciones y clasificaciones, incluyendo verificación de la evidencia de historial, pero no llama a onboarding persistente ni escribe validaciones/intentos.

`Created` en dry-run significa **would create**. `Reused`, `SkippedIncomplete` y `Ambiguous` conservan la misma semántica que una ejecución real.

### Reconstrucción conservadora de historial

El backfill de master es independiente del backfill de historial. Una cuenta puede quedar creada/reutilizada aunque el historial no sea reconstruible.

TPA8A sólo crea/reutiliza historial cuando dispone de:

- `PrenotificationTransactionId` persistido y transacción enlazada;
- coincidencia exacta de la identidad destino entre legacy y transacción;
- código de transacción prenotificación con una única semántica conocida para `AccountType` y naturaleza;
- `AchCycle` persistido con `ClearingHouseId`;
- `EffectiveEntryDate`, timestamp de creación y ciclo fuente;
- `CompanyEntryDescriptionCatalog.StandardEntryClassCode` como `ServiceType`;
- estado legacy y estado de la transacción en una combinación histórica coherente;
- timestamp de resolución cuando el resultado es final.

Combinaciones aceptadas:

| `CustomerThirdParty.Status` | `AchTransaction.State` | Validación reconstruida |
| --- | --- | --- |
| `Pending` | `Pending` | `PendingResponse` / intento `AwaitingResponse` |
| `Active` | `Certified` | `Accepted` |
| `Active` | `AppliedTacitly` | `TacitlyAccepted` |
| `Rejected` | `ReturnedByOperator` o `ReturnedByEpr` | `Rejected` |

Cualquier otra combinación se reporta como ambigua y no crea historia.

### Política histórica: no invención

La evidencia disponible no demuestra el día exacto de transición histórica ACH Colombia V32/V35. Por eso una validación histórica creada por TPA8A conserva:

- `PolicyRuleId = null`;
- `PolicyEffectiveFrom = null`;
- `ResponseDeadlineAtUtc = null`;
- `ExpiresAtUtc = null`;
- `LastMonetaryUseAtUtc = null`.

Esto preserva la evidencia operacional de prenotificación sin atribuirle una versión de política no demostrada. La elegibilidad actual no debe interpretar esa historia como autorización bajo una policy actual distinta; la revalidación contextual existente sigue siendo la autoridad cuando corresponda.

`GAP-TPA-016` permanece abierto.

### Privacidad y observabilidad

Los logs estructurados del proceso contienen sólo IDs internos, contadores, categorías y estado de ejecución. No registran número de cuenta, documento completo, nombres de receptor ni payload de evidencia legacy.

`EvidenceReference` y `CorrelationReference` del historial reconstruido usan únicamente IDs internos de `CustomerThirdParty`/`AchTransaction`.

### Compatibilidad y no-cutover

TPA8A es aditivo:

- `CustomerThirdParty` permanece operativo y no es modificado por el backfill;
- no se cambia la autoridad de lectura;
- no se cambia el selector transaccional TPA7;
- no se elimina legacy;
- no se modifica SINGLE/BULK onboarding;
- no se introduce una nueva autoridad de policy;
- no se agrega migración porque el esquema canónico necesario ya existe.

El eventual cutover pertenece a **TPA8B** y queda explícitamente fuera de este incremento.

### GAPs

- `GAP-TPA-015`: cerrado por TPA8A a nivel de capacidad de backfill seguro: las filas con evidencia suficiente convergen idempotentemente al master/historial canónico y las que no la tienen quedan reportadas sin invención. La verificación runtime de los tests focales queda diferida por ambiente.
- `GAP-TPA-013`: permanece abierto. TPA8A puede reutilizar una identidad persistida con tipo documental, pero no agrega tipo documental al ingress transaccional genérico.
- `GAP-TPA-016`: permanece abierto. No existe evidencia para fijar una fecha histórica exacta V32/V35, y TPA8A deliberadamente no enlaza una `PolicyRuleId` histórica.

### Tests focales TPA8A

Se agregan pruebas para:

- dry-run sin escrituras;
- idempotencia de ejecuciones repetidas;
- reuse de master existente aun cuando legacy no permita deducir `AccountType`;
- datos incompletos sin heurísticas;
- identidad ambigua;
- no duplicación para legacy que normaliza a la misma identidad;
- no invención de historial cuando falta `ServiceType`;
- reanudación desde un contexto de validación neutral persistido;
- reanudación por checkpoint `AfterLegacyThirdPartyId` y replay seguro del lote.

`python scripts/validate-solution-projects.py` y el guard estático focal TPA8A se ejecutan sobre el artefacto. El SDK/runtime .NET y Docker no están disponibles en el entorno suministrado, por lo que `dotnet test`/`dotnet build` y pruebas runtime de proveedor se registran como `NOT_RUN_ENVIRONMENT`, sin bloquear este incremento conforme al alcance solicitado.

## TPA8B — Cutover controlado de lecturas legacy

### Baseline y alcance

TPA8B parte exclusivamente de `ACHInterbank-R16-TPA8A-SAFE-BACKFILL.zip` y no repite el backfill.

El único read legacy marcado para cutover por el estado previo es el adaptador productivo `CustomerThirdPartyAppService` que atiende `GET /api/customer-third-parties`. El selector transaccional TPA7 ya consume el selector canónico/contextual y **no se modifica**. Los endpoints canónicos `GET /api/third-party-accounts` y la administración TPA5/TPA6 continúan sin cambios.

TPA8B no ejecuta TPA8C ni TPA9, no elimina `CustomerThirdParty`, no cambia onboarding SINGLE/BULK, no introduce migraciones y no modifica reglas normativas.

### Autoridad canónica del read de compatibilidad

`GET /api/customer-third-parties` conserva su contrato de compatibilidad, pero la respuesta productiva se construye primero desde:

```text
Customer
  -> ThirdParty
  -> ThirdPartyAccount
       -> ThirdPartyAccountValidation
            -> PrenotificationValidationAttempt
```

Las búsquedas por cliente originador resuelven `SourceAccountNumber` a **un único `CustomerId`**. Si no existe cliente o la cuenta fuente resuelve ambiguamente a más de un Customer, la lectura devuelve vacío y no elige una identidad por orden. Esto evita el comportamiento legacy que podía expandir por identidad documental compartida.

Los filtros de institución destino, cuenta destino, documento receptor y texto se aplican sobre el master canónico. El identificador del renglón canónico es `ThirdPartyAccount.Id` (el DTO de compatibilidad amplía `Id` a `long`, sin cambio de representación JSON).

### Proyección de estado sin `CustomerThirdParty.status`

El campo de compatibilidad `Status` ya no toma su valor de `CustomerThirdParty.Status`. Se proyecta exclusivamente desde validación canónica cuando el contexto puede resolverse sin inferencias:

1. Si una prenotificación legacy que coincide por Customer + institución + cuenta normalizada + documento tiene un `AchTransactionId` enlazado a exactamente una validación canónica, esa validación es la autoridad del renglón.
2. Si no existe ese enlace y la cuenta canónica tiene exactamente una validación de prenotificación, se usa ese único contexto.
3. Si existen múltiples contextos y no hay un enlace inequívoco, no se elige cámara, naturaleza ni servicio: el renglón se proyecta como `Pending` de compatibilidad y se omiten datos contextuales.
4. `Accepted` y `TacitlyAccepted` se proyectan a `Active`; `Rejected` se proyecta a `Rejected`; cualquier otro estado o ausencia de validación se proyecta a `Pending`. Esta traducción existe sólo para mantener el contrato histórico del endpoint y **no constituye autorización de transferencia**. La elegibilidad transaccional sigue perteneciendo al selector/eligibility contextual TPA7.

Por tanto, `CustomerThirdParty.status=Active` no puede hacer aparecer un renglón en `status=Active` cuando la validación canónica no lo respalda.

### Compatibilidad/rollback legacy

`CustomerThirdParty` y su código de lectura se conservan. Un registro legacy que no tiene un master canónico verificable permanece visible como fallback para diagnóstico/rollback, con sus datos persistidos y trazas disponibles, pero:

- se proyecta conservadoramente como `Pending`;
- nunca satisface filtros `Active` o `Rejected`;
- `StatusSource` lo identifica como `Compatibilidad legacy sin autoridad funcional`;
- no se usa para elegibilidad, selección transaccional ni onboarding.

De esta forma un backfill incompleto no oculta evidencia operacional, pero tampoco convierte un estado legacy global en autoridad funcional.

### Shadow después del cutover

El shadow TPA4 se mantiene porque todavía aporta evidencia de divergencia y rollback. Después de construir la respuesta canónica, el adaptador reconstruye de forma separada el slice que habría leído legacy y ejecuta `IThirdPartyAccountShadowComparisonService.CompareLegacyReadAsync`.

La comparación continúa siendo observacional:

- `MATCH`, `LEGACY_ONLY`, `CANONICAL_ONLY`, `STATUS_MISMATCH`, `ELIGIBILITY_MISMATCH`, `IDENTITY_INCOMPLETE` y `POLICY_CONTEXT_MISSING` se mantienen;
- una excepción o cancelación del shadow no cambia la respuesta canónica ya construida;
- los logs siguen sin documento, cuenta completa ni nombres.

No se elimina el shadow en TPA8B. Su retiro o modificación posterior requiere evidencia separada.

### Cambios deliberadamente excluidos

- no se repite `POST /api/third-party-accounts/backfill`;
- no se cambia `TransactionThirdPartyAccountSelector` ni el consumidor SPA TPA7;
- no se reactiva `CustomerThirdParty.Status` como autoridad;
- no se cambia onboarding silencioso;
- no se elimina legacy;
- no se agrega migración;
- no se inventa `AccountType`, tipo documental, cámara, naturaleza, servicio o historia faltante.

### Tests focales TPA8B

Se agregan/ajustan pruebas para demostrar:

- lectura productiva desde validación canónica;
- `CustomerThirdParty.status=Active` incapaz de sobreescribir un `Rejected` canónico;
- aislamiento por el `CustomerId` resuelto desde la cuenta fuente;
- fallback legacy visible pero conservador y no autoritativo;
- múltiples contextos canónicos sin inferir uno;
- preservación de filtros relevantes de institución/cuenta/documento;
- fallo del shadow sin alterar la autoridad canónica.

`python scripts/validate-solution-projects.py` y un guard estático focal TPA8B se ejecutan sobre el artefacto. El SDK/runtime .NET y Docker no están disponibles en el entorno suministrado; `dotnet test`/`dotnet build` y validación runtime de proveedor se registran como `NOT_RUN_ENVIRONMENT` y no bloquean este incremento.

### GAPs

TPA8B no cierra un GAP durable adicional. `GAP-TPA-013` y `GAP-TPA-016` permanecen abiertos sin reinterpretación. Los gaps legacy de retiro/concurrencia/protección de datos permanecen para fases posteriores; este incremento sólo cambia la autoridad del read previsto.

## TPA8C — Cierre de coherencia TPA8

### Baseline y alcance

TPA8C parte exclusivamente de `ACHInterbank-R17-TPA8B-CANONICAL-READ-CUTOVER.zip` y cierra TPA8 mediante verificación focal de coherencia entre el backfill TPA8A, el read cutover TPA8B, la autoridad contextual ya cerrada en TPA7 y la compatibilidad/rollback legacy.

No se agrega funcionalidad productiva nueva y no se ejecuta TPA9. La revisión se limita a `ThirdPartyAccountBackfillService`, `CustomerThirdPartyAppService`, el read model/shadow relacionado y los tests directos TPA8A/TPA8B. No se modifica selector TPA7, onboarding, migraciones ni código legacy.

### Resultado de la verificación

| Invariante de cierre | Evidencia TPA8C | Resultado |
| --- | --- | --- |
| Backfill idempotente/reanudable | Paginación por `AfterLegacyThirdPartyId`, natural keys/onboarding canónico, idempotency key de intento y recuperación desde `NotStarted`; tests de replay/checkpoint ya existentes | OK |
| Backfill sin invención | `AccountType`, identidad, cámara, naturaleza, servicio e historia sólo se reconstruyen con evidencia explícita; faltantes -> `SKIPPED_INCOMPLETE`, conflictos -> `AMBIGUOUS` | OK |
| Read productivo canónico | `CustomerThirdPartyAppService.GetAsync` construye primero desde `ThirdPartyAccount` + validación contextual; el status legacy no alimenta la proyección productiva | OK |
| `CustomerThirdParty.status=Active` no autoriza | La proyección `Active/Rejected/Pending` sale de `ThirdPartyAccountValidation`; legacy-only siempre queda `Pending` y no satisface filtros `Active`/`Rejected` | OK |
| Legacy no reconciliado conservador | No se infiere contexto faltante; múltiples validaciones sin enlace exacto y filas legacy-only quedan `Pending` | OK |
| Shadow observacional | Corre después de construir la respuesta canónica y sus excepciones/cancelaciones no modifican el resultado productivo | OK |
| Rollback disponible | `CustomerThirdParty`, su lectura legacy interna y shadow siguen presentes; no se elimina tabla, entidad ni código | OK |
| TPA7/onboarding sin cambios | TPA8C no modifica selector/eligibility TPA7 ni onboarding; TPA8A/TPA8B ya declaran ese límite y este cierre no introduce cambios productivos | OK |
| Sin migraciones | TPA8C no agrega ni modifica migraciones | OK |
| Gaps históricos no inventados | `GAP-TPA-013` y `GAP-TPA-016` permanecen abiertos | OK |

### Coherencia backfill -> cutover

Se agregan dos pruebas de cierre en `ThirdPartyAccountBackfillServiceTests` sin tocar código productivo:

1. `Tpa8Closure_BackfilledCanonicalHistoryFeedsCutoverCompatibilityRead` ejecuta el backfill real sobre evidencia coherente `Active + Certified`, verifica que la historia canónica reconstruida sea `Accepted` sin `PolicyRuleId`/`PolicyEffectiveFrom` inventados y luego comprueba que el read de compatibilidad consume esa autoridad canónica.
2. `Tpa8Closure_IncompleteBackfillRemainsConservativeLegacyFallback` demuestra el camino complementario: si el backfill no puede crear master por evidencia incompleta, la fila legacy permanece disponible pero se proyecta `Pending` y no pasa un filtro `Active`.

Estas pruebas conectan explícitamente TPA8A con TPA8B sin convertir el endpoint de compatibilidad en autoridad de elegibilidad transaccional. La autorización monetaria continúa perteneciendo al selector/gate contextual TPA7.

### Shadow y rollback al cierre

TPA8C conserva el shadow porque aún aporta señal de divergencia y soporte de rollback. No se retira por conveniencia. La comparación sigue siendo puramente observacional y PII-safe.

El legacy permanece disponible para rollback/diagnóstico. Esto no reactiva su `Status` como autoridad funcional: una fila sin reconciliación canónica continúa siendo sólo evidencia de compatibilidad.

### Tests y entorno

La suite focal de TPA8 queda compuesta por los tests TPA8A/TPA8B existentes más las dos pruebas cross-phase anteriores. Se ejecutan guardas estáticas focales sobre los archivos permitidos.

El SDK/runtime .NET y Docker no están instalados en el entorno suministrado; por ello `dotnet test`, `dotnet build` y la verificación runtime/proveedor se registran como `NOT_RUN_ENVIRONMENT` y no bloquean TPA8C.

### Estado final

- `TPA8_STATUS=CLOSED`.
- `NEXT_ALLOWED_PHASE=TPA9`.
- `GAP-TPA-013` permanece abierto.
- `GAP-TPA-016` permanece abierto.
- TPA9 no fue ejecutado.

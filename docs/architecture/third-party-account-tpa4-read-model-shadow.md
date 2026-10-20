# TPA4 — Read model canónico y shadow comparison contra legacy

Fecha: 2026-10-11  
Programa: `TPA-GOV-001`  
Alcance ejecutado: **únicamente TPA4**. TPA5 no se ejecuta en este incremento.

## 1. Objetivo y límites

TPA4 agrega una lectura canónica eficiente sobre el maestro genérico ya existente (`Customer -> ThirdParty -> ThirdPartyAccount`) y la historia contextual de validación de TPA3. También agrega comparación *shadow* desde el GET legacy para medir divergencias sin cambiar la autoridad productiva actual.

Se conservan los invariantes del programa:

- `ThirdParty` y `ThirdPartyAccount` siguen siendo agnósticos de cámara;
- cámara, naturaleza y servicio sólo intervienen en validación/elegibilidad;
- no existe `ThirdPartyAccountEnrollment`;
- onboarding silencioso SINGLE/BULK y `CustomerThirdParty` continúan operativos;
- no hay read cutover ni selector SPA canónico;
- no hay endpoints administrativos de escritura, UI administrativa ni aprobación/rechazo manual de prenotificaciones;
- TPA4 no modifica el path monetario ni ejecuta backfill histórico.

## 2. Read model canónico

### 2.1 Contratos y servicio

Se agregan:

- `IThirdPartyAccountReadService` en Application;
- `ThirdPartyAccountReadService` en Persistence;
- DTOs/query de lectura en `ThirdPartyAccountReadModels.cs`;
- `ThirdPartyAccountsController` con endpoints **sólo GET**:
  - `GET /api/third-party-accounts`;
  - `GET /api/third-party-accounts/{thirdPartyAccountId}`.

El endpoint conserva `[Authorize]` y la política `CanReadAch`. No expone comandos de master data ni comandos de validación.

### 2.2 Información proyectada

La proyección devuelve, de forma paginada y filtrable:

- cliente y tercero;
- cuenta canónica e institución financiera;
- `AccountType`;
- estado administrativo de tercero y cuenta;
- todas las validaciones contextuales de la cuenta por cámara/naturaleza/servicio/tipo;
- último intento de cada validación;
- resolución y causal técnica segura (`ReturnCode`);
- trazabilidad técnica relevante (`ValidationId`, `AttemptId`, `AchTransactionId`, `SourceCycleId`);
- elegibilidad contextual cuando el consumidor entrega explícitamente el contexto requerido.

El número de cuenta y el documento se entregan enmascarados (`****` + últimos cuatro caracteres alfanuméricos). La proyección no publica `ReturnDetail`, `EvidenceReference`, `CorrelationReference` ni `SourceFileReference` de los intentos, porque esos campos pueden contener detalle sensible no necesario para la consulta administrativa base.

### 2.3 Elegibilidad contextual

La lectura no inventa un contexto de política. Para calcular elegibilidad se exige, como conjunto:

- `eligibilityClearingHouseId`;
- `eligibilityTransactionNature`;
- `eligibilityEffectiveDate`;
- `eligibilityServiceType` es opcional y conserva las reglas de normalización de TPA3.

Si sólo llega una parte del contexto, el controller responde `400`; si no llega contexto, la cuenta se devuelve sin decisión de elegibilidad. La evaluación delega en `IThirdPartyAccountEligibilityService`, de modo que TPA4 no crea una segunda autoridad de reglas.

### 2.4 Estrategia de consulta y persistencia

No se agrega tabla de read model. La proyección es **query-time y reconstruible por definición**:

1. aplica filtros y cuenta sobre `ThirdPartyAccounts`;
2. pagina primero por IDs canónicos, con límite de `PageSize=100`;
3. carga el grafo necesario sólo para los IDs de la página;
4. ordena/mapea las validaciones e intentos en memoria;
5. evalúa elegibilidad sólo si fue solicitada.

Esta estrategia evita materializar el universo completo y evita introducir persistencia derivada prematuramente. Por tanto TPA4 agrega **cero migraciones**.

## 3. Shadow mode

### 3.1 Punto de integración

`CustomerThirdPartyAppService.GetAsync` sigue construyendo y devolviendo exactamente el `PagedResponse<CustomerThirdPartyListDto>` legacy. Después de completar esa lectura, invoca opcionalmente `IThirdPartyAccountShadowComparisonService` con los IDs legacy de la página y el alcance de la consulta.

El resultado *shadow* no reemplaza, filtra ni reordena la respuesta productiva. Cualquier excepción de comparación se registra sin datos de cliente/cuenta y la respuesta legacy permanece intacta.

### 3.2 Asociación legacy -> canonical

La comparación privilegia evidencia fuerte:

1. si la prenotificación legacy tiene un `PrenotificationValidationAttempt` canónico ligado por `AchTransactionId`, usa esa relación;
2. de lo contrario intenta identidad funcional por `CustomerId + FinancialInstitutionId + AccountNumberNormalized + ThirdParty.DocumentNumberNormalized`;
3. `AccountType` **no se inventa** porque `CustomerThirdParty` legacy no lo contiene. Si la ausencia de esa dimensión produce más de un candidato canónico, el resultado es `IDENTITY_INCOMPLETE`.

Para recuperar el `ServiceType` del contexto histórico no se usa el `ServiceClassCode` NACHA 200/220/225 como si fuera PPD/CCD/CTX. El fallback lee `CompanyEntryDescriptionCatalog.StandardEntryClassCode` mediante `CompanyEntryDescriptionId`, consistente con la fuente usada por TPA3 al registrar la validación.

La detección `CANONICAL_ONLY` se limita deliberadamente a consultas cuyo alcance legacy completo cabe en la primera página, no tienen filtro de status y tienen `SourceAccountNumber` resoluble. Además el universo canónico comparable se limita a 200 cuentas. Esto evita etiquetar como `CANONICAL_ONLY` registros que simplemente quedaron fuera de la página legacy.

## 4. Modelo de discrepancias

`ThirdPartyAccountShadowClassifier` usa estas clases:

- `MATCH`;
- `LEGACY_ONLY`;
- `CANONICAL_ONLY`;
- `STATUS_MISMATCH`;
- `ELIGIBILITY_MISMATCH`;
- `IDENTITY_INCOMPLETE`;
- `POLICY_CONTEXT_MISSING`.

Las equivalencias de estado son intencionalmente estrictas:

- legacy `Pending` sólo coincide con `PendingSend`/`PendingResponse`;
- legacy `Active` sólo coincide con validación `Accepted`/`TacitlyAccepted` y elegibilidad contextual `Allowed`;
- legacy `Rejected` sólo coincide con validación `Rejected` y elegibilidad contextual `Rejected`.

No se traduce el `Active` global legacy a estado administrativo `ThirdPartyAccount.Active`, ni se considera equivalente a `AllowedWithoutPrenotification`, porque esas afirmaciones requieren contexto que el modelo legacy no representa. Cuando no existe contexto confiable se reporta `POLICY_CONTEXT_MISSING`, no una equivalencia inferida.

## 5. Observabilidad segura

La comparación registra sólo agregados:

- `System.Diagnostics.Metrics` counter `achinterbank.third_party_account.shadow_comparison`;
- tag único `discrepancy=<CLASE>`;
- log estructurado de conteos por clase.

Los logs y métricas no incluyen `CustomerId`, `ThirdPartyId`, IDs de cuenta, nombres, documentos, números de cuenta ni valores de identidad. Los IDs presentes en objetos internos de resultado de comparación no son serializados por el GET legacy ni incluidos en la telemetría.

## 6. Autoridad legacy y SPA

TPA4 **no cambia la autoridad de lectura productiva**:

- `CustomerThirdPartiesController` y `CustomerThirdPartyAppService` continúan sirviendo el contrato legacy;
- el selector actual de transactions SPA que usa `status=Active` no se modifica;
- no se hace read cutover;
- no se elimina `CustomerThirdParty`;
- no se cambian archivos funcionales SPA.

La lectura canónica es aditiva y queda disponible para administración/selección futura, pero todavía no sustituye consumidores productivos legacy.

## 7. Gaps observados, no cerrados

TPA4 observa y preserva abiertos:

- `GAP-TPA-004`: el selector SPA aún depende de `Active` global;
- `GAP-TPA-013`: el tipo documental explícito de ingress sigue incompleto;
- `GAP-TPA-014`: `LastMonetaryUseAtUtc` todavía no es actualizado por el path monetario;
- `GAP-TPA-015`: históricos pueden carecer de identidad canónica suficiente; shadow los clasifica sin fabricar identidad;
- `GAP-TPA-016`: no se inventa una fecha histórica exacta V32/V35.

TPA4 no cierra ninguno de estos gaps y no introduce backfill para ocultarlos.

## 8. Pruebas de TPA4

Se agrega `ThirdPartyAccountReadShadowTests` para cubrir:

- lectura canónica cliente/tercero/cuenta/institución/tipo/estados con enmascaramiento;
- validaciones múltiples ACH Colombia/CENIT y último intento/causal/resultado;
- elegibilidad contextual delegada al servicio TPA3;
- clasificación `MATCH`, `LEGACY_ONLY`, `CANONICAL_ONLY`, `STATUS_MISMATCH`, `ELIGIBILITY_MISMATCH`, `IDENTITY_INCOMPLETE` y `POLICY_CONTEXT_MISSING`;
- comparación real `MATCH` mediante vínculo de intento;
- no exposición de cuenta/documento completos ni nombres en observabilidad shadow;
- preservación de la respuesta legacy aunque falle el shadow compare.

También permanece la caracterización previa `TransactionsApiService.getActiveThirdParties_ShouldKeepLegacyGlobalActiveSelector`, que demuestra que el selector SPA legacy sigue basado en `Active` y no fue cortado a lectura canónica en TPA4.

### Evidencia ejecutable disponible en este host

- `python scripts/validate-solution-projects.py`: `PASS`.
- SDK `.NET`: `NOT_RUN_ENVIRONMENT` (`dotnet` no está disponible).
- Docker/dual DB: `NOT_RUN_ENVIRONMENT` (`docker` no está disponible).

De acuerdo con la regla de la fase, esas limitaciones de runtime no bloquean el cierre coherente de TPA4; sí permanecen como riesgo de verificación para la siguiente ejecución en un host con toolchain.

## 9. Resultado de fase

TPA4 queda implementado como read model canónico aditivo + shadow compare no autoritativo. No hay migraciones, no hay cambios funcionales SPA, no hay read cutover y no se ejecutó TPA5.

`NEXT_ALLOWED_PHASE=TPA5`.

# Plan de implementación — TPA-GOV-001

**Baseline:** ACHInterbank R10 NACHA-CONFIG-UX final  
**Estado:** `PLANNED`  
**Primera fase permitida:** `TPA0`  
**Disciplina:** una fase por chat; no avanzar automáticamente.

## 1. Objetivo

Implementar administración de clientes, terceros, cuentas y validaciones para ACH Colombia V35 y CENIT con una separación estricta:

- **onboarding/master data genérico e independiente de cámara**;
- **validación/elegibilidad contextual por cámara y naturaleza**;
- onboarding silencioso single/bulk preservado o mejorado;
- resultados de prenotificación automáticos;
- migración incremental sin big bang.

## 2. Invariantes no negociables

- `INV-TPA-001`: silent onboarding single/bulk se mantiene.
- `INV-TPA-002`: onboarding de tercero/cuenta no recibe ni persiste contexto de cámara.
- `INV-TPA-003`: single, bulk y manual convergen al mismo servicio de master data.
- `INV-TPA-004`: ningún usuario fija manualmente el resultado interbancario.
- `INV-TPA-005`: ACH Colombia/CENIT se aíslan por política efectiva en validación/elegibilidad.
- `INV-TPA-006`: débito obligatorio falla cerrado; crédito opcional no se vuelve obligatorio.
- `INV-TPA-007`: historial de intentos append-only.
- `INV-TPA-008`: onboarding sin I/O externo.
- `INV-TPA-009`: PostgreSQL + SQL Server siguen first-class.
- `INV-TPA-010`: PII enmascarada por defecto.
- `INV-TPA-011`: `CustomerThirdParty` no se retira antes del cutover verificado.
- `INV-TPA-012`: elegibilidad es derivada, no un `Active` global en la cuenta.

## 3. Fases priorizadas

### TPA0 — Baseline y characterization

**P0 · Sin cambio funcional · Sin migración**

1. inventariar readers/writers/callers de legacy y policy services;
2. caracterizar onboarding single/bulk actual;
3. identificar si existe acoplamiento actual a cámara dentro del onboarding y registrarlo como gap, sin corregirlo;
4. caracterizar prenotificación/resultados automáticos/idempotencia;
5. caracterizar ACH Colombia credit/debit y CENIT credit/debit por separado;
6. confirmar debt `+3 business days`, `SlaDeadlineAtUtc`, selector `Active`, metadata V32;
7. confirmar fuentes de `AccountType` y normalización para futuro backfill;
8. producir `docs/architecture/third-party-account-tpa0-baseline.md`;
9. actualizar state/MEMORY sólo con evidencia durable.

**No crear esquema, endpoints ni UI.**

### TPA1 — Master data genérico aditivo

**P0 · Migración PostgreSQL + SQL Server**

Crear exclusivamente el maestro canónico:

- `ThirdParty`;
- `ThirdPartyAccount`.

Guardrails:

- `ThirdPartyAccount` no tiene `ClearingHouseId`, `TransactionNature`, servicio, prenotificación, ciclo ni SLA;
- claves únicas de maestro no incluyen cámara;
- `AccountType` pertenece a la identidad de la cuenta canónica;
- modelo legacy permanece autoridad visible;
- `ThirdPartyAccountValidation` y `PrenotificationValidationAttempt` se mantienen fuera de TPA1 y se introducen con el lifecycle de validación contextual en TPA3.

Agregar domain tests, configuraciones EF y migraciones aditivas para ambos providers. Onboarding v2/dual-write empieza en TPA2.

### TPA2 — Silent onboarding v2 genérico / dual-write master

**P0**

- introducir `IThirdPartyOnboardingService`;
- usarlo desde single y bulk;
- onboarding sólo crea/enriquece `ThirdParty` y `ThirdPartyAccount`;
- no resuelve políticas ni crea resultados de validación;
- compatibility adapter mantiene legacy cuando corresponda;
- concurrencia por unique conflict + re-read;
- cero nuevas llamadas externas.

### TPA3 — Elegibilidad y lifecycle de validación por cámara

**P0**

- introducir `IThirdPartyAccountEligibilityService`;
- integrar `ClearingHouseTransactionRule` como autoridad efectiva;
- crear/vincular `ThirdPartyAccountValidation` e intentos cuando corresponda;
- reemplazar gradualmente `+3` global por cálculo contextual probado;
- separar response deadline de otros SLA si TPA0 confirma mezcla;
- respuesta/tacit acceptance actualiza validación/attempt idempotentemente;
- re-prenotificación crea intento nuevo;
- expiración sólo con fuente aprobada.

### TPA4 — Read model canónico + shadow compare

**P1**

- queries CQS de tercero/cuenta/validaciones;
- endpoint de lectura canónico;
- adapter del GET legacy;
- shadow compare sin PII;
- índices/paginación/performance;
- opcional `ThirdPartyAccountOperationalState` sólo como proyección derivada.

### TPA5 — API administrativa de master data + comandos de validación

**P1**

Separar claramente:

- CRUD tercero/cuenta: sin cámara;
- request/retry prenotification: con contexto de cámara/naturaleza/servicio;
- deactivate/reactivate master;
- cambios materiales invalidan contexto de validación correspondiente;
- permisos finos/auditoría;
- ningún endpoint de approve/reject interbancario.

### TPA6 — UX cliente-céntrica + consola operacional

**P1**

- Cliente > Terceros > Cuentas como maestro genérico;
- “Validaciones operativas” en sección separada;
- consola global para Operaciones;
- alta manual no solicita cámara como dato del maestro;
- una acción separada puede iniciar validación contextual;
- masking/accessibility/branding.

### TPA7 — Integración con creación de transacciones

**P1**

- selector usa `EligibilityDecision` contextual;
- misma cuenta canónica reutilizable entre cámaras;
- retirar dependencia de `status=Active` global;
- crédito opcional sigue operando cuando política lo permite;
- débito bloquea sólo conforme a policy;
- regression single/bulk/NACHA.

### TPA8 — Backfill + cutover

**P1**

- backfill idempotente de tercero/cuenta;
- reconstruir contextos de validación sólo donde exista evidencia suficiente; no inventar cámara/naturaleza ambiguas;
- divergencias a cero o allowlist documentada;
- canónico pasa a autoridad de lectura/selector;
- rollback documentado.

### TPA9 — Hardening

**P1**

- concurrencia/carga;
- auth/IDOR/PII;
- observabilidad;
- jobs cluster-safe;
- failure/chaos local donde exista harness;
- performance de read models;
- resiliencia sin retry ciego.

### TPA10 — Certificación y decisión legacy

**P2**

- build/test backend + dual DB + Angular + E2E focal;
- trazabilidad normativa final;
- decidir `KEEP_COMPATIBILITY` o `RETIRE_LEGACY_WRITES`;
- no borrar físicamente legacy sin plan separado;
- actualizar status/index/memory.

## 4. Dependencias que TPA0 debe confirmar

- fuente real de `AccountType` para legacy/backfill;
- normalización actual de documentos/cuentas;
- comportamiento de source account sin `Customer`;
- semántica `Certified` vs `AppliedTacitly` por cámara;
- cálculo/ownership de `SlaDeadlineAtUtc`;
- consumidores externos de `/api/customer-third-parties`;
- si alguna ruta actual de onboarding depende de clearing house y si ese acoplamiento debe aislarse en TPA2/TPA3;
- ownership actual del soporte de autorización débito.

## 5. Gates

Cada fase requiere lo aplicable: focal tests, build, dual-provider persistence tests, Angular tests, runtime evidence, docs/state/memory, y declaración exacta de `BEHAVIORAL_DIFF`/`NORMATIVE_DIFF`.

Si el host no permite ejecutar una evidencia obligatoria: `IMPLEMENTED_UNVERIFIED_ENVIRONMENT`; nunca inventar PASS.

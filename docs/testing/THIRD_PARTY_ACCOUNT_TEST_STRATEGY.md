# Estrategia de pruebas y gates — TPA-GOV-001

## 1. Filosofía

Characterization primero. Master data genérico y validación contextual se prueban como responsabilidades diferentes. ACH Colombia y CENIT se prueban por separado sólo en policy/validation/eligibility, no en identidad del maestro.

## 2. TPA0 — characterization obligatoria

### Onboarding actual single/bulk

- comportamiento cuando tercero legacy existe/no existe;
- `Active/Rejected` legacy no se resetea accidentalmente;
- source account sin Customer conserva comportamiento actual;
- single/bulk crean/enriquecen `Customer`/`CustomerAccount` según baseline;
- verificar si alguna ruta actual de onboarding recibe/usa cámara; registrar acoplamiento como gap, sin corregirlo.

### Resultado automático

- aceptación/certificación -> estado legacy correspondiente;
- devolución -> `Rejected` legacy;
- evento duplicado idempotente;
- respuesta de otra prenotificación no muta registro incorrecto;
- confirmar ausencia de write endpoint manual de resultado.

### Policy characterization

- ACH Colombia credit/debit;
- CENIT credit/debit;
- deuda `+3 business days` global;
- `SlaDeadlineAtUtc`/fallback;
- metadata V32;
- selector SPA `status=Active`.

### Gap de modelo a registrar

`CustomerThirdParty` carece de `AccountType`, entidad separada `ThirdParty`, entidad separada `ThirdPartyAccount` e historial multi-intento; además su `Pending/Active/Rejected` mezcla maestro y validación. **No registrar “falta cámara en la cuenta” como gap**, porque el diseño objetivo deliberadamente mantiene la cuenta agnóstica de cámara.

## 3. TPA1 — dominio/persistencia

### Master data

- claves únicas sin cámara;
- dos cámaras reutilizan el mismo `ThirdPartyAccount`;
- normalización documento/cuenta;
- estados administrativos;
- PostgreSQL + SQL Server migrations/constraints.

### Validation context

- unique por cuenta+contexto validable conforme al modelo final;
- append-only attempts;
- no `Validated` sin intento/evidencia;
- cambios materiales invalidan contextos afectados, no borran historial.

## 4. TPA2 — contrato de onboarding genérico

- single/bulk/manual llaman el mismo servicio;
- onboarding ejecuta sin `ClearingHouseId`;
- no crea `ThirdPartyAccountValidation` por sí solo;
- dos operaciones por cámaras distintas convergen al mismo maestro;
- concurrencia unique conflict -> re-read;
- cero network calls;
- compatibilidad legacy preservada.

## 5. TPA3 — elegibilidad/validación

- `EligibilityDecision` por cámara/naturaleza/servicio/fecha;
- crédito opcional puede ser `AllowedWithoutPrenotification`;
- débito obligatorio produce `RequiresPrenotification` hasta cumplir policy;
- response/tacit acceptance idempotentes;
- re-prenotification crea nuevo attempt;
- deadlines boundaries/timezone/calendar;
- R10/R17/R35 y causales relevantes según autoridad real.

## 6. API/Angular

- master endpoints no piden cámara;
- validation request sí recibe contexto;
- no endpoint/botón approve/reject;
- IDOR/ownership;
- masking;
- selector contextual;
- alta manual guarda cuenta genérica;
- accesibilidad.

## 7. E2E controlado

1. crear cuenta manual sin cámara;
2. usarla en ACH Colombia credit sin prenote cuando policy permite;
3. reutilizar la misma cuenta en CENIT debit;
4. recibir `RequiresPrenotification`;
5. prenotificar, simular rechazo, corregir, re-prenotificar;
6. resolver automáticamente;
7. confirmar elegibilidad sin duplicar master.

## 8. Gates

```cmd
python scripts/validate-solution-projects.py
dotnet build ACHInterbank.sln -c Release
dotnet test tests/Cfa.ACHInterbank.Tests/Cfa.ACHInterbank.Tests.csproj -c Release --no-build
```

Para SPA:

```cmd
cd web/ach-interbank-ui
npm ci
npm run build
npm test -- --watch=false --browsers=ChromeHeadless
```

Dual DB cuando hay persistencia. Si el host no puede ejecutar, `IMPLEMENTED_UNVERIFIED_ENVIRONMENT`, nunca PASS inventado.

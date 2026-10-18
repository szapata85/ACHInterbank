# Contrato de onboarding silencioso genérico — TPA-GOV-001

## 1. Propósito

Convertir el onboarding silencioso actual en un contrato explícito, reusable y **agnóstico de cámara**. Este contrato aplica a creación single, bulk y alta manual.

## 2. Invariantes

### INV-TPA-ONB-001 — No pre-registro obligatorio
Una operación hoy válida no empieza a exigir alta manual previa de tercero/cuenta por introducir el nuevo módulo.

### INV-TPA-ONB-002 — Onboarding independiente de cámara
El onboarding crea/encuentra `ThirdParty` y `ThirdPartyAccount` sin conocer ACH Colombia, CENIT, naturaleza crédito/débito, PPD/CCD/CTX, ciclos, deadlines o causales.

### INV-TPA-ONB-003 — Single/Bulk/manual equivalentes
Los tres caminos consumen el mismo caso de uso canónico de onboarding.

### INV-TPA-ONB-004 — Idempotencia
Repetir la misma identidad funcional no crea duplicados.

Clave de tercero:

`CustomerId + DocumentType(normalizado) + DocumentNumber(normalizado)`

Clave de cuenta:

`ThirdPartyId + FinancialInstitutionId + AccountType + AccountNumber(normalizado)`

La cámara nunca forma parte de estas claves.

### INV-TPA-ONB-005 — Enriquecimiento no destructivo
Datos posteriores pueden completar campos faltantes, pero no deben reemplazar silenciosamente una identidad materialmente distinta. Conflictos reales se elevan como dominio/validación, no se “corrigen” por heurística.

### INV-TPA-ONB-006 — Sin I/O externo
No hay llamadas a ACH Colombia, CENIT, SFTP, MFT, SOAP ni otros servicios externos dentro del onboarding.

### INV-TPA-ONB-007 — Unidad de trabajo
El maestro participa en la unidad de trabajo de la creación de transacción según el patrón actual. No se agregan commits parciales ocultos.

### INV-TPA-ONB-008 — Concurrencia segura
Un conflicto de unique key por alta simultánea se resuelve con re-read idempotente del maestro; no se repite ciegamente la operación monetaria o el envío externo.

### INV-TPA-ONB-009 — No crea autorización
Crear una cuenta no significa que esté habilitada para cualquier cámara/naturaleza. La elegibilidad se evalúa después.

### INV-TPA-ONB-010 — No falsifica validación
El onboarding nunca crea un `Validated`, `Approved`, `Accepted` o equivalente por el simple hecho de descubrir la cuenta.

## 3. Contrato recomendado

```csharp
public interface IThirdPartyOnboardingService
{
    Task<ThirdPartyAccountRef> EnsureAccountAsync(
        ThirdPartyOnboardingRequest request,
        CancellationToken cancellationToken);
}
```

Entrada permitida:

- `CustomerId`;
- identidad del tercero;
- nombre/razón social si está disponible;
- entidad receptora;
- tipo de cuenta;
- número de cuenta;
- origen técnico del descubrimiento para auditoría.

Entrada prohibida en este puerto:

- `ClearingHouseId`;
- `TransactionNature`;
- `ServiceClass`/PPD/CCD/CTX;
- ciclos/horarios;
- `PrenotificationRequired`;
- Rxx/Dxx;
- deadline o aceptación tácita.

## 4. Separación con elegibilidad/validación

Después del onboarding, el caso de uso transaccional ejecuta:

```text
EnsureAccount(master only)
        -> EvaluateEligibility(transaction context)
        -> optionally Request/EnsureValidation
        -> continue/reject transaction according to policy
```

El mismo `ThirdPartyAccountId` puede ser usado por ACH Colombia hoy y CENIT mañana sin duplicar el maestro.

## 5. Casos esperados

| Caso | Resultado de onboarding | Resultado posterior |
|---|---|---|
| Tercero/cuenta inexistente, transacción single | Crear maestro genérico | Elegibilidad se resuelve por contexto de la transacción. |
| Mismo tercero/cuenta en bulk | Reusar mismo maestro | Cada item conserva su contexto operacional. |
| Mismo tercero/cuenta llega por otra cámara | No crear duplicado | Se evalúa política de la nueva cámara. |
| Prenotificación nueva | Asegurar maestro | Crear/vincular contexto e intento de validación fuera del onboarding. |
| Crédito sin prenotificación opcional | Asegurar maestro | `AllowedWithoutPrenotification` si política efectiva lo permite. |
| Débito con prenotificación obligatoria | Asegurar maestro | `RequiresPrenotification`/pending hasta cumplir política. |
| Datos insuficientes para crear maestro | No inventar datos | Preservar comportamiento actual de la transacción y registrar gap/resultado según contrato existente. |

## 6. Pruebas contractuales obligatorias

- single vs bulk producen el mismo `ThirdPartyAccount`;
- dos cámaras distintas reutilizan la misma cuenta canónica;
- el onboarding puede ejecutarse sin resolver una cámara;
- no crea registros de validación por sí solo;
- unique conflict concurrente converge a un solo registro;
- datos incompletos conservan compatibilidad actual;
- no hay llamadas externas;
- ningún log contiene cuenta/documento completo.

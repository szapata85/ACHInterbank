# Seguridad y privacidad — TPA-GOV-001

## 1. Datos sensibles

Identificación, nombre, entidad y número de cuenta son PII/datos financieros. Aplicar mínimo privilegio y redacción por defecto.

## 2. Reglas

- nunca loguear cuenta/documento completos;
- no incluir PII cruda en idempotency keys visibles;
- listas devuelven datos enmascarados salvo permiso específico;
- auditoría registra quién cambió master o solicitó validación;
- historial de validación es append-only funcionalmente;
- alta/edición/desactivación requieren policy backend;
- no hay endpoint manual para resultado de prenotificación;
- prevenir IDOR customer->thirdParty->account;
- fixtures sintéticos.

## 3. Separación de privilegios

- leer master;
- gestionar master;
- solicitar/reintentar validación;
- ver datos sensibles;
- exportar, si aplica.

Maker/checker interno de master data, si se requiere, es independiente del resultado ACH/CENIT.

## 4. Amenazas

- enumeración de cuentas;
- exposición en exportaciones;
- re-prenotificación abusiva;
- cambio de master mientras hay attempt pendiente;
- correlación errónea de respuesta;
- race conditions de onboarding;
- usar `Active` legacy como autorización universal;
- contaminar el master con `ClearingHouseId` y provocar duplicados o bypass de validación.

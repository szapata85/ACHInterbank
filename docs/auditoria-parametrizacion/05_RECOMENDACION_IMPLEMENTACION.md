# Recomendacion Implementacion

Fecha: 2026-05-19

## Recomendacion Adoptada

Implementar una entidad de dominio parametrizable por camara y naturaleza: `ClearingHouseTransactionRule`.

## Razonamiento

- Evita hard-code normativo en `NachaFileBuilder`.
- Mantiene Clean Architecture: Domain, Application, Persistence, API y SPA.
- Permite vigencia y fuente normativa auditable.
- Permite que ACH Colombia y CENIT diverjan sin copiar reglas.
- Permite preview antes de ejecutar export NACHA-M.

## Decisiones

| Decision | Resultado |
|---|---|
| Reglas iniciales por seed | Si, basadas en MAN-004 V32 y CENIT DSP-152 Anexo 2. |
| Endpoint CRUD protegido | Si. |
| Pantalla Angular administrativa | Si. |
| Bypass de prenotificacion | No. |
| Backdating | No. |
| Transmision externa | No. |

## Criterio de Uso

Cada regla debe tener fuente normativa, referencia, vigencia y estado activo. Si no hay regla vigente, export NACHA-M debe fallar de forma controlada y no generar archivo vacio.

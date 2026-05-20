# Brechas Arquitectura

Fecha: 2026-05-19  
Estado: brechas posteriores a implementacion controlada.

| ID | Brecha | Impacto | Estado | Accion |
|---|---|---|---|---|
| ARQ-PREN-001 | DEF-UAT-020 requiere archivo NACHA-M no vacio por camara. | Bloquea cierre normativo NACHA-M. | Abierta | Crear prenotificaciones UAT validas y reintentar export por ACH Colombia/CENIT. |
| ARQ-PREN-002 | Reglas requieren gobierno de cambios y aprobacion normativa. | Riesgo de parametrizacion erronea. | Abierta | Definir workflow de aprobacion/acta para cambios normativos. |
| ARQ-PREN-003 | Validacion receptor/identificacion queda parametrizada, pero no sustituye pruebas con camara. | Riesgo de interpretacion normativa. | Abierta | Homologacion o waiver formal por camara. |
| ARQ-PREN-004 | CENIT/CUD E2E sigue pendiente. | Bloquea productivo. | Abierta | Ejecutar UAT CENIT/CUD con evidencia formal. |

## Decision

La arquitectura permite continuar UAT controlado. Productivo permanece **NO-GO**.

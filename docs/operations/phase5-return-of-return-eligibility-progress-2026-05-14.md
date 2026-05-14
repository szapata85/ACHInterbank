# Fase 5 — avance mínimo de elegibilidad de devolución de devolución

Fecha: 2026-05-14

## Alcance implementado
- Se centralizó la evaluación de elegibilidad de devolución de devolución en `IAchReturnOfReturnEligibilityService`.
- Se agregaron modelos internos de request/resultado/fallas para desacoplar la validación de la orquestación.
- La validación regulatoria delega en `IAchRegulatoryCatalogService.ValidateReturnOfReturnAsync(...)` usando `AchReturnOfReturnPolicy`.
- No se generó archivo NACHA ni se modificaron estados finales.

## Fuera de alcance en este commit
- Generación de archivo de devolución de devolución.
- Cambios de naming/formato NACHA.
- Contabilidad, migraciones, endpoints públicos, catálogos y seeder.

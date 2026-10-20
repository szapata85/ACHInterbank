# ADR-TPA-004 — Migración por Branch by Abstraction

**Estado:** Accepted for implementation planning

## Decisión

`CustomerThirdParty` no se elimina al introducir el modelo canónico. Se añade el nuevo modelo y un adaptador/dual-write temporal. La lectura canónica se activa sólo después de shadow compare y backfill.

## Secuencia

1. characterization del legacy;
2. esquema aditivo;
3. backfill idempotente;
4. dual-write silencioso/manual;
5. respuestas actualizan ambos mientras sea necesario;
6. shadow read;
7. cutover de API/UI/selector;
8. detener write legacy;
9. retiro físico sólo con decisión posterior y evidencia multi-DB.

## Consecuencia

Las fases iniciales aceptan duplicación técnica controlada a cambio de reducir riesgo de regresión.

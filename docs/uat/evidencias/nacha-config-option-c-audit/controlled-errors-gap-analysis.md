# Fase 6A - Analisis de errores controlados

| Error requerido | Estado observado | Comentario |
|---|---|---|
| `NACHA_PROFILE_NOT_PUBLISHED` | No evidenciado | El resolver devuelve warning/fallback cuando no hay perfil publicado |
| `NACHA_PROFILE_NOT_EFFECTIVE` | No evidenciado | Vigencia se filtra, pero falta error especifico |
| `NACHA_PROFILE_AMBIGUOUS` | Parcial | Hay warning de ambiguedad; puede lanzar generico si `FailOnResolverAmbiguity` |
| `NACHA_REQUIRED_RECORD_MISSING` | No evidenciado | Missing layout se maneja como warning/fallback |
| `NACHA_REQUIRED_FIELD_MISSING` | No evidenciado | Renderer puede dejar blanco |
| `NACHA_FIELD_LENGTH_INVALID` | Parcial | Validador tiene `INVALID_FIELD_LENGTH`; renderer trunca |
| `NACHA_FIELD_VALIDATION_FAILED` | Parcial | Validaciones existen antes de publish, no runtime oficial completo |
| `NACHA_FIELD_SOURCE_NOT_FOUND` | No evidenciado | Missing property no falla de forma oficial |
| `NACHA_CALCULATION_FAILED` | No evidenciado | Calculos no tienen codigo especifico |
| `NACHA_LEGACY_GENERATION_DISABLED` | No evidenciado | No hay corte oficial contra fallback legacy |

Opcion C requiere errores funcionales predecibles y consumibles por API/SPA. Hoy predominan warnings, fallback y excepciones genericas.


# Fase 4.2 - correccion bloqueante de CI

## Veredicto local

FASE 4.2 CERRADA - CI LOCAL VERDE.

El cierre remoto permanece pendiente de publicar el commit y ejecutar un nuevo workflow. No se realizo push.

## Linea base

- Rama: `ACH-Interbank-Postgresql`.
- HEAD inicial: `64f05fef108c6e76881f04a77e0f95dc3cb16629`.
- Commit LIVE contenido: `64f05fef108c6e76881f04a77e0f95dc3cb16629`.
- Cambio preexistente preservado y no inspeccionado: `docs/uat/certificados_pruebas/`.

## Fallos originales reproducidos

| Prueba | Esperado | Resultado anterior | Reproducido |
| --- | --- | --- | --- |
| `ProcContrapartidasReadiness_ShouldNotBeOk_WhenMonetaryOrDirectionFieldsUsePlaceholders` | `False` | `True` | Si |
| `Readiness_ShouldNotUseFallback_WhenProcContrapartidasIsBootstrapPublished` | `Failed` | `ReadyWithWarnings` | Si |
| `Resolver_UsesBootstrapPublishedBaseMapping_ByDefault` | `TX-REF-001` | `0` | Si |

La reproduccion inicial termino con 42 aprobadas y 3 fallidas.

## Resultado

- Mapping canonico unico con fuentes persistidas para referencia, monto y direccion.
- Readiness bloquea placeholders y mappings publicados invalidos con `Failed`.
- Resolver no sustituye fuentes obligatorias por cadena vacia o cero.
- Bootstrap repara solo mappings identificados establemente como `seed`, preserva IDs y no altera mappings publicados por usuarios.
- SQL Server y PostgreSQL validaron base nueva, tres ejecuciones idempotentes, resolver y readiness.
- LIVE local focalizado: un intento exitoso por motor, respuesta persistida y sin duplicados.
- Comando exacto de CI: 2.113 aprobadas, 0 fallidas y 7 omitidas en 21m08s.

## Archivos de evidencia

- `CAUSA_RAIZ_BOOTSTRAP_PROC_CONTRAPARTIDAS.md`
- `EVIDENCIA_READINESS_Y_RESOLVER.md`
- `EVIDENCIA_SQLSERVER_POSTGRESQL.md`
- `EVIDENCIA_LIVE_POST_CORRECCION.md`
- `RESULTADO_COMANDO_CI_LOCAL.md`

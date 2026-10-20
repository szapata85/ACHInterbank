# Flujo de trabajo sin Git — ZIP full por fase

## Regla

No ejecutar `git status`, `git diff`, `git commit`, branches ni merges para este programa.

## Entrada de una fase

1. Tomar el ZIP full de la fase anterior.
2. Calcular SHA-256.
3. Extraer en un directorio limpio.
4. Leer `START_HERE.md` y `MEMORY_STATE.md`.
5. Verificar que `next_allowed_phase` coincide con la fase solicitada.

## Salida de una fase

El ZIP debe contener **todo el proyecto**, no sólo archivos modificados, e incluir siempre:

- `AGENTS.md`
- `MEMORY.md`
- `docs/`
- `src/`
- `tests/`
- `web/`
- scripts/configuración existentes
- `docs/ai/navigation-menu/MEMORY_STATE.md` actualizado

## Nombres sugeridos

```text
ACHInterbank-R34-NAV-IA-BASELINE-DOCS-FULL-PROJECT.zip
ACHInterbank-R34-NAV-IA-NAV1-FULL-PROJECT.zip
ACHInterbank-R34-NAV-IA-NAV2-FULL-PROJECT.zip
...
ACHInterbank-R34-NAV-IA-NAV6-FULL-PROJECT.zip
```

Si el proyecto recibe una nueva revisión global durante el proceso, conservar `NAV-IA-NAVx` y actualizar el prefijo base en `MEMORY_STATE.md`.

## Handoff mínimo

Antes de comprimir:

```yaml
active_phase_status: CLOSED | IMPLEMENTED_UNVERIFIED_ENVIRONMENT
next_allowed_phase: NAVx
last_output_package: <nombre zip>
output_sha256: <hash>
files_touched:
  - ...
tests_executed:
  - ...
tests_deferred:
  - ...
```

## Rollback

Rollback = volver al ZIP full de la fase anterior. No ejecutar scripts destructivos de reversión de datos salvo que la fase los haya documentado explícitamente.

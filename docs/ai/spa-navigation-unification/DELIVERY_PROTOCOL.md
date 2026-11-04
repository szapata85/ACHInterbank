# Protocolo obligatorio de entrega incremental — ACH Interbank

**Programa:** `NAV-UX-MFT-20261027`  
**Vigencia del acuerdo de trabajo:** 2026-10-27  
**Incremento documental:** `I04A`  
**Modo:** ChatGPT en modo chat, sin exigir Git ni trabajo asíncrono.

## Regla de entrega

**Al finalizar CADA implementación incremental, entregar en la misma respuesta y sin esperar a otro incremento:**

1. **ZIP íntegro y autocontenido del proyecto**, construido a partir del último ZIP completo válido. Debe incluir las fuentes originales y modificadas, tests, scripts, configuración, lockfiles, migraciones y documentación. No entregar únicamente diffs o parches.
2. **`docs/ai/spa-navigation-unification/MEMORY_STATE.md` actualizado dentro del ZIP** y copia Markdown externa con el mismo contenido, identificando fecha, incremento, cambios, archivos, pruebas efectivamente ejecutadas, resultados, bloqueos y próximo paso.
3. **Informe de avance en Markdown**, dentro del ZIP y como archivo externo. Informar qué se implementó, qué se comprobó realmente, qué no pudo probarse, pendientes, riesgos y estado general. No inventar porcentajes de avance funcional ni marcar pruebas como PASS si no corrieron.
4. **Documentación de arquitectura/ejecución/pruebas y manifiesto actualizados** para ese incremento cuando haya cambios relevantes, siempre incluidos en el ZIP.
5. **SHA-256 externo y validación de integridad del ZIP**. La suma del ZIP final va fuera del propio ZIP para evitar una referencia circular. Verificar que ningún archivo fuente previo desaparezca por accidente.
6. **Resumen visible en chat** con links al ZIP, memoria, avance, otra documentación cambiada y checksum, más el próximo incremento previsto.

## Comportamiento cuando una prueba no se puede ejecutar

- Implementar los cambios autorizados sin detenerse por un impedimento ambiental.
- Continuar con verificaciones realizables (revisión estructural, análisis estático o sintaxis).
- Registrar cada gate como `PASS`, `FAIL`, `NOT_RUN_ENVIRONMENT` o `PENDING_DEPLOYMENT` con evidencia; no equivale a un pase de producción.
- Sólo cambiar a `CLOSED` cuando se cumplan los criterios definidos para el incremento y las pruebas obligatorias de runtime.

## Identidad y rastreabilidad de la entrega

Formato recomendado de nombre: `ACHInterbank_<programa>_<incremento>_PROYECTO_COMPLETO.zip`. Cada entrega lleva un nombre distinto y siempre incluye la totalidad del proyecto fuente, no un ZIP parcial.

Cada actualización de memoria debe registrar: `parent_artifact`, `parent_sha256`, `output_artifact`, `output_sha256` (en archivo externo), `increment_id`, `status`, `changed_files`, `tests_executed`, `tests_pending`, `next_action` y una nota de continuidad para un nuevo chat.

## Preservaciones obligatorias

- Respetar código y documentación de incrementos anteriores, incluyendo `AGENTS.md`, `MEMORY.md` y memorias históricas.
- No alterar permisos IAM, autorización MFT, reglas de negocio, contratos API o integraciones bancarias fuera del alcance aprobado.
- `NAV3.5C` histórico continúa en estado `BLOCKED_NO_EXECUTE`.
- No añadir secretos o credenciales productivas a documentos o artefactos.
- Los artefactos regenerables (`bin/`, `obj/`, `node_modules/`, `.git/`) pueden omitirse si se documenta la exclusión; no omitir archivos fuente ni lockfiles.

## Lista de verificación por cierre

- [ ] Código o documentación del incremento aplicados en el proyecto completo
- [ ] Pruebas posibles ejecutadas y evidencia registrada; imposibles señaladas
- [ ] `MEMORY_STATE.md` actualizado dentro y fuera del ZIP
- [ ] `PROGRESS_REPORT_*.md` actualizado dentro y fuera del ZIP
- [ ] Plan, arquitectura y matriz de pruebas consistentes con el estado real
- [ ] ZIP completo íntegro y cadena de SHA-256 anterior/actual verificable
- [ ] Respuesta en chat con entregables, resultado y siguiente incremento

## Mejora obligatoria de I05 — continuidad antirretroceso

Antes de cada trabajo, comprobar el SHA-256 del ZIP padre frente al `parent_sha256` y el `output_artifact` del estado de memoria más reciente disponible. Ejecutar `scripts/verify-incremental-source-lineage.py` antes del empaquetado; el resultado debe tener `parent_hash_matches=true`, `deleted=[]` y `pass=true`.

Este control prueba **continuidad del artefacto presentado**, no que se conozcan todos los posibles incrementos desarrollados en otras conversaciones. Si falta el último ZIP declarado, comunicarlo y no iniciar una nueva rama desde uno más antiguo como si fuera el último.

Cada informe de avance distinguirá expresamente incremento **funcional**, **documental** o de **pruebas**, y diferenciará evidencias de fuente vs. runtime. Ver `BASELINE_LINEAGE_I05.md` y `PROGRESS_REPORT_I05_2026-10-27.md`.

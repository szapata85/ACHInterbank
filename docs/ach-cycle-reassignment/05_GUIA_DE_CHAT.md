# ACH-CR-001 — Continuación incremental con ChatGPT en modo chat

## Primer paso de cada nueva conversación

1. Adjuntar el **último ZIP completo** (`ACHInterbank_ACH_CR_Ixx_COMPLETO.zip`) como única fuente de código para continuar; el ZIP inicial histórico es solo baseline.
2. Abrir `docs/ach-cycle-reassignment/MEMORY_STATE.md` dentro del ZIP y leer `AGENTS.md` y `MEMORY.md` del repositorio.
3. Aplicar **solo** el incremento `NEXT_INCREMENT` de la memoria. No inferir que el proyecto ya implementó fases que figuran PENDING.
4. Devolver nuevo ZIP completo, memoria actualizada, changelog y evidencia estática; no solo trozos de código.

## Prompt histórico para I01 (ya implementado estáticamente; no reutilizar para el siguiente paso)

> Continúa ACH-CR-001 con el ZIP completo adjunto como source of truth. Lee primero `AGENTS.md`, `docs/ach-cycle-reassignment/00_START_HERE.md`, `01_ARQUITECTURA.md`, `02_PLAN_INCREMENTAL.md` y `MEMORY_STATE.md`. Ejecuta únicamente I01: elegibilidad de transacciones salientes no presentadas y selección de ciclo posterior elegible en la misma cámara, reutilizando políticas/calendario existentes. No implementes aún reasignación persistente, job Quartz, migraciones ni cambios UX. Preserva Clean Architecture, SOLID, compatibilidad ACH Colombia/CENIT, perfiles NACHA y comportamiento de otros flujos. No hagas llamadas reales, bloqueos ni ejecuciones que el entorno no soporte. Efectúa verificaciones estáticas, informa tests dinámicos NOT_RUN_ENVIRONMENT si corresponde. Actualiza arquitectura, casos, CHANGELOG y MEMORY_STATE con evidencias y NEXT_INCREMENT. Entrégame el ZIP completo I01 y el resumen de cambios, riesgos y checks.

## Prompt para el próximo paso I03 (y estándar para posteriores)

> Continúa ACH-CR-001 desde `ACHInterbank_ACH_CR_I02_COMPLETO.zip` adjunto. Lee `MEMORY_STATE.md` y aplica únicamente I03 (adaptador Quartz acotado de pendientes), **sin activar el despliegue automático por defecto mientras RISK-CR-001/exportador siga abierto**. Implementa **solo** `NEXT_INCREMENT` según `02_PLAN_INCREMENTAL.md`. Inspecciona primero los consumidores actuales, reutiliza patrones/componentes, respeta arquitectura limpia y SOLID, no introduzcas sobreingeniería. Mantén fail-closed ante exportación/transmisión/resultado desconocido; no hagas trabajo sobre redes reales. Valida estáticamente todos los archivos tocados; no declares como ejecutadas las pruebas que no se puedan ejecutar. Registra estado, decisiones, pendientes y evidencia en `MEMORY_STATE.md`, `CHANGELOG.md` y documentación técnica. Devuelve nuevo **ZIP completo** con todo el proyecto y el código/documentación del incremento.

## Reglas para entregar resultado y no perder contexto

- La respuesta de cada incremento incluye nombre/descarga del ZIP completo, alcance, archivos cambiados, resultado estático, riesgos, `NEXT_INCREMENT` y diferencias del esquema si las hubiera.
- El ZIP siguiente se genera **a partir del último ZIP entregado**, nunca a partir del original o de un conjunto parcial.
- La memoria es un artefacto Markdown versionado dentro del ZIP. No afirmar persistencia de memoria entre chats: para retomarlo, aportar el ZIP último.
- Documentación de estado de esta iniciativa es `MEMORY_STATE.md`, no reemplaza el `MEMORY.md` global (en inglés) del producto.
- Si `NEXT_INCREMENT` ya existe pero no ha pasado todas las validaciones, continuar ese mismo incremento; no saltar.
- Cada nueva salida conserva todas las fuentes originales y evidencia anterior, aunque se puedan actualizar documentos de la iniciativa.

## Plantilla de resumen de cada entrega

```text
Incremento ACH-CR-001: Ixx
Fuente: <zip origen>
Salida: <zip completo>
Código: <rutas + efecto>
Documentos: <rutas actualizadas>
Validación estática: PASS / FAIL / PARTIAL
Build/tests runtime: NOT_RUN_ENVIRONMENT / RESULTADO_REAL
Migración EF: ninguna / justificación
Riesgos pendientes: ...
Siguiente: Iyy
```

## Prompt vigente al terminar I03: ejecutar I04

> Continúa ACH-CR-001 desde `ACHInterbank_ACH_CR_I03_COMPLETO.zip` completo y lee `docs/ach-cycle-reassignment/MEMORY_STATE.md`, arquitectura, I03 evidencia y plan. Ejecuta solamente I04 (último ciclo, día no hábil, fecha valor y creación sábado), reutilizando calendario, políticas, `BatchResolver` y servicios ya existentes. Mantén I03 y su tarea Quartz deshabilitados por defecto y no toques el protocolo exportador pendiente de I05 salvo hallar bloqueo funcional justificado. Sin sobreingeniería, sin nuevas migraciones/infraestructura salvo evidencia, sin llamadas financieras externas. Escribe pruebas, ejecuta validación estática real y registra limitaciones de ambiente. Actualiza docs, CHANGELOG, MEMORY_STATE y entrega ZIP **completo I04** y número de incrementos restantes.

## Prompt vigente tras I04 — siguiente I05

> Continúa ACH-CR-001 desde `ACHInterbank_ACH_CR_I04_COMPLETO.zip`. Lee `docs/ach-cycle-reassignment/MEMORY_STATE.md`, I04 evidencia, arquitectura y plan. Ejecuta únicamente I05: inspecciona puntos de lectura/escritura de membresías de archivos NACHA y despacho, coordina protocolo entre exportador y reasignador para impedir doble presentación, y comprueba regresiones con CENIT; no reintentes operaciones con envío/ACK incierto. Preserva I04 (sábado y festivos sin cambio silencioso de fecha valor), no actives el job Quartz por defecto y reutiliza infraestructura existente. Sin sobreingeniería ni migraciones sin necesidad. Escribe pruebas y validaciones estáticas honestas, actualiza `CHANGELOG.md`, `MEMORY_STATE.md`, arquitectura y entrega ZIP completo I05.

## Prompt para el siguiente incremento I06

> Continúa ACH-CR-001 desde **`ACHInterbank_ACH_CR_I05_COMPLETO.zip`**. Lee `AGENTS.md`, `MEMORY.md`, `docs/ach-cycle-reassignment/MEMORY_STATE.md` y el documento `11_I05_IMPLEMENTACION_Y_EVIDENCIA.md`. Ejecuta únicamente I06: revisión de arquitectura Clean/SOLID, dependencias/DI, validadores históricos y I05, escenarios/limitaciones pendientes, documentación, guía de homologación y entrega del ZIP final completo. **No** actives Quartz, no simules pruebas .NET/BD/ACH, no introduzcas migraciones ni rediseños. Conserva todos los cambios I00–I05; entrega `MEMORY_STATE.md` y `CHANGELOG.md` actualizados e indica qué queda pendiente de UAT/go-live.

## Prompt después del cierre I06 — Homologación, no nuevo incremento automático

> Continúa la iniciativa ACH-CR-001 desde `ACHInterbank_ACH_CR_I06_COMPLETO.zip`. Lee `AGENTS.md`, `MEMORY.md`, `docs/ach-cycle-reassignment/MEMORY_STATE.md`, `12_I06_AUDITORIA_Y_CIERRE.md` y `13_PLAN_HOMOLOGACION_GO_NO_GO.md`. El plan I00–I06 está cerrado **solo estáticamente** y no quedan incrementos previstos. En un ambiente autorizado con .NET/SQL/Quartz, ejecuta la batería focal y las pruebas de concurrencia y registra evidencia objetiva. Conserva Quartz Disabled/flag false y no envíes transferencias reales. Si hay defectos, propone corrección focal versionada y devuelve nuevo ZIP completo y memoria actualizada; no conviertas checks estáticos en GO/UAT.

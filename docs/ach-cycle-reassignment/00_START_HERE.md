# ACH-CR-001 — Reasignación automática de ciclo para transacciones pendientes

- **Corte documental:** I06-FIX01 · 2026-10-26 (parche de compilación de I06).
- **Estado:** I00–I06 entregados **a nivel estático**; se reportaron tres errores de compilación I06 y se corrigieron en I06-FIX01, **todavía sin build .NET verificado**. Quartz DESHABILITADO; compilación, tests .NET, carreras SQL, integración cámara y UAT NO EJECUTADOS. **NO-GO productivo**.
- **Proyecto base:** `ACHInterbank_ACHCOL_CFA_F6_3_UNIFICADO.zip`, raíz `ACHInterbank_ajustado/`.
- **Contrato:** en cada incremento entregar **ZIP completo autocontenido**, no solo parches, junto con documentación y memoria actualizadas.
- **Incrementos pendientes:** **0**. Próxima fase distinta de incrementos: homologación en ambiente autorizado (`13_PLAN_HOMOLOGACION_GO_NO_GO.md`).

## Lectura sugerida

1. `MEMORY_STATE.md`: estado canónico de ESTA iniciativa para retomar desde cualquier chat.
2. `01_ARQUITECTURA.md`: decisiones, límites, reutilización, transacciones y diseño Clean Architecture.
3. `02_PLAN_INCREMENTAL.md`: plan cerrado en etapas pequeñas, criterios y archivos impactados.
4. `03_REGLAS_Y_ESCENARIOS.md`: matriz de política, diferencias ACH Colombia/CENIT y aceptación.
5. `04_VALIDACION_ESTATICA.md`: comandos, checklist, criterios y evidencias sin ejecutar runtime.
6. `05_GUIA_DE_CHAT.md`: prompt reutilizable y protocolo de transferencia entre conversaciones.
7. `06_TRAZABILIDAD.md`: correspondencia con código y normas disponibles en el proyecto.
8. `07_I01_IMPLEMENTACION_Y_EVIDENCIA.md`: implementación de solo lectura, controles y límites.
9. `08_I02_IMPLEMENTACION_Y_EVIDENCIA.md`: comando persistente, revalidación, auditoría y riesgos de concurrencia.
10. `09_I03_IMPLEMENTACION_Y_EVIDENCIA.md`: invocación Quartz, doble barrera opt-in, batch paginado y riesgos.
11. `10_I04_IMPLEMENTACION_Y_EVIDENCIA.md`: originación de día futuro, calendarización, política de fecha valor y tests.
12. `11_I05_IMPLEMENTACION_Y_EVIDENCIA.md`: frontera de admisión NACHA, protección de la ruta de seguridad y regresiones CENIT.
13. `12_I06_AUDITORIA_Y_CIERRE.md`: hallazgos, límites y evidencia de cierre.
14. `13_PLAN_HOMOLOGACION_GO_NO_GO.md`: pruebas ejecutables pendientes, pasos seguros, GO/NO-GO.
15. `14_I06_FIX01_CORRECCIONES_COMPILACION.md`: los tres diagnósticos C#, parche mínimo y guía de recompilación.
16. `15_I06_FIX01_VALIDACION.json`: resultados estáticos del parche y limitaciones de ejecución.
17. `CHANGELOG.md`: bitácora de cada entrega.

## Regla de oro

**No mover una operación si existe cualquier evidencia de presentación externa, despacho, resultado desconocido, archivo inmutable o devolución no resuelta.** El hecho de que `State == Pending` no prueba ausencia de envío.

## Alcance I00–I05 / fuera de alcance

I00 no altera archivos `.cs`, `.csproj`, `.sln`, scripts existentes, migraciones, infraestructura, endpoints ni UX. I01 agrega exclusivamente contrato, resultado, consulta read-only y tests no ejecutados. I02 agrega contrato/comando de traslado persistente focal, pruebas escritas y auditoría existente, **sin APIs, migraciones ni UX**. I03 añade adaptador Quartz deshabilitado por defecto, sin habilitar el despacho ni resolver I05. I04 ajusta solo la proyección de la originación no-CENIT al siguiente día hábil y mantiene prohibido el traslado entre fechas valor de instrucciones ya creadas. No ejecuta build, tests de ejecución, migraciones, contenedores ni integraciones. La evidencia de este corte es **estática únicamente**. No certifica ACH/CENIT, UAT, go-live ni comportamiento real.

Los documentos del programa no reemplazan `AGENTS.md`, `MEMORY.md` ni `docs/CURRENT_PROJECT_STATUS.md`; esos archivos conservan sus reglas y antecedentes. En caso de contradicción, prevalecen código/configuración actual y normativa aplicable para comportamiento, y `MEMORY_STATE.md` únicamente para el avance de ACH-CR-001.

## Política de trabajo incremental

- Una intención funcional acotada por incremento; no refactors amplios, patrones vacíos ni nuevos frameworks.
- Reutilizar servicios de calendario, ciclo, políticas, lote, auditoría, exportación y Quartz.
- Preservar arquitectura limpia: Domain (invariantes), Application (contratos/casos), Persistence (EF/orquestación), API/Quartz (adaptadores), SPA solo si aporta valor.
- Cada incremento debe dejar código coherente, sin estados inventados, con validación estática y lista explícita de pruebas dinámicas pendientes.
- Sin bloqueos técnicos por carencia de `.NET`/Docker: registrar `NOT_RUN_ENVIRONMENT` y continuar únicamente dentro del alcance de validación estática acordado.
- No reinterpretar evidencia normativa sobre la base de reglas genéricas; diferenciar regla externa, diseño propuesto y decisión pendiente.

## Qué significa este cierre

El paquete I06 entrega fuentes y documentación completa, **no** una validación de producción. La verificación por código no demuestra exclusión mutua efectiva entre procesos externos ni aprobación bancaria. Para seguir, utilizar `MEMORY_STATE.md` y la checklist de homologación; mantener el job deshabilitado.

# Changelog acumulativo — respuestas a prenotificaciones

## I00 — 2026-11-02 — Completado (documental)

- Se preservó íntegro el ZIP de origen `ACHInterbank_PROYECTO_COMPLETO_CORRECCIONES_CENIT_F08_2026-11-02.zip` como base de código.
- Se incorporó el plan detallado I01–I07, la auditoría previa SPA/backend y sus 14 comprobaciones estáticas (no pruebas runtime).
- Se agregó `MEMORY_STATE.md` portátil, protocolo de ChatGPT en modo chat, tablero de estados por incremento, matriz PRE-01..PRE-14, referencia normativa/documental suministrada y el informe I00.
- No se modificó ninguna clase, controlador, componente, migración ni test del proyecto fuente en I00.
- Pruebas de nueva funcionalidad: **no ejecutadas**. Validación ZIP y hash del origen registrada en `INFORME_I00.md`.

## I01 — 2026-11-02 — EN_PROGRESO (código implementado, runtime pendiente)

- Se agregó `IncomingPrenotificationReturn=10` sin cambiar `IncomingPrenotificationResponse=3`, y el enum tipado `Returned` / `NoReturnByDeadline`.
- Se añadió DTO dedicado con IDs originales, ciclo original/response, cámara, fecha, banco, causal e idempotency key; validación estructural pura en capa Application.
- Se agregaron dos endpoints read-only protegidos para describir y validar el contrato, sin ruta de ejecución nueva.
- Preview/Generate del escenario 10 permanece fail-closed con `PRENOTIFICATION_RETURN_NOT_IMPLEMENTED`.
- Se añadieron pruebas C# de compatibilidad, forma, resultados, payload y bloqueo en el servicio.
- Se documentaron los ejemplos por cámara, los casos de uso, la compatibilidad y las restricciones operativas.
- `PASS`: grafo de proyectos (7/7), verificación estática I01 (20/20). `NO_EJECUTADA_POR_AMBIENTE`: .NET build/tests (SDK no disponible).
- No se generó archivo NACHA de prenotificación, no se importó, no se afectaron estados. Próxima implementación: I02.

## I02 — 2026-11-02 — EN_PROGRESO (código implementado, runtime pendiente)

- Dos endpoints read-only `prenotifications/originated` y `prenotifications/eligibility-preview` con las autorizaciones existentes.
- Modelos y políticas de elegibilidad pura, selección paginada y control de intento actual, transmisión acreditada, respuestas previas, cámara, banco, ciclo y plazo.
- Validaciones diferenciadas ACHCOL/CENIT; no fabricación de aprobación por NACHA-M.
- Pruebas Xunit unitarias/SQLite añadidas, no ejecutadas por falta de SDK; 26/26 comprobaciones estáticas PASS.
- Se mantuvo bloqueo de generación y ninguna escritura de estado. I03 queda encargado de idempotencia durable transaccional, secuencias y NACHA-M de devolución.

## I03 — 2026-11-02 — EN_PROGRESO (código incorporado, runtime pendiente)

- Nuevo endpoint protegido de devolución de prenotificaciones, contrato de servicio, identidad idempotente y builder oficial.
- Revalidación I02 dentro de transacción serializable, reserva UNIQUE y rechazo de archivos duplicados por ID original.
- Perfil homologado, catálogo de causal, SEC, trace, monto cero y evidencia; directorio aislado con limpieza best-effort.
- Sin autoimportación, alteración de estados, transmisión externa ni generación de aprobación por ausencia de devolución.
- Nuevas pruebas Xunit y comprobaciones estáticas 31/31 PASS_STATIC; I01 20/20 e I02 26/26 permanecen PASS_STATIC. SDK .NET ausente, no compilado ni homologado.
- Riesgos y pendientes: pruebas reales multibase, concurrencia, recuperación de archivos huérfanos, validez de bytes NACHA y NachaUpload E2E.

## I04 — 2026-11-02 — EN_PROGRESO (código incorporado, runtime pendiente)

- Guarda de prenotificación entrante antes de motor de transiciones, por trace/adenda 99/importe cero/cámara/intento terminal y procedencia I03 (SHA-256).
- Compatibilidad SHA mayúscula del generador/minúscula de NachaUpload; validación de banco, causal y pertenencia sin confiar en nombres editables.
- Ajuste de decisión de catálogo por `IsPrenotification` y política CENIT para transacciones con Type monetario legado.
- Se reutiliza sincronización de validaciones/terceros y deduplicación del motor existente: sin segunda escritura de estado.
- Pruebas C# 12 casos de política más dos orquestaciones añadidas; I04 31/31 PASS_STATIC, demás 77/77 PASS_STATIC y grafo PASS. .NET y E2E no ejecutados.
- Pendientes: compilación, UAT por cámara, SHA tras descifrar, reupload/atomicidad sobre BD real.

## I05–I07 — Historial (ver detalles por incremento más abajo)

Agregar aquí para cada incremento: archivos modificados, decisiones, casos, evidencias, pruebas ejecutadas/no ejecutadas, SHA-256 del ZIP, hallazgos remanentes y próxima entrega.

## I05 — 2026-11-02 — EN_PROGRESO (código incorporado, runtime pendiente)

- Añadidas dos rutas de aceptación tácita protegidas: preview con reloj virtual de solo lectura y aplicación controlada por vencimiento real.
- Conservación de semántica de ausencia de devolución: no archivo NACHA-M ficticio.
- Uso de `TimeProvider` en el job existente sin cambiar políticas ni reescribir estados a mano.
- Revalidación de envío, cámara, banco, intento vigente, política canónica, plazo, resultado previo, archivo de devolución pendiente e idempotencia.
- Tests C# para política temporal y vistas virtuales por cámara. 36/36 comprobaciones estáticas I05 + 108/108 previas, grafo 7/7; .NET no ejecutado.
- `I06` seguirá con interfaz SPA, descarga autenticada y E2E de navegación.

## I06 — 2026-11-02 — EN_PROGRESO (SPA implementada, runtime pendiente)

- Flujo UI de tres etapas en la SPA actual, sin rutas, componentes ni dependencias extra.
- Selección de prenotificaciones salientes elegibles, búsqueda paginada, motivos de bloqueo y opciones excluyentes devolución/vencimiento.
- Preview por API, confirmación y generación I03 de archivo o aplicación I05 con reloj real; el reloj virtual es solo consulta.
- Descarga autenticada de blobs por HttpClient, transición manual a NachaUpload y reporte de resultado.
- Mejora de etiquetas: la respuesta de prenotificación legado ya no se ofrece como nueva acción, pero conserva representación histórica.
- Añadidos cinco tests Angular y verificador estático (32/32); regresión I01–I05 144/144 y grafo 7/7 PASS.
- Sin pruebas Karma/Playwright ni compilación por `npm ci --offline` fallido ENOTCACHED zone.js. I07 debe comprobar UI real y cumplimiento por cámara.

## I07 — 2026-11-02 — EN_PROGRESO (regresión estática y correcciones; homologación UAT pendiente)

- Selector de ciclos de devolución prenotificación corregido sin tocar escenarios preexistentes.
- UI robustecida frente a respuestas tardías; conserva tres pasos y estilos actuales.
- Vista previa de aceptación tácita refleja las guardas reales de apply-due.
- 3 pruebas C# y 3 Angular nuevas, no ejecutadas por ambiente; verificación estática I07 24/24, regresión completa 200/200, grafo 7/7.
- Matriz PRE-01..PRE-14: 0/14 homologadas. Nuevo ZIP completo I07 y plan I07-R de ejecución real.

## I07-R1 — 2026-11-02 (robustecimiento, no homologación)

- UI: se descartan respuestas tardías de previsualización según selección/cámara/causal/resultado actual.
- Backend/API: descargar solo si SHA-256 y longitud coinciden con evidencia; 422 si falla.
- Nuevas pruebas Angular/xUnit, verificador estático I07-R1, informe y estado actualizado.
- Resultado de comprobaciones: grafo 7/7; I01–I07 200/200; I07-R1 20/20; TS sintaxis 2/2.
- No hay SDK .NET ni npm offline disponible; **PRE-01..PRE-14: 0/14 homologadas**. Próximo I07-R2.

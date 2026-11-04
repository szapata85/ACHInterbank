# I04-ACH — Revisión y aprobación interna de candidatas de devolución

**Fecha:** 2 de noviembre de 2026. **Cámara:** ACH Colombia (`ACHCOL`) exclusivamente. **Base:** ZIP acumulativo I03-ACH. **Resultado:** incorporación en código, sin homologación en ejecución. **Producción:** NO-GO.

## Funcionalidad incorporada

Se añadió una sección «Candidatas de ACH Colombia» a la pantalla existente «Devoluciones», conservando Angular Material. Permite consultar hasta 50 candidatas, actualizar la vista, confirmar la aprobación o descartar un registro. Presenta referencia, tipo, valor, causal, ciclo y estado. En la interfaz se informa con claridad que **aprobar no genera ni envía un .RET**.

Una API independiente con autorización reutilizada ofrece:

- `GET api/ach-colombia/devoluciones-automaticas/candidatas?limit=50` — lectura con `P0.ReturnsRead`, límite 1–100, solo ACHCOL.
- `POST api/ach-colombia/devoluciones-automaticas/candidatas/{id}/decision` — acción `APROBAR` o `DESCARTAR`, con `P0.ReturnsGenerateFile`. Devuelve estado, no archivo, no efecto financiero.

La aprobación vuelve a validar configuración, cámara, evento funcional I02, causa y catálogos, horario de la ventana de cuatro ciclos, coincidencia con el ciclo I03 y ausencia de devolución manual generada. Rechaza por HTTP 409 un estado ya cambiado, vencimiento, causal no válida, CENIT, configuración desactivada o devolución manual existente. Rechaza los créditos tardíos y R10 sin base adicional; solo permite descartarlos. Los cambios se registran en la auditoría EF existente y usan `Status` como token de concurrencia.

## Decisiones de simplificación

No hay nueva tabla, cron, cola, ruta Angular, esquema de permisos ni generador de archivos. Reutiliza `AchColombiaReturnPreparations`, `AchReturnsGenerated`, catálogos, `AchColombiaReturnPreparationWindow`, auditoría `AchDbContext`, y las políticas actuales. Una única sección con lista y botones evita pantallas redundantes. Los estados internos son `PREPARADA_SIN_ENVIO`, `REVISION_TARDIA_CREDITO`, `APROBADA_SIN_ENVIO` y `DESCARTADA`; su traducción visible es española.

La aprobación **no habilita automáticamente el generador ni reserva recursos bancarios**. La generación manual sigue exactamente como estaba. Una aprobación I04 puede anteceder a una devolución manual generada más tarde: I05 deberá revalidar nuevamente contra los archivos generados, dentro de la misma operación de generación.

## Controles regulatorios y operativos

- ACH Colombia y CENIT se mantienen separados. Ninguna autorización ACH Colombia se propaga a CENIT.
- Créditos tardíos ACH Colombia permanecen para revisión especializada, no se elevan a aprobación automática.
- R10 se bloquea sin base verificable adicional; no se inventa información.
- No se aprueba un ciclo sugerido que ya no está vigente, ni se selecciona otro ciclo silenciosamente.
- `P0.ReturnsRead` y `P0.ReturnsGenerateFile` ya existían; no se conceden permisos nuevos.
- Si dos operadores actúan sobre la misma candidata, EF comprueba el valor original de `Status` y devuelve 409. La prueba con motor SQL Server está pendiente.

## Ejecución de pruebas

| Prueba | Resultado |
|---|---|
| Verificador estático I04, 35 reglas | PASS_STATIC 35/35 |
| Regresión estática I02 | PASS_STATIC 18/18 |
| Regresión estática I03 | PASS_STATIC 25/25 |
| Grafo de siete proyectos | PASS 7/7 |
| Parser TypeScript en 6 archivos | PASS_PARSER 6/6 |
| xUnit, dotnet build, SqlServer | NO EJECUTADO: falta SDK / motor |
| Angular build/Karma/Playwright | NO EJECUTADO: no hay node_modules / dependencias de build |
| Prueba de pantalla real y concurrencia distribuida | PENDIENTE |

**No se declara la implementación probada en ejecución ni lista para producción.** Ver `EVIDENCIA_I04_ACH.md` y script de verificaciones.

## Parada

Se detiene después de **I04-ACH**. **Siguiente I05-ACH**: generación controlada solo a partir de candidatas aprobadas, sin envío automático, con defensa de duplicidad en transacción y persistencia. CENIT permanece independiente. Mantener bloqueada la salida productiva hasta que pasen pruebas reales, migraciones y homologación.

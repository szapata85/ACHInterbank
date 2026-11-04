# MEMORY_STATE — DEVOLUCIONES AUTOMÁTICAS — I05-ACH

**Corte:** 2026-11-02. **Base obligatoria siguiente:** ZIP completo `ACHInterbank_COMPLETO_DEVOLUCIONES_AUTOMATICAS_I05_ACHCOLOMBIA_2026-11-02.zip`. **Último incremento:** I05-ACH, generación controlada desde aprobadas. **Siguiente:** I06-ACH. **Producción:** **NO-GO**. **CENIT:** sin modificación funcional en esta ruta.

## Hechos comprobados en código

- I01: parámetro ACH Colombia `DESACTIVADO`/`CON_APROBACION`/`AUTOMATICO`, `ProcessingConnected=false` (la generación verdaderamente automática sigue desconectada).
- I02: detección únicamente de fallas funcionales confirmadas de ACH Colombia desde la integración real; no crear devoluciones por errores técnicos.
- I03: tabla `AchColombiaReturnPreparations`, un índice único por original y otro por evento; cálculo de ventana de cuatro ciclos con número de ciclo, no orden horario (primer ciclo puede cruzar medianoche).
- I04: revisión y aprobación internas. `APROBADA_SIN_ENVIO` no crea archivo; `DESCARTADA` terminal. Controles de permisos y concurrencia de `Status`.
- **I05:** se añadió una acción deliberada de generación desde una única candidata `APROBADA_SIN_ENVIO`. Nueva ruta `POST api/ach-colombia/devoluciones-automaticas/candidatas/{candidateId}/generar-archivo`, con `P0.ReturnsGenerateFile`. El backend carga cámara/causal/ciclo **de la reserva**, no del navegador; repite validaciones I04 y llama al **generador existente** `IAchReturnsService.GenerateReturnsFileAsync`.
- Un resultado válido requiere archivo no vacío y registro correspondiente en `AchReturnsGenerated`; cambia a `ARCHIVO_GENERADO_SIN_ENVIO`. Para BD relacional se utiliza una transacción envolvente dentro de la estrategia de reintentos EF y auditoría existente (pendiente de prueba con proveedores reales).
- La SPA actual añade una confirmación y descarga con nombre de archivo devuelto por el servidor; respuesta JSON con contenido base64, sin enviar a la cámara. Angular Material, español y permisos existentes.

## Invariantes que no deben romperse

1. **Ninguna devolución de CENIT** debe entrar por la API I05-ACH; CENIT sigue independiente.
2. **Ningún modo configura envío automático:** I05 es generación **a petición**, no servicio autónomo. `DESACTIVADO` prohíbe generación desde candidatas; `AUTOMATICO` no autoriza saltarse una aprobación existente ni generar sin clic en I05.
3. Solo `APROBADA_SIN_ENVIO` + cámara ACH Colombia + causal válida + ventana vigente + evento verificado I02 + sin `AchReturnsGenerated` previo puede producir archivo.
4. Nunca inferir causal de mensajes técnicos ni autorizar R10 sin evidencia. Créditos tardíos quedan para revisión aparte.
5. **No prometer** que archivo generado implica archivo transmitido, compensado o liquidado. El generador histórico sí cambia el estado interno de la transacción a `ReturnedByEpr`, pendiente de validación semántica en UAT.
6. El doble clic, segunda instancia o un archivo manual competidor no debe generar duplicados: usar controles del generador (`AchReturnsGenerated` índice único, lock) y token `Status`. Prueba multiinstancia REAL pendiente.
7. No crear nueva tabla/cola/cron/endpoint de CENIT ni rediseñar SPA. Reutilizar pantalla, permisos, generador y auditoría existentes.
8. **No repetir generación por fallo de descarga** después de éxito de servidor: primero consultar registros/artefactos; I05 no crea funcionalidad de re-descarga.
9. `MEMORY.md` y `MEMORY_STATE.md` **en raíz** son históricos y se conservan. Este archivo y `docs/implementacion-devoluciones-automaticas/MEMORY_STATE.md` son continuidad actual de esta iniciativa.
10. El ZIP acumulativo completo, no un parche, es la entrada obligatoria del incremento siguiente.

## Pruebas y evidencia

- Verificador I05: **33/33 PASS_STATIC**. Regresión estática I02 **18/18**, I03 **25/25**.
- Script I04 histórico: **33/35** sobre I05; dos aserciones de ausencia absoluta de generación son incompatibles con el nuevo alcance, no se reescribió el histórico.
- Parseo TypeScript y grafo solución: sin error de sintaxis, 7/7 rutas válidas. No se dispone de SDK .NET, `node_modules`, SQL Server/PostgreSQL ni UAT en el entorno.
- xUnit/Jasmine agregados pero **no ejecutados**. Sin autorización productiva.
- Paridad PostgreSQL I01/I03, pruebas SQL, semántica de estado del generador, calibración de ventana y recuperación de archivo tras fallo de descarga: **pendientes**.

## Punto de parada preciso

**I05-ACH incorporado en código, empaquetado; ejecución y homologación pendientes. NO-GO.** I06-ACH deberá estudiar riesgos anteriores antes de conectar automatización/transporte; no activar CENIT ni asumir que un archivo `.RET` generado ha sido recibido por una cámara.

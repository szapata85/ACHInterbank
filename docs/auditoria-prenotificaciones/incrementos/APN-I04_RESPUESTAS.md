# APN-I04 — Recepción, clasificación, correlación y devoluciones

**Estado:** EJECUTADO_ESTATICAMENTE; HOMOLOGACION_RUNTIME_PENDIENTE. **Código productivo modificado:** no.

## Ruta identificada

1. `IncomingNachaPostParseProcessor.ProcessAsync` obtiene headers, detalles y adendas; clasifica por `IIncomingNachaFunctionalClassifier`.
2. Llama al `IIncomingNachaTransactionLinker.LinkAsync` con contexto de cámara, ciclo y fecha operacional.
3. Trata vínculos ambiguos o no encontrados con evidencia y resolución manual, no como éxito.
4. `IncomingNachaPrenotificationResolver` y `IncomingPrenotificationReturnGuard` participan en correlación y prevención de respuestas no elegibles.
5. `AchStateTransitionService.TransitionAsync` mueve el estado según el evento admitido.

## Reglas a verificar con archivos reales o fixtures

- Diferenciar devolución del participante receptor (`ReturnedByEpr`), rechazo/devolución del operador (`ReturnedByOperator`) y fallo de archivo.
- Correlación por identificación, traza y cámara correctas; nunca atribuir un archivo de CENIT a una prenotificación ACH Colombia.
- Validar causales NACHA-M aplicables a débito/crédito y adendas de devolución; nunca crear una respuesta positiva artificial.
- Duplicados, archivos tardíos y secuencias fuera de orden; idempotencia y estado del intento actual.
- Trazar `ingestionId`, hash, `AchTransactionId`, identificador de intento y timestamp de la devolución.

**No se han ejecutado P02, P03, P06, P09–P12 ni P17 sobre .NET/BD.** Existencia de clases y tests previos no demuestra conformidad bancaria ni ejecución de los casos APN.

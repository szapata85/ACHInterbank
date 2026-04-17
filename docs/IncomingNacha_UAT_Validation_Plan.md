# Plan de validación UAT/MVP — NACHA-M Entrante

## Escenarios obligatorios

1. Archivo ACH válido con créditos elegibles  
   - Esperado: Ingesta `Completado`, cola `Queued/Confirmed`, ejecución con `IsSuccess=true`.
2. Archivo CENIT válido con elegibles  
   - Esperado: misma trazabilidad con prioridad cámara CENIT.
3. Archivo duplicado  
   - Esperado: `Duplicado`, sin nuevas filas en cola de despacho.
4. Ciclo ambiguo/no resuelto  
   - Esperado: `Blocked`, evidencia en eventos de procesamiento.
5. Prenotificación  
   - Esperado: actualización de tercero, sin despacho a Proc_Transacciones.
6. Devolución válida  
   - Esperado: transición interna ACH, sin despacho a Proc_Transacciones.
7. Devolución con link inseguro  
   - Esperado: bloqueo y revisión manual.
8. Fuera de ventana  
   - Esperado: `WaitingWindow`.
9. Encolado posterior correcto  
   - Esperado: `IncomingNachaDispatchQueue` con `IdempotencyDispatchKey` único.
10. Ejecución Quartz handler  
   - Esperado: toma chunk correcto y no procesa no-elegibles.
11. Dispatch exitoso Proc_Transacciones  
   - Esperado: `Confirmed`.
12. Error técnico retryable  
   - Esperado: `RetryPending`, `NextAttemptAtUtc`.
13. Error funcional no retryable  
   - Esperado: `FailedFinal`.
14. Reproceso seguro  
   - Esperado: no duplicidad por `IdempotencyDispatchKey`.
15. Reconstrucción forense  
   - Esperado: archivo -> clasificación -> link -> cola -> ejecución -> estado final.

## Dataset mínimo UAT

- 2 cámaras (ACH=1, CENIT=2), ciclos activos e inactivos.
- 3 archivos NACHA por cámara (válido, duplicado, ambiguo).
- 30 transacciones por archivo (incluyendo prenote y devoluciones).
- Mapping publicado para `WSCFAACH.Proc_Transacciones`.

## Criterios de salida UAT técnico-controlado

- 100% de escenarios críticos (1,2,3,4,8,11,12,13,15) en verde.
- 0 duplicidades de envío.
- 100% trazabilidad de request/response hash por ejecución.
- 0 transacciones elegibles sin cola.

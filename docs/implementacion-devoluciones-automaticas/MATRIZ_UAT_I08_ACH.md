# Matriz UAT I08-ACH — Validación binaria, transacciones y estado financiero

**Estado general: PENDIENTE DE EJECUCIÓN.** Pruebas unitarias están escritas; sin SDK .NET en este ambiente. No confundir `PASS_STATIC` con `PASS_TEST`.

| ID | Prueba | Resultado requerido | Estado |
|---|---|---|---|
| UAT-I08-01 | Generación automática ACH con archivo determinista | Archivo reconstruido idéntico byte a byte, candidata `ARCHIVO_GENERADO_SIN_ENVIO`, cero transportes | PENDIENTE |
| UAT-I08-02 | Byte del artefacto alterado | Conflicto, rollback SQL íntegro, cero envíos | PENDIENTE |
| UAT-I08-03 | Cámara, ciclo o identificador de transacción distinto | Rechazo sin confirmar y sin afectar CENIT | PENDIENTE |
| UAT-I08-04 | Servicio de reconstrucción no registrado | No se llama al generador y permanece estado original | PENDIENTE |
| UAT-I08-05 | Reconstrucción falla (excepción controlada) | Conflicto, sin confirmación, sin reintento ciego | PENDIENTE |
| UAT-I08-06 | Dos ejecutores concurrentes SQL Server | Una sola devolución y una sola secuencia asignada; sin estado parcial | PENDIENTE |
| UAT-I08-07 | Dos ejecutores concurrentes PostgreSQL | Igual que UAT-I08-06, con migraciones aplicadas | PENDIENTE |
| UAT-I08-08 | Recuperación posterior I07 | SHA-256 coincide con archivo creado antes del commit | PENDIENTE |
| UAT-I08-09 | Mutación posterior del dato fuente | Recuperación bloqueada si bytes difieren; no entrega corrupta | PENDIENTE |
| UAT-I08-10 | Parámetro `DESACTIVADO` / bandera técnica `false` | No genera ni transmite | PENDIENTE |
| UAT-I08-11 | Generación explícita/manual | Flujo anterior I05 sin cambio ni regresión | PENDIENTE |
| UAT-I08-12 | CENIT | Ningún cambio funcional ni ejecución por ruta ACH | PENDIENTE |
| UAT-I08-13 | Reportes con `ReturnedByEpr` generado sin acuse | Nunca interpretar como aceptado, compensado o liquidado | PENDIENTE |
| UAT-I08-14 | Matriz cinco/siete ciclos | Regla certificada para la configuración operativa vigente | PENDIENTE |
| UAT-I08-15 | Doble pérdida/recuperación de conectividad tras generación | Sin nueva secuencia ni doble devolución | PENDIENTE |

Para ejecutar en un ambiente autorizado: `dotnet --info`, compilar solución, ejecutar filtro xUnit I08 e I01–I07, montar contenedores/bases efímeras, comparar archivos golden, guardar trazas y reportar cada caso con fecha, responsable y evidencia. **No marcar como aprobado sin salida verificable.**

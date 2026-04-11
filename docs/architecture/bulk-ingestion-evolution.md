# Bulk Ingestion Evolution Blueprint

## Objetivo
Preparar la arquitectura actual de carga masiva ACH para escalar sin romper contratos existentes.

## Extensiones introducidas
- `IBulkIngestionWorkDispatcher`
  - Abstrae el despacho de procesamiento (Quartz hoy, cola distribuida mañana).
- `IBulkIngestionProgressNotifier`
  - Abstrae notificaciones de avance (no-op hoy, SignalR/Webhook/EventBus mañana).
- `IBulkIngestionLifecycleService`
  - Define cancelación y archivado/expiración de lotes.
- `BulkIngestionEvolutionOptions`
  - Flags/configuración para activar estrategia evolutiva por ambiente.

## Escenarios futuros cubiertos por diseño
1. **Procesamiento por colas distribuidas**
   - Implementar `IBulkIngestionWorkDispatcher` con SQS/Rabbit/Kafka/Azure Queue.
2. **Carga desde almacenamiento externo**
   - Extender flujo de ingestión con referencias a blob/object storage y lectura lazy.
3. **Chunked processing de archivos grandes**
   - Activar `EnableChunkedLargeFileProcessing` + workers por chunk.
4. **Notificaciones de avance del lote**
   - Implementar `IBulkIngestionProgressNotifier` con pub/sub y canal UI en tiempo real.
5. **Cancelación de lotes**
   - Exponer endpoint sobre `IBulkIngestionLifecycleService.RequestCancellationAsync`.
6. **Paginación por batch**
   - Ya cubierta por `GetBatchItems(page,pageSize,status)` y configurable vía options.
7. **Expiración y archivado**
   - Implementar job de archivado usando `ArchiveExpiredBatchesAsync`.

## Guía de adopción incremental
- Fase 1: mantener Quartz + no-op notifier (estado actual).
- Fase 2: dispatcher distribuido coexistiendo por feature flag.
- Fase 3: progress real-time y cancelación expuesta al frontend.
- Fase 4: archival policy + storage tiering.

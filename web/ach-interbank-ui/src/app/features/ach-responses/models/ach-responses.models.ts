export type TipoRespuestaAch = 'Prenota' | 'Transaccion';

export type AchResponseProcessingStatus =
  | 'Recibida'
  | 'Homologada'
  | 'Notificada'
  | 'ErrorFuncional'
  | 'PendienteReintento'
  | 'RequiereRevisionManual'
  | 'NoHomologada'
  | 'Duplicada'
  | 'PendienteCorrelacion'
  | 'Huerfana'
  | 'EnRevision'
  | 'Resuelta'
  | 'Rechazada'
  | 'ErrorTecnico'
  | 'PendienteReproceso'
  | 'Reprocesando'
  | 'Reprocesada'
  | 'Cerrada';

export type AchResponseNotificationStatus =
  | 'Pendiente'
  | 'Exitosa'
  | 'ErrorFuncional'
  | 'ErrorTecnico'
  | 'PendienteReintento'
  | 'RequiereRevisionManual';

export interface ProcesarRespuestaAchRequest {
  tipoRespuesta: TipoRespuestaAch;
  idTransaccion: string;
  codigoCamaraCompensacion: string;
  codigoEntidadOrigen?: string | null;
  codigoEntidadDestino?: string | null;
  codigoEstadoExterno: string;
  codigoCausalExterna?: string | null;
  descripcionCausalExterna?: string | null;
  idCanal: number;
  nombreCanal: string;
  idTransaccionServicioExterno: number;
  fechaRecepcion?: string | null;
  correlationId?: string | null;
}

export interface ProcesarRespuestaAchResponse {
  achResponseId?: string | null;
  procesada: boolean;
  duplicada: boolean;
  existeHomologacion: boolean;
  permiteNotificacion: boolean;
  intentoPendienteCreado: boolean;
  estadoProcesamiento: AchResponseProcessingStatus | string;
  motivo?: string | null;
  hashIdempotencia?: string | null;
}

export interface NotificarRespuestaAchRequest {
  notificationAttemptId: number;
  correlationId?: string | null;
}

export interface NotificarRespuestaAchResponse {
  procesada: boolean;
  encontrada: boolean;
  yaProcesada: boolean;
  existeError: boolean;
  errorTecnico: boolean;
  estadoNotificacion?: AchResponseNotificationStatus | string | null;
  estadoProcesamiento?: AchResponseProcessingStatus | string | null;
  codigoError?: string | null;
  descripcionError?: string | null;
  errorTecnicoDetalle?: string | null;
  motivo?: string | null;
}

export interface AchResponseSearchRequest {
  fechaDesde?: string | null;
  fechaHasta?: string | null;
  tipoRespuesta?: TipoRespuestaAch | null;
  idTransaccion?: string | null;
  codigoCamaraCompensacion?: string | null;
  codigoEntidadOrigen?: string | null;
  codigoEntidadDestino?: string | null;
  codigoEstadoExterno?: string | null;
  estadoProcesamiento?: AchResponseProcessingStatus | string | null;
  correlationId?: string | null;
  pageNumber?: number;
  pageSize?: number;
}

export interface AchResponseDashboardRequest {
  fechaDesde?: string | null;
  fechaHasta?: string | null;
  tipoRespuesta?: TipoRespuestaAch | null;
}

export interface AchResponseDashboardResponse {
  totalRespuestas: number;
  recibidas: number;
  homologadas: number;
  notificadas: number;
  noHomologadas: number;
  revisionManual: number;
  pendientesReintento: number;
  erroresFuncionales: number;
  duplicadas: number;
}

export interface PagedResponse<T> {
  items: T[];
  pageNumber: number;
  pageSize: number;
  totalCount: number;
  totalPages: number;
}

export interface AchResponseListItemResponse {
  id: string;
  tipoRespuesta: TipoRespuestaAch | string;
  idTransaccion: string;
  codigoCamaraCompensacion: string;
  codigoEntidadOrigen?: string | null;
  codigoEntidadDestino?: string | null;
  codigoEstadoExterno: string;
  codigoCausalExterna?: string | null;
  estadoInternoNombre?: string | null;
  estadoProcesamiento: AchResponseProcessingStatus | string;
  permiteNotificacion: boolean;
  correlationId?: string | null;
  fechaRecepcion: string;
  fechaCreacion: string;
}

export interface AchResponseDetailResponse {
  id: string;
  tipoRespuesta: TipoRespuestaAch | string;
  idTransaccion: string;
  codigoCamaraCompensacion: string;
  codigoEntidadOrigen?: string | null;
  codigoEntidadDestino?: string | null;
  codigoEstadoExterno: string;
  codigoCausalExterna?: string | null;
  idEstadoInterno?: number | null;
  idEstadoServicioExterno?: number | null;
  estadoInternoNombre?: string | null;
  causalNormalizada?: string | null;
  descripcionCausal?: string | null;
  idTransaccionServicioExterno: number;
  hashIdempotencia: string;
  estadoProcesamiento: AchResponseProcessingStatus | string;
  motivoNoHomologacion?: string | null;
  permiteNotificacion: boolean;
  correlationId?: string | null;
  fechaRecepcion: string;
  fechaCreacion: string;
  fechaActualizacion?: string | null;
  notificationAttempts: AchResponseNotificationAttemptResponse[];
  clearingHouseId: number;
  appliedMappingId?: number | null;
  duplicateReceiptCount: number;
  version: string;
}

export interface AchResponseNotificationAttemptResponse {
  id: number;
  achResponseId: string;
  numeroIntento: number;
  estadoNotificacion: AchResponseNotificationStatus | string;
  idCanal: number;
  nombreCanal: string;
  idTransaccion: string;
  idEstado: number;
  causal?: string | null;
  idTransaccionServicioExterno: number;
  descripcionCausal?: string | null;
  existeError?: boolean | null;
  codigoError?: string | null;
  descripcionError?: string | null;
  errorTecnico?: string | null;
  fechaCreacion: string;
  fechaEnvio?: string | null;
}

export interface AchResponseStatusMappingRequest {
  codigoCamaraCompensacion: string;
  tipoRespuesta: TipoRespuestaAch;
  codigoEstadoExterno: string;
  codigoCausalExterna?: string | null;
  idEstadoInterno: number;
  idEstadoServicioExterno: number;
  estadoInternoNombre: string;
  causalNormalizada?: string | null;
  descripcionCausalNormalizada?: string | null;
  requiereCausal: boolean;
  permiteNotificacion: boolean;
  activo: boolean;
  fechaInicioVigencia: string;
  fechaFinVigencia?: string | null;
}

export interface AchResponseStatusMappingResponse {
  id: number;
  codigoCamaraCompensacion: string;
  tipoRespuesta: TipoRespuestaAch | string;
  codigoEstadoExterno: string;
  codigoCausalExterna?: string | null;
  idEstadoInterno: number;
  idEstadoServicioExterno: number;
  estadoInternoNombre: string;
  causalNormalizada?: string | null;
  descripcionCausalNormalizada?: string | null;
  requiereCausal: boolean;
  permiteNotificacion: boolean;
  activo: boolean;
  fechaInicioVigencia: string;
  fechaFinVigencia?: string | null;
  clearingHouseId: number;
  priority: number;
  version: string;
}

export interface AchResponseStatusMappingsFilter {
  codigoCamaraCompensacion?: string | null;
  tipoRespuesta?: TipoRespuestaAch | null;
  activo?: boolean | null;
}

export interface AchResponseMappingWriteRequest {
  clearingHouseId: number;
  responseType: TipoRespuestaAch;
  externalCode: string;
  externalCause?: string | null;
  internalStatusId: number;
  externalServiceStatusId: number;
  internalStatusName: string;
  normalizedCause?: string | null;
  normalizedDescription?: string | null;
  requiresCause: boolean;
  allowsNotification: boolean;
  priority: number;
  effectiveFrom: string;
  effectiveTo?: string | null;
  isActive: boolean;
  expectedVersion?: string | null;
  reason: string;
}

export interface AchResponseMappingModel {
  id: number;
  clearingHouseId: number;
  clearingHouseCode: string;
  responseType: TipoRespuestaAch | string;
  externalCode: string;
  externalCause?: string | null;
  internalStatusId: number;
  externalServiceStatusId: number;
  internalStatusName: string;
  normalizedCause?: string | null;
  normalizedDescription?: string | null;
  requiresCause: boolean;
  allowsNotification: boolean;
  priority: number;
  effectiveFrom: string;
  effectiveTo?: string | null;
  isActive: boolean;
  version: string;
}

export interface AchResponseAuditModel {
  id: number;
  entityType: string;
  entityId: string;
  action: string;
  previousState?: string | null;
  newState?: string | null;
  actor: string;
  reason: string;
  correlationId: string;
  occurredAtUtc: string;
  sanitizedMetadata?: string | null;
}

export interface AchResponseOrphanModel {
  id: string;
  achResponseId: string;
  clearingHouseId: number;
  responseType: string;
  externalIdentifiers: string;
  externalCode: string;
  receivedAtUtc: string;
  operationalDate: string;
  correlationId: string;
  orphanReason: string;
  candidateReferences?: string | null;
  resolutionStatus: string;
  resolvedReference?: string | null;
  resolvedAtUtc?: string | null;
  version: string;
}

export interface AchResponseReprocessModel {
  id: number;
  achResponseId: string;
  attemptNumber: number;
  status: string;
  reason: string;
  correlationId: string;
  requestedAtUtc: string;
  commandId: string;
}

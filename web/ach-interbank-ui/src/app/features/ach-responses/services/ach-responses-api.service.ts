import { Injectable, inject } from '@angular/core';
import { ApiService } from '../../../core/services/api.service';
import {
  AchResponseDashboardRequest,
  AchResponseDashboardResponse,
  AchResponseDetailResponse,
  AchResponseListItemResponse,
  AchResponseNotificationAttemptResponse,
  AchResponseSearchRequest,
  AchResponseStatusMappingResponse,
  AchResponseStatusMappingsFilter,
  NotificarRespuestaAchRequest,
  NotificarRespuestaAchResponse,
  PagedResponse,
  ProcesarRespuestaAchRequest,
  ProcesarRespuestaAchResponse
} from '../models/ach-responses.models';

@Injectable({ providedIn: 'root' })
export class AchResponsesApiService {
  private readonly api = inject(ApiService);

  process(request: ProcesarRespuestaAchRequest) {
    return this.api.post<ProcesarRespuestaAchResponse>('api/ach/responses/process', request);
  }

  sendNotification(request: NotificarRespuestaAchRequest) {
    return this.api.post<NotificarRespuestaAchResponse>('api/ach/responses/notifications/send', request);
  }

  search(request: AchResponseSearchRequest) {
    const params: Record<string, string | number | boolean> = {};

    this.addParam(params, 'fechaDesde', request.fechaDesde);
    this.addParam(params, 'fechaHasta', request.fechaHasta);
    this.addParam(params, 'tipoRespuesta', request.tipoRespuesta);
    this.addParam(params, 'idTransaccion', request.idTransaccion);
    this.addParam(params, 'codigoCamaraCompensacion', request.codigoCamaraCompensacion);
    this.addParam(params, 'codigoEntidadOrigen', request.codigoEntidadOrigen);
    this.addParam(params, 'codigoEntidadDestino', request.codigoEntidadDestino);
    this.addParam(params, 'codigoEstadoExterno', request.codigoEstadoExterno);
    this.addParam(params, 'estadoProcesamiento', request.estadoProcesamiento);
    this.addParam(params, 'correlationId', request.correlationId);
    this.addParam(params, 'pageNumber', request.pageNumber);
    this.addParam(params, 'pageSize', request.pageSize);

    return this.api.get<PagedResponse<AchResponseListItemResponse>>('api/ach/responses', { params });
  }

  getDashboard(request: AchResponseDashboardRequest) {
    const params: Record<string, string | number | boolean> = {};

    this.addParam(params, 'fechaDesde', request.fechaDesde);
    this.addParam(params, 'fechaHasta', request.fechaHasta);
    this.addParam(params, 'tipoRespuesta', request.tipoRespuesta);

    return this.api.get<AchResponseDashboardResponse>('api/ach/responses/dashboard', { params });
  }

  getDetail(id: string) {
    return this.api.get<AchResponseDetailResponse>(`api/ach/responses/${encodeURIComponent(id)}`);
  }

  getAttempts(id: string) {
    return this.api.get<AchResponseNotificationAttemptResponse[]>(
      `api/ach/responses/${encodeURIComponent(id)}/notification-attempts`
    );
  }

  getStatusMappings(filters?: AchResponseStatusMappingsFilter) {
    const params: Record<string, string | number | boolean> = {};

    this.addParam(params, 'codigoCamaraCompensacion', filters?.codigoCamaraCompensacion);
    this.addParam(params, 'tipoRespuesta', filters?.tipoRespuesta);
    this.addParam(params, 'activo', filters?.activo);

    return this.api.get<AchResponseStatusMappingResponse[]>('api/ach/response-status-mappings', { params });
  }

  private addParam(
    params: Record<string, string | number | boolean>,
    key: string,
    value: string | number | boolean | null | undefined
  ): void {
    if (value === null || value === undefined) {
      return;
    }

    if (typeof value === 'string' && value.trim().length === 0) {
      return;
    }

    params[key] = value;
  }
}

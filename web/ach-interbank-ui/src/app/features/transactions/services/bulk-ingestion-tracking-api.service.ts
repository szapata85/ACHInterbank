import { Injectable, inject } from '@angular/core';
import { catchError, throwError } from 'rxjs';
import { ApiService } from '../../../core/services/api.service';
import {
  BulkBatchItemsPageDto,
  BulkBatchProcessingSummaryDto,
  BulkBatchStatusDto,
  BulkFileUploadResponse,
  BulkIngestionItemStatus,
  RetryBatchRequest,
  RetryBatchResponse
} from '../transactions.models';

@Injectable({ providedIn: 'root' })
export class BulkIngestionTrackingApiService {
  private readonly api = inject(ApiService);

  upload(file: File, batchReference?: string, clientRequestId?: string) {
    const formData = new FormData();
    formData.append('file', file);

    if (batchReference?.trim()) {
      formData.append('batchReference', batchReference.trim());
    }

    if (clientRequestId?.trim()) {
      formData.append('clientRequestId', clientRequestId.trim());
    }

    return this.api.post<BulkFileUploadResponse>('transactions/bulk-ingestion/upload', formData).pipe(
      catchError((error) => this.mapError(error, 'No fue posible cargar el archivo de lote.'))
    );
  }

  getBatch(batchId: string) {
    return this.api.get<BulkBatchStatusDto>(`transactions/bulk-ingestion/${batchId}`).pipe(
      catchError((error) => this.mapError(error, 'No fue posible consultar el lote.'))
    );
  }

  getBatchItems(batchId: string, page: number, pageSize: number, status?: BulkIngestionItemStatus | null) {
    const params: Record<string, string | number> = { page, pageSize };
    if (status) {
      params.status = status;
    }

    return this.api.get<BulkBatchItemsPageDto>(`transactions/bulk-ingestion/${batchId}/items`, { params }).pipe(
      catchError((error) => this.mapError(error, 'No fue posible consultar los ítems del lote.'))
    );
  }

  getSummary(batchId: string) {
    return this.api.get<BulkBatchProcessingSummaryDto>(`transactions/bulk-ingestion/${batchId}/summary`).pipe(
      catchError((error) => this.mapError(error, 'No fue posible consultar el resumen del lote.'))
    );
  }

  retry(batchId: string, request: RetryBatchRequest) {
    return this.api.post<RetryBatchResponse>(`transactions/bulk-ingestion/${batchId}/retry`, request).pipe(
      catchError((error) => this.mapError(error, 'No fue posible ejecutar el reintento del lote.'))
    );
  }

  private mapError(error: any, fallback: string) {
    if (error.status === 400) {
      return throwError(() => new Error(error.error?.message ?? 'La solicitud no cumple validaciones de negocio.'));
    }

    if (error.status === 401) {
      return throwError(() => new Error('Sesión expirada. Inicie sesión nuevamente.'));
    }

    if (error.status === 404) {
      return throwError(() => new Error(error.error?.message ?? 'No se encontró el recurso solicitado.'));
    }

    return throwError(() => new Error(error.error?.message ?? fallback));
  }
}

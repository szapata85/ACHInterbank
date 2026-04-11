import { Injectable, inject } from '@angular/core';
import { catchError, throwError } from 'rxjs';
import { ApiService } from '../../../core/services/api.service';
import { BulkIngestionRequest, BulkIngestionResponse } from '../transactions.models';

@Injectable({ providedIn: 'root' })
export class BulkIngestionApiService {
  private readonly api = inject(ApiService);

  submit(request: BulkIngestionRequest) {
    return this.api.post<BulkIngestionResponse>('transactions/bulk/submit', request).pipe(
      catchError((error) => {
        if (error.status === 400) {
          return throwError(() => new Error(error.error?.message ?? 'Solicitud de ingestión masiva inválida.'));
        }

        if (error.status === 401) {
          return throwError(() => new Error('Sesión expirada. Inicie sesión nuevamente.'));
        }

        return throwError(() => new Error(error.error?.message ?? 'No fue posible enviar la ingestión masiva.'));
      })
    );
  }
}

import { Injectable, inject } from '@angular/core';
import { catchError, map, of, throwError } from 'rxjs';
import { ApiService } from '../../core/services/api.service';
import { DestinationInstitution, TransactionDraft, TransactionResponse } from './transactions.models';

@Injectable({ providedIn: 'root' })
export class TransactionsService {
  private readonly api = inject(ApiService);

  registerTransaction(payload: TransactionDraft) {
    return this.api.post<TransactionResponse>('transactions', payload).pipe(
      catchError((error) => {
        if (error.status === 400) {
          return throwError(() => new Error(error.error?.message ?? 'Solicitud inválida'));
        }
        if (error.status === 401) {
          return throwError(() => new Error('No autorizado, inicie sesión nuevamente.'));
        }
        return throwError(() => new Error('No fue posible crear la transacción.'));
      })
    );
  }

  getDestinationInstitutions() {
    return this.api
      .get<Array<{ id: number; name: string; routingNumber: string; status: number }>>('financial-institutions', {
        params: { includeInactive: false }
      })
      .pipe(
        map((institutions) =>
          (institutions ?? [])
            .filter((institution) => institution.status === 1)
            .map((institution) => ({
              id: institution.id,
              name: institution.name,
              routingNumber: institution.routingNumber
            }))
        ),
        catchError(() => of<DestinationInstitution[]>([]))
      );
  }
}

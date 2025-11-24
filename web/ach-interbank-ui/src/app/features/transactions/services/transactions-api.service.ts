import { Injectable, inject } from '@angular/core';
import { catchError, throwError } from 'rxjs';
import { ApiService } from '../../../core/services/api.service';
import { TransactionTypeEnum } from '../transactions.types';
import { TransactionDraft, TransactionResponse } from '../transactions.models';

@Injectable({ providedIn: 'root' })
export class TransactionsApiService {
  private readonly api = inject(ApiService);

  createTransaction(payload: TransactionDraft) {
    const sanitized: TransactionDraft = {
      ...payload,
      amount: Number(payload.amount),
      destinationInstitutionId: Number(payload.destinationInstitutionId),
      type: Number(payload.type) as TransactionTypeEnum
    };

    return this.api.post<TransactionResponse>('transactions', sanitized).pipe(
      catchError((error) => {
        if (error.status === 400) {
          return throwError(() => new Error(error.error?.message ?? 'Solicitud inválida'));
        }
        if (error.status === 401) {
          return throwError(() => new Error('Sesión expirada. Inicie sesión nuevamente.'));
        }
        return throwError(() => new Error('No fue posible crear la transacción.'));
      })
    );
  }

  getDestinationInstitutions() {
    return this.api.get<Array<{ id: number; name: string; routingNumber: string; status: number }>>(
      'financial-institutions',
      { params: { includeInactive: false } }
    );
  }
}

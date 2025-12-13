import { Injectable, inject } from '@angular/core';
import { catchError, map, throwError } from 'rxjs';
import { ApiService } from '../../../core/services/api.service';
import { TransactionTypeEnum } from '../transactions.types';
import { TransactionDraft, TransactionListFilter, TransactionListItem, TransactionResponse } from '../transactions.models';

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

  getAll(filter?: TransactionListFilter) {
    const params: Record<string, string | number> = {};

    if (filter?.achCycleId !== undefined && filter?.achCycleId !== null) {
      params.achCycleId = filter.achCycleId;
    }

    if (filter?.effectiveDate) {
      params.effectiveDate = filter.effectiveDate;
    }

    if (filter?.clearingHouseId !== undefined && filter?.clearingHouseId !== null) {
      params.clearingHouseId = filter.clearingHouseId;
    }

    return this.api.get<TransactionListItem[]>('transactions', { params }).pipe(
      map((items) => (items ?? []).map((item) => ({ ...item, amount: Number(item.amount) })))
    );
  }
}

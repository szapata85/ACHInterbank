import { Injectable, inject } from '@angular/core';
import { catchError, throwError } from 'rxjs';
import { ApiService } from '../../../core/services/api.service';
import { TransactionTypeEnum } from '../transactions.types';
import { CreateTransactionRequest, TransactionDraft, TransactionResponse } from '../transactions.models';

@Injectable({ providedIn: 'root' })
export class TransactionsApiService {
  private readonly api = inject(ApiService);

  createTransaction(payload: CreateTransactionRequest) {
    const sanitized: CreateTransactionRequest = {
      ...payload,
      amount: Number(payload.amount),
      destinationInstitutionId: Number(payload.destinationInstitutionId),
      type: Number(payload.type) as TransactionTypeEnum,
      reference: payload.reference?.trim() ?? '',
      sourceAccountNumber: payload.sourceAccountNumber?.trim() ?? '',
      destinationAccountNumber: payload.destinationAccountNumber?.trim() ?? '',
      companyName: payload.companyName?.trim(),
      companyIdentification: payload.companyIdentification?.trim(),
      companyEntryDescription: payload.companyEntryDescription?.trim(),
      addendas: payload.addendas
        ?.map((item) => ({
          addendaType: item.addendaType.trim().toUpperCase(),
          information: item.information.trim()
        }))
        .filter((item) => item.addendaType && item.information)
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
}

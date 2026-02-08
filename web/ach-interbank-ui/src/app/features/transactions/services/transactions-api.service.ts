import { Injectable, inject } from '@angular/core';
import { catchError, map, throwError } from 'rxjs';
import { ApiService } from '../../../core/services/api.service';
import { AccountTypeEnum, TransactionTypeEnum } from '../transactions.types';
import { ActiveThirdPartyAccount, TransactionDraft, TransactionListFilter, TransactionListItem, TransactionResponse } from '../transactions.models';

interface PagedResponse<T> {
  items: T[];
  total: number;
  page: number;
  pageSize: number;
}

interface CustomerThirdPartyApiItem {
  id: number;
  destinationInstitutionId: number;
  destinationInstitutionName: string;
  destinationAccountNumber: string;
  recipientIdNumber: string;
}

@Injectable({ providedIn: 'root' })
export class TransactionsApiService {
  private readonly api = inject(ApiService);

  createTransaction(payload: TransactionDraft) {
    const sanitized: TransactionDraft = {
      ...payload,
      amount: Number(payload.amount),
      destinationInstitutionId: Number(payload.destinationInstitutionId),
      type: Number(payload.type) as TransactionTypeEnum,
      accountType: Number(payload.accountType) as AccountTypeEnum,
      isPrenotification: Boolean(payload.isPrenotification),
      requiresIdentityValidation: Boolean(payload.requiresIdentityValidation),
      recipientIdNumber: payload.recipientIdNumber?.trim() || undefined
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



  getActiveThirdParties(destinationInstitutionId?: number | null) {
    const params: Record<string, string | number> = {
      status: 'Active',
      page: 1,
      pageSize: 500
    };

    if (destinationInstitutionId && destinationInstitutionId > 0) {
      params.destinationInstitutionId = destinationInstitutionId;
    }

    return this.api
      .get<PagedResponse<CustomerThirdPartyApiItem>>('api/customer-third-parties', { params })
      .pipe(
        map((response) =>
          (response?.items ?? []).map(
            (item): ActiveThirdPartyAccount => ({
              id: item.id,
              destinationInstitutionId: item.destinationInstitutionId,
              destinationInstitutionName: item.destinationInstitutionName,
              destinationAccountNumber: item.destinationAccountNumber,
              recipientIdNumber: item.recipientIdNumber
            })
          )
        )
      );
  }

  getAll(filter?: TransactionListFilter) {
    const params: Record<string, string | number> = {};

    if (filter?.achCycleId !== undefined && filter?.achCycleId !== null) {
      params.achCycleId = filter.achCycleId;
    }

    if (filter?.achCycleName !== undefined && filter?.achCycleName !== null) {
      params.achCycleName = filter.achCycleName;
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

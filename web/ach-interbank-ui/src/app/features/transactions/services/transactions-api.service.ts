import { Injectable, inject } from '@angular/core';
import { catchError, map, throwError } from 'rxjs';
import { ApiService } from '../../../core/services/api.service';
import { AccountTypeEnum, TransactionTypeEnum } from '../transactions.types';
import { ActiveThirdPartyAccount, BulkAchTransactionRequest, BulkAchTransactionResponse, CompanyEntryDescriptionOption, TransactionDraft, TransactionIntegrationResult, TransactionListFilter, TransactionListItem, TransactionPolicyPreview, TransactionResponse } from '../transactions.models';

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
  private readonly basePath = 'api/transactions';

  getCompanyEntryDescriptions() {
    return this.api.get<CompanyEntryDescriptionOption[]>(`${this.basePath}/company-entry-descriptions`);
  }

  previewPolicy(payload: TransactionDraft) {
    const params: Record<string, string | number | boolean> = {
      amount: Number(payload.amount),
      transactionExternalId: payload.transactionExternalId?.trim() || '',
      type: Number(payload.type),
      accountType: Number(payload.accountType),
      isPrenotification: Boolean(payload.isPrenotification),
      destinationInstitutionId: Number(payload.destinationInstitutionId),
      sourceAccountNumber: payload.sourceAccountNumber?.trim() || '',
      destinationAccountNumber: payload.destinationAccountNumber?.trim() || '',
      companyIdentification: payload.companyIdentification?.trim() || '',
      recipientIdNumber: payload.recipientIdNumber?.trim() || ''
    };

    return this.api.get<TransactionPolicyPreview>(`${this.basePath}/policies/preview`, { params });
  }

  createTransaction(payload: TransactionDraft) {
    const sanitized: TransactionDraft = {
      ...payload,
      amount: Number(payload.amount),
      destinationInstitutionId: Number(payload.destinationInstitutionId),
      type: Number(payload.type) as TransactionTypeEnum,
      accountType: Number(payload.accountType) as AccountTypeEnum,
      isPrenotification: Boolean(payload.isPrenotification),
      requiresIdentityValidation: Boolean(payload.requiresIdentityValidation),
      transactionExternalId: payload.transactionExternalId?.trim() || '',
      recipientIdNumber: payload.recipientIdNumber?.trim() || undefined,
      recipientName: payload.recipientName?.trim() || undefined
    };

    return this.api.post<TransactionResponse>(this.basePath, sanitized).pipe(
      catchError((error) => {
        if (error.status === 400) {
          return throwError(() => new Error(this.extractErrorMessage(error, 'Solicitud inválida')));
        }
        if (error.status === 404) {
          return throwError(() => new Error(this.extractErrorMessage(error, 'No se encontró el endpoint de creación de transacciones.')));
        }
        if (error.status === 401) {
          return throwError(() => new Error('Sesión expirada. Inicie sesión nuevamente.'));
        }
        return throwError(() => new Error(this.extractErrorMessage(error, 'No fue posible crear la transacción.')));
      })
    );
  }


  createBulkTransaction(payload: BulkAchTransactionRequest) {
    return this.api.post<BulkAchTransactionResponse>(`${this.basePath}/bulk`, payload).pipe(
      catchError((error) => {
        if (error.status === 400) {
          return throwError(() => new Error(error.error?.message ?? 'El lote no cumple las validaciones requeridas.'));
        }
        if (error.status === 401) {
          return throwError(() => new Error('Sesión expirada. Inicie sesión nuevamente.'));
        }
        return throwError(() => new Error(error.error?.message ?? 'No fue posible procesar el lote masivo.'));
      })
    );
  }

  getActiveThirdParties(sourceAccountNumber: string, destinationInstitutionId?: number | null) {
    const params: Record<string, string | number> = {
      status: 'Active',
      page: 1,
      pageSize: 500
    };

    params.sourceAccountNumber = sourceAccountNumber;

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

    return this.api.get<TransactionListItem[]>(this.basePath, { params }).pipe(
      map((items) => (items ?? []).map((item) => ({ ...item, amount: Number(item.amount) })))
    );
  }

  getIntegrationResult(transactionId: number) {
    return this.api.get<TransactionIntegrationResult>(`${this.basePath}/${transactionId}/integration-result`);
  }

  private extractErrorMessage(error: any, fallback: string): string {
    const nestedErrors = error?.error?.errors;
    if (nestedErrors && typeof nestedErrors === 'object') {
      const firstMessage = Object.values(nestedErrors)
        .flatMap((value) => Array.isArray(value) ? value : [String(value)])
        .find((value) => String(value).trim().length > 0);
      if (firstMessage) {
        return String(firstMessage);
      }
    }

    return error?.error?.message
      ?? error?.error?.Message
      ?? error?.message
      ?? fallback;
  }
}

import { Injectable, inject } from '@angular/core';
import { Observable, map } from 'rxjs';
import { ApiService } from '../../core/services/api.service';
import {
  CreateTransactionPolicyVersionRequest,
  PrenotificationMode,
  TransactionPolicy,
  TransactionPolicyPreview,
  UpdateTransactionPolicyMetadataRequest
} from './transaction-policies.models';
import { TransactionTypeEnum } from '../transactions/transactions.types';

type ApiPrenotificationMode = 1 | 2 | 3 | PrenotificationMode;
type ApiTransactionPolicy = Omit<TransactionPolicy, 'prenotificationMode'> & {
  prenotificationMode: ApiPrenotificationMode;
};
type ApiTransactionPolicyPreview = Omit<TransactionPolicyPreview, 'prenotificationMode'> & {
  prenotificationMode: ApiPrenotificationMode;
};

@Injectable({ providedIn: 'root' })
export class TransactionPoliciesService {
  private readonly api = inject(ApiService);
  private path(clearingHouseId: number): string { return `api/clearing-houses/${clearingHouseId}/transaction-policies`; }

  list(clearingHouseId: number): Observable<TransactionPolicy[]> {
    return this.api.get<ApiTransactionPolicy[]>(this.path(clearingHouseId)).pipe(
      map(policies => policies.map(policy => this.fromApi(policy)))
    );
  }

  current(clearingHouseId: number, transactionType: TransactionTypeEnum, effectiveAt: string): Observable<TransactionPolicy> {
    return this.api.get<ApiTransactionPolicy>(`${this.path(clearingHouseId)}/current`, { params: { transactionType, effectiveAt } }).pipe(
      map(policy => this.fromApi(policy))
    );
  }

  get(clearingHouseId: number, id: number): Observable<TransactionPolicy> {
    return this.api.get<ApiTransactionPolicy>(`${this.path(clearingHouseId)}/${id}`).pipe(
      map(policy => this.fromApi(policy))
    );
  }

  create(clearingHouseId: number, request: CreateTransactionPolicyVersionRequest): Observable<TransactionPolicy> {
    return this.api.post<ApiTransactionPolicy>(this.path(clearingHouseId), {
      ...request,
      prenotificationMode: this.toApiMode(request.prenotificationMode)
    }).pipe(map(policy => this.fromApi(policy)));
  }

  updateMetadata(clearingHouseId: number, id: number, request: UpdateTransactionPolicyMetadataRequest): Observable<TransactionPolicy> {
    return this.api.patch<ApiTransactionPolicy>(`${this.path(clearingHouseId)}/${id}/metadata`, request).pipe(
      map(policy => this.fromApi(policy))
    );
  }

  close(clearingHouseId: number, id: number, effectiveTo: string): Observable<TransactionPolicy> {
    return this.api.post<ApiTransactionPolicy>(`${this.path(clearingHouseId)}/${id}/close`, { effectiveTo }).pipe(
      map(policy => this.fromApi(policy))
    );
  }

  activate(clearingHouseId: number, id: number): Observable<TransactionPolicy> {
    return this.api.post<ApiTransactionPolicy>(`${this.path(clearingHouseId)}/${id}/activate`, {}).pipe(
      map(policy => this.fromApi(policy))
    );
  }

  preview(clearingHouseId: number, transactionType: TransactionTypeEnum, effectiveEntryDate: string): Observable<TransactionPolicyPreview> {
    return this.api.post<ApiTransactionPolicyPreview>(`${this.path(clearingHouseId)}/preview`, {
      transactionType,
      effectiveEntryDate,
      appliesToNachaExport: true
    }).pipe(map(preview => ({
      ...preview,
      prenotificationMode: this.fromApiMode(preview.prenotificationMode)
    })));
  }

  private fromApi(policy: ApiTransactionPolicy): TransactionPolicy {
    return {
      ...policy,
      prenotificationMode: this.fromApiMode(policy.prenotificationMode)
    };
  }

  private fromApiMode(mode: ApiPrenotificationMode): PrenotificationMode {
    if (mode === 1 || mode === 'Mandatory') {
      return 'Mandatory';
    }
    if (mode === 2 || mode === 'Optional') {
      return 'Optional';
    }
    return 'NotApplicable';
  }

  private toApiMode(mode: PrenotificationMode): 1 | 2 | 3 {
    return mode === 'Mandatory' ? 1 : mode === 'Optional' ? 2 : 3;
  }
}

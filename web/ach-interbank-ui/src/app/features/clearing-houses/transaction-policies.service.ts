import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { ApiService } from '../../core/services/api.service';
import { CreateTransactionPolicyVersionRequest, TransactionPolicy, TransactionPolicyPreview, UpdateTransactionPolicyMetadataRequest } from './transaction-policies.models';
import { TransactionTypeEnum } from '../transactions/transactions.types';

@Injectable({ providedIn: 'root' })
export class TransactionPoliciesService {
  private readonly api = inject(ApiService);
  private path(clearingHouseId: number): string { return `api/clearing-houses/${clearingHouseId}/transaction-policies`; }

  list(clearingHouseId: number): Observable<TransactionPolicy[]> { return this.api.get<TransactionPolicy[]>(this.path(clearingHouseId)); }
  current(clearingHouseId: number, transactionType: TransactionTypeEnum, effectiveAt: string): Observable<TransactionPolicy> {
    return this.api.get<TransactionPolicy>(`${this.path(clearingHouseId)}/current`, { params: { transactionType, effectiveAt } });
  }
  get(clearingHouseId: number, id: number): Observable<TransactionPolicy> { return this.api.get<TransactionPolicy>(`${this.path(clearingHouseId)}/${id}`); }
  create(clearingHouseId: number, request: CreateTransactionPolicyVersionRequest): Observable<TransactionPolicy> { return this.api.post<TransactionPolicy>(this.path(clearingHouseId), request); }
  updateMetadata(clearingHouseId: number, id: number, request: UpdateTransactionPolicyMetadataRequest): Observable<TransactionPolicy> { return this.api.patch<TransactionPolicy>(`${this.path(clearingHouseId)}/${id}/metadata`, request); }
  close(clearingHouseId: number, id: number, effectiveTo: string): Observable<TransactionPolicy> { return this.api.post<TransactionPolicy>(`${this.path(clearingHouseId)}/${id}/close`, { effectiveTo }); }
  activate(clearingHouseId: number, id: number): Observable<TransactionPolicy> { return this.api.post<TransactionPolicy>(`${this.path(clearingHouseId)}/${id}/activate`, {}); }
  preview(clearingHouseId: number, transactionType: TransactionTypeEnum, effectiveEntryDate: string): Observable<TransactionPolicyPreview> {
    return this.api.post<TransactionPolicyPreview>(`${this.path(clearingHouseId)}/preview`, { transactionType, effectiveEntryDate, appliesToNachaExport: true });
  }
}

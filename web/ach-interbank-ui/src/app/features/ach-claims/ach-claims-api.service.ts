import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { ApiService } from '../../core/services/api.service';
import {
  ClaimCaseDetail,
  ClaimCaseListItem,
  ClaimCaseQuery,
  ClaimCaseQueryResult,
  CreateDev14ClaimCaseRequest,
  ExpireClaimCaseRequest,
  RecordClaimProgressRequest,
  ResolveClaimCaseRequest
} from './ach-claims.models';

@Injectable({ providedIn: 'root' })
export class AchClaimsApiService {
  private readonly api = inject(ApiService);
  private readonly basePath = 'api/ach-claims';

  query(query: ClaimCaseQuery): Observable<ClaimCaseQueryResult> {
    const params: Record<string, string | number | boolean> = {};
    Object.entries(query).forEach(([key, value]) => {
      if (value !== undefined && value !== null && value !== '') {
        params[key] = value as string | number | boolean;
      }
    });
    return this.api.get<ClaimCaseQueryResult>(this.basePath, {
      params,
      headers: { 'X-Skip-Loading': 'true' }
    });
  }

  getById(id: number): Observable<ClaimCaseDetail> {
    return this.api.get<ClaimCaseDetail>(`${this.basePath}/${id}`, {
      headers: { 'X-Skip-Loading': 'true' }
    });
  }

  getByTransaction(transactionId: number): Observable<ClaimCaseListItem[]> {
    return this.api.get<ClaimCaseListItem[]>(`${this.basePath}/by-transaction/${transactionId}`, {
      headers: { 'X-Skip-Loading': 'true' }
    });
  }

  createDev14(request: CreateDev14ClaimCaseRequest, idempotencyKey: string): Observable<ClaimCaseDetail> {
    return this.api.post<ClaimCaseDetail>(`${this.basePath}/dev14`, request, {
      headers: { 'Idempotency-Key': idempotencyKey }
    });
  }

  recordProgress(id: number, request: RecordClaimProgressRequest, idempotencyKey: string): Observable<ClaimCaseDetail> {
    return this.api.post<ClaimCaseDetail>(`${this.basePath}/${id}/progress`, request, {
      headers: { 'Idempotency-Key': idempotencyKey }
    });
  }

  resolve(id: number, request: ResolveClaimCaseRequest, idempotencyKey: string): Observable<ClaimCaseDetail> {
    return this.api.post<ClaimCaseDetail>(`${this.basePath}/${id}/resolve`, request, {
      headers: { 'Idempotency-Key': idempotencyKey }
    });
  }

  expire(id: number, request: ExpireClaimCaseRequest, idempotencyKey: string): Observable<ClaimCaseDetail> {
    return this.api.post<ClaimCaseDetail>(`${this.basePath}/${id}/expire`, request, {
      headers: { 'Idempotency-Key': idempotencyKey }
    });
  }
}

import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { ApiService } from '../../core/services/api.service';
import {
  CfaOperationalTransactionRestriction,
  CfaOperationalTransactionRestrictionFilter,
  ChangeCfaOperationalTransactionRestrictionStatusRequest,
  SaveCfaOperationalTransactionRestrictionRequest,
  UpdateCfaOperationalTransactionRestrictionRequest
} from './operational-restrictions.models';

@Injectable({ providedIn: 'root' })
export class OperationalRestrictionsService {
  private readonly api = inject(ApiService);
  private readonly path = 'api/cfa-operational-transaction-restrictions';

  list(filters: CfaOperationalTransactionRestrictionFilter = {}): Observable<CfaOperationalTransactionRestriction[]> {
    const params: Record<string, string | number | boolean> = {};
    if (filters.clearingHouseCode) params['clearingHouseCode'] = filters.clearingHouseCode;
    if (filters.policyVersion) params['policyVersion'] = filters.policyVersion;
    if (filters.clearingHouseCycleConfigId != null) params['clearingHouseCycleConfigId'] = filters.clearingHouseCycleConfigId;
    if (filters.transactionType != null) params['transactionType'] = filters.transactionType;
    if (filters.isPrenotification != null) params['isPrenotification'] = filters.isPrenotification;
    if (filters.isActive != null) params['isActive'] = filters.isActive;
    if (filters.effectiveAt) params['effectiveAt'] = filters.effectiveAt;
    return this.api.get<CfaOperationalTransactionRestriction[]>(this.path, { params });
  }

  get(id: number): Observable<CfaOperationalTransactionRestriction> {
    return this.api.get<CfaOperationalTransactionRestriction>(`${this.path}/${id}`);
  }

  create(request: SaveCfaOperationalTransactionRestrictionRequest): Observable<CfaOperationalTransactionRestriction> {
    return this.api.post<CfaOperationalTransactionRestriction>(this.path, request);
  }

  update(id: number, request: UpdateCfaOperationalTransactionRestrictionRequest): Observable<CfaOperationalTransactionRestriction> {
    return this.api.put<CfaOperationalTransactionRestriction>(`${this.path}/${id}`, request);
  }

  activate(id: number, request: ChangeCfaOperationalTransactionRestrictionStatusRequest): Observable<CfaOperationalTransactionRestriction> {
    return this.api.patch<CfaOperationalTransactionRestriction>(`${this.path}/${id}/activate`, request);
  }

  deactivate(id: number, request: ChangeCfaOperationalTransactionRestrictionStatusRequest): Observable<CfaOperationalTransactionRestriction> {
    return this.api.patch<CfaOperationalTransactionRestriction>(`${this.path}/${id}/deactivate`, request);
  }
}

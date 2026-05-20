import { Injectable, inject } from '@angular/core';
import { map } from 'rxjs';
import { ApiService } from '../../../core/services/api.service';
import {
  ClearingHouseTransactionRuleItem,
  SaveClearingHouseTransactionRuleRequest,
  TransactionNature,
  TransactionPrerequisitePreviewRequest,
  TransactionPrerequisitePreviewResponse
} from '../transactions.models';

@Injectable({ providedIn: 'root' })
export class ClearingHouseTransactionRulesApiService {
  private readonly api = inject(ApiService);
  private readonly basePath = 'api/clearing-house-transaction-rules';

  getRules(filters: { clearingHouseId?: number | null; transactionNature?: TransactionNature | 'all'; includeInactive?: boolean }) {
    const params: Record<string, string | number | boolean> = {
      includeInactive: filters.includeInactive ?? false
    };

    if (filters.clearingHouseId) {
      params.clearingHouseId = filters.clearingHouseId;
    }

    if (filters.transactionNature && filters.transactionNature !== 'all') {
      params.transactionNature = filters.transactionNature;
    }

    return this.api.get<ClearingHouseTransactionRuleItem[]>(this.basePath, { params }).pipe(map((items) => items ?? []));
  }

  create(payload: SaveClearingHouseTransactionRuleRequest) {
    return this.api.post<ClearingHouseTransactionRuleItem>(this.basePath, payload);
  }

  update(id: number, payload: SaveClearingHouseTransactionRuleRequest) {
    return this.api.put<ClearingHouseTransactionRuleItem>(`${this.basePath}/${id}`, payload);
  }

  activate(id: number) {
    return this.api.patch<ClearingHouseTransactionRuleItem>(`${this.basePath}/${id}/activate`, {});
  }

  deactivate(id: number) {
    return this.api.patch<ClearingHouseTransactionRuleItem>(`${this.basePath}/${id}/deactivate`, {});
  }

  preview(payload: TransactionPrerequisitePreviewRequest) {
    return this.api.post<TransactionPrerequisitePreviewResponse>('api/transaction-prerequisite-policy/preview', payload);
  }
}

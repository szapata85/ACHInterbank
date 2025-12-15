import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { ApiService } from '../../../core/services/api.service';
import { DestinationInstitution } from '../../transactions/transactions.models';
import { FinancialInstitutionStatusEnum } from '../../transactions/transactions.types';

export type FinancialInstitutionPayload = Omit<DestinationInstitution, 'id'>;

@Injectable({ providedIn: 'root' })
export class FinancialInstitutionAdminService {
  private readonly api = inject(ApiService);
  private readonly basePath = 'financial-institutions';

  list(includeInactive = true): Observable<DestinationInstitution[]> {
    return this.api.get<DestinationInstitution[]>(this.basePath, {
      params: { includeInactive }
    });
  }

  create(payload: FinancialInstitutionPayload): Observable<DestinationInstitution> {
    return this.api.post<DestinationInstitution>(this.basePath, payload);
  }

  update(id: number, payload: FinancialInstitutionPayload): Observable<DestinationInstitution> {
    return this.api.put<DestinationInstitution>(`${this.basePath}/${id}`, { ...payload, id });
  }

  setStatus(id: number, status: FinancialInstitutionStatusEnum): Observable<void> {
    return this.api.patch<void>(`${this.basePath}/${id}/status`, status);
  }
}

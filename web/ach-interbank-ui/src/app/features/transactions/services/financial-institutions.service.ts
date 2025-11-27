import { Injectable, inject } from '@angular/core';
import { map, Observable } from 'rxjs';
import { ApiService } from '../../../core/services/api.service';
import { FinancialInstitutionStatusEnum } from '../transactions.types';

export interface FinancialInstitution {
  id: number;
  name: string;
  routingNumber: string;
  transitCode: string;
  checkDigit: string;
  isDefaultSource: boolean;
  status: FinancialInstitutionStatusEnum;
}

type FinancialInstitutionsResponse = FinancialInstitution[] | { data?: FinancialInstitution[] } | null | undefined;

@Injectable({ providedIn: 'root' })
export class FinancialInstitutionsService {
  private readonly api = inject(ApiService);

  getFinancialInstitutions(includeInactive = false): Observable<FinancialInstitution[]> {
    return this.api
      .get<FinancialInstitutionsResponse>('financial-institutions', {
        params: { includeInactive }
      })
      .pipe(
        map((response) => {
          if (!response) {
            return [];
          }

          if (Array.isArray(response)) {
            return response;
          }

          const nested = response.data;
          return Array.isArray(nested) ? nested : [];
        })
      );
  }
}

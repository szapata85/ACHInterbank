import { Injectable, inject } from '@angular/core';
import { map, Observable } from 'rxjs';

import { ApiService } from '../../../core/services/api.service';
import { FinancialInstitution } from '../transactions.models';

type FinancialInstitutionApiResponse = FinancialInstitution[] | { data?: FinancialInstitution[] };

@Injectable({ providedIn: 'root' })
export class FinancialInstitutionsService {
  private readonly api = inject(ApiService);

  getFinancialInstitutions(): Observable<FinancialInstitution[]> {
    return this.api
      .get<FinancialInstitutionApiResponse>('financial-institutions', {
        params: { includeInactive: false }
      })
      .pipe(
        map((response) => {
          if (Array.isArray(response)) {
            return response;
          }

          if (response?.data && Array.isArray(response.data)) {
            return response.data;
          }

          return [];
        })
      );
  }
}

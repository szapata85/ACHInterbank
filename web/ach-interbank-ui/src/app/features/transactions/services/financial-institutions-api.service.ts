import { Injectable, inject } from '@angular/core';
import { ApiService } from '../../../core/services/api.service';
import { DestinationInstitution } from '../transactions.models';

@Injectable({ providedIn: 'root' })
export class FinancialInstitutionsApiService {
  private readonly api = inject(ApiService);

  getAll(includeInactive = false) {
    return this.api.get<DestinationInstitution[]>('financial-institutions', {
      params: { includeInactive }
    });
  }
}

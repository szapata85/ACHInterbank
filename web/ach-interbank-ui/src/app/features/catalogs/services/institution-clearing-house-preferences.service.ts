import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { ApiService } from '../../../core/services/api.service';
import { InstitutionClearingHousePreference } from '../models/institution-clearing-house-preference.model';

@Injectable({ providedIn: 'root' })
export class InstitutionClearingHousePreferencesService {
  private readonly api = inject(ApiService);
  private readonly basePath = 'institution-clearing-house-preferences';

  list(): Observable<InstitutionClearingHousePreference[]> {
    return this.api.get<InstitutionClearingHousePreference[]>(this.basePath);
  }

  update(
    id: number,
    preference: Pick<InstitutionClearingHousePreference, 'id' | 'priority' | 'isDefault'>
  ): Observable<InstitutionClearingHousePreference> {
    return this.api.put<InstitutionClearingHousePreference>(`${this.basePath}/${id}`, preference);
  }
}

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

  create(
    preference: Pick<
      InstitutionClearingHousePreference,
      'financialInstitutionId' | 'clearingHouseId' | 'priority' | 'isDefault' | 'isActive'
    >
  ): Observable<InstitutionClearingHousePreference> {
    return this.api.post<InstitutionClearingHousePreference>(this.basePath, preference);
  }

  update(
    id: number,
    preference: Pick<InstitutionClearingHousePreference, 'id' | 'priority' | 'isDefault' | 'isActive'>
  ): Observable<InstitutionClearingHousePreference> {
    return this.api.put<InstitutionClearingHousePreference>(`${this.basePath}/${id}`, preference);
  }

  delete(id: number): Observable<void> {
    return this.api.delete<void>(`${this.basePath}/${id}`);
  }
}

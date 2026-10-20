import { Injectable, inject } from '@angular/core';
import { ApiService } from './api.service';
import {
  AchColombiaOperationalSchedule,
  AchColombiaOperationalScheduleAdministrationRow,
  SaveAchColombiaOperationalSchedule
} from '../models/ach-colombia-operational-schedule.model';

@Injectable({ providedIn: 'root' })
export class AchColombiaOperationalSchedulesService {
  private readonly api = inject(ApiService);
  private readonly base = 'api/ach-colombia/operational-schedules';

  administration() {
    return this.api.get<AchColombiaOperationalScheduleAdministrationRow[]>(`${this.base}/administration`);
  }

  create(value: SaveAchColombiaOperationalSchedule) {
    const { expectedRowVersion: _ignored, ...payload } = value;
    return this.api.post<AchColombiaOperationalSchedule>(this.base, payload);
  }

  update(id: number, value: SaveAchColombiaOperationalSchedule) {
    return this.api.put<AchColombiaOperationalSchedule>(`${this.base}/${id}`, {
      ...value,
      expectedRowVersion: value.expectedRowVersion ?? ''
    });
  }
}

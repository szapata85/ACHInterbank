import { Injectable, inject } from '@angular/core';
import { ApiService } from '../../../core/services/api.service';
import { NachaRecordLayoutDto } from '../models/nacha-layout.model';

@Injectable({ providedIn: 'root' })
export class NachaLayoutsService {
  private readonly api = inject(ApiService);
  private readonly basePath = 'nacha-layouts';

  getAll() {
    return this.api.get<NachaRecordLayoutDto[]>(this.basePath);
  }

  getById(id: number) {
    return this.api.get<NachaRecordLayoutDto>(`${this.basePath}/${id}`);
  }

  create(payload: NachaRecordLayoutDto) {
    return this.api.post<NachaRecordLayoutDto>(this.basePath, payload);
  }

  update(payload: NachaRecordLayoutDto) {
    return this.api.put<NachaRecordLayoutDto>(`${this.basePath}/${payload.id}`, payload);
  }

  delete(id: number) {
    return this.api.delete<void>(`${this.basePath}/${id}`);
  }
}

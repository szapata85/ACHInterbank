import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { ApiService } from '../../../core/services/api.service';
import { NachaRecordDefinitionDto } from '../models/nacha-record-definition.model';

@Injectable({ providedIn: 'root' })
export class NachaRecordDefinitionsService {
  private readonly api = inject(ApiService);
  private readonly basePath = 'nacha-record-definitions';

  getAll(): Observable<NachaRecordDefinitionDto[]> {
    return this.api.get<NachaRecordDefinitionDto[]>(this.basePath);
  }

  create(payload: NachaRecordDefinitionDto): Observable<NachaRecordDefinitionDto> {
    return this.api.post<NachaRecordDefinitionDto>(this.basePath, payload);
  }

  update(payload: NachaRecordDefinitionDto): Observable<NachaRecordDefinitionDto> {
    return this.api.put<NachaRecordDefinitionDto>(`${this.basePath}/${payload.id}`, payload);
  }

  delete(id: number): Observable<void> {
    return this.api.delete<void>(`${this.basePath}/${id}`);
  }
}

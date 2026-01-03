import { Injectable, inject } from '@angular/core';
import { ApiService } from '../../../core/services/api.service';
import { TaskDefinitionDto } from '../models/task-definition.model';

@Injectable({ providedIn: 'root' })
export class TaskDefinitionsService {
  private readonly api = inject(ApiService);
  private readonly basePath = 'taskdefinitions';

  getAll() {
    return this.api.get<TaskDefinitionDto[]>(this.basePath);
  }

  getById(id: number) {
    return this.api.get<TaskDefinitionDto>(`${this.basePath}/${id}`);
  }

  create(payload: TaskDefinitionDto) {
    return this.api.post<TaskDefinitionDto>(this.basePath, payload);
  }

  update(payload: TaskDefinitionDto) {
    return this.api.put<TaskDefinitionDto>(`${this.basePath}/${payload.id}`, payload);
  }

  delete(id: number) {
    return this.api.delete<void>(`${this.basePath}/${id}`);
  }
}

import { Injectable, inject } from '@angular/core';
import { ApiService } from '../../../core/services/api.service';
import {
  ManualExecutionResult,
  SchedulerExecution,
  SchedulerInstance,
  SchedulerOverview,
  SchedulerPagedResult,
  SchedulerSchedulePreview,
  SchedulerScheduleRequest,
  SchedulerTask,
  TaskDefinitionDto
} from '../models/task-definition.model';

@Injectable({ providedIn: 'root' })
export class TaskDefinitionsService {
  private readonly api = inject(ApiService);
  private readonly basePath = 'taskdefinitions';
  private readonly schedulerPath = 'api/scheduler';

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

  getOverview() {
    return this.api.get<SchedulerOverview>(`${this.schedulerPath}/overview`);
  }

  getSchedulerTasks() {
    return this.api.get<SchedulerTask[]>(`${this.schedulerPath}/tasks`);
  }

  getInstances() {
    return this.api.get<SchedulerInstance[]>(`${this.schedulerPath}/instances`);
  }

  getHistory(filters: Record<string, string | number | undefined> = {}) {
    const query = Object.entries(filters)
      .filter(([, value]) => value !== undefined && value !== '')
      .map(([key, value]) => `${encodeURIComponent(key)}=${encodeURIComponent(String(value))}`)
      .join('&');
    return this.api.get<SchedulerPagedResult<SchedulerExecution>>(`${this.schedulerPath}/history${query ? `?${query}` : ''}`);
  }

  executeNow(taskCode: string, reason: string, requestId: string) {
    return this.api.post<ManualExecutionResult>(`${this.schedulerPath}/tasks/${encodeURIComponent(taskCode)}/execute`, { reason, requestId });
  }

  pause(taskCode: string) {
    return this.api.post<void>(`${this.schedulerPath}/tasks/${encodeURIComponent(taskCode)}/pause`, {});
  }

  resume(taskCode: string) {
    return this.api.post<void>(`${this.schedulerPath}/tasks/${encodeURIComponent(taskCode)}/resume`, {});
  }

  updateSchedule(taskCode: string, request: SchedulerScheduleRequest) {
    return this.api.put<SchedulerTask>(`${this.schedulerPath}/tasks/${encodeURIComponent(taskCode)}/schedule`, request);
  }

  previewSchedule(request: SchedulerScheduleRequest) {
    return this.api.post<SchedulerSchedulePreview>(`${this.schedulerPath}/schedule/preview`, request);
  }
}

import { ChangeDetectionStrategy, ChangeDetectorRef, Component, OnDestroy, OnInit, inject } from '@angular/core';
import { FormBuilder, Validators } from '@angular/forms';
import { HttpErrorResponse } from '@angular/common/http';
import { catchError, finalize, forkJoin, map, Observable, of, Subject, switchMap, takeUntil, timer } from 'rxjs';
import { SharedModule } from '../../../shared/shared.module';
import {
  SchedulerExecution,
  SchedulerInstance,
  SchedulerOverview,
  SchedulerSchedulePreview,
  SchedulerScheduleRequest,
  SchedulerTask
} from '../models/task-definition.model';
import { TaskDefinitionsService } from '../services/task-definitions.service';
import { NotificationService } from '../../../core/services/notification.service';
import { AuthService } from '../../../core/services/auth.service';

@Component({
  selector: 'app-task-definitions',
  standalone: true,
  imports: [SharedModule],
  templateUrl: './task-definitions.component.html',
  styleUrls: ['./task-definitions.component.scss'],
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class TaskDefinitionsComponent implements OnInit, OnDestroy {
  private readonly service = inject(TaskDefinitionsService);
  private readonly auth = inject(AuthService);
  private readonly fb = inject(FormBuilder);
  private readonly cdr = inject(ChangeDetectorRef);
  private readonly notifications = inject(NotificationService);
  private readonly destroy$ = new Subject<void>();

  overview: SchedulerOverview | null = null;
  tasks: SchedulerTask[] = [];
  history: SchedulerExecution[] = [];
  instances: SchedulerInstance[] = [];
  historyTotal = 0;
  loading = false;
  overviewError = '';
  tasksError = '';
  instancesError = '';
  historyError = '';
  tasksLoaded = false;
  instancesLoaded = false;
  historyLoaded = false;
  manualTask: SchedulerTask | null = null;
  detailTask: SchedulerTask | null = null;
  scheduleTask: SchedulerTask | null = null;
  submittingManual = false;
  savingSchedule = false;
  previewing = false;
  preview: SchedulerSchedulePreview | null = null;
  private retryBlockedUntil = 0;

  readonly manualForm = this.fb.nonNullable.group({
    reason: ['', [Validators.required, Validators.minLength(10), Validators.maxLength(500)]],
    requestId: ['', Validators.required]
  });

  readonly historyForm = this.fb.nonNullable.group({
    taskCode: [''],
    status: [''],
    triggerType: [''],
    instanceId: [''],
    userName: [''],
    correlationId: [''],
    fromUtc: [''],
    toUtc: ['']
  });

  readonly scheduleForm = this.fb.nonNullable.group({
    periodicityType: [6, Validators.required],
    n: [null as number | null],
    minute: [null as number | null],
    timeOfDay: ['06:30'],
    weeklyDay: [1 as number | null],
    monthDay: [1 as number | null],
    cronExpression: ['0 30 6 ? * MON-FRI'],
    timeZoneId: ['America/Bogota', Validators.required],
    misfirePolicy: [0, Validators.required],
    onlyBusinessDays: [true],
    startAt: [''],
    endAt: ['']
  });

  readonly periodicityOptions = [
    { value: 0, label: 'Una vez' },
    { value: 1, label: 'Cada N minutos' },
    { value: 2, label: 'Cada hora' },
    { value: 3, label: 'Todos los días' },
    { value: 4, label: 'Semanal' },
    { value: 5, label: 'Mensual' },
    { value: 6, label: 'Expresión avanzada (cron)' }
  ];

  readonly misfireOptions = [
    { value: 0, label: 'Omitir la ejecución perdida' },
    { value: 1, label: 'Ejecutar una vez al recuperarse' }
  ];

  readonly weekDays = [
    { value: 1, label: 'Lunes' },
    { value: 2, label: 'Martes' },
    { value: 3, label: 'Miércoles' },
    { value: 4, label: 'Jueves' },
    { value: 5, label: 'Viernes' },
    { value: 6, label: 'Sábado' },
    { value: 0, label: 'Domingo' }
  ];

  get canViewHistory(): boolean { return this.auth.hasPermission('Scheduler.History.View'); }
  get canViewInstances(): boolean { return this.auth.hasPermission('Scheduler.ViewInstances'); }
  get canExecute(): boolean { return this.auth.hasPermission('Scheduler.Execute'); }
  get canManageSchedule(): boolean { return this.auth.hasPermission('Scheduler.ManageSchedule'); }
  get canPauseResume(): boolean { return this.auth.hasPermission('Scheduler.PauseResume'); }
  get reloadBlocked(): boolean { return Date.now() < this.retryBlockedUntil; }

  ngOnInit(): void {
    this.load();
    timer(30_000, 30_000)
      .pipe(
        switchMap(() => {
          if (this.loading || this.reloadBlocked) return of(null);
          return forkJoin({
            overview: this.sectionResult(this.service.getOverview(), 'No fue posible actualizar el resumen del clúster.'),
            instances: this.canViewInstances
              ? this.sectionResult(this.service.getInstances(), 'No fue posible actualizar las instancias del clúster.')
              : of({ succeeded: true, value: [] as SchedulerInstance[], error: '' })
          });
        }),
        takeUntil(this.destroy$)
      )
      .subscribe((result) => {
        if (!result) return;
        this.applyOverviewResult(result.overview);
        this.applyInstancesResult(result.instances);
        this.cdr.markForCheck();
      });
  }

  ngOnDestroy(): void {
    this.destroy$.next();
    this.destroy$.complete();
  }

  load(): void {
    if (this.loading || this.reloadBlocked) return;
    this.loading = true;
    this.overviewError = '';
    this.tasksError = '';
    this.instancesError = '';
    this.historyError = '';
    forkJoin({
      overview: this.sectionResult(this.service.getOverview(), 'No fue posible cargar el resumen del clúster.'),
      tasks: this.sectionResult(this.service.getSchedulerTasks(), 'No fue posible cargar las tareas del programador.'),
      instances: this.canViewInstances
        ? this.sectionResult(this.service.getInstances(), 'No fue posible cargar las instancias del clúster.')
        : of({ succeeded: true, value: [] as SchedulerInstance[], error: '' }),
      history: this.canViewHistory
        ? this.sectionResult(this.service.getHistory({ page: 1, pageSize: 25 }), 'No fue posible cargar el historial funcional.')
        : of({ succeeded: true, value: { items: [], page: 1, pageSize: 25, total: 0 }, error: '' })
    })
      .pipe(
        finalize(() => {
          this.loading = false;
          this.cdr.markForCheck();
        }),
        takeUntil(this.destroy$)
      )
      .subscribe(({ overview, tasks, instances, history }) => {
        this.applyOverviewResult(overview);
        this.tasksError = tasks.error;
        if (tasks.succeeded) {
          this.tasks = tasks.value ?? [];
          this.tasksLoaded = true;
        }
        this.applyInstancesResult(instances);
        this.historyError = history.error;
        if (history.succeeded && history.value) {
          this.history = history.value.items ?? [];
          this.historyTotal = history.value.total;
          this.historyLoaded = true;
        }
      });
  }

  filterHistory(): void {
    if (!this.canViewHistory || this.reloadBlocked) return;
    const raw = this.historyForm.getRawValue();
    this.historyError = '';
    this.service.getHistory({
      page: 1,
      pageSize: 50,
      taskCode: raw.taskCode || undefined,
      status: raw.status || undefined,
      triggerType: raw.triggerType || undefined,
      instanceId: raw.instanceId || undefined,
      userName: raw.userName || undefined,
      correlationId: raw.correlationId || undefined,
      fromUtc: this.toIso(raw.fromUtc),
      toUtc: this.toIso(raw.toUtc)
    }).subscribe({
      next: (result) => {
        this.history = result.items ?? [];
        this.historyTotal = result.total;
        this.historyLoaded = true;
        this.cdr.markForCheck();
      },
      error: (error) => {
        this.registerRetryAfter(error);
        this.historyError = this.errorMessage(error, 'No fue posible filtrar el historial.');
        this.notifications.error(this.historyError);
        this.cdr.markForCheck();
      }
    });
  }

  openManual(task: SchedulerTask): void {
    this.manualTask = task;
    this.manualForm.reset({ reason: '', requestId: crypto.randomUUID() });
    this.cdr.markForCheck();
  }

  closeManual(): void {
    if (this.submittingManual) return;
    this.manualTask = null;
    this.cdr.markForCheck();
  }

  executeNow(): void {
    if (!this.manualTask || this.manualForm.invalid || this.submittingManual) {
      this.manualForm.markAllAsTouched();
      return;
    }

    const { reason, requestId } = this.manualForm.getRawValue();
    this.submittingManual = true;
    this.service.executeNow(this.manualTask.taskCode, reason.trim(), requestId)
      .pipe(finalize(() => {
        this.submittingManual = false;
        this.cdr.markForCheck();
      }))
      .subscribe({
        next: (result) => {
          this.notifications.success(`${result.message}${result.executionId ? ` Ejecución: ${result.executionId}` : ''}`);
          this.manualTask = null;
          this.load();
        },
        error: (error) => {
          this.notifications.error(this.errorMessage(error, 'No fue posible iniciar la ejecución.'));
        }
      });
  }

  pause(task: SchedulerTask): void {
    this.service.pause(task.taskCode).subscribe({
      next: () => { this.notifications.success('La tarea quedó pausada.'); this.load(); },
      error: (error) => this.notifications.error(this.errorMessage(error, 'No fue posible pausar la tarea.'))
    });
  }

  resume(task: SchedulerTask): void {
    this.service.resume(task.taskCode).subscribe({
      next: () => { this.notifications.success('La tarea fue reanudada.'); this.load(); },
      error: (error) => this.notifications.error(this.errorMessage(error, 'No fue posible reanudar la tarea.'))
    });
  }

  openSchedule(task: SchedulerTask): void {
    this.scheduleTask = task;
    this.preview = null;
    this.scheduleForm.reset({
      periodicityType: task.periodicityType,
      n: task.n ?? null,
      minute: task.minute ?? null,
      timeOfDay: task.timeOfDay ?? '06:30',
      weeklyDay: task.weeklyDay ?? 1,
      monthDay: task.monthDay ?? 1,
      cronExpression: task.cronExpression ?? '',
      timeZoneId: task.timeZoneId || 'America/Bogota',
      misfirePolicy: task.misfirePolicy,
      onlyBusinessDays: task.onlyBusinessDays,
      startAt: this.toLocalDate(task.startAt),
      endAt: this.toLocalDate(task.endAt)
    });
    this.cdr.markForCheck();
  }

  closeSchedule(): void {
    if (this.savingSchedule) return;
    this.scheduleTask = null;
    this.preview = null;
    this.cdr.markForCheck();
  }

  previewSchedule(): void {
    if (this.scheduleForm.invalid) {
      this.scheduleForm.markAllAsTouched();
      return;
    }
    this.previewing = true;
    this.service.previewSchedule(this.schedulePayload())
      .pipe(finalize(() => { this.previewing = false; this.cdr.markForCheck(); }))
      .subscribe({
        next: (preview) => this.preview = preview,
        error: (error) => this.notifications.error(this.errorMessage(error, 'La programación no es válida.'))
      });
  }

  saveSchedule(): void {
    if (!this.scheduleTask || this.scheduleForm.invalid || this.savingSchedule) {
      this.scheduleForm.markAllAsTouched();
      return;
    }
    this.savingSchedule = true;
    this.service.updateSchedule(this.scheduleTask.taskCode, this.schedulePayload())
      .pipe(finalize(() => { this.savingSchedule = false; this.cdr.markForCheck(); }))
      .subscribe({
        next: () => {
          this.notifications.success('La programación fue actualizada.');
          this.scheduleTask = null;
          this.preview = null;
          this.load();
        },
        error: (error) => this.notifications.error(this.errorMessage(error, 'No fue posible guardar la programación.'))
      });
  }

  showDetail(task: SchedulerTask): void {
    this.detailTask = task;
    this.cdr.markForCheck();
  }

  closeDetail(): void {
    this.detailTask = null;
    this.cdr.markForCheck();
  }

  statusLabel(status: number): string {
    return ['Pendiente', 'En ejecución', 'Exitosa', 'Fallida', 'Recuperada', 'Omitida', 'Rechazada', 'Ejecución perdida'][status] ?? 'Desconocida';
  }

  misfirePolicyLabel(policy: number): string {
    return policy === 1 ? 'Ejecutar una vez al recuperarse' : 'Omitir la ejecución perdida';
  }

  misfireDescriptionLabel(policy: number): string {
    return policy === 1
      ? 'Se ejecuta una vez cuando el programador se recupera y continúa la programación normal.'
      : 'Se omite la ejecución perdida y continúa la programación normal.';
  }

  synchronizationStatusLabel(status: string): string {
    return ({
      Synchronized: 'Sincronizada',
      Pending: 'Pendiente',
      PendingSynchronization: 'Pendiente de sincronización',
      Failed: 'Fallida',
      Error: 'Con error',
      NotFound: 'No encontrada'
    } as Record<string, string>)[status] ?? 'Estado de sincronización no reconocido';
  }

  instanceStatusLabel(status: string): string {
    return ({ Online: 'En línea', Offline: 'Desconectada', Active: 'Activa', Inactive: 'Inactiva', Unknown: 'Desconocida' } as Record<string, string>)[status] ?? 'Estado no reconocido';
  }

  triggerTypeLabel(triggerType: string): string {
    return ({ Scheduled: 'Programada', Programada: 'Programada', Manual: 'Manual', Recovery: 'Recuperación', Recuperación: 'Recuperación', Misfire: 'Ejecución perdida', Retry: 'Reintento' } as Record<string, string>)[triggerType] ?? 'Tipo de activación no reconocido';
  }

  taskCurrentStateLabel(state: string): string {
    return ({ Running: 'En ejecución', Waiting: 'En espera', Pending: 'Pendiente', Paused: 'Pausada', Active: 'Activa', Failed: 'Fallida' } as Record<string, string>)[state] ?? 'Estado no reconocido';
  }

  taskResultLabel(result?: string | null): string {
    if (!result) return 'Sin ejecuciones';
    return ({ Succeeded: 'Exitosa', Success: 'Exitosa', Failed: 'Fallida', Recovered: 'Recuperada', Skipped: 'Omitida', Rejected: 'Rechazada' } as Record<string, string>)[result] ?? 'Resultado no reconocido';
  }

  formatDate(value?: string | null): string {
    if (!value) return '—';
    const date = new Date(value);
    return Number.isNaN(date.getTime()) ? '—' : new Intl.DateTimeFormat('es-CO', { dateStyle: 'short', timeStyle: 'medium' }).format(date);
  }

  formatDuration(value?: number | null): string {
    if (value === null || value === undefined) return '—';
    if (value < 1000) return `${value} ms`;
    return `${(value / 1000).toFixed(1)} s`;
  }

  trackByTask(_: number, task: SchedulerTask): string { return task.taskCode; }
  trackByExecution(_: number, execution: SchedulerExecution): string { return execution.executionId; }
  trackByInstance(_: number, instance: SchedulerInstance): string { return instance.instanceId; }

  private schedulePayload(): SchedulerScheduleRequest {
    const raw = this.scheduleForm.getRawValue();
    return {
      periodicityType: Number(raw.periodicityType),
      n: this.numberOrNull(raw.n),
      minute: this.numberOrNull(raw.minute),
      timeOfDay: raw.timeOfDay || null,
      weeklyDay: this.numberOrNull(raw.weeklyDay),
      monthDay: this.numberOrNull(raw.monthDay),
      cronExpression: raw.cronExpression || null,
      timeZoneId: raw.timeZoneId || 'America/Bogota',
      misfirePolicy: Number(raw.misfirePolicy),
      onlyBusinessDays: raw.onlyBusinessDays,
      startAt: this.toIso(raw.startAt) ?? null,
      endAt: this.toIso(raw.endAt) ?? null
    };
  }

  private numberOrNull(value: unknown): number | null {
    if (value === '' || value === null || value === undefined) return null;
    const number = Number(value);
    return Number.isFinite(number) ? number : null;
  }

  private toIso(value?: string | null): string | undefined {
    if (!value) return undefined;
    const date = new Date(value);
    return Number.isNaN(date.getTime()) ? undefined : date.toISOString();
  }

  private toLocalDate(value?: string | null): string {
    if (!value) return '';
    const date = new Date(value);
    if (Number.isNaN(date.getTime())) return '';
    return new Date(date.getTime() - date.getTimezoneOffset() * 60_000).toISOString().slice(0, 16);
  }

  private sectionResult<T>(request: Observable<T>, fallback: string): Observable<SectionLoadResult<T>> {
    return request.pipe(
      map((value) => ({ succeeded: true, value, error: '' })),
      catchError((error) => {
        this.registerRetryAfter(error);
        return of({ succeeded: false, error: this.errorMessage(error, fallback) });
      })
    );
  }

  private applyOverviewResult(result: SectionLoadResult<SchedulerOverview>): void {
    this.overviewError = result.error;
    if (result.succeeded && result.value) this.overview = result.value;
  }

  private applyInstancesResult(result: SectionLoadResult<SchedulerInstance[]>): void {
    this.instancesError = result.error;
    if (result.succeeded) {
      this.instances = result.value ?? [];
      this.instancesLoaded = true;
    }
  }

  private registerRetryAfter(error: unknown): void {
    if (!(error instanceof HttpErrorResponse) || error.status !== 429) return;
    const retryAfterSeconds = this.retryAfterSeconds(error);
    if (retryAfterSeconds <= 0) return;

    const candidate = Date.now() + retryAfterSeconds * 1000;
    if (candidate <= this.retryBlockedUntil) return;
    this.retryBlockedUntil = candidate;
    timer(retryAfterSeconds * 1000)
      .pipe(takeUntil(this.destroy$))
      .subscribe(() => this.cdr.markForCheck());
  }

  private retryAfterSeconds(error: HttpErrorResponse): number {
    const retryAfter = error.headers?.get('Retry-After')?.trim();
    if (!retryAfter) return 0;
    const seconds = Number(retryAfter);
    if (Number.isFinite(seconds) && seconds >= 0) return Math.ceil(seconds);
    const date = Date.parse(retryAfter);
    return Number.isNaN(date) ? 0 : Math.max(0, Math.ceil((date - Date.now()) / 1000));
  }

  private errorMessage(error: unknown, fallback: string): string {
    if (!(error instanceof HttpErrorResponse)) return fallback;
    if (error.status === 401) {
      return 'La sesión no está autorizada o expiró. Inicie sesión nuevamente.';
    }
    if (error.status === 429) {
      const retryAfter = this.retryAfterSeconds(error);
      return retryAfter > 0
        ? `Se alcanzó temporalmente el límite de solicitudes. Intente de nuevo en ${retryAfter} segundos.`
        : 'Se alcanzó temporalmente el límite de solicitudes. Intente de nuevo en unos instantes.';
    }
    const payload = error.error as { message?: unknown; title?: unknown; errors?: Record<string, unknown> } | string | null;
    if (typeof payload === 'string' && payload.trim()) return payload;
    if (payload && typeof payload === 'object') {
      if (typeof payload.message === 'string' && payload.message.trim()) return payload.message;
      if (payload.errors) {
        const validation = Object.values(payload.errors).flatMap((value) => Array.isArray(value) ? value : [value])
          .filter((value): value is string => typeof value === 'string');
        if (validation.length) return validation.join(' ');
      }
      if (typeof payload.title === 'string' && payload.title.trim()) return payload.title;
    }
    return error.status === 409 ? 'La tarea ya tiene una ejecución activa.' : fallback;
  }
}

interface SectionLoadResult<T> {
  succeeded: boolean;
  value?: T;
  error: string;
}

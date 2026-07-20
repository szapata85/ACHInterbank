import { ChangeDetectionStrategy, ChangeDetectorRef, Component, OnDestroy, OnInit, inject } from '@angular/core';
import { FormBuilder, Validators } from '@angular/forms';
import { HttpErrorResponse } from '@angular/common/http';
import { catchError, finalize, forkJoin, of, Subject, switchMap, takeUntil, timer } from 'rxjs';
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
  loadError = '';
  manualTask: SchedulerTask | null = null;
  detailTask: SchedulerTask | null = null;
  scheduleTask: SchedulerTask | null = null;
  submittingManual = false;
  savingSchedule = false;
  previewing = false;
  preview: SchedulerSchedulePreview | null = null;

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
    { value: 6, label: 'Cron avanzado' }
  ];

  readonly misfireOptions = [
    { value: 0, label: 'DoNothing — omitir la ejecución perdida' },
    { value: 1, label: 'FireAndProceed — ejecutar una vez al recuperarse' }
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

  ngOnInit(): void {
    this.load();
    timer(30_000, 30_000)
      .pipe(
        switchMap(() => forkJoin({
          overview: this.service.getOverview().pipe(catchError(() => of(this.overview))),
          instances: this.canViewInstances ? this.service.getInstances().pipe(catchError(() => of(this.instances))) : of([])
        })),
        takeUntil(this.destroy$)
      )
      .subscribe(({ overview, instances }) => {
        this.overview = overview;
        this.instances = instances;
        this.cdr.markForCheck();
      });
  }

  ngOnDestroy(): void {
    this.destroy$.next();
    this.destroy$.complete();
  }

  load(): void {
    this.loading = true;
    this.loadError = '';
    forkJoin({
      overview: this.service.getOverview(),
      tasks: this.service.getSchedulerTasks(),
      instances: this.canViewInstances ? this.service.getInstances() : of([]),
      history: this.canViewHistory ? this.service.getHistory({ page: 1, pageSize: 25 }) : of({ items: [], page: 1, pageSize: 25, total: 0 })
    })
      .pipe(
        finalize(() => {
          this.loading = false;
          this.cdr.markForCheck();
        }),
        takeUntil(this.destroy$)
      )
      .subscribe({
        next: ({ overview, tasks, instances, history }) => {
          this.overview = overview;
          this.tasks = tasks ?? [];
          this.instances = instances ?? [];
          this.history = history.items ?? [];
          this.historyTotal = history.total;
        },
        error: (error) => {
          this.loadError = this.errorMessage(error, 'No fue posible cargar la administración del programador.');
        }
      });
  }

  filterHistory(): void {
    if (!this.canViewHistory) return;
    const raw = this.historyForm.getRawValue();
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
        this.cdr.markForCheck();
      },
      error: (error) => this.notifications.error(this.errorMessage(error, 'No fue posible filtrar el historial.'))
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
    return ['Pendiente', 'En ejecución', 'Exitosa', 'Fallida', 'Recuperada', 'Omitida', 'Rechazada', 'Misfire'][status] ?? 'Desconocida';
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

  private errorMessage(error: unknown, fallback: string): string {
    if (!(error instanceof HttpErrorResponse)) return fallback;
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

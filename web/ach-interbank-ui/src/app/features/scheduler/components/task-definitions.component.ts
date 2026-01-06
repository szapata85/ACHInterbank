import { ChangeDetectionStrategy, ChangeDetectorRef, Component, OnDestroy, OnInit, inject } from '@angular/core';
import { FormArray, FormBuilder, Validators } from '@angular/forms';
import { finalize, Subject } from 'rxjs';
import { SharedModule } from '../../../shared/shared.module';
import { TaskDefinitionDto, TaskParameterDto } from '../models/task-definition.model';
import { TaskDefinitionsService } from '../services/task-definitions.service';
import { NotificationService } from '../../../core/services/notification.service';

interface EnumOption {
  value: number;
  label: string;
}

interface TaskDefinitionRow extends TaskDefinitionDto {
  statusLabel: string;
  periodicityLabel: string;
}

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
  private readonly fb = inject(FormBuilder);
  private readonly cdr = inject(ChangeDetectorRef);
  private readonly notifications = inject(NotificationService);
  private readonly destroy$ = new Subject<void>();

  tasks: TaskDefinitionRow[] = [];
  loading = false;
  saving = false;
  editing: TaskDefinitionDto | null = null;

  readonly statusOptions: EnumOption[] = [
    { value: 0, label: 'Deshabilitado' },
    { value: 1, label: 'Habilitado' }
  ];

  readonly calendarPolicyOptions: EnumOption[] = [
    { value: 0, label: 'Ignorar calendario' },
    { value: 1, label: 'Solo días hábiles' },
    { value: 2, label: 'Solo fines de semana' },
    { value: 3, label: 'Omitir festivos' },
    { value: 4, label: 'Mover al siguiente hábil' }
  ];

  readonly concurrencyOptions: EnumOption[] = [
    { value: 0, label: 'Permitir paralelo' },
    { value: 1, label: 'Omitir si está en ejecución' },
    { value: 2, label: 'Encolar' }
  ];

  readonly periodicityOptions: EnumOption[] = [
    { value: 0, label: 'Una vez' },
    { value: 1, label: 'Cada N minutos' },
    { value: 2, label: 'Hora con minuto fijo' },
    { value: 3, label: 'Diario a la hora' },
    { value: 4, label: 'Semanal' },
    { value: 5, label: 'Mensual' },
    { value: 6, label: 'Cron' }
  ];

  readonly columns = [
    { key: 'id', label: 'ID', width: '80px' },
    { key: 'code', label: 'Código' },
    { key: 'name', label: 'Nombre' },
    { key: 'statusLabel', label: 'Estado' },
    { key: 'periodicityLabel', label: 'Periodicidad' }
  ];

  form = this.fb.nonNullable.group({
    id: [0],
    code: ['', [Validators.required, Validators.maxLength(100)]],
    name: ['', [Validators.required, Validators.maxLength(200)]],
    status: [1, Validators.required],
    calendarPolicy: [1, Validators.required],
    timeZoneId: ['America/Bogota'],
    concurrencyPolicy: [1, Validators.required],
    retryOnFailure: [true],
    maxRetries: [null as number | null],
    retryBackoffSeconds: [60],
    periodicityType: [0, Validators.required],
    n: [null as number | null],
    minute: [null as number | null],
    timeOfDay: [''],
    weeklyDay: [null as number | null],
    monthDay: [null as number | null],
    cronExpression: [''],
    startAt: [''],
    endAt: [''],
    parameters: this.fb.array([])
  });

  get parameters(): FormArray {
    return this.form.get('parameters') as FormArray;
  }

  ngOnDestroy(): void {
    this.destroy$.next();
    this.destroy$.complete();
  }

  ngOnInit(): void {
    this.load();
  }

  load(): void {
    this.loading = true;
    this.service
      .getAll()
      .pipe(finalize(() => {
        this.loading = false;
        this.cdr.markForCheck();
      }))
      .subscribe({
        next: (items) => {
          this.tasks = (items ?? []).map((task) => ({
            ...task,
            statusLabel: this.statusOptions.find((o) => o.value === task.status)?.label ?? 'N/D',
            periodicityLabel: this.periodicityOptions.find((o) => o.value === task.periodicityType)?.label ?? 'N/D'
          }));
          this.cdr.markForCheck();
        },
        error: () => {
          this.notifications.error('No fue posible cargar las tareas');
        }
      });
  }

  startCreate(): void {
    this.editing = null;
    this.form.reset({
      id: 0,
      code: '',
      name: '',
      status: 1,
      calendarPolicy: 1,
      timeZoneId: 'America/Bogota',
      concurrencyPolicy: 1,
      retryOnFailure: true,
      maxRetries: null,
      retryBackoffSeconds: 60,
      periodicityType: 0,
      n: null,
      minute: null,
      timeOfDay: '',
      weeklyDay: null,
      monthDay: null,
      cronExpression: '',
      startAt: '',
      endAt: ''
    });
    this.parameters.clear();
    this.cdr.markForCheck();
  }

  startEdit(task: TaskDefinitionDto): void {
    this.editing = task;
    this.form.reset({
      id: task.id,
      code: task.code,
      name: task.name,
      status: task.status,
      calendarPolicy: task.calendarPolicy,
      timeZoneId: task.timeZoneId ?? 'America/Bogota',
      concurrencyPolicy: task.concurrencyPolicy,
      retryOnFailure: task.retryOnFailure,
      maxRetries: task.maxRetries ?? null,
      retryBackoffSeconds: task.retryBackoffSeconds,
      periodicityType: task.periodicityType,
      n: task.n ?? null,
      minute: task.minute ?? null,
      timeOfDay: task.timeOfDay ?? '',
      weeklyDay: task.weeklyDay ?? null,
      monthDay: task.monthDay ?? null,
      cronExpression: task.cronExpression ?? '',
      startAt: task.startAt ?? '',
      endAt: task.endAt ?? ''
    });
    this.parameters.clear();
    (task.parameters ?? []).forEach((param) => this.addParameter(param));
    this.cdr.markForCheck();
  }

  addParameter(param?: TaskParameterDto): void {
    this.parameters.push(this.fb.group({
      id: [param?.id ?? 0],
      key: [param?.key ?? '', Validators.required],
      value: [param?.value ?? '']
    }));
  }

  removeParameter(index: number): void {
    this.parameters.removeAt(index);
  }

  cancel(): void {
    this.editing = null;
    this.form.reset();
    this.parameters.clear();
    this.cdr.markForCheck();
  }

  save(): void {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    const payload = this.toPayload();
    this.saving = true;

    const request$ = payload.id
      ? this.service.update(payload)
      : this.service.create(payload);

    request$
      .pipe(finalize(() => {
        this.saving = false;
        this.cdr.markForCheck();
      }))
      .subscribe({
        next: () => {
          this.notifications.success('Tarea actualizada correctamente');
          this.cancel();
          this.load();
        },
        error: () => {
          this.notifications.error('No fue posible guardar la tarea');
        }
      });
  }

  remove(task: TaskDefinitionDto): void {
    if (!confirm(`¿Eliminar la tarea ${task.code}?`)) {
      return;
    }

    this.service.delete(task.id).subscribe({
      next: () => {
        this.notifications.success('Tarea eliminada');
        this.load();
      },
      error: () => {
        this.notifications.error('No fue posible eliminar la tarea');
      }
    });
  }

  private toPayload(): TaskDefinitionDto {
    const raw = this.form.getRawValue();
    return {
      id: raw.id,
      code: raw.code,
      name: raw.name,
      status: raw.status,
      calendarPolicy: raw.calendarPolicy,
      timeZoneId: raw.timeZoneId || null,
      concurrencyPolicy: raw.concurrencyPolicy,
      retryOnFailure: raw.retryOnFailure,
      maxRetries: raw.maxRetries ?? null,
      retryBackoffSeconds: raw.retryBackoffSeconds ?? 60,
      periodicityType: raw.periodicityType,
      n: raw.n ?? null,
      minute: raw.minute ?? null,
      timeOfDay: raw.timeOfDay || null,
      weeklyDay: raw.weeklyDay ?? null,
      monthDay: raw.monthDay ?? null,
      cronExpression: raw.cronExpression || null,
      startAt: raw.startAt || null,
      endAt: raw.endAt || null,
      parameters: (raw.parameters as TaskParameterDto[] ?? []).map((param) => ({
        id: param.id,
        key: param.key,
        value: param.value
      }))
    };
  }
}

import { ChangeDetectionStrategy, ChangeDetectorRef, Component, Inject, OnInit, ViewEncapsulation, inject } from '@angular/core';
import { HttpErrorResponse } from '@angular/common/http';
import { A11yModule } from '@angular/cdk/a11y';
import { CommonModule } from '@angular/common';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MatCardModule } from '@angular/material/card';
import { MatCheckboxModule } from '@angular/material/checkbox';
import { MAT_DIALOG_DATA, MatDialog, MatDialogModule, MatDialogRef } from '@angular/material/dialog';
import { MatDividerModule } from '@angular/material/divider';
import { MatExpansionModule } from '@angular/material/expansion';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatIconModule } from '@angular/material/icon';
import { MatInputModule } from '@angular/material/input';
import { MatMenuModule } from '@angular/material/menu';
import { MatProgressBarModule } from '@angular/material/progress-bar';
import { MatSelectModule } from '@angular/material/select';
import { MatTableModule } from '@angular/material/table';
import { MatTooltipModule } from '@angular/material/tooltip';
import { finalize, forkJoin, of } from 'rxjs';
import { SharedModule } from '../../../shared/shared.module';
import { AuthService } from '../../../core/services/auth.service';
import { NotificationService } from '../../../core/services/notification.service';
import {
  ManualExecutionResult,
  SchedulerExecution,
  SchedulerOverview,
  SchedulerSchedulePreview,
  SchedulerScheduleRequest,
  SchedulerTask,
  SchedulerTechnicalInfo
} from '../models/task-definition.model';
import { TaskDefinitionsService } from '../services/task-definitions.service';

const MATERIAL_IMPORTS = [
  MatButtonModule, MatCardModule, MatCheckboxModule, MatDialogModule, MatDividerModule,
  MatExpansionModule, MatFormFieldModule, MatIconModule, MatInputModule, MatMenuModule,
  MatProgressBarModule, MatSelectModule, MatTableModule, MatTooltipModule
];

@Component({
  selector: 'app-task-definitions',
  standalone: true,
  imports: [SharedModule, ...MATERIAL_IMPORTS],
  templateUrl: './task-definitions.component.html',
  styleUrls: ['./task-definitions.component.scss'],
  encapsulation: ViewEncapsulation.None,
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class TaskDefinitionsComponent implements OnInit {
  private readonly service = inject(TaskDefinitionsService);
  private readonly auth = inject(AuthService);
  private readonly fb = inject(FormBuilder);
  private readonly dialog = inject(MatDialog);
  private readonly notifications = inject(NotificationService);
  private readonly cdr = inject(ChangeDetectorRef);

  readonly displayedColumns = ['task', 'category', 'status', 'schedule', 'last', 'next', 'actions'];
  readonly historyColumns = ['started', 'type', 'task', 'result', 'requestedBy', 'duration', 'detail'];
  readonly filterForm = this.fb.nonNullable.group({ search: [''], category: [''], status: [''], clearingHouse: [''] });
  overview: SchedulerOverview | null = null;
  tasks: SchedulerTask[] = [];
  history: SchedulerExecution[] = [];
  loading = true;
  error = '';
  loaded = false;

  get canViewHistory(): boolean { return this.auth.hasPermission('Scheduler.History.View'); }
  get canExecute(): boolean { return this.auth.hasPermission('Scheduler.Execute'); }
  get canManageSchedule(): boolean { return this.auth.hasPermission('Scheduler.ManageSchedule'); }
  get canPauseResume(): boolean { return this.auth.hasPermission('Scheduler.PauseResume'); }
  get canViewTechnical(): boolean { return this.auth.hasPermission('Scheduler.Technical.View'); }
  get categories(): string[] { return [...new Set(this.tasks.map(x => x.category))].sort(); }
  get clearingHouses(): string[] {
    return [...new Set(this.tasks.flatMap(x => x.operationalContexts.map(c => c.clearingHouseName)))].sort();
  }
  get filteredTasks(): SchedulerTask[] {
    const filters = this.filterForm.getRawValue();
    const search = filters.search.trim().toLocaleLowerCase('es-CO');
    return this.tasks.filter(task =>
      (!search || `${task.name} ${task.description} ${task.category} ${task.processType}`.toLocaleLowerCase('es-CO').includes(search))
      && (!filters.category || task.category === filters.category)
      && (!filters.status || this.taskState(task) === filters.status)
      && (!filters.clearingHouse || task.operationalContexts.some(x => x.clearingHouseName === filters.clearingHouse)));
  }

  ngOnInit(): void { this.load(); }

  load(): void {
    if (this.loading && this.loaded) return;
    this.loading = true;
    this.error = '';
    forkJoin({
      overview: this.service.getOverview(),
      tasks: this.service.getSchedulerTasks(),
      history: this.canViewHistory ? this.service.getHistory({ page: 1, pageSize: 25 }) : of({ items: [], page: 1, pageSize: 25, total: 0 })
    }).pipe(finalize(() => { this.loading = false; this.cdr.markForCheck(); }))
      .subscribe({
        next: result => {
          this.overview = result.overview;
          this.tasks = result.tasks;
          this.history = result.history.items;
          this.loaded = true;
        },
        error: error => {
          this.error = schedulerErrorMessage(error, 'No fue posible consultar las tareas. Verifica la conexión e inténtalo nuevamente.');
          this.loaded = true;
        }
      });
  }

  clearFilters(): void { this.filterForm.reset({ search: '', category: '', status: '', clearingHouse: '' }); }

  openManual(task: SchedulerTask): void {
    const ref = this.dialog.open(SchedulerManualExecutionDialogComponent, {
      width: 'min(620px, calc(100vw - 32px))',
      disableClose: true,
      autoFocus: 'first-tabbable',
      restoreFocus: true,
      data: { task }
    });
    ref.afterClosed().subscribe((result?: ManualExecutionResult) => {
      if (!result) return;
      this.notifications.success(result.message);
      this.load();
    });
  }

  openSchedule(task: SchedulerTask): void {
    if (task.usesCycleSchedule) return;
    const ref = this.dialog.open(SchedulerScheduleDialogComponent, {
      width: 'min(820px, calc(100vw - 32px))',
      maxHeight: 'calc(100vh - 32px)',
      disableClose: true,
      restoreFocus: true,
      data: { task, canViewTechnical: this.canViewTechnical }
    });
    ref.afterClosed().subscribe((saved?: boolean) => {
      if (!saved) return;
      this.notifications.success('La programación fue actualizada correctamente.');
      this.load();
    });
  }

  openDetail(task: SchedulerTask): void {
    this.dialog.open(SchedulerTaskDetailDialogComponent, {
      width: 'min(760px, calc(100vw - 32px))',
      maxHeight: 'calc(100vh - 32px)',
      restoreFocus: true,
      data: { task, canViewTechnical: this.canViewTechnical }
    });
  }

  openHistory(task: SchedulerTask): void {
    if (!this.canViewHistory) return;
    document.getElementById('history-title')?.focus();
    this.history = this.history.filter(x => x.taskCode === task.taskCode);
  }

  toggle(task: SchedulerTask): void {
    const request = task.status === 'Pausada' ? this.service.resume(task.taskCode) : this.service.pause(task.taskCode);
    request.subscribe({
      next: () => { this.notifications.success(task.status === 'Pausada' ? 'La tarea fue activada.' : 'La tarea fue desactivada.'); this.load(); },
      error: error => this.notifications.error(schedulerErrorMessage(error, 'No fue posible actualizar el estado de la tarea.'))
    });
  }

  taskState(task: SchedulerTask): string {
    if (task.currentState === 'En ejecución') return 'En ejecución';
    if (task.status === 'Pausada' || task.status === 'Deshabilitada') return 'Inactiva';
    return 'Activa';
  }
  taskResult(task: SchedulerTask): string {
    if (!task.lastResult) return 'Sin ejecuciones';
    return ({ Succeeded: 'Finalizada correctamente', Success: 'Finalizada correctamente', Failed: 'Fallida', Skipped: 'Ejecución omitida', Rejected: 'Rechazada', Recovered: 'Recuperada' } as Record<string, string>)[task.lastResult] ?? 'Resultado no reconocido';
  }
  executionStatus(status: number): string {
    return ['Pendiente', 'En ejecución', 'Finalizada correctamente', 'Fallida', 'Recuperada', 'Ejecución omitida', 'Rechazada', 'Ejecución perdida'][status] ?? 'Estado no reconocido';
  }
  triggerType(value: string): string {
    return ({ Scheduled: 'Programada', Programada: 'Programada', Manual: 'Manual', Recovery: 'Recuperación', Recuperación: 'Recuperación', Retry: 'Reintento', Misfire: 'Ejecución perdida' } as Record<string, string>)[value] ?? 'Programada';
  }
  taskName(code: string): string { return this.tasks.find(x => x.taskCode === code)?.name ?? 'Tarea programada'; }
  showExecutionTask(code: string): void { const task = this.tasks.find(x => x.taskCode === code); if (task) this.openDetail(task); }
  formatDate(value?: string | null): string {
    if (!value) return 'No disponible';
    const date = new Date(value);
    return Number.isNaN(date.getTime()) ? 'No disponible' : new Intl.DateTimeFormat('es-CO', { dateStyle: 'medium', timeStyle: 'short', timeZone: 'America/Bogota' }).format(date);
  }
  formatDuration(value?: number | null): string {
    if (value === null || value === undefined) return 'No disponible';
    return value < 1000 ? `${value} ms` : `${(value / 1000).toFixed(1)} s`;
  }
}

interface ManualDialogData { task: SchedulerTask; }

@Component({
  selector: 'app-scheduler-manual-execution-dialog',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, A11yModule, MatButtonModule, MatDialogModule, MatFormFieldModule, MatInputModule, MatProgressBarModule, MatIconModule],
  template: `
    <h2 mat-dialog-title>Ejecutar tarea</h2>
    <mat-dialog-content>
      <p>Está a punto de ejecutar «{{ data.task.name }}» antes de su próxima fecha programada.</p>
      <dl class="dialog-summary">
        <dt>Tarea</dt><dd>{{ data.task.name }}</dd>
        <dt *ngIf="data.task.clearingHouse">Cámara y ciclo</dt><dd *ngIf="data.task.clearingHouse">{{ data.task.clearingHouse }} · {{ cycleNames }}</dd>
        <dt>Estado</dt><dd>{{ data.task.currentState }}</dd>
        <dt>Última ejecución</dt><dd>{{ formatDate(data.task.lastExecutionUtc) }}</dd>
        <dt>Próxima ejecución</dt><dd>{{ formatDate(data.task.nextExecutionUtc) }}</dd>
        <dt *ngIf="nextWindow">Próxima ventana</dt><dd *ngIf="nextWindow">{{ formatDate(nextWindow) }}</dd>
      </dl>
      <p class="warning"><mat-icon aria-hidden="true">info</mat-icon>La tarea evaluará las reglas de cámara, ciclo, calendario, ventana operativa e idempotencia. Esta acción no fuerza despachos ni modifica la próxima ejecución.</p>
      <form [formGroup]="form" id="manual-execution-form" (ngSubmit)="submit()">
        <mat-form-field appearance="outline" class="full-width">
          <mat-label>Motivo de la ejecución extraordinaria</mat-label>
          <textarea matInput rows="4" maxlength="500" formControlName="reason" cdkFocusInitial></textarea>
          <mat-hint align="end">{{ form.controls.reason.value.length }}/500</mat-hint>
          <mat-error *ngIf="form.controls.reason.hasError('required')">El motivo es obligatorio.</mat-error>
          <mat-error *ngIf="form.controls.reason.hasError('minlength')">Escribe al menos 10 caracteres.</mat-error>
          <mat-error *ngIf="form.controls.reason.hasError('maxlength')">El motivo no puede superar 500 caracteres.</mat-error>
        </mat-form-field>
        <p class="dialog-error" role="alert" *ngIf="error">{{ error }}</p>
      </form>
      <mat-progress-bar *ngIf="submitting" mode="indeterminate" aria-label="Solicitando ejecución"></mat-progress-bar>
    </mat-dialog-content>
    <mat-dialog-actions align="end">
      <button mat-button type="button" [disabled]="submitting" (click)="dialogRef.close()">Cancelar</button>
      <button mat-flat-button type="submit" form="manual-execution-form" [disabled]="submitting || form.invalid">{{ submitting ? 'Solicitando…' : 'Ejecutar ahora' }}</button>
    </mat-dialog-actions>`
})
export class SchedulerManualExecutionDialogComponent {
  private readonly fb = inject(FormBuilder);
  private readonly service = inject(TaskDefinitionsService);
  private readonly cdr = inject(ChangeDetectorRef);
  readonly dialogRef = inject(MatDialogRef<SchedulerManualExecutionDialogComponent>);
  readonly requestId = crypto.randomUUID();
  readonly form = this.fb.nonNullable.group({ reason: ['', [Validators.required, Validators.minLength(10), Validators.maxLength(500)]] });
  submitting = false;
  error = '';
  constructor(@Inject(MAT_DIALOG_DATA) public readonly data: ManualDialogData) {}
  get cycleNames(): string { return [...new Set(this.data.task.operationalContexts.map(x => x.cycleName))].join(', '); }
  get nextWindow(): string | null { return this.data.task.operationalContexts.map(x => x.nextValidWindowUtc).filter((x): x is string => !!x).sort()[0] ?? null; }
  submit(): void {
    if (this.form.invalid || this.submitting) { this.form.markAllAsTouched(); return; }
    this.submitting = true;
    this.error = '';
    this.service.executeNow(this.data.task.taskCode, this.form.controls.reason.value.trim(), this.requestId)
      .pipe(finalize(() => { this.submitting = false; this.cdr.markForCheck(); }))
      .subscribe({
        next: result => this.dialogRef.close(result),
        error: error => { this.error = schedulerErrorMessage(error, 'No fue posible solicitar la ejecución.'); this.cdr.markForCheck(); }
      });
  }
  formatDate(value?: string | null): string {
    return value ? new Intl.DateTimeFormat('es-CO', { dateStyle: 'medium', timeStyle: 'short', timeZone: 'America/Bogota' }).format(new Date(value)) : 'No disponible';
  }
}

interface ScheduleDialogData { task: SchedulerTask; canViewTechnical: boolean; }

@Component({
  selector: 'app-scheduler-schedule-dialog',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, ...MATERIAL_IMPORTS],
  template: `
    <h2 mat-dialog-title>Editar programación</h2>
    <mat-dialog-content>
      <p><strong>{{ data.task.name }}</strong></p>
      <p class="time-zone-note">Todas las horas corresponden a la hora de Colombia.</p>
      <form [formGroup]="form" class="schedule-grid" id="schedule-form" (ngSubmit)="save()">
        <mat-form-field appearance="outline" class="full-width">
          <mat-label>Frecuencia</mat-label>
          <mat-select formControlName="periodicityType">
            <mat-option *ngFor="let option of options" [value]="option.value">{{ option.label }}</mat-option>
          </mat-select>
        </mat-form-field>
        <mat-form-field appearance="outline" *ngIf="type === 1">
          <mat-label>Cada cuántos minutos</mat-label><input matInput type="number" formControlName="n" min="1" max="1440" />
          <mat-error>Indica un intervalo válido y seguro.</mat-error>
        </mat-form-field>
        <mat-form-field appearance="outline" *ngIf="type === 2">
          <mat-label>Minuto de la hora</mat-label><input matInput type="number" formControlName="minute" min="0" max="59" />
          <mat-error>El minuto debe estar entre 0 y 59.</mat-error>
        </mat-form-field>
        <mat-form-field appearance="outline" *ngIf="[3,4,5].includes(type)">
          <mat-label>Hora</mat-label><input matInput type="time" formControlName="timeOfDay" />
          <mat-error>La hora es obligatoria.</mat-error>
        </mat-form-field>
        <mat-form-field appearance="outline" *ngIf="type === 4">
          <mat-label>Día de la semana</mat-label><mat-select formControlName="weeklyDay"><mat-option *ngFor="let day of weekDays" [value]="day.value">{{ day.label }}</mat-option></mat-select>
          <mat-error>Selecciona un día.</mat-error>
        </mat-form-field>
        <mat-form-field appearance="outline" *ngIf="type === 5">
          <mat-label>Día del mes</mat-label><input matInput type="number" formControlName="monthDay" min="1" max="31" />
          <mat-error>El día debe estar entre 1 y 31.</mat-error>
        </mat-form-field>
        <mat-form-field appearance="outline" *ngIf="type === 0 || type === 7">
          <mat-label>{{ type === 0 ? 'Fecha y hora' : 'Primera fecha anual' }}</mat-label><input matInput type="datetime-local" formControlName="startAt" />
          <mat-error>Selecciona una fecha y hora.</mat-error>
        </mat-form-field>
        <mat-checkbox formControlName="onlyBusinessDays">Solo días hábiles</mat-checkbox>

        <mat-expansion-panel class="full-width" *ngIf="data.canViewTechnical" [expanded]="false">
          <mat-expansion-panel-header><mat-panel-title>Programación avanzada</mat-panel-title></mat-expansion-panel-header>
          <div class="schedule-grid">
            <mat-form-field appearance="outline" *ngIf="type === 6" class="full-width">
              <mat-label>Expresión CRON de Quartz</mat-label><input matInput formControlName="cronExpression" />
              <mat-error>La expresión es obligatoria y debe ser válida para Quartz.</mat-error>
            </mat-form-field>
            <mat-form-field appearance="outline"><mat-label>Zona horaria</mat-label><input matInput formControlName="timeZoneId" /><mat-error>La zona horaria es obligatoria.</mat-error></mat-form-field>
            <mat-form-field appearance="outline"><mat-label>Política ante ejecuciones omitidas</mat-label><mat-select formControlName="misfirePolicy"><mat-option [value]="0">Omitir y continuar</mat-option><mat-option [value]="1">Ejecutar una vez al recuperarse</mat-option></mat-select></mat-form-field>
            <mat-form-field appearance="outline"><mat-label>Hora límite</mat-label><input matInput type="datetime-local" formControlName="endAt" /></mat-form-field>
          </div>
        </mat-expansion-panel>
      </form>
      <section class="preview-panel" aria-live="polite" *ngIf="preview">
        <strong>{{ preview.description }}</strong><span>Próximas cinco ejecuciones</span>
        <ol><li *ngFor="let date of preview.nextExecutionsUtc">{{ formatDate(date) }}</li></ol>
      </section>
      <p class="dialog-error" role="alert" *ngIf="error">{{ error }}</p>
      <mat-progress-bar *ngIf="saving || previewing" mode="indeterminate"></mat-progress-bar>
    </mat-dialog-content>
    <mat-dialog-actions align="end">
      <button mat-button type="button" [disabled]="saving" (click)="dialogRef.close(false)">Cancelar</button>
      <button mat-stroked-button type="button" [disabled]="previewing || form.invalid" (click)="previewSchedule()">Actualizar próximas ejecuciones</button>
      <button mat-flat-button type="submit" form="schedule-form" [disabled]="saving || form.invalid">Guardar programación</button>
    </mat-dialog-actions>`
})
export class SchedulerScheduleDialogComponent implements OnInit {
  private readonly fb = inject(FormBuilder);
  private readonly service = inject(TaskDefinitionsService);
  private readonly cdr = inject(ChangeDetectorRef);
  readonly data = inject<ScheduleDialogData>(MAT_DIALOG_DATA);
  readonly dialogRef = inject(MatDialogRef<SchedulerScheduleDialogComponent>);
  readonly options = [
    { value: 0, label: 'Una sola vez' }, { value: 1, label: 'Cada cierto número de minutos' },
    { value: 2, label: 'Cada hora' }, { value: 3, label: 'Todos los días' },
    { value: 4, label: 'Determinados días de la semana' }, { value: 5, label: 'Determinado día del mes' },
    { value: 7, label: 'Una vez al año' }, { value: 6, label: 'Configuración avanzada' }
  ];
  readonly weekDays = ['Domingo','Lunes','Martes','Miércoles','Jueves','Viernes','Sábado'].map((label, value) => ({ label, value }));
  readonly form = this.fb.group({
    periodicityType: [this.data.task.periodicityType, Validators.required],
    n: [this.data.task.n ?? null as number | null], minute: [this.data.task.minute ?? null as number | null],
    timeOfDay: [this.data.task.timeOfDay ?? '06:30'], weeklyDay: [this.data.task.weeklyDay ?? 1],
    monthDay: [this.data.task.monthDay ?? 1], cronExpression: [this.data.task.cronExpression ?? ''],
    timeZoneId: [this.data.task.timeZoneId || 'America/Bogota', Validators.required],
    misfirePolicy: [this.data.task.misfirePolicy, Validators.required], onlyBusinessDays: [this.data.task.onlyBusinessDays],
    startAt: [toLocalDate(this.data.task.startAt)], endAt: [toLocalDate(this.data.task.endAt)]
  });
  preview: SchedulerSchedulePreview | null = null;
  previewing = false;
  saving = false;
  error = '';
  get type(): number { return Number(this.form.controls.periodicityType.value); }
  ngOnInit(): void {
    this.configureValidators();
    this.form.controls.periodicityType.valueChanges.subscribe(() => { this.configureValidators(); this.preview = null; });
    this.previewSchedule();
  }
  configureValidators(): void {
    const controls = this.form.controls;
    controls.n.setValidators(this.type === 1 ? [Validators.required, Validators.min(1), Validators.max(1440)] : []);
    controls.minute.setValidators(this.type === 2 ? [Validators.required, Validators.min(0), Validators.max(59)] : []);
    controls.timeOfDay.setValidators([3,4,5].includes(this.type) ? [Validators.required] : []);
    controls.weeklyDay.setValidators(this.type === 4 ? [Validators.required] : []);
    controls.monthDay.setValidators(this.type === 5 ? [Validators.required, Validators.min(1), Validators.max(31)] : []);
    controls.startAt.setValidators([0,7].includes(this.type) ? [Validators.required] : []);
    controls.cronExpression.setValidators(this.type === 6 ? [Validators.required] : []);
    Object.values(controls).forEach(control => control.updateValueAndValidity({ emitEvent: false }));
  }
  previewSchedule(): void {
    if (this.form.invalid) { this.form.markAllAsTouched(); return; }
    this.previewing = true; this.error = '';
    this.service.previewSchedule(this.payload()).pipe(finalize(() => { this.previewing = false; this.cdr.markForCheck(); })).subscribe({
      next: preview => { this.preview = preview; this.cdr.markForCheck(); },
      error: error => { this.error = schedulerErrorMessage(error, 'La programación no es válida.'); this.cdr.markForCheck(); }
    });
  }
  save(): void {
    if (this.form.invalid || this.saving) { this.form.markAllAsTouched(); return; }
    this.saving = true; this.error = '';
    this.service.updateSchedule(this.data.task.taskCode, this.payload()).pipe(finalize(() => { this.saving = false; this.cdr.markForCheck(); })).subscribe({
      next: () => this.dialogRef.close(true),
      error: error => { this.error = schedulerErrorMessage(error, 'No fue posible guardar la programación.'); this.cdr.markForCheck(); }
    });
  }
  payload(): SchedulerScheduleRequest {
    const value = this.form.getRawValue();
    return { periodicityType: Number(value.periodicityType), n: value.n, minute: value.minute, timeOfDay: value.timeOfDay,
      weeklyDay: value.weeklyDay, monthDay: value.monthDay, cronExpression: value.cronExpression || null,
      timeZoneId: value.timeZoneId || 'America/Bogota', misfirePolicy: Number(value.misfirePolicy),
      onlyBusinessDays: !!value.onlyBusinessDays, startAt: toIso(value.startAt), endAt: toIso(value.endAt) };
  }
  formatDate(value: string): string { return new Intl.DateTimeFormat('es-CO', { dateStyle: 'full', timeStyle: 'short', timeZone: 'America/Bogota' }).format(new Date(value)); }
}

interface DetailDialogData { task: SchedulerTask; canViewTechnical: boolean; }

@Component({
  selector: 'app-scheduler-task-detail-dialog',
  standalone: true,
  imports: [CommonModule, ...MATERIAL_IMPORTS],
  template: `
    <h2 mat-dialog-title>Detalle de la tarea</h2>
    <mat-dialog-content>
      <h3>{{ data.task.name }}</h3><p>{{ data.task.description }}</p>
      <dl class="dialog-summary"><dt>Categoría</dt><dd>{{ data.task.category }}</dd><dt>Tipo de proceso</dt><dd>{{ data.task.processType }}</dd><dt>Estado</dt><dd>{{ data.task.currentState }}</dd><dt>Programación</dt><dd>{{ data.task.scheduleDescription }}</dd></dl>
      <p class="cycle-governed" *ngIf="data.task.usesCycleSchedule">La programación de esta tarea depende del ciclo de compensación. Para modificar el horario, actualiza la configuración del ciclo correspondiente.</p>
      <section *ngIf="data.task.operationalContexts.length" aria-labelledby="contexts-title"><h4 id="contexts-title">Cámaras, ciclos y ventanas</h4><div class="context-list"><article *ngFor="let context of data.task.operationalContexts"><strong>{{ context.clearingHouseName }} · {{ context.cycleName }}</strong><span>Ventana: {{ context.windowDescription }}</span><span>Hora límite: {{ context.cutoffDescription }}</span><span>{{ context.status }}</span></article></div></section>
      <mat-expansion-panel *ngIf="data.canViewTechnical" [expanded]="false" (opened)="loadTechnical()">
        <mat-expansion-panel-header><mat-panel-title>Información técnica</mat-panel-title></mat-expansion-panel-header>
        <p *ngIf="technicalLoading">Consultando información técnica…</p><p class="dialog-error" *ngIf="technicalError">{{ technicalError }}</p>
        <dl class="dialog-summary" *ngIf="technical"><dt>Identificador interno</dt><dd>{{ technical.taskCode }}</dd><dt>Manejador</dt><dd>{{ technical.handlerCode }}</dd><dt *ngIf="technical.soapService">Servicio de integración</dt><dd *ngIf="technical.soapService">{{ technical.soapService }}</dd><dt>Grupo</dt><dd>{{ technical.jobGroup }}</dd><dt>Expresión CRON</dt><dd>{{ technical.cronExpression || 'No aplica: derivada de ciclos' }}</dd><dt>Zona horaria</dt><dd>{{ technical.timeZoneId }}</dd><dt>Activadores</dt><dd>{{ technical.triggerKeys.length }}</dd></dl>
      </mat-expansion-panel>
    </mat-dialog-content>
    <mat-dialog-actions align="end"><button mat-flat-button mat-dialog-close>Cerrar</button></mat-dialog-actions>`
})
export class SchedulerTaskDetailDialogComponent {
  private readonly service = inject(TaskDefinitionsService);
  private readonly cdr = inject(ChangeDetectorRef);
  technical: SchedulerTechnicalInfo | null = null; technicalLoading = false; technicalError = '';
  constructor(@Inject(MAT_DIALOG_DATA) public readonly data: DetailDialogData) {}
  loadTechnical(): void {
    if (this.technical || this.technicalLoading) return;
    this.technicalLoading = true;
    this.service.getTechnicalInfo(this.data.task.taskCode)
      .pipe(finalize(() => { this.technicalLoading = false; this.cdr.markForCheck(); }))
      .subscribe({
        next: value => { this.technical = value; this.cdr.markForCheck(); },
        error: error => { this.technicalError = schedulerErrorMessage(error, 'No fue posible consultar la información técnica.'); this.cdr.markForCheck(); }
      });
  }
}

function schedulerErrorMessage(error: unknown, fallback: string): string {
  if (!(error instanceof HttpErrorResponse)) return fallback;
  if (error.status === 403) return 'No tienes permiso para realizar esta acción.';
  if (error.status === 404) return 'La tarea solicitada no existe o ya no está disponible.';
  if (error.status === 409) return error.error?.message || 'La tarea ya está en ejecución y no puede iniciarse nuevamente hasta que finalice.';
  const payload = error.error as { message?: string; title?: string; errors?: Record<string, string[]> } | null;
  return payload?.message || (payload?.errors ? Object.values(payload.errors).flat().join(' ') : '') || payload?.title || fallback;
}

function toIso(value?: string | null): string | null {
  if (!value) return null; const date = new Date(value); return Number.isNaN(date.getTime()) ? null : date.toISOString();
}
function toLocalDate(value?: string | null): string {
  if (!value) return ''; const date = new Date(value); if (Number.isNaN(date.getTime())) return '';
  return new Date(date.getTime() - date.getTimezoneOffset() * 60_000).toISOString().slice(0, 16);
}

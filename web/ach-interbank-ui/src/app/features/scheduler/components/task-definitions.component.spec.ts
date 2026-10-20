import { HttpErrorResponse } from '@angular/common/http';
import { NoopAnimationsModule } from '@angular/platform-browser/animations';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { MAT_DIALOG_DATA, MatDialog, MatDialogRef } from '@angular/material/dialog';
import { of, Subject, throwError } from 'rxjs';
import { AuthService } from '../../../core/services/auth.service';
import { NotificationService } from '../../../core/services/notification.service';
import { SchedulerTask } from '../models/task-definition.model';
import { TaskDefinitionsService } from '../services/task-definitions.service';
import {
  SchedulerManualExecutionDialogComponent,
  SchedulerScheduleDialogComponent,
  TaskDefinitionsComponent
} from './task-definitions.component';

const task: SchedulerTask = {
  taskCode: 'CONTRAPARTIDA_DISPATCH',
  name: 'Despachar débitos originados por CFA',
  description: 'Evalúa y envía únicamente los débitos elegibles.',
  category: 'Integración SOAP',
  processType: 'Movimiento monetario débito',
  soapService: 'Proc_Contrapartidas',
  status: 'Activa', clearingHouse: 'ACH Colombia y CENIT',
  scheduleDescription: 'Gobernada por ciclos', cronExpression: null, timeZoneId: 'America/Bogota',
  misfirePolicy: 0, misfireDescription: 'Omitir y continuar.', lastExecutionUtc: null,
  nextExecutionUtc: '2026-08-07T12:30:00Z', lastResult: 'Succeeded', lastDurationMilliseconds: 120,
  lastSchedulerInstance: 'api-01', currentState: 'En espera', manualExecutionEnabled: true,
  requestsRecovery: false, allowsConcurrentExecution: false, periodicityType: 1, n: 5, minute: null,
  timeOfDay: null, weeklyDay: null, monthDay: null, onlyBusinessDays: true, startAt: null, endAt: null,
  synchronizationStatus: 'Synchronized', lastSynchronizationError: null, usesCycleSchedule: true,
  canEditSchedule: false,
  operationalContexts: [{
    cycleConfigId: 1, clearingHouseCode: 'ACHCOL', clearingHouseName: 'ACH Colombia', cycleName: 'Ciclo 1',
    windowDescription: '19:01 a 08:30', cutoffDescription: '08:30', nextValidWindowUtc: '2026-08-07T00:01:00Z',
    nextValidWindowEndUtc: '2026-08-07T13:30:00Z', status: 'Programada'
  }]
};

function serviceSpy(): jasmine.SpyObj<TaskDefinitionsService> {
  const service = jasmine.createSpyObj<TaskDefinitionsService>('TaskDefinitionsService', [
    'getOverview', 'getSchedulerTasks', 'getHistory', 'executeNow', 'pause', 'resume',
    'updateSchedule', 'previewSchedule', 'getTechnicalInfo'
  ]);
  service.getOverview.and.returnValue(of({ totalInstances: 1, activeInstances: 1, offlineInstances: 0,
    runningJobs: 0, upcomingExecutions: 1, recentFailures: 0, recentMisfires: 0,
    schedulerName: 'interno', persistentStore: true, clustered: true, pendingSynchronizations: 0 }));
  service.getSchedulerTasks.and.returnValue(of([task]));
  service.getHistory.and.returnValue(of({ items: [], page: 1, pageSize: 25, total: 0 }));
  service.previewSchedule.and.returnValue(of({ description: 'Cada 5 minutos', nextExecutionsUtc: [
    '2026-08-07T12:05:00Z','2026-08-07T12:10:00Z','2026-08-07T12:15:00Z','2026-08-07T12:20:00Z','2026-08-07T12:25:00Z'
  ] }));
  return service;
}

describe('TaskDefinitionsComponent', () => {
  let fixture: ComponentFixture<TaskDefinitionsComponent>;
  let component: TaskDefinitionsComponent;
  let service: jasmine.SpyObj<TaskDefinitionsService>;
  let dialog: jasmine.SpyObj<MatDialog>;

  beforeEach(async () => {
    service = serviceSpy();
    dialog = jasmine.createSpyObj<MatDialog>('MatDialog', ['open']);
    dialog.open.and.returnValue({ afterClosed: () => of(undefined) } as any);
    await TestBed.configureTestingModule({
      imports: [TaskDefinitionsComponent, NoopAnimationsModule],
      providers: [
        { provide: TaskDefinitionsService, useValue: service },
        { provide: AuthService, useValue: { hasPermission: () => true } },
        { provide: NotificationService, useValue: { success: jasmine.createSpy(), error: jasmine.createSpy() } }
      ]
    }).overrideComponent(TaskDefinitionsComponent, {
      add: { providers: [{ provide: MatDialog, useValue: dialog }] }
    }).compileComponents();
    fixture = TestBed.createComponent(TaskDefinitionsComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('presenta nombres humanizados, español y contexto operativo sin exponer códigos técnicos', () => {
    const text = fixture.nativeElement.textContent as string;
    expect(text).toContain('Tareas programadas');
    expect(text).toContain('Despachar débitos originados por CFA');
    expect(text).toContain('Cámara compensadora');
    expect(text).toContain('Gobernada por ciclos');
    expect(text).toContain('Ventana: 19:01 a 08:30');
    expect(text).not.toContain('CONTRAPARTIDA_DISPATCH');
    expect(text).not.toContain('Proc_Contrapartidas');
    expect(text).not.toContain('Synchronized');
  });

  it('distingue ausencia de tareas y filtros sin coincidencias', () => {
    component.tasks = [];
    fixture.detectChanges();
    expect(fixture.nativeElement.textContent).toContain('Todavía no hay tareas programadas disponibles.');
    component.tasks = [task];
    component.filterForm.controls.search.setValue('sin coincidencia');
    fixture.detectChanges();
    expect(fixture.nativeElement.textContent).toContain('No encontramos tareas que coincidan con los filtros seleccionados.');
  });

  it('abre ejecución manual con datos funcionales y sin flujo directo al handler', () => {
    component.openManual(task);
    expect(dialog.open).toHaveBeenCalledWith(SchedulerManualExecutionDialogComponent, jasmine.objectContaining({
      disableClose: true, restoreFocus: true, data: { task }
    }));
  });

  it('muestra el estado de error empresarial', () => {
    service.getSchedulerTasks.and.returnValue(throwError(() => new HttpErrorResponse({ status: 503 })));
    component.load();
    fixture.detectChanges();
    expect(fixture.nativeElement.textContent).toContain('No fue posible consultar las tareas.');
  });
});

describe('SchedulerManualExecutionDialogComponent', () => {
  let fixture: ComponentFixture<SchedulerManualExecutionDialogComponent>;
  let component: SchedulerManualExecutionDialogComponent;
  let service: jasmine.SpyObj<TaskDefinitionsService>;
  let ref: jasmine.SpyObj<MatDialogRef<SchedulerManualExecutionDialogComponent>>;

  beforeEach(async () => {
    service = serviceSpy();
    ref = jasmine.createSpyObj('MatDialogRef', ['close']);
    await TestBed.configureTestingModule({ imports: [SchedulerManualExecutionDialogComponent, NoopAnimationsModule], providers: [
      { provide: TaskDefinitionsService, useValue: service }, { provide: MatDialogRef, useValue: ref },
      { provide: MAT_DIALOG_DATA, useValue: { task } }
    ] }).compileComponents();
    fixture = TestBed.createComponent(SchedulerManualExecutionDialogComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('exige motivo, muestra mat-error y evita doble envío', () => {
    component.form.controls.reason.markAsTouched(); fixture.detectChanges();
    expect(fixture.nativeElement.textContent).toContain('El motivo es obligatorio.');
    const pending = new Subject<any>(); service.executeNow.and.returnValue(pending);
    component.form.controls.reason.setValue('Ejecución extraordinaria autorizada');
    component.submit(); component.submit();
    expect(service.executeNow).toHaveBeenCalledTimes(1);
    pending.next({ outcome: 0, executionId: 'seguimiento', message: 'Solicitud aceptada' }); pending.complete();
    expect(ref.close).toHaveBeenCalled();
  });

  it('presenta conflictos 409 como condición operativa', () => {
    service.executeNow.and.returnValue(throwError(() => new HttpErrorResponse({ status: 409, error: { message: 'La tarea ya está en ejecución y no puede iniciarse nuevamente hasta que finalice.' } })));
    component.form.controls.reason.setValue('Ejecución extraordinaria autorizada'); component.submit(); fixture.detectChanges();
    expect(fixture.nativeElement.textContent).toContain('La tarea ya está en ejecución');
  });
});

describe('SchedulerScheduleDialogComponent', () => {
  it('ofrece el constructor visual y cinco próximas ejecuciones', async () => {
    const service = serviceSpy(); const ref = jasmine.createSpyObj('MatDialogRef', ['close']);
    const editable = { ...task, usesCycleSchedule: false, canEditSchedule: true };
    await TestBed.configureTestingModule({ imports: [SchedulerScheduleDialogComponent, NoopAnimationsModule], providers: [
      { provide: TaskDefinitionsService, useValue: service }, { provide: MatDialogRef, useValue: ref },
      { provide: MAT_DIALOG_DATA, useValue: { task: editable, canViewTechnical: true } }
    ] }).compileComponents();
    const fixture = TestBed.createComponent(SchedulerScheduleDialogComponent); fixture.detectChanges();
    const text = fixture.nativeElement.textContent as string;
    expect(fixture.componentInstance.options.map(x => x.label)).toContain('Cada cierto número de minutos');
    expect(fixture.componentInstance.options.map(x => x.label)).toContain('Una vez al año');
    expect(text).toContain('Todas las horas corresponden a la hora de Colombia.');
    expect(text).toContain('Programación avanzada');
    expect(fixture.componentInstance.preview?.nextExecutionsUtc).toHaveSize(5);
  });
});

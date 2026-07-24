import { HttpErrorResponse, HttpHeaders } from '@angular/common/http';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { of, Subject, throwError } from 'rxjs';
import { AuthService } from '../../../core/services/auth.service';
import { NotificationService } from '../../../core/services/notification.service';
import { SchedulerTask } from '../models/task-definition.model';
import { TaskDefinitionsService } from '../services/task-definitions.service';
import { TaskDefinitionsComponent } from './task-definitions.component';

describe('TaskDefinitionsComponent', () => {
  let fixture: ComponentFixture<TaskDefinitionsComponent>;
  let component: TaskDefinitionsComponent;
  let service: jasmine.SpyObj<TaskDefinitionsService>;

  const task: SchedulerTask = {
    taskCode: 'ACH_CYCLE_SCHEDULER',
    name: 'Programador de ciclos',
    description: 'Programa los ciclos operativos',
    status: 'Activa',
    clearingHouse: null,
    scheduleDescription: 'Lunes a viernes a las 6:30 a. m.',
    cronExpression: '0 30 6 ? * MON-FRI',
    timeZoneId: 'America/Bogota',
    misfirePolicy: 0,
    misfireDescription: 'Omitir la ejecucion perdida.',
    lastExecutionUtc: null,
    nextExecutionUtc: '2026-07-21T11:30:00Z',
    lastResult: 'Succeeded',
    lastDurationMilliseconds: 120,
    lastSchedulerInstance: 'achinterbank-api-01',
    currentState: 'En espera',
    manualExecutionEnabled: true,
    requestsRecovery: false,
    allowsConcurrentExecution: false,
    periodicityType: 6,
    n: null,
    minute: null,
    timeOfDay: '06:30',
    weeklyDay: 1,
    monthDay: 1,
    onlyBusinessDays: true,
    startAt: null,
    endAt: null,
    synchronizationStatus: 'Pending',
    lastSynchronizationError: null
  };

  beforeEach(async () => {
    service = jasmine.createSpyObj<TaskDefinitionsService>('TaskDefinitionsService', [
      'getOverview', 'getSchedulerTasks', 'getInstances', 'getHistory', 'executeNow',
      'pause', 'resume', 'updateSchedule', 'previewSchedule'
    ]);
    service.getOverview.and.returnValue(of({
      totalInstances: 2, activeInstances: 2, offlineInstances: 0, runningJobs: 0,
      upcomingExecutions: 1, recentFailures: 0, recentMisfires: 0,
      schedulerName: 'ACHInterbankScheduler', persistentStore: true, clustered: true,
      pendingSynchronizations: 0
    }));
    service.getSchedulerTasks.and.returnValue(of([task]));
    service.getInstances.and.returnValue(of([{
      instanceId: 'instance-1', instanceName: 'api-01', hostName: 'servidor-01',
      startedAtUtc: '2026-07-21T11:00:00Z', lastHeartbeatUtc: '2026-07-21T11:30:00Z',
      status: 'Online', isCurrentInstance: true, currentlyExecutingJobs: 0, version: 'test'
    }]));
    service.getHistory.and.returnValue(of({ items: [{
      executionId: 'execution-1', taskCode: task.taskCode, jobName: 'job', jobGroup: 'group',
      triggerName: 'trigger', triggerType: 'Recovery', fireInstanceId: 'fire', schedulerInstanceId: 'instance-1',
      schedulerInstanceName: 'api-01', requestedByUserId: null, requestedByUserName: 'admin', requestReason: 'test',
      idempotencyKey: null, correlationId: 'correlation-1', scheduledFireTimeUtc: null, actualFireTimeUtc: null,
      startedAtUtc: '2026-07-21T11:00:00Z', finishedAtUtc: null, durationMilliseconds: 100, status: 4,
      isRecovery: true, refireCount: 0, misfireDetected: true, resultSummary: 'ok', errorCode: null,
      errorSummary: null
    } as any], page: 1, pageSize: 25, total: 1 }));

    await TestBed.configureTestingModule({
      imports: [TaskDefinitionsComponent],
      providers: [
        { provide: TaskDefinitionsService, useValue: service },
        { provide: AuthService, useValue: { hasPermission: () => true } },
        { provide: NotificationService, useValue: { success: jasmine.createSpy(), error: jasmine.createSpy() } }
      ]
    }).compileComponents();

    fixture = TestBed.createComponent(TaskDefinitionsComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('muestra el tablero, la tarea y la programacion legible', () => {
    const text = fixture.nativeElement.textContent as string;
    expect(text).toMatch(/Administraci.n de tareas programadas/);
    expect(text).toContain('Programador de ciclos');
    expect(text).toContain('Lunes a viernes a las 6:30 a. m.');
    expect(text).toContain('Almacenamiento persistente');
    expect(text).toContain('Tareas en ejecución');
    expect(text).toContain('Ejecuciones perdidas recientes');
    expect(text).toContain('Omitir la ejecución perdida');
    expect(text).toContain('Última señal de actividad');
    expect(text).toContain('Identificador de instancia');
    expect(text).toContain('Servidor');
    expect(text).toContain('Identificador de correlación');
    expect(text).toContain('Recuperación');
    expect(text).not.toContain('Synchronized');
    expect(text).not.toContain('Online');
    expect(text).not.toContain('Recovery');
    expect(text).not.toContain('Succeeded');
    expect(text).not.toContain('[object Object]');
  });

  it('traduce estados, activadores y valores desconocidos sin exponer inglés crudo', () => {
    expect(component.synchronizationStatusLabel('Synchronized')).toBe('Sincronizada');
    expect(component.instanceStatusLabel('Online')).toBe('En línea');
    expect(component.instanceStatusLabel('Offline')).toBe('Desconectada');
    expect(component.triggerTypeLabel('Recovery')).toBe('Recuperación');
    expect(component.triggerTypeLabel('Misfire')).toBe('Ejecución perdida');
    expect(component.synchronizationStatusLabel('Unexpected')).toBe('Estado de sincronización no reconocido');
    expect(component.taskResultLabel('Succeeded')).toBe('Exitosa');
    expect(component.taskResultLabel('Unexpected')).toBe('Resultado no reconocido');
  });

  it('muestra los textos traducidos en los diálogos', () => {
    component.openManual(task);
    component.openSchedule(task);
    component.showDetail(task);
    fixture.detectChanges();
    const text = fixture.nativeElement.textContent as string;
    expect(text).toContain('Identificador de solicitud');
    expect(text).toContain('Acción ante una ejecución perdida');
    expect(text).toContain('Expresión cron');
    expect(text).toContain('Recuperación automática');
    expect(text).not.toContain('DoNothing');
    expect(text).not.toContain('FireAndProceed');
  });

  it('exige motivo y evita doble envio mientras la solicitud esta en curso', () => {
    const pending = new Subject<{ outcome: number; executionId: string; message: string }>();
    service.executeNow.and.returnValue(pending.asObservable());
    component.openManual(task);
    component.executeNow();
    expect(service.executeNow).not.toHaveBeenCalled();

    component.manualForm.patchValue({ reason: 'Reproceso autorizado por Operaciones' });
    component.executeNow();
    component.executeNow();

    expect(service.executeNow).toHaveBeenCalledTimes(1);
    expect(component.submittingManual).toBeTrue();
    pending.next({ outcome: 0, executionId: crypto.randomUUID(), message: 'Aceptada' });
    pending.complete();
  });

  it('presenta errores de tareas sin convertirlos en una lista vacía válida', () => {
    component.tasks = [];
    component.tasksLoaded = false;
    service.getSchedulerTasks.and.returnValue(throwError(() => new HttpErrorResponse({
      status: 409,
      error: { message: 'La tarea ya tiene una ejecucion activa.' }
    })));

    component.load();

    expect(component.tasksError).toBe('La tarea ya tiene una ejecucion activa.');
    expect(component.tasksError).not.toContain('[object Object]');
    expect(component.tasksLoaded).toBeFalse();
  });

  it('conserva tareas e instancias cuando falla únicamente el historial', () => {
    service.getHistory.and.returnValue(throwError(() => new HttpErrorResponse({
      status: 503,
      error: { title: 'Historial temporalmente no disponible' }
    })));

    component.load();
    fixture.detectChanges();

    expect(component.tasks).toEqual([task]);
    expect(component.instances).toHaveSize(1);
    expect(component.tasksLoaded).toBeTrue();
    expect(component.instancesLoaded).toBeTrue();
    expect(component.historyError).toBe('Historial temporalmente no disponible');
    const text = fixture.nativeElement.textContent as string;
    expect(text).toContain('Programador de ciclos');
    expect(text).toContain('api-01');
    expect(text).toContain('No se pudo cargar el historial');
  });

  it('distingue HTTP 429, respeta Retry-After y bloquea la recarga inmediata', () => {
    service.getHistory.and.returnValue(throwError(() => new HttpErrorResponse({
      status: 429,
      headers: new HttpHeaders({ 'Retry-After': '3' })
    })));

    component.load();

    expect(component.historyError).toContain('límite de solicitudes');
    expect(component.historyError).toContain('3 segundos');
    expect(component.reloadBlocked).toBeTrue();
  });

  it('distingue una sesión no autorizada', () => {
    service.getOverview.and.returnValue(throwError(() => new HttpErrorResponse({ status: 401 })));

    component.load();

    expect(component.overviewError).toContain('sesión no está autorizada');
  });
});

import { HttpErrorResponse } from '@angular/common/http';
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
    endAt: null
  };

  beforeEach(async () => {
    service = jasmine.createSpyObj<TaskDefinitionsService>('TaskDefinitionsService', [
      'getOverview', 'getSchedulerTasks', 'getInstances', 'getHistory', 'executeNow',
      'pause', 'resume', 'updateSchedule', 'previewSchedule'
    ]);
    service.getOverview.and.returnValue(of({
      totalInstances: 2, activeInstances: 2, offlineInstances: 0, runningJobs: 0,
      upcomingExecutions: 1, recentFailures: 0, recentMisfires: 0,
      schedulerName: 'ACHInterbankScheduler', persistentStore: true, clustered: true
    }));
    service.getSchedulerTasks.and.returnValue(of([task]));
    service.getInstances.and.returnValue(of([]));
    service.getHistory.and.returnValue(of({ items: [], page: 1, pageSize: 25, total: 0 }));

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
    expect(text).not.toContain('[object Object]');
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

  it('presenta errores funcionales sin serializar objetos', () => {
    service.getSchedulerTasks.and.returnValue(throwError(() => new HttpErrorResponse({
      status: 409,
      error: { message: 'La tarea ya tiene una ejecucion activa.' }
    })));

    component.load();

    expect(component.loadError).toBe('La tarea ya tiene una ejecucion activa.');
    expect(component.loadError).not.toContain('[object Object]');
  });
});

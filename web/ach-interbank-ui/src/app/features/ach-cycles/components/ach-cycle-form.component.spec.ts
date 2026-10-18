import { ComponentFixture, TestBed } from '@angular/core/testing';
import { ActivatedRoute, Router } from '@angular/router';
import { Subject, of, throwError } from 'rxjs';
import { AchCycleFormComponent } from './ach-cycle-form.component';
import { AchCyclesApiService } from '../../../core/services/ach-cycles-api.service';
import { AchCycleConfigurationOption, AchCycleSummary } from '../../../core/models/ach-cycle.model';
import { ClearingHousesApiService } from '../../../core/services/clearing-houses-api.service';

describe('AchCycleFormComponent', () => {
  let fixture: ComponentFixture<AchCycleFormComponent>;
  let component: AchCycleFormComponent;
  let cyclesApi: jasmine.SpyObj<AchCyclesApiService>;
  let clearingHousesApi: jasmine.SpyObj<ClearingHousesApiService>;
  let router: jasmine.SpyObj<Router>;
  let routeId: string | null;

  const configuration: AchCycleConfigurationOption = {
    id: 42,
    clearingHouseId: 7,
    clearingHouseName: 'Cámara de prueba',
    cycleName: 'Ciclo canónico',
    startTime: '08:00:00',
    endTime: '10:00:00',
    cutoffTime: '09:45:00',
    isActive: true,
    effectiveFrom: '2026-01-01T00:00:00Z',
    effectiveTo: null,
    isCurrent: true
  };

  const existingCycle: AchCycleSummary = {
    id: 'cycle-9',
    cycleName: 'Ciclo canónico',
    clearingHouseId: 7,
    clearingHouseName: 'Cámara de prueba',
    processingDate: '2026-07-22T00:00:00Z',
    startTime: '08:00:00',
    endTime: '10:00:00',
    cutoffTime: '09:45:00',
    rescheduleOnHoliday: true,
    clearingHouseCycleConfigId: 42,
    operationalStatus: 'Activo'
  };

  beforeEach(async () => {
    routeId = null;
    cyclesApi = jasmine.createSpyObj<AchCyclesApiService>('AchCyclesApiService', [
      'getById',
      'getCurrentConfigurations',
      'create',
      'update'
    ]);
    cyclesApi.getCurrentConfigurations.and.returnValue(of([configuration]));
    cyclesApi.create.and.returnValue(of(existingCycle));
    cyclesApi.update.and.returnValue(of(existingCycle));

    clearingHousesApi = jasmine.createSpyObj<ClearingHousesApiService>('ClearingHousesApiService', ['list']);
    clearingHousesApi.list.and.returnValue(of([{ id: 7, code: 'TEST', name: 'Cámara de prueba' }]));
    router = jasmine.createSpyObj<Router>('Router', ['navigate']);

    await TestBed.configureTestingModule({
      imports: [AchCycleFormComponent],
      providers: [
        { provide: AchCyclesApiService, useValue: cyclesApi },
        { provide: ClearingHousesApiService, useValue: clearingHousesApi },
        {
          provide: ActivatedRoute,
          useValue: { snapshot: { paramMap: { get: (key: string) => key === 'id' ? routeId : null } } }
        },
        { provide: Router, useValue: router }
      ]
    }).compileComponents();
  });

  function createComponent(id: string | null = null): void {
    routeId = id;
    fixture = TestBed.createComponent(AchCycleFormComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  }

  function selectValidContext(): void {
    component.form.patchValue({ clearingHouseId: 7, processingDate: '2026-07-22' });
    component.configurationContextChanged();
    component.form.controls.clearingHouseCycleConfigId.setValue(42);
    fixture.detectChanges();
  }

  it('carga cámaras al iniciar', () => {
    createComponent();

    expect(clearingHousesApi.list).toHaveBeenCalledTimes(1);
    expect(component.clearingHouses).toEqual([{ id: 7, code: 'TEST', name: 'Cámara de prueba' }]);
  });

  it('crea con el payload canónico de la configuración vigente y navega a la lista', () => {
    createComponent();
    selectValidContext();
    component.form.controls.rescheduleOnHoliday.setValue(true);

    component.save();

    expect(cyclesApi.getCurrentConfigurations).toHaveBeenCalledWith(7, '2026-07-22');
    expect(cyclesApi.create).toHaveBeenCalledWith({
      clearingHouseId: 7,
      clearingHouseCycleConfigId: 42,
      cycleName: 'Ciclo canónico',
      processingDate: '2026-07-22',
      startTime: '08:00:00',
      endTime: '10:00:00',
      cutoffTime: '09:45:00',
      rescheduleOnHoliday: true
    });
    expect(cyclesApi.update).not.toHaveBeenCalled();
    expect(router.navigate).toHaveBeenCalledWith(['/ach-cycles']);
  });

  it('edita, aplica el patch, conserva la configuración seleccionada y usa update', () => {
    cyclesApi.getById.and.returnValue(of(existingCycle));
    createComponent('cycle-9');

    expect(cyclesApi.getById).toHaveBeenCalledWith('cycle-9');
    expect(cyclesApi.getCurrentConfigurations).toHaveBeenCalledWith(7, '2026-07-22');
    expect(component.isEdit).toBeTrue();
    expect(component.form.getRawValue()).toEqual({
      clearingHouseId: 7,
      processingDate: '2026-07-22',
      clearingHouseCycleConfigId: 42,
      rescheduleOnHoliday: true
    });

    component.save();

    expect(cyclesApi.update).toHaveBeenCalledWith('cycle-9', {
      clearingHouseId: 7,
      clearingHouseCycleConfigId: 42,
      cycleName: 'Ciclo canónico',
      processingDate: '2026-07-22',
      startTime: '08:00:00',
      endTime: '10:00:00',
      cutoffTime: '09:45:00',
      rescheduleOnHoliday: true
    });
    expect(cyclesApi.create).not.toHaveBeenCalled();
    expect(router.navigate).toHaveBeenCalledWith(['/ach-cycles']);
  });

  it('conserva solo configuraciones activas y vigentes', () => {
    const inactive = { ...configuration, id: 43, isActive: false };
    const notCurrent = { ...configuration, id: 44, isCurrent: false };
    cyclesApi.getCurrentConfigurations.and.returnValue(of([configuration, inactive, notCurrent]));
    createComponent();

    component.form.patchValue({ clearingHouseId: 7, processingDate: '2026-07-22' });
    component.configurationContextChanged();

    expect(component.configurations).toEqual([configuration]);
    expect(component.configurationLabel(configuration)).toContain('Ciclo canónico');
    expect(component.configurationLabel(configuration)).toContain('08:00–10:00');
  });

  it('limpia solo la configuración cuando cambia a un contexto incompatible', () => {
    createComponent();
    component.form.patchValue({
      clearingHouseId: 7,
      processingDate: '2026-07-22',
      clearingHouseCycleConfigId: 42,
      rescheduleOnHoliday: true
    });
    cyclesApi.getCurrentConfigurations.and.returnValue(of([]));

    component.configurationContextChanged();
    fixture.detectChanges();

    expect(component.form.controls.clearingHouseCycleConfigId.value).toBeNull();
    expect(component.form.controls.processingDate.value).toBe('2026-07-22');
    expect(component.form.controls.rescheduleOnHoliday.value).toBeTrue();
    expect(fixture.nativeElement.textContent).toContain('No hay configuraciones activas y vigentes');
  });

  it('muestra loading foundation mientras consulta configuraciones', () => {
    const configurations$ = new Subject<AchCycleConfigurationOption[]>();
    cyclesApi.getCurrentConfigurations.and.returnValue(configurations$);
    createComponent();
    component.form.patchValue({ clearingHouseId: 7, processingDate: '2026-07-22' });

    component.configurationContextChanged();
    fixture.detectChanges();

    expect(component.isLoadingConfigurations).toBeTrue();
    expect(fixture.nativeElement.querySelector('ui-estado-carga')).not.toBeNull();

    configurations$.next([configuration]);
    configurations$.complete();
    fixture.detectChanges();

    expect(component.isLoadingConfigurations).toBeFalse();
    expect(fixture.nativeElement.querySelector('ui-estado-carga')).toBeNull();
  });

  it('conserva el formulario y muestra el error backend sin object Object', () => {
    createComponent();
    selectValidContext();
    cyclesApi.create.and.returnValue(
      throwError(() => ({ error: { detail: 'La configuración está fuera de vigencia.' } }))
    );

    component.save();
    fixture.detectChanges();

    expect(component.form.controls.clearingHouseCycleConfigId.value).toBe(42);
    const alert = fixture.nativeElement.querySelector('[role="alert"]') as HTMLElement;
    expect(alert).not.toBeNull();
    expect(alert.textContent).toContain('La configuración está fuera de vigencia.');
    expect(alert.querySelector('ui-estado-error')).not.toBeNull();
    expect(fixture.nativeElement.textContent).not.toContain('[object Object]');
  });

  it('bloquea un segundo save mientras el primero sigue en curso', () => {
    const save$ = new Subject<AchCycleSummary>();
    cyclesApi.create.and.returnValue(save$);
    createComponent();
    selectValidContext();

    component.save();
    component.save();

    expect(component.isSaving).toBeTrue();
    expect(cyclesApi.create).toHaveBeenCalledTimes(1);
  });

  it('usa controles Material y primitives foundation', () => {
    createComponent();

    const root = fixture.nativeElement as HTMLElement;
    expect(root.querySelector('app-page-header')).not.toBeNull();
    expect(root.querySelectorAll('mat-form-field').length).toBe(3);
    expect(root.querySelectorAll('mat-select').length).toBe(2);
    expect(root.querySelector('mat-checkbox')).not.toBeNull();
    expect(root.querySelector('input[type="date"]')).not.toBeNull();
    expect(root.querySelectorAll('button').length).toBe(2);
    expect(root.querySelector('select')).toBeNull();
    expect(root.querySelector('input[type="checkbox"]:not(.mdc-checkbox__native-control)')).toBeNull();
  });
});

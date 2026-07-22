import { ComponentFixture, TestBed } from '@angular/core/testing';
import { ActivatedRoute, Router } from '@angular/router';
import { of, throwError } from 'rxjs';
import { AchCycleFormComponent } from './ach-cycle-form.component';
import { AchCyclesApiService, ClearingHousesApiService } from '../services/ach-cycles-api.service';
import { AchCycleConfigurationOption } from '../models/ach-cycle.model';

describe('AchCycleFormComponent', () => {
  let fixture: ComponentFixture<AchCycleFormComponent>;
  let component: AchCycleFormComponent;
  let cyclesApi: jasmine.SpyObj<AchCyclesApiService>;

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

  beforeEach(async () => {
    cyclesApi = jasmine.createSpyObj<AchCyclesApiService>('AchCyclesApiService', [
      'getById',
      'getCurrentConfigurations',
      'create',
      'update'
    ]);
    cyclesApi.getCurrentConfigurations.and.returnValue(of([configuration]));
    cyclesApi.create.and.returnValue(of({} as never));

    const clearingHousesApi = jasmine.createSpyObj<ClearingHousesApiService>('ClearingHousesApiService', ['list']);
    clearingHousesApi.list.and.returnValue(of([{ id: 7, code: 'TEST', name: 'Cámara de prueba' }]));

    await TestBed.configureTestingModule({
      imports: [AchCycleFormComponent],
      providers: [
        { provide: AchCyclesApiService, useValue: cyclesApi },
        { provide: ClearingHousesApiService, useValue: clearingHousesApi },
        { provide: ActivatedRoute, useValue: { snapshot: { paramMap: { get: () => null } } } },
        { provide: Router, useValue: { navigate: jasmine.createSpy('navigate') } }
      ]
    }).compileComponents();

    fixture = TestBed.createComponent(AchCycleFormComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('carga configuraciones vigentes y envía el identificador canónico', () => {
    component.form.patchValue({ clearingHouseId: 7, processingDate: '2026-07-22' });
    component.configurationContextChanged();
    component.form.controls.clearingHouseCycleConfigId.setValue(42);
    fixture.detectChanges();

    const selector = fixture.nativeElement.querySelector(
      'select[formControlName="clearingHouseCycleConfigId"]'
    ) as HTMLSelectElement;
    expect(selector.textContent).toContain('Ciclo canónico');
    expect(selector.textContent).toContain('08:00–10:00');

    component.save();

    expect(cyclesApi.create).toHaveBeenCalledWith(jasmine.objectContaining({
      clearingHouseId: 7,
      clearingHouseCycleConfigId: 42,
      cycleName: 'Ciclo canónico',
      processingDate: '2026-07-22'
    }));
  });

  it('limpia solo la configuración cuando cambia a un contexto incompatible', () => {
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

  it('conserva el formulario y muestra un error funcional sin object Object', () => {
    component.form.patchValue({ clearingHouseId: 7, processingDate: '2026-07-22' });
    component.configurationContextChanged();
    component.form.controls.clearingHouseCycleConfigId.setValue(42);
    cyclesApi.create.and.returnValue(throwError(() => ({ error: { detail: 'La configuración está fuera de vigencia.' } })));

    component.save();
    fixture.detectChanges();

    expect(component.form.controls.clearingHouseCycleConfigId.value).toBe(42);
    expect(fixture.nativeElement.querySelector('[role="alert"]').textContent).toContain('fuera de vigencia');
    expect(fixture.nativeElement.textContent).not.toContain('[object Object]');
  });
});

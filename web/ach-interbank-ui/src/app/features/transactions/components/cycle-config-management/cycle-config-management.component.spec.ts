import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideNoopAnimations } from '@angular/platform-browser/animations';
import { ActivatedRoute, convertToParamMap, provideRouter } from '@angular/router';
import { BehaviorSubject, of, throwError } from 'rxjs';
import { AuthService } from '../../../../core/services/auth.service';
import { ClearingHousesService } from '../../../clearing-houses/clearing-houses.service';
import { ClearingHouseCycleConfigsApiService } from '../../services/clearing-house-cycle-configs-api.service';
import { CycleConfigManagementComponent } from './cycle-config-management.component';

describe('CycleConfigManagementComponent', () => {
  let fixture: ComponentFixture<CycleConfigManagementComponent>;
  let component: CycleConfigManagementComponent;
  let params$: BehaviorSubject<ReturnType<typeof convertToParamMap>>;
  let api: jasmine.SpyObj<ClearingHouseCycleConfigsApiService>;
  let houses: jasmine.SpyObj<ClearingHousesService>;

  beforeEach(async () => {
    params$ = new BehaviorSubject(convertToParamMap({ id: '7' }));
    api = jasmine.createSpyObj('ClearingHouseCycleConfigsApiService', ['getByClearingHouse', 'createVersion', 'inactivate']);
    houses = jasmine.createSpyObj('ClearingHousesService', ['get']);
    houses.get.and.returnValue(of(house(7, 'ACHCOL')));
    api.getByClearingHouse.and.returnValue(of([cycle()]));
    api.createVersion.and.returnValue(of(cycle()));
    api.inactivate.and.returnValue(of({ ...cycle(), isActive: false }));

    await TestBed.configureTestingModule({
      imports: [CycleConfigManagementComponent],
      providers: [
        provideRouter([]),
        provideNoopAnimations(),
        { provide: ActivatedRoute, useValue: { paramMap: params$.asObservable() } },
        { provide: AuthService, useValue: { hasPermission: () => true } },
        { provide: ClearingHousesService, useValue: houses },
        { provide: ClearingHouseCycleConfigsApiService, useValue: api }
      ]
    }).compileComponents();

    fixture = TestBed.createComponent(CycleConfigManagementComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('loads the fixed camera and cycles from the route id', () => {
    expect(houses.get).toHaveBeenCalledWith(7);
    expect(api.getByClearingHouse).toHaveBeenCalledWith(jasmine.objectContaining({ clearingHouseId: 7 }));
    expect(component.clearingHouse?.code).toBe('ACHCOL');
    expect(component.dataSource.data.length).toBe(1);
  });

  it('does not render a camera selector or legacy controls', () => {
    const element: HTMLElement = fixture.nativeElement;
    expect(element.querySelector('[formControlName="clearingHouseId"]')).toBeNull();
    expect(element.querySelector('ui-selector-buscable')).toBeNull();
    expect(element.querySelector('ui-grilla-empresarial')).toBeNull();
    expect(element.querySelector('app-confirm-dialog')).toBeNull();
    expect(element.querySelector('table[mat-table]')).not.toBeNull();
  });

  it('handles invalid ids without requesting data', () => {
    params$.next(convertToParamMap({ id: 'invalid' }));
    fixture.detectChanges();
    expect(component.error).toBe('La cámara indicada no es válida.');
    expect(component.loading).toBeFalse();
  });

  it('reloads when the route id changes', () => {
    houses.get.and.returnValue(of(house(8, 'CENIT')));
    params$.next(convertToParamMap({ id: '8' }));
    fixture.detectChanges();
    expect(houses.get).toHaveBeenCalledWith(8);
    expect(api.getByClearingHouse).toHaveBeenCalledWith(jasmine.objectContaining({ clearingHouseId: 8 }));
    expect(component.clearingHouse?.code).toBe('CENIT');
  });

  it('shows an HTTP error state', () => {
    houses.get.and.returnValue(throwError(() => ({ status: 404 })));
    params$.next(convertToParamMap({ id: '9' }));
    fixture.detectChanges();
    expect(component.loading).toBeFalse();
    expect(component.error).toContain('no existe');
  });

  it('filters by name, state and validity', () => {
    component.allItems = [cycle(), { ...cycle(), id: 2, cycleName: 'Nocturno', isActive: false }];
    component.filterForm.patchValue({ cycleName: 'ciclo', status: 'active', validity: 'all' });
    expect(component.dataSource.data.map(item => item.id)).toEqual([1]);
  });

  it('validates time order, cutoff and duplicate versions', () => {
    component.openCreateForm();
    component.form.patchValue({
      cycleName: 'Ciclo 1',
      startTime: '10:00',
      endTime: '09:00',
      cutoffTime: '11:00',
      effectiveFrom: new Date(2026, 0, 1)
    });
    expect(component.form.hasError('timeOrder')).toBeTrue();
    component.form.patchValue({ startTime: '08:00', endTime: '10:00', cutoffTime: '11:00' });
    expect(component.form.hasError('cutoffOutside')).toBeTrue();
    component.form.patchValue({ cutoffTime: '09:00' });
    component.save();
    expect(component.form.hasError('duplicate')).toBeTrue();
    expect(api.createVersion).not.toHaveBeenCalled();
  });

  it('creates a version using the route camera id', () => {
    component.openCreateForm();
    component.form.setValue({
      cycleName: 'Ciclo nuevo',
      startTime: '08:00',
      endTime: '10:00',
      cutoffTime: '09:30',
      effectiveFrom: new Date(2027, 0, 1)
    });
    component.save();
    expect(api.createVersion).toHaveBeenCalledWith(jasmine.objectContaining({
      clearingHouseId: 7,
      cycleName: 'Ciclo nuevo',
      startTime: '08:00:00',
      endTime: '10:00:00',
      cutoffTime: '09:30:00'
    }));
  });

  it('preserves versioning, history and inactivation operations', () => {
    component.createVersion(cycle());
    expect(component.editingSource?.id).toBe(1);
    component.allItems = [cycle(), { ...cycle(), id: 2, effectiveFrom: '2025-01-01T00:00:00Z' }];
    component.historyCycleName = 'Ciclo 1';
    expect(component.historyItems.length).toBe(2);
    (component as any).inactivate(cycle());
    expect(api.inactivate).toHaveBeenCalledWith(1, jasmine.objectContaining({ effectiveTo: jasmine.any(String) }));
  });

  function house(id: number, code: string): any {
    return { id, code, name: code, isActive: true, isReady: true };
  }

  function cycle(): any {
    return {
      id: 1, clearingHouseId: 7, clearingHouseName: 'ACH Colombia', cycleName: 'Ciclo 1',
      startTime: '08:00:00', endTime: '10:00:00', cutoffTime: '09:30:00', isActive: true,
      effectiveFrom: '2026-01-01T00:00:00Z', effectiveTo: null, isCurrent: true
    };
  }
});

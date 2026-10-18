import { ComponentFixture, TestBed } from '@angular/core/testing';
import { MatDialog } from '@angular/material/dialog';
import { provideNoopAnimations } from '@angular/platform-browser/animations';
import { ActivatedRoute, convertToParamMap, provideRouter } from '@angular/router';
import { BehaviorSubject, of, Subject, throwError } from 'rxjs';
import { AuthService } from '../../../../core/services/auth.service';
import { ClearingHousesService } from '../../../../core/services/clearing-houses.service';
import { ClearingHouseCycleConfigsApiService } from '../../../../core/services/clearing-house-cycle-configs-api.service';
import { OperationalConfirmDialogComponent } from '../../../../shared/components/operational-confirm-dialog.component';
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
    expect(element.querySelector('app-page-header')).not.toBeNull();
    expect(element.querySelector('table[mat-table]')).not.toBeNull();
    expect(component.canManage).toBeTrue();
  });

  it('handles invalid ids without requesting data', () => {
    houses.get.calls.reset();
    api.getByClearingHouse.calls.reset();
    params$.next(convertToParamMap({ id: 'invalid' }));
    fixture.detectChanges();
    expect(component.error).toBe('La cámara indicada no es válida.');
    expect(component.loading).toBeFalse();
    expect(houses.get).not.toHaveBeenCalled();
    expect(api.getByClearingHouse).not.toHaveBeenCalled();
    expect(fixture.nativeElement.querySelector('ui-estado-error')).not.toBeNull();
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
    expect(fixture.nativeElement.querySelector('ui-estado-error')).not.toBeNull();
  });

  it('uses the shared loading and empty states', () => {
    const pendingHouse = new Subject<any>();
    houses.get.and.returnValue(pendingHouse.asObservable());
    api.getByClearingHouse.and.returnValue(of([]));

    params$.next(convertToParamMap({ id: '10' }));
    fixture.detectChanges();
    expect(component.loading).toBeTrue();
    expect(fixture.nativeElement.querySelector('ui-estado-carga')).not.toBeNull();

    pendingHouse.next(house(10, 'VACIA'));
    pendingHouse.complete();
    fixture.detectChanges();
    expect(component.loading).toBeFalse();
    expect(fixture.nativeElement.querySelector('ui-estado-vacio')).not.toBeNull();
  });

  it('filters by name, state and validity', () => {
    component.allItems = [cycle(), { ...cycle(), id: 2, cycleName: 'Nocturno', isActive: false }];
    component.filterForm.patchValue({ cycleName: 'ciclo', status: 'active', validity: 'all' });
    expect(component.dataSource.data.map(item => item.id)).toEqual([1]);
  });

  it('validates stage order, cutoff and duplicate versions', () => {
    component.openCreateForm();
    component.form.patchValue({
      cycleName: 'Ciclo 1',
      startTime: '10:00',
      endTime: '10:00',
      cutoffTime: '11:00',
      outputReleaseTime: '10:00',
      effectiveFrom: new Date(2026, 0, 1)
    });
    component.form.patchValue({ endTime: '10:00', startTime: '10:00' });
    expect(component.form.hasError('timeOrder')).toBeTrue();
    component.form.patchValue({ startTime: '08:00', endTime: '10:00', cutoffTime: '11:00' });
    expect(component.form.hasError('cutoffOutside')).toBeTrue();
    component.form.patchValue({ cutoffTime: '09:00' });
    component.save();
    expect(component.form.hasError('duplicate')).toBeTrue();
    expect(api.createVersion).not.toHaveBeenCalled();
  });

  it('rejects a new version that overlaps a closed historical version', () => {
    component.allItems = [{
      ...cycle(),
      isActive: false,
      effectiveFrom: '2026-01-01T00:00:00Z',
      effectiveTo: '2026-01-31T00:00:00Z'
    }];
    component.openCreateForm();
    component.form.patchValue({
      cycleName: 'Ciclo 1',
      startTime: '08:00',
      endTime: '10:00',
      cutoffTime: '09:30',
      outputReleaseTime: '10:30',
      effectiveFrom: new Date(2026, 0, 15)
    });

    component.save();

    expect(component.form.hasError('overlap')).toBeTrue();
    expect(api.createVersion).not.toHaveBeenCalled();
  });

  it('creates a version using the route camera id', () => {
    component.openCreateForm();
    component.form.setValue({
      cycleName: 'Ciclo nuevo',
      startTime: '08:00',
      endTime: '10:00',
      cutoffTime: '09:30',
      outputReleaseTime: '10:30',
      compensationStatementStartTime: '',
      compensationStatementEndTime: '',
      compensationPaymentStartTime: '',
      compensationPaymentEndTime: '',
      outputReleaseStartTime: '',
      outputReleaseEndTime: '',
      allowsMonetaryCredit: true,
      allowsMonetaryDebit: true,
      allowsCreditPrenotification: true,
      allowsDebitPrenotification: true,
      allowsReturn: true,
      allowsReturnOfReturn: true,
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


  it('validates complete expanded stages in close-plan-payment-release order', () => {
    component.openCreateForm();
    component.form.patchValue({
      cycleName: 'Ciclo etapas',
      startTime: '08:00',
      endTime: '10:00',
      cutoffTime: '10:00',
      outputReleaseTime: '11:00',
      compensationStatementStartTime: '10:00',
      compensationStatementEndTime: '10:30',
      compensationPaymentStartTime: '10:15',
      compensationPaymentEndTime: '10:45',
      outputReleaseStartTime: '10:45',
      outputReleaseEndTime: '11:00'
    });
    expect(component.form.hasError('stageOrder')).toBeTrue();

    component.form.patchValue({ compensationPaymentStartTime: '10:30' });
    expect(component.form.hasError('stageOrder')).toBeFalse();

    component.form.patchValue({ outputReleaseEndTime: '' });
    expect(component.form.hasError('incompleteStages')).toBeTrue();
  });

  it('uses the shared destructive confirmation before inactivation and respects cancel', () => {
    const dialog = (component as any).dialog as MatDialog;
    const open = spyOn(dialog, 'open').and.returnValue({ afterClosed: () => of(false) } as any);

    component.askInactivate(cycle());

    expect(open).toHaveBeenCalledWith(OperationalConfirmDialogComponent, {
      data: jasmine.objectContaining({
        title: 'Inactivar configuración',
        confirmLabel: 'Sí, inactivar',
        icon: 'event_busy',
        destructive: true
      }),
      width: 'min(520px, calc(100vw - 2rem))'
    });
    expect(api.inactivate).not.toHaveBeenCalled();
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
      policyVersion: 'V1', startTime: '08:00:00', endTime: '10:00:00', cutoffTime: '09:30:00',
      outputReleaseTime: '10:30:00', allowsMonetaryCredit: true, allowsMonetaryDebit: true,
      allowsCreditPrenotification: true, allowsDebitPrenotification: true, allowsReturn: true,
      allowsReturnOfReturn: true, isActive: true,
      effectiveFrom: '2026-01-01T00:00:00Z', effectiveTo: null, isCurrent: true
    };
  }
});

import { ComponentFixture, TestBed } from '@angular/core/testing';
import { of, throwError } from 'rxjs';
import { NotificationService } from '../../../../core/services/notification.service';
import { ClearingHousesApiService } from '../../../ach-cycles/services/ach-cycles-api.service';
import { ClearingHouseCycleConfigsApiService } from '../../services/clearing-house-cycle-configs-api.service';
import { CycleConfigManagementComponent } from './cycle-config-management.component';

describe('CycleConfigManagementComponent', () => {
  let fixture: ComponentFixture<CycleConfigManagementComponent>;
  let component: CycleConfigManagementComponent;
  let api: jasmine.SpyObj<ClearingHouseCycleConfigsApiService>;
  let housesApi: jasmine.SpyObj<ClearingHousesApiService>;
  let notifications: jasmine.SpyObj<NotificationService>;

  beforeEach(async () => {
    api = jasmine.createSpyObj<ClearingHouseCycleConfigsApiService>('ClearingHouseCycleConfigsApiService', ['getByClearingHouse', 'createVersion', 'inactivate']);
    housesApi = jasmine.createSpyObj<ClearingHousesApiService>('ClearingHousesApiService', ['list']);
    notifications = jasmine.createSpyObj<NotificationService>('NotificationService', ['success', 'warning', 'error']);

    housesApi.list.and.returnValue(of([{ id: 1, name: 'ACH Colombia', code: 'ACHCOL' } as any]));
    api.getByClearingHouse.and.returnValue(of([]));
    api.createVersion.and.returnValue(of({} as any));
    api.inactivate.and.returnValue(of({} as any));

    await TestBed.configureTestingModule({
      imports: [CycleConfigManagementComponent],
      providers: [
        { provide: ClearingHouseCycleConfigsApiService, useValue: api },
        { provide: ClearingHousesApiService, useValue: housesApi },
        { provide: NotificationService, useValue: notifications }
      ]
    }).compileComponents();

    fixture = TestBed.createComponent(CycleConfigManagementComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('renders form when openCreateForm is called', () => {
    component.openCreateForm();
    fixture.detectChanges();

    expect(component.showForm).toBeTrue();
    expect(component.editingSource).toBeNull();
  });

  it('resolves nested action icon clicks for edit, clone and inactivate', () => {
    const item = {
      id: 99,
      clearingHouseId: 1,
      clearingHouseName: 'ACH Colombia',
      cycleName: 'Ciclo-Original',
      startTime: '08:00:00',
      endTime: '09:00:00',
      cutoffTime: '09:00:00',
      isActive: true,
      effectiveFrom: '2026-01-01T00:00:00Z',
      effectiveTo: null,
      isCurrent: true
    } as any;
    const cdr = (component as any).cdr as { markForCheck: jasmine.Spy };
    const markForCheckSpy = spyOn(cdr, 'markForCheck').and.callThrough();

    const actionColumn = component.columnDefs.find((column) => column.headerName === 'Acciones');
    expect(actionColumn?.onCellClicked).toBeDefined();
    const onCellClicked = actionColumn?.onCellClicked as NonNullable<typeof actionColumn.onCellClicked>;

    const editIcon = createActionIcon('edit');
    onCellClicked({ event: { target: editIcon }, data: item } as any);

    expect(component.showForm).toBeTrue();
    expect(component.editingSource).toBe(item);
    expect(component.form.controls.cycleName.value).toBe('Ciclo-Original');
    expect(markForCheckSpy).toHaveBeenCalledTimes(1);

    const cloneIcon = createActionIcon('clone');
    onCellClicked({ event: { target: cloneIcon }, data: item } as any);

    expect(component.form.controls.cycleName.value).toBe('Ciclo-Original-V2');
    expect(markForCheckSpy).toHaveBeenCalledTimes(3);

    const askInactivateSpy = spyOn(component, 'askInactivate').and.callThrough();
    const inactivateIcon = createActionIcon('inactivate');
    onCellClicked({ event: { target: inactivateIcon }, data: item } as any);

    expect(askInactivateSpy).toHaveBeenCalledWith(item);
    expect(markForCheckSpy).toHaveBeenCalledTimes(4);
  });

  it('loads grid results after search', () => {
    api.getByClearingHouse.and.returnValue(of([
      {
        id: 10,
        clearingHouseId: 1,
        clearingHouseName: 'ACH Colombia',
        cycleName: 'Ciclo 1',
        startTime: '08:00:00',
        endTime: '10:00:00',
        cutoffTime: '10:00:00',
        isActive: true,
        effectiveFrom: '2026-01-01T00:00:00Z',
        effectiveTo: null,
        isCurrent: true
      }
    ] as any));

    component.filterForm.patchValue({ clearingHouseId: 1 });
    component.search();

    expect(component.visibleItems.length).toBe(1);
  });

  it('shows warning and blocks save when schedule validation fails', () => {
    component.openCreateForm();
    component.form.patchValue({
      clearingHouseId: 1,
      cycleName: 'Ciclo-X',
      startTime: '10:00',
      endTime: '09:00',
      cutoffTime: '09:00',
      effectiveFrom: '2026-01-01'
    });

    component.save();

    expect(notifications.warning).toHaveBeenCalled();
    expect(api.createVersion).not.toHaveBeenCalled();
  });

  it('supports clone and inactivate actions', () => {
    const item = {
      id: 99,
      clearingHouseId: 1,
      clearingHouseName: 'ACH Colombia',
      cycleName: 'Ciclo-Original',
      startTime: '08:00:00',
      endTime: '09:00:00',
      cutoffTime: '09:00:00',
      isActive: true,
      effectiveFrom: '2026-01-01T00:00:00Z',
      effectiveTo: null,
      isCurrent: true
    } as any;
    const cdr = (component as any).cdr as { markForCheck: jasmine.Spy };
    const markForCheckSpy = spyOn(cdr, 'markForCheck').and.callThrough();

    component.clone(item);
    expect(component.form.controls.cycleName.value).toContain('-V2');
    expect(markForCheckSpy).toHaveBeenCalledTimes(2);

    component.askInactivate(item);
    expect(markForCheckSpy).toHaveBeenCalledTimes(3);
    expect(component.selectedForInactivation).toBe(item);
  });

  it('reports API errors in search', () => {
    api.getByClearingHouse.and.returnValue(throwError(() => new Error('boom')));
    component.filterForm.patchValue({ clearingHouseId: 1 });

    component.search();

    expect(notifications.error).toHaveBeenCalled();
    expect(component.loadError).toContain('No fue posible consultar configuraciones de ciclos');
  });

  function createActionIcon(action: string): HTMLElement {
    const button = document.createElement('button');
    button.setAttribute('data-action', action);
    const icon = document.createElement('span');
    button.appendChild(icon);
    return icon;
  }
});

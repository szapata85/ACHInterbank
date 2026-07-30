import { NoopAnimationsModule } from '@angular/platform-browser/animations';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { MatDialog } from '@angular/material/dialog';
import { of, Subject, throwError } from 'rxjs';
import { NotificationService } from '../../../core/services/notification.service';
import { ClearingHousesApiService } from '../../ach-cycles/services/ach-cycles-api.service';
import { ClearingHouseOption } from '../../ach-cycles/models/ach-cycle.model';
import { FinancialInstitutionsApiService } from '../../transactions/services/financial-institutions-api.service';
import { DestinationInstitution } from '../../transactions/transactions.models';
import { FinancialInstitutionStatusEnum } from '../../transactions/transactions.types';
import { InstitutionClearingHousePreference } from '../models/institution-clearing-house-preference.model';
import { InstitutionClearingHousePreferencesService } from '../services/institution-clearing-house-preferences.service';
import { ClearingHousePreferencesComponent } from './clearing-house-preferences.component';

describe('ClearingHousePreferencesComponent', () => {
  let fixture: ComponentFixture<ClearingHousePreferencesComponent>;
  let component: ClearingHousePreferencesComponent;
  let service: jasmine.SpyObj<InstitutionClearingHousePreferencesService>;
  let institutionsApi: jasmine.SpyObj<FinancialInstitutionsApiService>;
  let clearingHousesApi: jasmine.SpyObj<ClearingHousesApiService>;
  let dialog: jasmine.SpyObj<MatDialog>;
  let notifications: jasmine.SpyObj<NotificationService>;

  const defaultSource: DestinationInstitution = {
    id: 77,
    name: 'Origen ACH configurable',
    routingNumber: '00001',
    transitCode: '283',
    checkDigit: '6',
    isDefaultSource: true,
    status: FinancialInstitutionStatusEnum.Active
  };
  const activeInstitution: DestinationInstitution = {
    id: 5,
    name: 'Banco activo',
    routingNumber: '00002',
    transitCode: '284',
    checkDigit: '5',
    isDefaultSource: false,
    status: FinancialInstitutionStatusEnum.Active
  };
  const inactiveInstitution: DestinationInstitution = {
    ...activeInstitution,
    id: 9,
    name: 'Banco inactivo',
    status: FinancialInstitutionStatusEnum.Inactive
  };
  const clearingHouses: ClearingHouseOption[] = [
    { id: 101, name: 'Cámara Z' },
    { id: 202, name: 'Cámara A' },
    { id: 303, name: 'Cámara adicional' }
  ];
  const preference: InstitutionClearingHousePreference = {
    id: 41,
    financialInstitutionId: 77,
    financialInstitutionName: defaultSource.name,
    clearingHouseId: 303,
    clearingHouseName: 'Cámara adicional',
    priority: 2,
    isDefault: true,
    isActive: true
  };

  beforeEach(async () => {
    service = jasmine.createSpyObj<InstitutionClearingHousePreferencesService>(
      'InstitutionClearingHousePreferencesService',
      ['list', 'create', 'update', 'delete']
    );
    service.list.and.returnValue(of([preference]));

    institutionsApi = jasmine.createSpyObj<FinancialInstitutionsApiService>(
      'FinancialInstitutionsApiService',
      ['getAll']
    );
    institutionsApi.getAll.and.returnValue(
      of([activeInstitution, inactiveInstitution, defaultSource])
    );

    clearingHousesApi = jasmine.createSpyObj<ClearingHousesApiService>(
      'ClearingHousesApiService',
      ['listAdministrative']
    );
    clearingHousesApi.listAdministrative.and.returnValue(of(clearingHouses));

    notifications = jasmine.createSpyObj<NotificationService>(
      'NotificationService',
      ['success', 'error']
    );

    await TestBed.configureTestingModule({
      imports: [ClearingHousePreferencesComponent, NoopAnimationsModule],
      providers: [
        { provide: InstitutionClearingHousePreferencesService, useValue: service },
        { provide: FinancialInstitutionsApiService, useValue: institutionsApi },
        { provide: ClearingHousesApiService, useValue: clearingHousesApi },
        { provide: NotificationService, useValue: notifications }
      ]
    }).compileComponents();

    fixture = TestBed.createComponent(ClearingHousePreferencesComponent);
    component = fixture.componentInstance;
    const injectedDialog = (
      component as unknown as { dialog: MatDialog }
    ).dialog;
    spyOn(injectedDialog, 'open');
    dialog = injectedDialog as jasmine.SpyObj<MatDialog>;
    fixture.detectChanges();
  });

  afterEach(() => fixture.destroy());

  it('loads the default source by its flag and every dynamic clearing house in API order', () => {
    expect(institutionsApi.getAll).toHaveBeenCalledTimes(1);
    expect(component.institutions.map((item) => item.id)).toEqual([5, 77]);
    expect(component.institutions.find((item) => item.isDefaultSource)?.id).toBe(77);
    expect(component.institutions.some((item) => item.id === 9)).toBeFalse();
    expect(component.clearingHouses).toEqual(clearingHouses);
    expect(component.clearingHouses.length).toBe(3);
    expect(component.pageSize).toBe(10);
  });

  it('shows a recoverable load error when preferences fail', () => {
    service.list.and.returnValue(
      throwError(() => ({ error: { detail: 'Preferencias no disponibles' } }))
    );

    component.loadPreferences();
    fixture.detectChanges();

    expect(component.loadError).toBeTrue();
    expect(component.loadErrorMessage).toBe('Preferencias no disponibles');
    expect(component.loading).toBeFalse();
    expect(fixture.nativeElement.textContent).toContain('Preferencias no disponibles');
  });

  it('exposes catalog loading errors without inventing clearing houses', () => {
    clearingHousesApi.listAdministrative.and.returnValue(
      throwError(() => ({ error: { detail: 'Catálogo de cámaras no disponible' } }))
    );

    component.loadCatalogs();

    expect(component.catalogsError).toBe('Catálogo de cámaras no disponible');
    expect(component.clearingHouses).toEqual([]);
    expect(component.institutions).toEqual([]);
    expect(component.catalogsLoading).toBeFalse();
  });

  it('marks required Material fields and does not create an invalid relation', () => {
    component.startCreate();

    component.create();
    fixture.detectChanges();

    expect(component.createForm.invalid).toBeTrue();
    expect(service.create).not.toHaveBeenCalled();
    expect(fixture.nativeElement.textContent).toContain('Selecciona una institución');
    expect(fixture.nativeElement.textContent).toContain('Selecciona una cámara compensadora');
  });

  it('creates a relation for a non-hardcoded default source and a third dynamic camera', () => {
    const created = { ...preference, priority: 1 };
    service.create.and.returnValue(of(created));
    component.startCreate();
    component.createForm.patchValue({
      financialInstitutionId: 77,
      clearingHouseId: 303,
      priority: 1,
      isDefault: true,
      isActive: true
    });

    component.create();

    expect(service.create).toHaveBeenCalledTimes(1);
    expect(service.create.calls.mostRecent().args[0]).toEqual({
      financialInstitutionId: 77,
      clearingHouseId: 303,
      priority: 1,
      isDefault: true,
      isActive: true
    });
    expect(component.editing?.id).toBe(41);
    expect(notifications.success).toHaveBeenCalledWith('Relación creada correctamente.');
  });

  it('prevents a duplicate create while the first request is pending', () => {
    const pending = new Subject<InstitutionClearingHousePreference>();
    service.create.and.returnValue(pending);
    component.startCreate();
    component.createForm.patchValue({
      financialInstitutionId: 77,
      clearingHouseId: 303,
      priority: 2
    });

    component.create();
    component.create();

    expect(service.create).toHaveBeenCalledTimes(1);
    expect(component.saving).toBeTrue();

    pending.error({ error: { detail: 'La relación ya existe' } });
    expect(component.saving).toBeFalse();
    expect(component.operationError).toBe('La relación ya existe');
    expect(component.showCreateForm).toBeTrue();
  });

  it('updates only editable preference fields and reports success', () => {
    const updated = { ...preference, priority: 3, isDefault: false };
    service.update.and.returnValue(of(updated));
    component.startEdit(preference);
    component.form.patchValue({ priority: 3, isDefault: false });

    component.save();

    expect(service.update).toHaveBeenCalledOnceWith(41, {
      id: 41,
      priority: 3,
      isDefault: false,
      isActive: true
    });
    expect(component.editing).toEqual(updated);
    expect(notifications.success).toHaveBeenCalledWith('Preferencia actualizada correctamente.');
  });

  it('changes status once after confirmation without opening an unrelated editor', () => {
    const pending = new Subject<InstitutionClearingHousePreference>();
    dialog.open.and.returnValue({ afterClosed: () => of(true) } as never);
    service.update.and.returnValue(pending);

    component.toggleActive(preference);
    component.toggleActive(preference);

    expect(dialog.open).toHaveBeenCalledTimes(1);
    expect(service.update).toHaveBeenCalledTimes(1);
    expect(service.update.calls.mostRecent().args).toEqual([
      41,
      {
        id: 41,
        priority: 2,
        isDefault: true,
        isActive: false
      }
    ]);
    expect(component.saving).toBeTrue();

    pending.next({ ...preference, isActive: false });
    pending.complete();

    expect(component.saving).toBeFalse();
    expect(component.editing).toBeNull();
    expect(notifications.success).toHaveBeenCalledWith('Relación inactivada correctamente.');
  });

  it('deletes only after a positive Material confirmation', () => {
    dialog.open.and.returnValue({ afterClosed: () => of(false) } as never);
    component.deletePreference(preference);
    expect(service.delete).not.toHaveBeenCalled();

    dialog.open.and.returnValue({ afterClosed: () => of(true) } as never);
    service.delete.and.returnValue(of(void 0));
    component.deletePreference(preference);

    expect(service.delete).toHaveBeenCalledOnceWith(41);
    expect(component.preferences).toEqual([]);
    expect(notifications.success).toHaveBeenCalledWith('Relación eliminada correctamente.');
  });
});

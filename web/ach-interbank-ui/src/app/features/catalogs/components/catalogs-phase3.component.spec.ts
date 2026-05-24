import { ComponentFixture, TestBed } from '@angular/core/testing';
import { ActivatedRoute } from '@angular/router';
import { of, throwError } from 'rxjs';
import { NotificationService } from '../../../core/services/notification.service';
import { CustomerThirdPartiesComponent } from '../../customer-third-parties/components/customer-third-parties.component';
import { CustomerThirdPartiesService } from '../../customer-third-parties/services/customer-third-parties.service';
import { BankHolidaysAdminService } from '../services/bank-holidays-admin.service';
import { CatalogTypesApiService } from '../services/catalog-types-api.service';
import { FinancialInstitutionAdminService } from '../services/financial-institution-admin.service';
import { BankHolidaysComponent } from './bank-holidays.component';
import { CatalogTypesAdminComponent } from './catalog-types-admin.component';
import { FinancialInstitutionsComponent } from './financial-institutions.component';

describe('Catalogos fase 3', () => {
  afterEach(() => TestBed.resetTestingModule());

  it('FinancialInstitutions_ShouldLoadAndExposeLegibleGridColumns', async () => {
    const service = jasmine.createSpyObj<FinancialInstitutionAdminService>('FinancialInstitutionAdminService', ['list']);
    service.list.and.returnValue(of([
      {
        id: 1,
        name: 'Cooperativa Financiera de Antioquia',
        routingNumber: '00001',
        transitCode: '001',
        checkDigit: '0',
        isDefaultSource: true,
        status: 1
      } as any
    ]));

    await TestBed.configureTestingModule({
      imports: [FinancialInstitutionsComponent],
      providers: [{ provide: FinancialInstitutionAdminService, useValue: service }]
    }).compileComponents();

    const fixture = TestBed.createComponent(FinancialInstitutionsComponent);
    fixture.detectChanges();

    const component = fixture.componentInstance;
    expect(component.institutions.length).toBe(1);
    expect(component.loadError).toBeFalse();
    expect(component.columnDefs.find((column) => column.field === 'name')?.minWidth).toBeGreaterThanOrEqual(200);
    expect(component.columnDefs.find((column) => column.colId === 'actions')?.minWidth).toBeGreaterThanOrEqual(180);
  });

  it('FinancialInstitutions_ShouldShowError_WhenApiFails', async () => {
    const service = jasmine.createSpyObj<FinancialInstitutionAdminService>('FinancialInstitutionAdminService', ['list']);
    service.list.and.returnValue(throwError(() => new Error('api')));

    await TestBed.configureTestingModule({
      imports: [FinancialInstitutionsComponent],
      providers: [{ provide: FinancialInstitutionAdminService, useValue: service }]
    }).compileComponents();

    const fixture = TestBed.createComponent(FinancialInstitutionsComponent);
    fixture.detectChanges();

    expect(fixture.componentInstance.loadError).toBeTrue();
    expect(fixture.componentInstance.loading).toBeFalse();
  });

  it('BankHolidays_ShouldLoad_WhenApiReturnsData', async () => {
    const service = jasmine.createSpyObj<BankHolidaysAdminService>('BankHolidaysAdminService', ['list']);
    service.list.and.returnValue(of([{ id: 1, date: '2026-01-01T00:00:00', description: 'Año Nuevo', countryCode: 'CO' }]));

    await TestBed.configureTestingModule({
      imports: [BankHolidaysComponent],
      providers: [{ provide: BankHolidaysAdminService, useValue: service }]
    }).compileComponents();

    const fixture = TestBed.createComponent(BankHolidaysComponent);
    fixture.detectChanges();

    expect(service.list).toHaveBeenCalled();
    expect(fixture.componentInstance.holidays.length).toBe(1);
    expect(fixture.componentInstance.loadError).toBeFalse();
  });

  it('BankHolidays_ShouldShowEmptyState_WhenNoData', async () => {
    const service = jasmine.createSpyObj<BankHolidaysAdminService>('BankHolidaysAdminService', ['list']);
    service.list.and.returnValue(of([]));

    await TestBed.configureTestingModule({
      imports: [BankHolidaysComponent],
      providers: [{ provide: BankHolidaysAdminService, useValue: service }]
    }).compileComponents();

    const fixture = TestBed.createComponent(BankHolidaysComponent);
    fixture.detectChanges();

    expect(fixture.componentInstance.holidays).toEqual([]);
    expect(fixture.componentInstance.hasSearched).toBeTrue();
    expect(fixture.componentInstance.loadError).toBeFalse();
  });

  it('BankHolidays_ShouldShowError_WhenApiFails', async () => {
    const service = jasmine.createSpyObj<BankHolidaysAdminService>('BankHolidaysAdminService', ['list']);
    service.list.and.returnValue(throwError(() => new Error('api')));

    await TestBed.configureTestingModule({
      imports: [BankHolidaysComponent],
      providers: [{ provide: BankHolidaysAdminService, useValue: service }]
    }).compileComponents();

    const fixture = TestBed.createComponent(BankHolidaysComponent);
    fixture.detectChanges();

    expect(fixture.componentInstance.loadError).toBeTrue();
    expect(fixture.componentInstance.loading).toBeFalse();
  });

  it('CatalogTypes_ShouldLoadRowsAndExposeActions', async () => {
    const api = jasmine.createSpyObj<CatalogTypesApiService>('CatalogTypesApiService', ['list']);
    api.list.and.returnValue(of([{ code: 'CC', name: 'Cédula de Ciudadanía', description: null }]));

    await TestBed.configureTestingModule({
      imports: [CatalogTypesAdminComponent],
      providers: [
        { provide: CatalogTypesApiService, useValue: api },
        { provide: NotificationService, useValue: jasmine.createSpyObj('NotificationService', ['success', 'error']) },
        {
          provide: ActivatedRoute,
          useValue: {
            snapshot: {
              data: { catalogType: 'document-types', title: 'Tipos de documento', subtitle: 'Administra documentos.' }
            }
          }
        }
      ]
    }).compileComponents();

    const fixture = TestBed.createComponent(CatalogTypesAdminComponent);
    fixture.detectChanges();

    expect(api.list).toHaveBeenCalledWith('document-types');
    expect(fixture.componentInstance.rows.length).toBe(1);
    expect(fixture.componentInstance.loadError).toBeFalse();
    expect(fixture.componentInstance.columnas.find((column) => column.headerName === 'Acciones')?.minWidth).toBeGreaterThanOrEqual(150);
  });

  it('CatalogTypes_ShouldShowError_WhenApiFails', async () => {
    const api = jasmine.createSpyObj<CatalogTypesApiService>('CatalogTypesApiService', ['list']);
    api.list.and.returnValue(throwError(() => new Error('api')));

    await TestBed.configureTestingModule({
      imports: [CatalogTypesAdminComponent],
      providers: [
        { provide: CatalogTypesApiService, useValue: api },
        { provide: NotificationService, useValue: jasmine.createSpyObj('NotificationService', ['success', 'error']) },
        { provide: ActivatedRoute, useValue: { snapshot: { data: { catalogType: 'transaction-codes' } } } }
      ]
    }).compileComponents();

    const fixture = TestBed.createComponent(CatalogTypesAdminComponent);
    fixture.detectChanges();

    expect(fixture.componentInstance.loadError).toBeTrue();
    expect(fixture.componentInstance.loading).toBeFalse();
  });

  it('CustomerThirdParties_ShouldLoadInitialPage', async () => {
    const service = jasmine.createSpyObj<CustomerThirdPartiesService>('CustomerThirdPartiesService', ['search']);
    service.search.and.returnValue(of({
      items: [{ id: 1, customerName: 'UAT', destinationInstitutionName: 'Banco', destinationAccountNumber: '123' } as any],
      total: 1,
      page: 1,
      pageSize: 20
    }));

    await TestBed.configureTestingModule({
      imports: [CustomerThirdPartiesComponent],
      providers: [
        { provide: CustomerThirdPartiesService, useValue: service },
        { provide: NotificationService, useValue: jasmine.createSpyObj('NotificationService', ['success', 'error']) }
      ]
    }).compileComponents();

    const fixture = TestBed.createComponent(CustomerThirdPartiesComponent);
    fixture.detectChanges();

    expect(service.search).toHaveBeenCalled();
    expect(fixture.componentInstance.rows.length).toBe(1);
    expect(fixture.componentInstance.loadError).toBeFalse();
  });

  it('CustomerThirdParties_ShouldShowError_WhenApiFails', async () => {
    const service = jasmine.createSpyObj<CustomerThirdPartiesService>('CustomerThirdPartiesService', ['search']);
    service.search.and.returnValue(throwError(() => new Error('api')));

    await TestBed.configureTestingModule({
      imports: [CustomerThirdPartiesComponent],
      providers: [
        { provide: CustomerThirdPartiesService, useValue: service },
        { provide: NotificationService, useValue: jasmine.createSpyObj('NotificationService', ['success', 'error']) }
      ]
    }).compileComponents();

    const fixture = TestBed.createComponent(CustomerThirdPartiesComponent);
    fixture.detectChanges();

    expect(fixture.componentInstance.loadError).toBeTrue();
    expect(fixture.componentInstance.loading).toBeFalse();
  });
});

import { TestBed } from '@angular/core/testing';
import { NoopAnimationsModule } from '@angular/platform-browser/animations';
import { ActivatedRoute, Router, convertToParamMap } from '@angular/router';
import { of, Subject, throwError } from 'rxjs';
import { AuthService } from '../../../core/services/auth.service';
import { CustomersApiService } from '../../../core/services/customers-api.service';
import { CustomerDetail, CustomerSummary } from '../../../core/models/customer.model';
import { NotificationService } from '../../../core/services/notification.service';
import { CustomerFormComponent } from './customer-form.component';
import { CustomersListComponent } from './customers-list.component';

const apiMock = () => jasmine.createSpyObj<CustomersApiService>('CustomersApiService', ['getAll', 'getById', 'create', 'update']);
const notifyMock = () => jasmine.createSpyObj<NotificationService>('NotificationService', ['success', 'error']);
const routerMock = () => jasmine.createSpyObj<Router>('Router', ['navigate']);
const details = (overrides: Partial<CustomerDetail> = {}): CustomerDetail => ({
  id: 7, personType: 'PN', documentType: 'CC', documentNumber: '001234',
  firstName: 'Ana', middleName: '', lastName: 'Pérez', secondLastName: '',
  companyName: '', gender: '', accountNumber: '0000123', accountNumbers: ['0000123'],
  ...overrides
});

async function mountForm(id?: string) {
  const api = apiMock();
  const notifications = notifyMock();
  const router = routerMock();
  api.getById.and.returnValue(of(details()));
  api.create.and.returnValue(of(details()));
  api.update.and.returnValue(of(details()));
  await TestBed.configureTestingModule({
    imports: [CustomerFormComponent, NoopAnimationsModule],
    providers: [
      { provide: CustomersApiService, useValue: api },
      { provide: NotificationService, useValue: notifications },
      { provide: Router, useValue: router },
      { provide: ActivatedRoute, useValue: { snapshot: { paramMap: convertToParamMap(id ? { id } : {}) } } }
    ]
  }).compileComponents();
  const fixture = TestBed.createComponent(CustomerFormComponent);
  fixture.detectChanges();
  return { fixture, component: fixture.componentInstance, api, notifications, router };
}

describe('Clientes — formularios Material y validaciones', () => {
  afterEach(() => TestBed.resetTestingModule());

  it('requiere nombre y apellido para persona natural y razón social para jurídica', async () => {
    const { component, fixture } = await mountForm();
    expect(fixture.nativeElement.querySelectorAll('mat-form-field').length).toBeGreaterThan(3);
    component.form.patchValue({ personType: 'PN', documentType: 'CC', documentNumber: '001234' });
    expect(component.form.controls.firstName.invalid).toBeTrue();
    expect(component.form.controls.lastName.invalid).toBeTrue();
    component.form.patchValue({ firstName: 'Ana', lastName: 'Pérez' });
    expect(component.form.valid).toBeTrue();
    component.form.patchValue({ personType: 'PJ' });
    expect(component.form.controls.companyName.invalid).toBeTrue();
    expect(component.form.controls.firstName.valid).toBeTrue();
    component.form.patchValue({ companyName: 'Empresa SAS' });
    expect(component.form.valid).toBeTrue();
  });

  it('conserva ceros iniciales, impide cuentas duplicadas y agrega la cuenta pendiente antes de guardar', async () => {
    const { component, api } = await mountForm();
    component.form.patchValue({ personType: 'PN', documentType: 'CC', documentNumber: '001234', firstName: 'Ana', lastName: 'Pérez' });
    component.accountNumberInput.setValue('0000123');
    expect(component.addAccountNumber()).toBeTrue();
    component.accountNumberInput.setValue('0000123');
    expect(component.addAccountNumber()).toBeFalse();
    expect(component.accountNumbers).toEqual(['0000123']);
    component.accountNumberInput.setValue('0000456');
    component.save();
    expect(api.create).toHaveBeenCalledWith(jasmine.objectContaining({
      documentNumber: '001234', accountNumbers: ['0000123', '0000456'], personType: 'PN'
    }));
  });

  it('no permite guardar dos veces mientras la petición está en curso', async () => {
    const { component, api } = await mountForm();
    const pending = new Subject<CustomerDetail>();
    api.create.and.returnValue(pending.asObservable());
    component.form.patchValue({ personType: 'PJ', documentType: 'NIT', documentNumber: '900123', companyName: 'Empresa SAS' });
    component.accountNumbers = ['0000123'];
    component.save();
    component.save();
    expect(api.create).toHaveBeenCalledTimes(1);
    pending.next(details());
    pending.complete();
    expect(component.saving).toBeFalse();
  });

  it('al editar muestra un error recuperable si el servidor falla, sin guardar datos', async () => {
    const { component, api } = await mountForm('7');
    api.getById.and.returnValue(throwError(() => new Error('fallo de red')));
    component.loadCustomer();
    expect(component.loadError).toBeTrue();
    component.save();
    expect(api.update).not.toHaveBeenCalled();
  });
});

describe('Clientes — búsqueda y permisos del listado', () => {
  afterEach(() => TestBed.resetTestingModule());

  async function mountList(canManage: boolean, fail = false) {
    const api = apiMock();
    const router = routerMock();
    const notifications = notifyMock();
    const data: CustomerSummary[] = [{
      id: 9, documentType: 'NIT', documentNumber: '9001', accountNumber: '0001',
      accountNumbers: ['0001'], personType: 'PJ', companyName: 'Compañía Ejemplo', fullName: ''
    }];
    api.getAll.and.returnValue(fail ? throwError(() => new Error('fallo')) : of(data));
    await TestBed.configureTestingModule({
      imports: [CustomersListComponent, NoopAnimationsModule],
      providers: [
        { provide: CustomersApiService, useValue: api },
        { provide: NotificationService, useValue: notifications },
        { provide: Router, useValue: router },
        { provide: AuthService, useValue: { hasPermission: () => canManage } }
      ]
    }).compileComponents();
    const fixture = TestBed.createComponent(CustomersListComponent);
    fixture.detectChanges();
    return { fixture, component: fixture.componentInstance, api, router };
  }

  it('filtra también por razón social y muestra tipo de persona legible', async () => {
    const { component } = await mountList(true);
    component.filtersForm.controls.name.setValue('ejemplo');
    expect(component.filteredCustomers.length).toBe(1);
    expect(component.filasGrilla[0].tipoPersona).toBe('Persona jurídica');
    component.filtersForm.controls.name.setValue('sin coincidencias');
    expect(component.filteredCustomers.length).toBe(0);
  });

  it('oculta la edición cuando falta CanManageAch', async () => {
    const { component, router, fixture } = await mountList(false);
    expect(component.canManage).toBeFalse();
    expect(fixture.nativeElement.querySelector('button[mat-flat-button]')).toBeNull();
    component.create();
    component.edit(component.customers[0]);
    expect(router.navigate).not.toHaveBeenCalled();
  });

  it('ofrece reintento cuando falla la consulta', async () => {
    const { component } = await mountList(true, true);
    expect(component.loadError).toBeTrue();
    expect(component.loading).toBeFalse();
  });
});

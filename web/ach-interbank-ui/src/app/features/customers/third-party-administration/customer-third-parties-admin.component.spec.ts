import { HttpErrorResponse } from '@angular/common/http';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { ActivatedRoute, Router } from '@angular/router';
import { of, throwError } from 'rxjs';
import { AuthService } from '../../../core/services/auth.service';
import { CustomersApiService } from '../../../core/services/customers-api.service';
import { NotificationService } from '../../../core/services/notification.service';
import { CustomerThirdPartiesAdminComponent } from './customer-third-parties-admin.component';
import { ThirdPartyAdministrationApiService } from './third-party-administration-api.service';
import { ThirdPartyAdministrativeStatus } from './third-party-administration.models';

describe('CustomerThirdPartiesAdminComponent', () => {
  let fixture: ComponentFixture<CustomerThirdPartiesAdminComponent>;
  let component: CustomerThirdPartiesAdminComponent;
  let api: jasmine.SpyObj<ThirdPartyAdministrationApiService>;
  let auth: jasmine.SpyObj<AuthService>;
  let router: jasmine.SpyObj<Router>;

  beforeEach(async () => {
    api = jasmine.createSpyObj<ThirdPartyAdministrationApiService>('ThirdPartyAdministrationApiService', [
      'listThirdParties', 'createThirdParty'
    ]);
    auth = jasmine.createSpyObj<AuthService>('AuthService', ['hasPermission']);
    router = jasmine.createSpyObj<Router>('Router', ['navigate']);
    const customers = jasmine.createSpyObj<CustomersApiService>('CustomersApiService', ['getById']);
    const notifications = jasmine.createSpyObj<NotificationService>('NotificationService', ['success', 'error']);

    auth.hasPermission.and.returnValue(true);
    customers.getById.and.returnValue(of({
      id: 7,
      firstName: 'Cliente',
      lastName: 'Prueba',
      personType: 'PN',
      documentType: 'CC',
      documentNumber: '11112222',
      accountNumber: '9999000011112222',
      accountNumbers: ['9999000011112222']
    }));
    api.listThirdParties.and.returnValue(of({
      items: [{
        thirdPartyId: 11,
        customerId: 7,
        personType: 'PJ',
        documentType: 'NIT',
        documentNumberMasked: '••••5678',
        displayName: 'Proveedor Uno',
        status: ThirdPartyAdministrativeStatus.Active,
        accountCount: 2,
        createdAt: '2026-10-11T10:00:00Z',
        updatedAt: '2026-10-11T11:00:00Z'
      }],
      total: 1,
      page: 1,
      pageSize: 20
    }));

    await TestBed.configureTestingModule({
      imports: [CustomerThirdPartiesAdminComponent],
      providers: [
        { provide: ThirdPartyAdministrationApiService, useValue: api },
        { provide: AuthService, useValue: auth },
        { provide: Router, useValue: router },
        { provide: CustomersApiService, useValue: customers },
        { provide: NotificationService, useValue: notifications },
        {
          provide: ActivatedRoute,
          useValue: { snapshot: { paramMap: { get: (key: string) => key === 'id' ? '7' : null } } }
        }
      ]
    }).compileComponents();

    fixture = TestBed.createComponent(CustomerThirdPartiesAdminComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('carga lista canónica y navega al detalle del tercero', () => {
    expect(api.listThirdParties).toHaveBeenCalledWith(7, jasmine.objectContaining({ page: 1, pageSize: 20 }));
    expect(component.items.length).toBe(1);
    expect(fixture.nativeElement.textContent).toContain('Proveedor Uno');
    expect(fixture.nativeElement.textContent).toContain('••••5678');

    component.openDetail(component.items[0]);
    expect(router.navigate).toHaveBeenCalledWith(['/customers', 7, 'third-parties', 11]);
  });

  it('crea tercero sin dimensión de cámara y conserva masking en lectura', () => {
    api.createThirdParty.and.returnValue(of({
      thirdPartyId: 12,
      customerId: 7,
      personType: 'PJ',
      documentType: 'NIT',
      documentNumberMasked: '••••1234',
      displayName: 'Nuevo tercero',
      status: ThirdPartyAdministrativeStatus.Active,
      accountCount: 0,
      createdAt: '2026-10-11T10:00:00Z',
      updatedAt: '2026-10-11T10:00:00Z'
    }));

    component.createOpen = true;
    component.createForm.setValue({
      personType: 'PJ',
      documentType: 'NIT',
      documentNumber: '9000001234',
      displayName: 'Nuevo tercero'
    });
    component.createThirdParty();

    const command = api.createThirdParty.calls.mostRecent().args[1] as any;
    expect(command.clearingHouseId).toBeUndefined();
    expect(command.transactionNature).toBeUndefined();
    expect(command.documentNumber).toBe('9000001234');
    expect(router.navigate).toHaveBeenCalledWith(['/customers', 7, 'third-parties', 12]);
  });


  it('presenta error de lectura sin reemplazar datos con un fallback legacy', () => {
    api.listThirdParties.and.returnValue(throwError(() => new HttpErrorResponse({ status: 500, error: { message: 'Fallo controlado.' } })));
    component.load(1);
    expect(component.loadError).toContain('Fallo controlado');
    expect(component.items).toEqual([]);
  });

  it('muestra conflicto y oculta comandos a usuarios sin CanManageAch', () => {
    api.createThirdParty.and.returnValue(throwError(() => new HttpErrorResponse({
      status: 409,
      error: { code: 'DUPLICATE_IDENTITY_CONFLICT', message: 'Identidad ya registrada con datos incompatibles.' }
    })));
    component.createForm.setValue({ personType: 'PJ', documentType: 'NIT', documentNumber: '900', displayName: 'Duplicado' });
    component.createThirdParty();
    expect(component.commandError).toContain('Identidad ya registrada');

    auth.hasPermission.and.returnValue(false);
    component.createOpen = false;
    fixture.detectChanges();
    expect(fixture.nativeElement.querySelector('[data-testid="new-third-party"]')).toBeNull();
  });
});

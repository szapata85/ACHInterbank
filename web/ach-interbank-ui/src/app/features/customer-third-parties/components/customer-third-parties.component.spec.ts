import { NoopAnimationsModule } from '@angular/platform-browser/animations';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { of, Subject } from 'rxjs';
import { NotificationService } from '../../../core/services/notification.service';
import { PagedResponse, CustomerThirdPartyRow } from '../models/customer-third-party.model';
import { CustomerThirdPartiesService } from '../services/customer-third-parties.service';
import { CustomerThirdPartiesComponent } from './customer-third-parties.component';

describe('CustomerThirdPartiesComponent', () => {
  let fixture: ComponentFixture<CustomerThirdPartiesComponent>;
  let component: CustomerThirdPartiesComponent;
  let service: jasmine.SpyObj<CustomerThirdPartiesService>;

  const response: PagedResponse<CustomerThirdPartyRow> = {
    items: [{
      id: 1,
      customerId: 10,
      customerName: 'Cliente UAT',
      destinationInstitutionName: 'Entidad receptora',
      destinationAccountNumber: '123456789',
      recipientIdNumber: 'REC-01',
      status: 'Pending',
      prenotificationTransactionId: 99
    }],
    total: 1,
    page: 1,
    pageSize: 20
  };

  beforeEach(async () => {
    service = jasmine.createSpyObj<CustomerThirdPartiesService>('CustomerThirdPartiesService', ['search']);
    service.search.and.returnValue(of(response));

    await TestBed.configureTestingModule({
      imports: [CustomerThirdPartiesComponent, NoopAnimationsModule],
      providers: [
        { provide: CustomerThirdPartiesService, useValue: service },
        {
          provide: NotificationService,
          useValue: jasmine.createSpyObj<NotificationService>('NotificationService', ['success', 'error', 'warning', 'info'])
        }
      ]
    }).compileComponents();

    fixture = TestBed.createComponent(CustomerThirdPartiesComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('crea un formulario reactivo con sus valores iniciales y ejecuta la consulta inicial', () => {
    expect(component.filterForm.getRawValue()).toEqual({
      search: '',
      destinationAccountNumber: '',
      recipientIdNumber: '',
      status: ''
    });
    expect(service.search).toHaveBeenCalledTimes(1);
  });

  it('bloquea la búsqueda inválida, marca controles y no invoca el servicio', () => {
    service.search.calls.reset();
    component.filterForm.controls.destinationAccountNumber.setValue('cuenta-inválida');

    component.search();

    expect(component.filterForm.controls.destinationAccountNumber.touched).toBeTrue();
    expect(service.search).not.toHaveBeenCalled();
  });

  it('evita doble consulta mientras hay una solicitud en curso', () => {
    const pending = new Subject<PagedResponse<CustomerThirdPartyRow>>();
    service.search.and.returnValue(pending);
    service.search.calls.reset();

    component.search();
    component.search();

    expect(service.search).toHaveBeenCalledTimes(1);
    pending.next(response);
    pending.complete();
  });

  it('limpia filtros y vuelve a consultar desde la primera página', () => {
    component.filterForm.patchValue({
      search: 'cliente',
      destinationAccountNumber: '123456',
      recipientIdNumber: 'REC-01',
      status: 'Rejected'
    });
    service.search.calls.reset();

    component.clear();

    expect(component.filterForm.getRawValue()).toEqual({
      search: '',
      destinationAccountNumber: '',
      recipientIdNumber: '',
      status: ''
    });
    expect(service.search).toHaveBeenCalledTimes(1);
  });

  it('presenta el estado como lectura y no renderiza acciones manuales', () => {
    const text = (fixture.nativeElement as HTMLElement).textContent ?? '';
    expect(text).toContain('Resolución automática');
    expect(text).not.toContain('Aprobar');
    expect(text).not.toContain('Rechazar');
    component.showDetail(component.rows[0]);
    fixture.detectChanges();
    expect(fixture.nativeElement.querySelector('[data-testid="third-party-detail"]')).not.toBeNull();
  });
});

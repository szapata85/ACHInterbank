import { ComponentFixture, TestBed } from '@angular/core/testing';
import { ActivatedRoute, convertToParamMap, provideRouter } from '@angular/router';
import { BehaviorSubject, Subject } from 'rxjs';
import { provideNoopAnimations } from '@angular/platform-browser/animations';
import { AuthService } from '../../core/services/auth.service';
import { ClearingHousesService } from './clearing-houses.service';
import { TransactionPoliciesService } from './transaction-policies.service';
import { TransactionPoliciesComponent } from './transaction-policies.component';
import { TransactionTypeEnum } from '../transactions/transactions.types';

describe('TransactionPoliciesComponent', () => {
  let fixture: ComponentFixture<TransactionPoliciesComponent>;
  let component: TransactionPoliciesComponent;
  let routeParams$: BehaviorSubject<ReturnType<typeof convertToParamMap>>;
  let getHouse: jasmine.Spy;
  let listPolicies: jasmine.Spy;
  let houseRequests: Map<number, Subject<any>>;
  let policyRequests: Map<number, Subject<any>>;

  beforeEach(async () => {
    routeParams$ = new BehaviorSubject(convertToParamMap({ id: '7' }));
    houseRequests = new Map();
    policyRequests = new Map();
    getHouse = jasmine.createSpy('get').and.callFake((id: number) => houseRequests.get(id)!);
    listPolicies = jasmine.createSpy('list').and.callFake((id: number) => policyRequests.get(id)!);

    await TestBed.configureTestingModule({
      imports: [TransactionPoliciesComponent],
      providers: [
        provideRouter([]),
        provideNoopAnimations(),
        { provide: ActivatedRoute, useValue: { paramMap: routeParams$.asObservable() } },
        { provide: AuthService, useValue: { hasPermission: () => true } },
        { provide: ClearingHousesService, useValue: { get: getHouse } },
        {
          provide: TransactionPoliciesService,
          useValue: {
            list: listPolicies,
            create: () => new Subject(),
            updateMetadata: () => new Subject(),
            preview: () => new Subject(),
            close: () => new Subject(),
            activate: () => new Subject()
          }
        }
      ]
    }).compileComponents();
  });

  it('starts loading and clears it after both requests assign the camera and policies', () => {
    createFor(7);
    expect(component.loading).toBeTrue();

    completeLoad(7, 'CENIT', [policy(7, null)]);

    expect(component.loading).toBeFalse();
    expect(component.clearingHouse?.code).toBe('CENIT');
    expect(component.policies).toEqual([policy(7, null)]);
    expect(component.lead(component.currentDebit)).toBe('Sin plazo mínimo documentado');
  });

  it('clears loading and shows the HTTP error message', () => {
    createFor(7);
    houseRequests.get(7)!.error({ status: 404 });

    expect(component.loading).toBeFalse();
    expect(component.error).toBe('La cámara solicitada no existe o ya no está disponible.');
  });

  it('does not request data for an invalid route id', () => {
    routeParams$.next(convertToParamMap({ id: 'invalid' }));
    fixture = TestBed.createComponent(TransactionPoliciesComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();

    expect(component.loading).toBeFalse();
    expect(component.error).toBe('La cámara indicada no es válida.');
    expect(getHouse).not.toHaveBeenCalled();
    expect(listPolicies).not.toHaveBeenCalled();
  });

  it('loads again when the route id changes', () => {
    createFor(7);
    completeLoad(7, 'CENIT', [policy(7, null)]);

    houseRequests.set(8, new Subject());
    policyRequests.set(8, new Subject());
    routeParams$.next(convertToParamMap({ id: '8' }));

    expect(component.loading).toBeTrue();
    expect(getHouse).toHaveBeenCalledWith(8);
    expect(listPolicies).toHaveBeenCalledWith(8);
    completeLoad(8, 'ACHCOL', [policy(8, 3)]);
    expect(component.loading).toBeFalse();
    expect(component.clearingHouse?.code).toBe('ACHCOL');
    expect(component.lead(component.currentDebit)).toBe('3 días hábiles');
  });

  it('clears and disables lead days for optional policies', () => {
    createFor(7);
    completeLoad(7, 'CENIT', [policy(7, null)]);

    component.createVersion();
    component.form.controls.prenotificationMode.setValue('Optional');

    expect(component.form.controls.prenotificationLeadBusinessDays.disabled).toBeTrue();
    expect(component.form.controls.prenotificationLeadBusinessDays.value).toBeNull();
  });

  it('keeps listening for route changes after an HTTP error', () => {
    createFor(7);
    houseRequests.get(7)!.error({ status: 403 });
    expect(component.loading).toBeFalse();
    expect(component.error).toContain('permisos');

    houseRequests.set(8, new Subject());
    policyRequests.set(8, new Subject());
    routeParams$.next(convertToParamMap({ id: '8' }));
    completeLoad(8, 'ACHCOL', [policy(8, 3)]);

    expect(component.clearingHouse?.code).toBe('ACHCOL');
    expect(component.error).toBe('');
  });

  it('renders no local h1 because the application shell owns the page heading', () => {
    createFor(7);
    completeLoad(7, 'ACHCOL', [policy(7, 3)]);
    expect(fixture.nativeElement.querySelectorAll('h1').length).toBe(0);
    expect(fixture.nativeElement.textContent).toContain('Configuración transaccional');
  });

  it('validates integer, non-negative and maximum lead days', () => {
    createFor(7);
    completeLoad(7, 'ACHCOL', [policy(7, 3)]);
    component.createVersion();
    const lead = component.form.controls.prenotificationLeadBusinessDays;
    lead.setValue(1.5);
    expect(lead.hasError('integer')).toBeTrue();
    lead.setValue(-1);
    expect(lead.hasError('min')).toBeTrue();
    lead.setValue(366);
    expect(lead.hasError('max')).toBeTrue();
  });

  function createFor(id: number): void {
    houseRequests.set(id, new Subject());
    policyRequests.set(id, new Subject());
    fixture = TestBed.createComponent(TransactionPoliciesComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  }

  function completeLoad(id: number, code: string, policies: any[]): void {
    houseRequests.get(id)!.next({ id, code, name: code, isActive: true, isReady: true });
    houseRequests.get(id)!.complete();
    policyRequests.get(id)!.next(policies);
    policyRequests.get(id)!.complete();
    fixture.detectChanges();
  }

  function policy(clearingHouseId: number, leadDays: number | null): any {
    return {
      id: clearingHouseId,
      clearingHouseId,
      clearingHouseName: 'Cámara',
      transactionType: TransactionTypeEnum.Debit,
      prenotificationMode: 'Mandatory',
      prenotificationLeadBusinessDays: leadDays,
      effectiveFrom: '2026-01-01',
      effectiveTo: null,
      isActive: true,
      normativeSource: 'DSP',
      normativeReference: '4.7',
      notes: '',
      createdAt: '2026-01-01',
      updatedAt: '2026-01-01'
    };
  }
});

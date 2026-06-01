import { ComponentFixture, TestBed } from '@angular/core/testing';
import { ActivatedRoute } from '@angular/router';
import { of } from 'rxjs';
import { CenitRegulatoryApiService } from '../services/cenit-regulatory-api.service';
import { CenitRegulatoryPageComponent } from './cenit-regulatory-page.component';

describe('CenitRegulatoryPageComponent', () => {
  const service = () => jasmine.createSpyObj<CenitRegulatoryApiService>('CenitRegulatoryApiService', [
    'getReturnCodes',
    'getFileRejectionCodes',
    'getTransactionTypePolicies',
    'getReturnPolicies',
    'getReturnOfReturnPolicies',
    'getPrenotificationPolicies'
  ]);

  function create(
    view: string,
    api = service(),
    configure?: (api: jasmine.SpyObj<CenitRegulatoryApiService>) => void
  ): ComponentFixture<CenitRegulatoryPageComponent> {
    api.getReturnCodes.and.returnValue(of([]));
    api.getFileRejectionCodes.and.returnValue(of([]));
    api.getTransactionTypePolicies.and.returnValue(of([]));
    api.getReturnPolicies.and.returnValue(of([]));
    api.getReturnOfReturnPolicies.and.returnValue(of([]));
    api.getPrenotificationPolicies.and.returnValue(of([]));
    configure?.(api);

    TestBed.configureTestingModule({
      imports: [CenitRegulatoryPageComponent],
      providers: [
        { provide: ActivatedRoute, useValue: { snapshot: { data: { view } } } },
        { provide: CenitRegulatoryApiService, useValue: api }
      ]
    });

    const fixture = TestBed.createComponent(CenitRegulatoryPageComponent);
    fixture.detectChanges();
    return fixture;
  }

  afterEach(() => TestBed.resetTestingModule());

  it('Regulatory_ShouldCallReturnCodesForReturnCauses', () => {
    const api = service();
    create('causales-devolucion', api);
    expect(api.getReturnCodes).toHaveBeenCalled();
  });

  it('Regulatory_ShouldCallFileRejectionCodesForRejectionCauses', () => {
    const api = service();
    create('causales-rechazo', api);
    expect(api.getFileRejectionCodes).toHaveBeenCalled();
  });

  it('Regulatory_ShouldCallTransactionPoliciesForTransactionPolicies', () => {
    const api = service();
    create('politicas-transaccion', api);
    expect(api.getTransactionTypePolicies).toHaveBeenCalled();
  });

  it('Regulatory_ShouldRenderRowsWhenReturnCodesArrive', () => {
    const api = service();
    const component = create('causales-devolucion', api, (mock) => {
      mock.getReturnCodes.and.returnValue(of([
        {
          code: 'R01',
          description: 'Fondos insuficientes',
          appliesToDebit: true,
          appliesToCredit: false,
          appliesToPrenotification: false,
          appliesToReturn: true,
          requiresAddenda: false,
          maxDaysAllowed: 2,
          isActive: true
        }
      ]));
    }).componentInstance;

    expect(component.rows.length).toBe(1);
    expect(component.columnasTabla.length).toBeGreaterThan(0);
    expect(Object.values(component.rows[0])).toContain('R01');
  });
});

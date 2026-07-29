import { ComponentFixture, TestBed } from '@angular/core/testing';
import { ActivatedRoute } from '@angular/router';
import { of } from 'rxjs';
import { AuthService } from '../../core/services/auth.service';
import { ClearingHousesService } from './clearing-houses.service';
import { TransactionPoliciesService } from './transaction-policies.service';
import { TransactionPoliciesComponent } from './transaction-policies.component';
import { TransactionTypeEnum } from '../transactions/transactions.types';

describe('TransactionPoliciesComponent', () => {
  let fixture: ComponentFixture<TransactionPoliciesComponent>;
  let component: TransactionPoliciesComponent;
  const policy = { id: 1, clearingHouseId: 7, clearingHouseName: 'CENIT', transactionType: TransactionTypeEnum.Debit, prenotificationMode: 'Mandatory', prenotificationLeadBusinessDays: null, effectiveFrom: '2026-01-01', effectiveTo: null, isActive: true, normativeSource: 'DSP', normativeReference: '4.7', notes: '', createdAt: '2026-01-01', updatedAt: '2026-01-01' } as any;

  beforeEach(async () => {
    await TestBed.configureTestingModule({ imports: [TransactionPoliciesComponent], providers: [
      { provide: ActivatedRoute, useValue: { paramMap: of(new Map([['id', '7']])) } },
      { provide: AuthService, useValue: { hasPermission: () => true } },
      { provide: ClearingHousesService, useValue: { get: () => of({ id: 7, code: 'CENIT', name: 'CENIT', isActive: true, isReady: true } as any) } },
      { provide: TransactionPoliciesService, useValue: { list: () => of([policy]), create: () => of(policy), updateMetadata: () => of(policy), preview: () => of({ ruleConfigured: true }) } }
    ] }).compileComponents();
    fixture = TestBed.createComponent(TransactionPoliciesComponent); component = fixture.componentInstance; fixture.detectChanges();
  });

  it('loads the clearing house from the route and preserves nullable lead days', () => {
    expect(component.clearingHouse?.code).toBe('CENIT');
    expect(component.currentDebit?.prenotificationLeadBusinessDays).toBeNull();
    expect(component.lead(component.currentDebit)).toBe('Sin plazo mínimo documentado');
  });

  it('clears and disables lead days for optional policies', () => {
    component.createVersion(); component.form.controls.prenotificationMode.setValue('Optional');
    expect(component.form.controls.prenotificationLeadBusinessDays.disabled).toBeTrue();
    expect(component.form.controls.prenotificationLeadBusinessDays.value).toBeNull();
  });
});

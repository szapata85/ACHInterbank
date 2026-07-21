import { ComponentFixture, TestBed } from '@angular/core/testing';
import { of, throwError } from 'rxjs';
import { NotificationService } from '../../../../core/services/notification.service';
import { ClearingHousesApiService } from '../../../ach-cycles/services/ach-cycles-api.service';
import { TransactionTypeEnum } from '../../transactions.types';
import { ClearingHouseTransactionRulesApiService } from '../../services/clearing-house-transaction-rules-api.service';
import { ClearingHouseTransactionRulesComponent } from './clearing-house-transaction-rules.component';

describe('ClearingHouseTransactionRulesComponent', () => {
  let fixture: ComponentFixture<ClearingHouseTransactionRulesComponent>;
  let component: ClearingHouseTransactionRulesComponent;
  let api: jasmine.SpyObj<ClearingHouseTransactionRulesApiService>;
  let housesApi: jasmine.SpyObj<ClearingHousesApiService>;
  let notifications: jasmine.SpyObj<NotificationService>;

  beforeEach(async () => {
    api = jasmine.createSpyObj<ClearingHouseTransactionRulesApiService>('ClearingHouseTransactionRulesApiService', [
      'getRules',
      'create',
      'update',
      'activate',
      'deactivate',
      'preview'
    ]);
    housesApi = jasmine.createSpyObj<ClearingHousesApiService>('ClearingHousesApiService', ['listAdministrative']);
    notifications = jasmine.createSpyObj<NotificationService>('NotificationService', ['success', 'warning', 'error']);

    housesApi.listAdministrative.and.returnValue(of([{ id: 1, name: 'ACH Colombia', code: 'ACH' } as any]));
    api.getRules.and.returnValue(of([]));
    api.create.and.returnValue(of({ id: 1 } as any));
    api.update.and.returnValue(of({ id: 1 } as any));
    api.activate.and.returnValue(of({ id: 1 } as any));
    api.deactivate.and.returnValue(of({ id: 1 } as any));
    api.preview.and.returnValue(of({
      ruleConfigured: true,
      requiresPrenotification: true,
      prenotificationMode: 'Mandatory',
      requiresReceiverIdentificationValidation: true,
      receiverIdentificationValidationMode: 'Mandatory',
      normativeSource: 'MAN-004',
      normativeReference: '2.11',
      decision: 'PRENOTIFICATION_REQUIRED',
      message: 'Requiere prenotificación.'
    } as any));

    await TestBed.configureTestingModule({
      imports: [ClearingHouseTransactionRulesComponent],
      providers: [
        { provide: ClearingHouseTransactionRulesApiService, useValue: api },
        { provide: ClearingHousesApiService, useValue: housesApi },
        { provide: NotificationService, useValue: notifications }
      ]
    }).compileComponents();

    fixture = TestBed.createComponent(ClearingHouseTransactionRulesComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('loads rules on init', () => {
    expect(api.getRules).toHaveBeenCalled();
    expect(housesApi.listAdministrative).toHaveBeenCalled();
  });

  it('opens create form with mandatory debit defaults', () => {
    component.openCreateForm();

    expect(component.showForm).toBeTrue();
    expect(component.form.controls.transactionType.value).toBe(TransactionTypeEnum.Debit);
    expect(component.form.controls.prenotificationMode.value).toBe('Mandatory');
  });

  it('saves valid rule', () => {
    component.openCreateForm();
    component.form.patchValue({
      clearingHouseId: 1,
      normativeSource: 'MAN-004 ACH Colombia V32',
      normativeReference: '2.11.4'
    });

    component.save();

    expect(api.create).toHaveBeenCalled();
    expect(notifications.success).toHaveBeenCalled();
  });

  it('shows preview result', () => {
    component.openCreateForm();
    component.form.patchValue({
      clearingHouseId: 1,
      normativeSource: 'MAN-004 ACH Colombia V32',
      normativeReference: '2.11.4'
    });

    component.preview();

    expect(api.preview).toHaveBeenCalled();
    expect(component.previewResult?.decision).toBe('PRENOTIFICATION_REQUIRED');
  });

  it('reports search errors', () => {
    api.getRules.and.returnValue(throwError(() => new Error('boom')));

    component.search();

    expect(notifications.error).toHaveBeenCalled();
    expect(component.loadError).toContain('No fue posible consultar reglas');
  });
});

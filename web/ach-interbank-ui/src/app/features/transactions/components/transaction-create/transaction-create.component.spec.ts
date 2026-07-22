import { ComponentFixture, TestBed } from '@angular/core/testing';
import { Router } from '@angular/router';
import { of, Subject } from 'rxjs';
import { NotificationService } from '../../../../core/services/notification.service';
import { CustomerSummary } from '../../../customers/models/customer.model';
import { CustomersApiService } from '../../../customers/services/customers-api.service';
import { FinancialInstitutionsApiService } from '../../services/financial-institutions-api.service';
import { TransactionsApiService } from '../../services/transactions-api.service';
import { AccountTypeEnum, TransactionTypeEnum } from '../../transactions.types';
import { TransactionCreateComponent } from './transaction-create.component';

describe('TransactionCreateComponent', () => {
  let component: TransactionCreateComponent;
  let fixture: ComponentFixture<TransactionCreateComponent>;
  let txApi: jasmine.SpyObj<TransactionsApiService>;
  let notifications: jasmine.SpyObj<NotificationService>;
  const activeThirdPartyAccount = {
    destinationAccountNumber: '9876543210',
    destinationInstitutionId: 7,
    recipientIdNumber: '10101010',
    destinationInstitutionName: 'Banco destino UAT'
  };

  beforeEach(async () => {
    txApi = jasmine.createSpyObj<TransactionsApiService>('TransactionsApiService', ['getCompanyEntryDescriptions', 'createTransaction', 'getActiveThirdParties', 'previewPolicy']);
    notifications = jasmine.createSpyObj<NotificationService>('NotificationService', ['success', 'error']);

    await TestBed.configureTestingModule({
      imports: [TransactionCreateComponent],
      providers: [
        { provide: TransactionsApiService, useValue: txApi },
        { provide: FinancialInstitutionsApiService, useValue: { getAll: () => of([]) } },
        { provide: CustomersApiService, useValue: { getAll: () => of([] as CustomerSummary[]) } },
        { provide: NotificationService, useValue: notifications },
        { provide: Router, useValue: jasmine.createSpyObj('Router', ['navigate']) }
      ]
    }).compileComponents();

    fixture = TestBed.createComponent(TransactionCreateComponent);
    component = fixture.componentInstance;
    txApi.getCompanyEntryDescriptions.and.returnValue(of([{ id: 1, term: 'NOMINAS', description: 'Nómina' }] as any));
    txApi.getActiveThirdParties.and.returnValue(of([activeThirdPartyAccount] as any));
    txApi.previewPolicy.and.returnValue(of({ canSubmit: true } as any));
    fixture.detectChanges();
  });

  function fillValidForm(amount: string | number = '1.000') {
    component.form.patchValue({
      amount,
      transactionExternalId: 'tx-001',
      reference: 'ref',
      type: TransactionTypeEnum.Credit,
      accountType: AccountTypeEnum.Checking,
      isPrenotification: false,
      destinationInstitutionId: 7,
      sourceAccountNumber: '1234567890',
      destinationAccountNumber: '9876543210',
      recipientIdNumber: '10101010',
      recipientName: 'Receptor',
      requiresIdentityValidation: false,
      companyName: 'Empresa',
      companyIdentification: 'AB12',
      sourcePersonType: 'PJ',
      recipientPersonType: 'PN',
      companyEntryDescriptionId: 1
    });
    component.activeDestinationAccounts = [activeThirdPartyAccount] as any;
    component.filteredDestinationAccounts = [activeThirdPartyAccount] as any;
    component.form.patchValue({
      destinationInstitutionId: activeThirdPartyAccount.destinationInstitutionId,
      destinationAccountNumber: activeThirdPartyAccount.destinationAccountNumber,
      recipientIdNumber: activeThirdPartyAccount.recipientIdNumber
    }, { emitEvent: false });
    component.addendas.at(0).patchValue({
      addendaType: '05',
      collectorId: '9001234567890',
      receiverCustomerCode: 'CLI0000000001',
      serviceDescription: 'SERVQA',
      information: 'Detalle'
    });
    component.form.updateValueAndValidity();
  }

  it('TransactionCreateComponent_ShouldBuildValidPayload_WhenFormValid', () => {
    txApi.createTransaction.and.returnValue(of({ id: 11 } as any));
    fillValidForm('1.234,50');

    component.submit();

    const payload = txApi.createTransaction.calls.mostRecent().args[0] as any;
    expect(payload.amount).toBe(1234.5);
    expect(payload.sourceAccountNumber).toBe('1234567890');
    expect(payload.destinationAccountNumber).toBe('9876543210');
    expect(payload.companyIdentification).toBe('AB12');
    expect(payload.destinationInstitutionId).toBe(7);
    expect(payload.addendas.length).toBe(1);
    expect(payload.addendas[0].collectorId).toBe('9001234567890');
    expect(payload.addendas[0].receiverCustomerCode).toBe('CLI0000000001');
    expect(payload.addendas[0].serviceDescription).toBe('SERVQA');
  });

  it('TransactionCreateComponent_ShouldInitializeDefaultAddenda', () => {
    expect(component.addendas.length).toBe(1);
    expect(component.addendas.at(0).get('addendaType')?.value).toBe('05');
    expect(component.addendas.at(0).get('information')?.value).toBe('');
    expect(component.addendas.at(0).get('information')?.hasError('required')).toBeTrue();
  });

  it('TransactionCreateComponent_ShouldNotSubmit_WhenFormInvalid', () => {
    component.form.patchValue({ transactionExternalId: '' });

    component.submit();

    expect(txApi.createTransaction).not.toHaveBeenCalled();
  });

  it('muestra resumen accesible con etiquetas funcionales, secciones y conteo', () => {
    component.form.patchValue({ companyEntryDescriptionId: null });
    component.addendas.at(0).patchValue({ information: '' });

    component.submit();
    fixture.detectChanges();

    const text = fixture.nativeElement.textContent;
    expect(text).toContain('Faltan datos para registrar la transacción');
    expect(text).toContain('Monto');
    expect(text).toContain('Institución destino');
    expect(text).toContain('Descripción de la entrada');
    expect(text).toContain('Información adicional · Addenda 1');
    expect(text).not.toContain('companyEntryDescriptionId');
    expect(text).not.toContain('[object Object]');
    expect(component.incompleteFieldCount).toBeGreaterThan(3);
    expect(fixture.nativeElement.querySelector('[role="alert"]')).not.toBeNull();
  });

  it('actualiza el resumen al corregir campos y expone validaciones cruzadas', () => {
    component.form.patchValue({ sourceAccountNumber: '1234567890', destinationAccountNumber: '1234567890' });
    component.submit();
    expect(component.validationIssues.some((x) => x.message.includes('deben ser diferentes'))).toBeTrue();

    const before = component.incompleteFieldCount;
    component.form.controls.amount.setValue('1.000');
    component.form.controls.amount.updateValueAndValidity();
    expect(component.incompleteFieldCount).toBeLessThan(before);
  });

  it('identifica el índice de la addenda y lleva el foco al campo seleccionado', () => {
    component.addAddenda();
    component.addendas.at(1).controls['information'].setValue('');
    component.submit();
    fixture.detectChanges();
    const issue = component.validationIssues.find((x) => x.path === 'addendas.1.information');
    expect(issue?.label).toContain('Addenda 2');

    component.focusIssue(issue!);
    expect(document.activeElement?.getAttribute('data-validation-path')).toBe('addendas.1.information');
  });

  it('mantiene el botón accionable, evita doble envío y conserva datos ante error backend', () => {
    const pending = new Subject<any>();
    txApi.createTransaction.and.returnValue(pending);
    fillValidForm();
    fixture.detectChanges();
    const submit = fixture.nativeElement.querySelector('button[type="submit"]') as HTMLButtonElement;
    expect(submit.disabled).toBeFalse();

    component.submit(); component.submit();
    expect(txApi.createTransaction).toHaveBeenCalledTimes(1);
    expect(component.isSubmitting.value).toBeTrue();

    pending.error(new Error('Cuenta destino no autorizada'));
    fixture.detectChanges();
    expect(component.form.controls.transactionExternalId.value).toBe('tx-001');
    expect(component.addendas.length).toBe(1);
    expect(fixture.nativeElement.textContent).toContain('Cuenta destino no autorizada');
    expect(component.isSubmitting.value).toBeFalse();
  });

  it('muestra explicación aunque el formulario incompleto aún no se haya enviado', () => {
    fixture.detectChanges();
    const submit = fixture.nativeElement.querySelector('button[type="submit"]') as HTMLButtonElement;
    expect(submit.disabled).toBeFalse();
    expect(fixture.nativeElement.textContent).toContain('campos con problemas');
  });

  it('TransactionCreateComponent_ShouldCreateTransaction_WhenValid', () => {
    txApi.createTransaction.and.returnValue(of({ id: 11 } as any));
    fillValidForm();

    component.submit();

    expect(txApi.createTransaction).toHaveBeenCalledTimes(1);
    expect(component.createdResponse.value).toEqual({ id: 11 } as any);
    expect(notifications.success).toHaveBeenCalledWith('Transacción creada correctamente');
  });

  it('TransactionCreateComponent_ShouldSupportPrenotificationWithZeroAmount', () => {
    txApi.createTransaction.and.returnValue(of({ id: 11 } as any));
    fillValidForm(0);
    component.form.patchValue({ isPrenotification: true, amount: 0 });
    component.activeDestinationAccounts = [activeThirdPartyAccount] as any;
    component.filteredDestinationAccounts = [activeThirdPartyAccount] as any;
    component.form.patchValue({
      destinationInstitutionId: activeThirdPartyAccount.destinationInstitutionId,
      destinationAccountNumber: activeThirdPartyAccount.destinationAccountNumber,
      recipientIdNumber: activeThirdPartyAccount.recipientIdNumber
    }, { emitEvent: false });
    component.form.updateValueAndValidity();

    component.submit();

    expect(txApi.createTransaction).toHaveBeenCalledTimes(1);
    const payload = txApi.createTransaction.calls.mostRecent().args[0] as any;
    expect(payload.isPrenotification).toBeTrue();
    expect(payload.amount).toBe(0);
  });

  it('TransactionCreateComponent_ShouldRequireAddendaInformation', () => {
    fillValidForm();
    component.addendas.at(0).patchValue({ information: '' });

    component.submit();

    expect(component.form.invalid).toBeTrue();
    expect(txApi.createTransaction).not.toHaveBeenCalled();
  });
});

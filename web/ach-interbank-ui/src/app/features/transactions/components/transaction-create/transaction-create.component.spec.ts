import { ComponentFixture, TestBed, fakeAsync, tick } from '@angular/core/testing';
import { NoopAnimationsModule } from '@angular/platform-browser/animations';
import { Router } from '@angular/router';
import { of, Subject } from 'rxjs';
import { NotificationService } from '../../../../core/services/notification.service';
import { CustomerSummary } from '../../../customers/models/customer.model';
import { CustomersApiService } from '../../../customers/services/customers-api.service';
import { FinancialInstitutionsApiService } from '../../services/financial-institutions-api.service';
import { TransactionsApiService } from '../../services/transactions-api.service';
import {
  ActiveThirdPartyAccount,
  CompanyEntryDescriptionOption,
  DestinationInstitution
} from '../../transactions.models';
import {
  AccountTypeEnum,
  FinancialInstitutionStatusEnum,
  TransactionTypeEnum
} from '../../transactions.types';
import { TransactionCreateComponent } from './transaction-create.component';

describe('TransactionCreateComponent', () => {
  let component: TransactionCreateComponent;
  let fixture: ComponentFixture<TransactionCreateComponent>;
  let txApi: jasmine.SpyObj<TransactionsApiService>;
  let customersApi: jasmine.SpyObj<CustomersApiService>;
  let institutionsApi: jasmine.SpyObj<FinancialInstitutionsApiService>;
  let notifications: jasmine.SpyObj<NotificationService>;
  const activeThirdPartyAccount: ActiveThirdPartyAccount = {
    id: 41,
    destinationAccountNumber: '9876543210',
    destinationInstitutionId: 7,
    recipientIdNumber: '10101010',
    destinationInstitutionName: 'Banco destino UAT'
  };
  const customers: CustomerSummary[] = [
    {
      id: 10,
      fullName: 'Ángela Pérez',
      documentType: 'CC',
      documentNumber: '12345678',
      accountNumber: '1234567890',
      accountNumbers: ['1234567890', '1234567891'],
      personType: 'PN',
      companyName: null
    },
    {
      id: 20,
      fullName: 'Empresa Nómina',
      documentType: 'NIT',
      documentNumber: '900123456',
      accountNumber: '2234567890',
      accountNumbers: ['2234567890'],
      personType: 'PJ',
      companyName: 'Empresa Nómina'
    }
  ];
  const institutions: DestinationInstitution[] = [
    {
      id: 7,
      name: 'Banco destino Ágil',
      routingNumber: '007',
      transitCode: '017',
      checkDigit: '1',
      isDefaultSource: false,
      status: FinancialInstitutionStatusEnum.Active
    },
    {
      id: 8,
      name: 'Banco inactivo',
      routingNumber: '008',
      transitCode: '018',
      checkDigit: '2',
      isDefaultSource: false,
      status: FinancialInstitutionStatusEnum.Inactive
    }
  ];

  beforeEach(async () => {
    txApi = jasmine.createSpyObj<TransactionsApiService>('TransactionsApiService', ['getCompanyEntryDescriptions', 'createTransaction', 'getActiveThirdParties', 'previewPolicy']);
    customersApi = jasmine.createSpyObj<CustomersApiService>('CustomersApiService', ['getAll']);
    institutionsApi = jasmine.createSpyObj<FinancialInstitutionsApiService>('FinancialInstitutionsApiService', ['getAll']);
    notifications = jasmine.createSpyObj<NotificationService>('NotificationService', ['success', 'error']);
    customersApi.getAll.and.returnValue(of(customers));
    institutionsApi.getAll.and.returnValue(of(institutions));

    await TestBed.configureTestingModule({
      imports: [TransactionCreateComponent, NoopAnimationsModule],
      providers: [
        { provide: TransactionsApiService, useValue: txApi },
        { provide: FinancialInstitutionsApiService, useValue: institutionsApi },
        { provide: CustomersApiService, useValue: customersApi },
        { provide: NotificationService, useValue: notifications },
        { provide: Router, useValue: jasmine.createSpyObj('Router', ['navigate']) }
      ]
    }).compileComponents();

    fixture = TestBed.createComponent(TransactionCreateComponent);
    component = fixture.componentInstance;
    const descriptions: CompanyEntryDescriptionOption[] = [
      { id: 1, term: 'NOMINAS', description: 'Nómina', standardEntryClassCode: 'PPD' },
      { id: 2, term: 'PAGOS', description: 'Pago de proveedores', standardEntryClassCode: 'CCD' }
    ];
    txApi.getCompanyEntryDescriptions.and.returnValue(of(descriptions));
    txApi.getActiveThirdParties.and.returnValue(of([activeThirdPartyAccount]));
    txApi.previewPolicy.and.returnValue(of({ canSubmit: true } as any));
    fixture.detectChanges();
  });

  function fillValidForm(amount: string | number = '1.000') {
    component.form.patchValue({
      amount,
      transactionExternalId: 'tx-001',
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
    component.activeDestinationAccounts = [activeThirdPartyAccount];
    component.filteredDestinationAccounts = [activeThirdPartyAccount];
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
    expect(payload.reference).toBeUndefined();
    expect(payload.legacyReference).toBeUndefined();
    expect(payload.legacyReferenceId).toBeUndefined();
    expect(payload.customerId).toBeUndefined();
    expect(payload.customerSearchControl).toBeUndefined();
    expect(payload.sourceAccountSearchControl).toBeUndefined();
    expect(payload.companyEntryDescriptionSearchControl).toBeUndefined();
    expect(payload.destinationInstitutionSearchControl).toBeUndefined();
    expect(payload.destinationAccountSearchControl).toBeUndefined();
    expect(typeof payload.destinationInstitutionId).toBe('number');
    expect(typeof payload.companyEntryDescriptionId).toBe('number');
    expect(typeof payload.sourceAccountNumber).toBe('string');
    expect(typeof payload.destinationAccountNumber).toBe('string');
  });

  it('filtra clientes por nombre, documento, cuenta y sin distinguir acentos', () => {
    component.customerSearchControl.setValue('angela');
    expect(component.filteredCustomerOptions.map((option) => option.value)).toEqual([10]);

    component.customerSearchControl.setValue('CC 12345678');
    expect(component.filteredCustomerOptions.map((option) => option.value)).toEqual([10]);

    component.customerSearchControl.setValue('1234567891');
    expect(component.filteredCustomerOptions.map((option) => option.value)).toEqual([10]);
  });

  it('selecciona cliente por ID, autocompleta origen y conserva el flujo de terceros', () => {
    const option = component.customerOptions.find((item) => item.value === 10)!;

    component.selectCustomer(option);

    expect(component.form.get('customerId')?.value).toBe(10);
    expect(component.form.get('sourceAccountNumber')?.value).toBe('1234567890');
    expect(component.form.get('companyIdentification')?.value).toBe('12345678');
    expect(component.form.get('sourcePersonType')?.value).toBe('PN');
    expect(component.selectedCustomerAccounts).toEqual(['1234567890', '1234567891']);
    expect(txApi.getActiveThirdParties).toHaveBeenCalledWith('1234567890');
  });

  it('al editar el cliente seleccionado limpia solo el ID y ejecuta el comportamiento manual vigente', () => {
    component.selectCustomer(component.customerOptions.find((item) => item.value === 10)!);
    const completedCompanyName = component.form.get('companyName')?.value;
    const completedSourceAccount = component.form.get('sourceAccountNumber')?.value;

    component.customerSearchControl.setValue('cliente arbitrario');

    expect(component.form.get('customerId')?.value).toBeNull();
    expect(component.form.get('customerId')?.hasError('invalidSelection')).toBeTrue();
    expect(component.selectedCustomerAccounts).toEqual([]);
    expect(component.form.get('companyName')?.value).toBe(completedCompanyName);
    expect(component.form.get('sourceAccountNumber')?.value).toBe(completedSourceAccount);
  });

  it('la opción manual conserva customerId nulo y una etiqueta segura', () => {
    const manualOption = component.customerOptions.find((item) => item.manual)!;

    component.selectCustomer(manualOption);

    expect(component.form.get('customerId')?.value).toBeNull();
    expect(component.displayCustomerOption(component.customerSearchControl.value)).toBe('Diligenciar manualmente');
    expect(component.customerSearchControl.valid).toBeTrue();
  });

  it('filtra y selecciona la cuenta de origen exacta sin consultas por cada pulsación', fakeAsync(() => {
    component.selectCustomer(component.customerOptions.find((item) => item.value === 10)!);
    txApi.getActiveThirdParties.calls.reset();

    component.sourceAccountSearchControl.setValue('7891');
    tick(300);

    expect(component.filteredSourceAccountOptions.map((option) => option.value)).toEqual(['1234567891']);
    expect(component.form.get('sourceAccountNumber')?.value).toBe('');
    expect(txApi.getActiveThirdParties).not.toHaveBeenCalled();

    component.selectSourceAccount(component.filteredSourceAccountOptions[0]);
    tick(300);

    expect(component.form.get('sourceAccountNumber')?.value).toBe('1234567891');
    expect(txApi.getActiveThirdParties).toHaveBeenCalledTimes(1);
    expect(txApi.getActiveThirdParties).toHaveBeenCalledWith('1234567891');
  }));

  it('mantiene el campo manual de cuenta origen cuando no existe cliente seleccionado', () => {
    component.clearCustomerSearch();
    fixture.detectChanges();

    const sourceInput = fixture.nativeElement.querySelector(
      'input[formcontrolname="sourceAccountNumber"][data-testid="transaction-source-account"]'
    );
    expect(component.selectedCustomerAccounts).toEqual([]);
    expect(sourceInput).not.toBeNull();
  });

  it('filtra descripción por texto y término, conserva ID y sincroniza NOMINAS', () => {
    expect(component.form.get('companyEntryDescriptionId')?.value).toBe(1);
    expect(component.displayCompanyEntryDescriptionOption(component.companyEntryDescriptionSearchControl.value))
      .toBe('Nómina (NOMINAS)');

    component.companyEntryDescriptionSearchControl.setValue('proveedores');
    expect(component.filteredCompanyEntryDescriptionOptions.map((option) => option.value)).toEqual([2]);

    component.companyEntryDescriptionSearchControl.setValue('pagos');
    expect(component.filteredCompanyEntryDescriptionOptions.map((option) => option.value)).toEqual([2]);
    component.selectCompanyEntryDescription(component.filteredCompanyEntryDescriptionOptions[0]);
    expect(component.form.get('companyEntryDescriptionId')?.value).toBe(2);
  });

  it('filtra solo entidades activas, conserva el ID y lo limpia al editar texto', () => {
    expect(component.destinationInstitutionOptions.map((option) => option.value)).toEqual([7]);

    component.destinationInstitutionSearchControl.setValue('banco agil');
    expect(component.filteredDestinationInstitutionOptions.map((option) => option.value)).toEqual([7]);
    component.selectDestinationInstitution(component.filteredDestinationInstitutionOptions[0]);
    expect(component.form.get('destinationInstitutionId')?.value).toBe(7);

    component.destinationInstitutionSearchControl.setValue('entidad arbitraria');
    expect(component.form.get('destinationInstitutionId')?.value).toBeNull();
    expect(component.destinationInstitutionSearchControl.hasError('invalidSelection')).toBeTrue();
  });

  it('filtra cuenta destino por cuenta, receptor y entidad y autocompleta el receptor', () => {
    component.form.patchValue({ sourceAccountNumber: '1234567890' }, { emitEvent: false });
    component.selectDestinationInstitution(component.destinationInstitutionOptions[0]);
    component.activeDestinationAccounts = [activeThirdPartyAccount];
    component.filteredDestinationAccounts = [activeThirdPartyAccount];
    component.form.get('destinationInstitutionId')?.setValue(7);

    component.destinationAccountOptions = [{
      value: activeThirdPartyAccount.destinationAccountNumber,
      label: `${activeThirdPartyAccount.destinationAccountNumber} · ${activeThirdPartyAccount.recipientIdNumber} · ${activeThirdPartyAccount.destinationInstitutionName}`,
      normalizedSearch: `${activeThirdPartyAccount.destinationAccountNumber} ${activeThirdPartyAccount.recipientIdNumber} banco destino uat`,
      source: activeThirdPartyAccount
    }];
    component.filteredDestinationAccountSearchOptions = [...component.destinationAccountOptions];

    component.destinationAccountSearchControl.setValue('10101010 banco destino');
    expect(component.filteredDestinationAccountSearchOptions).toHaveSize(1);
    component.selectDestinationAccount(component.filteredDestinationAccountSearchOptions[0]);

    expect(component.form.get('destinationAccountNumber')?.value).toBe('9876543210');
    expect(component.form.get('recipientIdNumber')?.value).toBe('10101010');
  });

  it('texto arbitrario en catálogo obligatorio bloquea envío, muestra mat-error y enfoca el autocomplete', fakeAsync(() => {
    fillValidForm();
    component.destinationInstitutionSearchControl.setValue('sin selección real');

    component.submit();
    tick();
    fixture.detectChanges();

    expect(txApi.createTransaction).not.toHaveBeenCalled();
    expect(component.form.get('destinationInstitutionId')?.value).toBeNull();
    expect(component.validationIssues.some((issue) => issue.path === 'destinationInstitutionId')).toBeTrue();
    expect(fixture.nativeElement.textContent).toContain('Seleccione una entidad financiera destino de la lista.');
    expect(document.activeElement?.getAttribute('data-testid')).toBe('transaction-destination-institution');
  }));

  it('prenotificación mantiene cuenta destino manual y monto cero', () => {
    component.form.get('isPrenotification')?.setValue(true);
    fixture.detectChanges();

    const manualDestination = fixture.nativeElement.querySelector(
      'input[formcontrolname="destinationAccountNumber"][data-testid="transaction-destination-account"]'
    );
    expect(component.form.get('amount')?.value).toBe(0);
    expect(manualDestination).not.toBeNull();
    expect(component.destinationAccountSearchControl.value).toBe('');
  });

  it('elimina Referencia legado del FormGroup y de la interfaz visible', () => {
    fixture.detectChanges();

    expect(component.form.contains('reference')).toBeFalse();
    expect(component.form.contains('legacyReference')).toBeFalse();
    expect(fixture.nativeElement.textContent).not.toContain('Referencia legado');
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
    expect(component.form.get('transactionExternalId')?.touched).toBeTrue();
  });

  it('rechaza valor cero y negativo en una transacción monetaria', () => {
    component.form.get('amount')?.setValue('0');
    expect(component.form.get('amount')?.hasError('nonPositiveAmount')).toBeTrue();

    component.form.get('amount')?.setValue('-1');
    expect(component.form.get('amount')?.hasError('nonPositiveAmount')).toBeTrue();
  });

  it('respeta longitudes máximas del contrato', () => {
    component.form.get('transactionExternalId')?.setValue('X'.repeat(65));
    component.form.get('companyName')?.setValue('N'.repeat(17));
    component.form.get('recipientName')?.setValue('R'.repeat(101));

    expect(component.form.get('transactionExternalId')?.hasError('maxlength')).toBeTrue();
    expect(component.form.get('companyName')?.hasError('maxlength')).toBeTrue();
    expect(component.form.get('recipientName')?.hasError('maxlength')).toBeTrue();
  });

  it('muestra resumen accesible con etiquetas funcionales, secciones y conteo', () => {
    component.form.patchValue({ companyEntryDescriptionId: null });
    component.addendas.at(0).patchValue({ information: '' });

    component.submit();
    fixture.detectChanges();

    const text = fixture.nativeElement.textContent;
    expect(text).toContain('Faltan datos para registrar la transacción');
    expect(text).toContain('Valor de la transacción');
    expect(text).toContain('Entidad financiera destino');
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

  it('aplica validadores condicionales y limpia la opción oculta de identidad', () => {
    component.form.patchValue({ type: TransactionTypeEnum.Credit, requiresIdentityValidation: true });
    expect(component.form.get('recipientIdNumber')?.hasError('required')).toBeTrue();

    component.form.patchValue({ requiresIdentityValidation: false });
    expect(component.form.get('recipientIdNumber')?.hasError('required')).toBeFalse();

    component.form.patchValue({ type: TransactionTypeEnum.Debit, requiresIdentityValidation: true });
    expect(component.form.get('requiresIdentityValidation')?.value).toBeFalse();
    expect(component.form.get('recipientIdNumber')?.hasError('required')).toBeTrue();
    expect(component.addendas.at(0).get('collectorId')?.hasError('required')).toBeTrue();

    component.form.patchValue({ type: TransactionTypeEnum.Credit });
    expect(component.addendas.at(0).get('collectorId')?.hasError('required')).toBeFalse();
  });

  it('renderiza controles editables con Angular Material y mensajes mat-error', () => {
    component.submit();
    fixture.detectChanges();

    expect(fixture.nativeElement.querySelectorAll('mat-form-field').length).toBeGreaterThan(10);
    expect(fixture.nativeElement.querySelectorAll('mat-select').length).toBeGreaterThan(3);
    expect(fixture.nativeElement.querySelectorAll('mat-autocomplete').length).toBeGreaterThanOrEqual(4);
    expect(fixture.nativeElement.querySelectorAll('.mat-mdc-form-field-error').length).toBeGreaterThan(5);
    expect(fixture.nativeElement.querySelector('input[formcontrolname="reference"]')).toBeNull();
    expect(fixture.nativeElement.textContent).not.toContain('[object Object]');
  });

  it('muestra explicación aunque el formulario incompleto aún no se haya enviado', () => {
    fixture.detectChanges();
    const submit = fixture.nativeElement.querySelector('button[type="submit"]') as HTMLButtonElement;
    expect(submit.disabled).toBeFalse();
    expect(fixture.nativeElement.textContent).toContain('Registre la operación en cuatro secciones');
    expect(fixture.nativeElement.querySelector('[data-testid="validation-summary"]')).toBeNull();
  });

  it('TransactionCreateComponent_ShouldCreateTransaction_WhenValid', () => {
    txApi.createTransaction.and.returnValue(of({ id: 11 } as any));
    fillValidForm();

    component.submit();

    expect(txApi.createTransaction).toHaveBeenCalledTimes(1);
    expect(component.createdResponse.value).toEqual({ id: 11 } as any);
    expect(notifications.success).toHaveBeenCalledWith('Transacción creada correctamente');
    expect(component.form.get('customerId')?.value).toBeNull();
    expect(component.customerSearchControl.value).toBe('');
    expect(component.sourceAccountSearchControl.value).toBe('');
    expect(component.destinationInstitutionSearchControl.value).toBe('');
    expect(component.destinationAccountSearchControl.value).toBe('');
    expect(component.form.get('companyEntryDescriptionId')?.value).toBe(1);
    expect(component.displayCompanyEntryDescriptionOption(component.companyEntryDescriptionSearchControl.value))
      .toBe('Nómina (NOMINAS)');
  });

  it('TransactionCreateComponent_ShouldSupportPrenotificationWithZeroAmount', () => {
    txApi.createTransaction.and.returnValue(of({ id: 11 } as any));
    fillValidForm(0);
    component.form.patchValue({ isPrenotification: true, amount: 0 });
    component.activeDestinationAccounts = [activeThirdPartyAccount];
    component.filteredDestinationAccounts = [activeThirdPartyAccount];
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

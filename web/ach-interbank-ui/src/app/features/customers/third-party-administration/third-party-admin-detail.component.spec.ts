import { HttpErrorResponse } from '@angular/common/http';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { MatDialog } from '@angular/material/dialog';
import { ActivatedRoute, Router } from '@angular/router';
import { of, throwError } from 'rxjs';
import { AuthService } from '../../../core/services/auth.service';
import { ClearingHousesApiService } from '../../../core/services/clearing-houses-api.service';
import { CustomersApiService } from '../../../core/services/customers-api.service';
import { FinancialInstitutionsApiService } from '../../../core/services/financial-institutions-api.service';
import { NotificationService } from '../../../core/services/notification.service';
import { TransactionsApiService } from '../../transactions/services/transactions-api.service';
import { ThirdPartyAdminDetailComponent } from './third-party-admin-detail.component';
import { ThirdPartyAdministrationApiService } from './third-party-administration-api.service';
import {
  PrenotificationValidationAttemptStatus,
  PrenotificationValidationResolution,
  ThirdPartyAccountReadDto,
  ThirdPartyAccountType,
  ThirdPartyAccountValidationStatus,
  ThirdPartyAdministrativeStatus,
  ThirdPartyTransactionNature
} from './third-party-administration.models';

describe('ThirdPartyAdminDetailComponent', () => {
  let fixture: ComponentFixture<ThirdPartyAdminDetailComponent>;
  let component: ThirdPartyAdminDetailComponent;
  let api: jasmine.SpyObj<ThirdPartyAdministrationApiService>;
  let auth: jasmine.SpyObj<AuthService>;
  let txApi: jasmine.SpyObj<TransactionsApiService>;

  const account: ThirdPartyAccountReadDto = {
    thirdPartyAccountId: 22,
    thirdPartyId: 11,
    customerId: 7,
    customerDisplayName: 'Cliente Prueba',
    thirdPartyDisplayName: 'Proveedor Uno',
    personType: 'PJ',
    documentType: 'NIT',
    documentNumberMasked: '••••5678',
    thirdPartyStatus: ThirdPartyAdministrativeStatus.Active,
    financialInstitutionId: 3,
    financialInstitutionName: 'Banco Demo',
    accountType: ThirdPartyAccountType.Savings,
    accountNumberMasked: '••••4321',
    accountStatus: ThirdPartyAdministrativeStatus.Active,
    validations: []
  };

  beforeEach(async () => {
    api = jasmine.createSpyObj<ThirdPartyAdministrationApiService>('ThirdPartyAdministrationApiService', [
      'getThirdParty', 'listAccounts', 'updateThirdParty', 'setThirdPartyStatus', 'createAccount', 'updateAccount',
      'setAccountStatus', 'getValidationHistory', 'requestRevalidation'
    ]);
    auth = jasmine.createSpyObj<AuthService>('AuthService', ['hasPermission']);
    txApi = jasmine.createSpyObj<TransactionsApiService>('TransactionsApiService', ['getCompanyEntryDescriptions', 'previewPolicy']);
    const customers = jasmine.createSpyObj<CustomersApiService>('CustomersApiService', ['getById']);
    const institutions = jasmine.createSpyObj<FinancialInstitutionsApiService>('FinancialInstitutionsApiService', ['getAll']);
    const clearingHouses = jasmine.createSpyObj<ClearingHousesApiService>('ClearingHousesApiService', ['list']);
    const notifications = jasmine.createSpyObj<NotificationService>('NotificationService', ['success', 'error']);
    const router = jasmine.createSpyObj<Router>('Router', ['navigate']);
    const dialog = jasmine.createSpyObj<MatDialog>('MatDialog', ['open']);

    auth.hasPermission.and.returnValue(true);
    customers.getById.and.returnValue(of({
      id: 7,
      firstName: 'Cliente',
      lastName: 'Prueba',
      personType: 'PN',
      documentType: 'CC',
      documentNumber: '11112222',
      accountNumber: '9999000011112222',
      accountNumbers: ['9999000011112222', '8888000033334444']
    }));
    institutions.getAll.and.returnValue(of([{ id: 3, name: 'Banco Demo', routingNumber: '1', transitCode: '1', checkDigit: '0', isDefaultSource: false, status: 1 }]));
    clearingHouses.list.and.returnValue(of([{ id: 2, name: 'CENIT', code: 'CENIT', isActive: true }]));
    txApi.getCompanyEntryDescriptions.and.returnValue(of([{ id: 9, term: 'PAGO', description: 'Pago', standardEntryClassCode: 'PPD' }]));
    api.getThirdParty.and.returnValue(of({
      thirdPartyId: 11,
      customerId: 7,
      personType: 'PJ',
      documentType: 'NIT',
      documentNumberMasked: '••••5678',
      displayName: 'Proveedor Uno',
      status: ThirdPartyAdministrativeStatus.Active,
      accountCount: 1,
      createdAt: '2026-10-11T10:00:00Z',
      updatedAt: '2026-10-11T11:00:00Z'
    }));
    api.listAccounts.and.returnValue(of({ items: [account], total: 1, page: 1, pageSize: 50 }));
    api.getValidationHistory.and.returnValue(of([{
      validationId: 31,
      thirdPartyAccountId: 22,
      clearingHouseId: 2,
      clearingHouseCode: 'CENIT',
      clearingHouseName: 'CENIT',
      transactionNature: ThirdPartyTransactionNature.Debit,
      serviceType: 'PPD',
      validationType: 1,
      status: ThirdPartyAccountValidationStatus.Rejected,
      effectiveDate: '2026-10-11',
      attempts: [{
        attemptId: 41,
        attemptNumber: 1,
        achTransactionId: 51,
        status: PrenotificationValidationAttemptStatus.Rejected,
        resolution: PrenotificationValidationResolution.Rejected,
        sentAtUtc: '2026-10-11T12:00:00Z',
        resolvedAtUtc: '2026-10-11T13:00:00Z',
        returnCode: 'R03',
        sourceCycleId: 'C2'
      }]
    }]));
    dialog.open.and.returnValue({ afterClosed: () => of(true) } as any);

    await TestBed.configureTestingModule({
      imports: [ThirdPartyAdminDetailComponent],
      providers: [
        { provide: ThirdPartyAdministrationApiService, useValue: api },
        { provide: AuthService, useValue: auth },
        { provide: CustomersApiService, useValue: customers },
        { provide: FinancialInstitutionsApiService, useValue: institutions },
        { provide: ClearingHousesApiService, useValue: clearingHouses },
        { provide: TransactionsApiService, useValue: txApi },
        { provide: NotificationService, useValue: notifications },
        { provide: Router, useValue: router },
        { provide: MatDialog, useValue: dialog },
        {
          provide: ActivatedRoute,
          useValue: {
            snapshot: { paramMap: { get: (key: string) => key === 'id' ? '7' : key === 'thirdPartyId' ? '11' : null } }
          }
        }
      ]
    }).compileComponents();

    fixture = TestBed.createComponent(ThirdPartyAdminDetailComponent);
    component = fixture.componentInstance;
    (component as any).dialog = dialog;
    fixture.changeDetectorRef.markForCheck();
    fixture.detectChanges();
  });

  it('muestra detalle/cuentas enmascaradas y carga historial con cámara, naturaleza, causal y trazabilidad', () => {
    expect(component.accounts.length).toBe(1);
    component.selectAccount(account);
    fixture.changeDetectorRef.markForCheck();
    fixture.detectChanges();

    const text = fixture.nativeElement.textContent as string;
    expect(text).toContain('••••4321');
    expect(text).toContain('CENIT');
    expect(text).toContain('Débito');
    expect(text).toContain('Rechazada');
    expect(text).toContain('R03');
    expect(text).toContain('TX 51');
    expect(text).not.toContain('9999000011112222');
  });

  it('crea y actualiza cuenta sin cámara; identidad bancaria no se edita silenciosamente', () => {
    api.createAccount.and.returnValue(of(account));
    api.updateAccount.and.returnValue(of({ ...account, accountStatus: ThirdPartyAdministrativeStatus.Inactive }));

    component.accountCreateForm.setValue({ financialInstitutionId: 3, accountType: ThirdPartyAccountType.Savings, accountNumber: '1234567890' });
    component.createAccount();
    const createCommand = api.createAccount.calls.mostRecent().args[2] as any;
    expect(createCommand.clearingHouseId).toBeUndefined();
    expect(createCommand.transactionNature).toBeUndefined();

    component.selectedAccount = account;
    component.accountEditForm.setValue({ status: ThirdPartyAdministrativeStatus.Inactive });
    component.saveAccountAdministrativeData();
    const updateCommand = api.updateAccount.calls.mostRecent().args[3] as any;
    expect(updateCommand.accountNumber).toBeUndefined();
    expect(updateCommand.financialInstitutionId).toBeUndefined();
    expect(updateCommand.status).toBe(ThirdPartyAdministrativeStatus.Inactive);
  });


  it('actualiza estados administrativos con los comandos dedicados sin convertirlos en elegibilidad', () => {
    api.setThirdPartyStatus.and.returnValue(of({
      thirdPartyId: 11,
      customerId: 7,
      personType: 'PJ',
      documentType: 'NIT',
      documentNumberMasked: '••••5678',
      displayName: 'Proveedor Uno',
      status: ThirdPartyAdministrativeStatus.Inactive,
      accountCount: 1,
      createdAt: '2026-10-11T10:00:00Z',
      updatedAt: '2026-10-11T12:00:00Z'
    }));
    api.setAccountStatus.and.returnValue(of({ ...account, accountStatus: ThirdPartyAdministrativeStatus.Inactive }));

    component.requestThirdPartyStatusChange();
    expect(api.setThirdPartyStatus).toHaveBeenCalledWith(7, 11, { status: ThirdPartyAdministrativeStatus.Inactive });
    expect(component.thirdParty?.status).toBe(ThirdPartyAdministrativeStatus.Inactive);

    component.requestAccountStatusChange(account);
    expect(api.setAccountStatus).toHaveBeenCalledWith(7, 11, 22, { status: ThirdPartyAdministrativeStatus.Inactive });
    expect(component.accounts[0].accountStatus).toBe(ThirdPartyAdministrativeStatus.Inactive);
  });

  it('solicita revalidación contextual desde backend sin recalcular policy en frontend', () => {
    component.selectAccount(account);
    component.toggleRevalidation();
    fixture.changeDetectorRef.markForCheck();
    fixture.detectChanges();
    expect(fixture.nativeElement.textContent).toContain('••••••••2222');
    expect(fixture.nativeElement.textContent).not.toContain('9999000011112222');
    component.revalidationForm.setValue({
      clearingHouseId: 2,
      transactionNature: ThirdPartyTransactionNature.Debit,
      effectiveDate: '2026-10-12',
      companyEntryDescriptionId: 9,
      sourceAccountIndex: 0
    });
    api.requestRevalidation.and.returnValue(of({
      achTransactionId: 52,
      validationId: 31,
      attemptId: 42,
      attemptNumber: 2,
      validationStatus: ThirdPartyAccountValidationStatus.PendingResponse,
      attemptStatus: PrenotificationValidationAttemptStatus.AwaitingResponse,
      responseDeadlineAtUtc: null,
      reusedExistingAttempt: false
    }));

    component.requestRevalidation();
    const command = api.requestRevalidation.calls.mostRecent().args[3];
    expect(command.clearingHouseId).toBe(2);
    expect(command.transactionNature).toBe(ThirdPartyTransactionNature.Debit);
    expect(command.serviceType).toBe('PPD');
    expect(command.companyEntryDescriptionId).toBe(9);
    expect(command.idempotencyKey.length).toBeGreaterThan(8);
    expect(txApi.previewPolicy).not.toHaveBeenCalled();
  });

  it('no renderiza controles manuales aprobar/rechazar y restringe escrituras sin CanManageAch', () => {
    component.selectAccount(account);
    fixture.changeDetectorRef.markForCheck();
    fixture.detectChanges();
    const buttons = Array.from(fixture.nativeElement.querySelectorAll('button')).map((button: any) => button.textContent.trim());
    expect(buttons.some((text) => /^Aprobar$/i.test(text))).toBeFalse();
    expect(buttons.some((text) => /^Rechazar$/i.test(text))).toBeFalse();

    auth.hasPermission.and.returnValue(false);
    (component as any).cdr.markForCheck();
    fixture.detectChanges();
    expect(fixture.nativeElement.querySelector('[data-testid="new-third-party-account"]')).toBeNull();
    expect(fixture.nativeElement.querySelector('[data-testid="request-revalidation"]')).toBeNull();
    expect(fixture.nativeElement.querySelector('[data-testid="account-status-action"]')).toBeNull();
  });

  it('maneja conflictos de comandos sin alterar la lectura productiva', () => {
    api.updateThirdParty.and.returnValue(throwError(() => new HttpErrorResponse({ status: 409, error: { message: 'Conflicto de identidad.' } })));
    component.thirdPartyEditForm.setValue({ displayName: 'Otro' });
    component.saveThirdParty();
    expect(component.commandError).toContain('Conflicto de identidad');
    expect(component.thirdParty?.displayName).toBe('Proveedor Uno');
  });
});

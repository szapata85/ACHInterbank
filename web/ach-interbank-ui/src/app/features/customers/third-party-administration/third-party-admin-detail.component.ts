import { HttpErrorResponse } from '@angular/common/http';
import { ChangeDetectionStrategy, ChangeDetectorRef, Component, OnInit, inject } from '@angular/core';
import { FormBuilder, Validators } from '@angular/forms';
import { MatDialog, MatDialogModule } from '@angular/material/dialog';
import { ActivatedRoute, Router, RouterModule } from '@angular/router';
import { forkJoin } from 'rxjs';
import { CustomerDetail } from '../../../core/models/customer.model';
import { DestinationInstitution } from '../../../core/models/financial-institution.model';
import { ClearingHouseOption } from '../../../core/models/clearing-house.model';
import { AuthService } from '../../../core/services/auth.service';
import { ClearingHousesApiService } from '../../../core/services/clearing-houses-api.service';
import { CustomersApiService } from '../../../core/services/customers-api.service';
import { FinancialInstitutionsApiService } from '../../../core/services/financial-institutions-api.service';
import { NotificationService } from '../../../core/services/notification.service';
import { getHttpErrorMessage } from '../../../core/utils/http-error-message.util';
import { OperationalConfirmDialogComponent, OperationalConfirmDialogData } from '../../../shared/components/operational-confirm-dialog.component';
import { SharedModule } from '../../../shared/shared.module';
import { CompanyEntryDescriptionOption } from '../../transactions/transactions.models';
import { TransactionsApiService } from '../../transactions/services/transactions-api.service';
import { ThirdPartyAdministrationApiService } from './third-party-administration-api.service';
import {
  ThirdPartyAccountReadDto,
  ThirdPartyAccountType,
  ThirdPartyAccountValidationHistoryDto,
  ThirdPartyAdministrativeStatus,
  ThirdPartyAdministrationDto,
  ThirdPartyTransactionNature
} from './third-party-administration.models';
import {
  accountTypeLabel,
  administrativeStatusLabel,
  administrativeStatusTone,
  attemptStatusLabel,
  maskForDisplay,
  transactionNatureLabel,
  validationStatusLabel,
  validationStatusTone
} from './third-party-administration.presentation';

@Component({
  selector: 'app-third-party-admin-detail',
  standalone: true,
  imports: [SharedModule, RouterModule, MatDialogModule],
  templateUrl: './third-party-admin-detail.component.html',
  styleUrls: ['./third-party-admin-detail.component.scss'],
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class ThirdPartyAdminDetailComponent implements OnInit {
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly fb = inject(FormBuilder);
  private readonly api = inject(ThirdPartyAdministrationApiService);
  private readonly customersApi = inject(CustomersApiService);
  private readonly institutionsApi = inject(FinancialInstitutionsApiService);
  private readonly clearingHousesApi = inject(ClearingHousesApiService);
  private readonly transactionsApi = inject(TransactionsApiService);
  private readonly auth = inject(AuthService);
  private readonly notifications = inject(NotificationService);
  private readonly dialog = inject(MatDialog);
  private readonly cdr = inject(ChangeDetectorRef);

  readonly ThirdPartyAdministrativeStatus = ThirdPartyAdministrativeStatus;
  readonly ThirdPartyAccountType = ThirdPartyAccountType;
  readonly ThirdPartyTransactionNature = ThirdPartyTransactionNature;
  readonly administrativeStatusLabel = administrativeStatusLabel;
  readonly administrativeStatusTone = administrativeStatusTone;
  readonly accountTypeLabel = accountTypeLabel;
  readonly validationStatusLabel = validationStatusLabel;
  readonly validationStatusTone = validationStatusTone;
  readonly transactionNatureLabel = transactionNatureLabel;
  readonly attemptStatusLabel = attemptStatusLabel;
  readonly maskForDisplay = maskForDisplay;

  customerId = 0;
  thirdPartyId = 0;
  customer: CustomerDetail | null = null;
  thirdParty: ThirdPartyAdministrationDto | null = null;
  accounts: ThirdPartyAccountReadDto[] = [];
  totalAccounts = 0;
  accountPage = 1;
  readonly accountPageSize = 50;
  selectedAccount: ThirdPartyAccountReadDto | null = null;
  validationHistory: ThirdPartyAccountValidationHistoryDto[] = [];

  institutions: DestinationInstitution[] = [];
  clearingHouses: ClearingHouseOption[] = [];
  companyDescriptions: CompanyEntryDescriptionOption[] = [];

  loading = true;
  accountsLoading = false;
  historyLoading = false;
  saving = false;
  revalidating = false;
  loadError = '';
  historyError = '';
  commandError = '';
  thirdPartyEditOpen = false;
  accountCreateOpen = false;
  accountEditOpen = false;
  revalidationOpen = false;
  private revalidationIdempotencyKey = this.newIdempotencyKey();

  readonly thirdPartyEditForm = this.fb.group({
    displayName: ['', [Validators.required, Validators.maxLength(200)]]
  });

  readonly accountCreateForm = this.fb.group({
    financialInstitutionId: [null as number | null, Validators.required],
    accountType: [null as ThirdPartyAccountType | null, Validators.required],
    accountNumber: ['', [Validators.required, Validators.maxLength(50)]]
  });

  readonly accountEditForm = this.fb.group({
    status: [ThirdPartyAdministrativeStatus.Active, Validators.required]
  });

  readonly revalidationForm = this.fb.group({
    clearingHouseId: [null as number | null, Validators.required],
    transactionNature: [null as ThirdPartyTransactionNature | null, Validators.required],
    effectiveDate: ['', Validators.required],
    companyEntryDescriptionId: [null as number | null, Validators.required],
    sourceAccountIndex: [null as number | null, Validators.required]
  });

  get canManage(): boolean {
    return this.auth.hasPermission('CanManageAch');
  }

  get totalAccountPages(): number {
    return Math.max(1, Math.ceil(this.totalAccounts / this.accountPageSize));
  }

  get customerSourceAccounts(): string[] {
    if (!this.customer) return [];
    const values = this.customer.accountNumbers?.length
      ? this.customer.accountNumbers
      : [this.customer.accountNumber];
    return values.filter((value): value is string => !!value?.trim());
  }

  ngOnInit(): void {
    this.customerId = Number(this.route.snapshot.paramMap.get('id'));
    this.thirdPartyId = Number(this.route.snapshot.paramMap.get('thirdPartyId'));
    if (!Number.isFinite(this.customerId) || !Number.isFinite(this.thirdPartyId) || this.customerId <= 0 || this.thirdPartyId <= 0) {
      void this.router.navigate(['/customers']);
      return;
    }
    this.loadAll();
  }

  loadAll(): void {
    this.loading = true;
    this.loadError = '';
    forkJoin({
      customer: this.customersApi.getById(this.customerId),
      thirdParty: this.api.getThirdParty(this.customerId, this.thirdPartyId),
      institutions: this.institutionsApi.getAll(false)
    }).subscribe({
      next: ({ customer, thirdParty, institutions }) => {
        this.customer = customer;
        this.thirdParty = thirdParty;
        this.institutions = institutions ?? [];
        this.thirdPartyEditForm.patchValue({ displayName: thirdParty.displayName }, { emitEvent: false });
        this.loading = false;
        this.loadAccounts(1);
        if (this.canManage) this.loadRevalidationReferences();
        this.cdr.markForCheck();
      },
      error: (error) => {
        this.loading = false;
        this.loadError = this.errorMessage(error, 'No fue posible cargar el detalle del tercero.');
        this.cdr.markForCheck();
      }
    });
  }

  loadAccounts(page = 1): void {
    this.accountsLoading = true;
    this.accountPage = page;
    this.api.listAccounts(this.customerId, this.thirdPartyId, { page, pageSize: this.accountPageSize }).subscribe({
      next: (response) => {
        this.accounts = response.items ?? [];
        this.totalAccounts = response.total ?? 0;
        this.accountPage = response.page ?? page;
        this.accountsLoading = false;
        if (this.selectedAccount) {
          this.selectedAccount = this.accounts.find((item) => item.thirdPartyAccountId === this.selectedAccount!.thirdPartyAccountId) ?? null;
        }
        this.cdr.markForCheck();
      },
      error: (error) => {
        this.accounts = [];
        this.totalAccounts = 0;
        this.accountsLoading = false;
        this.loadError = this.errorMessage(error, 'No fue posible cargar las cuentas del tercero.');
        this.cdr.markForCheck();
      }
    });
  }

  toggleThirdPartyEdit(): void {
    if (!this.canManage || !this.thirdParty) return;
    this.thirdPartyEditOpen = !this.thirdPartyEditOpen;
    this.commandError = '';
    this.thirdPartyEditForm.setValue({ displayName: this.thirdParty.displayName });
  }

  saveThirdParty(): void {
    if (!this.canManage || !this.thirdParty || this.saving || this.thirdPartyEditForm.invalid) {
      this.thirdPartyEditForm.markAllAsTouched();
      return;
    }
    this.saving = true;
    this.commandError = '';
    this.api.updateThirdParty(this.customerId, this.thirdPartyId, {
      displayName: this.thirdPartyEditForm.getRawValue().displayName!.trim()
    }).subscribe({
      next: (updated) => {
        this.thirdParty = updated;
        this.saving = false;
        this.thirdPartyEditOpen = false;
        this.notifications.success('Datos administrativos del tercero actualizados.');
        this.cdr.markForCheck();
      },
      error: (error) => this.finishCommandError(error, 'No fue posible actualizar el tercero.')
    });
  }

  requestThirdPartyStatusChange(): void {
    if (!this.canManage || !this.thirdParty) return;
    const target = this.thirdParty.status === ThirdPartyAdministrativeStatus.Active
      ? ThirdPartyAdministrativeStatus.Inactive
      : ThirdPartyAdministrativeStatus.Active;
    const activating = target === ThirdPartyAdministrativeStatus.Active;
    const data: OperationalConfirmDialogData = {
      title: activating ? 'Activar tercero administrativamente' : 'Desactivar tercero administrativamente',
      message: activating
        ? 'La activación administrativa no certifica elegibilidad interbancaria. ¿Deseas continuar?'
        : 'La desactivación administrativa bloqueará el uso contextual de sus cuentas sin cambiar resultados históricos de validación.',
      confirmLabel: activating ? 'Activar' : 'Desactivar',
      destructive: !activating
    };
    this.dialog.open(OperationalConfirmDialogComponent, { data, width: 'min(520px, calc(100vw - 2rem))' })
      .afterClosed().subscribe((confirmed) => {
        if (confirmed) this.setThirdPartyStatus(target);
      });
  }

  toggleAccountCreate(): void {
    if (!this.canManage) return;
    this.accountCreateOpen = !this.accountCreateOpen;
    this.commandError = '';
    if (!this.accountCreateOpen) {
      this.accountCreateForm.reset({ financialInstitutionId: null, accountType: null, accountNumber: '' });
    }
  }

  createAccount(): void {
    if (!this.canManage || this.saving || this.accountCreateForm.invalid) {
      this.accountCreateForm.markAllAsTouched();
      return;
    }
    const raw = this.accountCreateForm.getRawValue();
    this.saving = true;
    this.commandError = '';
    this.api.createAccount(this.customerId, this.thirdPartyId, {
      financialInstitutionId: Number(raw.financialInstitutionId),
      accountType: Number(raw.accountType) as ThirdPartyAccountType,
      accountNumber: raw.accountNumber!.trim()
    }).subscribe({
      next: (created) => {
        this.saving = false;
        this.accountCreateOpen = false;
        this.accountCreateForm.reset({ financialInstitutionId: null, accountType: null, accountNumber: '' });
        this.notifications.success('Cuenta registrada.');
        this.loadAccounts(1);
        this.selectAccount(created);
      },
      error: (error) => this.finishCommandError(error, 'No fue posible registrar la cuenta.')
    });
  }

  selectAccount(account: ThirdPartyAccountReadDto): void {
    this.selectedAccount = account;
    this.accountEditOpen = false;
    this.revalidationOpen = false;
    this.accountEditForm.setValue({ status: account.accountStatus });
    this.loadHistory();
  }

  toggleAccountEdit(): void {
    if (!this.canManage || !this.selectedAccount) return;
    this.accountEditOpen = !this.accountEditOpen;
    this.commandError = '';
    this.accountEditForm.setValue({ status: this.selectedAccount.accountStatus });
  }

  saveAccountAdministrativeData(): void {
    if (!this.canManage || !this.selectedAccount || this.saving || this.accountEditForm.invalid) return;
    this.saving = true;
    this.commandError = '';
    const status = Number(this.accountEditForm.getRawValue().status) as ThirdPartyAdministrativeStatus;
    this.api.updateAccount(this.customerId, this.thirdPartyId, this.selectedAccount.thirdPartyAccountId, { status }).subscribe({
      next: (updated) => {
        this.saving = false;
        this.accountEditOpen = false;
        this.replaceSelectedAccount(updated);
        this.notifications.success('Datos administrativos de la cuenta actualizados.');
      },
      error: (error) => this.finishCommandError(error, 'No fue posible actualizar la cuenta.')
    });
  }

  requestAccountStatusChange(account: ThirdPartyAccountReadDto): void {
    if (!this.canManage) return;
    const target = account.accountStatus === ThirdPartyAdministrativeStatus.Active
      ? ThirdPartyAdministrativeStatus.Inactive
      : ThirdPartyAdministrativeStatus.Active;
    const activating = target === ThirdPartyAdministrativeStatus.Active;
    const data: OperationalConfirmDialogData = {
      title: activating ? 'Activar cuenta administrativamente' : 'Desactivar cuenta administrativamente',
      message: activating
        ? 'La activación administrativa no significa que la cuenta esté habilitada para transferir. ¿Deseas continuar?'
        : 'La desactivación administrativa no elimina ni reescribe el historial de validaciones.',
      confirmLabel: activating ? 'Activar' : 'Desactivar',
      destructive: !activating
    };
    this.dialog.open(OperationalConfirmDialogComponent, { data, width: 'min(520px, calc(100vw - 2rem))' })
      .afterClosed().subscribe((confirmed) => {
        if (confirmed) this.setAccountStatus(account, target);
      });
  }

  loadHistory(): void {
    if (!this.selectedAccount) return;
    this.historyLoading = true;
    this.historyError = '';
    this.api.getValidationHistory(this.customerId, this.thirdPartyId, this.selectedAccount.thirdPartyAccountId).subscribe({
      next: (history) => {
        this.validationHistory = history ?? [];
        this.historyLoading = false;
        this.cdr.markForCheck();
      },
      error: (error) => {
        this.validationHistory = [];
        this.historyLoading = false;
        this.historyError = this.errorMessage(error, 'No fue posible cargar el historial de validaciones.');
        this.cdr.markForCheck();
      }
    });
  }

  toggleRevalidation(): void {
    if (!this.canManage || !this.selectedAccount) return;
    this.revalidationOpen = !this.revalidationOpen;
    this.commandError = '';
    if (this.revalidationOpen) {
      this.revalidationIdempotencyKey = this.newIdempotencyKey();
      this.revalidationForm.reset({
        clearingHouseId: null,
        transactionNature: null,
        effectiveDate: '',
        companyEntryDescriptionId: null,
        sourceAccountIndex: null
      });
    }
  }

  requestRevalidation(): void {
    if (!this.canManage || !this.selectedAccount || this.revalidating || this.revalidationForm.invalid) {
      this.revalidationForm.markAllAsTouched();
      return;
    }
    const raw = this.revalidationForm.getRawValue();
    const companyDescription = this.companyDescriptions.find((option) => option.id === Number(raw.companyEntryDescriptionId));
    if (!companyDescription) {
      this.commandError = 'Selecciona una descripción de entrada válida.';
      return;
    }
    const sourceAccountNumber = this.customerSourceAccounts[Number(raw.sourceAccountIndex)];
    if (!sourceAccountNumber) {
      this.commandError = 'Selecciona una cuenta origen válida del cliente.';
      return;
    }

    this.revalidating = true;
    this.commandError = '';
    this.api.requestRevalidation(this.customerId, this.thirdPartyId, this.selectedAccount.thirdPartyAccountId, {
      clearingHouseId: Number(raw.clearingHouseId),
      transactionNature: Number(raw.transactionNature) as ThirdPartyTransactionNature,
      effectiveDate: raw.effectiveDate!,
      serviceType: companyDescription.standardEntryClassCode,
      companyEntryDescriptionId: companyDescription.id,
      sourceAccountNumber,
      idempotencyKey: this.revalidationIdempotencyKey
    }).subscribe({
      next: (result) => {
        this.revalidating = false;
        this.revalidationOpen = false;
        this.revalidationIdempotencyKey = this.newIdempotencyKey();
        this.notifications.success(result.reusedExistingAttempt
          ? 'La solicitud ya existía y se reutilizó de forma idempotente.'
          : 'Nueva prenotificación/revalidación solicitada. El resultado se resolverá automáticamente.');
        this.loadHistory();
        this.cdr.markForCheck();
      },
      error: (error) => {
        this.revalidating = false;
        this.commandError = this.errorMessage(error, 'No fue posible solicitar la revalidación.');
        this.cdr.markForCheck();
      }
    });
  }

  previousAccountPage(): void {
    if (this.accountPage > 1 && !this.accountsLoading) this.loadAccounts(this.accountPage - 1);
  }

  nextAccountPage(): void {
    if (this.accountPage < this.totalAccountPages && !this.accountsLoading) this.loadAccounts(this.accountPage + 1);
  }

  private setThirdPartyStatus(status: ThirdPartyAdministrativeStatus): void {
    this.saving = true;
    this.commandError = '';
    this.api.setThirdPartyStatus(this.customerId, this.thirdPartyId, { status }).subscribe({
      next: (updated) => {
        this.thirdParty = updated;
        this.saving = false;
        this.notifications.success('Estado administrativo del tercero actualizado.');
        this.cdr.markForCheck();
      },
      error: (error) => this.finishCommandError(error, 'No fue posible actualizar el estado del tercero.')
    });
  }

  private setAccountStatus(account: ThirdPartyAccountReadDto, status: ThirdPartyAdministrativeStatus): void {
    this.saving = true;
    this.commandError = '';
    this.api.setAccountStatus(this.customerId, this.thirdPartyId, account.thirdPartyAccountId, { status }).subscribe({
      next: (updated) => {
        this.saving = false;
        this.replaceSelectedAccount(updated);
        this.notifications.success('Estado administrativo de la cuenta actualizado.');
      },
      error: (error) => this.finishCommandError(error, 'No fue posible actualizar el estado de la cuenta.')
    });
  }

  private replaceSelectedAccount(updated: ThirdPartyAccountReadDto): void {
    this.accounts = this.accounts.map((item) => item.thirdPartyAccountId === updated.thirdPartyAccountId ? updated : item);
    if (this.selectedAccount?.thirdPartyAccountId === updated.thirdPartyAccountId) {
      this.selectedAccount = updated;
    }
    this.cdr.markForCheck();
  }

  private loadRevalidationReferences(): void {
    forkJoin({
      clearingHouses: this.clearingHousesApi.list(),
      companyDescriptions: this.transactionsApi.getCompanyEntryDescriptions()
    }).subscribe({
      next: ({ clearingHouses, companyDescriptions }) => {
        this.clearingHouses = (clearingHouses ?? []).filter((item) => item.isActive !== false);
        this.companyDescriptions = companyDescriptions ?? [];
        this.cdr.markForCheck();
      },
      error: () => {
        this.clearingHouses = [];
        this.companyDescriptions = [];
        this.cdr.markForCheck();
      }
    });
  }

  private finishCommandError(error: unknown, fallback: string): void {
    this.saving = false;
    this.commandError = this.errorMessage(error, fallback);
    this.cdr.markForCheck();
  }

  private errorMessage(error: unknown, fallback: string): string {
    if (error instanceof HttpErrorResponse && error.status === 403) return 'No tienes permisos para realizar esta operación.';
    if (error instanceof HttpErrorResponse && error.status === 409) return getHttpErrorMessage(error, 'La operación entra en conflicto con el estado actual.');
    return getHttpErrorMessage(error, fallback);
  }

  private newIdempotencyKey(): string {
    const cryptoApi = globalThis.crypto as Crypto | undefined;
    if (cryptoApi?.randomUUID) return cryptoApi.randomUUID();
    return `tpa6-${Date.now()}-${Math.random().toString(36).slice(2, 18)}`;
  }
}

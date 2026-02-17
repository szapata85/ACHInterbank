import { ChangeDetectionStrategy, ChangeDetectorRef, Component, OnDestroy, OnInit, inject } from '@angular/core';
import { FormArray, FormBuilder, FormControl, FormGroup, Validators } from '@angular/forms';
import { debounceTime, distinctUntilChanged, map, shareReplay, take, takeUntil, tap } from 'rxjs';
import { Subject } from 'rxjs';
import { Router } from '@angular/router';
import { TransactionsApiService } from '../../services/transactions-api.service';
import { ActiveThirdPartyAccount, TransactionDraft, TransactionResponse } from '../../transactions.models';
import { AccountTypeEnum, FinancialInstitutionStatusEnum, TransactionTypeEnum } from '../../transactions.types';
import { NotificationService } from '../../../../core/services/notification.service';
import { SharedModule } from '../../../../shared/shared.module';
import { FinancialInstitutionsApiService } from '../../services/financial-institutions-api.service';
import { CustomersApiService } from '../../../customers/services/customers-api.service';
import { CustomerSummary } from '../../../customers/models/customer.model';

@Component({
  selector: 'app-transaction-create',
  standalone: true,
  imports: [SharedModule],
  templateUrl: './transaction-create.component.html',
  styleUrls: ['./transaction-create.component.scss'],
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class TransactionCreateComponent implements OnInit, OnDestroy {
  private readonly fb = inject(FormBuilder);
  private readonly api = inject(TransactionsApiService);
  private readonly cdr = inject(ChangeDetectorRef);
  private readonly router = inject(Router);
  private readonly notifications = inject(NotificationService);
  private readonly financialInstitutionsApi = inject(FinancialInstitutionsApiService);
  private readonly customersApi = inject(CustomersApiService);
  private readonly destroy$ = new Subject<void>();

  readonly TransactionType = TransactionTypeEnum;
  readonly AccountType = AccountTypeEnum;
  readonly customers$ = this.customersApi.getAll().pipe(
    map((list) =>
      (list ?? [])
        .filter((item) => (item.accountNumbers?.length ?? 0) > 0 || !!item.accountNumber)
        .sort((a, b) => a.fullName.localeCompare(b.fullName))
    ),
    shareReplay({ bufferSize: 1, refCount: true })
  );
  readonly institutions$ = this.financialInstitutionsApi.getAll().pipe(
    map((list) =>
      (list ?? [])
        .filter((item) => item.status === FinancialInstitutionStatusEnum.Active)
        .sort((a, b) => a.name.localeCompare(b.name))
    )
  );
  readonly form: FormGroup = this.fb.group({
    customerId: [null],
    amount: ['', [Validators.required]],
    reference: ['', [Validators.required, Validators.maxLength(30)]],
    type: [TransactionTypeEnum.Credit, Validators.required],
    accountType: [AccountTypeEnum.Checking, Validators.required],
    isPrenotification: [false],
    destinationInstitutionId: [null, [Validators.required, Validators.min(1)]],
    sourceAccountNumber: ['', [Validators.required, Validators.pattern(/^[0-9]{6,18}$/)]],
    destinationAccountNumber: ['', [Validators.required, Validators.pattern(/^[0-9]{6,18}$/)]],
    recipientIdNumber: [''],
    requiresIdentityValidation: [false],
    companyName: ['', [Validators.required, Validators.maxLength(16)]],
    companyIdentification: ['', [Validators.required, Validators.pattern(/^[A-Z0-9]{4,10}$/)]],
    companyEntryDescription: ['PAGOS', [Validators.required, Validators.maxLength(10)]],
    addendas: this.fb.array([])
  });

  readonly isSubmitting = new FormControl(false, { nonNullable: true });
  readonly errorMessage = new FormControl<string | null>(null);
  readonly successMessage = new FormControl<string | null>(null);
  readonly createdResponse = new FormControl<TransactionResponse | null>(null);

  activeDestinationAccounts: ActiveThirdPartyAccount[] = [];
  filteredDestinationAccounts: ActiveThirdPartyAccount[] = [];
  selectedCustomerAccounts: string[] = [];
  private readonly amountFormatter = new Intl.NumberFormat('es-CO', {
    minimumFractionDigits: 0,
    maximumFractionDigits: 0
  });

  ngOnInit(): void {
    this.form.setValidators([this.validateAccountDifference, this.validateBusinessRules]);
    this.form.valueChanges.pipe(takeUntil(this.destroy$)).subscribe(() => {
      this.errorMessage.setValue(null);
      this.successMessage.setValue(null);
    });


    this.form
      .get('customerId')
      ?.valueChanges.pipe(takeUntil(this.destroy$))
      .subscribe((customerId) => {
        if (!customerId) {
          this.selectedCustomerAccounts = [];
          this.loadActiveDestinationAccounts();
          this.cdr.markForCheck();
          return;
        }

        this.customers$.pipe(take(1), takeUntil(this.destroy$)).subscribe((customers) => {
          const selected = (customers ?? []).find((item) => item.id === Number(customerId));
          if (!selected) {
            return;
          }

          const accounts = ((selected.accountNumbers?.length ? selected.accountNumbers : [selected.accountNumber]) ?? [])
            .filter((item) => !!item);
          this.selectedCustomerAccounts = accounts;

          this.form.patchValue({
            sourceAccountNumber: accounts[0] ?? '',
            companyName: this.normalizeCompanyName(selected),
            companyIdentification: selected.documentNumber
          }, { emitEvent: false });

          this.loadActiveDestinationAccounts();
          this.cdr.markForCheck();
        });
      });

    this.form
      .get('sourceAccountNumber')
      ?.valueChanges.pipe(debounceTime(250), distinctUntilChanged(), takeUntil(this.destroy$))
      .subscribe(() => this.loadActiveDestinationAccounts());

    this.form
      .get('destinationInstitutionId')
      ?.valueChanges.pipe(takeUntil(this.destroy$))
      .subscribe(() => this.filterActiveDestinationAccounts());

    this.form
      .get('isPrenotification')
      ?.valueChanges.pipe(takeUntil(this.destroy$))
      .subscribe((isPrenotification) => {
        const amountControl = this.form.get('amount');
        if (!amountControl) {
          return;
        }

        const validators = isPrenotification
          ? [Validators.required, Validators.min(0), Validators.max(0)]
          : [Validators.required, Validators.min(0.01)];

        amountControl.setValidators(validators);
        if (isPrenotification) {
          amountControl.setValue(0, { emitEvent: false });
        }

        this.loadActiveDestinationAccounts();
        amountControl.updateValueAndValidity({ emitEvent: false });
      });
  }

  ngOnDestroy(): void {
    this.destroy$.next();
    this.destroy$.complete();
  }

  get addendas(): FormArray<FormGroup> {
    return this.form.get('addendas') as FormArray<FormGroup>;
  }

  addAddenda(): void {
    this.addendas.push(
      this.fb.group({
        addendaType: ['05', [Validators.required]],
        information: ['', [Validators.required, Validators.maxLength(80)]]
      })
    );
  }

  removeAddenda(index: number): void {
    this.addendas.removeAt(index);
  }

  submit(): void {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    const payload = this.form.getRawValue() as TransactionDraft;
    const parsedAmount = this.parseMaskedAmount(payload.amount);
    if (parsedAmount === null || (!Boolean(payload.isPrenotification) && parsedAmount <= 0)) {
      this.form.get('amount')?.setErrors({ invalidAmount: true });
      this.form.get('amount')?.markAsTouched();
      return;
    }

    const sanitized: TransactionDraft = {
      ...payload,
      type: Number(payload.type) as TransactionTypeEnum,
      accountType: Number(payload.accountType) as AccountTypeEnum,
      isPrenotification: Boolean(payload.isPrenotification),
      amount: parsedAmount,
      destinationInstitutionId: Number(payload.destinationInstitutionId),
      reference: payload.reference.trim(),
      sourceAccountNumber: payload.sourceAccountNumber.trim(),
      destinationAccountNumber: payload.destinationAccountNumber.trim(),
      recipientIdNumber: payload.recipientIdNumber?.trim() || undefined,
      requiresIdentityValidation: Boolean(payload.requiresIdentityValidation),
      companyName: payload.companyName.trim(),
      companyIdentification: payload.companyIdentification.trim().toUpperCase(),
      companyEntryDescription: payload.companyEntryDescription.trim().toUpperCase(),
      addendas: payload.addendas
        .map((item) => this.buildAddendaPayload(item))
        .filter((item) => item.addendaType && item.information)
    };

    this.isSubmitting.setValue(true);
    this.errorMessage.setValue(null);
    this.successMessage.setValue(null);

    this.api
      .createTransaction(sanitized)
      .pipe(
        take(1),
        tap({
          next: (response) => {
            this.createdResponse.setValue(response);
            this.successMessage.setValue('Transacción creada correctamente.');
            this.notifications.success('Transacción creada correctamente');
            this.isSubmitting.setValue(false);
            this.form.reset({
              customerId: null,
              type: TransactionTypeEnum.Credit,
              accountType: AccountTypeEnum.Checking,
              isPrenotification: false,
              companyEntryDescription: 'PAGOS'
            });
            this.addendas.clear();
            this.activeDestinationAccounts = [];
            this.filteredDestinationAccounts = [];
            this.selectedCustomerAccounts = [];
            this.cdr.markForCheck();
            this.router.navigate(['/transactions']);
          },
          error: (error: Error) => {
            this.isSubmitting.setValue(false);
            this.successMessage.setValue(null);
            this.errorMessage.setValue(error.message || 'Ocurrió un error inesperado');
            this.notifications.error(this.errorMessage.value ?? 'Error al crear la transacción');
            this.cdr.markForCheck();
          }
        })
      )
      .subscribe();
  }


  get sourceAccountRequiredForDestinationSelection(): boolean {
    if (Boolean(this.form.get('isPrenotification')?.value)) {
      return false;
    }

    return !String(this.form.get('sourceAccountNumber')?.value ?? '').trim();
  }

  trackAddenda(index: number): number {
    return index;
  }

  get amountPreviewValue(): number {
    return this.parseMaskedAmount(this.form.get('amount')?.value) ?? 0;
  }

  onAmountInput(event: Event): void {
    const input = event.target as HTMLInputElement;
    const formatted = this.formatMaskedAmount(input.value);
    input.value = formatted;
    this.form.get('amount')?.setValue(formatted, { emitEvent: false });
    this.form.get('amount')?.updateValueAndValidity({ emitEvent: false });
  }


  private normalizeCompanyName(customer: CustomerSummary): string {
    const raw = (customer.companyName?.trim() || customer.fullName.trim()).toUpperCase();
    return raw.length > 16 ? raw.slice(0, 16) : raw;
  }

  private validateAccountDifference = (group: FormGroup) => {
    const source = group.get('sourceAccountNumber')?.value;
    const destination = group.get('destinationAccountNumber')?.value;
    if (source && destination && source === destination) {
      return { sameAccount: true };
    }
    return null;
  };



  onDestinationAccountSelected(accountNumber: string): void {
    const selected = this.filteredDestinationAccounts.find((item) => item.destinationAccountNumber === accountNumber);
    if (!selected) {
      return;
    }

    this.form.patchValue({ recipientIdNumber: selected.recipientIdNumber }, { emitEvent: false });
  }

  private loadActiveDestinationAccounts(): void {
    const selectedIsPrenotification = Boolean(this.form.get('isPrenotification')?.value);
    const sourceAccountNumber = String(this.form.get('sourceAccountNumber')?.value ?? '').trim();

    if (selectedIsPrenotification || !sourceAccountNumber) {
      this.activeDestinationAccounts = [];
      this.filteredDestinationAccounts = [];
      this.form.patchValue({ destinationAccountNumber: '' }, { emitEvent: false });
      this.cdr.markForCheck();
      return;
    }

    this.api
      .getActiveThirdParties(sourceAccountNumber)
      .pipe(take(1), takeUntil(this.destroy$))
      .subscribe((items) => {
        this.activeDestinationAccounts = items;
        this.filterActiveDestinationAccounts();
        this.cdr.markForCheck();
      });
  }

  private filterActiveDestinationAccounts(): void {
    const institutionId = Number(this.form.get('destinationInstitutionId')?.value);
    const selectedIsPrenotification = Boolean(this.form.get('isPrenotification')?.value);

    if (selectedIsPrenotification || this.sourceAccountRequiredForDestinationSelection) {
      this.filteredDestinationAccounts = [];
      this.form.patchValue({ destinationAccountNumber: '' }, { emitEvent: false });
      this.cdr.markForCheck();
      return;
    }

    this.filteredDestinationAccounts = institutionId > 0
      ? this.activeDestinationAccounts.filter((item) => item.destinationInstitutionId === institutionId)
      : this.activeDestinationAccounts;

    const currentDestination = this.form.get('destinationAccountNumber')?.value;
    const exists = this.filteredDestinationAccounts.some((item) => item.destinationAccountNumber === currentDestination);
    if (!exists) {
      this.form.patchValue({ destinationAccountNumber: '' }, { emitEvent: false });
    }

    this.cdr.markForCheck();
  }

  private validateBusinessRules = (group: FormGroup) => {
    const isPrenotification = Boolean(group.get('isPrenotification')?.value);
    const amount = this.parseMaskedAmount(group.get('amount')?.value) ?? 0;
    const type = Number(group.get('type')?.value) as TransactionTypeEnum;
    const recipientId = group.get('recipientIdNumber')?.value;
    const requiresIdentityValidation = Boolean(group.get('requiresIdentityValidation')?.value);
    const addendas = group.get('addendas') as FormArray<FormGroup>;

    const errors: Record<string, boolean> = {};

    if (isPrenotification && amount !== 0) {
      errors.prenoteAmount = true;
    }

    if (type === TransactionTypeEnum.Debit && !recipientId) {
      errors.missingRecipientId = true;
    }

    if (type === TransactionTypeEnum.Credit && requiresIdentityValidation && !recipientId) {
      errors.missingRecipientId = true;
    }

    if (!addendas || addendas.length === 0) {
      errors.missingAddenda = true;
    }

    if (addendas && addendas.length > 0) {
      const invalidAddenda = addendas.controls.some((control) => {
        const type = control.get('addendaType')?.value;
        return type !== '05';
      });

      if (invalidAddenda) {
        errors.invalidAddenda = true;
      }
    }

    return Object.keys(errors).length > 0 ? errors : null;
  };

  private buildAddendaPayload(item: TransactionDraft['addendas'][number]) {
    return {
      addendaType: item.addendaType?.trim().toUpperCase(),
      information: item.information.trim()
    };
  }

  private formatMaskedAmount(rawValue: string): string {
    const raw = String(rawValue ?? '');
    if (!raw.trim()) {
      return '';
    }

    const normalized = raw
      .replace(/\./g, '')
      .replace(/[^\d,]/g, '');

    if (!normalized) {
      return '';
    }

    const separatorIndex = normalized.indexOf(',');
    if (separatorIndex === -1) {
      const digits = normalized.replace(/\D/g, '');
      return digits ? this.amountFormatter.format(Number(digits)) : '';
    }

    const integerDigits = normalized.slice(0, separatorIndex).replace(/\D/g, '');
    const decimalDigits = normalized.slice(separatorIndex + 1).replace(/\D/g, '').slice(0, 2);
    const integerFormatted = integerDigits ? this.amountFormatter.format(Number(integerDigits)) : '0';
    const hasTrailingSeparator = separatorIndex === normalized.length - 1;

    if (decimalDigits) {
      return `${integerFormatted},${decimalDigits}`;
    }

    return hasTrailingSeparator ? `${integerFormatted},` : integerFormatted;
  }

  private parseMaskedAmount(value: unknown): number | null {
    if (typeof value === 'number') {
      return Number.isFinite(value) ? value : null;
    }

    const raw = String(value ?? '').trim();
    if (!raw) {
      return null;
    }

    const normalized = raw.replace(/\./g, '').replace(',', '.').replace(/[^\d.]/g, '');
    if (!normalized) {
      return null;
    }

    const parsed = Number(normalized);
    return Number.isFinite(parsed) ? parsed : null;
  }
}

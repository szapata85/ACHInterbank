import { ChangeDetectionStrategy, ChangeDetectorRef, Component, ElementRef, OnDestroy, OnInit, ViewChild, inject } from '@angular/core';
import { AbstractControl, FormArray, FormBuilder, FormControl, FormGroup, ValidationErrors, ValidatorFn, Validators } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MatCheckboxModule } from '@angular/material/checkbox';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatSelectModule } from '@angular/material/select';
import { EMPTY, catchError, debounceTime, distinctUntilChanged, finalize, map, shareReplay, take, takeUntil, tap } from 'rxjs';
import { Subject } from 'rxjs';
import { Router } from '@angular/router';
import { TransactionsApiService } from '../../services/transactions-api.service';
import { ActiveThirdPartyAccount, CompanyEntryDescriptionOption, TransactionDraft, TransactionPolicyPreview, TransactionResponse } from '../../transactions.models';
import { AccountTypeEnum, FinancialInstitutionStatusEnum, TransactionTypeEnum } from '../../transactions.types';
import { NotificationService } from '../../../../core/services/notification.service';
import { SharedModule } from '../../../../shared/shared.module';
import { FinancialInstitutionsApiService } from '../../services/financial-institutions-api.service';
import { CustomersApiService } from '../../../customers/services/customers-api.service';
import { CustomerSummary } from '../../../customers/models/customer.model';
import { recipientIdentityValidator } from '../../transaction-policy.validators';

type TransactionSection = 'operation' | 'originator' | 'recipient' | 'concept' | 'review';

interface TransactionValidationIssue {
  path: string;
  label: string;
  message: string;
  section: TransactionSection;
}

interface TransactionSectionState {
  key: TransactionSection;
  label: string;
  symbol: '✓' | '!' | '○';
  status: 'complete' | 'attention' | 'pending';
  errorCount: number;
}

const MAX_TRANSACTION_AMOUNT = 9_999_999_999_999_999.99;

function parseMonetaryValue(value: unknown): number | null {
  if (typeof value === 'number') {
    return Number.isFinite(value) ? value : null;
  }

  const raw = String(value ?? '').trim();
  if (!raw) {
    return null;
  }

  const negative = raw.startsWith('-');
  const normalized = raw.replace(/\./g, '').replace(',', '.').replace(/[^\d.]/g, '');
  if (!normalized) {
    return null;
  }

  const parsed = Number(`${negative ? '-' : ''}${normalized}`);
  return Number.isFinite(parsed) ? parsed : null;
}

const positiveMoneyValidator: ValidatorFn = (control: AbstractControl): ValidationErrors | null => {
  const raw = String(control.value ?? '').trim();
  if (!raw) {
    return null;
  }

  const value = parseMonetaryValue(raw);
  if (value === null) {
    return { invalidAmount: true };
  }
  if (value <= 0) {
    return { nonPositiveAmount: true };
  }
  if (value > MAX_TRANSACTION_AMOUNT) {
    return { amountOverflow: true };
  }

  const decimalPart = raw.replace(/\./g, '').split(',')[1] ?? '';
  return decimalPart.length > 2 ? { amountScale: true } : null;
};

const zeroMoneyValidator: ValidatorFn = (control: AbstractControl): ValidationErrors | null => {
  const value = parseMonetaryValue(control.value);
  return value === 0 ? null : { prenoteAmount: true };
};

@Component({
  selector: 'app-transaction-create',
  standalone: true,
  imports: [
    SharedModule,
    MatButtonModule,
    MatCheckboxModule,
    MatFormFieldModule,
    MatInputModule,
    MatProgressSpinnerModule,
    MatSelectModule
  ],
  templateUrl: './transaction-create.component.html',
  styleUrls: ['./transaction-create.component.scss'],
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class TransactionCreateComponent implements OnInit, OnDestroy {
  @ViewChild('validationSummary') private validationSummary?: ElementRef<HTMLElement>;
  private readonly fb = inject(FormBuilder);
  private readonly api = inject(TransactionsApiService);
  private readonly cdr = inject(ChangeDetectorRef);
  private readonly router = inject(Router);
  private readonly notifications = inject(NotificationService);
  private readonly financialInstitutionsApi = inject(FinancialInstitutionsApiService);
  private readonly customersApi = inject(CustomersApiService);
  private readonly destroy$ = new Subject<void>();
  validationAttempted = false;

  private readonly fieldMetadata: Record<string, { label: string; section: TransactionSection }> = {
    amount: { label: 'Valor de la transacción', section: 'operation' },
    transactionExternalId: { label: 'ID de operación del cliente', section: 'operation' },
    type: { label: 'Tipo de operación', section: 'operation' },
    accountType: { label: 'Tipo de cuenta destino', section: 'recipient' },
    sourceAccountNumber: { label: 'Número de cuenta de origen', section: 'originator' },
    companyName: { label: 'Nombre o razón social del originador', section: 'originator' },
    companyIdentification: { label: 'Identificación del originador', section: 'originator' },
    sourcePersonType: { label: 'Tipo de persona del originador', section: 'originator' },
    destinationInstitutionId: { label: 'Entidad financiera destino', section: 'recipient' },
    destinationAccountNumber: { label: 'Número de cuenta destino', section: 'recipient' },
    recipientIdNumber: { label: 'Identificación del receptor', section: 'recipient' },
    recipientName: { label: 'Nombre o razón social del receptor', section: 'recipient' },
    recipientPersonType: { label: 'Tipo de identificación del receptor', section: 'recipient' },
    companyEntryDescriptionId: { label: 'Descripción de la entrada', section: 'concept' }
  };

  private readonly sectionDefinitions: Array<{ key: TransactionSection; label: string }> = [
    { key: 'operation', label: 'Operación' },
    { key: 'originator', label: 'Origen CFA' },
    { key: 'recipient', label: 'Entidad y receptor' },
    { key: 'concept', label: 'Recaudo o addenda' },
    { key: 'review', label: 'Revisión' }
  ];

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
  readonly customerOptions$ = this.customers$.pipe(
    map((customers) =>
      (customers ?? []).map((customer) => ({
        valor: customer.id,
        etiqueta: `${customer.fullName} · ${customer.documentType} ${customer.documentNumber} · ${(customer.accountNumbers?.length ?? 0)} cuenta(s)`
      }))
    ),
    shareReplay({ bufferSize: 1, refCount: true })
  );
  readonly institutionOptions$ = this.institutions$.pipe(
    map((institutions) => (institutions ?? []).map((institution) => ({ valor: institution.id, etiqueta: institution.name }))),
    shareReplay({ bufferSize: 1, refCount: true })
  );
  readonly form: FormGroup = this.fb.group({
    customerId: [null],
    amount: ['', [Validators.required, positiveMoneyValidator]],
    transactionExternalId: ['', [Validators.required, Validators.maxLength(64)]],
    type: [TransactionTypeEnum.Credit, Validators.required],
    accountType: [AccountTypeEnum.Checking, Validators.required],
    isPrenotification: [false],
    destinationInstitutionId: [null, [Validators.required, Validators.min(1)]],
    sourceAccountNumber: ['', [Validators.required, Validators.pattern(/^[0-9]{6,18}$/)]],
    destinationAccountNumber: ['', [Validators.required, Validators.pattern(/^[0-9]{6,18}$/)]],
    recipientIdNumber: ['', [Validators.maxLength(20)]],
    recipientName: ['', [Validators.maxLength(100)]],
    requiresIdentityValidation: [false],
    companyName: ['', [Validators.required, Validators.maxLength(16)]],
    companyIdentification: ['', [Validators.required, Validators.pattern(/^[A-Z0-9]{4,10}$/)]],
    sourcePersonType: ['PJ', [Validators.required]],
    recipientPersonType: ['PN', [Validators.required]],
    companyEntryDescriptionId: [null, [Validators.required, Validators.min(1)]],
    addendas: this.fb.array([])
  });

  readonly isSubmitting = new FormControl(false, { nonNullable: true });
  readonly errorMessage = new FormControl<string | null>(null);
  readonly successMessage = new FormControl<string | null>(null);
  readonly createdResponse = new FormControl<TransactionResponse | null>(null);

  activeDestinationAccounts: ActiveThirdPartyAccount[] = [];
  filteredDestinationAccounts: ActiveThirdPartyAccount[] = [];
  selectedCustomerAccounts: string[] = [];
  companyEntryDescriptionOptions: CompanyEntryDescriptionOption[] = [];
  policyPreview: TransactionPolicyPreview | null = null;
  catalogsLoading = true;

  get selectedCustomerAccountOptions() {
    return this.selectedCustomerAccounts.map((accountNumber) => ({ valor: accountNumber, etiqueta: accountNumber }));
  }

  get filteredDestinationAccountOptions() {
    return this.filteredDestinationAccounts.map((account) => ({
      valor: account.destinationAccountNumber,
      etiqueta: `${account.destinationAccountNumber} · ${account.recipientIdNumber} · ${account.destinationInstitutionName}`
    }));
  }

  get companyEntryDescriptionOptionsForSelect() {
    return this.companyEntryDescriptionOptions.map((option) => ({
      valor: option.id,
      etiqueta: `${option.description} (${option.term})`
    }));
  }

  get validationIssues(): TransactionValidationIssue[] {
    if (!this.validationAttempted && !this.form.touched) {
      return [];
    }
    return this.collectValidationIssues();
  }

  get validationSections(): Array<{ key: TransactionSection; label: string; issues: TransactionValidationIssue[] }> {
    const issues = this.validationIssues;
    return this.sectionDefinitions
      .map((section) => ({ ...section, issues: issues.filter((issue) => issue.section === section.key) }))
      .filter((section) => section.issues.length > 0);
  }

  get sectionStates(): TransactionSectionState[] {
    const allIssues = this.collectValidationIssues();
    return this.sectionDefinitions.map((section) => {
      const issues = allIssues.filter((issue) => issue.section === section.key);
      const visibleErrors = issues.filter((issue) => this.validationAttempted || this.form.get(issue.path)?.touched);
      if (issues.length === 0) {
        return { ...section, symbol: '✓', status: 'complete', errorCount: 0 };
      }
      if (visibleErrors.length > 0 || this.validationAttempted) {
        return { ...section, symbol: '!', status: 'attention', errorCount: issues.length };
      }
      return { ...section, symbol: '○', status: 'pending', errorCount: 0 };
    });
  }

  get incompleteFieldCount(): number {
    return this.collectValidationIssues().length;
  }

  private readonly amountFormatter = new Intl.NumberFormat('es-CO', {
    minimumFractionDigits: 0,
    maximumFractionDigits: 0
  });

  ngOnInit(): void {
    this.form.setValidators([this.validateAccountDifference, this.validateBusinessRules, recipientIdentityValidator()]);
    this.ensureDefaultAddenda();

    this.api.getCompanyEntryDescriptions()
      .pipe(
        take(1),
        takeUntil(this.destroy$),
        finalize(() => {
          this.catalogsLoading = false;
          this.cdr.markForCheck();
        })
      )
      .subscribe({
        next: (items) => {
          this.companyEntryDescriptionOptions = (items ?? []).sort((a, b) => a.term.localeCompare(b.term));
          const defaultItem = this.companyEntryDescriptionOptions.find((x) => x.term === 'NOMINAS')
            ?? this.companyEntryDescriptionOptions[0];
          if (defaultItem) {
            this.form.patchValue({ companyEntryDescriptionId: defaultItem.id }, { emitEvent: false });
          }
        },
        error: () => {
          this.errorMessage.setValue('No fue posible cargar el catálogo de conceptos de entrada.');
        }
      });
    this.form.valueChanges.pipe(takeUntil(this.destroy$)).subscribe(() => {
      this.errorMessage.setValue(null);
      this.successMessage.setValue(null);
    });

    this.form.valueChanges.pipe(debounceTime(250), takeUntil(this.destroy$)).subscribe(() => this.loadPolicyPreview());


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
            companyIdentification: selected.documentNumber,
            sourcePersonType: selected.personType === 'PN' ? 'PN' : 'PJ'
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
      .get('destinationAccountNumber')
      ?.valueChanges.pipe(takeUntil(this.destroy$))
      .subscribe((value) => this.onDestinationAccountSelected(String(value ?? '')));

    this.form
      .get('isPrenotification')
      ?.valueChanges.pipe(takeUntil(this.destroy$))
      .subscribe((isPrenotification) => {
        const amountControl = this.form.get('amount');
        if (!amountControl) {
          return;
        }

        const validators = isPrenotification
          ? [Validators.required, zeroMoneyValidator]
          : [Validators.required, positiveMoneyValidator];

        amountControl.setValidators(validators);
        if (isPrenotification) {
          amountControl.setValue(0, { emitEvent: false });
        }

        this.loadActiveDestinationAccounts();
        amountControl.updateValueAndValidity({ emitEvent: false });
      });

    this.form
      .get('type')
      ?.valueChanges.pipe(takeUntil(this.destroy$))
      .subscribe(() => this.updateConditionalValidators());

    this.form
      .get('requiresIdentityValidation')
      ?.valueChanges.pipe(takeUntil(this.destroy$))
      .subscribe(() => this.updateConditionalValidators());

    this.updateConditionalValidators();
  }

  ngOnDestroy(): void {
    this.destroy$.next();
    this.destroy$.complete();
  }

  get addendas(): FormArray<FormGroup> {
    return this.form.get('addendas') as FormArray<FormGroup>;
  }

  addAddenda(): void {
    const group = this.fb.group({
        addendaType: ['05', [Validators.required]],
        collectorId: ['', [Validators.maxLength(13)]],
        receiverCustomerCode: ['', [Validators.maxLength(30)]],
        serviceDescription: ['', [Validators.maxLength(15)]],
        information: ['', [Validators.required, Validators.maxLength(80)]]
      });
    this.addendas.push(group);
    this.updateAddendaValidators(group);
  }

  removeAddenda(index: number): void {
    this.addendas.removeAt(index);
  }

  submit(): void {
    if (this.isSubmitting.value) {
      return;
    }
    if (this.form.invalid) {
      this.validationAttempted = true;
      this.form.markAllAsTouched();
      this.cdr.markForCheck();
      setTimeout(() => this.focusFirstInvalidControl());
      return;
    }

    const payload = this.form.getRawValue() as TransactionDraft;
    const parsedAmount = this.parseMaskedAmount(payload.amount);
    if (parsedAmount === null || (!Boolean(payload.isPrenotification) && parsedAmount <= 0)) {
      this.form.get('amount')?.setErrors({ invalidAmount: true });
      this.form.get('amount')?.markAsTouched();
      this.validationAttempted = true;
      this.cdr.markForCheck();
      setTimeout(() => this.focusFirstInvalidControl());
      return;
    }

    const addendas = payload.addendas
      .map((item) => this.buildAddendaPayload(item))
      .filter((item) => item.addendaType && item.information);
    const sanitized: TransactionDraft = {
      type: Number(payload.type) as TransactionTypeEnum,
      accountType: Number(payload.accountType) as AccountTypeEnum,
      isPrenotification: Boolean(payload.isPrenotification),
      amount: parsedAmount,
      destinationInstitutionId: Number(payload.destinationInstitutionId),
      transactionExternalId: payload.transactionExternalId.trim(),
      sourceAccountNumber: this.extractDigits(payload.sourceAccountNumber).slice(0, 18),
      destinationAccountNumber: this.extractDigits(payload.destinationAccountNumber).slice(0, 18),
      recipientIdNumber: payload.recipientIdNumber?.trim() || undefined,
      recipientName: payload.recipientName?.trim() || undefined,
      requiresIdentityValidation: Boolean(payload.requiresIdentityValidation),
      companyName: payload.companyName.trim(),
      companyIdentification: payload.companyIdentification.trim().toUpperCase(),
      sourcePersonType: payload.sourcePersonType === 'PN' ? 'PN' : 'PJ',
      recipientPersonType: payload.recipientPersonType === 'PJ' ? 'PJ' : 'PN',
      companyEntryDescriptionId: Number(payload.companyEntryDescriptionId),
      addendas
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
              transactionExternalId: '',
              type: TransactionTypeEnum.Credit,
              accountType: AccountTypeEnum.Checking,
              isPrenotification: false,
              sourcePersonType: 'PJ',
              recipientPersonType: 'PN',
              companyEntryDescriptionId: this.companyEntryDescriptionOptions.find((x) => x.term === 'NOMINAS')?.id ?? null
            });
            this.addendas.clear();
            this.ensureDefaultAddenda();
            this.activeDestinationAccounts = [];
            this.filteredDestinationAccounts = [];
            this.selectedCustomerAccounts = [];
            this.policyPreview = null;
            this.validationAttempted = false;
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
        }),
        catchError(() => EMPTY)
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

  focusIssue(issue: TransactionValidationIssue): void {
    const container = document.querySelector<HTMLElement>(`[data-validation-path="${issue.path}"]`);
    if (!container) {
      return;
    }
    container.scrollIntoView({ behavior: 'smooth', block: 'center' });
    const focusTarget = container.matches('input, select, textarea, button, [tabindex]')
      ? container
      : container.querySelector<HTMLElement>('input, select, textarea, button, [tabindex]');
    (focusTarget ?? container).focus();
  }

  focusFirstInvalidControl(): string | null {
    const issue = this.collectValidationIssues()[0] ?? null;
    if (!issue) {
      this.validationSummary?.nativeElement.focus();
      return null;
    }

    this.focusIssue(issue);
    return issue.path;
  }

  focusSection(section: TransactionSection): void {
    const issue = this.collectValidationIssues().find((item) => item.section === section);
    if (issue) {
      this.validationAttempted = true;
      this.focusIssue(issue);
      return;
    }
    document.querySelector<HTMLElement>(`[data-transaction-section="${section}"]`)?.focus();
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



  onNumericAccountInput(controlName: 'sourceAccountNumber' | 'destinationAccountNumber', event: Event): void {
    const input = event.target as HTMLInputElement | null;
    if (!input) {
      return;
    }

    const digitsOnly = this.extractDigits(input.value).slice(0, 18);
    if (input.value !== digitsOnly) {
      input.value = digitsOnly;
    }

    const control = this.form.get(controlName);
    if (control && control.value !== digitsOnly) {
      control.setValue(digitsOnly);
      control.markAsDirty();
      control.markAsTouched();
    }
  }

  onDestinationAccountSelected(accountNumber: string): void {
    const selected = this.filteredDestinationAccounts.find((item) => item.destinationAccountNumber === accountNumber);
    if (!selected) {
      return;
    }

    this.form.patchValue({ recipientIdNumber: selected.recipientIdNumber, recipientName: '' }, { emitEvent: false });
  }

  private extractDigits(value: unknown): string {
    return String(value ?? '').replace(/\D+/g, '');
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


  private loadPolicyPreview(): void {
    const raw = this.form.getRawValue() as TransactionDraft;
    const parsedAmount = this.parseMaskedAmount(raw.amount);

    if (!raw.destinationInstitutionId
      || !raw.sourceAccountNumber
      || !raw.destinationAccountNumber
      || !String(raw.transactionExternalId ?? '').trim()
      || parsedAmount === null) {
      this.policyPreview = null;
      this.form.updateValueAndValidity({ emitEvent: false });
      this.cdr.markForCheck();
      return;
    }

    this.api.previewPolicy({
      ...raw,
      amount: parsedAmount,
      sourceAccountNumber: this.extractDigits(raw.sourceAccountNumber).slice(0, 18),
      destinationAccountNumber: this.extractDigits(raw.destinationAccountNumber).slice(0, 18)
    } as TransactionDraft)
      .pipe(take(1), takeUntil(this.destroy$))
      .subscribe({
        next: (preview) => {
          this.policyPreview = preview;
          this.form.updateValueAndValidity({ emitEvent: false });
          this.cdr.markForCheck();
        },
        error: () => {
          this.policyPreview = null;
          this.cdr.markForCheck();
        }
      });
  }

  private validateBusinessRules = (group: FormGroup) => {
    const isPrenotification = Boolean(group.get('isPrenotification')?.value);
    const amount = this.parseMaskedAmount(group.get('amount')?.value) ?? 0;
    const type = Number(group.get('type')?.value) as TransactionTypeEnum;
    const recipientId = group.get('recipientIdNumber')?.value;
    const recipientName = group.get('recipientName')?.value;
    const requiresIdentityValidation = Boolean(group.get('requiresIdentityValidation')?.value);
    const addendas = group.get('addendas') as FormArray<FormGroup>;

    const errors: Record<string, boolean> = {};

    if (isPrenotification && amount !== 0) {
      errors.prenoteAmount = true;
    }

    if ((type === TransactionTypeEnum.Debit || type === TransactionTypeEnum.Reversal) && !recipientId) {
      errors.missingRecipientId = true;
    }

    if (type === TransactionTypeEnum.Credit && requiresIdentityValidation && !recipientId) {
      errors.missingRecipientId = true;
    }

    if (recipientId && !String(recipientName ?? '').trim()) {
      errors.missingRecipientName = true;
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

  private updateConditionalValidators(): void {
    const type = Number(this.form.get('type')?.value) as TransactionTypeEnum;
    const identityRequired = type === TransactionTypeEnum.Debit
      || type === TransactionTypeEnum.Reversal
      || (type === TransactionTypeEnum.Credit && Boolean(this.form.get('requiresIdentityValidation')?.value));
    const recipientId = this.form.get('recipientIdNumber');
    recipientId?.setValidators([
      ...(identityRequired ? [Validators.required] : []),
      Validators.maxLength(20)
    ]);
    recipientId?.updateValueAndValidity({ emitEvent: false });

    if (type !== TransactionTypeEnum.Credit) {
      this.form.get('requiresIdentityValidation')?.setValue(false, { emitEvent: false });
    }

    this.addendas.controls.forEach((group) => this.updateAddendaValidators(group));
    this.form.updateValueAndValidity({ emitEvent: false });
    this.cdr.markForCheck();
  }

  private updateAddendaValidators(group: FormGroup): void {
    const type = Number(this.form.get('type')?.value) as TransactionTypeEnum;
    const debitFieldsRequired = type === TransactionTypeEnum.Debit
      || type === TransactionTypeEnum.Reversal;
    const validators: Record<string, ValidatorFn[]> = {
      collectorId: [
        ...(debitFieldsRequired ? [Validators.required] : []),
        Validators.pattern(/^\d{1,13}$/),
        Validators.maxLength(13)
      ],
      receiverCustomerCode: [
        ...(debitFieldsRequired ? [Validators.required] : []),
        Validators.maxLength(30)
      ],
      serviceDescription: [
        ...(debitFieldsRequired ? [Validators.required] : []),
        Validators.maxLength(15)
      ]
    };

    Object.entries(validators).forEach(([controlName, controlValidators]) => {
      const control = group.get(controlName);
      control?.setValidators(controlValidators);
      control?.updateValueAndValidity({ emitEvent: false });
    });
  }

  private collectValidationIssues(): TransactionValidationIssue[] {
    const issues: TransactionValidationIssue[] = [];
    Object.entries(this.fieldMetadata).forEach(([path, metadata]) => {
      const control = this.form.get(path);
      if (control?.invalid) {
        issues.push({ path, ...metadata, message: this.validationMessage(control) });
      }
    });

    this.addendas.controls.forEach((addenda, index) => {
      const addendaFields: Array<[string, string]> = [
        ['addendaType', 'Tipo de addenda'],
        ['collectorId', 'Identificación del recaudador'],
        ['receiverCustomerCode', 'Código del cliente receptor'],
        ['serviceDescription', 'Descripción del servicio'],
        ['information', 'Información adicional']
      ];
      addendaFields.forEach(([controlName, label]) => {
        const control = addenda.get(controlName);
        if (control?.invalid) {
          issues.push({
            path: `addendas.${index}.${controlName}`,
            label: `${label} · Addenda ${index + 1}`,
            message: this.validationMessage(control),
            section: 'concept'
          });
        }
      });
    });

    const crossFieldIssues: Record<string, TransactionValidationIssue> = {
      sameAccount: { path: 'destinationAccountNumber', label: 'Cuenta destino', message: 'La cuenta origen y la cuenta destino deben ser diferentes.', section: 'recipient' },
      missingAddenda: { path: 'addendas', label: 'Información de la addenda', message: 'Debe registrar al menos una addenda completa.', section: 'concept' },
      invalidAddenda: { path: 'addendas.0.addendaType', label: 'Tipo de addenda', message: 'La descripción de entrada solo admite una addenda tipo 05.', section: 'concept' },
      recipientIdentityFormat: { path: 'recipientIdNumber', label: 'Identificación del receptor', message: 'La identificación es incompatible con el tipo de persona receptor.', section: 'recipient' },
      missingRecipientId: { path: 'recipientIdNumber', label: 'Identificación del receptor', message: 'Debe diligenciar la identificación requerida para esta operación.', section: 'recipient' },
      missingRecipientName: { path: 'recipientName', label: 'Nombre del receptor', message: 'Debe diligenciar el nombre asociado a la identificación del receptor.', section: 'recipient' },
      prenoteAmount: { path: 'amount', label: 'Monto', message: 'La prenotificación debe registrarse con monto cero.', section: 'operation' }
    };
    Object.keys(this.form.errors ?? {}).forEach((key) => {
      const issue = crossFieldIssues[key];
      if (issue && !issues.some((item) => item.path === issue.path && item.message === issue.message)) {
        issues.push(issue);
      }
    });

    if (this.policyPreview && !this.policyPreview.canSubmit) {
      issues.push({
        path: 'destinationAccountNumber',
        label: 'Regla operativa',
        message: this.policyPreview.message || 'La transacción no cumple la regla operativa vigente.',
        section: 'review'
      });
    }

    return issues;
  }

  private validationMessage(control: AbstractControl): string {
    const errors = control.errors ?? {};
    if (errors['required']) return 'Campo obligatorio.';
    if (errors['pattern']) return 'Formato inválido.';
    if (errors['maxlength']) return `Longitud excedida. Máximo ${errors['maxlength'].requiredLength} caracteres.`;
    if (errors['nonPositiveAmount']) return 'El valor debe ser mayor que cero.';
    if (errors['amountScale']) return 'Use máximo dos decimales.';
    if (errors['amountOverflow']) return 'El valor supera el máximo permitido.';
    if (errors['min'] || errors['max']) return 'Valor fuera del rango permitido.';
    if (errors['invalidAmount']) return 'Ingrese un monto válido.';
    return 'Revise el valor ingresado.';
  }

  private buildAddendaPayload(item: TransactionDraft['addendas'][number]) {
    return {
      addendaType: item.addendaType?.trim().toUpperCase(),
      collectorId: item.collectorId?.trim() || undefined,
      receiverCustomerCode: item.receiverCustomerCode?.trim() || undefined,
      serviceDescription: item.serviceDescription?.trim() || undefined,
      information: item.information.trim()
    };
  }

  private ensureDefaultAddenda(): void {
    if (this.addendas.length > 0) {
      return;
    }

    this.addAddenda();
  }

  private formatMaskedAmount(rawValue: string): string {
    const raw = String(rawValue ?? '');
    if (!raw.trim()) {
      return '';
    }

    const negative = raw.trim().startsWith('-');
    const normalized = raw
      .replace(/\./g, '')
      .replace(/[^\d,]/g, '');

    if (!normalized) {
      return '';
    }

    const separatorIndex = normalized.indexOf(',');
    if (separatorIndex === -1) {
      const digits = normalized.replace(/\D/g, '');
      return digits ? `${negative ? '-' : ''}${this.amountFormatter.format(Number(digits))}` : '';
    }

    const integerDigits = normalized.slice(0, separatorIndex).replace(/\D/g, '');
    const decimalDigits = normalized.slice(separatorIndex + 1).replace(/\D/g, '').slice(0, 2);
    const integerFormatted = integerDigits ? this.amountFormatter.format(Number(integerDigits)) : '0';
    const hasTrailingSeparator = separatorIndex === normalized.length - 1;

    if (decimalDigits) {
      return `${negative ? '-' : ''}${integerFormatted},${decimalDigits}`;
    }

    return `${negative ? '-' : ''}${hasTrailingSeparator ? `${integerFormatted},` : integerFormatted}`;
  }

  private parseMaskedAmount(value: unknown): number | null {
    return parseMonetaryValue(value);
  }
}

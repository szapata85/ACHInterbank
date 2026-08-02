import { ChangeDetectionStrategy, ChangeDetectorRef, Component, ElementRef, OnDestroy, OnInit, ViewChild, inject } from '@angular/core';
import { AbstractControl, FormArray, FormBuilder, FormControl, FormGroup, ValidationErrors, ValidatorFn, Validators } from '@angular/forms';
import { MatAutocompleteModule } from '@angular/material/autocomplete';
import { MatButtonModule } from '@angular/material/button';
import { MatCheckboxModule } from '@angular/material/checkbox';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatIconModule } from '@angular/material/icon';
import { MatInputModule } from '@angular/material/input';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatSelectModule } from '@angular/material/select';
import { EMPTY, catchError, debounceTime, distinctUntilChanged, finalize, map, shareReplay, take, takeUntil, tap } from 'rxjs';
import { Subject } from 'rxjs';
import { Router } from '@angular/router';
import { TransactionsApiService } from '../../services/transactions-api.service';
import { ActiveThirdPartyAccount, CompanyEntryDescriptionOption, DestinationInstitution, TransactionDraft, TransactionPolicyPreview, TransactionResponse } from '../../transactions.models';
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

interface CatalogSearchOption<TValue, TSource> {
  value: TValue;
  label: string;
  normalizedSearch: string;
  source: TSource;
}

type CustomerSearchOption = CatalogSearchOption<number | null, CustomerSummary | null> & {
  manual: boolean;
};
type SourceAccountSearchOption = CatalogSearchOption<string, string>;
type CompanyEntryDescriptionSearchOption = CatalogSearchOption<number, CompanyEntryDescriptionOption>;
type DestinationInstitutionSearchOption = CatalogSearchOption<number, DestinationInstitution>;
type DestinationAccountSearchOption = CatalogSearchOption<string, ActiveThirdPartyAccount>;

type CustomerSearchValue = string | CustomerSearchOption | null;
type SourceAccountSearchValue = string | SourceAccountSearchOption | null;
type CompanyEntryDescriptionSearchValue = string | CompanyEntryDescriptionSearchOption | null;
type DestinationInstitutionSearchValue = string | DestinationInstitutionSearchOption | null;
type DestinationAccountSearchValue = string | DestinationAccountSearchOption | null;

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

function normalizeCatalogSearch(value: string): string {
  return value
    .normalize('NFD')
    .replace(/[\u0300-\u036f]/g, '')
    .replace(/\s+/g, ' ')
    .trim()
    .toLocaleLowerCase();
}

@Component({
  selector: 'app-transaction-create',
  standalone: true,
  imports: [
    SharedModule,
    MatAutocompleteModule,
    MatButtonModule,
    MatCheckboxModule,
    MatFormFieldModule,
    MatIconModule,
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
    customerId: { label: 'Cliente originador', section: 'originator' },
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
    ),
    shareReplay({ bufferSize: 1, refCount: true })
  );
  readonly form: FormGroup = this.fb.group({
    customerId: [null],
    amount: ['', [Validators.required, positiveMoneyValidator]],
    transactionExternalId: ['', [Validators.required, Validators.maxLength(64)]],
    type: [TransactionTypeEnum.Debit, Validators.required],
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

  customers: CustomerSummary[] = [];
  institutions: DestinationInstitution[] = [];
  activeDestinationAccounts: ActiveThirdPartyAccount[] = [];
  filteredDestinationAccounts: ActiveThirdPartyAccount[] = [];
  selectedCustomerAccounts: string[] = [];
  companyEntryDescriptionOptions: CompanyEntryDescriptionOption[] = [];

  customerOptions: CustomerSearchOption[] = [];
  filteredCustomerOptions: CustomerSearchOption[] = [];
  sourceAccountOptions: SourceAccountSearchOption[] = [];
  filteredSourceAccountOptions: SourceAccountSearchOption[] = [];
  companyEntryDescriptionSearchOptions: CompanyEntryDescriptionSearchOption[] = [];
  filteredCompanyEntryDescriptionOptions: CompanyEntryDescriptionSearchOption[] = [];
  destinationInstitutionOptions: DestinationInstitutionSearchOption[] = [];
  filteredDestinationInstitutionOptions: DestinationInstitutionSearchOption[] = [];
  destinationAccountOptions: DestinationAccountSearchOption[] = [];
  filteredDestinationAccountSearchOptions: DestinationAccountSearchOption[] = [];

  readonly customerSearchControl = new FormControl<CustomerSearchValue>(
    '',
    this.realSelectionValidator('customerId', true)
  );
  readonly sourceAccountSearchControl = new FormControl<SourceAccountSearchValue>(
    '',
    this.realSelectionValidator('sourceAccountNumber')
  );
  readonly companyEntryDescriptionSearchControl = new FormControl<CompanyEntryDescriptionSearchValue>(
    '',
    this.realSelectionValidator('companyEntryDescriptionId')
  );
  readonly destinationInstitutionSearchControl = new FormControl<DestinationInstitutionSearchValue>(
    '',
    this.realSelectionValidator('destinationInstitutionId')
  );
  readonly destinationAccountSearchControl = new FormControl<DestinationAccountSearchValue>(
    '',
    this.realSelectionValidator('destinationAccountNumber')
  );

  readonly displayCustomerOption = (value: CustomerSearchValue): string => this.displayCatalogOption(value);
  readonly displaySourceAccountOption = (value: SourceAccountSearchValue): string => this.displayCatalogOption(value);
  readonly displayCompanyEntryDescriptionOption = (value: CompanyEntryDescriptionSearchValue): string =>
    this.displayCatalogOption(value);
  readonly displayDestinationInstitutionOption = (value: DestinationInstitutionSearchValue): string =>
    this.displayCatalogOption(value);
  readonly displayDestinationAccountOption = (value: DestinationAccountSearchValue): string =>
    this.displayCatalogOption(value);

  policyPreview: TransactionPolicyPreview | null = null;
  catalogsLoading = true;

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
    this.initializeAutocompleteSubscriptions();

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
          this.companyEntryDescriptionSearchOptions = this.companyEntryDescriptionOptions.map((option) =>
            this.buildCompanyEntryDescriptionSearchOption(option)
          );
          this.filteredCompanyEntryDescriptionOptions = [...this.companyEntryDescriptionSearchOptions];
          this.restoreDefaultCompanyEntryDescription();
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
      .subscribe((customerId) => this.onCustomerIdChanged(customerId));

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

        this.resetDestinationAccountSelection();
        this.loadActiveDestinationAccounts();
        amountControl.updateValueAndValidity({ emitEvent: false });
        this.destinationAccountSearchControl.updateValueAndValidity({ emitEvent: false });
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

  filterCustomerOptions(value: CustomerSearchValue): void {
    const term = this.searchText(value);
    this.filteredCustomerOptions = this.customerOptions.filter((option) =>
      this.matchesCatalogSearch(option.normalizedSearch, term)
    );

    if (typeof value === 'string') {
      if (this.form.get('customerId')?.value !== null) {
        this.form.get('customerId')?.setValue(null);
      }
      this.updateCustomerSelectionError(Boolean(term));
    }

    this.customerSearchControl.updateValueAndValidity({ emitEvent: false });
    this.cdr.markForCheck();
  }

  filterSourceAccountOptions(value: SourceAccountSearchValue): void {
    const term = this.searchText(value);
    this.filteredSourceAccountOptions = this.sourceAccountOptions
      .filter((option) => this.matchesCatalogSearch(option.normalizedSearch, term));

    if (typeof value === 'string' && this.form.get('sourceAccountNumber')?.value) {
      this.form.get('sourceAccountNumber')?.setValue('');
    }

    this.sourceAccountSearchControl.updateValueAndValidity({ emitEvent: false });
    this.cdr.markForCheck();
  }

  filterCompanyEntryDescriptionOptions(value: CompanyEntryDescriptionSearchValue): void {
    const term = this.searchText(value);
    this.filteredCompanyEntryDescriptionOptions = this.companyEntryDescriptionSearchOptions
      .filter((option) => this.matchesCatalogSearch(option.normalizedSearch, term));

    if (typeof value === 'string' && this.form.get('companyEntryDescriptionId')?.value !== null) {
      this.form.get('companyEntryDescriptionId')?.setValue(null);
    }

    this.companyEntryDescriptionSearchControl.updateValueAndValidity({ emitEvent: false });
    this.cdr.markForCheck();
  }

  filterDestinationInstitutionOptions(value: DestinationInstitutionSearchValue): void {
    const term = this.searchText(value);
    this.filteredDestinationInstitutionOptions = this.destinationInstitutionOptions
      .filter((option) => this.matchesCatalogSearch(option.normalizedSearch, term));

    if (typeof value === 'string' && this.form.get('destinationInstitutionId')?.value !== null) {
      this.form.get('destinationInstitutionId')?.setValue(null);
    }

    this.destinationInstitutionSearchControl.updateValueAndValidity({ emitEvent: false });
    this.cdr.markForCheck();
  }

  filterDestinationAccountOptions(value: DestinationAccountSearchValue): void {
    const term = this.searchText(value);
    this.filteredDestinationAccountSearchOptions = this.destinationAccountOptions
      .filter((option) => this.matchesCatalogSearch(option.normalizedSearch, term));

    if (typeof value === 'string' && this.form.get('destinationAccountNumber')?.value) {
      this.form.get('destinationAccountNumber')?.setValue('');
    }

    this.destinationAccountSearchControl.updateValueAndValidity({ emitEvent: false });
    this.cdr.markForCheck();
  }

  selectCustomer(option: CustomerSearchOption): void {
    this.customerSearchControl.setValue(option, { emitEvent: false });
    this.filteredCustomerOptions = [...this.customerOptions];
    this.updateCustomerSelectionError(false);
    this.form.get('customerId')?.setValue(option.value);
    this.customerSearchControl.updateValueAndValidity({ emitEvent: false });
  }

  selectSourceAccount(option: SourceAccountSearchOption): void {
    this.sourceAccountSearchControl.setValue(option, { emitEvent: false });
    this.filteredSourceAccountOptions = [...this.sourceAccountOptions];
    this.form.get('sourceAccountNumber')?.setValue(option.value);
    this.sourceAccountSearchControl.updateValueAndValidity({ emitEvent: false });
    this.cdr.markForCheck();
  }

  selectCompanyEntryDescription(option: CompanyEntryDescriptionSearchOption): void {
    this.companyEntryDescriptionSearchControl.setValue(option, { emitEvent: false });
    this.filteredCompanyEntryDescriptionOptions = [...this.companyEntryDescriptionSearchOptions];
    this.form.get('companyEntryDescriptionId')?.setValue(option.value);
    this.companyEntryDescriptionSearchControl.updateValueAndValidity({ emitEvent: false });
    this.cdr.markForCheck();
  }

  selectDestinationInstitution(option: DestinationInstitutionSearchOption): void {
    this.destinationInstitutionSearchControl.setValue(option, { emitEvent: false });
    this.filteredDestinationInstitutionOptions = [...this.destinationInstitutionOptions];
    this.form.get('destinationInstitutionId')?.setValue(option.value);
    this.destinationInstitutionSearchControl.updateValueAndValidity({ emitEvent: false });
    this.cdr.markForCheck();
  }

  selectDestinationAccount(option: DestinationAccountSearchOption): void {
    this.destinationAccountSearchControl.setValue(option, { emitEvent: false });
    this.filteredDestinationAccountSearchOptions = [...this.destinationAccountOptions];
    this.form.get('destinationAccountNumber')?.setValue(option.value);
    this.destinationAccountSearchControl.updateValueAndValidity({ emitEvent: false });
    this.cdr.markForCheck();
  }

  clearCustomerSearch(): void {
    this.customerSearchControl.setValue('', { emitEvent: false });
    this.filteredCustomerOptions = [...this.customerOptions];
    this.updateCustomerSelectionError(false);
    this.form.get('customerId')?.setValue(null);
    this.customerSearchControl.updateValueAndValidity({ emitEvent: false });
  }

  clearSourceAccountSearch(): void {
    this.sourceAccountSearchControl.setValue('', { emitEvent: false });
    this.filteredSourceAccountOptions = [...this.sourceAccountOptions];
    this.form.get('sourceAccountNumber')?.setValue('');
    this.sourceAccountSearchControl.updateValueAndValidity({ emitEvent: false });
  }

  clearCompanyEntryDescriptionSearch(): void {
    this.companyEntryDescriptionSearchControl.setValue('', { emitEvent: false });
    this.filteredCompanyEntryDescriptionOptions = [...this.companyEntryDescriptionSearchOptions];
    this.form.get('companyEntryDescriptionId')?.setValue(null);
    this.companyEntryDescriptionSearchControl.updateValueAndValidity({ emitEvent: false });
  }

  clearDestinationInstitutionSearch(): void {
    this.destinationInstitutionSearchControl.setValue('', { emitEvent: false });
    this.filteredDestinationInstitutionOptions = [...this.destinationInstitutionOptions];
    this.form.get('destinationInstitutionId')?.setValue(null);
    this.destinationInstitutionSearchControl.updateValueAndValidity({ emitEvent: false });
  }

  clearDestinationAccountSearch(): void {
    this.destinationAccountSearchControl.setValue('', { emitEvent: false });
    this.filteredDestinationAccountSearchOptions = [...this.destinationAccountOptions];
    this.form.get('destinationAccountNumber')?.setValue('');
    this.destinationAccountSearchControl.updateValueAndValidity({ emitEvent: false });
  }

  shouldShowAutocompleteError(control: AbstractControl): boolean {
    return control.invalid && (control.touched || control.dirty || this.validationAttempted);
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
      this.markAutocompleteControlsAsTouched();
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
      this.markAutocompleteControlsAsTouched();
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
              type: TransactionTypeEnum.Debit,
              accountType: AccountTypeEnum.Checking,
              isPrenotification: false,
              sourcePersonType: 'PJ',
              recipientPersonType: 'PN',
              companyEntryDescriptionId: this.defaultCompanyEntryDescriptionOption()?.value ?? null
            });
            this.addendas.clear();
            this.ensureDefaultAddenda();
            this.activeDestinationAccounts = [];
            this.filteredDestinationAccounts = [];
            this.selectedCustomerAccounts = [];
            this.policyPreview = null;
            this.validationAttempted = false;
            this.resetAutocompleteStateAfterSubmit();
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

  private initializeAutocompleteSubscriptions(): void {
    this.customerSearchControl.valueChanges
      .pipe(takeUntil(this.destroy$))
      .subscribe((value) => this.filterCustomerOptions(value));
    this.sourceAccountSearchControl.valueChanges
      .pipe(takeUntil(this.destroy$))
      .subscribe((value) => this.filterSourceAccountOptions(value));
    this.companyEntryDescriptionSearchControl.valueChanges
      .pipe(takeUntil(this.destroy$))
      .subscribe((value) => this.filterCompanyEntryDescriptionOptions(value));
    this.destinationInstitutionSearchControl.valueChanges
      .pipe(takeUntil(this.destroy$))
      .subscribe((value) => this.filterDestinationInstitutionOptions(value));
    this.destinationAccountSearchControl.valueChanges
      .pipe(takeUntil(this.destroy$))
      .subscribe((value) => this.filterDestinationAccountOptions(value));

    this.customers$
      .pipe(takeUntil(this.destroy$))
      .subscribe((customers) => {
        this.customers = customers;
        this.customerOptions = [
          {
            value: null,
            label: 'Diligenciar manualmente',
            normalizedSearch: normalizeCatalogSearch('Diligenciar manualmente'),
            source: null,
            manual: true
          },
          ...customers.map((customer) => this.buildCustomerSearchOption(customer))
        ];
        this.filteredCustomerOptions = [...this.customerOptions];
        this.restoreCustomerSearchText();
        this.cdr.markForCheck();
      });

    this.institutions$
      .pipe(takeUntil(this.destroy$))
      .subscribe((institutions) => {
        this.institutions = institutions;
        this.destinationInstitutionOptions = institutions.map((institution) =>
          this.buildDestinationInstitutionSearchOption(institution)
        );
        this.filteredDestinationInstitutionOptions = [...this.destinationInstitutionOptions];
        this.restoreDestinationInstitutionSearchText();
        this.cdr.markForCheck();
      });
  }

  private onCustomerIdChanged(customerId: unknown): void {
    if (customerId == null) {
      this.selectedCustomerAccounts = [];
      this.rebuildSourceAccountOptions();
      this.sourceAccountSearchControl.setValue('', { emitEvent: false });
      this.sourceAccountSearchControl.updateValueAndValidity({ emitEvent: false });
      this.loadActiveDestinationAccounts();
      this.cdr.markForCheck();
      return;
    }

    const selected = this.customers.find((item) => item.id === Number(customerId));
    if (!selected) {
      return;
    }

    const accounts = this.getCustomerAccounts(selected);
    this.selectedCustomerAccounts = accounts;
    this.rebuildSourceAccountOptions();

    this.form.patchValue({
      sourceAccountNumber: accounts[0] ?? '',
      companyName: this.normalizeCompanyName(selected),
      companyIdentification: selected.documentNumber,
      sourcePersonType: selected.personType === 'PN' ? 'PN' : 'PJ'
    }, { emitEvent: false });

    const selectedSourceAccount = this.sourceAccountOptions.find((option) => option.value === accounts[0]);
    this.sourceAccountSearchControl.setValue(selectedSourceAccount ?? '', { emitEvent: false });
    this.sourceAccountSearchControl.updateValueAndValidity({ emitEvent: false });
    this.restoreCustomerSearchText();
    this.loadActiveDestinationAccounts();
    this.cdr.markForCheck();
  }

  private buildCustomerSearchOption(customer: CustomerSummary): CustomerSearchOption {
    const accounts = this.getCustomerAccounts(customer);
    const label = `${customer.fullName} · ${customer.documentType} ${customer.documentNumber} · ${accounts.length} cuenta(s)`;
    return {
      value: customer.id,
      label,
      normalizedSearch: normalizeCatalogSearch([
        customer.fullName,
        customer.companyName ?? '',
        customer.documentType,
        customer.documentNumber,
        ...accounts
      ].join(' ')),
      source: customer,
      manual: false
    };
  }

  private buildCompanyEntryDescriptionSearchOption(
    option: CompanyEntryDescriptionOption
  ): CompanyEntryDescriptionSearchOption {
    const label = `${option.description} (${option.term})`;
    return {
      value: option.id,
      label,
      normalizedSearch: normalizeCatalogSearch(
        `${option.description} ${option.term} ${option.standardEntryClassCode ?? ''}`
      ),
      source: option
    };
  }

  private buildDestinationInstitutionSearchOption(
    institution: DestinationInstitution
  ): DestinationInstitutionSearchOption {
    return {
      value: institution.id,
      label: institution.name,
      normalizedSearch: normalizeCatalogSearch([
        institution.name,
        institution.routingNumber,
        institution.transitCode,
        institution.checkDigit
      ].join(' ')),
      source: institution
    };
  }

  private buildDestinationAccountSearchOption(
    account: ActiveThirdPartyAccount
  ): DestinationAccountSearchOption {
    const label = `${account.destinationAccountNumber} · ${account.recipientIdNumber} · ${account.destinationInstitutionName}`;
    return {
      value: account.destinationAccountNumber,
      label,
      normalizedSearch: normalizeCatalogSearch(label),
      source: account
    };
  }

  private getCustomerAccounts(customer: CustomerSummary): string[] {
    return (customer.accountNumbers?.length ? customer.accountNumbers : [customer.accountNumber])
      .map((account) => String(account ?? '').trim())
      .filter((account) => Boolean(account));
  }

  private rebuildSourceAccountOptions(): void {
    this.sourceAccountOptions = this.selectedCustomerAccounts.map((accountNumber) => ({
      value: accountNumber,
      label: accountNumber,
      normalizedSearch: normalizeCatalogSearch(accountNumber),
      source: accountNumber
    }));
    this.filteredSourceAccountOptions = [...this.sourceAccountOptions];
  }

  private restoreCustomerSearchText(): void {
    const customerId = this.form.get('customerId')?.value;
    if (customerId == null) {
      this.customerSearchControl.updateValueAndValidity({ emitEvent: false });
      return;
    }

    const selectedOption = this.customerOptions.find((option) => option.value === Number(customerId));
    this.customerSearchControl.setValue(selectedOption ?? '', { emitEvent: false });
    this.customerSearchControl.updateValueAndValidity({ emitEvent: false });
  }

  private restoreDestinationInstitutionSearchText(): void {
    const institutionId = Number(this.form.get('destinationInstitutionId')?.value ?? 0);
    const selectedOption = this.destinationInstitutionOptions.find((option) => option.value === institutionId);
    if (selectedOption) {
      this.destinationInstitutionSearchControl.setValue(selectedOption, { emitEvent: false });
    }
    this.destinationInstitutionSearchControl.updateValueAndValidity({ emitEvent: false });
  }

  private restoreDefaultCompanyEntryDescription(): void {
    const defaultOption = this.defaultCompanyEntryDescriptionOption();
    this.form.patchValue(
      { companyEntryDescriptionId: defaultOption?.value ?? null },
      { emitEvent: false }
    );
    this.companyEntryDescriptionSearchControl.setValue(defaultOption ?? '', { emitEvent: false });
    this.companyEntryDescriptionSearchControl.updateValueAndValidity({ emitEvent: false });
    this.cdr.markForCheck();
  }

  private defaultCompanyEntryDescriptionOption(): CompanyEntryDescriptionSearchOption | null {
    return this.companyEntryDescriptionSearchOptions.find(
      (option) => option.source.term.trim().toUpperCase() === 'NOMINAS'
    ) ?? this.companyEntryDescriptionSearchOptions[0] ?? null;
  }

  private resetDestinationAccountSelection(): void {
    this.form.patchValue({ destinationAccountNumber: '' }, { emitEvent: false });
    this.destinationAccountSearchControl.setValue('', { emitEvent: false });
    this.filteredDestinationAccountSearchOptions = [...this.destinationAccountOptions];
    this.destinationAccountSearchControl.updateValueAndValidity({ emitEvent: false });
  }

  private resetAutocompleteStateAfterSubmit(): void {
    this.customerSearchControl.reset('', { emitEvent: false });
    this.sourceAccountSearchControl.reset('', { emitEvent: false });
    this.destinationInstitutionSearchControl.reset('', { emitEvent: false });
    this.destinationAccountSearchControl.reset('', { emitEvent: false });
    const defaultDescription = this.defaultCompanyEntryDescriptionOption();
    this.companyEntryDescriptionSearchControl.reset(defaultDescription ?? '', { emitEvent: false });

    this.filteredCustomerOptions = [...this.customerOptions];
    this.sourceAccountOptions = [];
    this.filteredSourceAccountOptions = [];
    this.filteredDestinationInstitutionOptions = [...this.destinationInstitutionOptions];
    this.destinationAccountOptions = [];
    this.filteredDestinationAccountSearchOptions = [];
    this.filteredCompanyEntryDescriptionOptions = [...this.companyEntryDescriptionSearchOptions];
    this.updateCustomerSelectionError(false);
    this.updateAutocompleteValidity();
  }

  private markAutocompleteControlsAsTouched(): void {
    [
      this.customerSearchControl,
      this.sourceAccountSearchControl,
      this.companyEntryDescriptionSearchControl,
      this.destinationInstitutionSearchControl,
      this.destinationAccountSearchControl
    ].forEach((control) => {
      control.markAsTouched();
      control.updateValueAndValidity({ emitEvent: false });
    });
  }

  private updateAutocompleteValidity(): void {
    this.customerSearchControl.updateValueAndValidity({ emitEvent: false });
    this.sourceAccountSearchControl.updateValueAndValidity({ emitEvent: false });
    this.companyEntryDescriptionSearchControl.updateValueAndValidity({ emitEvent: false });
    this.destinationInstitutionSearchControl.updateValueAndValidity({ emitEvent: false });
    this.destinationAccountSearchControl.updateValueAndValidity({ emitEvent: false });
  }

  private realSelectionValidator(functionalPath: string, allowEmpty = false): ValidatorFn {
    return (searchControl: AbstractControl): ValidationErrors | null => {
      const searchValue = searchControl.value;
      const functionalValue = this.form.get(functionalPath)?.value;
      const searchIsEmpty = searchValue == null || searchValue === '';
      const functionalIsEmpty = functionalValue == null || functionalValue === '';

      if (allowEmpty && searchIsEmpty && functionalIsEmpty) {
        return null;
      }

      if (!this.isCatalogSearchOption(searchValue)) {
        return { invalidSelection: true };
      }

      return searchValue.value === functionalValue
        ? null
        : { invalidSelection: true };
    };
  }

  private isCatalogSearchOption(value: unknown): value is CatalogSearchOption<unknown, unknown> {
    return typeof value === 'object'
      && value !== null
      && 'value' in value
      && 'label' in value;
  }

  private displayCatalogOption(value: unknown): string {
    if (typeof value === 'string') {
      return value;
    }
    return this.isCatalogSearchOption(value) ? value.label : '';
  }

  private searchText(value: unknown): string {
    if (typeof value === 'string') {
      return normalizeCatalogSearch(value);
    }
    return this.isCatalogSearchOption(value)
      ? value.normalizedSearch
      : '';
  }

  private matchesCatalogSearch(normalizedOption: string, normalizedTerm: string): boolean {
    return !normalizedTerm
      || normalizedTerm.split(' ').every((token) => normalizedOption.includes(token));
  }

  private updateCustomerSelectionError(invalidSelection: boolean): void {
    const control = this.form.get('customerId');
    if (!control) {
      return;
    }

    const errors = { ...(control.errors ?? {}) };
    if (invalidSelection) {
      errors['invalidSelection'] = true;
    } else {
      delete errors['invalidSelection'];
    }
    control.setErrors(Object.keys(errors).length > 0 ? errors : null);
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
      this.destinationAccountOptions = [];
      this.filteredDestinationAccountSearchOptions = [];
      this.resetDestinationAccountSelection();
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
      this.destinationAccountOptions = [];
      this.filteredDestinationAccountSearchOptions = [];
      this.resetDestinationAccountSelection();
      this.cdr.markForCheck();
      return;
    }

    this.filteredDestinationAccounts = institutionId > 0
      ? this.activeDestinationAccounts.filter((item) => item.destinationInstitutionId === institutionId)
      : this.activeDestinationAccounts;
    this.destinationAccountOptions = this.filteredDestinationAccounts.map((account) =>
      this.buildDestinationAccountSearchOption(account)
    );
    this.filteredDestinationAccountSearchOptions = [...this.destinationAccountOptions];

    const currentDestination = this.form.get('destinationAccountNumber')?.value;
    const selectedOption = this.destinationAccountOptions.find((option) => option.value === currentDestination);
    if (!selectedOption) {
      this.resetDestinationAccountSelection();
    } else {
      this.destinationAccountSearchControl.setValue(selectedOption, { emitEvent: false });
      this.destinationAccountSearchControl.updateValueAndValidity({ emitEvent: false });
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
    if (errors['invalidSelection']) return 'Seleccione una opción válida de la lista.';
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

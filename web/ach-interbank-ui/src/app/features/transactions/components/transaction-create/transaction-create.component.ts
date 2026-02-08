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
import { ReturnReasonsApiService } from '../../services/return-reasons-api.service';

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
  private readonly returnReasonsApi = inject(ReturnReasonsApiService);
  private readonly destroy$ = new Subject<void>();

  readonly TransactionType = TransactionTypeEnum;
  readonly AccountType = AccountTypeEnum;
  readonly institutions$ = this.financialInstitutionsApi.getAll().pipe(
    map((list) =>
      (list ?? [])
        .filter((item) => item.status === FinancialInstitutionStatusEnum.Active)
        .sort((a, b) => a.name.localeCompare(b.name))
    )
  );
  readonly returnReasons$ = this.returnReasonsApi.getAll().pipe(
    map((list) => list ?? []),
    map((list) => ({
      receiver: list.filter((item) => item.category === 'R'),
      operator: list.filter((item) => item.category === 'D')
    })),
    shareReplay({ bufferSize: 1, refCount: true })
  );

  readonly form: FormGroup = this.fb.group({
    amount: [null, [Validators.required, Validators.min(0.01)]],
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

  ngOnInit(): void {
    this.form.setValidators([this.validateAccountDifference, this.validateBusinessRules]);
    this.form.valueChanges.pipe(takeUntil(this.destroy$)).subscribe(() => {
      this.errorMessage.setValue(null);
      this.successMessage.setValue(null);
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
        information: ['', [Validators.required, Validators.maxLength(80)]],
        returnReasonCode: [''],
        originalTraceSequence: ['']
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
    const sanitized: TransactionDraft = {
      ...payload,
      type: Number(payload.type) as TransactionTypeEnum,
      accountType: Number(payload.accountType) as AccountTypeEnum,
      isPrenotification: Boolean(payload.isPrenotification),
      amount: Number(payload.amount),
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
              type: TransactionTypeEnum.Credit,
              accountType: AccountTypeEnum.Checking,
              isPrenotification: false,
              companyEntryDescription: 'PAGOS'
            });
            this.addendas.clear();
            this.activeDestinationAccounts = [];
            this.filteredDestinationAccounts = [];
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
    const amount = Number(group.get('amount')?.value ?? 0);
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
        if (type !== '05' && type !== '99') {
          return true;
        }

        if (type === '99') {
          const reason = control.get('returnReasonCode')?.value;
          const seq = control.get('originalTraceSequence')?.value;
          return !reason || !seq;
        }

        return false;
      });

      if (invalidAddenda) {
        errors.invalidAddenda = true;
      }
    }

    return Object.keys(errors).length > 0 ? errors : null;
  };

  private buildAddendaPayload(item: TransactionDraft['addendas'][number]) {
    const addendaType = item.addendaType?.trim().toUpperCase();
    if (addendaType === '99') {
      const reason = item.returnReasonCode?.trim().toUpperCase();
      const sequence = item.originalTraceSequence?.trim();
      const details = item.information.trim();
      return {
        addendaType,
        information: `CAUSAL:${reason} SEQ:${sequence} ${details}`.trim()
      };
    }

    return {
      addendaType,
      information: item.information.trim()
    };
  }
}

import { ChangeDetectionStrategy, ChangeDetectorRef, Component, OnDestroy, OnInit, inject } from '@angular/core';
import { FormArray, FormBuilder, FormControl, FormGroup, Validators } from '@angular/forms';
import { map, take, takeUntil, tap } from 'rxjs';
import { Subject } from 'rxjs';
import { Router } from '@angular/router';
import { TransactionsApiService } from '../../services/transactions-api.service';
import { TransactionDraft, TransactionResponse } from '../../transactions.models';
import { TransactionTypeEnum } from '../../transactions.types';
import { NotificationService } from '../../../../core/services/notification.service';

@Component({
  selector: 'app-transaction-create',
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
  private readonly destroy$ = new Subject<void>();

  readonly TransactionType = TransactionTypeEnum;
  readonly institutions$ = this.api
    .getDestinationInstitutions()
    .pipe(map((list) => (list ?? []).filter((item) => item.status === 1).sort((a, b) => a.name.localeCompare(b.name))));

  readonly form: FormGroup = this.fb.group({
    amount: [null, [Validators.required, Validators.min(0.01)]],
    reference: ['', [Validators.required, Validators.maxLength(30)]],
    type: [TransactionTypeEnum.Credit, Validators.required],
    destinationInstitutionId: [null, [Validators.required, Validators.min(1)]],
    sourceAccountNumber: ['', [Validators.required, Validators.pattern(/^[0-9]{6,18}$/)]],
    destinationAccountNumber: ['', [Validators.required, Validators.pattern(/^[0-9]{6,18}$/)]],
    companyName: ['', [Validators.required, Validators.maxLength(16)]],
    companyIdentification: ['', [Validators.required, Validators.pattern(/^[A-Z0-9]{4,10}$/)]],
    companyEntryDescription: ['PAGOS', [Validators.required, Validators.maxLength(10)]],
    addendas: this.fb.array([])
  });

  readonly isSubmitting = new FormControl(false, { nonNullable: true });
  readonly errorMessage = new FormControl<string | null>(null);
  readonly successMessage = new FormControl<string | null>(null);
  readonly createdResponse = new FormControl<TransactionResponse | null>(null);

  ngOnInit(): void {
    this.form.setValidators(this.validateAccountDifference);
    this.form.valueChanges.pipe(takeUntil(this.destroy$)).subscribe(() => {
      this.errorMessage.setValue(null);
      this.successMessage.setValue(null);
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
        addendaType: ['', [Validators.required, Validators.maxLength(80)]],
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
    const sanitized: TransactionDraft = {
      ...payload,
      type: Number(payload.type) as TransactionTypeEnum,
      amount: Number(payload.amount),
      destinationInstitutionId: Number(payload.destinationInstitutionId),
      reference: payload.reference.trim(),
      sourceAccountNumber: payload.sourceAccountNumber.trim(),
      destinationAccountNumber: payload.destinationAccountNumber.trim(),
      companyName: payload.companyName.trim(),
      companyIdentification: payload.companyIdentification.trim().toUpperCase(),
      companyEntryDescription: payload.companyEntryDescription.trim().toUpperCase(),
      addendas: payload.addendas
        .map((item) => ({
          addendaType: item.addendaType.trim().toUpperCase(),
          information: item.information.trim()
        }))
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
              companyEntryDescription: 'PAGOS'
            });
            this.addendas.clear();
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
}

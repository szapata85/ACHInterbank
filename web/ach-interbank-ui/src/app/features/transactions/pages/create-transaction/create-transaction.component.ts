import { ChangeDetectionStrategy, Component, OnDestroy, OnInit, inject } from '@angular/core';
import { FormBuilder, Validators } from '@angular/forms';
import { Subject, finalize, map, takeUntil } from 'rxjs';
import { NotificationService } from '../../../../core/services/notification.service';
import { SharedModule } from '../../../../shared/shared.module';
import { TransactionsApiService } from '../../services/transactions-api.service';
import { CreateTransactionRequest, TransactionResponse } from '../../transactions.models';
import { TransactionTypeEnum } from '../../transactions.types';

@Component({
  selector: 'app-create-transaction',
  standalone: true,
  imports: [SharedModule],
  templateUrl: './create-transaction.component.html',
  styleUrls: ['./create-transaction.component.scss'],
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class CreateTransactionComponent implements OnInit, OnDestroy {
  private readonly fb = inject(FormBuilder);
  private readonly api = inject(TransactionsApiService);
  private readonly notifications = inject(NotificationService);
  private readonly destroy$ = new Subject<void>();

  readonly institutions$ = this.api
    .getDestinationInstitutions()
    .pipe(map((list) => (list ?? []).filter((item) => item.status === 1).sort((a, b) => a.name.localeCompare(b.name))));

  readonly form = this.fb.group({
    sourceAccount: ['', [Validators.required, Validators.pattern(/^[0-9]{6,18}$/)]],
    destinationAccount: ['', [Validators.required, Validators.pattern(/^[0-9]{6,18}$/)]],
    amount: [null, [Validators.required, Validators.min(0.01)]],
    description: ['', [Validators.required, Validators.maxLength(200)]],
    destinationInstitutionId: [null, [Validators.required, Validators.min(1)]]
  });

  isSubmitting = false;
  submissionError: string | null = null;
  submissionSuccess: string | null = null;
  createdResponse: TransactionResponse | null = null;

  ngOnInit(): void {
    this.form.valueChanges.pipe(takeUntil(this.destroy$)).subscribe(() => {
      this.submissionError = null;
      this.submissionSuccess = null;
      this.createdResponse = null;
    });
  }

  ngOnDestroy(): void {
    this.destroy$.next();
    this.destroy$.complete();
  }

  submit(): void {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    const value = this.form.getRawValue();
    const payload: CreateTransactionRequest = {
      amount: Number(value.amount),
      reference: value.description.trim(),
      type: TransactionTypeEnum.Credit,
      destinationInstitutionId: Number(value.destinationInstitutionId),
      sourceAccountNumber: value.sourceAccount.trim(),
      destinationAccountNumber: value.destinationAccount.trim()
    };

    this.isSubmitting = true;
    this.submissionError = null;

    this.api
      .createTransaction(payload)
      .pipe(
        takeUntil(this.destroy$),
        finalize(() => {
          this.isSubmitting = false;
        })
      )
      .subscribe({
        next: (response) => {
          this.createdResponse = response;
          this.submissionSuccess = 'Transacción creada exitosamente';
          this.notifications.success('Transacción creada correctamente');
          this.form.reset();
        },
        error: (error: Error) => {
          this.submissionError = error.message || 'Ocurrió un error al crear la transacción';
          this.notifications.error(this.submissionError);
        }
      });
  }
}

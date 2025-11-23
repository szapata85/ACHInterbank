import { CommonModule } from '@angular/common';
import { ChangeDetectionStrategy, Component, DestroyRef, inject, OnInit } from '@angular/core';
import { FormArray, FormBuilder, FormControl, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { Observable, map, tap, take } from 'rxjs';
import { TransactionsService } from './transactions.service';
import { DestinationInstitution, TransactionDraft, TransactionResponse } from './transactions.models';
import { TransactionTypeEnum } from './transactions.types';
import { ErrorMessageComponent } from '../shared/error-message.component';

@Component({
  selector: 'app-transaction-form',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, ErrorMessageComponent],
  templateUrl: './transaction-form.component.html',
  styleUrls: ['./transaction-form.component.scss'],
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class TransactionFormComponent implements OnInit {
  private readonly fb = inject(FormBuilder);
  private readonly transactionsService = inject(TransactionsService);
  private readonly destroyRef = inject(DestroyRef);

  readonly TransactionType = TransactionTypeEnum;

  readonly institutions$: Observable<DestinationInstitution[]> = this.transactionsService
    .getDestinationInstitutions()
    .pipe(map((list) => list.sort((a, b) => a.name.localeCompare(b.name))));

  readonly form: FormGroup = this.fb.group({
    amount: [null, [Validators.required, Validators.min(0.01)]],
    reference: ['', [Validators.required, Validators.maxLength(30)]],
    type: [TransactionTypeEnum.Credit, Validators.required],
    destinationInstitutionId: [null, [Validators.required, Validators.min(1)]],
    sourceAccountNumber: [
      '',
      [Validators.required, Validators.pattern(/^[0-9]{6,18}$/)]
    ],
    destinationAccountNumber: [
      '',
      [Validators.required, Validators.pattern(/^[0-9]{6,18}$/)]
    ],
    companyName: ['', [Validators.required, Validators.maxLength(16)]],
    companyIdentification: [
      '',
      [Validators.required, Validators.pattern(/^[A-Z0-9]{4,10}$/)]
    ],
    companyEntryDescription: ['PAGOS', [Validators.required, Validators.maxLength(10)]],
    addendas: this.fb.array([])
  });

  readonly submissionState$ = new FormControl<'idle' | 'pending' | 'success' | 'error'>('idle', { nonNullable: true });
  readonly response$ = new FormControl<TransactionResponse | null>(null);
  readonly errorMessage$ = new FormControl<string | null>(null);

  ngOnInit(): void {
    this.form.setValidators(this.validateAccountDifference);
    this.form.valueChanges.pipe(takeUntilDestroyed(this.destroyRef)).subscribe(() => {
      if (this.submissionState$.value !== 'idle') {
        this.submissionState$.setValue('idle');
        this.errorMessage$.setValue(null);
        this.response$.setValue(null);
      }
    });
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
    const sanitizedPayload: TransactionDraft = {
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
    this.submissionState$.setValue('pending');
    this.errorMessage$.setValue(null);

    this.transactionsService
      .registerTransaction(sanitizedPayload)
      .pipe(
        take(1),
        tap({
          next: (response) => {
            this.response$.setValue(response);
            this.errorMessage$.setValue(null);
            this.submissionState$.setValue('success');
            this.form.reset({
              type: TransactionTypeEnum.Credit,
              companyEntryDescription: 'PAGOS'
            });
            this.addendas.clear();
          },
          error: (error) => {
            this.response$.setValue(null);
            this.errorMessage$.setValue(error.message ?? 'Error inesperado');
            this.submissionState$.setValue('error');
          }
        })
      )
      .subscribe();
  }

  trackAddenda(_: number, item: FormGroup): string {
    return item.get('addendaType')?.value ?? '';
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

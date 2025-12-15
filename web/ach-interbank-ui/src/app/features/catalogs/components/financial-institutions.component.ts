import { NgFor, NgIf } from '@angular/common';
import { ChangeDetectionStrategy, ChangeDetectorRef, Component, OnInit, inject } from '@angular/core';
import { FormBuilder, Validators } from '@angular/forms';
import { finalize } from 'rxjs/operators';
import { SharedModule } from '../../../shared/shared.module';
import { DestinationInstitution } from '../../transactions/transactions.models';
import { FinancialInstitutionStatusEnum } from '../../transactions/transactions.types';
import {
  FinancialInstitutionAdminService,
  FinancialInstitutionPayload
} from '../services/financial-institution-admin.service';

@Component({
  selector: 'app-financial-institutions',
  templateUrl: './financial-institutions.component.html',
  styleUrls: ['./financial-institutions.component.scss'],
  standalone: true,
  imports: [SharedModule, NgFor, NgIf],
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class FinancialInstitutionsComponent implements OnInit {
  readonly statusEnum = FinancialInstitutionStatusEnum;

  private readonly service = inject(FinancialInstitutionAdminService);
  private readonly fb = inject(FormBuilder);
  private readonly cdr = inject(ChangeDetectorRef);

  institutions: DestinationInstitution[] = [];
  loading = false;
  saving = false;
  showForm = false;
  editing: DestinationInstitution | null = null;

  form = this.fb.nonNullable.group({
    name: ['', [Validators.required, Validators.maxLength(100)]],
    routingNumber: ['', [Validators.required, Validators.maxLength(9)]],
    transitCode: ['', [Validators.required, Validators.maxLength(4)]],
    checkDigit: ['', [Validators.required, Validators.maxLength(1)]],
    isDefaultSource: [false],
    status: [FinancialInstitutionStatusEnum.Active, Validators.required]
  });

  ngOnInit(): void {
    this.loadInstitutions();
  }

  loadInstitutions(): void {
    this.loading = true;
    this.service
      .list(true)
      .pipe(
        finalize(() => {
          this.loading = false;
          this.cdr.markForCheck();
        })
      )
      .subscribe((data) => {
        this.institutions = data;
      });
  }

  startCreate(): void {
    this.editing = null;
    this.showForm = true;
    this.form.reset({
      name: '',
      routingNumber: '',
      transitCode: '',
      checkDigit: '',
      isDefaultSource: false,
      status: FinancialInstitutionStatusEnum.Active
    });
    this.cdr.markForCheck();
  }

  startEdit(item: DestinationInstitution): void {
    this.editing = item;
    this.showForm = true;
    this.form.reset({
      name: item.name,
      routingNumber: item.routingNumber,
      transitCode: item.transitCode,
      checkDigit: item.checkDigit,
      isDefaultSource: item.isDefaultSource,
      status: item.status
    });
    this.cdr.markForCheck();
  }

  cancelEdit(): void {
    this.showForm = false;
    this.editing = null;
    this.form.reset({
      name: '',
      routingNumber: '',
      transitCode: '',
      checkDigit: '',
      isDefaultSource: false,
      status: FinancialInstitutionStatusEnum.Active
    });
    this.cdr.markForCheck();
  }

  save(): void {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    const payload = this.form.getRawValue() as FinancialInstitutionPayload;
    this.saving = true;

    const request$ = this.editing
      ? this.service.update(this.editing.id, payload)
      : this.service.create(payload);

    request$
      .pipe(
        finalize(() => {
          this.saving = false;
          this.cdr.markForCheck();
        })
      )
      .subscribe(() => {
        this.cancelEdit();
        this.loadInstitutions();
      });
  }

  toggleStatus(item: DestinationInstitution): void {
    const nextStatus =
      item.status === FinancialInstitutionStatusEnum.Active
        ? FinancialInstitutionStatusEnum.Inactive
        : FinancialInstitutionStatusEnum.Active;

    this.saving = true;
    this.service
      .setStatus(item.id, nextStatus)
      .pipe(
        finalize(() => {
          this.saving = false;
          this.cdr.markForCheck();
        })
      )
      .subscribe(() => this.loadInstitutions());
  }
}

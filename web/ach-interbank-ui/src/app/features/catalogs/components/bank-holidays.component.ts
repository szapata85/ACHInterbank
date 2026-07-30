import { ChangeDetectionStrategy, ChangeDetectorRef, Component, DestroyRef, OnInit, TemplateRef, ViewChild, inject } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { FormBuilder, Validators } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MatCardModule } from '@angular/material/card';
import { MAT_DATE_LOCALE, MatNativeDateModule } from '@angular/material/core';
import { MatDatepickerModule } from '@angular/material/datepicker';
import { MatDialog, MatDialogModule } from '@angular/material/dialog';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatIconModule } from '@angular/material/icon';
import { MatInputModule } from '@angular/material/input';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatTableModule } from '@angular/material/table';
import { finalize, take } from 'rxjs/operators';
import { NotificationService } from '../../../core/services/notification.service';
import { SharedModule } from '../../../shared/shared.module';
import {
  BankHoliday,
  parseBankHolidayLocalDate,
  toBankHolidayDateOnly
} from '../models/bank-holiday.model';
import { BankHolidaysAdminService } from '../services/bank-holidays-admin.service';

@Component({
  selector: 'app-bank-holidays',
  templateUrl: './bank-holidays.component.html',
  styleUrls: ['./bank-holidays.component.scss'],
  standalone: true,
  imports: [
    SharedModule,
    MatButtonModule,
    MatCardModule,
    MatDatepickerModule,
    MatDialogModule,
    MatFormFieldModule,
    MatIconModule,
    MatInputModule,
    MatNativeDateModule,
    MatProgressSpinnerModule,
    MatTableModule
  ],
  providers: [{ provide: MAT_DATE_LOCALE, useValue: 'es-CO' }],
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class BankHolidaysComponent implements OnInit {
  private readonly service = inject(BankHolidaysAdminService);
  private readonly fb = inject(FormBuilder);
  private readonly notifications = inject(NotificationService);
  private readonly cdr = inject(ChangeDetectorRef);
  private readonly destroyRef = inject(DestroyRef);
  private readonly dialog = inject(MatDialog);
  private readonly dateFormatter = new Intl.DateTimeFormat('es-CO', {
    day: 'numeric',
    month: 'long',
    year: 'numeric'
  });
  private loadSequence = 0;
  private deleteDialogOpen = false;

  @ViewChild('deleteDialog', { static: true })
  private deleteDialog!: TemplateRef<unknown>;

  holidays: BankHoliday[] = [];
  loading = false;
  loadError = false;
  saving = false;
  showForm = false;
  hasSearched = false;
  editing: BankHoliday | null = null;
  pendingDelete: BankHoliday | null = null;
  successMessage = '';
  operationError = '';
  lastLoadedYear = new Date().getFullYear();

  readonly displayedColumns = ['date', 'description', 'countryCode', 'actions'];
  readonly minYear = 1900;
  readonly maxYear = 2100;

  readonly filterForm = this.fb.nonNullable.group({
    year: [
      new Date().getFullYear(),
      [Validators.required, Validators.min(1900), Validators.max(2100)]
    ]
  });

  readonly form = this.fb.group({
    date: this.fb.control<Date | null>(null, Validators.required),
    description: this.fb.nonNullable.control('', [
      Validators.required,
      Validators.pattern(/\S/),
      Validators.maxLength(200)
    ]),
    countryCode: this.fb.nonNullable.control('CO', [
      Validators.required,
      Validators.pattern(/\S/),
      Validators.maxLength(5)
    ])
  });

  ngOnInit(): void {
    this.search();
  }

  load(year: number): void {
    const sequence = ++this.loadSequence;
    this.loading = true;
    this.loadError = false;
    this.hasSearched = true;
    this.lastLoadedYear = year;
    this.cdr.markForCheck();

    this.service
      .list(year)
      .pipe(
        takeUntilDestroyed(this.destroyRef),
        finalize(() => {
          if (sequence === this.loadSequence) {
            this.loading = false;
            this.cdr.markForCheck();
          }
        })
      )
      .subscribe({
        next: (data) => {
          if (sequence !== this.loadSequence) {
            return;
          }

          this.holidays = [...data].sort((left, right) => left.date.localeCompare(right.date));
        },
        error: () => {
          if (sequence !== this.loadSequence) {
            return;
          }

          this.holidays = [];
          this.loadError = true;
        }
      });
  }

  search(): void {
    if (this.loading || this.filterForm.invalid) {
      this.filterForm.markAllAsTouched();
      return;
    }

    this.load(this.filterForm.controls.year.value);
  }

  startCreate(): void {
    if (this.loading || this.saving) {
      return;
    }

    this.clearOperationFeedback();
    this.editing = null;
    this.showForm = true;
    this.form.reset({
      date: null,
      description: '',
      countryCode: 'CO'
    });
    this.cdr.markForCheck();
  }

  startEdit(item: BankHoliday): void {
    if (this.saving) {
      return;
    }

    this.clearOperationFeedback();
    this.editing = item;
    this.showForm = true;
    this.form.reset({
      date: parseBankHolidayLocalDate(item.date),
      description: item.description,
      countryCode: item.countryCode
    });
    this.cdr.markForCheck();
  }

  cancelEdit(): void {
    if (this.saving) {
      return;
    }

    this.closeForm();
    this.operationError = '';
    this.cdr.markForCheck();
  }

  save(): void {
    if (this.saving) {
      return;
    }

    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    const values = this.form.getRawValue();
    const payload: BankHoliday = {
      id: this.editing?.id ?? 0,
      date: toBankHolidayDateOnly(values.date),
      description: values.description.trim(),
      countryCode: values.countryCode.trim().toUpperCase()
    };
    const wasEditing = this.editing !== null;

    this.clearOperationFeedback();
    this.saving = true;
    const request = wasEditing ? this.service.update(payload) : this.service.create(payload);

    request
      .pipe(
        takeUntilDestroyed(this.destroyRef),
        finalize(() => {
          this.saving = false;
          this.cdr.markForCheck();
        })
      )
      .subscribe({
        next: () => {
          this.closeForm();
          this.successMessage = wasEditing
            ? 'Festivo actualizado correctamente.'
            : 'Festivo creado correctamente.';
          this.notifications.success(this.successMessage);
          this.search();
        },
        error: () => {
          this.operationError = wasEditing
            ? 'No fue posible actualizar el festivo. Intenta nuevamente.'
            : 'No fue posible crear el festivo. Intenta nuevamente.';
          this.notifications.error(this.operationError);
        }
      });
  }

  remove(item: BankHoliday): void {
    if (this.saving || this.deleteDialogOpen) {
      return;
    }

    this.clearOperationFeedback();
    this.pendingDelete = item;
    this.deleteDialogOpen = true;

    this.dialog
      .open(this.deleteDialog, {
        width: 'min(480px, calc(100vw - 2rem))',
        maxWidth: '100vw',
        autoFocus: 'first-tabbable',
        restoreFocus: true
      })
      .afterClosed()
      .pipe(take(1), takeUntilDestroyed(this.destroyRef))
      .subscribe((confirmed: unknown) => {
        this.deleteDialogOpen = false;
        this.pendingDelete = null;
        if (confirmed === true) {
          this.deleteHoliday(item);
        }
        this.cdr.markForCheck();
      });
  }

  formatHolidayDate(value: string): string {
    const date = parseBankHolidayLocalDate(value);
    return date ? this.dateFormatter.format(date) : value;
  }

  private deleteHoliday(item: BankHoliday): void {
    if (this.saving) {
      return;
    }

    this.saving = true;
    this.service
      .delete(item.id)
      .pipe(
        takeUntilDestroyed(this.destroyRef),
        finalize(() => {
          this.saving = false;
          this.cdr.markForCheck();
        })
      )
      .subscribe({
        next: () => {
          this.successMessage = 'Festivo eliminado correctamente.';
          this.notifications.success(this.successMessage);
          this.search();
        },
        error: () => {
          this.operationError = 'No fue posible eliminar el festivo. Intenta nuevamente.';
          this.notifications.error(this.operationError);
        }
      });
  }

  private closeForm(): void {
    this.showForm = false;
    this.editing = null;
    this.form.reset({
      date: null,
      description: '',
      countryCode: 'CO'
    });
  }

  private clearOperationFeedback(): void {
    this.successMessage = '';
    this.operationError = '';
  }
}

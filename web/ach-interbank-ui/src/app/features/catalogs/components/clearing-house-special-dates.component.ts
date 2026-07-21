import { NgFor, NgIf } from '@angular/common';
import {
  ChangeDetectionStrategy,
  ChangeDetectorRef,
  Component,
  NgZone,
  OnDestroy,
  OnInit,
  inject
} from '@angular/core';
import { FormBuilder, Validators } from '@angular/forms';
import { ColDef, GridApi } from 'ag-grid-community';
import { Subject, of } from 'rxjs';
import { finalize, tap } from 'rxjs/operators';
import { SharedModule } from '../../../shared/shared.module';
import { BankHoliday } from '../models/bank-holiday.model';
import { ClearingHouseSpecialDate } from '../models/clearing-house-special-date.model';
import { BankHolidaysAdminService } from '../services/bank-holidays-admin.service';
import { ClearingHouseSpecialDatesService } from '../services/clearing-house-special-dates.service';
import { ClearingHousesApiService } from '../../ach-cycles/services/ach-cycles-api.service';
import { ClearingHouseOption } from '../../ach-cycles/models/ach-cycle.model';

@Component({
  selector: 'app-clearing-house-special-dates',
  templateUrl: './clearing-house-special-dates.component.html',
  styleUrls: ['./clearing-house-special-dates.component.scss'],
  standalone: true,
  imports: [SharedModule, NgIf, NgFor],
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class ClearingHouseSpecialDatesComponent implements OnInit, OnDestroy {
  private readonly service = inject(ClearingHouseSpecialDatesService);
  private readonly clearingHouseApi = inject(ClearingHousesApiService);
  private readonly bankHolidaysService = inject(BankHolidaysAdminService);
  private readonly fb = inject(FormBuilder);
  private readonly cdr = inject(ChangeDetectorRef);
  private readonly zone = inject(NgZone);

  specialDates: ClearingHouseSpecialDate[] = [];
  clearingHouses: ClearingHouseOption[] = [];
  private readonly bankHolidaysByYear = new Map<number, BankHoliday[]>();
  loading = false;
  saving = false;
  showForm = false;
  editing: ClearingHouseSpecialDate | null = null;
  gridApi?: GridApi<ClearingHouseSpecialDate>;
  private readonly destroy$ = new Subject<void>();

  readonly columnDefs: ColDef<ClearingHouseSpecialDate>[] = [
    {
      field: 'date',
      headerName: 'Fecha',
      maxWidth: 160,
      valueFormatter: (params) => this.formatDate(params.value)
    },
    { field: 'clearingHouseName', headerName: 'CÃ¡mara', flex: 1, filter: 'agTextColumnFilter' },
    { field: 'description', headerName: 'DescripciÃ³n', flex: 1, filter: 'agTextColumnFilter' },
    {
      headerName: 'Acciones',
      colId: 'actions',
      maxWidth: 200,
      cellRenderer: (params) => {
        const container = document.createElement('div');
        container.classList.add('row-actions');

        const edit = document.createElement('button');
        edit.type = 'button';
        edit.classList.add('link');
        edit.innerText = 'Editar';
        edit.addEventListener('click', () => {
          this.zone.run(() => {
            params.context?.componentParent?.startEdit(params.data);
          });
        });

        const status = document.createElement('button');
        status.type = 'button';
        status.classList.add('link');
        status.classList.add('danger');
        status.innerText = params.data.isActive ? 'Desactivar' : 'Activar';
        status.addEventListener('click', () => {
          this.zone.run(() => {
            params.context?.componentParent?.remove(params.data);
          });
        });

        container.append(edit, status);
        return container;
      }
    }
  ];

  readonly defaultColDef: ColDef<ClearingHouseSpecialDate> = {
    resizable: true,
    sortable: true,
    suppressHeaderKeyboardEvent: () => true,
    filterParams: { suppressAndOrCondition: true }
  };

  readonly noRowsTemplate = 'No hay fechas especiales registradas.';
  readonly loadingTemplate = 'Cargando fechas especiales...';

  form = this.fb.nonNullable.group({
    clearingHouseId: [0, Validators.min(1)],
    date: ['', Validators.required],
    description: ['', [Validators.required, Validators.maxLength(200)]]
  });

  get dateErrorText(): string | null {
    const control = this.form.get('date');
    if (!control || !control.touched) {
      return null;
    }
    if (control.hasError('duplicateDate')) {
      return 'Ya existe una fecha especial para esta cÃ¡mara.';
    }
    if (control.hasError('weekendDate')) {
      return 'No se permiten fechas en fin de semana.';
    }
    if (control.hasError('bankHoliday')) {
      return 'La fecha coincide con un festivo bancario.';
    }
    return null;
  }

  ngOnInit(): void {
    this.load();
    this.loadClearingHouses();
  }

  ngOnDestroy(): void {
    this.destroy$.next();
    this.destroy$.complete();
  }

  onGridReady(api: GridApi<ClearingHouseSpecialDate>): void {
    this.gridApi = api;
    this.updateGridOverlays();
  }

  load(): void {
    this.loading = true;
    this.updateGridOverlays();
    this.service
      .list()
      .pipe(
        finalize(() => {
          this.loading = false;
          this.cdr.markForCheck();
        })
      )
      .subscribe((data) => {
        this.specialDates = data;
        this.updateGridOverlays();
      });
  }

  loadClearingHouses(): void {
    this.clearingHouseApi.listAdministrative().subscribe((data) => {
      this.clearingHouses = data ?? [];
      this.cdr.markForCheck();
    });
  }

  startCreate(): void {
    this.editing = null;
    this.showForm = true;
    this.form.reset({
      clearingHouseId: this.clearingHouses[0]?.id ?? 0,
      date: '',
      description: ''
    });
    this.cdr.markForCheck();
  }

  startEdit(item: ClearingHouseSpecialDate): void {
    this.editing = item;
    this.showForm = true;
    this.form.reset({
      clearingHouseId: item.clearingHouseId,
      date: item.date,
      description: item.description
    });
    this.cdr.markForCheck();
  }

  cancelEdit(): void {
    this.showForm = false;
    this.editing = null;
    this.form.reset({
      clearingHouseId: this.clearingHouses[0]?.id ?? 0,
      date: '',
      description: ''
    });
    this.cdr.markForCheck();
  }

  save(): void {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    const dateValue = this.form.value.date ?? '';
    const clearingHouseId = this.form.value.clearingHouseId ?? 0;
    const year = this.getYear(dateValue);

    if (!year) {
      this.setDateValidationError('required');
      return;
    }

    this.ensureBankHolidays(year).subscribe((holidays) => {
      if (!this.validateDate(dateValue, clearingHouseId, holidays)) {
        this.cdr.markForCheck();
        return;
      }

      const payload: ClearingHouseSpecialDate = {
        id: this.editing?.id ?? 0,
        clearingHouseId,
        date: dateValue,
        description: this.form.value.description ?? '',
        isActive: this.editing?.isActive ?? true
      };

      this.saving = true;
      const request = this.editing ? this.service.update(payload) : this.service.create(payload);

      request
        .pipe(
          finalize(() => {
            this.saving = false;
            this.cdr.markForCheck();
          })
        )
        .subscribe(() => {
          this.cancelEdit();
          this.load();
        });
    });
  }

  remove(item: ClearingHouseSpecialDate): void {
    if (item.isActive && !confirm(`¿Desactivar la fecha especial del ${this.formatDate(item.date)}?`)) {
      return;
    }

    this.saving = true;
    this.service
      .changeStatus(item.id, !item.isActive)
      .pipe(
        finalize(() => {
          this.saving = false;
          this.cdr.markForCheck();
        })
      )
      .subscribe(() => {
        this.load();
      });
  }

  private updateGridOverlays(): void {
    if (!this.gridApi) return;

    if (this.loading) {
      this.gridApi.showLoadingOverlay();
    } else if (!this.specialDates.length) {
      this.gridApi.showNoRowsOverlay();
    } else {
      this.gridApi.hideOverlay();
    }
  }

  private formatDate(value?: string | null): string {
    if (!value) return '';
    const date = new Date(value);
    return Number.isNaN(date.getTime()) ? value : date.toLocaleDateString('es-CO');
  }

  private ensureBankHolidays(year: number) {
    const cached = this.bankHolidaysByYear.get(year);
    if (cached) {
      return of(cached);
    }

    return this.bankHolidaysService.list(year).pipe(
      tap((holidays) => {
        this.bankHolidaysByYear.set(year, holidays ?? []);
      }),
      finalize(() => this.cdr.markForCheck())
    );
  }

  private validateDate(dateValue: string, clearingHouseId: number, holidays: BankHoliday[]): boolean {
    const control = this.form.get('date');
    if (!control) {
      return false;
    }

    const currentErrors = control.errors ?? {};
    delete currentErrors.duplicateDate;
    delete currentErrors.weekendDate;
    delete currentErrors.bankHoliday;
    control.setErrors(Object.keys(currentErrors).length ? currentErrors : null);

    const normalized = this.normalizeDate(dateValue);
    if (!normalized) {
      this.setDateValidationError('required');
      return false;
    }

    if (this.isWeekend(normalized)) {
      this.setDateValidationError('weekendDate');
      return false;
    }

    const isHoliday = holidays.some((holiday) => this.normalizeDate(holiday.date) === normalized);
    if (isHoliday) {
      this.setDateValidationError('bankHoliday');
      return false;
    }

    const isDuplicate = this.specialDates.some(
      (item) =>
        item.clearingHouseId === clearingHouseId &&
        this.normalizeDate(item.date) === normalized &&
        item.id !== (this.editing?.id ?? 0)
    );
    if (isDuplicate) {
      this.setDateValidationError('duplicateDate');
      return false;
    }

    return true;
  }

  private setDateValidationError(key: string): void {
    const control = this.form.get('date');
    if (!control) return;
    const errors = control.errors ?? {};
    control.setErrors({ ...errors, [key]: true });
    control.markAsTouched();
  }

  private normalizeDate(value: string | null | undefined): string {
    if (!value) return '';
    return value.split('T')[0];
  }

  private getYear(value: string): number | null {
    const normalized = this.normalizeDate(value);
    const year = Number(normalized.split('-')[0]);
    return Number.isFinite(year) ? year : null;
  }

  private isWeekend(value: string): boolean {
    const date = new Date(`${value}T00:00:00`);
    const day = date.getDay();
    return day === 0 || day === 6;
  }
}

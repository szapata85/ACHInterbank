import { NgIf } from '@angular/common';
import { ChangeDetectionStrategy, ChangeDetectorRef, Component, NgZone, OnDestroy, inject } from '@angular/core';
import { FormBuilder, Validators } from '@angular/forms';
import { AgGridModule } from 'ag-grid-angular';
import { ColDef, GridApi, GridReadyEvent } from 'ag-grid-community';
import { Subject } from 'rxjs';
import { finalize } from 'rxjs/operators';
import { SharedModule } from '../../../shared/shared.module';
import { BankHoliday } from '../models/bank-holiday.model';
import { BankHolidaysAdminService } from '../services/bank-holidays-admin.service';

@Component({
  selector: 'app-bank-holidays',
  templateUrl: './bank-holidays.component.html',
  styleUrls: ['./bank-holidays.component.scss'],
  standalone: true,
  imports: [SharedModule, NgIf, AgGridModule],
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class BankHolidaysComponent implements OnDestroy {
  private readonly service = inject(BankHolidaysAdminService);
  private readonly fb = inject(FormBuilder);
  private readonly cdr = inject(ChangeDetectorRef);
  private readonly zone = inject(NgZone);

  holidays: BankHoliday[] = [];
  loading = false;
  saving = false;
  showForm = false;
  hasSearched = false;
  editing: BankHoliday | null = null;
  gridApi?: GridApi<BankHoliday>;
  private readonly destroy$ = new Subject<void>();

  readonly columnDefs: ColDef<BankHoliday>[] = [
    {
      field: 'date',
      headerName: 'Fecha',
      maxWidth: 160,
      valueFormatter: (params) => this.formatDate(params.value)
    },
    { field: 'description', headerName: 'Descripción', flex: 1, filter: 'agTextColumnFilter' },
    { field: 'countryCode', headerName: 'País', maxWidth: 120 },
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

        const remove = document.createElement('button');
        remove.type = 'button';
        remove.classList.add('link');
        remove.classList.add('danger');
        remove.innerText = 'Eliminar';
        remove.addEventListener('click', () => {
          this.zone.run(() => {
            params.context?.componentParent?.remove(params.data);
          });
        });

        container.append(edit, remove);
        return container;
      }
    }
  ];

  readonly defaultColDef: ColDef<BankHoliday> = {
    resizable: true,
    sortable: true,
    suppressHeaderKeyboardEvent: () => true,
    filterParams: { suppressAndOrCondition: true }
  };

  readonly noRowsTemplate = 'No hay festivos registrados.';
  readonly loadingTemplate = 'Cargando festivos...';

  filterForm = this.fb.nonNullable.group({
    year: [new Date().getFullYear(), [Validators.required, Validators.min(1900), Validators.max(2100)]]
  });

  form = this.fb.nonNullable.group({
    date: ['', Validators.required],
    description: ['', [Validators.required, Validators.maxLength(200)]],
    countryCode: ['CO', [Validators.required, Validators.maxLength(5)]]
  });

  ngOnDestroy(): void {
    this.destroy$.next();
    this.destroy$.complete();
  }

  onGridReady(event: GridReadyEvent<BankHoliday>): void {
    this.gridApi = event.api;
    this.updateGridOverlays();
  }

  load(year?: number): void {
    this.loading = true;
    this.hasSearched = true;
    this.updateGridOverlays();
    this.service
      .list(year)
      .pipe(
        finalize(() => {
          this.loading = false;
          this.cdr.markForCheck();
        })
      )
      .subscribe((data) => {
        this.holidays = data;
        this.updateGridOverlays();
      });
  }

  search(): void {
    if (this.filterForm.invalid) {
      this.filterForm.markAllAsTouched();
      return;
    }

    const { year } = this.filterForm.getRawValue();
    this.load(year);
  }

  startCreate(): void {
    this.editing = null;
    this.showForm = true;
    this.form.reset({
      date: '',
      description: '',
      countryCode: 'CO'
    });
    this.cdr.markForCheck();
  }

  startEdit(item: BankHoliday): void {
    this.editing = item;
    this.showForm = true;
    this.form.reset({
      date: item.date,
      description: item.description,
      countryCode: item.countryCode
    });
    this.cdr.markForCheck();
  }

  cancelEdit(): void {
    this.showForm = false;
    this.editing = null;
    this.form.reset({
      date: '',
      description: '',
      countryCode: 'CO'
    });
    this.cdr.markForCheck();
  }

  save(): void {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    const payload: BankHoliday = {
      id: this.editing?.id ?? 0,
      date: this.form.value.date ?? '',
      description: this.form.value.description ?? '',
      countryCode: this.form.value.countryCode ?? 'CO'
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
        this.search();
      });
  }

  remove(item: BankHoliday): void {
    if (!confirm(`¿Eliminar el festivo del ${this.formatDate(item.date)}?`)) {
      return;
    }

    this.saving = true;
    this.service
      .delete(item.id)
      .pipe(
        finalize(() => {
          this.saving = false;
          this.cdr.markForCheck();
        })
      )
      .subscribe(() => {
        if (this.hasSearched) {
          this.search();
        }
      });
  }

  private updateGridOverlays(): void {
    if (!this.gridApi) return;

    if (this.loading) {
      this.gridApi.showLoadingOverlay();
    } else if (this.hasSearched && !this.holidays.length) {
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
}

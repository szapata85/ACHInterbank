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
import { AgGridModule } from 'ag-grid-angular';
import { ColDef, GridApi, GridReadyEvent } from 'ag-grid-community';
import { Subject } from 'rxjs';
import { finalize } from 'rxjs/operators';
import { SharedModule } from '../../../shared/shared.module';
import { ClearingHouseSpecialDate } from '../models/clearing-house-special-date.model';
import { ClearingHouseSpecialDatesService } from '../services/clearing-house-special-dates.service';
import { ClearingHousesApiService } from '../../ach-cycles/services/ach-cycles-api.service';
import { ClearingHouseOption } from '../../ach-cycles/models/ach-cycle.model';

@Component({
  selector: 'app-clearing-house-special-dates',
  templateUrl: './clearing-house-special-dates.component.html',
  styleUrls: ['./clearing-house-special-dates.component.scss'],
  standalone: true,
  imports: [SharedModule, NgIf, NgFor, AgGridModule],
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class ClearingHouseSpecialDatesComponent implements OnInit, OnDestroy {
  private readonly service = inject(ClearingHouseSpecialDatesService);
  private readonly clearingHouseApi = inject(ClearingHousesApiService);
  private readonly fb = inject(FormBuilder);
  private readonly cdr = inject(ChangeDetectorRef);
  private readonly zone = inject(NgZone);

  specialDates: ClearingHouseSpecialDate[] = [];
  clearingHouses: ClearingHouseOption[] = [];
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
    { field: 'clearingHouseName', headerName: 'Cámara', flex: 1, filter: 'agTextColumnFilter' },
    { field: 'description', headerName: 'Descripción', flex: 1, filter: 'agTextColumnFilter' },
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

  ngOnInit(): void {
    this.load();
    this.loadClearingHouses();
  }

  ngOnDestroy(): void {
    this.destroy$.next();
    this.destroy$.complete();
  }

  onGridReady(event: GridReadyEvent<ClearingHouseSpecialDate>): void {
    this.gridApi = event.api;
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
    this.clearingHouseApi.list().subscribe((data) => {
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

    const payload: ClearingHouseSpecialDate = {
      id: this.editing?.id ?? 0,
      clearingHouseId: this.form.value.clearingHouseId ?? 0,
      date: this.form.value.date ?? '',
      description: this.form.value.description ?? ''
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
  }

  remove(item: ClearingHouseSpecialDate): void {
    if (!confirm(`¿Eliminar la fecha especial del ${this.formatDate(item.date)}?`)) {
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
}

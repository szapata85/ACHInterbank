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
import { AgGridModule } from 'ag-grid-angular';
import { ColDef, GridApi, GridReadyEvent } from 'ag-grid-community';
import { FormBuilder, Validators } from '@angular/forms';
import { Subject } from 'rxjs';
import { finalize, map, takeUntil } from 'rxjs/operators';
import { ClearingHousesApiService } from '../../ach-cycles/services/ach-cycles-api.service';
import { ClearingHouseOption } from '../../ach-cycles/models/ach-cycle.model';
import { SharedModule } from '../../../shared/shared.module';
import { InstitutionClearingHousePreference } from '../models/institution-clearing-house-preference.model';
import { InstitutionClearingHousePreferencesService } from '../services/institution-clearing-house-preferences.service';
import { FinancialInstitutionsApiService } from '../../transactions/services/financial-institutions-api.service';
import { FinancialInstitutionStatusEnum } from '../../transactions/transactions.types';

@Component({
  selector: 'app-clearing-house-preferences',
  templateUrl: './clearing-house-preferences.component.html',
  styleUrls: ['./clearing-house-preferences.component.scss'],
  standalone: true,
  imports: [SharedModule, NgIf, NgFor, AgGridModule],
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class ClearingHousePreferencesComponent implements OnInit, OnDestroy {
  private readonly service = inject(InstitutionClearingHousePreferencesService);
  private readonly fb = inject(FormBuilder);
  private readonly cdr = inject(ChangeDetectorRef);
  private readonly zone = inject(NgZone);
  private readonly financialInstitutionsApi = inject(FinancialInstitutionsApiService);
  private readonly clearingHouseApi = inject(ClearingHousesApiService);

  preferences: InstitutionClearingHousePreference[] = [];
  clearingHouses: ClearingHouseOption[] = [];
  loading = false;
  saving = false;
  editing: InstitutionClearingHousePreference | null = null;
  gridApi?: GridApi<InstitutionClearingHousePreference>;
  private readonly destroy$ = new Subject<void>();

  readonly institutions$ = this.financialInstitutionsApi.getAll().pipe(
    map((list) =>
      (list ?? [])
        .filter((item) => item.status === FinancialInstitutionStatusEnum.Active)
        .sort((a, b) => a.name.localeCompare(b.name))
    )
  );

  readonly priorityOptions: { value: number; label: string }[] = [
    { value: 1, label: '1 - Máxima prioridad' },
    { value: 2, label: '2 - Alta prioridad' },
    { value: 3, label: '3 - Prioridad media' },
    { value: 4, label: '4 - Prioridad baja' },
    { value: 5, label: '5 - Prioridad mínima' }
  ];

  readonly columnDefs: ColDef<InstitutionClearingHousePreference>[] = [
    { field: 'financialInstitutionName', headerName: 'Institución', flex: 1.2, filter: 'agTextColumnFilter' },
    { field: 'clearingHouseName', headerName: 'Cámara compensadora', flex: 1, filter: 'agTextColumnFilter' },
    {
      field: 'priority',
      headerName: 'Prioridad',
      maxWidth: 200,
      filter: 'agTextColumnFilter',
      valueFormatter: (params) => this.mapPriorityLabel(params.value)
    },
    {
      field: 'isDefault',
      headerName: 'Predeterminada',
      maxWidth: 180,
      cellRenderer: (params) => {
        const pill = document.createElement('span');
        pill.classList.add('pill');
        if (params.value) {
          pill.classList.add('success');
          pill.innerText = 'Sí';
        } else {
          pill.classList.add('muted');
          pill.innerText = 'No';
        }
        return pill;
      }
    },
    {
      field: 'isActive',
      headerName: 'Estado',
      maxWidth: 140,
      cellRenderer: (params) => {
        const pill = document.createElement('span');
        pill.classList.add('pill');
        pill.innerText = params.value ? 'Activa' : 'Inactiva';
        pill.classList.add(params.value ? 'success' : 'muted');
        return pill;
      }
    },
    {
      headerName: 'Acciones',
      colId: 'actions',
      maxWidth: 240,
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

        const toggle = document.createElement('button');
        toggle.type = 'button';
        toggle.classList.add('link');
        toggle.innerText = params.data?.isActive ? 'Inactivar' : 'Activar';
        toggle.addEventListener('click', () => {
          this.zone.run(() => params.context?.componentParent?.toggleActive(params.data));
        });

        const remove = document.createElement('button');
        remove.type = 'button';
        remove.classList.add('link', 'danger');
        remove.innerText = 'Eliminar';
        remove.addEventListener('click', () => {
          this.zone.run(() => params.context?.componentParent?.deletePreference(params.data));
        });

        container.append(edit);
        container.append(toggle);
        container.append(remove);
        return container;
      }
    }
  ];

  readonly defaultColDef: ColDef<InstitutionClearingHousePreference> = {
    resizable: true,
    sortable: true,
    suppressHeaderKeyboardEvent: () => true,
    filterParams: { suppressAndOrCondition: true }
  };

  readonly noRowsTemplate = 'No hay preferencias registradas.';
  readonly loadingTemplate = 'Cargando preferencias...';

  form = this.fb.nonNullable.group({
    priority: [1, [Validators.required]],
    isDefault: [false],
    isActive: [true]
  });

  createForm = this.fb.nonNullable.group({
    financialInstitutionId: [null as number | null, [Validators.required, Validators.min(1)]],
    clearingHouseId: [null as number | null, [Validators.required]],
    priority: [1, [Validators.required]],
    isDefault: [false],
    isActive: [true]
  });

  ngOnInit(): void {
    this.loadCatalogs();
    this.loadPreferences();
  }

  ngOnDestroy(): void {
    this.destroy$.next();
    this.destroy$.complete();
  }

  onGridReady(event: GridReadyEvent<InstitutionClearingHousePreference>): void {
    this.gridApi = event.api;
    this.updateGridOverlays();
  }

  loadPreferences(): void {
    this.loading = true;
    this.updateGridOverlays();
    this.service
      .list()
      .pipe(
        takeUntil(this.destroy$),
        finalize(() => {
          this.loading = false;
          this.cdr.markForCheck();
        })
      )
      .subscribe((data) => {
        this.preferences = data;
        this.updateGridOverlays();
        if (this.editing) {
          const updated = data.find((pref) => pref.id === this.editing?.id);
          if (updated) {
            this.startEdit(updated, false);
          }
        } else if (data.length) {
          this.startEdit(data[0], false);
        }
      });
  }

  loadCatalogs(): void {
    this.clearingHouseApi
      .list()
      .pipe(takeUntil(this.destroy$))
      .subscribe((houses) => {
        this.clearingHouses = houses;
        this.cdr.markForCheck();
      });
  }

  startCreate(): void {
    this.editing = null;
    this.form.reset({ priority: 1, isDefault: false, isActive: true });
    this.createForm.reset({
      financialInstitutionId: '',
      clearingHouseId: null,
      priority: 1,
      isDefault: false,
      isActive: true
    });
    this.cdr.markForCheck();
  }

  startEdit(preference: InstitutionClearingHousePreference, markForCheck = true): void {
    this.editing = preference;
    this.ensurePriorityOption(preference.priority);
    this.form.reset({
      priority: preference.priority,
      isDefault: preference.isDefault,
      isActive: preference.isActive
    });
    this.createForm.reset({
      financialInstitutionId: '',
      clearingHouseId: null,
      priority: 1,
      isDefault: false,
      isActive: true
    });
    if (markForCheck) {
      this.cdr.markForCheck();
    }
  }

  cancelEdit(): void {
    this.editing = null;
    this.form.reset({ priority: 1, isDefault: false, isActive: true });
    this.createForm.reset({
      financialInstitutionId: '',
      clearingHouseId: null,
      priority: 1,
      isDefault: false,
      isActive: true
    });
    this.cdr.markForCheck();
  }

  save(): void {
    if (!this.editing) {
      return;
    }

    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    const payload = {
      id: this.editing.id,
      ...this.form.getRawValue()
    };

    this.saving = true;
    this.service
      .update(this.editing.id, payload)
      .pipe(
        finalize(() => {
          this.saving = false;
          this.cdr.markForCheck();
        }),
        takeUntil(this.destroy$)
      )
      .subscribe((updated) => {
        this.preferences = this.preferences.map((pref) => (pref.id === updated.id ? updated : pref));
        this.startEdit(updated, false);
      });
  }

  create(): void {
    if (this.createForm.invalid) {
      this.createForm.markAllAsTouched();
      return;
    }

    const raw = this.createForm.getRawValue();
    const payload = {
      ...raw,
      financialInstitutionId: Number(raw.financialInstitutionId)
    };

    this.saving = true;
    this.service
      .create(payload)
      .pipe(
        finalize(() => {
          this.saving = false;
          this.cdr.markForCheck();
        }),
        takeUntil(this.destroy$)
      )
      .subscribe((created) => {
        this.preferences = [...this.preferences, created];
        this.updateGridOverlays();
        this.startEdit(created, false);
      });
  }

  toggleActive(preference: InstitutionClearingHousePreference): void {
    const payload = { ...preference, isActive: !preference.isActive };
    this.saving = true;
    this.service
      .update(preference.id, payload)
      .pipe(
        finalize(() => {
          this.saving = false;
          this.cdr.markForCheck();
        }),
        takeUntil(this.destroy$)
      )
      .subscribe((updated) => {
        this.preferences = this.preferences.map((pref) => (pref.id === updated.id ? updated : pref));
        this.startEdit(updated, false);
      });
  }

  deletePreference(preference: InstitutionClearingHousePreference): void {
    this.saving = true;
    this.service
      .delete(preference.id)
      .pipe(
        finalize(() => {
          this.saving = false;
          this.cdr.markForCheck();
        }),
        takeUntil(this.destroy$)
      )
      .subscribe(() => {
        this.preferences = this.preferences.filter((pref) => pref.id !== preference.id);
        if (this.editing?.id === preference.id) {
          this.editing = null;
        }
        this.updateGridOverlays();
      });
  }

  private updateGridOverlays(): void {
    if (!this.gridApi) {
      return;
    }

    if (this.loading) {
      this.gridApi.showLoadingOverlay();
      return;
    }

    if (!this.preferences.length) {
      this.gridApi.showNoRowsOverlay();
    } else {
      this.gridApi.hideOverlay();
    }
  }

  private mapPriorityLabel(value: number): string {
    const option = this.priorityOptions.find((opt) => opt.value === value);
    if (option) {
      return option.label;
    }
    return `Prioridad ${value}`;
  }

  private ensurePriorityOption(value: number): void {
    if (!this.priorityOptions.some((opt) => opt.value === value)) {
      this.priorityOptions.push({ value, label: `Prioridad ${value}` });
    }
  }
}

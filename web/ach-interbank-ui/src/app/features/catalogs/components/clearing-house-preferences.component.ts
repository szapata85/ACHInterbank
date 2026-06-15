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
import { CellClickedEvent, ColDef, GridApi } from 'ag-grid-community';
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
  imports: [SharedModule, NgIf, NgFor],
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
  showCreateForm = false;
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
    { value: 1, label: 'Alta' },
    { value: 2, label: 'Normal' },
    { value: 3, label: 'Baja' }
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
      sortable: false,
      filter: false,
      cellRenderer: (params) => {
        const toggleLabel = params.data?.isActive ? 'Inactivar' : 'Activar';
        return `
          <div class="row-actions">
            <button type="button" class="link" data-action="edit">Editar</button>
            <button type="button" class="link" data-action="toggle">${toggleLabel}</button>
            <button type="button" class="link danger" data-action="delete">Eliminar</button>
          </div>
        `;
      },
      onCellClicked: (params) => this.handleActionClick(params)
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

  onGridReady(api: GridApi<InstitutionClearingHousePreference>): void {
    this.gridApi = api;
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
    this.showCreateForm = true;
    this.editing = null;
    this.form.reset({ priority: 1, isDefault: false, isActive: true });
    this.createForm.reset({
      financialInstitutionId: null,
      clearingHouseId: null,
      priority: 1,
      isDefault: false,
      isActive: true
    });
    this.cdr.markForCheck();
  }

  startEdit(preference: InstitutionClearingHousePreference, markForCheck = true): void {
    this.showCreateForm = false;
    this.editing = preference;
    this.form.reset({
      priority: this.normalizePriority(preference.priority),
      isDefault: preference.isDefault,
      isActive: preference.isActive
    });
    this.createForm.reset({
      financialInstitutionId: null,
      clearingHouseId: null,
      priority: 1,
      isDefault: false,
      isActive: true
    });
    if (markForCheck) {
      this.cdr.markForCheck();
    }
    this.cdr.detectChanges();
  }

  cancelEdit(): void {
    this.editing = null;
    this.showCreateForm = false;
    this.form.reset({ priority: 1, isDefault: false, isActive: true });
    this.createForm.reset({
      financialInstitutionId: null,
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
    const normalized = this.normalizePriority(value);
    const option = this.priorityOptions.find((opt) => opt.value === normalized);
    return option?.label ?? "Normal";
  }

  private handleActionClick(params: CellClickedEvent<InstitutionClearingHousePreference>): void {
    const event = params.event as MouseEvent | undefined;
    const target = event?.target as HTMLElement | null;
    const button = target?.closest('button[data-action]') as HTMLButtonElement | null;
    const preference = params.data;

    if (!button || !preference) {
      return;
    }

    event?.preventDefault();
    event?.stopPropagation();

    const action = button.dataset['action'];

    this.zone.run(() => {
      if (action === 'edit') {
        this.startEdit(preference);
        return;
      }

      if (action === 'toggle') {
        this.toggleActive(preference);
        return;
      }

      if (action === 'delete') {
        this.deletePreference(preference);
      }
    });
  }

  private normalizePriority(value: number): number {
    if (value <= 1) {
      return 1;
    }

    if (value >= 3) {
      return 3;
    }

    return 2;
  }
}

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
import { MatButtonModule } from '@angular/material/button';
import { MatCardModule } from '@angular/material/card';
import { MatCheckboxModule } from '@angular/material/checkbox';
import { MatDialog, MatDialogModule } from '@angular/material/dialog';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatProgressBarModule } from '@angular/material/progress-bar';
import { MatSelectModule } from '@angular/material/select';
import { CellClickedEvent, ColDef, ICellRendererParams } from 'ag-grid-community';
import { FormBuilder, Validators } from '@angular/forms';
import { forkJoin, Subject } from 'rxjs';
import { filter, finalize, takeUntil } from 'rxjs/operators';
import { ClearingHousesApiService } from '../../ach-cycles/services/ach-cycles-api.service';
import { ClearingHouseOption } from '../../ach-cycles/models/ach-cycle.model';
import { NotificationService } from '../../../core/services/notification.service';
import { SharedModule } from '../../../shared/shared.module';
import { InstitutionClearingHousePreference } from '../models/institution-clearing-house-preference.model';
import { InstitutionClearingHousePreferencesService } from '../services/institution-clearing-house-preferences.service';
import { FinancialInstitutionsApiService } from '../../transactions/services/financial-institutions-api.service';
import { DestinationInstitution } from '../../transactions/transactions.models';
import { FinancialInstitutionStatusEnum } from '../../transactions/transactions.types';
import {
  CatalogActionConfirmDialogComponent,
  CatalogActionConfirmDialogData
} from './catalog-action-confirm-dialog.component';
import { catalogErrorMessage } from './catalog-error-message.util';

@Component({
  selector: 'app-clearing-house-preferences',
  templateUrl: './clearing-house-preferences.component.html',
  styleUrls: ['./clearing-house-preferences.component.scss'],
  standalone: true,
  imports: [
    SharedModule,
    NgIf,
    NgFor,
    MatButtonModule,
    MatCardModule,
    MatCheckboxModule,
    MatDialogModule,
    MatFormFieldModule,
    MatProgressBarModule,
    MatSelectModule
  ],
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class ClearingHousePreferencesComponent implements OnInit, OnDestroy {
  private readonly service = inject(InstitutionClearingHousePreferencesService);
  private readonly fb = inject(FormBuilder);
  private readonly cdr = inject(ChangeDetectorRef);
  private readonly zone = inject(NgZone);
  private readonly financialInstitutionsApi = inject(FinancialInstitutionsApiService);
  private readonly clearingHouseApi = inject(ClearingHousesApiService);
  private readonly dialog = inject(MatDialog);
  private readonly notifications = inject(NotificationService);

  preferences: InstitutionClearingHousePreference[] = [];
  institutions: DestinationInstitution[] = [];
  clearingHouses: ClearingHouseOption[] = [];
  loading = false;
  loadError = false;
  loadErrorMessage = 'No fue posible cargar las preferencias.';
  catalogsLoading = false;
  catalogsError: string | null = null;
  saving = false;
  showCreateForm = false;
  editing: InstitutionClearingHousePreference | null = null;
  operationError: string | null = null;
  successMessage: string | null = null;
  readonly pageSize = 10;
  private readonly destroy$ = new Subject<void>();

  readonly priorityOptions: { value: number; label: string }[] = [
    { value: 1, label: 'Alta' },
    { value: 2, label: 'Normal' },
    { value: 3, label: 'Baja' }
  ];

  readonly columnDefs: ColDef<InstitutionClearingHousePreference>[] = [
    {
      field: 'financialInstitutionName',
      headerName: 'Institución',
      flex: 1.2,
      minWidth: 220,
      filter: 'agTextColumnFilter'
    },
    {
      field: 'clearingHouseName',
      headerName: 'Cámara compensadora',
      flex: 1,
      minWidth: 190,
      filter: 'agTextColumnFilter'
    },
    {
      field: 'priority',
      headerName: 'Prioridad',
      minWidth: 130,
      maxWidth: 200,
      filter: 'agTextColumnFilter',
      valueFormatter: (params) => this.mapPriorityLabel(params.value)
    },
    {
      field: 'isDefault',
      headerName: 'Predeterminada',
      minWidth: 150,
      maxWidth: 180,
      cellRenderer: (
        params: ICellRendererParams<InstitutionClearingHousePreference, boolean>
      ) => {
        const pill = document.createElement('span');
        pill.classList.add('catalog-pill');
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
      minWidth: 120,
      maxWidth: 140,
      cellRenderer: (
        params: ICellRendererParams<InstitutionClearingHousePreference, boolean>
      ) => {
        const pill = document.createElement('span');
        pill.classList.add('catalog-pill');
        pill.innerText = params.value ? 'Activa' : 'Inactiva';
        pill.classList.add(params.value ? 'success' : 'muted');
        return pill;
      }
    },
    {
      headerName: 'Acciones',
      colId: 'actions',
      minWidth: 240,
      maxWidth: 240,
      sortable: false,
      filter: false,
      cellRenderer: (params: ICellRendererParams<InstitutionClearingHousePreference>) => {
        const container = document.createElement('div');
        container.classList.add('catalog-row-actions');
        const toggleLabel = params.data?.isActive ? 'Inactivar' : 'Activar';
        const relationLabel = params.data
          ? `${params.data.financialInstitutionName} / ${params.data.clearingHouseName}`
          : 'relación';

        const actions: Array<{
          action: 'edit' | 'toggle' | 'delete';
          label: string;
          accessibleLabel: string;
          destructive?: boolean;
        }> = [
          {
            action: 'edit',
            label: 'Editar',
            accessibleLabel: `Editar ${relationLabel}`
          },
          {
            action: 'toggle',
            label: toggleLabel,
            accessibleLabel: `${toggleLabel} ${relationLabel}`
          },
          {
            action: 'delete',
            label: 'Eliminar',
            accessibleLabel: `Eliminar ${relationLabel}`,
            destructive: true
          }
        ];

        actions.forEach((action) => {
          const button = document.createElement('button');
          button.type = 'button';
          button.classList.add('link');
          if (action.destructive) {
            button.classList.add('danger');
          }
          button.dataset['action'] = action.action;
          button.textContent = action.label;
          button.setAttribute('aria-label', action.accessibleLabel);
          container.append(button);
        });

        return container;
      },
      onCellClicked: (params) => this.handleActionClick(params)
    }
  ];

  readonly defaultColDef: ColDef<InstitutionClearingHousePreference> = {
    resizable: true,
    sortable: true,
    filterParams: { suppressAndOrCondition: true }
  };

  form = this.fb.nonNullable.group({
    priority: [1, [Validators.required, Validators.min(1), Validators.max(3)]],
    isDefault: [false],
    isActive: [true]
  });

  createForm = this.fb.nonNullable.group({
    financialInstitutionId: [null as number | null, [Validators.required, Validators.min(1)]],
    clearingHouseId: [null as number | null, [Validators.required, Validators.min(1)]],
    priority: [1, [Validators.required, Validators.min(1), Validators.max(3)]],
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

  loadPreferences(): void {
    if (this.loading) {
      return;
    }

    this.loading = true;
    this.loadError = false;
    this.loadErrorMessage = 'No fue posible cargar las preferencias.';
    this.service
      .list()
      .pipe(
        takeUntil(this.destroy$),
        finalize(() => {
          this.loading = false;
          this.cdr.markForCheck();
        })
      )
      .subscribe({
        next: (data) => {
          this.preferences = data ?? [];
          if (this.editing) {
            const updated = this.preferences.find((pref) => pref.id === this.editing?.id);
            if (updated) {
              this.setEditing(updated, false);
            }
          }
        },
        error: (error: unknown) => {
          this.preferences = [];
          this.loadError = true;
          this.loadErrorMessage = catalogErrorMessage(
            error,
            'No fue posible cargar las preferencias.'
          );
          this.notifications.error(this.loadErrorMessage);
        }
      });
  }

  loadCatalogs(): void {
    if (this.catalogsLoading) {
      return;
    }

    this.catalogsLoading = true;
    this.catalogsError = null;
    forkJoin({
      institutions: this.financialInstitutionsApi.getAll(),
      clearingHouses: this.clearingHouseApi.listAdministrative()
    })
      .pipe(
        takeUntil(this.destroy$),
        finalize(() => {
          this.catalogsLoading = false;
          this.cdr.markForCheck();
        })
      )
      .subscribe({
        next: ({ institutions, clearingHouses }) => {
          this.institutions = (institutions ?? [])
            .filter((item) => item.status === FinancialInstitutionStatusEnum.Active)
            .sort((a, b) => a.name.localeCompare(b.name));
          this.clearingHouses = clearingHouses ?? [];
        },
        error: (error: unknown) => {
          this.institutions = [];
          this.clearingHouses = [];
          this.catalogsError = catalogErrorMessage(
            error,
            'No fue posible cargar las instituciones o cámaras disponibles.'
          );
          this.notifications.error(this.catalogsError);
        }
      });
  }

  startCreate(): void {
    if (this.saving) {
      return;
    }

    this.showCreateForm = true;
    this.editing = null;
    this.operationError = null;
    this.successMessage = null;
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
    if (this.saving) {
      return;
    }

    this.operationError = null;
    this.successMessage = null;
    this.setEditing(preference, markForCheck);
  }

  private setEditing(
    preference: InstitutionClearingHousePreference,
    markForCheck = true
  ): void {
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
  }

  cancelEdit(): void {
    if (this.saving) {
      return;
    }

    this.editing = null;
    this.showCreateForm = false;
    this.operationError = null;
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
    if (this.saving || !this.editing) {
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

    this.operationError = null;
    this.successMessage = null;
    this.saving = true;
    this.form.disable({ emitEvent: false });
    this.service
      .update(this.editing.id, payload)
      .pipe(
        finalize(() => {
          this.saving = false;
          this.form.enable({ emitEvent: false });
          this.cdr.markForCheck();
        }),
        takeUntil(this.destroy$)
      )
      .subscribe({
        next: (updated) => {
          this.preferences = this.preferences.map((pref) => (pref.id === updated.id ? updated : pref));
          this.setEditing(updated, false);
          this.successMessage = 'Preferencia actualizada correctamente.';
          this.notifications.success(this.successMessage);
        },
        error: (error: unknown) => {
          this.operationError = catalogErrorMessage(
            error,
            'No fue posible actualizar la preferencia.'
          );
          this.notifications.error(this.operationError);
        }
      });
  }

  create(): void {
    if (this.saving) {
      return;
    }

    if (this.createForm.invalid) {
      this.createForm.markAllAsTouched();
      return;
    }

    const raw = this.createForm.getRawValue();
    const payload = {
      ...raw,
      financialInstitutionId: Number(raw.financialInstitutionId),
      clearingHouseId: Number(raw.clearingHouseId)
    };

    this.operationError = null;
    this.successMessage = null;
    this.saving = true;
    this.createForm.disable({ emitEvent: false });
    this.service
      .create(payload)
      .pipe(
        finalize(() => {
          this.saving = false;
          this.createForm.enable({ emitEvent: false });
          this.cdr.markForCheck();
        }),
        takeUntil(this.destroy$)
      )
      .subscribe({
        next: (created) => {
          this.preferences = [...this.preferences, created];
          this.setEditing(created, false);
          this.successMessage = 'Relación creada correctamente.';
          this.notifications.success(this.successMessage);
        },
        error: (error: unknown) => {
          this.operationError = catalogErrorMessage(
            error,
            'No fue posible crear la relación.'
          );
          this.notifications.error(this.operationError);
        }
      });
  }

  toggleActive(preference: InstitutionClearingHousePreference): void {
    if (this.saving) {
      return;
    }

    const willActivate = !preference.isActive;
    const defaultWarning = preference.isDefault && !willActivate
      ? ' Esta relación está marcada como predeterminada.'
      : '';

    this.dialog
      .open<CatalogActionConfirmDialogComponent, CatalogActionConfirmDialogData, boolean>(
        CatalogActionConfirmDialogComponent,
        {
          width: '30rem',
          maxWidth: 'calc(100vw - 2rem)',
          restoreFocus: true,
          autoFocus: 'dialog',
          data: {
            title: willActivate ? 'Activar relación' : 'Inactivar relación',
            message:
              `¿Deseas ${willActivate ? 'activar' : 'inactivar'} la relación entre `
              + `“${preference.financialInstitutionName}” y “${preference.clearingHouseName}”?`
              + defaultWarning,
            confirmText: willActivate ? 'Activar' : 'Inactivar',
            destructive: !willActivate
          }
        }
      )
      .afterClosed()
      .pipe(
        filter((confirmed): confirmed is true => confirmed === true),
        takeUntil(this.destroy$)
      )
      .subscribe(() => this.updateActiveState(preference));
  }

  private updateActiveState(preference: InstitutionClearingHousePreference): void {
    if (this.saving) {
      return;
    }

    const payload = {
      id: preference.id,
      priority: preference.priority,
      isDefault: preference.isDefault,
      isActive: !preference.isActive
    };
    this.operationError = null;
    this.successMessage = null;
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
      .subscribe({
        next: (updated) => {
          this.preferences = this.preferences.map((pref) => (pref.id === updated.id ? updated : pref));
          if (this.editing?.id === updated.id) {
            this.setEditing(updated, false);
          }
          this.successMessage = updated.isActive
            ? 'Relación activada correctamente.'
            : 'Relación inactivada correctamente.';
          this.notifications.success(this.successMessage);
        },
        error: (error: unknown) => {
          this.operationError = catalogErrorMessage(
            error,
            'No fue posible cambiar el estado de la relación.'
          );
          this.notifications.error(this.operationError);
        }
      });
  }

  deletePreference(preference: InstitutionClearingHousePreference): void {
    if (this.saving) {
      return;
    }

    this.dialog
      .open<CatalogActionConfirmDialogComponent, CatalogActionConfirmDialogData, boolean>(
        CatalogActionConfirmDialogComponent,
        {
          width: '30rem',
          maxWidth: 'calc(100vw - 2rem)',
          restoreFocus: true,
          autoFocus: 'dialog',
          data: {
            title: 'Eliminar relación',
            message:
              `Eliminarás la relación entre “${preference.financialInstitutionName}” y `
              + `“${preference.clearingHouseName}”. Esta acción no se puede deshacer.`,
            confirmText: 'Eliminar',
            destructive: true
          }
        }
      )
      .afterClosed()
      .pipe(
        filter((confirmed): confirmed is true => confirmed === true),
        takeUntil(this.destroy$)
      )
      .subscribe(() => this.performDelete(preference));
  }

  private performDelete(preference: InstitutionClearingHousePreference): void {
    if (this.saving) {
      return;
    }

    this.operationError = null;
    this.successMessage = null;
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
      .subscribe({
        next: () => {
          this.preferences = this.preferences.filter((pref) => pref.id !== preference.id);
          if (this.editing?.id === preference.id) {
            this.editing = null;
          }
          this.successMessage = 'Relación eliminada correctamente.';
          this.notifications.success(this.successMessage);
        },
        error: (error: unknown) => {
          this.operationError = catalogErrorMessage(
            error,
            'No fue posible eliminar la relación.'
          );
          this.notifications.error(this.operationError);
        }
      });
  }

  private mapPriorityLabel(value: number): string {
    const normalized = this.normalizePriority(value);
    const option = this.priorityOptions.find((opt) => opt.value === normalized);
    return option?.label ?? "Normal";
  }

  private handleActionClick(params: CellClickedEvent<InstitutionClearingHousePreference>): void {
    if (this.saving) {
      return;
    }

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

  trackInstitution(_: number, institution: DestinationInstitution): number {
    return institution.id;
  }

  trackClearingHouse(_: number, clearingHouse: ClearingHouseOption): number {
    return clearingHouse.id;
  }

}

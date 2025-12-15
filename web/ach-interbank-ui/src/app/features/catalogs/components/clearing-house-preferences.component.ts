import { NgIf } from '@angular/common';
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
import { finalize, takeUntil } from 'rxjs/operators';
import { SharedModule } from '../../../shared/shared.module';
import { InstitutionClearingHousePreference } from '../models/institution-clearing-house-preference.model';
import { InstitutionClearingHousePreferencesService } from '../services/institution-clearing-house-preferences.service';

@Component({
  selector: 'app-clearing-house-preferences',
  templateUrl: './clearing-house-preferences.component.html',
  styleUrls: ['./clearing-house-preferences.component.scss'],
  standalone: true,
  imports: [SharedModule, NgIf, AgGridModule],
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class ClearingHousePreferencesComponent implements OnInit, OnDestroy {
  private readonly service = inject(InstitutionClearingHousePreferencesService);
  private readonly fb = inject(FormBuilder);
  private readonly cdr = inject(ChangeDetectorRef);
  private readonly zone = inject(NgZone);

  preferences: InstitutionClearingHousePreference[] = [];
  loading = false;
  saving = false;
  editing: InstitutionClearingHousePreference | null = null;
  gridApi?: GridApi<InstitutionClearingHousePreference>;
  private readonly destroy$ = new Subject<void>();

  readonly columnDefs: ColDef<InstitutionClearingHousePreference>[] = [
    { field: 'financialInstitutionName', headerName: 'Institución', flex: 1.2, filter: 'agTextColumnFilter' },
    { field: 'clearingHouseName', headerName: 'Cámara compensadora', flex: 1, filter: 'agTextColumnFilter' },
    { field: 'priority', headerName: 'Prioridad', maxWidth: 140, filter: 'agNumberColumnFilter' },
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
      headerName: 'Acciones',
      colId: 'actions',
      maxWidth: 160,
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

        container.append(edit);
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
    priority: [1, [Validators.required, Validators.min(1), Validators.max(99)]],
    isDefault: [false]
  });

  ngOnInit(): void {
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

  startEdit(preference: InstitutionClearingHousePreference, markForCheck = true): void {
    this.editing = preference;
    this.form.reset({
      priority: preference.priority,
      isDefault: preference.isDefault
    });
    if (markForCheck) {
      this.cdr.markForCheck();
    }
  }

  cancelEdit(): void {
    this.editing = null;
    this.form.reset({ priority: 1, isDefault: false });
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
}

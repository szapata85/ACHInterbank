import { NgFor, NgIf } from '@angular/common';
import { ChangeDetectionStrategy, ChangeDetectorRef, Component, OnInit, inject } from '@angular/core';
import { AgGridModule } from 'ag-grid-angular';
import { ColDef, GridApi, GridReadyEvent } from 'ag-grid-community';
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
  imports: [SharedModule, NgFor, NgIf, AgGridModule],
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
  gridApi?: GridApi<DestinationInstitution>;

  readonly columnDefs: ColDef<DestinationInstitution>[] = [
    { field: 'name', headerName: 'Nombre', flex: 1, filter: 'agTextColumnFilter' },
    { field: 'routingNumber', headerName: 'Routing', maxWidth: 140 },
    { field: 'transitCode', headerName: 'Transit', maxWidth: 120 },
    { field: 'checkDigit', headerName: 'Dígito', maxWidth: 120 },
    {
      field: 'isDefaultSource',
      headerName: 'Origen por defecto',
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
      field: 'status',
      headerName: 'Estado',
      maxWidth: 140,
      cellRenderer: (params) => {
        const pill = document.createElement('span');
        pill.classList.add('pill');
        if (params.value === FinancialInstitutionStatusEnum.Active) {
          pill.classList.add('success');
          pill.innerText = 'Activa';
        } else {
          pill.classList.add('warning');
          pill.innerText = 'Inactiva';
        }
        return pill;
      }
    },
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
          params.context?.componentParent?.startEdit(params.data);
        });

        const toggle = document.createElement('button');
        toggle.type = 'button';
        toggle.classList.add('link');
        toggle.classList.add('danger');
        toggle.innerText =
          params.data.status === FinancialInstitutionStatusEnum.Active ? 'Desactivar' : 'Activar';
        toggle.addEventListener('click', () => {
          params.context?.componentParent?.toggleStatus(params.data);
        });

        container.append(edit, toggle);
        return container;
      }
    }
  ];

  readonly defaultColDef: ColDef<DestinationInstitution> = {
    resizable: true,
    sortable: true,
    suppressHeaderKeyboardEvent: () => true,
    filterParams: { suppressAndOrCondition: true }
  };

  readonly noRowsTemplate = 'No hay instituciones registradas.';
  readonly loadingTemplate = 'Cargando instituciones...';

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

  onGridReady(event: GridReadyEvent<DestinationInstitution>): void {
    this.gridApi = event.api;
    this.updateGridOverlays();
  }

  loadInstitutions(): void {
    this.loading = true;
    this.updateGridOverlays();
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
        this.updateGridOverlays();
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

  private updateGridOverlays(): void {
    if (!this.gridApi) {
      return;
    }

    if (this.loading) {
      this.gridApi.showLoadingOverlay();
      return;
    }

    if (!this.institutions.length) {
      this.gridApi.showNoRowsOverlay();
    } else {
      this.gridApi.hideOverlay();
    }
  }
}

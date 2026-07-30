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
import { MatButtonModule } from '@angular/material/button';
import { MatCardModule } from '@angular/material/card';
import { MatCheckboxModule } from '@angular/material/checkbox';
import { MatDialog, MatDialogModule } from '@angular/material/dialog';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatProgressBarModule } from '@angular/material/progress-bar';
import { MatSelectModule } from '@angular/material/select';
import { ColDef, ICellRendererParams } from 'ag-grid-community';
import { FormBuilder, Validators } from '@angular/forms';
import { Subject } from 'rxjs';
import { filter, finalize, takeUntil } from 'rxjs/operators';
import { NotificationService } from '../../../core/services/notification.service';
import { SharedModule } from '../../../shared/shared.module';
import { DestinationInstitution } from '../../transactions/transactions.models';
import { FinancialInstitutionStatusEnum } from '../../transactions/transactions.types';
import {
  FinancialInstitutionAdminService,
  FinancialInstitutionPayload
} from '../services/financial-institution-admin.service';
import {
  CatalogActionConfirmDialogComponent,
  CatalogActionConfirmDialogData
} from './catalog-action-confirm-dialog.component';
import { catalogErrorMessage } from './catalog-error-message.util';

@Component({
  selector: 'app-financial-institutions',
  templateUrl: './financial-institutions.component.html',
  styleUrls: ['./financial-institutions.component.scss'],
  standalone: true,
  imports: [
    SharedModule,
    NgIf,
    MatButtonModule,
    MatCardModule,
    MatCheckboxModule,
    MatDialogModule,
    MatFormFieldModule,
    MatInputModule,
    MatProgressBarModule,
    MatSelectModule
  ],
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class FinancialInstitutionsComponent implements OnInit, OnDestroy {
  readonly statusEnum = FinancialInstitutionStatusEnum;
  readonly pageSizeOptions = [10, 25, 50];
  readonly pageSize = this.pageSizeOptions[0];

  private readonly service = inject(FinancialInstitutionAdminService);
  private readonly fb = inject(FormBuilder);
  private readonly cdr = inject(ChangeDetectorRef);
  private readonly zone = inject(NgZone);
  private readonly dialog = inject(MatDialog);
  private readonly notifications = inject(NotificationService);

  institutions: DestinationInstitution[] = [];
  loading = false;
  loadError = false;
  loadErrorMessage = 'No fue posible cargar las instituciones financieras.';
  saving = false;
  showForm = false;
  editing: DestinationInstitution | null = null;
  operationError: string | null = null;
  successMessage: string | null = null;
  private readonly destroy$ = new Subject<void>();

  readonly columnDefs: ColDef<DestinationInstitution>[] = [
    { field: 'name', headerName: 'Nombre', flex: 1, minWidth: 240, filter: 'agTextColumnFilter' },
    { field: 'routingNumber', headerName: 'Routing', minWidth: 120, maxWidth: 150 },
    { field: 'transitCode', headerName: 'Transit', minWidth: 110, maxWidth: 130 },
    { field: 'checkDigit', headerName: 'Dígito', minWidth: 105, maxWidth: 125 },
    {
      field: 'isDefaultSource',
      headerName: 'Origen por defecto',
      minWidth: 170,
      maxWidth: 180,
      cellRenderer: (params: ICellRendererParams<DestinationInstitution, boolean>) => {
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
      field: 'status',
      headerName: 'Estado',
      minWidth: 130,
      maxWidth: 140,
      cellRenderer: (
        params: ICellRendererParams<DestinationInstitution, FinancialInstitutionStatusEnum>
      ) => {
        const pill = document.createElement('span');
        pill.classList.add('catalog-pill');
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
      minWidth: 210,
      maxWidth: 230,
      sortable: false,
      filter: false,
      floatingFilter: false,
      cellRenderer: (params: ICellRendererParams<DestinationInstitution>) => {
        const container = document.createElement('div');
        container.classList.add('catalog-row-actions');

        const edit = document.createElement('button');
        edit.type = 'button';
        edit.classList.add('link');
        edit.innerText = 'Editar';
        edit.setAttribute('aria-label', `Editar ${params.data?.name ?? 'institución'}`);
        edit.addEventListener('click', () => {
          this.zone.run(() => {
            if (params.data) {
              this.startEdit(params.data);
            }
          });
        });

        const toggle = document.createElement('button');
        toggle.type = 'button';
        toggle.classList.add('link');
        toggle.classList.add('danger');
        toggle.innerText =
          params.data?.status === FinancialInstitutionStatusEnum.Active ? 'Desactivar' : 'Activar';
        toggle.setAttribute(
          'aria-label',
          `${toggle.innerText} ${params.data?.name ?? 'institución'}`
        );
        toggle.addEventListener('click', () => {
          this.zone.run(() => {
            if (params.data) {
              this.toggleStatus(params.data);
            }
          });
        });

        container.append(edit, toggle);
        return container;
      }
    }
  ];

  readonly defaultColDef: ColDef<DestinationInstitution> = {
    resizable: true,
    sortable: true,
    filterParams: { suppressAndOrCondition: true }
  };

  form = this.fb.nonNullable.group({
    name: ['', [Validators.required, Validators.pattern(/\S/), Validators.maxLength(100)]],
    routingNumber: [
      '',
      [Validators.required, Validators.pattern(/^\d+$/), Validators.maxLength(9)]
    ],
    transitCode: [
      '',
      [Validators.required, Validators.pattern(/^\d+$/), Validators.maxLength(4)]
    ],
    checkDigit: [
      '',
      [Validators.required, Validators.pattern(/^\d$/), Validators.maxLength(1)]
    ],
    isDefaultSource: [false],
    status: [FinancialInstitutionStatusEnum.Active, Validators.required]
  });

  ngOnInit(): void {
    this.loadInstitutions();
    this.setupCheckDigitAutoCalc();
  }

  ngOnDestroy(): void {
    this.destroy$.next();
    this.destroy$.complete();
  }

  loadInstitutions(): void {
    if (this.loading) {
      return;
    }

    this.loading = true;
    this.loadError = false;
    this.loadErrorMessage = 'No fue posible cargar las instituciones financieras.';
    this.service
      .list(true)
      .pipe(
        takeUntil(this.destroy$),
        finalize(() => {
          this.loading = false;
          this.cdr.markForCheck();
        })
      )
      .subscribe({
        next: (data) => {
          this.institutions = data ?? [];
        },
        error: (error: unknown) => {
          this.institutions = [];
          this.loadError = true;
          this.loadErrorMessage = catalogErrorMessage(
            error,
            'No fue posible cargar las instituciones financieras.'
          );
          this.notifications.error(this.loadErrorMessage);
        }
      });
  }

  startCreate(): void {
    if (this.saving) {
      return;
    }

    this.editing = null;
    this.showForm = true;
    this.operationError = null;
    this.successMessage = null;
    this.form.reset({
      name: '',
      routingNumber: '',
      transitCode: '',
      checkDigit: '',
      isDefaultSource: false,
      status: FinancialInstitutionStatusEnum.Active
    });
    this.updateCheckDigit();
    this.cdr.markForCheck();
  }

  startEdit(item: DestinationInstitution): void {
    if (this.saving) {
      return;
    }

    this.editing = item;
    this.showForm = true;
    this.operationError = null;
    this.successMessage = null;
    this.form.reset({
      name: item.name,
      routingNumber: item.routingNumber,
      transitCode: item.transitCode,
      checkDigit: item.checkDigit,
      isDefaultSource: item.isDefaultSource,
      status: item.status
    });
    this.updateCheckDigit();
    this.cdr.markForCheck();
  }

  cancelEdit(): void {
    if (this.saving) {
      return;
    }

    this.resetEditor();
  }

  private resetEditor(): void {
    this.showForm = false;
    this.editing = null;
    this.operationError = null;
    this.form.reset({
      name: '',
      routingNumber: '',
      transitCode: '',
      checkDigit: '',
      isDefaultSource: false,
      status: FinancialInstitutionStatusEnum.Active
    });
    this.updateCheckDigit();
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

    const raw = this.form.getRawValue();
    const payload: FinancialInstitutionPayload = {
      ...raw,
      name: raw.name.trim(),
      routingNumber: raw.routingNumber.trim(),
      transitCode: raw.transitCode.trim(),
      checkDigit: raw.checkDigit.trim()
    };
    const wasEditing = !!this.editing;
    this.operationError = null;
    this.successMessage = null;
    this.saving = true;
    this.form.disable({ emitEvent: false });

    const request$ = this.editing
      ? this.service.update(this.editing.id, payload)
      : this.service.create(payload);

    request$
      .pipe(
        finalize(() => {
          this.saving = false;
          this.form.enable({ emitEvent: false });
          this.cdr.markForCheck();
        }),
        takeUntil(this.destroy$)
      )
      .subscribe({
        next: () => {
          this.resetEditor();
          this.successMessage = wasEditing
            ? 'Institución actualizada correctamente.'
            : 'Institución creada correctamente.';
          this.notifications.success(this.successMessage);
          this.loadInstitutions();
        },
        error: (error: unknown) => {
          this.operationError = catalogErrorMessage(
            error,
            wasEditing
              ? 'No fue posible actualizar la institución.'
              : 'No fue posible crear la institución.'
          );
          this.notifications.error(this.operationError);
        }
      });
  }

  toggleStatus(item: DestinationInstitution): void {
    if (this.saving) {
      return;
    }

    const nextStatus =
      item.status === FinancialInstitutionStatusEnum.Active
        ? FinancialInstitutionStatusEnum.Inactive
        : FinancialInstitutionStatusEnum.Active;
    const action = nextStatus === FinancialInstitutionStatusEnum.Active ? 'activar' : 'desactivar';
    const defaultSourceWarning = item.isDefaultSource && nextStatus === FinancialInstitutionStatusEnum.Inactive
      ? ' Esta institución es actualmente el origen por defecto.'
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
            title: `${action === 'activar' ? 'Activar' : 'Desactivar'} institución`,
            message: `¿Deseas ${action} “${item.name}”?${defaultSourceWarning}`,
            confirmText: action === 'activar' ? 'Activar' : 'Desactivar',
            destructive: action === 'desactivar'
          }
        }
      )
      .afterClosed()
      .pipe(
        filter((confirmed): confirmed is true => confirmed === true),
        takeUntil(this.destroy$)
      )
      .subscribe(() => this.updateStatus(item, nextStatus));
  }

  private updateStatus(
    item: DestinationInstitution,
    nextStatus: FinancialInstitutionStatusEnum
  ): void {
    if (this.saving) {
      return;
    }

    this.operationError = null;
    this.successMessage = null;
    this.saving = true;
    this.service
      .setStatus(item.id, nextStatus)
      .pipe(
        finalize(() => {
          this.saving = false;
          this.cdr.markForCheck();
        }),
        takeUntil(this.destroy$)
      )
      .subscribe({
        next: () => {
          this.successMessage =
            nextStatus === FinancialInstitutionStatusEnum.Active
              ? 'Institución activada correctamente.'
              : 'Institución desactivada correctamente.';
          this.notifications.success(this.successMessage);
          this.loadInstitutions();
        },
        error: (error: unknown) => {
          this.operationError = catalogErrorMessage(
            error,
            'No fue posible cambiar el estado de la institución.'
          );
          this.notifications.error(this.operationError);
        }
      });
  }

  private setupCheckDigitAutoCalc(): void {
    const routingCtrl = this.form.controls.routingNumber;
    const transitCtrl = this.form.controls.transitCode;

    routingCtrl.valueChanges.pipe(takeUntil(this.destroy$)).subscribe(() => this.updateCheckDigit());
    transitCtrl.valueChanges.pipe(takeUntil(this.destroy$)).subscribe(() => this.updateCheckDigit());

    this.updateCheckDigit();
  }

  private updateCheckDigit(): void {
    const routing = (this.form.controls.routingNumber.value ?? '').trim();
    const transit = (this.form.controls.transitCode.value ?? '').trim();
    const ruta = `${routing}${transit}`;
    const unchangedPersistedRoute =
      !!this.editing
      && routing === this.editing.routingNumber.trim()
      && transit === this.editing.transitCode.trim();

    if (unchangedPersistedRoute) {
      this.form.controls.checkDigit.setValue(this.editing?.checkDigit.trim() ?? '', {
        emitEvent: false
      });
      return;
    }

    if (ruta.length !== 8 || /\D/.test(ruta)) {
      this.form.controls.checkDigit.setValue('', { emitEvent: false });
      return;
    }

    const pesos = [3, 7, 1, 3, 7, 1, 3, 7];
    const suma = ruta
      .split('')
      .map((char, index) => (Number.isNaN(+char) ? 0 : +char) * pesos[index])
      .reduce((acc, val) => acc + val, 0);

    const proximoMultiplo10 = Math.ceil(suma / 10) * 10;
    const digitoChequeo = proximoMultiplo10 - suma;
    const value = digitoChequeo === 10 ? '0' : digitoChequeo.toString();

    this.form.controls.checkDigit.setValue(value, { emitEvent: false });
  }

}

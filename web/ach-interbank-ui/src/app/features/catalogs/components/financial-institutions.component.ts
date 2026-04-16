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
import { ColDef } from 'ag-grid-community';
import { FormBuilder, Validators } from '@angular/forms';
import { Subject } from 'rxjs';
import { finalize, takeUntil } from 'rxjs/operators';
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
  imports: [SharedModule, NgIf],
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

  institutions: DestinationInstitution[] = [];
  loading = false;
  saving = false;
  showForm = false;
  editing: DestinationInstitution | null = null;
  private readonly destroy$ = new Subject<void>();

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
          this.zone.run(() => {
            params.context?.componentParent?.startEdit(params.data);
          });
        });

        const toggle = document.createElement('button');
        toggle.type = 'button';
        toggle.classList.add('link');
        toggle.classList.add('danger');
        toggle.innerText =
          params.data.status === FinancialInstitutionStatusEnum.Active ? 'Desactivar' : 'Activar';
        toggle.addEventListener('click', () => {
          this.zone.run(() => {
            params.context?.componentParent?.toggleStatus(params.data);
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
    suppressHeaderKeyboardEvent: () => true,
    filterParams: { suppressAndOrCondition: true }
  };

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
    this.setupCheckDigitAutoCalc();
  }

  ngOnDestroy(): void {
    this.destroy$.next();
    this.destroy$.complete();
  }

  loadInstitutions(): void {
    this.loading = true;
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
    this.updateCheckDigit();
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
    this.updateCheckDigit();
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
    this.updateCheckDigit();
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

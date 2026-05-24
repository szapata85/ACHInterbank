import { ChangeDetectionStrategy, ChangeDetectorRef, Component, OnDestroy, OnInit, inject } from '@angular/core';
import { FormBuilder } from '@angular/forms';
import { Subject } from 'rxjs';
import { finalize, takeUntil } from 'rxjs/operators';
import { NotificationService } from '../../../core/services/notification.service';
import { SharedModule } from '../../../shared/shared.module';
import { CustomerThirdPartyFilters, CustomerThirdPartyRow, CustomerThirdPartyStatus } from '../models/customer-third-party.model';
import { CustomerThirdPartiesService } from '../services/customer-third-parties.service';

interface CustomerThirdPartyTableRow extends CustomerThirdPartyRow {
  validationReceivedAtDisplay: string;
}

@Component({
  selector: 'app-customer-third-parties',
  standalone: true,
  imports: [SharedModule],
  templateUrl: './customer-third-parties.component.html',
  styleUrls: ['./customer-third-parties.component.css'],
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class CustomerThirdPartiesComponent implements OnInit, OnDestroy {
  private readonly service = inject(CustomerThirdPartiesService);
  private readonly fb = inject(FormBuilder);
  private readonly cdr = inject(ChangeDetectorRef);
  private readonly notifications = inject(NotificationService);
  private readonly destroy$ = new Subject<void>();

  readonly columns = [
    { key: 'customerName', label: 'Cliente' },
    { key: 'destinationInstitutionName', label: 'Entidad destino' },
    { key: 'destinationAccountNumber', label: 'Cuenta destino' },
    { key: 'recipientIdNumber', label: 'Documento receptor' },
    { key: 'status', label: 'Estado' },
    { key: 'prenotificationTransactionId', label: 'ID prenotificación' },
    { key: 'validationCycleId', label: 'Ciclo validación' },
    { key: 'validationReceivedAtDisplay', label: 'Fecha validación' },
    { key: 'validationMessage', label: 'Observación' }
  ];

  readonly statusOptions: Array<{ value: '' | CustomerThirdPartyStatus; label: string }> = [
    { value: '', label: 'Todos' },
    { value: 'Pending', label: 'Pendiente' },
    { value: 'Active', label: 'Aprobado' },
    { value: 'Rejected', label: 'Rechazado' }
  ];

  filterForm = this.fb.nonNullable.group({
    search: [''],
    destinationAccountNumber: [''],
    recipientIdNumber: [''],
    status: ['']
  });

  rows: CustomerThirdPartyTableRow[] = [];
  loading = false;
  loadError = false;
  hasSearched = false;
  page = 1;
  pageSize = 20;
  total = 0;

  ngOnInit(): void {
    this.search();
  }

  ngOnDestroy(): void {
    this.destroy$.next();
    this.destroy$.complete();
  }

  search(page = 1): void {
    const { search, destinationAccountNumber, recipientIdNumber, status } = this.filterForm.getRawValue();

    const filters: CustomerThirdPartyFilters = {
      search: search || null,
      destinationAccountNumber: destinationAccountNumber || null,
      recipientIdNumber: recipientIdNumber || null,
      status: (status as CustomerThirdPartyStatus) || null,
      page,
      pageSize: this.pageSize
    };

    this.loading = true;
    this.loadError = false;
    this.hasSearched = true;
    this.cdr.markForCheck();

    this.service
      .search(filters)
      .pipe(
        takeUntil(this.destroy$),
        finalize(() => {
          this.loading = false;
          this.cdr.markForCheck();
        })
      )
      .subscribe({
        next: (response) => {
          this.rows = (response.items ?? []).map((item) => ({
            ...item,
            validationReceivedAtDisplay: this.formatDate(item.validationReceivedAt)
          }));
          this.total = response.total;
          this.page = response.page;
        },
        error: () => {
          this.rows = [];
          this.total = 0;
          this.loadError = true;
          this.notifications.error('No fue posible consultar terceros de prenotificación.');
        }
      });
  }

  updateStatus(row: CustomerThirdPartyRow, status: CustomerThirdPartyStatus): void {
    const validationMessage = status === 'Rejected' ? window.prompt('Indique motivo de rechazo (opcional):') : null;

    this.service
      .updateStatus(row.id, { status, validationMessage })
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: () => {
          this.notifications.success('Estado actualizado correctamente.');
          this.search(this.page);
        },
        error: () => {
          this.notifications.error('No fue posible actualizar el estado.');
        }
      });
  }

  clear(): void {
    this.filterForm.reset({
      search: '',
      destinationAccountNumber: '',
      recipientIdNumber: '',
      status: ''
    });
    this.rows = [];
    this.total = 0;
    this.page = 1;
    this.hasSearched = false;
    this.cdr.markForCheck();
  }

  onPageChange(page: number): void {
    this.search(page);
  }

  private formatDate(value?: string | null): string {
    if (!value) {
      return '-';
    }

    const date = new Date(value);
    return Number.isNaN(date.getTime()) ? value : date.toLocaleString();
  }
}

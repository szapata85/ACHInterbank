import { ChangeDetectionStrategy, ChangeDetectorRef, Component, ElementRef, OnDestroy, OnInit, ViewChild, inject } from '@angular/core';
import { FormBuilder, FormControl, Validators } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MatChipsModule } from '@angular/material/chips';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatIconModule } from '@angular/material/icon';
import { MatInputModule } from '@angular/material/input';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatSelectModule } from '@angular/material/select';
import { MatTooltipModule } from '@angular/material/tooltip';
import { Subject } from 'rxjs';
import { finalize, takeUntil } from 'rxjs/operators';
import { NotificationService } from '../../../core/services/notification.service';
import { SharedModule } from '../../../shared/shared.module';
import { CustomerThirdPartyFilters, CustomerThirdPartyRow, CustomerThirdPartyStatus } from '../models/customer-third-party.model';
import { CustomerThirdPartiesService } from '../services/customer-third-parties.service';

interface CustomerThirdPartyTableRow extends CustomerThirdPartyRow {
  statusLabel: string;
  statusHelp: string;
  validationReceivedAtDisplay: string;
}

type StatusFilter = '' | CustomerThirdPartyStatus;

@Component({
  selector: 'app-customer-third-parties',
  standalone: true,
  imports: [
    SharedModule,
    MatButtonModule,
    MatChipsModule,
    MatFormFieldModule,
    MatIconModule,
    MatInputModule,
    MatProgressSpinnerModule,
    MatSelectModule,
    MatTooltipModule
  ],
  templateUrl: './customer-third-parties.component.html',
  styleUrls: ['./customer-third-parties.component.css'],
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class CustomerThirdPartiesComponent implements OnInit, OnDestroy {
  @ViewChild('filterFormElement') private filterFormElement?: ElementRef<HTMLFormElement>;

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
    { key: 'statusLabel', label: 'Estado' },
    { key: 'prenotificationTransactionId', label: 'ID prenotificación' },
    { key: 'validationCycleId', label: 'Ciclo validación' },
    { key: 'validationReceivedAtDisplay', label: 'Fecha validación' },
    { key: 'validationMessage', label: 'Resultado automático' }
  ];

  readonly statusOptions: Array<{ value: StatusFilter; label: string }> = [
    { value: '', label: 'Todos los estados' },
    { value: 'Pending', label: 'Pendiente' },
    { value: 'Active', label: 'Aprobada' },
    { value: 'Rejected', label: 'Rechazada' }
  ];

  readonly filterForm = this.fb.nonNullable.group({
    search: ['', [Validators.maxLength(100)]],
    destinationAccountNumber: ['', [Validators.maxLength(50), Validators.pattern(/^[0-9]*$/)]],
    recipientIdNumber: ['', [Validators.maxLength(50), Validators.pattern(/^[A-Za-z0-9-]*$/)]],
    status: new FormControl<StatusFilter>('', { nonNullable: true })
  });

  rows: CustomerThirdPartyTableRow[] = [];
  selectedRow: CustomerThirdPartyTableRow | null = null;
  loading = false;
  loadError = false;
  searchAttempted = false;
  page = 1;
  readonly pageSize = 20;
  total = 0;

  ngOnInit(): void {
    this.search();
  }

  ngOnDestroy(): void {
    this.destroy$.next();
    this.destroy$.complete();
  }

  search(page = 1): void {
    if (this.loading) {
      return;
    }

    this.searchAttempted = true;
    if (this.filterForm.invalid) {
      this.filterForm.markAllAsTouched();
      this.focusFirstInvalidControl();
      return;
    }

    const values = this.filterForm.getRawValue();
    const filters: CustomerThirdPartyFilters = {
      search: this.normalizeOptional(values.search),
      destinationAccountNumber: this.normalizeOptional(values.destinationAccountNumber),
      recipientIdNumber: this.normalizeOptional(values.recipientIdNumber),
      status: values.status || null,
      page,
      pageSize: this.pageSize
    };

    this.loading = true;
    this.loadError = false;
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
          this.rows = (response.items ?? []).map(item => this.toTableRow(item));
          this.total = response.total;
          this.page = response.page;
          this.selectedRow = this.selectedRow
            ? this.rows.find(row => row.id === this.selectedRow?.id) ?? null
            : null;
        },
        error: () => {
          this.rows = [];
          this.total = 0;
          this.selectedRow = null;
          this.loadError = true;
          this.notifications.error('No fue posible consultar los terceros y sus prenotificaciones.');
        }
      });
  }

  clear(): void {
    if (this.loading) {
      return;
    }

    this.filterForm.reset({
      search: '',
      destinationAccountNumber: '',
      recipientIdNumber: '',
      status: ''
    });
    this.filterForm.markAsPristine();
    this.filterForm.markAsUntouched();
    this.searchAttempted = false;
    this.selectedRow = null;
    this.page = 1;
    this.search(1);
  }

  showDetail(row: CustomerThirdPartyTableRow): void {
    this.selectedRow = row;
    this.cdr.markForCheck();
  }

  closeDetail(): void {
    this.selectedRow = null;
    this.cdr.markForCheck();
  }

  shouldShowError(controlName: 'search' | 'destinationAccountNumber' | 'recipientIdNumber'): boolean {
    const control = this.filterForm.controls[controlName];
    return control.invalid && (control.touched || control.dirty || this.searchAttempted);
  }

  onPageChange(page: number): void {
    this.search(page);
  }

  statusClass(status: CustomerThirdPartyStatus): string {
    return `status-${status.toLowerCase()}`;
  }

  private toTableRow(item: CustomerThirdPartyRow): CustomerThirdPartyTableRow {
    const status = this.describeStatus(item.status, item.validationMessage);
    return {
      ...item,
      statusLabel: status.label,
      statusHelp: status.help,
      validationReceivedAtDisplay: this.formatDate(item.validationReceivedAt)
    };
  }

  private describeStatus(status: CustomerThirdPartyStatus, message?: string | null): { label: string; help: string } {
    if (status === 'Active') {
      return {
        label: 'Aprobada',
        help: message || 'Confirmada mediante procesamiento automático NACHA-M.'
      };
    }

    if (status === 'Rejected') {
      return {
        label: 'Rechazada',
        help: message || 'Rechazada mediante respuesta automática NACHA-M.'
      };
    }

    return {
      label: 'Pendiente',
      help: 'En espera de respuesta NACHA-M o del vencimiento del plazo normativo aplicable.'
    };
  }

  private focusFirstInvalidControl(): void {
    const invalid = this.filterFormElement?.nativeElement.querySelector<HTMLElement>(
      'input.ng-invalid, mat-select.ng-invalid'
    );
    invalid?.focus();
  }

  private normalizeOptional(value: string): string | null {
    const normalized = value.trim();
    return normalized.length > 0 ? normalized : null;
  }

  private formatDate(value?: string | null): string {
    if (!value) {
      return 'Sin respuesta';
    }

    const date = new Date(value);
    return Number.isNaN(date.getTime()) ? value : date.toLocaleString('es-CO');
  }
}

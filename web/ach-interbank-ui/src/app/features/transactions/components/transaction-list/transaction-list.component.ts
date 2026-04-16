import { ChangeDetectionStrategy, ChangeDetectorRef, Component, OnInit, inject } from '@angular/core';
import { FormBuilder } from '@angular/forms';
import { Router, RouterModule } from '@angular/router';
import { ColDef, RowSelectionOptions } from 'ag-grid-community';
import { SharedModule } from '../../../../shared/shared.module';
import { TransactionsApiService } from '../../services/transactions-api.service';
import { TransactionListItem } from '../../transactions.models';
import { NotificationService } from '../../../../core/services/notification.service';
import { TransactionTypeEnum } from '../../transactions.types';
import { AchCyclesApiService, ClearingHousesApiService } from '../../../ach-cycles/services/ach-cycles-api.service';
import { AchCycleSummary, ClearingHouseOption } from '../../../ach-cycles/models/ach-cycle.model';

type TransactionListRow = TransactionListItem & {
  typeLabel: string;
  transactionNatureLabel: string;
  returnManagementLabel: string;
  amountText: string;
  effectiveEntryDateText: string;
};

interface TransactionGroup {
  batchId: number;
  batchLabel: string;
  batchDate: string;
  items: TransactionListRow[];
}

interface AchCycleOption {
  id: string;
  label: string;
  name: string;
}

interface ColumnOption {
  field: keyof TransactionListRow;
  label: string;
}

@Component({
  selector: 'app-transaction-list',
  standalone: true,
  imports: [SharedModule, RouterModule],
  templateUrl: './transaction-list.component.html',
  styleUrls: ['./transaction-list.component.scss'],
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class TransactionListComponent implements OnInit {
  private readonly fb = inject(FormBuilder);
  private readonly api = inject(TransactionsApiService);
  private readonly achCyclesApi = inject(AchCyclesApiService);
  private readonly clearingHousesApi = inject(ClearingHousesApiService);
  private readonly notifications = inject(NotificationService);
  private readonly router = inject(Router);
  private readonly cdr = inject(ChangeDetectorRef);
  private readonly currencyFormatter = new Intl.NumberFormat('es-CO', {
    style: 'currency',
    currency: 'COP'
  });
  private readonly dateFormatter = new Intl.DateTimeFormat('es-CO', {
    timeZone: 'UTC',
    year: 'numeric',
    month: '2-digit',
    day: '2-digit'
  });

  private readonly allColumnDefs: ColDef<TransactionListRow>[] = [
    { field: 'id', headerName: 'ID', width: 90, maxWidth: 120, sortable: true },
    { field: 'transactionExternalId', headerName: 'ID operación', flex: 1, sortable: true, filter: 'agTextColumnFilter' },
    { field: 'reference', headerName: 'Referencia legado', flex: 1, sortable: true, filter: 'agTextColumnFilter' },
    { field: 'typeLabel', headerName: 'Tipo', width: 160, filter: 'agSetColumnFilter' },
    { field: 'transactionNatureLabel', headerName: 'Naturaleza', width: 170, filter: 'agSetColumnFilter' },
    { field: 'returnManagementLabel', headerName: 'Gestión devolución', width: 220, filter: 'agSetColumnFilter' },
    { field: 'achCycleName', headerName: 'Ciclo', width: 200, filter: 'agTextColumnFilter' },
    { field: 'clearingHouseName', headerName: 'Cámara', width: 200, filter: 'agTextColumnFilter' },
    { field: 'amountText', headerName: 'Monto', width: 140, maxWidth: 180, cellClass: 'text-end' },
    { field: 'sourceAccountNumber', headerName: 'Cuenta origen', filter: 'agTextColumnFilter' },
    { field: 'destinationAccountNumber', headerName: 'Cuenta destino', filter: 'agTextColumnFilter' },
    { field: 'destinationInstitutionName', headerName: 'Institución destino', filter: 'agTextColumnFilter', flex: 1 },
    { field: 'effectiveEntryDateText', headerName: 'Fecha efectiva', width: 160 }
  ];

  readonly columnOptions: ColumnOption[] = [
    { field: 'transactionExternalId', label: 'ID operación' },
    { field: 'reference', label: 'Referencia legado' },
    { field: 'typeLabel', label: 'Tipo' },
    { field: 'transactionNatureLabel', label: 'Naturaleza' },
    { field: 'returnManagementLabel', label: 'Gestión devolución' },
    { field: 'achCycleName', label: 'Ciclo' },
    { field: 'clearingHouseName', label: 'Cámara' },
    { field: 'amountText', label: 'Monto' },
    { field: 'sourceAccountNumber', label: 'Cuenta origen' },
    { field: 'destinationAccountNumber', label: 'Cuenta destino' },
    { field: 'destinationInstitutionName', label: 'Institución destino' },
    { field: 'effectiveEntryDateText', label: 'Fecha efectiva' }
  ];

  readonly mandatoryColumnFields = new Set<keyof TransactionListRow>(['id']);
  visibleColumnFields = new Set<keyof TransactionListRow>([
    'id',
    'transactionExternalId',
    'reference',
    'transactionNatureLabel',
    'typeLabel',
    'returnManagementLabel',
    'amountText',
    'sourceAccountNumber',
    'destinationAccountNumber',
    'effectiveEntryDateText'
  ]);

  get columnDefs(): ColDef<TransactionListRow>[] {
    return this.allColumnDefs.filter((column) => {
      const field = column.field as keyof TransactionListRow | undefined;
      return !field || this.visibleColumnFields.has(field);
    });
  }

  readonly defaultColDef: ColDef<TransactionListRow> = {
    resizable: true,
    sortable: true,
    suppressHeaderKeyboardEvent: () => true,
    filterParams: { suppressAndOrCondition: true }
  };

  readonly rowSelection = {
    mode: 'singleRow'
  } satisfies RowSelectionOptions;

  readonly noRowsTemplate = 'No hay transacciones registradas.';
  readonly loadingTemplate = 'Cargando transacciones...';

  loading = false;
  returnView: 'all' | 'received' | 'sent' = 'all';
  groups: TransactionGroup[] = [];
  cycles: AchCycleOption[] = [];
  clearingHouses: ClearingHouseOption[] = [];
  readonly filtrosForm = this.fb.group({
    selectedCycleId: [null as string | null],
    selectedClearingHouseId: [null as number | null],
    selectedDate: ['']
  });

  ngOnInit(): void {
    this.filtrosForm.controls.selectedClearingHouseId.valueChanges.subscribe(() => this.onClearingHouseChange());
    this.loadClearingHouses();
    this.loadCycles();
  }

  createNew(): void {
    this.router.navigate(['/transactions/create']);
  }

  createBulk(): void {
    this.router.navigate(['/transactions/bulk-ingestion/upload']);
  }

  openBulkTracking(): void {
    this.router.navigate(['/transactions/bulk-ingestion/tracking']);
  }

  applyFilters(): void {
    this.loadTransactions();
  }

  onClearingHouseChange(): void {
    this.filtrosForm.patchValue({ selectedCycleId: null }, { emitEvent: false });
    this.loadCycles(false);
  }

  onDateChange(): void {
    this.filtrosForm.patchValue({ selectedCycleId: null }, { emitEvent: false });
    this.loadCycles(false);
  }

  isColumnVisible(field: keyof TransactionListRow): boolean {
    return this.visibleColumnFields.has(field);
  }

  canToggleColumn(field: keyof TransactionListRow): boolean {
    return !this.mandatoryColumnFields.has(field);
  }

  toggleColumn(field: keyof TransactionListRow, checked: boolean): void {
    if (!this.canToggleColumn(field)) {
      return;
    }

    if (checked) {
      this.visibleColumnFields.add(field);
      this.cdr.markForCheck();
      return;
    }

    const optionalVisibleCount = Array.from(this.visibleColumnFields)
      .filter((visibleField) => !this.mandatoryColumnFields.has(visibleField)).length;

    if (optionalVisibleCount <= 1) {
      this.notifications.warning('Debe quedar al menos una columna visible además del ID.');
      return;
    }

    this.visibleColumnFields.delete(field);
    this.cdr.markForCheck();
  }

  private loadClearingHouses(): void {
    this.clearingHousesApi.list().subscribe({
      next: (items) => {
        this.clearingHouses = items ?? [];
        this.cdr.markForCheck();
      },
      error: () => {
        this.notifications.error('No fue posible cargar las cámaras compensadoras');
      }
    });
  }

  private loadCycles(autoLoadTransactions = true): void {
    this.achCyclesApi
      .search({
        page: 1,
        pageSize: 100,
        clearingHouseId: this.filtrosForm.controls.selectedClearingHouseId.value ?? undefined,
        startDate: this.filtrosForm.controls.selectedDate.value || undefined,
        endDate: this.filtrosForm.controls.selectedDate.value || undefined
      })
      .subscribe({
        next: (response) => {
          const mapped = (response?.items ?? []).map((cycle) => this.mapCycleOption(cycle));
          const distinct = this.distinctCycles(mapped);
          const selectedClearingHouseId = this.filtrosForm.controls.selectedClearingHouseId.value;
          this.cycles = selectedClearingHouseId == null
            ? distinct.map((option) => ({ ...option, id: option.name }))
            : distinct;

          if (!this.filtrosForm.controls.selectedCycleId.value && this.cycles.length > 0) {
            this.filtrosForm.patchValue({ selectedCycleId: this.cycles[0].id }, { emitEvent: false });
          }

          if (this.cycles.length === 0) {
            this.filtrosForm.patchValue({ selectedCycleId: null }, { emitEvent: false });
          }

          if (autoLoadTransactions) {
            this.loadTransactions();
          }

          this.cdr.markForCheck();
        },
        error: () => {
          this.notifications.error('No fue posible cargar los ciclos ACH');
          this.cdr.markForCheck();
        }
      });
  }

  private loadTransactions(): void {
    this.loading = true;
    this.cdr.markForCheck();
    const selectedClearingHouseId = this.filtrosForm.controls.selectedClearingHouseId.value;
    const selectedCycleId = this.filtrosForm.controls.selectedCycleId.value;
    const selectedDate = this.filtrosForm.controls.selectedDate.value;
    const useCycleName = selectedClearingHouseId == null;
    this.api
      .getAll({
        achCycleId: useCycleName ? null : selectedCycleId,
        achCycleName: useCycleName ? selectedCycleId : null,
        effectiveDate: selectedDate || undefined,
        clearingHouseId: selectedClearingHouseId ?? undefined
      })
      .subscribe({
        next: (items) => {
          const normalized = (items ?? []).map((item) => this.mapRow(item));
          const filtered = normalized.filter((item) => this.matchesReturnView(item));
          const grouped = new Map<number, TransactionGroup>();

          filtered.forEach((item) => {
            const current = grouped.get(item.achBatchId);
            if (!current) {
              grouped.set(item.achBatchId, {
                batchId: item.achBatchId,
                batchLabel: this.buildBatchLabel(item),
                batchDate: this.formatDate(item.effectiveEntryDate),
                items: [item]
              });
            } else {
              current.items.push(item);
            }
          });

          this.groups = Array.from(grouped.values()).sort((a, b) => b.batchId - a.batchId);
          this.cdr.markForCheck();
        },
        error: () => {
          this.notifications.error('No fue posible cargar las transacciones');
          this.loading = false;
          this.cdr.markForCheck();
        },
        complete: () => {
          this.loading = false;
          this.cdr.markForCheck();
        }
      });
  }

  private mapCycleOption(cycle: AchCycleSummary): AchCycleOption {
    const id = cycle.id?.trim();
    const name = cycle.cycleName?.trim() || `Ciclo ${id}`;

    return { id, label: name, name };
  }

  private distinctCycles(options: AchCycleOption[]): AchCycleOption[] {
    const seen = new Set<string>();

    return options.filter((option) => {
      const key = option.name.toLocaleLowerCase();
      if (seen.has(key)) {
        return false;
      }

      seen.add(key);
      return true;
    });
  }

  private mapRow(item: TransactionListItem): TransactionListRow {
    return {
      ...item,
      typeLabel: this.formatType(item.type),
      transactionNatureLabel: this.formatTransactionNature(item.isPrenotification),
      returnManagementLabel: this.formatReturnManagement(item.transactionCode),
      amountText: this.currencyFormatter.format(item.amount ?? 0),
      effectiveEntryDateText: this.formatDate(item.effectiveEntryDate)
    };
  }

  setReturnView(view: 'all' | 'received' | 'sent'): void {
    this.returnView = view;
    this.loadTransactions();
  }

  private matchesReturnView(item: TransactionListRow): boolean {
    if (this.returnView === 'all') {
      return true;
    }

    const code = (item.transactionCode ?? '').trim();
    const receivedCodes = new Set(['21', '31', '51']);
    const sentCodes = new Set(['26', '36', '56']);

    return this.returnView === 'received' ? receivedCodes.has(code) : sentCodes.has(code);
  }

  private formatReturnManagement(code: string | null | undefined): string {
    const normalized = (code ?? '').trim();
    if (['21', '31', '51'].includes(normalized)) {
      return 'Devolución recibida';
    }

    if (['26', '36', '56'].includes(normalized)) {
      return 'Devolución enviada';
    }

    return 'No devolución';
  }

  private formatDate(value: string | null | undefined): string {
    if (!value) {
      return '-';
    }

    const date = new Date(value);
    return isNaN(date.getTime()) ? value : this.dateFormatter.format(date);
  }

  private formatType(type: TransactionTypeEnum): string {
    if (type === TransactionTypeEnum.Credit) {
      return 'Crédito';
    }

    if (type === TransactionTypeEnum.Debit) {
      return 'Débito';
    }

    if (type === TransactionTypeEnum.Prenotification) {
      return 'Prenotificación';
    }

    if (type === TransactionTypeEnum.Reversal) {
      return 'Reverso';
    }

    if (type === TransactionTypeEnum.Return) {
      return 'Devolución';
    }

    return 'Desconocido';
  }

  private formatTransactionNature(isPrenotification: boolean): string {
    return isPrenotification ? 'Prenotificación' : 'Transacción';
  }

  private buildBatchLabel(item: TransactionListItem): string {
    const sequence = item.batchSequenceNumber > 0 ? item.batchSequenceNumber : item.achBatchId;
    return `Lote ${sequence} · ${item.batchCompanyName || 'Sin compañía'}`;
  }
}

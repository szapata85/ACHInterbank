import { ChangeDetectionStrategy, ChangeDetectorRef, Component, OnInit, inject } from '@angular/core';
import { Router, RouterModule } from '@angular/router';
import { AgGridModule } from 'ag-grid-angular';
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

@Component({
  selector: 'app-transaction-list',
  standalone: true,
  imports: [SharedModule, RouterModule, AgGridModule],
  templateUrl: './transaction-list.component.html',
  styleUrls: ['./transaction-list.component.scss'],
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class TransactionListComponent implements OnInit {
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

  readonly columnDefs: ColDef<TransactionListRow>[] = [
    { field: 'id', headerName: 'ID', width: 90, maxWidth: 120, sortable: true },
    { field: 'reference', headerName: 'Referencia', flex: 1, sortable: true, filter: 'agTextColumnFilter' },
    { field: 'typeLabel', headerName: 'Tipo', width: 160, filter: 'agSetColumnFilter' },
    { field: 'transactionNatureLabel', headerName: 'Naturaleza', width: 170, filter: 'agSetColumnFilter' },
    { field: 'achCycleName', headerName: 'Ciclo', width: 200, filter: 'agTextColumnFilter' },
    { field: 'clearingHouseName', headerName: 'Cámara', width: 200, filter: 'agTextColumnFilter' },
    { field: 'amountText', headerName: 'Monto', width: 140, maxWidth: 180, cellClass: 'text-end' },
    { field: 'sourceAccountNumber', headerName: 'Cuenta origen', filter: 'agTextColumnFilter' },
    { field: 'destinationAccountNumber', headerName: 'Cuenta destino', filter: 'agTextColumnFilter' },
    { field: 'destinationInstitutionName', headerName: 'Institución destino', filter: 'agTextColumnFilter', flex: 1 },
    { field: 'effectiveEntryDateText', headerName: 'Fecha efectiva', width: 160 }
  ];

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
  groups: TransactionGroup[] = [];
  cycles: AchCycleOption[] = [];
  clearingHouses: ClearingHouseOption[] = [];
  selectedCycleId: string | null = null;
  selectedClearingHouseId: number | null = null;
  selectedDate = '';

  ngOnInit(): void {
    this.loadClearingHouses();
    this.loadCycles();
  }

  createNew(): void {
    this.router.navigate(['/transactions/create']);
  }

  applyFilters(): void {
    this.loadTransactions();
  }

  onClearingHouseChange(): void {
    this.selectedCycleId = null;
    this.loadCycles(false);
  }

  onDateChange(): void {
    this.selectedCycleId = null;
    this.loadCycles(false);
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
        clearingHouseId: this.selectedClearingHouseId ?? undefined,
        startDate: this.selectedDate || undefined,
        endDate: this.selectedDate || undefined
      })
      .subscribe({
        next: (response) => {
          const mapped = (response?.items ?? []).map((cycle) => this.mapCycleOption(cycle));
          const distinct = this.distinctCycles(mapped);
          this.cycles = this.selectedClearingHouseId == null
            ? distinct.map((option) => ({ ...option, id: option.name }))
            : distinct;

          if (!this.selectedCycleId && this.cycles.length > 0) {
            this.selectedCycleId = this.cycles[0].id;
          }

          if (this.cycles.length === 0) {
            this.selectedCycleId = null;
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
    const useCycleName = this.selectedClearingHouseId == null;
    this.api
      .getAll({
        achCycleId: useCycleName ? null : this.selectedCycleId,
        achCycleName: useCycleName ? this.selectedCycleId : null,
        effectiveDate: this.selectedDate || undefined,
        clearingHouseId: this.selectedClearingHouseId ?? undefined
      })
      .subscribe({
        next: (items) => {
          const normalized = (items ?? []).map((item) => this.mapRow(item));
          const grouped = new Map<number, TransactionGroup>();

          normalized.forEach((item) => {
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
      amountText: this.currencyFormatter.format(item.amount ?? 0),
      effectiveEntryDateText: this.formatDate(item.effectiveEntryDate)
    };
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

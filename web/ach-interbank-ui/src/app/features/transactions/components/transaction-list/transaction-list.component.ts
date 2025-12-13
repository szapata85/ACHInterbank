import { ChangeDetectionStrategy, Component, OnInit, inject } from '@angular/core';
import { Router, RouterModule } from '@angular/router';
import { SharedModule } from '../../../../shared/shared.module';
import { TransactionsApiService } from '../../services/transactions-api.service';
import { TransactionListItem } from '../../transactions.models';
import { NotificationService } from '../../../../core/services/notification.service';
import { TransactionTypeEnum } from '../../transactions.types';
import { TableColumn } from '../../../../shared/components/table.component';
import { AchCyclesApiService, ClearingHousesApiService } from '../../../ach-cycles/services/ach-cycles-api.service';
import { AchCycleSummary, ClearingHouseOption } from '../../../ach-cycles/models/ach-cycle.model';

type TransactionListRow = TransactionListItem & {
  typeLabel: string;
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
  id: number;
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
  private readonly api = inject(TransactionsApiService);
  private readonly achCyclesApi = inject(AchCyclesApiService);
  private readonly clearingHousesApi = inject(ClearingHousesApiService);
  private readonly notifications = inject(NotificationService);
  private readonly router = inject(Router);
  private readonly currencyFormatter = new Intl.NumberFormat('es-CO', {
    style: 'currency',
    currency: 'COP'
  });
  private readonly dateFormatter = new Intl.DateTimeFormat('es-CO', {
    timeZone: 'America/Bogota',
    year: 'numeric',
    month: '2-digit',
    day: '2-digit'
  });

  readonly columns: TableColumn[] = [
    { key: 'id', label: 'ID', width: '80px' },
    { key: 'reference', label: 'Referencia' },
    { key: 'typeLabel', label: 'Tipo' },
    { key: 'amountText', label: 'Monto', align: 'end' },
    { key: 'sourceAccountNumber', label: 'Cuenta origen' },
    { key: 'destinationAccountNumber', label: 'Cuenta destino' },
    { key: 'destinationInstitutionName', label: 'Institución destino' },
    { key: 'effectiveEntryDateText', label: 'Fecha efectiva' }
  ];

  loading = false;
  groups: TransactionGroup[] = [];
  cycles: AchCycleOption[] = [];
  clearingHouses: ClearingHouseOption[] = [];
  selectedCycleId: number | null = null;
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
        date: this.selectedDate || undefined
      })
      .subscribe({
        next: (response) => {
          this.cycles = (response?.items ?? []).map((cycle) => this.mapCycleOption(cycle));

          if (!this.selectedCycleId && this.cycles.length > 0) {
            this.selectedCycleId = this.cycles[0].id;
          }

          if (this.cycles.length === 0) {
            this.selectedCycleId = null;
          }

          if (autoLoadTransactions) {
            this.loadTransactions();
          }
        },
        error: () => {
          this.notifications.error('No fue posible cargar los ciclos ACH');
        }
      });
  }

  private loadTransactions(): void {
    this.loading = true;
    this.api
      .getAll({
        achCycleId: this.selectedCycleId,
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
                batchDate: this.formatDate(item.batchEffectiveEntryDate),
                items: [item]
              });
            } else {
              current.items.push(item);
            }
          });

          this.groups = Array.from(grouped.values()).sort((a, b) => b.batchId - a.batchId);
        },
        error: () => {
          this.notifications.error('No fue posible cargar las transacciones');
        },
        complete: () => {
          this.loading = false;
        }
      });
  }

  private mapCycleOption(cycle: AchCycleSummary): AchCycleOption {
    const id = Number(cycle.id);
    const date = this.formatDate(cycle.date);
    const name = cycle.cycleName?.trim() || `Ciclo ${id}`;
    const label = `${name}${date !== '-' ? ' · ' + date : ''}`;

    return { id, label };
  }

  private mapRow(item: TransactionListItem): TransactionListRow {
    return {
      ...item,
      typeLabel: this.formatType(item.type),
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

  private buildBatchLabel(item: TransactionListItem): string {
    const sequence = item.batchSequenceNumber > 0 ? item.batchSequenceNumber : item.achBatchId;
    return `Lote ${sequence} · ${item.batchCompanyName || 'Sin compañía'}`;
  }
}

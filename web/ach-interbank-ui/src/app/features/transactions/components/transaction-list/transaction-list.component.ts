import { ChangeDetectionStrategy, Component, OnInit, inject } from '@angular/core';
import { Router, RouterModule } from '@angular/router';
import { SharedModule } from '../../../../shared/shared.module';
import { TransactionsApiService } from '../../services/transactions-api.service';
import { TransactionListItem } from '../../transactions.models';
import { NotificationService } from '../../../../core/services/notification.service';
import { TransactionTypeEnum } from '../../transactions.types';
import { TableColumn } from '../../../../shared/components/table.component';

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

  ngOnInit(): void {
    this.load();
  }

  createNew(): void {
    this.router.navigate(['/transactions/create']);
  }

  private load(): void {
    this.loading = true;
    this.api.getAll().subscribe({
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

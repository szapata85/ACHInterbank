import { CommonModule } from '@angular/common';
import { ChangeDetectionStrategy, Component, EventEmitter, Input, Output } from '@angular/core';
import { TransactionIntegrationResult, TransactionIntegrationResultItem } from '../../transactions.models';

@Component({
  selector: 'app-transaction-integration-result',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './transaction-integration-result.component.html',
  styleUrls: ['./transaction-integration-result.component.scss'],
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class TransactionIntegrationResultComponent {
  @Input({ required: true }) result!: TransactionIntegrationResult;
  @Output() closePanel = new EventEmitter<void>();

  get latest(): TransactionIntegrationResultItem | null {
    return this.result?.latest ?? null;
  }

  statusClass(item: TransactionIntegrationResultItem): string {
    if (item.transportStatus === 'Failed' || item.transportStatus === 'TimedOut') {
      return 'failed';
    }

    return item.businessStatus.toLocaleLowerCase();
  }

  resultLabel(item: TransactionIntegrationResultItem): string {
    if (item.transportStatus === 'Failed' || item.transportStatus === 'TimedOut') {
      return 'Error técnico';
    }

    const labels: Record<string, string> = {
      Success: 'Exitoso',
      Rejected: 'Rechazado',
      PendingCatalog: 'Pendiente de interpretación',
      ManualReview: 'Requiere revisión',
      Unknown: 'Desconocido'
    };

    return labels[item.businessStatus] ?? 'Desconocido';
  }
}

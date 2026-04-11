import { ChangeDetectionStrategy, Component, computed, inject, signal } from '@angular/core';
import { ActivatedRoute, RouterModule } from '@angular/router';
import { finalize, take } from 'rxjs';
import { NotificationService } from '../../../../core/services/notification.service';
import { SharedModule } from '../../../../shared/shared.module';
import {
  BulkBatchItemsPageDto,
  BulkBatchProcessingSummaryDto,
  BulkIngestionBatchStatus,
  BulkIngestionItemStatus,
  BulkIngestionRetryScope
} from '../../transactions.models';
import { BulkIngestionTrackingApiService } from '../../services/bulk-ingestion-tracking-api.service';

@Component({
  selector: 'app-bulk-ingestion-detail',
  standalone: true,
  imports: [SharedModule, RouterModule],
  templateUrl: './bulk-ingestion-detail.component.html',
  styleUrls: ['./bulk-ingestion-detail.component.scss'],
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class BulkIngestionDetailComponent {
  private readonly route = inject(ActivatedRoute);
  private readonly api = inject(BulkIngestionTrackingApiService);
  private readonly notifications = inject(NotificationService);

  readonly batchId = signal<string>('');
  readonly summary = signal<BulkBatchProcessingSummaryDto | null>(null);
  readonly itemsPage = signal<BulkBatchItemsPageDto | null>(null);
  readonly page = signal(1);
  readonly pageSize = signal(25);
  readonly itemStatusFilter = signal<BulkIngestionItemStatus | null>(null);
  readonly isLoading = signal(false);

  readonly totalPages = computed(() => {
    const total = this.itemsPage()?.total ?? 0;
    return Math.max(1, Math.ceil(total / this.pageSize()));
  });

  constructor() {
    const batchId = this.route.snapshot.paramMap.get('batchId') ?? '';
    this.batchId.set(batchId);
    if (batchId) {
      this.loadAll();
    }
  }

  loadAll(): void {
    const batchId = this.batchId();
    if (!batchId) {
      return;
    }

    this.isLoading.set(true);
    this.api.getSummary(batchId).pipe(take(1)).subscribe({
      next: (summary) => this.summary.set(summary),
      error: (error: Error) => this.notifications.error(error.message)
    });

    this.loadItems();
  }

  loadItems(): void {
    this.isLoading.set(true);
    this.api.getBatchItems(this.batchId(), this.page(), this.pageSize(), this.itemStatusFilter())
      .pipe(
        take(1),
        finalize(() => this.isLoading.set(false))
      )
      .subscribe({
        next: (page) => this.itemsPage.set(page),
        error: (error: Error) => this.notifications.error(error.message)
      });
  }

  changePage(offset: number): void {
    const next = this.page() + offset;
    if (next < 1 || next > this.totalPages()) {
      return;
    }

    this.page.set(next);
    this.loadItems();
  }

  applyStatusFilter(value: string): void {
    this.itemStatusFilter.set(value ? Number(value) as BulkIngestionItemStatus : null);
    this.page.set(1);
    this.loadItems();
  }

  retry(scope: BulkIngestionRetryScope): void {
    this.api.retry(this.batchId(), { scope }).pipe(take(1)).subscribe({
      next: () => {
        this.notifications.success('Reintento solicitado correctamente.');
        this.loadAll();
      },
      error: (error: Error) => this.notifications.error(error.message)
    });
  }

  statusLabel(status: BulkIngestionBatchStatus | BulkIngestionItemStatus): string {
    const map: Record<number, string> = {
      1: 'Cargado / Listo',
      2: 'Parseado / Error estructural',
      3: 'Validado / Error funcional',
      4: 'En cola / Procesado',
      5: 'Procesando',
      6: 'Parcialmente procesado',
      7: 'Completado',
      8: 'Fallido',
      9: 'Reintentando',
      10: 'Cancelado'
    };

    return map[status] ?? `Estado ${status}`;
  }

  canRetry(): boolean {
    const status = this.summary()?.status.status;
    return status === BulkIngestionBatchStatus.PartiallyProcessed || status === BulkIngestionBatchStatus.Failed;
  }
}

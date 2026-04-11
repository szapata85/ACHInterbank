import { ChangeDetectionStrategy, Component, computed, inject, signal } from '@angular/core';
import { Router, RouterModule } from '@angular/router';
import { forkJoin, of, take } from 'rxjs';
import { catchError } from 'rxjs/operators';
import { NotificationService } from '../../../../core/services/notification.service';
import { SharedModule } from '../../../../shared/shared.module';
import { BulkBatchStatusDto, BulkIngestionBatchStatus, BulkIngestionRetryScope } from '../../transactions.models';
import { BulkIngestionTrackingApiService } from '../../services/bulk-ingestion-tracking-api.service';

@Component({
  selector: 'app-bulk-ingestion-tracking',
  standalone: true,
  imports: [SharedModule, RouterModule],
  templateUrl: './bulk-ingestion-tracking.component.html',
  styleUrls: ['./bulk-ingestion-tracking.component.scss'],
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class BulkIngestionTrackingComponent {
  private readonly api = inject(BulkIngestionTrackingApiService);
  private readonly notifications = inject(NotificationService);
  private readonly router = inject(Router);

  readonly batchIdInput = signal('');
  readonly statusFilter = signal<string>('ALL');
  readonly isLoading = signal(false);
  readonly rows = signal<BulkBatchStatusDto[]>([]);

  readonly filteredRows = computed(() => {
    const selectedStatus = this.statusFilter();
    return this.rows().filter((row) => selectedStatus === 'ALL' || String(row.status) === selectedStatus);
  });

  readonly totals = computed(() => {
    const items = this.filteredRows();
    return {
      batches: items.length,
      records: items.reduce((acc, item) => acc + item.totalRecords, 0),
      succeeded: items.reduce((acc, item) => acc + item.totalSucceeded, 0),
      failed: items.reduce((acc, item) => acc + item.totalFailed, 0)
    };
  });

  constructor() {
    this.refreshFromRecent();
  }

  searchByBatchId(): void {
    const batchId = this.batchIdInput().trim();
    if (!batchId) {
      this.notifications.warning('Ingrese un Batch ID para buscar.');
      return;
    }

    this.isLoading.set(true);
    this.api.getBatch(batchId).pipe(take(1)).subscribe({
      next: (row) => {
        this.upsertRow(row);
        this.persistRecentIds();
        this.batchIdInput.set('');
        this.isLoading.set(false);
      },
      error: (error: Error) => {
        this.notifications.error(error.message);
        this.isLoading.set(false);
      }
    });
  }

  refreshFromRecent(): void {
    const ids = this.readRecentIds();
    if (!ids.length) {
      this.rows.set([]);
      return;
    }

    this.isLoading.set(true);
    forkJoin(ids.map((id) => this.api.getBatch(id).pipe(catchError(() => of(null))))).pipe(take(1)).subscribe((items) => {
      this.rows.set(items.filter((item): item is BulkBatchStatusDto => !!item));
      this.isLoading.set(false);
    });
  }

  retryFailed(batchId: string): void {
    this.api.retry(batchId, { scope: BulkIngestionRetryScope.FailedOnly }).pipe(take(1)).subscribe({
      next: () => {
        this.notifications.success('Reintento del lote solicitado.');
        this.refreshSingle(batchId);
      },
      error: (error: Error) => this.notifications.error(error.message)
    });
  }

  openDetail(batchId: string): void {
    this.router.navigate(['/transactions/bulk-ingestion', batchId]);
  }

  statusLabel(status: BulkIngestionBatchStatus): string {
    const map: Record<number, string> = {
      1: 'Cargado',
      2: 'Validado',
      3: 'En cola',
      4: 'Procesando',
      5: 'Completado',
      6: 'Completado con errores',
      7: 'Fallido'
    };

    return map[status] ?? `Estado ${status}`;
  }

  canRetry(status: BulkIngestionBatchStatus): boolean {
    return status === BulkIngestionBatchStatus.CompletedWithErrors || status === BulkIngestionBatchStatus.Failed;
  }

  private refreshSingle(batchId: string): void {
    this.api.getBatch(batchId).pipe(take(1)).subscribe({
      next: (row) => {
        this.upsertRow(row);
        this.persistRecentIds();
      }
    });
  }

  private upsertRow(row: BulkBatchStatusDto): void {
    const next = [...this.rows()];
    const index = next.findIndex((item) => item.batchId === row.batchId);
    if (index >= 0) {
      next[index] = row;
    } else {
      next.unshift(row);
    }

    this.rows.set(next.sort((a, b) => new Date(b.uploadedAtUtc).getTime() - new Date(a.uploadedAtUtc).getTime()));
  }

  private readRecentIds(): string[] {
    const raw = localStorage.getItem('ach.bulk.recentBatchIds');
    if (!raw) {
      return [];
    }

    try {
      const parsed = JSON.parse(raw);
      return Array.isArray(parsed) ? parsed.filter((id) => typeof id === 'string') : [];
    } catch {
      return [];
    }
  }

  private persistRecentIds(): void {
    const ids = this.rows().map((item) => item.batchId).slice(0, 25);
    localStorage.setItem('ach.bulk.recentBatchIds', JSON.stringify(ids));
  }
}

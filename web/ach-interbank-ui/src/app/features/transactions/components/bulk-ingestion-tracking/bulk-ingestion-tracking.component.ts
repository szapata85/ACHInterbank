import { ChangeDetectionStrategy, Component, computed, inject, signal } from '@angular/core';
import { FormBuilder, Validators } from '@angular/forms';
import { Router, RouterModule } from '@angular/router';
import { ColDef } from 'ag-grid-community';
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
  private readonly fb = inject(FormBuilder);
  private readonly api = inject(BulkIngestionTrackingApiService);
  private readonly notifications = inject(NotificationService);
  private readonly router = inject(Router);

  readonly batchIdInput = signal('');
  readonly statusFilter = signal<string>('ALL');
  readonly isLoading = signal(false);
  readonly rows = signal<BulkBatchStatusDto[]>([]);
  readonly filtrosForm = this.fb.group({
    batchId: [''],
    status: ['ALL', Validators.required]
  });
  readonly columnDefs: ColDef<BulkBatchStatusDto>[] = [
    {
      headerName: 'Referencia',
      minWidth: 260,
      valueGetter: (params) => `${params.data?.batchReference ?? '-'} (${params.data?.batchId ?? ''})`
    },
    { headerName: 'Estado', minWidth: 170, valueGetter: (params) => params.data ? this.statusLabel(params.data.status) : '' },
    { field: 'uploadedAtUtc', headerName: 'Fecha carga', minWidth: 180, valueFormatter: (params) => params.value ? new Date(params.value).toLocaleString('es-CO') : '-' },
    { field: 'totalRecords', headerName: 'Total', width: 110 },
    { field: 'progressPercent', headerName: 'Progreso', width: 120, valueFormatter: (params) => `${Number(params.value ?? 0).toFixed(2)}%` },
    { field: 'totalSucceeded', headerName: 'Éxitos', width: 110 },
    { field: 'totalFailed', headerName: 'Fallos', width: 110 },
    {
      headerName: 'Acciones',
      minWidth: 220,
      sortable: false,
      filter: false,
      cellRenderer: (params) => this.canRetry(params.data?.status as BulkIngestionBatchStatus)
        ? '<button class="btn btn-outline btn-grid" data-action="detail">Detalle</button> <button class="btn btn-secondary btn-grid" data-action="retry">Reintentar</button>'
        : '<button class="btn btn-outline btn-grid" data-action="detail">Detalle</button>',
      onCellClicked: (params) => {
        const target = params.event?.target as HTMLElement | null;
        const action = target?.getAttribute('data-action');
        if (action === 'detail') {
          this.openDetail(params.data!.batchId);
        }
        if (action === 'retry') {
          this.retryFailed(params.data!.batchId);
        }
      }
    }
  ];

  readonly filteredRows = computed(() => {
    const selectedStatus = this.filtrosForm.controls.status.value ?? 'ALL';
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
    this.filtrosForm.controls.status.valueChanges.subscribe((value) => {
      const normalized = value ?? 'ALL';
      this.statusFilter.set(normalized);
    });
    this.refreshFromRecent();
  }

  searchByBatchId(): void {
    const batchId = this.batchIdInput().trim();
    if (!batchId) {
      this.notifications.warning('Ingrese un identificador de lote para buscar.');
      return;
    }

    this.isLoading.set(true);
    this.api.getBatch(batchId).pipe(take(1)).subscribe({
      next: (row) => {
        this.upsertRow(row);
        this.persistRecentIds();
        this.batchIdInput.set('');
        this.filtrosForm.patchValue({ batchId: '' }, { emitEvent: false });
        this.isLoading.set(false);
      },
      error: (error: Error) => {
        this.notifications.error(error.message);
        this.isLoading.set(false);
      }
    });
  }

  onBatchIdInput(value: string): void {
    this.batchIdInput.set(value);
    this.filtrosForm.patchValue({ batchId: value }, { emitEvent: false });
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
    if (this.isLoading()) {
      return;
    }
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
      2: 'Parseado',
      3: 'Validado',
      4: 'En cola',
      5: 'Procesando',
      6: 'Parcialmente procesado',
      7: 'Completado',
      8: 'Fallido',
      9: 'Reintentando',
      10: 'Cancelado'
    };

    return map[status] ?? `Estado ${status}`;
  }

  canRetry(status: BulkIngestionBatchStatus): boolean {
    return status === BulkIngestionBatchStatus.PartiallyProcessed || status === BulkIngestionBatchStatus.Failed;
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

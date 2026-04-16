import { ChangeDetectionStrategy, ChangeDetectorRef, Component, computed, inject, signal } from '@angular/core';
import { RouterModule } from '@angular/router';
import { ColDef } from 'ag-grid-community';
import { take } from 'rxjs';
import { NotificationService } from '../../../../core/services/notification.service';
import { SharedModule } from '../../../../shared/shared.module';
import {
  BulkAchTransactionItemRequest,
  BulkAchTransactionItemResult,
  BulkAchTransactionRequest,
  BulkAchTransactionResponse,
  BulkIngestionProcessingMode,
  BulkIngestionSourceType
} from '../../transactions.models';
import { BulkIngestionApiService } from '../../services/bulk-ingestion-api.service';

@Component({
  selector: 'app-transaction-bulk-create',
  standalone: true,
  imports: [SharedModule, RouterModule],
  templateUrl: './transaction-bulk-create.component.html',
  styleUrls: ['./transaction-bulk-create.component.scss'],
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class TransactionBulkCreateComponent {
  private readonly ingestionApi = inject(BulkIngestionApiService);
  private readonly notifications = inject(NotificationService);
  private readonly cdr = inject(ChangeDetectorRef);

  readonly jsonInput = signal('');
  readonly batchReference = signal(this.buildDefaultBatchReference());
  readonly chunkSize = signal<number>(200);
  readonly isSubmitting = signal(false);
  readonly validationError = signal<string | null>(null);
  readonly payload = signal<BulkAchTransactionRequest | null>(null);
  readonly response = signal<BulkAchTransactionResponse | null>(null);
  readonly parseWarnings = signal<string[]>([]);
  readonly resultColumnDefs: ColDef<any>[] = [
    { headerName: '#', valueGetter: (params) => (params.data?.index ?? 0) + 1, width: 90 },
    { field: 'transactionExternalId', headerName: 'ID operación', valueGetter: (params) => params.data?.transactionExternalId || '-', minWidth: 150 },
    { field: 'reference', headerName: 'Referencia legado', valueGetter: (params) => params.data?.reference || '-', minWidth: 170 },
    { field: 'statusLabel', headerName: 'Estado', minWidth: 120 },
    { field: 'transactionId', headerName: 'ID transacción', valueGetter: (params) => params.data?.transactionId ?? '-', minWidth: 150 },
    { field: 'errorCode', headerName: 'Código error', valueGetter: (params) => params.data?.errorCode ?? '-', minWidth: 130 },
    { headerName: 'Detalle', valueGetter: (params) => this.formatError(params.data), minWidth: 260 }
  ];

  readonly transactionCount = computed(() => this.payload()?.transactions.length ?? 0);
  readonly summary = computed(() => {
    const result = this.response();
    if (!result) {
      return null;
    }

    const successRate = result.totalProcessed > 0
      ? Math.round((result.totalSucceeded / result.totalProcessed) * 100)
      : 0;

    return {
      successRate,
      hasPartialSuccess: result.totalSucceeded > 0 && result.totalFailed > 0,
      allSuccess: result.totalFailed === 0 && result.totalProcessed > 0,
      allFailed: result.totalSucceeded === 0 && result.totalProcessed > 0
    };
  });

  readonly resultRows = computed(() => {
    const rows = this.response()?.itemResults ?? [];
    return rows
      .slice()
      .sort((a, b) => a.index - b.index)
      .map((item) => ({
        ...item,
        statusLabel: item.succeeded ? 'Exitosa' : 'Fallida'
      }));
  });

  onJsonInput(value: string): void {
    this.jsonInput.set(value);
    this.validationError.set(null);
    this.response.set(null);
    this.parseWarnings.set([]);
  }

  validateJson(): void {
    this.response.set(null);

    const parsed = this.parseAndValidatePayload();
    if (!parsed) {
      this.payload.set(null);
      return;
    }

    this.payload.set(parsed.payload);
    this.parseWarnings.set(parsed.warnings);
  }

  submit(): void {
    if (this.isSubmitting()) {
      return;
    }
    const currentPayload = this.payload();
    if (!currentPayload) {
      this.validationError.set('Primero valida el JSON para poder enviar el lote.');
      return;
    }

    this.isSubmitting.set(true);
    this.validationError.set(null);

    this.ingestionApi.submit({
      batchReference: currentPayload.batchReference,
      sourceType: BulkIngestionSourceType.InlineTransactions,
      processingMode: BulkIngestionProcessingMode.Synchronous,
      chunkSize: currentPayload.chunkSize,
      transactions: currentPayload.transactions
    })
      .pipe(take(1))
      .subscribe({
        next: (submission) => {
          const result = submission.immediateResult;
          if (!result) {
            this.validationError.set('La plataforma aceptó la solicitud pero no entregó resultado síncrono.');
            this.isSubmitting.set(false);
            this.notifications.warning(this.validationError());
            this.cdr.markForCheck();
            return;
          }

          this.response.set(result);
          this.isSubmitting.set(false);

          if (result.totalFailed === 0) {
            this.notifications.success(`Lote procesado con éxito. ${result.totalSucceeded} transacciones exitosas.`);
          } else if (result.totalSucceeded > 0) {
            this.notifications.warning(`Lote procesado con éxito parcial. Éxitos: ${result.totalSucceeded}, fallos: ${result.totalFailed}.`);
          } else {
            this.notifications.error('El lote fue procesado pero todas las transacciones fallaron.');
          }

          this.cdr.markForCheck();
        },
        error: (error: Error) => {
          this.isSubmitting.set(false);
          this.validationError.set(error.message || 'No fue posible procesar el lote.');
          this.notifications.error(this.validationError() ?? 'No fue posible procesar el lote.');
          this.cdr.markForCheck();
        }
      });
  }

  formatError(item: BulkAchTransactionItemResult): string {
    if (item.succeeded) {
      return '-';
    }

    return item.errorMessage?.trim() || item.errorCode?.trim() || 'Sin detalle';
  }

  private parseAndValidatePayload(): { payload: BulkAchTransactionRequest; warnings: string[] } | null {
    const raw = this.jsonInput().trim();
    if (!raw) {
      this.validationError.set('Debe ingresar o pegar un JSON.');
      return null;
    }

    let parsed: unknown;
    try {
      parsed = JSON.parse(raw);
    } catch {
      this.validationError.set('El contenido no es un JSON válido. Verifica comillas, llaves y comas.');
      return null;
    }

    if (!parsed || typeof parsed !== 'object' || Array.isArray(parsed)) {
      this.validationError.set('La raíz del JSON debe ser un objeto.');
      return null;
    }

    const candidate = parsed as Partial<BulkAchTransactionRequest>;
    const transactions = Array.isArray(candidate.transactions) ? candidate.transactions : null;

    if (!transactions || transactions.length === 0) {
      this.validationError.set('El lote debe incluir al menos una transacción en "transactions".');
      return null;
    }

    const warnings: string[] = [];
    const normalizedBatchReference = (candidate.batchReference ?? this.batchReference()).trim();
    const normalizedChunkSize = Number(candidate.chunkSize ?? this.chunkSize());

    if (!normalizedBatchReference) {
      this.validationError.set('batchReference es obligatorio.');
      return null;
    }

    if (!Number.isFinite(normalizedChunkSize) || normalizedChunkSize < 50 || normalizedChunkSize > 1000) {
      warnings.push('chunkSize fuera de rango recomendado (50-1000). Se usará 200.');
    }

    const invalidIndexes: number[] = [];
    const normalizedTransactions = transactions.map((item, index) => {
      if (!this.isBulkItemValid(item)) {
        invalidIndexes.push(index + 1);
      }
      const candidate = item as BulkAchTransactionItemRequest;
      if (!candidate.transactionExternalId?.trim() && candidate.reference?.trim()) {
        warnings.push(`Fila ${index + 1}: usando referencia legado como llave operativa transicional.`);
      }

      return {
        ...candidate,
        transactionExternalId: candidate.transactionExternalId?.trim() || undefined,
        reference: candidate.reference?.trim() || undefined
      } as BulkAchTransactionItemRequest;
    });

    if (invalidIndexes.length > 0) {
      this.validationError.set(`Existen filas con estructura inválida: ${invalidIndexes.slice(0, 20).join(', ')}.`);
      return null;
    }

    const payload: BulkAchTransactionRequest = {
      batchReference: normalizedBatchReference,
      chunkSize: Number.isFinite(normalizedChunkSize) && normalizedChunkSize >= 50 && normalizedChunkSize <= 1000
        ? Math.trunc(normalizedChunkSize)
        : 200,
      transactions: normalizedTransactions
    };

    this.validationError.set(null);
    return { payload, warnings };
  }

  private isBulkItemValid(item: unknown): item is BulkAchTransactionItemRequest {
    if (!item || typeof item !== 'object' || Array.isArray(item)) {
      return false;
    }

    const candidate = item as Partial<BulkAchTransactionItemRequest>;
    const hasOperationalId = (typeof candidate.transactionExternalId === 'string' && candidate.transactionExternalId.trim().length > 0)
      || (typeof candidate.reference === 'string' && candidate.reference.trim().length > 0);

    return hasOperationalId
      && typeof candidate.sourceAccountNumber === 'string'
      && typeof candidate.destinationAccountNumber === 'string'
      && typeof candidate.companyName === 'string'
      && typeof candidate.companyIdentification === 'string'
      && typeof candidate.companyEntryDescriptionId === 'number'
      && typeof candidate.destinationInstitutionId === 'number'
      && typeof candidate.amount === 'number';
  }

  private buildDefaultBatchReference(): string {
    const now = new Date();
    const stamp = `${now.getFullYear()}${String(now.getMonth() + 1).padStart(2, '0')}${String(now.getDate()).padStart(2, '0')}`;
    return `BATCH-${stamp}-001`;
  }
}

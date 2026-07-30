import { CommonModule } from '@angular/common';
import {
  ChangeDetectionStrategy,
  ChangeDetectorRef,
  Component,
  DestroyRef,
  OnInit,
  inject
} from '@angular/core';
import { FormBuilder, FormControl, ReactiveFormsModule, Validators } from '@angular/forms';
import { HttpErrorResponse } from '@angular/common/http';
import { MatButtonModule } from '@angular/material/button';
import { MatCardModule } from '@angular/material/card';
import { MatChipsModule } from '@angular/material/chips';
import {
  MAT_DIALOG_DATA,
  MatDialog,
  MatDialogModule,
  MatDialogRef
} from '@angular/material/dialog';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatIconModule } from '@angular/material/icon';
import { MatInputModule } from '@angular/material/input';
import { MatProgressBarModule } from '@angular/material/progress-bar';
import { MatSelectModule } from '@angular/material/select';
import { MatTooltipModule } from '@angular/material/tooltip';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { ColDef, RowSelectionOptions } from 'ag-grid-community';
import { finalize } from 'rxjs';
import { SharedModule } from '../../../../shared/shared.module';
import { NotificationService } from '../../../../core/services/notification.service';
import { AchCyclesApiService } from '../../../ach-cycles/services/ach-cycles-api.service';
import { AchCycleSummary } from '../../../ach-cycles/models/ach-cycle.model';
import { AchReturnsApiService } from '../../services/ach-returns-api.service';
import { ReturnReasonsApiService } from '../../services/return-reasons-api.service';
import { ReturnEligibleTransaction, ReturnReason } from '../../transactions.models';

type EligibilityFilter = 'all' | 'eligible' | 'blocked';

interface ReturnReasonDialogData {
  reasons: ReturnReason[];
  selectedCount: number;
}

interface ReturnDetailDialogData {
  id: string;
  traceNumber: string;
  reference: string;
  amount: string;
  transactionCode: string;
  effectiveDate: string;
  cycleId: string;
  originatingDfi: string;
  receivingDfi: string;
  sourceAccount: string;
  destinationAccount: string;
  prenotification: string;
  eligibility: string;
}

interface GeneratedReturnEvidence {
  fileName: string;
  cycleLabel: string;
  reasonCode: string;
  transactionCount: number;
}

@Component({
  selector: 'app-ach-return-reason-dialog',
  standalone: true,
  imports: [
    CommonModule,
    ReactiveFormsModule,
    MatButtonModule,
    MatDialogModule,
    MatFormFieldModule,
    MatIconModule,
    MatSelectModule
  ],
  template: `
    <h2 mat-dialog-title>
      <mat-icon aria-hidden="true">assignment_return</mat-icon>
      Generar archivo de devoluciones
    </h2>
    <mat-dialog-content>
      <p>
        Se incluirán {{ data.selectedCount }} transacción(es) elegible(s). La causal se aplicará
        a todas las seleccionadas.
      </p>
      <mat-form-field appearance="outline">
        <mat-label>Causal de devolución</mat-label>
        <mat-select [formControl]="reasonControl">
          <mat-option *ngFor="let reason of data.reasons" [value]="reason.code">
            {{ reason.code }} · {{ reason.description }}
          </mat-option>
        </mat-select>
        <mat-error *ngIf="reasonControl.hasError('required')">Selecciona una causal.</mat-error>
      </mat-form-field>
      <p class="notice">
        Esta acción genera el archivo NACHA-M .RET existente; no aprueba, rechaza ni reprocesa transacciones.
      </p>
    </mat-dialog-content>
    <mat-dialog-actions align="end">
      <button mat-button type="button" (click)="dialogRef.close()">Cancelar</button>
      <button
        mat-flat-button
        color="primary"
        type="button"
        [disabled]="reasonControl.invalid"
        [mat-dialog-close]="reasonControl.value">
        Generar archivo .RET
      </button>
    </mat-dialog-actions>
  `,
  styles: [`
    h2 {
      display: flex;
      align-items: center;
      gap: .65rem;
    }
    mat-dialog-content {
      display: grid;
      gap: .75rem;
      width: min(34rem, 100%);
    }
    mat-dialog-content p {
      margin: 0;
      line-height: 1.5;
    }
    mat-form-field {
      width: 100%;
    }
    .notice {
      padding: .75rem;
      border-radius: var(--radius-md);
      color: var(--color-text-muted);
      background: var(--color-surface-muted);
      font-size: .84rem;
    }
    @media (max-width: 480px) {
      mat-dialog-actions {
        align-items: stretch;
        flex-direction: column-reverse;
      }
      mat-dialog-actions button {
        width: 100%;
      }
    }
  `],
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class AchReturnReasonDialogComponent {
  readonly data = inject<ReturnReasonDialogData>(MAT_DIALOG_DATA);
  readonly dialogRef = inject(MatDialogRef<AchReturnReasonDialogComponent>);
  readonly reasonControl = new FormControl<string>(
    this.data.reasons.find((reason) => reason.code.startsWith('R'))?.code
      ?? this.data.reasons[0]?.code
      ?? '',
    { nonNullable: true, validators: Validators.required }
  );
}

@Component({
  selector: 'app-ach-return-detail-dialog',
  standalone: true,
  imports: [CommonModule, MatButtonModule, MatDialogModule, MatIconModule],
  template: `
    <h2 mat-dialog-title>
      <mat-icon aria-hidden="true">receipt_long</mat-icon>
      Detalle de transacción retornable
    </h2>
    <mat-dialog-content>
      <dl>
        <div><dt>ID</dt><dd>{{ data.id }}</dd></div>
        <div><dt>Traza</dt><dd class="mono">{{ data.traceNumber }}</dd></div>
        <div><dt>Referencia</dt><dd class="mono">{{ data.reference }}</dd></div>
        <div><dt>Monto</dt><dd class="amount">{{ data.amount }}</dd></div>
        <div><dt>Código de transacción</dt><dd>{{ data.transactionCode }}</dd></div>
        <div><dt>Fecha efectiva</dt><dd>{{ data.effectiveDate }}</dd></div>
        <div><dt>Ciclo</dt><dd class="mono">{{ data.cycleId }}</dd></div>
        <div><dt>DFI originadora</dt><dd>{{ data.originatingDfi }}</dd></div>
        <div><dt>DFI receptora</dt><dd>{{ data.receivingDfi }}</dd></div>
        <div><dt>Cuenta origen</dt><dd class="mono">{{ data.sourceAccount }}</dd></div>
        <div><dt>Cuenta destino</dt><dd class="mono">{{ data.destinationAccount }}</dd></div>
        <div><dt>Prenotificación</dt><dd>{{ data.prenotification }}</dd></div>
        <div class="wide"><dt>Elegibilidad</dt><dd>{{ data.eligibility }}</dd></div>
      </dl>
    </mat-dialog-content>
    <mat-dialog-actions align="end">
      <button mat-flat-button color="primary" type="button" mat-dialog-close>Cerrar</button>
    </mat-dialog-actions>
  `,
  styles: [`
    h2 {
      display: flex;
      align-items: center;
      gap: .65rem;
    }
    dl {
      display: grid;
      grid-template-columns: repeat(2, minmax(0, 1fr));
      gap: .75rem;
      width: min(42rem, 100%);
      margin: 0;
    }
    dl div {
      min-width: 0;
      padding: .65rem .75rem;
      border-radius: var(--radius-md);
      background: var(--color-surface-muted);
    }
    dt {
      color: var(--color-text-muted);
      font-size: .75rem;
      font-weight: 750;
    }
    dd {
      margin: .25rem 0 0;
      overflow-wrap: anywhere;
    }
    .wide {
      grid-column: 1 / -1;
    }
    .mono {
      font-family: ui-monospace, SFMono-Regular, Consolas, monospace;
      font-size: .82rem;
    }
    .amount {
      text-align: right;
      font-weight: 750;
    }
    @media (max-width: 560px) {
      dl {
        grid-template-columns: 1fr;
      }
      .wide {
        grid-column: 1;
      }
      mat-dialog-actions button {
        width: 100%;
      }
    }
  `],
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class AchReturnDetailDialogComponent {
  readonly data = inject<ReturnDetailDialogData>(MAT_DIALOG_DATA);
}

@Component({
  selector: 'app-ach-returns-management',
  standalone: true,
  imports: [
    SharedModule,
    MatButtonModule,
    MatCardModule,
    MatChipsModule,
    MatDialogModule,
    MatFormFieldModule,
    MatIconModule,
    MatInputModule,
    MatProgressBarModule,
    MatSelectModule,
    MatTooltipModule
  ],
  templateUrl: './ach-returns-management.component.html',
  styleUrls: ['./ach-returns-management.component.scss'],
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class AchReturnsManagementComponent implements OnInit {
  private readonly fb = inject(FormBuilder);
  private readonly cyclesApi = inject(AchCyclesApiService);
  private readonly returnsApi = inject(AchReturnsApiService);
  private readonly returnReasonsApi = inject(ReturnReasonsApiService);
  private readonly notifications = inject(NotificationService);
  private readonly dialog = inject(MatDialog);
  private readonly cdr = inject(ChangeDetectorRef);
  private readonly destroyRef = inject(DestroyRef);

  cycles: AchCycleSummary[] = [];
  reasons: ReturnReason[] = [];
  allRows: ReturnEligibleTransaction[] = [];
  rows: ReturnEligibleTransaction[] = [];
  loadError: string | null = null;
  catalogError: string | null = null;
  actionError: string | null = null;
  loading = false;
  loadingCatalogs = false;
  generating = false;
  hasLoaded = false;
  lastGenerated: GeneratedReturnEvidence | null = null;
  readonly selectedRows = new Set<number>();

  readonly columnDefs: ColDef<ReturnEligibleTransaction>[] = [
    { field: 'traceNumber', headerName: 'Traza', minWidth: 170 },
    { field: 'reference', headerName: 'Referencia', minWidth: 180 },
    {
      field: 'effectiveEntryDate',
      headerName: 'Fecha',
      minWidth: 135,
      valueFormatter: (params) => this.formatDate(params.value)
    },
    {
      field: 'originatingDfi',
      headerName: 'DFI origen',
      minWidth: 130
    },
    {
      field: 'receivingDfi',
      headerName: 'DFI destino',
      minWidth: 130
    },
    {
      field: 'amount',
      headerName: 'Monto',
      minWidth: 150,
      type: 'numericColumn',
      cellClass: 'amount-column',
      valueFormatter: (params) => this.formatAmount(params.value)
    },
    { field: 'transactionCode', headerName: 'Código Tx', minWidth: 125 },
    {
      field: 'isEligible',
      headerName: 'Estado',
      minWidth: 210,
      valueGetter: (params) =>
        params.data?.isEligible ? 'Elegible' : (params.data?.validationMessage || 'No elegible')
    }
  ];

  readonly filterForm = this.fb.nonNullable.group({
    cycleId: ['', Validators.required],
    eligibility: ['all' as EligibilityFilter],
    query: ['']
  });
  readonly rowSelection = {
    mode: 'multiRow',
    checkboxes: (params) => !!params.data?.isEligible,
    hideDisabledCheckboxes: false,
    headerCheckbox: true,
    selectAll: 'filtered',
    isRowSelectable: (rowNode) => !!rowNode.data?.isEligible
  } satisfies RowSelectionOptions<ReturnEligibleTransaction>;

  get eligibleCount(): number {
    return this.allRows.filter((row) => row.isEligible).length;
  }

  get blockedCount(): number {
    return this.allRows.length - this.eligibleCount;
  }

  get selectedTransaction(): ReturnEligibleTransaction | null {
    if (this.selectedRows.size !== 1) {
      return null;
    }
    const selectedId = Array.from(this.selectedRows)[0];
    return this.allRows.find((row) => row.id === selectedId) ?? null;
  }

  get selectedCycleLabel(): string {
    const id = this.filterForm.controls.cycleId.value;
    const cycle = this.cycles.find((item) => item.id === id);
    return cycle ? this.cycleLabel(cycle) : 'Ciclo no identificado';
  }

  ngOnInit(): void {
    this.loadCatalogs();
  }

  loadTransactions(): void {
    if (this.loading || this.generating) {
      return;
    }
    if (this.filterForm.invalid) {
      this.filterForm.markAllAsTouched();
      this.notifications.warning('Selecciona un ciclo operativo.');
      this.cdr.markForCheck();
      return;
    }

    const cycleId = this.filterForm.controls.cycleId.value;
    this.loading = true;
    this.hasLoaded = true;
    this.loadError = null;
    this.actionError = null;
    this.lastGenerated = null;
    this.allRows = [];
    this.rows = [];
    this.selectedRows.clear();
    this.cdr.markForCheck();

    this.returnsApi.getTransactionsByCycle(cycleId)
      .pipe(
        takeUntilDestroyed(this.destroyRef),
        finalize(() => {
          this.loading = false;
          this.cdr.markForCheck();
        })
      )
      .subscribe({
        next: (items) => {
          this.allRows = Array.isArray(items) ? items : [];
          this.applyClientFilters();
        },
        error: (error: unknown) => {
          this.allRows = [];
          this.rows = [];
          this.loadError = this.errorMessage(error, 'No fue posible cargar las transacciones del ciclo seleccionado.');
        }
      });
  }

  applyClientFilters(): void {
    const { eligibility, query } = this.filterForm.getRawValue();
    const normalizedQuery = query.trim().toLocaleLowerCase('es-CO');
    this.rows = this.allRows.filter((row) => {
      const eligibilityMatches = eligibility === 'all'
        || (eligibility === 'eligible' && row.isEligible)
        || (eligibility === 'blocked' && !row.isEligible);
      const queryMatches = !normalizedQuery
        || [row.traceNumber, row.reference, row.transactionCode]
          .some((value) => `${value ?? ''}`.toLocaleLowerCase('es-CO').includes(normalizedQuery));
      return eligibilityMatches && queryMatches;
    });
    const visibleIds = new Set(this.rows.map((row) => row.id));
    Array.from(this.selectedRows)
      .filter((id) => !visibleIds.has(id))
      .forEach((id) => this.selectedRows.delete(id));
    this.cdr.markForCheck();
  }

  clearFilters(): void {
    this.filterForm.reset({ cycleId: '', eligibility: 'all', query: '' });
    this.allRows = [];
    this.rows = [];
    this.selectedRows.clear();
    this.loadError = null;
    this.actionError = null;
    this.lastGenerated = null;
    this.hasLoaded = false;
    this.cdr.markForCheck();
  }

  retry(): void {
    this.loadTransactions();
  }

  onSelectionChanged(rows: ReturnEligibleTransaction[]): void {
    this.selectedRows.clear();
    rows.filter((row) => row.isEligible).forEach((row) => this.selectedRows.add(row.id));
    this.cdr.markForCheck();
  }

  openDetail(): void {
    const row = this.selectedTransaction;
    if (!row) {
      this.notifications.warning('Selecciona una sola transacción para consultar el detalle.');
      return;
    }

    const data: ReturnDetailDialogData = {
      id: String(row.id),
      traceNumber: this.safeIdentifier(row.traceNumber),
      reference: this.safeIdentifier(row.reference),
      amount: this.formatAmount(row.amount),
      transactionCode: this.safeIdentifier(row.transactionCode),
      effectiveDate: this.formatDate(row.effectiveEntryDate),
      cycleId: this.safeIdentifier(row.achCycleId),
      originatingDfi: this.safeIdentifier(row.originatingDfi),
      receivingDfi: this.safeIdentifier(row.receivingDfi),
      sourceAccount: this.maskAccount(row.sourceAccountNumber),
      destinationAccount: this.maskAccount(row.destinationAccountNumber),
      prenotification: row.isPrenotification ? 'Sí' : 'No',
      eligibility: row.isEligible ? 'Elegible' : this.safeIdentifier(row.validationMessage || 'No elegible')
    };
    this.dialog.open(AchReturnDetailDialogComponent, {
      data,
      width: 'min(94vw, 760px)',
      maxHeight: '90vh',
      autoFocus: 'dialog'
    });
  }

  openReasonDialog(): void {
    if (this.generating || this.loading) {
      return;
    }
    if (this.selectedRows.size === 0) {
      this.notifications.warning('Selecciona al menos una transacción elegible.');
      return;
    }
    if (this.reasons.length === 0) {
      this.notifications.error('No hay causales de devolución disponibles.');
      return;
    }

    const data: ReturnReasonDialogData = {
      reasons: this.reasons,
      selectedCount: this.selectedRows.size
    };
    this.dialog.open(AchReturnReasonDialogComponent, {
      data,
      width: 'min(94vw, 620px)',
      maxHeight: '90vh',
      disableClose: true
    })
      .afterClosed()
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe((reasonCode: string | undefined) => {
        if (reasonCode) {
          this.generateFile(reasonCode);
        }
      });
  }

  generateFile(reasonCode: string): void {
    if (this.generating || this.loading) {
      return;
    }
    const cycleId = this.filterForm.controls.cycleId.value;
    const selectedItems = this.allRows
      .filter((row) => row.isEligible && this.selectedRows.has(row.id))
      .map((row) => ({ transactionId: row.id, returnReasonCode: reasonCode }));
    if (!cycleId || selectedItems.length === 0) {
      this.notifications.warning('No hay transacciones elegibles seleccionadas para generar el archivo.');
      return;
    }

    this.generating = true;
    this.actionError = null;
    this.lastGenerated = null;
    this.returnsApi.generateFile({ cycleId, items: selectedItems })
      .pipe(
        takeUntilDestroyed(this.destroyRef),
        finalize(() => {
          this.generating = false;
          this.cdr.markForCheck();
        })
      )
      .subscribe({
        next: (blob) => {
          const fileName = `devoluciones_${cycleId}.RET`;
          const link = document.createElement('a');
          const objectUrl = URL.createObjectURL(blob);
          link.href = objectUrl;
          link.download = fileName;
          link.click();
          URL.revokeObjectURL(objectUrl);

          this.lastGenerated = {
            fileName,
            cycleLabel: this.selectedCycleLabel,
            reasonCode,
            transactionCount: selectedItems.length
          };
          this.notifications.success('Archivo NACHA-M de devoluciones generado correctamente.');
        },
        error: (error: unknown) => {
          this.actionError = this.errorMessage(
            error,
            'No fue posible generar el archivo de devoluciones.'
          );
        }
      });
  }

  cycleLabel(cycle: AchCycleSummary): string {
    return `${cycle.cycleName} · ${cycle.clearingHouseName} · ${this.formatDate(
      cycle.date ?? cycle.processingDate
    )}`;
  }

  private loadCatalogs(): void {
    this.loadingCatalogs = true;
    this.catalogError = null;
    let pending = 2;
    const complete = (): void => {
      pending -= 1;
      if (pending === 0) {
        this.loadingCatalogs = false;
        this.cdr.markForCheck();
      }
    };

    this.cyclesApi.search({ page: 1, pageSize: 100 })
      .pipe(takeUntilDestroyed(this.destroyRef), finalize(complete))
      .subscribe({
        next: (response) => {
          this.cycles = response.items ?? [];
          this.cdr.markForCheck();
        },
        error: (error: unknown) => {
          this.cycles = [];
          this.catalogError = this.errorMessage(error, 'No fue posible cargar los ciclos operativos.');
        }
      });

    this.returnReasonsApi.getForReturns()
      .pipe(takeUntilDestroyed(this.destroyRef), finalize(complete))
      .subscribe({
        next: (items) => {
          this.reasons = (items ?? []).filter((reason) => reason.isForReturn);
          this.cdr.markForCheck();
        },
        error: (error: unknown) => {
          this.reasons = [];
          this.catalogError = this.errorMessage(error, 'No fue posible cargar las causales de devolución.');
        }
      });
  }

  private formatAmount(value: number | null | undefined): string {
    if (typeof value !== 'number' || !Number.isFinite(value)) {
      return 'COP 0,00';
    }
    return new Intl.NumberFormat('es-CO', {
      style: 'currency',
      currency: 'COP',
      minimumFractionDigits: 2,
      maximumFractionDigits: 2
    }).format(value);
  }

  private formatDate(value: string | Date | null | undefined): string {
    if (!value) {
      return 'Sin fecha';
    }
    if (value instanceof Date) {
      const year = value.getFullYear();
      const month = `${value.getMonth() + 1}`.padStart(2, '0');
      const day = `${value.getDate()}`.padStart(2, '0');
      return `${day}/${month}/${year}`;
    }
    const match = /^(\d{4})-(\d{2})-(\d{2})/.exec(value);
    return match ? `${match[3]}/${match[2]}/${match[1]}` : 'Sin fecha';
  }

  private maskAccount(value: string | null | undefined): string {
    const normalized = `${value ?? ''}`.trim();
    if (!normalized) {
      return 'No informada';
    }
    const visible = normalized.slice(-4);
    return `${'•'.repeat(Math.max(4, Math.min(8, normalized.length - visible.length)))}${visible}`;
  }

  private safeIdentifier(value: string | null | undefined): string {
    return `${value ?? ''}`.replace(/[\r\n\t]+/g, ' ').trim().slice(0, 120) || 'No informado';
  }

  private errorMessage(error: unknown, fallback: string): string {
    if (error instanceof HttpErrorResponse) {
      const detail = typeof error.error?.detail === 'string'
        ? error.error.detail
        : typeof error.error?.message === 'string'
          ? error.error.message
          : typeof error.error?.Message === 'string'
            ? error.error.Message
            : '';
      return this.safeMessage(detail, fallback);
    }
    return this.safeMessage(error instanceof Error ? error.message : '', fallback);
  }

  private safeMessage(value: string | null | undefined, fallback: string): string {
    const sanitized = `${value ?? ''}`
      .replace(/[\r\n\t]+/g, ' ')
      .replace(/\s{2,}/g, ' ')
      .trim();
    return sanitized ? sanitized.slice(0, 300) : fallback;
  }
}

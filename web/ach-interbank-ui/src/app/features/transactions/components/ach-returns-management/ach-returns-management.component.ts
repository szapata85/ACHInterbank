import { ChangeDetectionStrategy, ChangeDetectorRef, Component, OnInit, inject } from '@angular/core';
import { ColDef } from 'ag-grid-community';
import { SharedModule } from '../../../../shared/shared.module';
import { NotificationService } from '../../../../core/services/notification.service';
import { AchCyclesApiService } from '../../../ach-cycles/services/ach-cycles-api.service';
import { AchCycleSummary } from '../../../ach-cycles/models/ach-cycle.model';
import { AchReturnsApiService } from '../../services/ach-returns-api.service';
import { ReturnReasonsApiService } from '../../services/return-reasons-api.service';
import { ReturnEligibleTransaction, ReturnReason } from '../../transactions.models';

@Component({
  selector: 'app-ach-returns-management',
  standalone: true,
  imports: [SharedModule],
  templateUrl: './ach-returns-management.component.html',
  styleUrls: ['./ach-returns-management.component.scss'],
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class AchReturnsManagementComponent implements OnInit {
  private readonly cyclesApi = inject(AchCyclesApiService);
  private readonly returnsApi = inject(AchReturnsApiService);
  private readonly returnReasonsApi = inject(ReturnReasonsApiService);
  private readonly notifications = inject(NotificationService);
  private readonly cdr = inject(ChangeDetectorRef);

  cycles: AchCycleSummary[] = [];
  reasons: ReturnReason[] = [];
  rows: ReturnEligibleTransaction[] = [];

  selectedCycleId: string | null = null;
  selectedReasonCode = '';
  showReasonModal = false;
  loading = false;

  selectedRows = new Set<number>();
  readonly columnDefs: ColDef<ReturnEligibleTransaction>[] = [
    {
      headerName: 'Sel.',
      width: 90,
      checkboxSelection: (params) => !!params.data?.isEligible,
      headerCheckboxSelection: true,
      headerCheckboxSelectionFilteredOnly: true
    },
    { field: 'id', headerName: 'ID', width: 110 },
    { field: 'traceNumber', headerName: 'Trace', minWidth: 170 },
    { field: 'reference', headerName: 'Referencia', minWidth: 180 },
    { field: 'amount', headerName: 'Monto', valueFormatter: (params) => Number(params.value ?? 0).toLocaleString('es-CO', { style: 'currency', currency: 'COP' }) },
    { field: 'transactionCode', headerName: 'Cod. Tx', width: 130 },
    {
      headerName: 'Estado',
      minWidth: 220,
      valueGetter: (params) => params.data?.isEligible ? 'Elegible' : (params.data?.validationMessage || 'No elegible')
    }
  ];

  ngOnInit(): void {
    this.loadCycles();
    this.loadReasons();
  }

  toggleSelection(id: number, checked: boolean): void {
    if (checked) {
      this.selectedRows.add(id);
    } else {
      this.selectedRows.delete(id);
    }
  }

  selectAllEligible(checked: boolean): void {
    this.selectedRows.clear();
    if (checked) {
      this.rows.filter((r) => r.isEligible).forEach((row) => this.selectedRows.add(row.id));
    }
  }

  openReasonModal(): void {
    if (this.selectedRows.size === 0) {
      this.notifications.warning('Seleccione al menos una transacción.');
      return;
    }

    this.selectedReasonCode = this.reasons.find((r) => r.code.startsWith('R'))?.code ?? '';
    this.showReasonModal = true;
    this.cdr.markForCheck();
  }

  closeReasonModal(): void {
    this.showReasonModal = false;
    this.cdr.markForCheck();
  }

  loadTransactions(): void {
    if (!this.selectedCycleId) {
      this.notifications.warning('Seleccione un ciclo operativo.');
      return;
    }

    this.loading = true;
    this.rows = [];
    this.selectedRows.clear();

    this.returnsApi.getTransactionsByCycle(this.selectedCycleId).subscribe({
      next: (items) => {
        this.rows = items;
        this.loading = false;
        this.cdr.markForCheck();
      },
      error: () => {
        this.loading = false;
        this.notifications.error('No fue posible cargar las transacciones del ciclo.');
        this.cdr.markForCheck();
      }
    });
  }

  generateFile(): void {
    if (this.loading) {
      return;
    }
    if (!this.selectedCycleId) {
      this.notifications.warning('Seleccione el ciclo de operación.');
      return;
    }

    if (!this.selectedReasonCode) {
      this.notifications.warning('Seleccione una causal de devolución (Rxx).');
      return;
    }

    const selectedItems = this.rows
      .filter((row) => this.selectedRows.has(row.id))
      .map((row) => ({ transactionId: row.id, returnReasonCode: this.selectedReasonCode }));

    if (selectedItems.length === 0) {
      this.notifications.warning('No hay transacciones seleccionadas para devolver.');
      return;
    }

    this.loading = true;
    this.returnsApi.generateFile({ cycleId: this.selectedCycleId, items: selectedItems }).subscribe({
      next: (blob) => {
        const fileName = `devoluciones_${this.selectedCycleId}.RET`;
        const link = document.createElement('a');
        link.href = URL.createObjectURL(blob);
        link.download = fileName;
        link.click();
        URL.revokeObjectURL(link.href);

        this.loading = false;
        this.showReasonModal = false;
        this.notifications.success('Archivo NACHA-M de devoluciones generado correctamente.');
        this.cdr.markForCheck();
      },
      error: (err) => {
        this.loading = false;
        this.notifications.error(err?.error?.message ?? 'No fue posible generar el archivo de devoluciones.');
        this.cdr.markForCheck();
      }
    });
  }

  onSelectionChanged(rows: ReturnEligibleTransaction[]): void {
    this.selectedRows.clear();
    rows.forEach((row) => this.selectedRows.add(row.id));
    this.cdr.markForCheck();
  }

  private loadCycles(): void {
    this.cyclesApi.search({ page: 1, pageSize: 100 }).subscribe({
      next: (response) => {
        this.cycles = response.items ?? [];
        if (!this.selectedCycleId && this.cycles.length > 0) {
          this.selectedCycleId = this.cycles[0].id;
        }
        this.cdr.markForCheck();
      },
      error: () => this.notifications.error('No fue posible cargar los ciclos operativos.')
    });
  }

  private loadReasons(): void {
    this.returnReasonsApi.getForReturns().subscribe({
      next: (items) => {
        this.reasons = items ?? [];
        this.cdr.markForCheck();
      },
      error: () => this.notifications.error('No fue posible cargar el catálogo de causales de devolución.')
    });
  }
}

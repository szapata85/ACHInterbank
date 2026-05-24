import { ChangeDetectionStrategy, ChangeDetectorRef, Component, OnInit, inject } from '@angular/core';
import { FormBuilder, Validators } from '@angular/forms';
import { ColDef, RowSelectionOptions } from 'ag-grid-community';
import { finalize } from 'rxjs';
import { SharedModule } from '../../../../shared/shared.module';
import { NotificationService } from '../../../../core/services/notification.service';
import { AchCyclesApiService } from '../../../ach-cycles/services/ach-cycles-api.service';
import { AchCycleSummary } from '../../../ach-cycles/models/ach-cycle.model';
import { AchReturnsApiService } from '../../services/ach-returns-api.service';
import { ReturnReasonsApiService } from '../../services/return-reasons-api.service';
import { ReturnEligibleTransaction, ReturnReason } from '../../transactions.models';
import { OpcionSelectorBuscable } from '../../../../shared/components/ui/ui-selector-buscable.component';

@Component({
  selector: 'app-ach-returns-management',
  standalone: true,
  imports: [SharedModule],
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
  private readonly cdr = inject(ChangeDetectorRef);

  cycles: AchCycleSummary[] = [];
  reasons: ReturnReason[] = [];
  rows: ReturnEligibleTransaction[] = [];
  loadError = '';

  showReasonModal = false;
  loading = false;

  selectedRows = new Set<number>();
  readonly columnDefs: ColDef<ReturnEligibleTransaction>[] = [
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
  readonly filtrosForm = this.fb.group({
    cycleId: [null as string | null, Validators.required]
  });
  readonly rowSelection = {
    mode: 'multiRow',
    checkboxes: (params) => !!params.data?.isEligible,
    hideDisabledCheckboxes: false,
    headerCheckbox: true,
    selectAll: 'filtered',
    isRowSelectable: (rowNode) => !!rowNode.data?.isEligible
  } satisfies RowSelectionOptions<ReturnEligibleTransaction>;
  readonly devolucionForm = this.fb.group({
    reasonCode: ['', Validators.required]
  });
  get cycleOptions(): OpcionSelectorBuscable[] {
    return this.cycles.map((cycle) => ({
      valor: cycle.id,
      etiqueta: `${cycle.cycleName} (${this.toDate(cycle.date ?? cycle.processingDate)})`
    }));
  }
  get reasonOptions(): OpcionSelectorBuscable[] {
    return this.reasons.map((reason) => ({
      valor: reason.code,
      etiqueta: `${reason.code} - ${reason.description}`
    }));
  }

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

    this.devolucionForm.patchValue({
      reasonCode: this.reasons.find((r) => r.code.startsWith('R'))?.code ?? ''
    });
    this.showReasonModal = true;
    this.cdr.markForCheck();
  }

  closeReasonModal(): void {
    this.showReasonModal = false;
    this.cdr.markForCheck();
  }

  loadTransactions(): void {
    const selectedCycleId = this.filtrosForm.controls.cycleId.value;
    if (!selectedCycleId) {
      this.notifications.warning('Seleccione un ciclo operativo.');
      return;
    }

    this.loading = true;
    this.loadError = '';
    this.rows = [];
    this.selectedRows.clear();

    this.returnsApi.getTransactionsByCycle(selectedCycleId).pipe(
      finalize(() => {
        this.loading = false;
        this.cdr.markForCheck();
      })
    ).subscribe({
      next: (items) => {
        this.rows = Array.isArray(items) ? items : [];
      },
      error: () => {
        this.rows = [];
        this.loadError = 'No fue posible cargar las devoluciones del ciclo seleccionado.';
        this.notifications.error('No fue posible cargar las transacciones del ciclo.');
      }
    });
  }

  generateFile(): void {
    if (this.loading) {
      return;
    }
    const selectedCycleId = this.filtrosForm.controls.cycleId.value;
    if (!selectedCycleId) {
      this.notifications.warning('Seleccione el ciclo de operación.');
      return;
    }

    const selectedReasonCode = this.devolucionForm.controls.reasonCode.value ?? '';
    if (!selectedReasonCode) {
      this.notifications.warning('Seleccione una causal de devolución (Rxx).');
      return;
    }

    const selectedItems = this.rows
      .filter((row) => this.selectedRows.has(row.id))
      .map((row) => ({ transactionId: row.id, returnReasonCode: selectedReasonCode }));

    if (selectedItems.length === 0) {
      this.notifications.warning('No hay transacciones seleccionadas para devolver.');
      return;
    }

    this.loading = true;
    this.returnsApi.generateFile({ cycleId: selectedCycleId, items: selectedItems }).pipe(
      finalize(() => {
        this.loading = false;
        this.cdr.markForCheck();
      })
    ).subscribe({
      next: (blob) => {
        const fileName = `devoluciones_${selectedCycleId}.RET`;
        const link = document.createElement('a');
        link.href = URL.createObjectURL(blob);
        link.download = fileName;
        link.click();
        URL.revokeObjectURL(link.href);

        this.showReasonModal = false;
        this.notifications.success('Archivo NACHA-M de devoluciones generado correctamente.');
      },
      error: (err) => {
        this.notifications.error(err?.error?.message ?? 'No fue posible generar el archivo de devoluciones.');
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
        if (!this.filtrosForm.controls.cycleId.value && this.cycles.length > 0) {
          this.filtrosForm.patchValue({ cycleId: this.cycles[0].id }, { emitEvent: false });
        }
        this.cdr.markForCheck();
      },
      error: () => {
        this.cycles = [];
        this.loadError = 'No fue posible cargar los ciclos operativos.';
        this.notifications.error('No fue posible cargar los ciclos operativos.');
        this.cdr.markForCheck();
      }
    });
  }

  private loadReasons(): void {
    this.returnReasonsApi.getForReturns().subscribe({
      next: (items) => {
        this.reasons = items ?? [];
        this.cdr.markForCheck();
      },
      error: () => {
        this.reasons = [];
        this.notifications.error('No fue posible cargar el catálogo de causales de devolución.');
        this.cdr.markForCheck();
      }
    });
  }

  private toDate(value: string | Date | null | undefined): string {
    if (!value) {
      return 'Sin fecha';
    }

    const date = new Date(value);
    if (Number.isNaN(date.getTime())) {
      return 'Sin fecha';
    }

    return date.toISOString().slice(0, 10);
  }
}

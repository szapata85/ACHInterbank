import { ChangeDetectionStrategy, ChangeDetectorRef, Component, OnInit, inject } from '@angular/core';
import { FormBuilder, Validators } from '@angular/forms';
import { HttpErrorResponse } from '@angular/common/http';
import { finalize } from 'rxjs';
import { SharedModule } from '../../../../shared/shared.module';
import { NotificationService } from '../../../../core/services/notification.service';
import { FinancialInstitutionsApiService } from '../../../transactions/services/financial-institutions-api.service';
import { DestinationInstitution } from '../../../transactions/transactions.models';
import { FinancialInstitutionStatusEnum } from '../../../transactions/transactions.types';
import { ClearingHousesApiService } from '../../../ach-cycles/services/ach-cycles-api.service';
import {
  AvailableInboundCycle,
  GenerateNachaInboundSimulationRequest,
  DifferentialResponseEligibleTransaction,
  InboundResponseMode,
  NachaInboundSimulationItem,
  NachaInboundSimulationResult,
  NachaInboundSimulationType,
  NachaSimulationMode,
  NachaInboundSimulatorService
} from '../../services/nacha-inbound-simulator.service';

@Component({
  selector: 'app-nacha-inbound-simulator',
  standalone: true,
  imports: [SharedModule],
  templateUrl: './nacha-inbound-simulator.component.html',
  styleUrls: ['./nacha-inbound-simulator.component.scss'],
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class NachaInboundSimulatorComponent implements OnInit {
  private readonly fb = inject(FormBuilder);
  private readonly api = inject(NachaInboundSimulatorService);
  private readonly financialInstitutionsApi = inject(FinancialInstitutionsApiService);
  private readonly clearingHousesApi = inject(ClearingHousesApiService);
  private readonly notifications = inject(NotificationService);
  private readonly cdr = inject(ChangeDetectorRef);

  loading = false;
  generating = false;
  previewing = false;
  simulations: NachaInboundSimulationItem[] = [];
  originFinancialInstitutions: DestinationInstitution[] = [];
  defaultDestination: DestinationInstitution | null = null;
  result: NachaInboundSimulationResult | null = null;
  error: string | null = null;
  eligibleTransactions: DifferentialResponseEligibleTransaction[] = [];
  eligibleTotal = 0;
  eligiblePage = 1;
  readonly eligiblePageSize = 10;
  loadingEligible = false;
  loadingCycles = false;
  availableCycles: AvailableInboundCycle[] = [];
  cycleAvailabilityMessage: string | null = null;
  clearingHouses: Array<{ value: string; label: string }> = [];
  readonly selectedTransactionIds = new Set<number>();
  private readonly selectedTransactionReferences = new Map<number, string>();

  get availableCycleOptions(): Array<{ valor: string; etiqueta: string; descripcion: string }> {
    return this.availableCycles.map((cycle) => ({
      valor: cycle.cycleCode,
      etiqueta: `${cycle.cycleName} · ${cycle.clearingHouseName}`,
      descripcion: `${this.formatProcessingDate(cycle.processingDate)} · ${cycle.transactionCount} transacciones · ${cycle.status}`
    }));
  }

  readonly incomingScenarios: Array<{ value: NachaInboundSimulationType; label: string }> = [
    { value: 'IncomingCredit', label: 'Crédito entrante' },
    { value: 'IncomingDebit', label: 'Débito entrante' }
  ];

  readonly differentialScenarios: Array<{ value: NachaInboundSimulationType; label: string }> = [
    { value: 'IncomingCreditConfirmation', label: 'Aceptación de crédito' },
    { value: 'IncomingCreditRejection', label: 'Rechazo de crédito' },
    { value: 'IncomingCreditReturn', label: 'Devolución de crédito' },
    { value: 'IncomingDebitConfirmation', label: 'Aceptación de débito' },
    { value: 'IncomingDebitRejection', label: 'Rechazo de débito' },
    { value: 'IncomingDebitReturn', label: 'Devolución de débito' },
    { value: 'IncomingPrenotificationResponse', label: 'Respuesta de prenotificación' }
  ];

  readonly responseModes: Array<{ value: InboundResponseMode; label: string }> = [
    { value: 'Approved', label: 'Aprobada' },
    { value: 'Rejected', label: 'Rechazada' },
    { value: 'Confirmed', label: 'Confirmada' },
    { value: 'Returned', label: 'Devuelta' },
    { value: 'Failed', label: 'Fallida' }
  ];

  readonly form = this.fb.group({
    simulationMode: ['IncomingTransactions' as NachaSimulationMode, Validators.required],
    clearingHouseCode: ['', Validators.required],
    scenarioType: ['IncomingCredit' as NachaInboundSimulationType, Validators.required],
    originFinancialInstitutionId: [null as number | null, Validators.required],
    entriesCount: [1, [Validators.required, Validators.min(1), Validators.max(10)]],
    amount: [1000, [Validators.required, Validators.min(0)]],
    referencePrefix: ['UAT-IN-CRED', Validators.required],
    businessDate: [new Date().toISOString().substring(0, 10), Validators.required],
    cycleCode: ['', Validators.required],
    pendingPrenotificationReferencesText: [''],
    transactionReferencesText: [''],
    responseMode: [null as InboundResponseMode | null],
    reasonCode: [''],
    notes: ['']
  });

  ngOnInit(): void {
    this.loadFinancialInstitutions();
    this.loadClearingHouses();
    this.load();
  }

  loadClearingHouses(): void {
    this.clearingHousesApi.list().subscribe({
      next: (items) => {
        this.clearingHouses = (items ?? [])
          .filter((item) => !!item.code)
          .map((item) => ({ value: item.code!, label: item.name }));
        const current = this.form.controls.clearingHouseCode.value;
        if (!current && this.clearingHouses.length === 1) {
          this.form.controls.clearingHouseCode.setValue(this.clearingHouses[0].value);
        }
        this.loadAvailableCycles();
        this.cdr.markForCheck();
      },
      error: (error: HttpErrorResponse) => {
        this.notifications.error(this.errorMessage(error));
        this.cdr.markForCheck();
      }
    });
  }

  loadAvailableCycles(): void {
    const clearingHouseCode = this.form.controls.clearingHouseCode.value;
    const processingDate = this.form.controls.businessDate.value;
    const scenarioType = this.form.controls.scenarioType.value;
    if (!clearingHouseCode || !processingDate || !scenarioType) {
      this.availableCycles = [];
      this.form.controls.cycleCode.setValue('');
      return;
    }

    const selected = this.form.controls.cycleCode.value;
    this.loadingCycles = true;
    this.cycleAvailabilityMessage = null;
    this.api.availableCycles({ clearingHouseCode, processingDate, scenarioType })
      .pipe(finalize(() => {
        this.loadingCycles = false;
        this.cdr.markForCheck();
      }))
      .subscribe({
        next: (items) => {
          this.availableCycles = items ?? [];
          if (selected && !this.availableCycles.some((item) => item.cycleCode === selected)) {
            this.form.controls.cycleCode.setValue('');
            this.cycleAvailabilityMessage = 'El ciclo seleccionado dejó de estar disponible. Seleccione otro ciclo.';
          } else if (!selected && this.availableCycles.length === 1) {
            this.form.controls.cycleCode.setValue(this.availableCycles[0].cycleCode);
          }
        },
        error: (error: HttpErrorResponse) => {
          this.availableCycles = [];
          this.notifications.error(this.errorMessage(error));
        }
      });
  }

  availabilityContextChanged(): void {
    this.loadAvailableCycles();
    if (this.isDifferentialMode()) {
      this.loadEligibleTransactions();
    }
  }

  private formatProcessingDate(value: string): string {
    const [year, month, day] = value.substring(0, 10).split('-');
    return year && month && day ? `${day}/${month}/${year}` : value;
  }

  loadFinancialInstitutions(): void {
    this.financialInstitutionsApi.getAll(false).subscribe({
      next: (items) => {
        const active = (items ?? []).filter((item) => item.status === FinancialInstitutionStatusEnum.Active);
        this.originFinancialInstitutions = active.filter((item) => !item.isDefaultSource);
        const defaults = active.filter((item) => item.isDefaultSource);
        this.defaultDestination = defaults.length === 1 ? defaults[0] : null;
        if (!this.defaultDestination) {
          this.notifications.error('No se pudo resolver una unica entidad destino/receptora default CFA.');
        }
        this.cdr.markForCheck();
      },
      error: (error: HttpErrorResponse) => {
        this.notifications.error(this.errorMessage(error));
        this.cdr.markForCheck();
      }
    });
  }

  load(): void {
    this.loading = true;
    this.api.list()
      .pipe(finalize(() => {
        this.loading = false;
        this.cdr.markForCheck();
      }))
      .subscribe({
        next: (items) => this.simulations = items ?? [],
        error: (error: HttpErrorResponse) => this.error = this.errorMessage(error)
      });
  }

  preview(): void {
    if (this.form.invalid || (this.isDifferentialMode() && this.selectedTransactionIds.size === 0)) {
      this.form.markAllAsTouched();
      return;
    }

    this.previewing = true;
    this.api.preview(this.payload())
      .pipe(finalize(() => {
        this.previewing = false;
        this.cdr.markForCheck();
      }))
      .subscribe({
        next: (response) => {
          if (response?.eligible) {
            this.notifications.success('Solicitud elegible para generar el archivo simulado.');
          } else {
            this.notifications.error(response?.message || 'La solicitud no es elegible.');
          }
        },
        error: (error: HttpErrorResponse) => this.notifications.error(this.errorMessage(error))
      });
  }

  generate(): void {
    if (this.form.invalid || this.reasonRequiredMissing() || !this.defaultDestination
      || (this.isDifferentialMode() && this.selectedTransactionIds.size === 0)) {
      this.form.markAllAsTouched();
      if (!this.defaultDestination) {
        this.notifications.error('La entidad destino/receptora default CFA no esta disponible. No se puede generar.');
      }
      return;
    }

    this.generating = true;
    this.result = null;
    this.api.generate(this.payload())
      .pipe(finalize(() => {
        this.generating = false;
        this.cdr.markForCheck();
      }))
      .subscribe({
        next: (result) => {
          this.result = result;
          this.notifications.success('Archivo NACHA-M simulado generado. Debe cargarse manualmente por NachaUpload.');
          this.load();
        },
        error: (error: HttpErrorResponse) => this.notifications.error(this.errorMessage(error))
      });
  }

  download(id: number): void {
    window.open(this.api.downloadUrl(id), '_blank', 'noopener');
  }

  requiresReferences(): boolean {
    return this.isDifferentialMode();
  }

  requiresPrenotificationReferences(): boolean {
    return this.form.controls.scenarioType.value === 'IncomingPrenotificationResponse';
  }

  reasonRequiredMissing(): boolean {
    const scenario = `${this.form.controls.scenarioType.value}`;
    return (scenario.includes('Rejection') || scenario.includes('Return')) && !this.form.controls.reasonCode.value?.trim();
  }

  isDifferentialMode(): boolean {
    return this.form.controls.simulationMode.value === 'DifferentialResponses';
  }

  get scenarios(): Array<{ value: NachaInboundSimulationType; label: string }> {
    return this.isDifferentialMode() ? this.differentialScenarios : this.incomingScenarios;
  }

  changeMode(mode: NachaSimulationMode): void {
    const current = this.form.controls.simulationMode.value;
    if (current === mode) {
      return;
    }

    const hasUncommittedChanges = this.form.dirty || this.selectedTransactionIds.size > 0 || this.result !== null;
    if (hasUncommittedChanges && !window.confirm('Cambiar de modo limpiará la selección y los campos incompatibles. ¿Desea continuar?')) {
      return;
    }

    this.form.controls.simulationMode.setValue(mode);
    this.form.controls.scenarioType.setValue(mode === 'DifferentialResponses'
      ? 'IncomingCreditConfirmation'
      : 'IncomingCredit');
    this.form.controls.responseMode.setValue(mode === 'DifferentialResponses' ? 'Approved' : null);
    this.form.controls.reasonCode.setValue('');
    this.form.controls.pendingPrenotificationReferencesText.setValue('');
    this.form.controls.transactionReferencesText.setValue('');
    this.result = null;
    this.selectedTransactionIds.clear();
    this.selectedTransactionReferences.clear();
    this.eligibleTransactions = [];
    this.eligibleTotal = 0;
    this.eligiblePage = 1;
    this.form.markAsPristine();
    if (mode === 'DifferentialResponses') {
      this.loadEligibleTransactions();
    }
    this.loadAvailableCycles();
  }

  loadEligibleTransactions(page = 1): void {
    if (!this.isDifferentialMode()) {
      return;
    }

    this.loadingEligible = true;
    this.eligiblePage = Math.max(1, page);
    this.api.eligibleDifferentialTransactions({
      clearingHouseCode: this.form.controls.clearingHouseCode.value ?? '',
      destinationFinancialInstitutionId: this.form.controls.originFinancialInstitutionId.value ?? undefined,
      fromDate: this.form.controls.businessDate.value ?? undefined,
      state: 'Pending',
      page: this.eligiblePage,
      pageSize: this.eligiblePageSize
    }).pipe(finalize(() => {
      this.loadingEligible = false;
      this.cdr.markForCheck();
    })).subscribe({
      next: (response) => {
        this.eligibleTransactions = response.items ?? [];
        this.eligibleTotal = response.total ?? 0;
      },
      error: (error: HttpErrorResponse) => this.notifications.error(this.errorMessage(error))
    });
  }

  toggleTransaction(item: DifferentialResponseEligibleTransaction): void {
    if (!item.eligible) {
      return;
    }
    if (this.selectedTransactionIds.has(item.id)) {
      this.selectedTransactionIds.delete(item.id);
      this.selectedTransactionReferences.delete(item.id);
    } else {
      this.selectedTransactionIds.add(item.id);
      this.selectedTransactionReferences.set(item.id, item.identifier);
    }
  }

  selectedCount(): number {
    return this.selectedTransactionIds.size;
  }

  selectedReferences(): string[] {
    return Array.from(this.selectedTransactionReferences.values());
  }

  private payload(): GenerateNachaInboundSimulationRequest {
    return {
      simulationMode: this.form.controls.simulationMode.value ?? 'IncomingTransactions',
      clearingHouseCode: this.form.controls.clearingHouseCode.value ?? 'ACHCOL',
      scenarioType: this.form.controls.scenarioType.value ?? 'IncomingCredit',
      originFinancialInstitutionId: Number(this.form.controls.originFinancialInstitutionId.value),
      entriesCount: Number(this.form.controls.entriesCount.value ?? 1),
      amount: Number(this.form.controls.amount.value ?? 0),
      referencePrefix: this.form.controls.referencePrefix.value ?? 'UAT-IN',
      businessDate: this.form.controls.businessDate.value ?? new Date().toISOString().substring(0, 10),
      cycleCode: this.form.controls.cycleCode.value ?? '',
      pendingPrenotificationReferences: this.splitLines(this.form.controls.pendingPrenotificationReferencesText.value),
      transactionReferences: this.isDifferentialMode()
        ? this.selectedReferences()
        : this.splitLines(this.form.controls.transactionReferencesText.value),
      responseMode: this.form.controls.responseMode.value,
      reasonCode: this.form.controls.reasonCode.value || null,
      notes: this.form.controls.notes.value || null
    };
  }

  private splitLines(value?: string | null): string[] {
    return (value ?? '').split(/\r?\n|,/).map((x) => x.trim()).filter(Boolean);
  }

  private errorMessage(error: HttpErrorResponse): string {
    return error.error?.detail || error.error?.message || error.message || 'Error no controlado.';
  }
}

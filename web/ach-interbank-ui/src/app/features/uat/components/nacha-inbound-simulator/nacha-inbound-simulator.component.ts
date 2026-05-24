import { ChangeDetectionStrategy, ChangeDetectorRef, Component, OnInit, inject } from '@angular/core';
import { FormBuilder, Validators } from '@angular/forms';
import { HttpErrorResponse } from '@angular/common/http';
import { finalize } from 'rxjs';
import { SharedModule } from '../../../../shared/shared.module';
import { NotificationService } from '../../../../core/services/notification.service';
import { FinancialInstitutionsApiService } from '../../../transactions/services/financial-institutions-api.service';
import { DestinationInstitution } from '../../../transactions/transactions.models';
import { FinancialInstitutionStatusEnum } from '../../../transactions/transactions.types';
import {
  GenerateNachaInboundSimulationRequest,
  InboundResponseMode,
  NachaInboundSimulationItem,
  NachaInboundSimulationResult,
  NachaInboundSimulationType,
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

  readonly clearingHouses = [
    { value: 'ACHCOL', label: 'ACH Colombia' },
    { value: 'CENIT', label: 'CENIT' }
  ];

  readonly scenarios: Array<{ value: NachaInboundSimulationType; label: string }> = [
    { value: 'IncomingCredit', label: 'Credito entrante' },
    { value: 'IncomingDebit', label: 'Debito entrante' },
    { value: 'IncomingPrenotificationResponse', label: 'Respuesta prenotificacion' },
    { value: 'IncomingCreditConfirmation', label: 'Confirmacion credito' },
    { value: 'IncomingCreditRejection', label: 'Rechazo credito' },
    { value: 'IncomingCreditReturn', label: 'Devolucion credito' },
    { value: 'IncomingDebitConfirmation', label: 'Confirmacion debito' },
    { value: 'IncomingDebitRejection', label: 'Rechazo debito' },
    { value: 'IncomingDebitReturn', label: 'Devolucion debito' }
  ];

  readonly responseModes: Array<{ value: InboundResponseMode; label: string }> = [
    { value: 'Approved', label: 'Aprobada' },
    { value: 'Rejected', label: 'Rechazada' },
    { value: 'Confirmed', label: 'Confirmada' },
    { value: 'Returned', label: 'Devuelta' },
    { value: 'Failed', label: 'Fallida' }
  ];

  readonly form = this.fb.group({
    clearingHouseCode: ['ACHCOL', Validators.required],
    scenarioType: ['IncomingCredit' as NachaInboundSimulationType, Validators.required],
    originFinancialInstitutionId: [null as number | null, Validators.required],
    entriesCount: [1, [Validators.required, Validators.min(1), Validators.max(10)]],
    amount: [1000, [Validators.required, Validators.min(0)]],
    referencePrefix: ['UAT-IN-CRED', Validators.required],
    businessDate: [new Date().toISOString().substring(0, 10), Validators.required],
    cycleCode: ['Ciclo 3', Validators.required],
    pendingPrenotificationReferencesText: [''],
    transactionReferencesText: [''],
    responseMode: [null as InboundResponseMode | null],
    reasonCode: [''],
    notes: ['']
  });

  ngOnInit(): void {
    this.loadFinancialInstitutions();
    this.load();
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
    if (this.form.invalid) {
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
        next: () => this.notifications.success('Solicitud elegible para generar archivo simulado.'),
        error: (error: HttpErrorResponse) => this.notifications.error(this.errorMessage(error))
      });
  }

  generate(): void {
    if (this.form.invalid || this.reasonRequiredMissing() || !this.defaultDestination) {
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
    return `${this.form.controls.scenarioType.value}`.includes('Confirmation')
      || `${this.form.controls.scenarioType.value}`.includes('Rejection')
      || `${this.form.controls.scenarioType.value}`.includes('Return');
  }

  requiresPrenotificationReferences(): boolean {
    return this.form.controls.scenarioType.value === 'IncomingPrenotificationResponse';
  }

  reasonRequiredMissing(): boolean {
    const scenario = `${this.form.controls.scenarioType.value}`;
    return (scenario.includes('Rejection') || scenario.includes('Return')) && !this.form.controls.reasonCode.value?.trim();
  }

  private payload(): GenerateNachaInboundSimulationRequest {
    return {
      clearingHouseCode: this.form.controls.clearingHouseCode.value ?? 'ACHCOL',
      scenarioType: this.form.controls.scenarioType.value ?? 'IncomingCredit',
      originFinancialInstitutionId: Number(this.form.controls.originFinancialInstitutionId.value),
      entriesCount: Number(this.form.controls.entriesCount.value ?? 1),
      amount: Number(this.form.controls.amount.value ?? 0),
      referencePrefix: this.form.controls.referencePrefix.value ?? 'UAT-IN',
      businessDate: this.form.controls.businessDate.value ?? new Date().toISOString().substring(0, 10),
      cycleCode: this.form.controls.cycleCode.value ?? 'Ciclo 3',
      pendingPrenotificationReferences: this.splitLines(this.form.controls.pendingPrenotificationReferencesText.value),
      transactionReferences: this.splitLines(this.form.controls.transactionReferencesText.value),
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

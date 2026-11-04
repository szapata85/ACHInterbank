import {
  AbstractControl,
  FormBuilder,
  ValidationErrors,
  Validators
} from '@angular/forms';
import {
  ChangeDetectionStrategy,
  ChangeDetectorRef,
  Component,
  DestroyRef,
  OnInit,
  inject
} from '@angular/core';
import { HttpErrorResponse } from '@angular/common/http';
import { Router } from '@angular/router';
import { MatButtonModule } from '@angular/material/button';
import { MatButtonToggleModule } from '@angular/material/button-toggle';
import { MatCardModule } from '@angular/material/card';
import { MatCheckboxModule } from '@angular/material/checkbox';
import { MatChipsModule } from '@angular/material/chips';
import { MatNativeDateModule } from '@angular/material/core';
import { MatDatepickerModule } from '@angular/material/datepicker';
import { MatDialog, MatDialogModule } from '@angular/material/dialog';
import { MatDividerModule } from '@angular/material/divider';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatIconModule } from '@angular/material/icon';
import { MatInputModule } from '@angular/material/input';
import { MatProgressBarModule } from '@angular/material/progress-bar';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatSelectModule } from '@angular/material/select';
import { MatTableModule } from '@angular/material/table';
import { MatTooltipModule } from '@angular/material/tooltip';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { finalize, forkJoin, Observable } from 'rxjs';
import { SharedModule } from '../../../../shared/shared.module';
import { NotificationService } from '../../../../core/services/notification.service';
import {
  OperationalConfirmDialogComponent,
  OperationalConfirmDialogData
} from '../../../../shared/components/operational-confirm-dialog.component';
import { DestinationInstitution, FinancialInstitutionStatusEnum } from '../../../../core/models/financial-institution.model';
import { FinancialInstitutionsApiService } from '../../../../core/services/financial-institutions-api.service';
import { ClearingHousesApiService } from '../../../../core/services/clearing-houses-api.service';
import { ClearingHouseOption, NachaProfileOption } from '../../../../core/models/clearing-house.model';
import { ClearingHousesService } from '../../../../core/services/clearing-houses.service';
import { NachaConfigApiService } from '../../../../core/services/nacha-config-api.service';
import { NachaConfigProfileReadModel } from '../../../../core/models/nacha-config.model';
import {
  AchReturnCodeOption,
  AvailableInboundCycle,
  DifferentialResponseEligibleTransaction,
  GenerateNachaInboundSimulationRequest,
  InboundResponseMode,
  NachaInboundSimulationItem,
  NachaInboundSimulationResult,
  NachaInboundSimulationType,
  NachaInboundSimulatorService,
  NachaSimulationMode,
  PrenotificationCandidate,
  PrenotificationContractRequest,
  PrenotificationEligibilityPreview,
  PrenotificationTacitPreview,
  PrenotificationTacitApplyResult,
  PrenotificationOutcome
} from '../../services/nacha-inbound-simulator.service';

const MONEY_PATTERN = /^\d+(?:[.,]\d{1,2})?$/;

function exactMoneyValidator(control: AbstractControl<string | null>): ValidationErrors | null {
  const value = `${control.value ?? ''}`.trim();
  if (!value) {
    return null;
  }
  if (!MONEY_PATTERN.test(value)) {
    return { money: true };
  }

  const normalized = value.replace(',', '.');
  const [whole, decimal = ''] = normalized.split('.');
  const cents = BigInt(whole) * 100n + BigInt(decimal.padEnd(2, '0'));
  return cents > BigInt(Number.MAX_SAFE_INTEGER) ? { moneyRange: true } : null;
}

function todayLocal(): Date {
  const now = new Date();
  return new Date(now.getFullYear(), now.getMonth(), now.getDate());
}

@Component({
  selector: 'app-nacha-inbound-simulator',
  standalone: true,
  imports: [
    SharedModule,
    MatButtonModule,
    MatButtonToggleModule,
    MatCardModule,
    MatCheckboxModule,
    MatChipsModule,
    MatDatepickerModule,
    MatDialogModule,
    MatDividerModule,
    MatFormFieldModule,
    MatIconModule,
    MatInputModule,
    MatNativeDateModule,
    MatProgressBarModule,
    MatProgressSpinnerModule,
    MatSelectModule,
    MatTableModule,
    MatTooltipModule
  ],
  templateUrl: './nacha-inbound-simulator.component.html',
  styleUrls: ['./nacha-inbound-simulator.component.scss'],
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class NachaInboundSimulatorComponent implements OnInit {
  private readonly fb = inject(FormBuilder);
  private readonly api = inject(NachaInboundSimulatorService);
  private readonly financialInstitutionsApi = inject(FinancialInstitutionsApiService);
  private readonly clearingHousesApi = inject(ClearingHousesApiService);
  private readonly clearingHousesService = inject(ClearingHousesService);
  private readonly nachaConfigApi = inject(NachaConfigApiService);
  private readonly notifications = inject(NotificationService);
  private readonly dialog = inject(MatDialog);
  private readonly cdr = inject(ChangeDetectorRef);
  private readonly destroyRef = inject(DestroyRef);
  private readonly router = inject(Router);

  loadingHistory = false;
  loadingInstitutions = false;
  loadingClearingHouses = false;
  loadingProfiles = false;
  loadingCycles = false;
  loadingEligible = false;
  loadingReturnCodes = false;
  generating = false;
  previewing = false;
  hasLoadedHistory = false;
  prenotificationFlow = false;
  pnHouseCode = '';
  pnSearch = '';
  pnPage = 1;
  readonly pnPageSize = 10;
  pnTotal = 0;
  pnLoading = false;
  pnBusy = false;
  pnOptionsLoading = false;
  pnError: string | null = null;
  pnCandidates: PrenotificationCandidate[] = [];
  pnSelected: PrenotificationCandidate | null = null;
  pnOutcome: PrenotificationOutcome = 'Returned';
  pnReasonCode = '';
  pnResponseCycleId = '';
  pnReturnCodes: AchReturnCodeOption[] = [];
  pnCycles: AvailableInboundCycle[] = [];
  pnVirtualDateTime = '';
  pnReturnPreview: PrenotificationEligibilityPreview | null = null;
  pnTacitPreview: PrenotificationTacitPreview | null = null;
  pnPreviewError: string | null = null;
  pnGenerated: NachaInboundSimulationResult | null = null;
  pnApplied: number | null = null;
  private pnIdempotencyKey = '';
  // I07: una consulta anterior no puede sobrescribir la cámara/búsqueda vigente.
  private pnQueryVersion = 0;
  private pnOptionsVersion = 0;
  private pnPreviewVersion = 0;
  downloadingId: number | null = null;

  simulations: NachaInboundSimulationItem[] = [];
  originFinancialInstitutions: DestinationInstitution[] = [];
  defaultDestination: DestinationInstitution | null = null;
  clearingHouses: ClearingHouseOption[] = [];
  activeProfiles: NachaConfigProfileReadModel[] = [];
  availableCycles: AvailableInboundCycle[] = [];
  eligibleTransactions: DifferentialResponseEligibleTransaction[] = [];
  returnCodes: AchReturnCodeOption[] = [];
  result: NachaInboundSimulationResult | null = null;

  historyError: string | null = null;
  institutionCatalogError: string | null = null;
  clearingHouseError: string | null = null;
  profileError: string | null = null;
  cycleError: string | null = null;
  eligibleError: string | null = null;
  returnCodesError: string | null = null;

  eligibleTotal = 0;
  eligiblePage = 1;
  readonly eligiblePageSize = 10;
  readonly selectedTransactionIds = new Set<number>();
  private readonly selectedTransactionReferences = new Map<number, string>();
  private officialProfiles: NachaConfigProfileReadModel[] = [];
  private selectedProfileOptions: NachaProfileOption[] = [];
  private profilesCatalogLoaded = false;

  readonly eligibleColumns = [
    'select',
    'identifier',
    'traceNumber',
    'institution',
    'transactionType',
    'effectiveDate',
    'cycle',
    'account',
    'amount',
    'state',
    'eligibility'
  ];
  readonly historyColumns = ['id', 'clearingHouse', 'scenario', 'file', 'sha256', 'action'];

  readonly incomingScenarios: Array<{ value: NachaInboundSimulationType; label: string }> = [
    { value: 'IncomingCredit', label: 'Crédito entrante' },
    { value: 'IncomingDebit', label: 'Débito entrante' }
  ];
  readonly differentialScenarios: Array<{ value: NachaInboundSimulationType; label: string }> = [
    { value: 'IncomingDebitReturn', label: 'Devolución de débito' },
    { value: 'IncomingCreditReturn', label: 'Devolución de crédito' },
    { value: 'IncomingCreditConfirmation', label: 'Aceptación de crédito' },
    { value: 'IncomingCreditRejection', label: 'Rechazo de crédito' },
    { value: 'IncomingDebitConfirmation', label: 'Aceptación de débito' },
    { value: 'IncomingDebitRejection', label: 'Rechazo de débito' }
  ];
  readonly responseModes: Array<{ value: InboundResponseMode; label: string }> = [
    { value: 'Approved', label: 'Aprobada' },
    { value: 'Rejected', label: 'Rechazada' },
    { value: 'Confirmed', label: 'Confirmada' },
    { value: 'Returned', label: 'Devuelta' },
    { value: 'Failed', label: 'Fallida' }
  ];

  readonly form = this.fb.group({
    simulationMode: this.fb.nonNullable.control<NachaSimulationMode>('DifferentialResponses'),
    clearingHouseCode: this.fb.nonNullable.control('', Validators.required),
    scenarioType: this.fb.nonNullable.control<NachaInboundSimulationType>('IncomingDebitReturn', Validators.required),
    originFinancialInstitutionId: this.fb.control<number | null>(null, Validators.required),
    entriesCount: this.fb.nonNullable.control(1, [Validators.required, Validators.min(1), Validators.max(10)]),
    amount: this.fb.nonNullable.control('', exactMoneyValidator),
    referencePrefix: this.fb.nonNullable.control('', Validators.maxLength(24)),
    businessDate: this.fb.control<Date | null>(todayLocal(), Validators.required),
    cycleCode: this.fb.nonNullable.control('', Validators.required),
    pendingPrenotificationReferencesText: this.fb.nonNullable.control(''),
    responseMode: this.fb.control<InboundResponseMode | null>('Returned'),
    reasonCode: this.fb.nonNullable.control('', Validators.maxLength(20)),
    notes: this.fb.nonNullable.control('', Validators.maxLength(500))
  });

  get scenarios(): Array<{ value: NachaInboundSimulationType; label: string }> {
    return this.isDifferentialMode() ? this.differentialScenarios : this.incomingScenarios;
  }

  get selectedClearingHouse(): ClearingHouseOption | null {
    const code = this.form.controls.clearingHouseCode.value;
    return this.clearingHouses.find((item) => item.code === code) ?? null;
  }

  get selectedOrigin(): DestinationInstitution | null {
    const id = this.form.controls.originFinancialInstitutionId.value;
    return this.originFinancialInstitutions.find((item) => item.id === id) ?? null;
  }

  get operationLabel(): string {
    const scenario = this.form.controls.scenarioType.value;
    return this.scenarios.find((item) => item.value === scenario)?.label ?? scenario;
  }

  get applicableReturnCodes(): AchReturnCodeOption[] {
    const debit = this.form.controls.scenarioType.value === 'IncomingDebitReturn';
    const credit = this.form.controls.scenarioType.value === 'IncomingCreditReturn';
    return this.returnCodes.filter((item) =>
      item.isActive
      && (item.flowType === 'Any' || item.flowType === 'Return')
      && (!debit || item.appliesToDebit)
      && (!credit || item.appliesToCredit)
    );
  }

  get operationalSummary(): string {
    const institution = this.selectedOrigin?.name ?? 'la entidad externa seleccionada';
    const cycle = this.availableCycles.find((item) => item.cycleCode === this.form.controls.cycleCode.value)?.cycleName
      ?? 'el ciclo seleccionado';
    const reason = this.applicableReturnCodes.find((item) => item.code === this.form.controls.reasonCode.value);
    if (this.isReturnScenario()) {
      return `Va a simular una devolución ${reason?.code ?? ''} de ${institution} sobre ${this.simulationCount} transacción(es) originada(s) por CFA. El archivo se generará para ${cycle}.`;
    }
    return `Va a simular ${this.operationLabel.toLowerCase()} de ${institution} sobre ${this.simulationCount} transacción(es), para ${cycle}.`;
  }

  get profileRequiredButUnavailable(): boolean {
    return !!this.selectedClearingHouse?.requiresNachaProfile
      && this.profilesCatalogLoaded
      && !this.loadingProfiles
      && this.activeProfiles.length === 0;
  }

  get canExecute(): boolean {
    return this.form.valid
      && !this.generating
      && !this.previewing
      && !this.loadingReturnCodes
      && (!this.isReturnScenario() || (!this.returnCodesError && this.applicableReturnCodes.length > 0))
      && !!this.defaultDestination
      && !this.profileRequiredButUnavailable
      && (!this.isDifferentialMode() || this.selectedTransactionIds.size > 0);
  }

  get simulationCount(): number {
    return this.isDifferentialMode()
      ? this.selectedTransactionIds.size
      : this.form.controls.entriesCount.value;
  }

  ngOnInit(): void {
    this.configureConditionalValidators();
    this.loadOfficialProfiles();
    this.loadFinancialInstitutions();
    this.loadClearingHouses();
    this.loadHistory();
  }

  // I06: flujo guiado sobre una sola prenotificación, dentro de la SPA existente.
  openPrenotifications(): void {
    this.prenotificationFlow = true;
    this.pnHouseCode = this.form.controls.clearingHouseCode.value || this.clearingHouses[0]?.code || '';
    this.pnSearch = '';
    this.pnOutcome = 'Returned';
    this.resetPnSelection();
    this.loadPrenotifications();
  }

  closePrenotifications(): void {
    this.prenotificationFlow = false;
    this.pnQueryVersion++;
    this.pnLoading = false;
    this.pnCandidates = [];
    this.pnTotal = 0;
    this.resetPnSelection();
  }

  changePnHouse(code: string): void {
    if (this.pnBusy) { return; }
    this.pnHouseCode = code;
    this.resetPnSelection();
    this.pnCandidates = [];
    this.loadPrenotifications();
  }

  loadPrenotifications(page = 1): void {
    if (!this.pnHouseCode) {
      this.pnCandidates = [];
      this.pnTotal = 0;
      return;
    }
    this.pnPage = Math.max(1, page);
    const version = ++this.pnQueryVersion;
    this.pnLoading = true;
    this.pnError = null;
    this.pnCandidates = [];
    this.pnTotal = 0;
    const house = this.pnHouseCode;
    const search = this.pnSearch.trim();
    this.api.listOriginatedPrenotifications({
      clearingHouseCode: house,
      search: search || undefined,
      page: this.pnPage,
      pageSize: this.pnPageSize
    }).pipe(
      takeUntilDestroyed(this.destroyRef),
      finalize(() => {
        if (version === this.pnQueryVersion) { this.pnLoading = false; }
        this.cdr.markForCheck();
      })
    ).subscribe({
      next: (pageResult) => {
        if (version !== this.pnQueryVersion || !this.prenotificationFlow
            || house !== this.pnHouseCode || search !== this.pnSearch.trim()) { return; }
        this.pnCandidates = pageResult.items ?? [];
        this.pnTotal = pageResult.total;
      },
      error: (error: HttpErrorResponse) => {
        if (version !== this.pnQueryVersion || !this.prenotificationFlow) { return; }
        this.pnCandidates = [];
        this.pnTotal = 0;
        this.pnError = this.errorMessage(error, 'No se pudieron consultar las prenotificaciones.');
      }
    });
  }

  selectPrenotification(candidate: PrenotificationCandidate): void {
    if (this.pnBusy || this.pnLoading) { return; }
    this.resetPnSelection();
    this.pnSelected = candidate;
    this.pnIdempotencyKey = crypto.randomUUID();
    this.loadPnOptions(candidate);
  }

  changePnOutcome(outcome: PrenotificationOutcome): void {
    this.pnOutcome = outcome;
    this.clearPnPreview();
  }

  changePnDetails(): void { this.clearPnPreview(); }

  get pnApplicableCodes(): AchReturnCodeOption[] {
    return this.pnReturnCodes.filter(code => code.isActive
      && code.appliesToPrenotification && (code.flowType === 'Return' || code.flowType === 'Any'));
  }

  get pnCanPreview(): boolean {
    if (!this.pnSelected || this.pnBusy || this.pnOptionsLoading) { return false; }
    if (this.pnOutcome === 'Returned') {
      return this.pnSelected.eligibleForReturn && !!this.pnReasonCode && !!this.pnResponseCycleId;
    }
    return this.pnSelected.violations.length === 0;
  }

  get pnCanCommit(): boolean {
    if (!this.pnCanPreview || !!this.pnPreviewError || this.pnGenerated !== null || this.pnApplied !== null) {
      return false;
    }
    return this.pnOutcome === 'Returned'
      ? this.pnReturnPreview?.businessEligible === true
      : !this.pnVirtualDateTime && this.pnTacitPreview?.executionEnabled === true
        && this.pnTacitPreview.items.length === 1
        && this.pnTacitPreview.items[0].eligibleAtRealTime;
  }

  previewPn(): void {
    if (!this.pnCanPreview) { return; }
    const payload = this.pnContract();
    this.pnBusy = true;
    this.clearPnPreview();
    // La respuesta solo sirve para la seleccion, resultado y campos que se validaron.
    const version = this.pnPreviewVersion;
    const outcome = this.pnOutcome;
    const selectedId = this.pnSelected?.originalPrenotificationId;
    const houseCode = this.pnHouseCode;
    const isCurrent = () => version === this.pnPreviewVersion
      && this.prenotificationFlow
      && this.pnSelected?.originalPrenotificationId === selectedId
      && this.pnHouseCode === houseCode
      && this.pnOutcome === outcome;
    const virtual = outcome === 'NoReturnByDeadline' && this.pnVirtualDateTime
      ? new Date(this.pnVirtualDateTime) : null;
    if (virtual && Number.isNaN(virtual.getTime())) {
      this.pnBusy = false;
      this.pnPreviewError = 'Fecha de prueba inválida.';
      return;
    }
    const request: Observable<PrenotificationEligibilityPreview | PrenotificationTacitPreview> = outcome === 'Returned'
      ? this.api.previewPrenotificationReturn(payload)
      : this.api.previewTacitAcceptance(payload, virtual?.toISOString() ?? null);
    request.pipe(takeUntilDestroyed(this.destroyRef), finalize(() => {
      this.pnBusy = false;
      this.cdr.markForCheck();
    })).subscribe({
      next: (result) => {
        if (!isCurrent()) { return; }
        if (outcome === 'Returned') {
          const preview = result as PrenotificationEligibilityPreview;
          this.pnReturnPreview = preview;
          this.pnPreviewError = preview.businessEligible ? null
            : this.pnIssues(preview.violations.map(issue => issue.message));
        } else {
          const preview = result as PrenotificationTacitPreview;
          this.pnTacitPreview = preview;
          const issues = [...preview.violations, ...preview.items.flatMap(item => item.violations)];
          this.pnPreviewError = issues.length ? this.pnIssues(issues.map(issue => issue.message)) : null;
        }
        this.cdr.markForCheck();
      },
      error: (error: HttpErrorResponse) => {
        if (!isCurrent()) { return; }
        this.pnPreviewError = this.errorMessage(error, 'No se pudo validar la prenotificación.');
        this.cdr.markForCheck();
      }
    });
  }

  confirmPn(): void {
    if (!this.pnCanCommit) { return; }
    const isReturn = this.pnOutcome === 'Returned';
    const data: OperationalConfirmDialogData = {
      title: isReturn ? 'Generar devolución de prenotificación' : 'Aplicar aceptación tácita',
      message: isReturn
        ? 'Se generará un archivo NACHA-M UAT, sin importarlo automáticamente. Descárguelo y cárguelo en NachaUpload.'
        : 'Se actualizará el estado de la prenotificación en este entorno UAT únicamente si el plazo real venció. Esta acción no genera archivos.',
      confirmLabel: isReturn ? 'Generar archivo' : 'Aplicar vencimiento',
      icon: isReturn ? 'assignment_return' : 'schedule'
    };
    this.dialog.open(OperationalConfirmDialogComponent, {
      data, width: 'min(92vw, 520px)', disableClose: true
    }).afterClosed().pipe(takeUntilDestroyed(this.destroyRef)).subscribe(confirmed => {
      if (confirmed && this.pnCanCommit) { this.executePn(); }
    });
  }

  private executePn(): void {
    this.pnBusy = true;
    const isReturn = this.pnOutcome === 'Returned';
    const request = this.pnContract();
    const action: Observable<NachaInboundSimulationResult | PrenotificationTacitApplyResult> = isReturn
      ? this.api.generatePrenotificationReturn(request)
      : this.api.applyDueTacitAcceptance(request);
    action.pipe(takeUntilDestroyed(this.destroyRef), finalize(() => {
      this.pnBusy = false;
      this.cdr.markForCheck();
    })).subscribe({
      next: (response) => {
        if (isReturn) {
          this.pnGenerated = response as NachaInboundSimulationResult;
          this.notifications.success('Archivo generado. Descárguelo y procéselo mediante NachaUpload.');
          this.loadHistory();
        } else {
          const applied = response as { applied: number; alreadyApplied: number };
          this.pnApplied = applied.applied;
          this.notifications.success(applied.applied ? 'Aceptación tácita aplicada.' : 'Resultado ya aplicado previamente.');
        }
        // El backend conserva la autoridad; refrescar elegibilidad después de la acción.
        this.loadPrenotifications(this.pnPage);
      },
      error: (error: HttpErrorResponse) => {
        this.pnPreviewError = this.errorMessage(error, 'No se pudo completar la simulación.');
        this.clearPnPreviewResults();
      }
    });
  }

  private loadPnOptions(candidate: PrenotificationCandidate): void {
    const version = ++this.pnOptionsVersion;
    this.pnOptionsLoading = true;
    forkJoin({
      codes: this.api.returnCodes(candidate.clearingHouseCode),
      cycles: this.api.availableCycles({
        clearingHouseCode: candidate.clearingHouseCode,
        processingDate: candidate.originalBusinessDate,
        scenarioType: 'IncomingPrenotificationReturn'
      })
    }).pipe(takeUntilDestroyed(this.destroyRef), finalize(() => {
      if (version === this.pnOptionsVersion) { this.pnOptionsLoading = false; }
      this.cdr.markForCheck();
    })).subscribe({
      next: result => {
        if (version !== this.pnOptionsVersion || !this.prenotificationFlow
            || this.pnSelected?.originalPrenotificationId !== candidate.originalPrenotificationId
            || this.pnHouseCode !== candidate.clearingHouseCode) { return; }
        this.pnReturnCodes = result.codes ?? [];
        this.pnCycles = (result.cycles ?? []).filter(cycle => cycle.cycleId !== candidate.originalCycleId);
      },
      error: (error: HttpErrorResponse) => {
        if (version !== this.pnOptionsVersion || !this.prenotificationFlow) { return; }
        this.pnPreviewError = this.errorMessage(error, 'No se pudieron cargar las causales o ciclos de devolución.');
      }
    });
  }

  private pnContract(): PrenotificationContractRequest {
    const candidate = this.pnSelected!;
    return {
      outcome: this.pnOutcome,
      clearingHouseCode: candidate.clearingHouseCode,
      originalPrenotificationIds: [candidate.originalPrenotificationId],
      originalCycleId: candidate.originalCycleId,
      responseCycleId: this.pnOutcome === 'Returned' ? this.pnResponseCycleId : null,
      businessDate: candidate.originalBusinessDate,
      respondingFinancialInstitutionId: candidate.respondingFinancialInstitutionId,
      reasonCode: this.pnOutcome === 'Returned' ? this.pnReasonCode : null,
      idempotencyKey: this.pnOutcome === 'Returned' ? this.pnIdempotencyKey : null
    };
  }

  resetPnSelectionForSearch(): void { this.resetPnSelection(); }

  loadPnPage(page: number): void {
    this.resetPnSelection();
    this.loadPrenotifications(page);
  }

  pnIssuesText(candidate: PrenotificationCandidate): string {
    if (candidate.violations.length) {
      return this.pnIssues(candidate.violations.map(issue => issue.message));
    }
    return this.pnOutcome === 'Returned'
      ? 'El plazo reglamentario de devolución ya venció. Puede revisar el resultado sin devolución.'
      : 'Esta prenotificación no tiene un intento pendiente válido para simular el vencimiento.';
  }

  private resetPnSelection(): void {
    this.pnOptionsVersion++;
    this.pnOptionsLoading = false;
    this.pnSelected = null;
    this.pnReasonCode = '';
    this.pnResponseCycleId = '';
    this.pnReturnCodes = [];
    this.pnCycles = [];
    this.pnVirtualDateTime = '';
    this.pnGenerated = null;
    this.pnApplied = null;
    this.pnIdempotencyKey = '';
    this.clearPnPreview();
  }

  private clearPnPreview(): void {
    this.pnPreviewVersion++;
    this.clearPnPreviewResults();
    this.pnPreviewError = null;
  }

  private clearPnPreviewResults(): void {
    this.pnReturnPreview = null;
    this.pnTacitPreview = null;
  }

  pnIssues(messages: string[]): string {
    return messages.filter(Boolean).slice(0, 3).join(' · ') || 'La operación no es elegible.';
  }

  loadFinancialInstitutions(): void {
    this.loadingInstitutions = true;
    this.institutionCatalogError = null;
    this.financialInstitutionsApi.getAll(false)
      .pipe(
        takeUntilDestroyed(this.destroyRef),
        finalize(() => {
          this.loadingInstitutions = false;
          this.cdr.markForCheck();
        })
      )
      .subscribe({
        next: (items) => {
          const active = (items ?? []).filter((item) => item.status === FinancialInstitutionStatusEnum.Active);
          this.originFinancialInstitutions = active.filter((item) => item.isDefaultSource !== true);
          const defaults = active.filter((item) => item.isDefaultSource === true);
          this.defaultDestination = defaults.length === 1 ? defaults[0] : null;
          this.institutionCatalogError = defaults.length === 0
            ? 'No existe una institución activa marcada como fuente predeterminada para recibir la simulación.'
            : defaults.length > 1
              ? 'Existen varias instituciones activas marcadas como fuente predeterminada. Corrija el catálogo antes de simular.'
              : null;
        },
        error: (error: HttpErrorResponse) => {
          this.originFinancialInstitutions = [];
          this.defaultDestination = null;
          this.institutionCatalogError = this.errorMessage(error, 'No fue posible cargar las instituciones financieras.');
        }
      });
  }

  loadClearingHouses(): void {
    this.loadingClearingHouses = true;
    this.clearingHouseError = null;
    this.clearingHousesApi.list()
      .pipe(
        takeUntilDestroyed(this.destroyRef),
        finalize(() => {
          this.loadingClearingHouses = false;
          this.cdr.markForCheck();
        })
      )
      .subscribe({
        next: (items) => {
          this.clearingHouses = (items ?? []).filter((item) => !!item.code);
          if (this.prenotificationFlow && !this.pnHouseCode) {
            this.pnHouseCode = this.clearingHouses[0]?.code || '';
            this.loadPrenotifications();
          }
          if (!this.form.controls.clearingHouseCode.value && this.clearingHouses.length === 1) {
            this.form.controls.clearingHouseCode.setValue(this.clearingHouses[0].code ?? '');
            this.clearingHouseChanged();
          }
        },
        error: (error: HttpErrorResponse) => {
          this.clearingHouses = [];
          this.clearingHouseError = this.errorMessage(error, 'No fue posible cargar las cámaras operativas.');
        }
      });
  }

  private loadOfficialProfiles(): void {
    this.nachaConfigApi.listarPerfilesReadOnly()
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (profiles) => {
          this.officialProfiles = (profiles ?? []).filter((profile) =>
            profile.isOfficialModel
            && profile.isPublished
            && profile.isCurrent
          );
          this.profilesCatalogLoaded = true;
          this.resolveActiveProfiles();
          this.cdr.markForCheck();
        },
        error: (error: HttpErrorResponse) => {
          this.profilesCatalogLoaded = true;
          this.profileError = this.errorMessage(error, 'No fue posible consultar los perfiles oficiales NACHA-M.');
          this.cdr.markForCheck();
        }
      });
  }

  clearingHouseChanged(): void {
    this.form.controls.cycleCode.setValue('');
    this.activeProfiles = [];
    this.selectedProfileOptions = [];
    this.profileError = null;
    const code = this.form.controls.clearingHouseCode.value;
    if (!code) {
      this.availableCycles = [];
      return;
    }

    this.loadingProfiles = true;
    this.clearingHousesService.profiles(code)
      .pipe(
        takeUntilDestroyed(this.destroyRef),
        finalize(() => {
          this.loadingProfiles = false;
          this.resolveActiveProfiles();
          this.cdr.markForCheck();
        })
      )
      .subscribe({
        next: (profiles) => {
          this.selectedProfileOptions = (profiles ?? []).filter((profile) =>
            profile.isPublished !== false && profile.isCurrent !== false
          );
          this.resolveActiveProfiles();
        },
        error: (error: HttpErrorResponse) => {
          this.selectedProfileOptions = [];
          this.profileError = this.errorMessage(error, 'No fue posible resolver el perfil NACHA-M de la cámara.');
        }
      });
    this.loadAvailableCycles();
    this.loadReturnCodes();
    if (this.isDifferentialMode()) {
      this.loadEligibleTransactions();
    }
  }

  simulationContextChanged(): void {
    this.synchronizeResponseMode();
    this.configureConditionalValidators();
    this.form.controls.cycleCode.setValue('');
    this.result = null;
    this.loadAvailableCycles();
    this.loadReturnCodes();
    if (this.isDifferentialMode()) {
      this.selectedTransactionIds.clear();
      this.selectedTransactionReferences.clear();
      this.loadEligibleTransactions();
    }
  }

  loadReturnCodes(): void {
    const clearingHouseCode = this.form.controls.clearingHouseCode.value;
    if (!clearingHouseCode || !this.isReturnScenario()) {
      this.returnCodes = [];
      this.returnCodesError = null;
      return;
    }

    this.loadingReturnCodes = true;
    this.returnCodesError = null;
    this.api.returnCodes(clearingHouseCode)
      .pipe(
        takeUntilDestroyed(this.destroyRef),
        finalize(() => {
          this.loadingReturnCodes = false;
          this.cdr.markForCheck();
        })
      )
      .subscribe({
        next: (items) => {
          this.returnCodes = items ?? [];
          const selected = this.form.controls.reasonCode.value;
          if (selected && !this.applicableReturnCodes.some((item) => item.code === selected)) {
            this.form.controls.reasonCode.setValue('');
          }
        },
        error: (error: HttpErrorResponse) => {
          this.returnCodes = [];
          this.returnCodesError = this.errorMessage(error, 'No fue posible consultar las causales de devolución.');
        }
      });
  }

  loadAvailableCycles(): void {
    const clearingHouseCode = this.form.controls.clearingHouseCode.value;
    const processingDate = this.toLocalDate(this.form.controls.businessDate.value);
    const scenarioType = this.form.controls.scenarioType.value;
    if (!clearingHouseCode || !processingDate) {
      this.availableCycles = [];
      this.form.controls.cycleCode.setValue('');
      return;
    }

    const selected = this.form.controls.cycleCode.value;
    this.loadingCycles = true;
    this.cycleError = null;
    this.api.availableCycles({ clearingHouseCode, processingDate, scenarioType })
      .pipe(
        takeUntilDestroyed(this.destroyRef),
        finalize(() => {
          this.loadingCycles = false;
          this.cdr.markForCheck();
        })
      )
      .subscribe({
        next: (items) => {
          this.availableCycles = items ?? [];
          if (selected && !this.availableCycles.some((item) => item.cycleCode === selected)) {
            this.form.controls.cycleCode.setValue('');
          } else if (!selected && this.availableCycles.length === 1) {
            this.form.controls.cycleCode.setValue(this.availableCycles[0].cycleCode);
          }
        },
        error: (error: HttpErrorResponse) => {
          this.availableCycles = [];
          this.form.controls.cycleCode.setValue('');
          this.cycleError = this.errorMessage(error, 'No fue posible consultar los ciclos disponibles.');
        }
      });
  }

  loadEligibleTransactions(page = 1): void {
    if (!this.isDifferentialMode()) {
      return;
    }

    this.loadingEligible = true;
    this.eligibleError = null;
    this.eligiblePage = Math.max(1, page);
    this.api.eligibleDifferentialTransactions({
      clearingHouseCode: this.form.controls.clearingHouseCode.value,
      destinationFinancialInstitutionId: this.form.controls.originFinancialInstitutionId.value ?? undefined,
      fromDate: this.toLocalDate(this.form.controls.businessDate.value) ?? undefined,
      state: 'Pending',
      page: this.eligiblePage,
      pageSize: this.eligiblePageSize
    })
      .pipe(
        takeUntilDestroyed(this.destroyRef),
        finalize(() => {
          this.loadingEligible = false;
          this.cdr.markForCheck();
        })
      )
      .subscribe({
        next: (response) => {
          this.eligibleTransactions = response.items ?? [];
          this.eligibleTotal = response.total ?? 0;
        },
        error: (error: HttpErrorResponse) => {
          this.eligibleTransactions = [];
          this.eligibleTotal = 0;
          this.eligibleError = this.errorMessage(error, 'No fue posible consultar las operaciones elegibles.');
        }
      });
  }

  loadHistory(): void {
    if (this.loadingHistory) {
      return;
    }
    this.loadingHistory = true;
    this.historyError = null;
    this.api.list()
      .pipe(
        takeUntilDestroyed(this.destroyRef),
        finalize(() => {
          this.loadingHistory = false;
          this.hasLoadedHistory = true;
          this.cdr.markForCheck();
        })
      )
      .subscribe({
        next: (items) => this.simulations = items ?? [],
        error: (error: HttpErrorResponse) => {
          this.simulations = [];
          this.historyError = this.errorMessage(error, 'No fue posible cargar las simulaciones recientes.');
        }
      });
  }

  requestModeChange(mode: NachaSimulationMode): void {
    if (this.form.controls.simulationMode.value === mode) {
      return;
    }
    if (!this.form.dirty && this.selectedTransactionIds.size === 0 && !this.result) {
      this.applyMode(mode);
      return;
    }

    const data: OperationalConfirmDialogData = {
      title: 'Cambiar modo de simulación',
      message: 'El cambio limpiará la selección y los campos que no son compatibles con el nuevo modo.',
      confirmLabel: 'Cambiar modo',
      icon: 'swap_horiz'
    };
    this.dialog.open(OperationalConfirmDialogComponent, { data, width: 'min(92vw, 520px)' })
      .afterClosed()
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe((confirmed) => {
        if (confirmed) {
          this.applyMode(mode);
        }
      });
  }

  preview(): void {
    if (!this.validateBeforeAction() || this.previewing || this.generating) {
      return;
    }

    this.previewing = true;
    this.api.preview(this.payload())
      .pipe(
        takeUntilDestroyed(this.destroyRef),
        finalize(() => {
          this.previewing = false;
          this.cdr.markForCheck();
        })
      )
      .subscribe({
        next: (response) => {
          if (response.eligible) {
            this.notifications.success('La configuración es elegible para generar la simulación.');
          } else {
            this.notifications.error(this.safeText(response.message, 'La solicitud no es elegible.'));
          }
        },
        error: (error: HttpErrorResponse) =>
          this.notifications.error(this.errorMessage(error, 'No fue posible validar la configuración.'))
      });
  }

  confirmGeneration(): void {
    if (!this.validateBeforeAction()) {
      return;
    }
    const data: OperationalConfirmDialogData = {
      title: 'Generar simulación NACHA-M',
      message: `Se generará un archivo UAT con ${this.simulationCount} operación(es). El archivo no se transmitirá automáticamente.`,
      confirmLabel: 'Generar archivo',
      icon: 'science'
    };
    this.dialog.open(OperationalConfirmDialogComponent, { data, width: 'min(92vw, 560px)', disableClose: true })
      .afterClosed()
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe((confirmed) => {
        if (confirmed) {
          this.executeGenerate();
        }
      });
  }

  executeGenerate(): void {
    if (!this.validateBeforeAction() || this.generating || this.previewing) {
      return;
    }

    this.generating = true;
    this.result = null;
    this.api.generate(this.payload())
      .pipe(
        takeUntilDestroyed(this.destroyRef),
        finalize(() => {
          this.generating = false;
          this.cdr.markForCheck();
        })
      )
      .subscribe({
        next: (result) => {
          this.result = result;
          this.notifications.success('La simulación NACHA-M se generó correctamente.');
          this.loadHistory();
        },
        error: (error: HttpErrorResponse) =>
          this.notifications.error(this.errorMessage(error, 'No fue posible generar la simulación.'))
      });
  }

  resetSimulation(): void {
    const clearingHouseCode = this.form.controls.clearingHouseCode.value;
    this.form.reset({
      simulationMode: 'DifferentialResponses',
      clearingHouseCode,
      scenarioType: 'IncomingDebitReturn',
      originFinancialInstitutionId: null,
      entriesCount: 1,
      amount: '',
      referencePrefix: '',
      businessDate: todayLocal(),
      cycleCode: '',
      pendingPrenotificationReferencesText: '',
      responseMode: 'Returned',
      reasonCode: '',
      notes: ''
    });
    this.result = null;
    this.selectedTransactionIds.clear();
    this.selectedTransactionReferences.clear();
    this.eligibleTransactions = [];
    this.eligibleTotal = 0;
    this.configureConditionalValidators();
    this.loadAvailableCycles();
    this.loadReturnCodes();
    this.cdr.markForCheck();
  }

  toggleTransaction(item: DifferentialResponseEligibleTransaction): void {
    if (!item.eligible || this.generating) {
      return;
    }
    if (this.selectedTransactionIds.has(item.id)) {
      this.selectedTransactionIds.delete(item.id);
      this.selectedTransactionReferences.delete(item.id);
    } else {
      this.selectedTransactionIds.add(item.id);
      this.selectedTransactionReferences.set(item.id, item.identifier);
    }
    this.cdr.markForCheck();
  }

  download(id: number, fileName?: string): void {
    if (this.downloadingId !== null) { return; }
    this.downloadingId = id;
    this.api.downloadFile(id).pipe(
      takeUntilDestroyed(this.destroyRef),
      finalize(() => { this.downloadingId = null; this.cdr.markForCheck(); })
    ).subscribe({
      next: blob => {
        const url = URL.createObjectURL(blob);
        const a = document.createElement('a');
        a.href = url;
        a.download = (fileName || this.pnGenerated?.fileName || this.result?.fileName || `nacha-uat-${id}.txt`)
          .replace(/[^a-zA-Z0-9._-]/g, '_');
        document.body.appendChild(a);
        a.click();
        a.remove();
        window.setTimeout(() => URL.revokeObjectURL(url), 1000);
      },
      error: (error: HttpErrorResponse) =>
        this.notifications.error(this.errorMessage(error, 'No fue posible descargar el archivo.'))
    });
  }

  goToUpload(): void {
    void this.router.navigate(['/transactions/nacha-upload']);
  }

  copyIdentifier(value: string | number, label: string): void {
    if (!navigator.clipboard) {
      this.notifications.warning('El portapapeles no está disponible en este navegador.');
      return;
    }
    void navigator.clipboard.writeText(String(value))
      .then(() => this.notifications.success(`${label} copiado.`))
      .catch(() => this.notifications.error(`No fue posible copiar ${label.toLowerCase()}.`));
  }

  isDifferentialMode(): boolean {
    return this.form.controls.simulationMode.value === 'DifferentialResponses';
  }

  requiresPrenotificationReferences(): boolean {
    return this.form.controls.scenarioType.value === 'IncomingPrenotificationResponse';
  }

  isReturnScenario(): boolean {
    return this.form.controls.scenarioType.value === 'IncomingCreditReturn'
      || this.form.controls.scenarioType.value === 'IncomingDebitReturn';
  }

  formatProcessingDate(value: string): string {
    const [year, month, day] = value.substring(0, 10).split('-');
    return year && month && day ? `${day}/${month}/${year}` : value;
  }

  private resolveActiveProfiles(): void {
    const allowedIds = new Set(this.selectedProfileOptions.map((profile) => profile.id));
    this.activeProfiles = this.officialProfiles.filter((profile) => allowedIds.has(profile.profileId));
    if (this.selectedProfileOptions.length > 0 && this.profilesCatalogLoaded && this.activeProfiles.length === 0) {
      this.profileError = 'La cámara no tiene un perfil oficial, publicado y vigente en el modelo nacha-config.';
    } else if (this.activeProfiles.length > 0) {
      this.profileError = null;
    }
  }

  private applyMode(mode: NachaSimulationMode): void {
    this.form.controls.simulationMode.setValue(mode);
    this.form.controls.scenarioType.setValue(
      mode === 'DifferentialResponses' ? 'IncomingDebitReturn' : 'IncomingCredit'
    );
    this.form.controls.responseMode.setValue(mode === 'DifferentialResponses' ? 'Returned' : null);
    this.form.controls.reasonCode.setValue('');
    this.form.controls.pendingPrenotificationReferencesText.setValue('');
    this.form.controls.cycleCode.setValue('');
    this.result = null;
    this.selectedTransactionIds.clear();
    this.selectedTransactionReferences.clear();
    this.eligibleTransactions = [];
    this.eligibleTotal = 0;
    this.eligiblePage = 1;
    this.configureConditionalValidators();
    this.form.markAsPristine();
    this.loadAvailableCycles();
    this.loadReturnCodes();
    if (mode === 'DifferentialResponses') {
      this.loadEligibleTransactions();
    }
    this.cdr.markForCheck();
  }

  private configureConditionalValidators(): void {
    const reason = this.form.controls.reasonCode;
    const response = this.form.controls.responseMode;
    const references = this.form.controls.pendingPrenotificationReferencesText;
    const scenario = this.form.controls.scenarioType.value;
    const reasonRequired = scenario.includes('Rejection') || scenario.includes('Return');
    const incomingTransaction = !this.isDifferentialMode();

    reason.setValidators(reasonRequired
      ? [Validators.required, Validators.maxLength(20)]
      : [Validators.maxLength(20)]);
    response.setValidators(this.isDifferentialMode() ? Validators.required : null);
    this.form.controls.amount.setValidators(incomingTransaction
      ? [Validators.required, exactMoneyValidator]
      : exactMoneyValidator);
    this.form.controls.referencePrefix.setValidators(incomingTransaction
      ? [Validators.required, Validators.maxLength(24)]
      : Validators.maxLength(24));
    references.setValidators(this.requiresPrenotificationReferences() ? Validators.required : null);
    reason.updateValueAndValidity({ emitEvent: false });
    response.updateValueAndValidity({ emitEvent: false });
    references.updateValueAndValidity({ emitEvent: false });
    this.form.controls.amount.updateValueAndValidity({ emitEvent: false });
    this.form.controls.referencePrefix.updateValueAndValidity({ emitEvent: false });
  }

  scenarioLabel(value: string): string {
    if (value === 'IncomingPrenotificationReturn' || value === '10') { return 'Devolución de prenotificación'; }
    if (value === 'IncomingPrenotificationResponse' || value === '3') { return 'Respuesta de prenotificación (legado)'; }
    return [...this.incomingScenarios, ...this.differentialScenarios]
      .find((item) => item.value === value)?.label ?? 'Respuesta de contraparte';
  }

  private synchronizeResponseMode(): void {
    const scenario = this.form.controls.scenarioType.value;
    this.form.controls.responseMode.setValue(
      scenario.includes('Return') ? 'Returned'
        : scenario.includes('Rejection') ? 'Rejected'
          : scenario.includes('Confirmation') ? 'Approved'
            : this.form.controls.responseMode.value
    );
    this.form.controls.reasonCode.setValue('');
  }

  private validateBeforeAction(): boolean {
    this.configureConditionalValidators();
    if (this.form.invalid || !this.defaultDestination || this.profileRequiredButUnavailable
      || (this.isDifferentialMode() && this.selectedTransactionIds.size === 0)) {
      this.form.markAllAsTouched();
      if (!this.defaultDestination) {
        this.notifications.error(this.institutionCatalogError ?? 'La institución receptora no está disponible.');
      } else if (this.profileRequiredButUnavailable) {
        this.notifications.error(this.profileError ?? 'No hay un perfil oficial vigente para la cámara.');
      } else if (this.isDifferentialMode() && this.selectedTransactionIds.size === 0) {
        this.notifications.warning('Seleccione al menos una operación elegible.');
      }
      this.cdr.markForCheck();
      return false;
    }
    return true;
  }

  private payload(): GenerateNachaInboundSimulationRequest {
    return {
      simulationMode: this.form.controls.simulationMode.value,
      clearingHouseCode: this.form.controls.clearingHouseCode.value,
      scenarioType: this.form.controls.scenarioType.value,
      originFinancialInstitutionId: this.form.controls.originFinancialInstitutionId.value ?? 0,
      entriesCount: this.form.controls.entriesCount.value,
      amount: this.isDifferentialMode() ? 0 : this.parseMoney(this.form.controls.amount.value),
      referencePrefix: this.form.controls.referencePrefix.value.trim(),
      businessDate: this.toLocalDate(this.form.controls.businessDate.value) ?? '',
      cycleCode: this.form.controls.cycleCode.value,
      pendingPrenotificationReferences: this.splitLines(
        this.form.controls.pendingPrenotificationReferencesText.value
      ),
      transactionReferences: this.isDifferentialMode()
        ? Array.from(this.selectedTransactionReferences.values())
        : [],
      responseMode: this.form.controls.responseMode.value,
      reasonCode: this.form.controls.reasonCode.value.trim() || null,
      notes: this.form.controls.notes.value.trim() || null
    };
  }

  private parseMoney(value: string): number {
    const normalized = value.trim().replace(',', '.');
    const [whole, decimal = ''] = normalized.split('.');
    const cents = BigInt(whole) * 100n + BigInt(decimal.padEnd(2, '0'));
    return Number(cents) / 100;
  }

  private toLocalDate(value: Date | null): string | null {
    if (!value || Number.isNaN(value.getTime())) {
      return null;
    }
    const year = value.getFullYear();
    const month = `${value.getMonth() + 1}`.padStart(2, '0');
    const day = `${value.getDate()}`.padStart(2, '0');
    return `${year}-${month}-${day}`;
  }

  private splitLines(value: string): string[] {
    return value.split(/\r?\n|,/).map((item) => item.trim()).filter(Boolean);
  }

  private errorMessage(error: HttpErrorResponse, fallback: string): string {
    const detail = typeof error.error?.detail === 'string'
      ? error.error.detail
      : typeof error.error?.message === 'string'
        ? error.error.message
        : typeof error.message === 'string'
          ? error.message
          : '';
    return this.safeText(detail, fallback);
  }

  private safeText(value: string | null | undefined, fallback: string): string {
    const sanitized = `${value ?? ''}`
      .replace(/[\r\n\t]+/g, ' ')
      .replace(/\s{2,}/g, ' ')
      .trim();
    return sanitized ? sanitized.slice(0, 300) : fallback;
  }
}

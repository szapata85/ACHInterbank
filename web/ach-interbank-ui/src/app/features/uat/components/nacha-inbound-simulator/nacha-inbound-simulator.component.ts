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
import { finalize } from 'rxjs';
import { SharedModule } from '../../../../shared/shared.module';
import { NotificationService } from '../../../../core/services/notification.service';
import {
  OperationalConfirmDialogComponent,
  OperationalConfirmDialogData
} from '../../../../shared/components/operational-confirm-dialog.component';
import { FinancialInstitutionsApiService } from '../../../transactions/services/financial-institutions-api.service';
import { DestinationInstitution } from '../../../transactions/transactions.models';
import { FinancialInstitutionStatusEnum } from '../../../transactions/transactions.types';
import { ClearingHousesApiService } from '../../../ach-cycles/services/ach-cycles-api.service';
import { ClearingHouseOption } from '../../../ach-cycles/models/ach-cycle.model';
import { ClearingHousesService } from '../../../clearing-houses/clearing-houses.service';
import { NachaProfileOption } from '../../../clearing-houses/clearing-houses.models';
import { NachaConfigApiService } from '../../../nacha-config-admin/services/nacha-config-api.service';
import { NachaConfigProfileReadModel } from '../../../nacha-config-admin/models/nacha-config-admin.models';
import {
  AvailableInboundCycle,
  DifferentialResponseEligibleTransaction,
  GenerateNachaInboundSimulationRequest,
  InboundResponseMode,
  NachaInboundSimulationItem,
  NachaInboundSimulationResult,
  NachaInboundSimulationType,
  NachaInboundSimulatorService,
  NachaSimulationMode
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

  loadingHistory = false;
  loadingInstitutions = false;
  loadingClearingHouses = false;
  loadingProfiles = false;
  loadingCycles = false;
  loadingEligible = false;
  generating = false;
  previewing = false;
  hasLoadedHistory = false;

  simulations: NachaInboundSimulationItem[] = [];
  originFinancialInstitutions: DestinationInstitution[] = [];
  defaultDestination: DestinationInstitution | null = null;
  clearingHouses: ClearingHouseOption[] = [];
  activeProfiles: NachaConfigProfileReadModel[] = [];
  availableCycles: AvailableInboundCycle[] = [];
  eligibleTransactions: DifferentialResponseEligibleTransaction[] = [];
  result: NachaInboundSimulationResult | null = null;

  historyError: string | null = null;
  institutionCatalogError: string | null = null;
  clearingHouseError: string | null = null;
  profileError: string | null = null;
  cycleError: string | null = null;
  eligibleError: string | null = null;

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
    simulationMode: this.fb.nonNullable.control<NachaSimulationMode>('IncomingTransactions'),
    clearingHouseCode: this.fb.nonNullable.control('', Validators.required),
    scenarioType: this.fb.nonNullable.control<NachaInboundSimulationType>('IncomingCredit', Validators.required),
    originFinancialInstitutionId: this.fb.control<number | null>(null, Validators.required),
    entriesCount: this.fb.nonNullable.control(1, [Validators.required, Validators.min(1), Validators.max(10)]),
    amount: this.fb.nonNullable.control('1000.00', [Validators.required, exactMoneyValidator]),
    referencePrefix: this.fb.nonNullable.control('UAT-IN-CRED', [Validators.required, Validators.maxLength(24)]),
    businessDate: this.fb.control<Date | null>(todayLocal(), Validators.required),
    cycleCode: this.fb.nonNullable.control('', Validators.required),
    pendingPrenotificationReferencesText: this.fb.nonNullable.control(''),
    responseMode: this.fb.control<InboundResponseMode | null>(null),
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
    if (this.isDifferentialMode()) {
      this.loadEligibleTransactions();
    }
  }

  simulationContextChanged(): void {
    this.configureConditionalValidators();
    this.form.controls.cycleCode.setValue('');
    this.result = null;
    this.loadAvailableCycles();
    if (this.isDifferentialMode()) {
      this.selectedTransactionIds.clear();
      this.selectedTransactionReferences.clear();
      this.loadEligibleTransactions();
    }
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
      simulationMode: 'IncomingTransactions',
      clearingHouseCode,
      scenarioType: 'IncomingCredit',
      originFinancialInstitutionId: null,
      entriesCount: 1,
      amount: '1000.00',
      referencePrefix: 'UAT-IN-CRED',
      businessDate: todayLocal(),
      cycleCode: '',
      pendingPrenotificationReferencesText: '',
      responseMode: null,
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

  download(id: number): void {
    window.open(this.api.downloadUrl(id), '_blank', 'noopener');
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
      mode === 'DifferentialResponses' ? 'IncomingCreditConfirmation' : 'IncomingCredit'
    );
    this.form.controls.responseMode.setValue(mode === 'DifferentialResponses' ? 'Approved' : null);
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

    reason.setValidators(reasonRequired
      ? [Validators.required, Validators.maxLength(20)]
      : [Validators.maxLength(20)]);
    response.setValidators(this.isDifferentialMode() ? Validators.required : null);
    references.setValidators(this.requiresPrenotificationReferences() ? Validators.required : null);
    reason.updateValueAndValidity({ emitEvent: false });
    response.updateValueAndValidity({ emitEvent: false });
    references.updateValueAndValidity({ emitEvent: false });
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
      amount: this.parseMoney(this.form.controls.amount.value),
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

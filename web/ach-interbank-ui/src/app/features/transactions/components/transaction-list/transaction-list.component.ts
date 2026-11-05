import { ChangeDetectionStrategy, ChangeDetectorRef, Component, ElementRef, OnInit, ViewChild, inject } from '@angular/core';
import { AbstractControl, FormBuilder, FormControl, ValidationErrors, ValidatorFn } from '@angular/forms';
import { MatAutocompleteModule } from '@angular/material/autocomplete';
import { MatButtonModule } from '@angular/material/button';
import { MatNativeDateModule } from '@angular/material/core';
import { MatDatepickerModule } from '@angular/material/datepicker';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatExpansionModule } from '@angular/material/expansion';
import { MatMenuModule } from '@angular/material/menu';
import { MatButtonToggleModule } from '@angular/material/button-toggle';
import { MatIconModule } from '@angular/material/icon';
import { MatInputModule } from '@angular/material/input';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatTooltipModule } from '@angular/material/tooltip';
import { Router, RouterModule } from '@angular/router';
import { ColDef, RowSelectionOptions } from 'ag-grid-community';
import { debounceTime, distinctUntilChanged, finalize } from 'rxjs';
import { SharedModule } from '../../../../shared/shared.module';
import { TransactionsApiService } from '../../services/transactions-api.service';
import { TransactionListItem } from '../../transactions.models';
import { NotificationService } from '../../../../core/services/notification.service';
import { AuthService } from '../../../../core/services/auth.service';
import { TransactionTypeEnum } from '../../../../core/models/transaction-types.model';
import { ClearingHouseOption } from '../../../../core/models/clearing-house.model';
import { ClearingHousesApiService } from '../../../../core/services/clearing-houses-api.service';
import { AchCyclesApiService } from '../../../../core/services/ach-cycles-api.service';
import { AchCycleSummary } from '../../../../core/models/ach-cycle.model';
import { TransactionIntegrationResult } from '../../transactions.models';
import { TransactionIntegrationResultComponent } from '../transaction-integration-result/transaction-integration-result.component';

type TransactionListRow = TransactionListItem & {
  typeLabel: string;
  transactionNatureLabel: string;
  returnManagementLabel: string;
  amountText: string;
  effectiveEntryDateText: string;
};

interface TransactionGroup {
  batchId: number;
  batchLabel: string;
  batchDate: string;
  batchDateKey: string;
  cycleName: string;
  clearingHouseName: string;
  registrationTime: string;
  latestRegistrationTimestamp: number;
  totalAmount: string;
  items: TransactionListRow[];
}

interface TransactionSection {
  key: string;
  date: string;
  cycleName: string;
  clearingHouseName: string;
  transactionCount: number;
  batches: TransactionGroup[];
}

interface AchCycleOption {
  id: string;
  label: string;
  name: string;
}

interface ClearingHouseSearchOption {
  id: number | null;
  label: string;
}

interface CycleSearchOption {
  id: string | null;
  label: string;
}

type ClearingHouseSearchValue = string | ClearingHouseSearchOption | null;
type CycleSearchValue = string | CycleSearchOption | null;

interface ColumnOption {
  field: keyof TransactionListRow;
  label: string;
}

const validOptionalDate: ValidatorFn = (control: AbstractControl): ValidationErrors | null => {
  const value = control.value;
  if (value == null || value === '') {
    return null;
  }

  return value instanceof Date && !Number.isNaN(value.getTime())
    ? null
    : { invalidDate: true };
};

@Component({
  selector: 'app-transaction-list',
  standalone: true,
  imports: [
    SharedModule,
    RouterModule,
    MatAutocompleteModule,
    MatButtonModule,
    MatDatepickerModule,
    MatFormFieldModule,
    MatExpansionModule,
    MatMenuModule,
    MatButtonToggleModule,
    MatIconModule,
    MatInputModule,
    MatNativeDateModule,
    MatProgressSpinnerModule,
    MatTooltipModule,
    TransactionIntegrationResultComponent
  ],
  templateUrl: './transaction-list.component.html',
  styleUrls: ['./transaction-list.component.scss'],
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class TransactionListComponent implements OnInit {
  @ViewChild('selectedDateInput') private selectedDateInput?: ElementRef<HTMLInputElement>;
  private readonly fb = inject(FormBuilder);
  private readonly api = inject(TransactionsApiService);
  private readonly achCyclesApi = inject(AchCyclesApiService);
  private readonly clearingHousesApi = inject(ClearingHousesApiService);
  private readonly notifications = inject(NotificationService);
  private readonly auth = inject(AuthService);
  readonly canCreate = this.auth.hasPermission(['Transactions.Create', 'CanManageAch']);
  readonly canBulkUpload = this.auth.hasPermission(['BulkIngestion.Upload', 'CanManageAch']);
  readonly canTrackLoads = this.auth.hasPermission(['BulkIngestion.Read', 'CanReadAch']);
  private readonly router = inject(Router);
  private readonly cdr = inject(ChangeDetectorRef);
  private readonly currencyFormatter = new Intl.NumberFormat('es-CO', {
    style: 'currency',
    currency: 'COP'
  });
  private readonly dateFormatter = new Intl.DateTimeFormat('es-CO', {
    timeZone: 'UTC',
    year: 'numeric',
    month: '2-digit',
    day: '2-digit'
  });

  private readonly allColumnDefs: ColDef<TransactionListRow>[] = [
    { field: 'id', headerName: 'N.º', width: 90, maxWidth: 120, sortable: true },
    { field: 'transactionExternalId', headerName: 'Identificador', flex: 1, sortable: true, filter: 'agTextColumnFilter' },
    { field: 'reference', headerName: 'Referencia anterior', flex: 1, sortable: true, filter: 'agTextColumnFilter' },
    { field: 'typeLabel', headerName: 'Tipo', width: 160, filter: 'agTextColumnFilter' },
    { field: 'transactionNatureLabel', headerName: 'Naturaleza', width: 170, filter: 'agTextColumnFilter' },
    { field: 'returnManagementLabel', headerName: 'Gestión de devolución', width: 220, filter: 'agTextColumnFilter' },
    { field: 'achCycleName', headerName: 'Ciclo', width: 200, filter: 'agTextColumnFilter' },
    { field: 'clearingHouseName', headerName: 'Cámara', width: 200, filter: 'agTextColumnFilter' },
    { field: 'amountText', headerName: 'Valor', width: 140, maxWidth: 180, cellClass: 'text-end' },
    { field: 'sourceAccountNumber', headerName: 'Cuenta de origen', filter: 'agTextColumnFilter' },
    { field: 'destinationAccountNumber', headerName: 'Cuenta de destino', filter: 'agTextColumnFilter' },
    { field: 'destinationInstitutionName', headerName: 'Institución de destino', filter: 'agTextColumnFilter', flex: 1 },
    { field: 'effectiveEntryDateText', headerName: 'Fecha de aplicación', width: 160 }
  ];

  readonly columnOptions: ColumnOption[] = [
    { field: 'transactionExternalId', label: 'Identificador' },
    { field: 'reference', label: 'Referencia anterior' },
    { field: 'typeLabel', label: 'Tipo' },
    { field: 'transactionNatureLabel', label: 'Naturaleza' },
    { field: 'returnManagementLabel', label: 'Gestión de devolución' },
    { field: 'achCycleName', label: 'Ciclo' },
    { field: 'clearingHouseName', label: 'Cámara' },
    { field: 'amountText', label: 'Valor' },
    { field: 'sourceAccountNumber', label: 'Cuenta de origen' },
    { field: 'destinationAccountNumber', label: 'Cuenta de destino' },
    { field: 'destinationInstitutionName', label: 'Institución de destino' },
    { field: 'effectiveEntryDateText', label: 'Fecha de aplicación' }
  ];

  readonly mandatoryColumnFields = new Set<keyof TransactionListRow>(['id']);
  visibleColumnFields = new Set<keyof TransactionListRow>([
    'id',
    'transactionExternalId',
    'typeLabel',
    'amountText',
    'destinationAccountNumber',
    'effectiveEntryDateText'
  ]);

  private cachedColumnDefs: ColDef<TransactionListRow>[] | null = null;

  get columnDefs(): ColDef<TransactionListRow>[] {
    if (!this.cachedColumnDefs) {
      this.cachedColumnDefs = this.allColumnDefs.filter((column) => {
        const field = column.field as keyof TransactionListRow | undefined;
        return !field || this.visibleColumnFields.has(field);
      });
    }
    return this.cachedColumnDefs;
  }

  readonly defaultColDef: ColDef<TransactionListRow> = {
    resizable: true,
    sortable: true,
    suppressHeaderKeyboardEvent: () => true,
    filterParams: { suppressAndOrCondition: true }
  };

  readonly rowSelection = {
    mode: 'singleRow',
    enableClickSelection: true
  } satisfies RowSelectionOptions;

  readonly noRowsTemplate = 'No hay transacciones registradas.';
  readonly loadingTemplate = 'Consultando transacciones…';
  readonly batchPageSize = 12;

  loading = false;
  returnView: 'all' | 'received' | 'sent' = 'all';
  groups: TransactionGroup[] = [];
  sections: TransactionSection[] = [];
  private allTransactions: TransactionListRow[] = [];
  visibleBatchCount = this.batchPageSize;
  expandedBatchId: number | null = null;
  readonly quickSearchControl = new FormControl('', { nonNullable: true });
  private cyclesRequestId = 0;
  private integrationRequestId = 0;
  private readonly timeFormatter = new Intl.DateTimeFormat('es-CO', {
    timeZone: 'America/Bogota', hour: '2-digit', minute: '2-digit', hour12: true
  });
  cycles: AchCycleOption[] = [];
  clearingHouses: ClearingHouseOption[] = [];
  filteredClearingHouseOptions: ClearingHouseSearchOption[] = [
    { id: null, label: 'Todas las cámaras' }
  ];
  filteredCycleOptions: CycleSearchOption[] = [
    { id: null, label: 'Todos los ciclos' }
  ];
  selectedIntegrationResult: TransactionIntegrationResult | null = null;
  loadingIntegrationResult = false;
  loadError = false;
  searchAttempted = false;
  readonly filtrosForm = this.fb.group({
    selectedCycleId: new FormControl<string | null>(null),
    selectedClearingHouseId: new FormControl<number | null>(null),
    selectedDate: new FormControl<Date | null>(null, validOptionalDate)
  });
  readonly clearingHouseSearchControl = new FormControl<ClearingHouseSearchValue>('');
  readonly cycleSearchControl = new FormControl<CycleSearchValue>('');
  readonly displayClearingHouseOption = (value: ClearingHouseSearchValue): string =>
    typeof value === 'string' ? value : value?.label ?? '';
  readonly displayCycleOption = (value: CycleSearchValue): string =>
    typeof value === 'string' ? value : value?.label ?? '';

  get totalTransactions(): number {
    return this.groups.reduce((total, group) => total + group.items.length, 0);
  }

  get visibleBatchTotal(): number {
    return Math.min(this.groups.length, this.visibleBatchCount);
  }

  get hasMoreBatches(): boolean {
    return this.groups.length > this.visibleBatchCount;
  }

  get hasLocalFilter(): boolean {
    return this.returnView !== 'all' || this.quickSearchControl.value.trim().length > 0;
  }

  showMoreBatches(): void {
    this.visibleBatchCount += this.batchPageSize;
    this.buildSections();
    this.cdr.markForCheck();
  }

  expandBatch(id: number): void {
    this.expandedBatchId = id;
    this.cdr.markForCheck();
  }

  collapseBatch(id: number): void {
    if (this.expandedBatchId === id) {
      this.expandedBatchId = null;
      this.cdr.markForCheck();
    }
  }

  trackSection(_index: number, section: TransactionSection): string { return section.key; }
  trackBatch(_index: number, group: TransactionGroup): number { return group.batchId; }

  clearQuickSearch(): void {
    this.quickSearchControl.setValue('');
  }

  ngOnInit(): void {
    this.filtrosForm.controls.selectedClearingHouseId.valueChanges.subscribe(() => this.onClearingHouseChange());
    this.clearingHouseSearchControl.valueChanges.subscribe((value) => this.filterClearingHouseOptions(value));
    this.cycleSearchControl.valueChanges.subscribe((value) => this.filterCycleOptions(value));
    this.quickSearchControl.valueChanges.pipe(debounceTime(200), distinctUntilChanged())
      .subscribe(() => this.updateView());
    this.loadClearingHouses();
    this.loadCycles();
  }

  createNew(): void {
    this.router.navigate(['/transactions/create']);
  }

  createBulk(): void {
    this.router.navigate(['/transactions/bulk-ingestion/upload']);
  }

  openBulkTracking(): void {
    this.router.navigate(['/transactions/bulk-ingestion/tracking']);
  }

  applyFilters(): void {
    if (this.loading) {
      return;
    }

    this.searchAttempted = true;
    this.filtrosForm.updateValueAndValidity();
    if (this.filtrosForm.invalid) {
      this.filtrosForm.markAllAsTouched();
      this.focusFirstInvalidControl();
      this.cdr.markForCheck();
      return;
    }

    this.loadTransactions();
  }

  clearFilters(): void {
    if (this.loading) {
      return;
    }

    this.filtrosForm.reset({
      selectedCycleId: null,
      selectedClearingHouseId: null,
      selectedDate: null
    }, { emitEvent: false });
    this.clearingHouseSearchControl.setValue('', { emitEvent: false });
    this.cycleSearchControl.setValue('', { emitEvent: false });
    this.filterClearingHouseOptions('');
    this.filterCycleOptions('');
    this.filtrosForm.markAsPristine();
    this.filtrosForm.markAsUntouched();
    this.searchAttempted = false;
    this.quickSearchControl.setValue('', { emitEvent: false });
    this.returnView = 'all';
    this.selectedIntegrationResult = null;
    this.loadError = false;
    this.loadCycles(true, false);
    this.cdr.markForCheck();
  }

  shouldShowDateError(): boolean {
    const control = this.filtrosForm.controls.selectedDate;
    return control.invalid && (control.touched || control.dirty || this.searchAttempted);
  }

  onClearingHouseChange(): void {
    this.filtrosForm.patchValue({ selectedCycleId: null }, { emitEvent: false });
    this.cycleSearchControl.setValue('', { emitEvent: false });
    this.filterCycleOptions('');
    if (this.filtrosForm.controls.selectedDate.invalid) {
      return;
    }
    this.loadCycles(false);
  }

  onDateChange(): void {
    this.filtrosForm.patchValue({ selectedCycleId: null }, { emitEvent: false });
    this.cycleSearchControl.setValue('', { emitEvent: false });
    this.filterCycleOptions('');
    if (this.filtrosForm.controls.selectedDate.invalid) {
      return;
    }
    this.loadCycles(false);
  }

  filterClearingHouseOptions(value: ClearingHouseSearchValue): void {
    const term = this.normalizeSearchText(typeof value === 'string' ? value : value?.label ?? '');
    this.filteredClearingHouseOptions = this.getClearingHouseSearchOptions()
      .filter((option) => this.normalizeSearchText(option.label).includes(term));

    if (typeof value === 'string') {
      const selectedId = this.filtrosForm.controls.selectedClearingHouseId.value;
      const selectedOption = this.getClearingHouseSearchOptions().find((option) => option.id === selectedId);
      if (selectedId !== null && this.normalizeSearchText(selectedOption?.label ?? '') !== term) {
        this.filtrosForm.controls.selectedClearingHouseId.setValue(null);
      }
    }

    this.cdr.markForCheck();
  }

  filterCycleOptions(value: CycleSearchValue): void {
    const term = this.normalizeSearchText(typeof value === 'string' ? value : value?.label ?? '');
    this.filteredCycleOptions = this.getCycleSearchOptions()
      .filter((option) => this.normalizeSearchText(option.label).includes(term));

    if (typeof value === 'string') {
      const selectedId = this.filtrosForm.controls.selectedCycleId.value;
      const selectedOption = this.getCycleSearchOptions().find((option) => option.id === selectedId);
      if (selectedId !== null && this.normalizeSearchText(selectedOption?.label ?? '') !== term) {
        this.filtrosForm.controls.selectedCycleId.setValue(null, { emitEvent: false });
      }
    }

    this.cdr.markForCheck();
  }

  selectClearingHouse(option: ClearingHouseSearchOption): void {
    this.clearingHouseSearchControl.setValue(option, { emitEvent: false });
    this.filteredClearingHouseOptions = this.getClearingHouseSearchOptions();
    this.filtrosForm.controls.selectedClearingHouseId.setValue(option.id);
  }

  selectCycle(option: CycleSearchOption): void {
    this.cycleSearchControl.setValue(option, { emitEvent: false });
    this.filteredCycleOptions = this.getCycleSearchOptions();
    this.filtrosForm.controls.selectedCycleId.setValue(option.id, { emitEvent: false });
    this.cdr.markForCheck();
  }

  clearClearingHouseSearch(): void {
    const hadSelectedClearingHouse = this.filtrosForm.controls.selectedClearingHouseId.value !== null;
    this.clearingHouseSearchControl.setValue('');
    if (!hadSelectedClearingHouse) {
      this.onClearingHouseChange();
    }
  }

  clearCycleSearch(): void {
    this.cycleSearchControl.setValue('');
    this.filtrosForm.controls.selectedCycleId.setValue(null, { emitEvent: false });
    this.cdr.markForCheck();
  }

  isColumnVisible(field: keyof TransactionListRow): boolean {
    return this.visibleColumnFields.has(field);
  }

  canToggleColumn(field: keyof TransactionListRow): boolean {
    return !this.mandatoryColumnFields.has(field);
  }

  toggleColumn(field: keyof TransactionListRow, checked: boolean): void {
    if (!this.canToggleColumn(field)) {
      return;
    }

    if (checked) {
      this.visibleColumnFields.add(field);
      this.cachedColumnDefs = null;
      this.cdr.markForCheck();
      return;
    }

    const optionalVisibleCount = Array.from(this.visibleColumnFields)
      .filter((visibleField) => !this.mandatoryColumnFields.has(visibleField)).length;

    if (optionalVisibleCount <= 1) {
      this.notifications.warning('Conserva al menos una columna de información además del número.');
      return;
    }

    this.visibleColumnFields.delete(field);
    this.cachedColumnDefs = null;
    this.cdr.markForCheck();
  }

  private loadClearingHouses(): void {
    this.clearingHousesApi.list().subscribe({
      next: (items) => {
        this.clearingHouses = items ?? [];
        this.filteredClearingHouseOptions = this.getClearingHouseSearchOptions();
        this.restoreClearingHouseSearchText();
        this.cdr.markForCheck();
      },
      error: () => {
        this.notifications.error('No fue posible cargar las cámaras compensadoras');
      }
    });
  }

  private loadCycles(autoLoadTransactions = true, restoreSearchText = true): void {
    const requestId = ++this.cyclesRequestId;
    if (this.filtrosForm.controls.selectedDate.invalid) {
      return;
    }

    const selectedDate = this.formatApiDate(this.filtrosForm.controls.selectedDate.value);
    this.achCyclesApi
      .search({
        page: 1,
        pageSize: 100,
        clearingHouseId: this.filtrosForm.controls.selectedClearingHouseId.value ?? undefined,
        startDate: selectedDate,
        endDate: selectedDate
      })
      .subscribe({
        next: (response) => {
          if (requestId !== this.cyclesRequestId) { return; }
          const mapped = (response?.items ?? []).map((cycle) => this.mapCycleOption(cycle));
          const distinct = this.distinctCycles(mapped);
          const selectedClearingHouseId = this.filtrosForm.controls.selectedClearingHouseId.value;
          this.cycles = selectedClearingHouseId == null
            ? distinct.map((option) => ({ ...option, id: option.name }))
            : distinct;

          if (autoLoadTransactions && !this.filtrosForm.controls.selectedCycleId.value && this.cycles.length > 0) {
            this.filtrosForm.patchValue({ selectedCycleId: this.cycles[0].id }, { emitEvent: false });
          }

          if (this.cycles.length === 0) {
            this.filtrosForm.patchValue({ selectedCycleId: null }, { emitEvent: false });
          }

          this.filteredCycleOptions = this.getCycleSearchOptions();
          if (restoreSearchText) {
            this.restoreCycleSearchText();
          }

          if (autoLoadTransactions) {
            this.loadTransactions();
          }

          this.cdr.markForCheck();
        },
        error: () => {
          if (requestId !== this.cyclesRequestId) { return; }
          this.notifications.error('No fue posible cargar los ciclos de compensación');
          this.cdr.markForCheck();
        }
      });
  }

  private loadTransactions(): void {
    if (this.loading || this.filtrosForm.invalid) {
      return;
    }

    this.loading = true;
    this.loadError = false;
    this.selectedIntegrationResult = null;
    this.loadingIntegrationResult = false;
    this.integrationRequestId++;
    this.cdr.markForCheck();
    const selectedClearingHouseId = this.filtrosForm.controls.selectedClearingHouseId.value;
    const selectedCycleId = this.filtrosForm.controls.selectedCycleId.value;
    const selectedDate = this.formatApiDate(this.filtrosForm.controls.selectedDate.value);
    const useCycleName = selectedClearingHouseId == null;
    this.api
      .getAll({
        achCycleId: useCycleName ? null : selectedCycleId,
        achCycleName: useCycleName ? selectedCycleId : null,
        effectiveDate: selectedDate || undefined,
        clearingHouseId: selectedClearingHouseId ?? undefined
      })
      .pipe(
        finalize(() => {
          this.loading = false;
          this.cdr.markForCheck();
        })
      )
      .subscribe({
        next: (items) => {
          this.allTransactions = (items ?? []).map((item) => this.mapRow(item));
          this.updateView();
        },
        error: () => {
          this.allTransactions = [];
          this.groups = [];
          this.sections = [];
          this.loadError = true;
          this.notifications.error('No fue posible cargar las transacciones');
          this.cdr.markForCheck();
        }
      });
  }

  /** Resultados ya recibidos: filtrar localmente evita solicitar la misma consulta al servidor. */
  private updateView(): void {
    const search = this.normalizeSearchText(this.quickSearchControl.value);
    const filtered = this.allTransactions.filter((item) => this.matchesReturnView(item) && (
      !search || [
        item.id, item.transactionExternalId, item.reference, item.traceNumber,
        item.sourceAccountNumber, item.destinationAccountNumber, item.destinationInstitutionName,
        item.batchCompanyName, item.batchSequenceNumber, item.achCycleName,
        item.clearingHouseName
      ].some((value) => this.normalizeSearchText(String(value ?? '')).includes(search))
    ));

    const grouped = new Map<number, TransactionGroup>();
    for (const item of filtered) {
      let group = grouped.get(item.achBatchId);
      if (!group) {
        const effectiveDate = item.batchEffectiveEntryDate || item.effectiveEntryDate;
        group = {
          batchId: item.achBatchId,
          batchLabel: this.buildBatchLabel(item),
          batchDate: this.formatDate(effectiveDate),
          batchDateKey: this.dateKey(effectiveDate),
          clearingHouseName: item.clearingHouseName || 'Cámara sin identificar',
          cycleName: item.achCycleName || 'Ciclo sin identificar',
          registrationTime: '',
          latestRegistrationTimestamp: 0,
          totalAmount: '',
          items: []
        };
        grouped.set(item.achBatchId, group);
      }
      group.items.push(item);
    }

    this.groups = Array.from(grouped.values());
    for (const group of this.groups) {
      group.totalAmount = this.currencyFormatter.format(group.items.reduce((sum, item) => sum + (item.amount || 0), 0));
      group.registrationTime = this.registrationRange(group.items);
      group.latestRegistrationTimestamp = group.items.reduce((latest, item) => {
        const time = item.createdAt ? new Date(item.createdAt).getTime() : Number.NaN;
        return Number.isFinite(time) ? Math.max(latest, time) : latest;
      }, 0);
    }
    this.groups.sort((a, b) =>
      b.batchDateKey.localeCompare(a.batchDateKey) ||
      a.clearingHouseName.localeCompare(b.clearingHouseName, 'es') ||
      a.cycleName.localeCompare(b.cycleName, 'es') ||
      b.latestRegistrationTimestamp - a.latestRegistrationTimestamp || b.batchId - a.batchId
    );

    this.visibleBatchCount = this.batchPageSize;
    if (!this.groups.some((group) => group.batchId === this.expandedBatchId)) {
      this.expandedBatchId = this.groups[0]?.batchId ?? null;
    }
    this.selectedIntegrationResult = null;
    this.loadingIntegrationResult = false;
    this.integrationRequestId++;
    this.buildSections();
    this.cdr.markForCheck();
  }

  private buildSections(): void {
    const sections = new Map<string, TransactionSection>();
    for (const group of this.groups.slice(0, this.visibleBatchCount)) {
      const key = `${group.batchDateKey}|${group.clearingHouseName}|${group.cycleName}`;
      let section = sections.get(key);
      if (!section) {
        section = {
          key, date: group.batchDate, cycleName: group.cycleName,
          clearingHouseName: group.clearingHouseName, transactionCount: 0, batches: []
        };
        sections.set(key, section);
      }
      section.batches.push(group);
      section.transactionCount += group.items.length;
    }
    this.sections = Array.from(sections.values());
  }

  private dateKey(value: string | null | undefined): string {
    if (!value) { return ''; }
    const parsed = new Date(value);
    return !Number.isNaN(parsed.getTime()) ? parsed.toISOString().slice(0, 10) : value.slice(0, 10);
  }

  /** Hora del registro, derivada de createdAt; no equivale al horario de la cámara. */
  private registrationRange(items: TransactionListRow[]): string {
    const times = items.map((item) =>
      item.createdAt && /[T ]\d{2}:\d{2}/.test(item.createdAt)
        ? new Date(item.createdAt).getTime() : Number.NaN
    ).filter((time) => Number.isFinite(time)).sort((a, b) => a - b);
    if (!times.length) { return 'Hora de registro no disponible'; }
    const first = this.timeFormatter.format(times[0]);
    const last = this.timeFormatter.format(times[times.length - 1]);
    if (first === last) { return `Registro: ${first}`; }
    const firstDay = this.formatDate(new Date(times[0]).toISOString());
    const lastDay = this.formatDate(new Date(times[times.length - 1]).toISOString());
    return firstDay === lastDay
      ? `Registros: ${first} a ${last}`
      : `Registros: ${firstDay} ${first} a ${lastDay} ${last}`;
  }

  private mapCycleOption(cycle: AchCycleSummary): AchCycleOption {
    const id = cycle.id?.trim();
    const name = cycle.cycleName?.trim() || `Ciclo ${id}`;

    return { id, label: name, name };
  }

  private distinctCycles(options: AchCycleOption[]): AchCycleOption[] {
    const seen = new Set<string>();

    return options.filter((option) => {
      const key = option.name.toLocaleLowerCase();
      if (seen.has(key)) {
        return false;
      }

      seen.add(key);
      return true;
    });
  }

  private mapRow(item: TransactionListItem): TransactionListRow {
    return {
      ...item,
      typeLabel: this.formatType(item.type),
      transactionNatureLabel: this.formatTransactionNature(item.isPrenotification),
      returnManagementLabel: this.formatReturnManagement(item.transactionCode),
      amountText: this.currencyFormatter.format(item.amount ?? 0),
      effectiveEntryDateText: this.formatDate(item.effectiveEntryDate)
    };
  }

  setReturnView(view: 'all' | 'received' | 'sent'): void {
    if (this.returnView === view) { return; }
    this.returnView = view;
    this.updateView();
  }

  onTransactionSelection(rows: TransactionListRow[]): void {
    const transaction = rows[0];
    const requestId = ++this.integrationRequestId;
    this.selectedIntegrationResult = null;
    if (!transaction) {
      this.loadingIntegrationResult = false;
      this.cdr.markForCheck();
      return;
    }

    this.loadingIntegrationResult = true;
    this.api.getIntegrationResult(transaction.id).pipe(finalize(() => {
      if (requestId === this.integrationRequestId) {
        this.loadingIntegrationResult = false;
        this.cdr.markForCheck();
      }
    })).subscribe({
      next: (result) => {
        if (requestId === this.integrationRequestId) {
          this.selectedIntegrationResult = result;
          this.cdr.markForCheck();
        }
      },
      error: () => {
        if (requestId === this.integrationRequestId) {
          this.notifications.error('No fue posible consultar el resultado del procesamiento.');
          this.cdr.markForCheck();
        }
      }
    });
  }

  closeIntegrationResult(): void {
    this.integrationRequestId++;
    this.loadingIntegrationResult = false;
    this.selectedIntegrationResult = null;
    this.cdr.markForCheck();
  }

  private matchesReturnView(item: TransactionListRow): boolean {
    if (this.returnView === 'all') {
      return true;
    }

    const code = (item.transactionCode ?? '').trim();
    const receivedCodes = new Set(['21', '31', '51']);
    const sentCodes = new Set(['26', '36', '56']);

    return this.returnView === 'received' ? receivedCodes.has(code) : sentCodes.has(code);
  }

  private formatReturnManagement(code: string | null | undefined): string {
    const normalized = (code ?? '').trim();
    if (['21', '31', '51'].includes(normalized)) {
      return 'Devolución recibida';
    }

    if (['26', '36', '56'].includes(normalized)) {
      return 'Devolución enviada';
    }

    return 'No devolución';
  }

  private formatDate(value: string | null | undefined): string {
    if (!value) {
      return '-';
    }

    const date = new Date(value);
    return isNaN(date.getTime()) ? value : this.dateFormatter.format(date);
  }

  private getClearingHouseSearchOptions(): ClearingHouseSearchOption[] {
    return [
      { id: null, label: 'Todas las cámaras' },
      ...this.clearingHouses.map((house) => ({ id: house.id, label: house.name }))
    ];
  }

  private getCycleSearchOptions(): CycleSearchOption[] {
    return [
      { id: null, label: 'Todos los ciclos' },
      ...this.cycles.map((cycle) => ({ id: cycle.id, label: cycle.label }))
    ];
  }

  private restoreClearingHouseSearchText(): void {
    const selectedId = this.filtrosForm.controls.selectedClearingHouseId.value;
    const selectedOption = this.getClearingHouseSearchOptions().find((option) => option.id === selectedId);
    this.clearingHouseSearchControl.setValue(selectedOption ?? '', { emitEvent: false });
  }

  private restoreCycleSearchText(): void {
    const selectedId = this.filtrosForm.controls.selectedCycleId.value;
    const selectedOption = this.getCycleSearchOptions().find((option) => option.id === selectedId);
    this.cycleSearchControl.setValue(selectedOption ?? '', { emitEvent: false });
  }

  private normalizeSearchText(value: string): string {
    return value
      .normalize('NFD')
      .replace(/[\u0300-\u036f]/g, '')
      .trim()
      .toLocaleLowerCase();
  }

  private formatApiDate(value: Date | null): string | undefined {
    if (!(value instanceof Date) || Number.isNaN(value.getTime())) {
      return undefined;
    }

    const year = value.getFullYear();
    const month = String(value.getMonth() + 1).padStart(2, '0');
    const day = String(value.getDate()).padStart(2, '0');
    return `${year}-${month}-${day}`;
  }

  private focusFirstInvalidControl(): void {
    if (this.filtrosForm.controls.selectedDate.invalid) {
      setTimeout(() => this.selectedDateInput?.nativeElement.focus());
    }
  }

  private formatType(type: TransactionTypeEnum): string {
    if (type === TransactionTypeEnum.Credit) {
      return 'Crédito';
    }

    if (type === TransactionTypeEnum.Debit) {
      return 'Débito';
    }

    if (type === TransactionTypeEnum.Prenotification) {
      return 'Prenotificación';
    }

    if (type === TransactionTypeEnum.Reversal) {
      return 'Reverso';
    }

    if (type === TransactionTypeEnum.Return) {
      return 'Devolución';
    }

    return 'Desconocido';
  }

  private formatTransactionNature(isPrenotification: boolean): string {
    return isPrenotification ? 'Prenotificación' : 'Transacción';
  }

  private buildBatchLabel(item: TransactionListItem): string {
    const sequence = item.batchSequenceNumber > 0 ? item.batchSequenceNumber : item.achBatchId;
    return `Lote ${sequence} · ${item.batchCompanyName || 'Empresa no informada'}`;
  }
}

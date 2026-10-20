import { CommonModule } from '@angular/common';
import {
  ChangeDetectionStrategy,
  ChangeDetectorRef,
  Component,
  DestroyRef,
  OnInit,
  TemplateRef,
  ViewChild,
  inject
} from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import {
  AbstractControl,
  FormControl,
  FormGroup,
  ReactiveFormsModule,
  ValidationErrors,
  Validators
} from '@angular/forms';
import { HttpErrorResponse } from '@angular/common/http';
import { RouterLink } from '@angular/router';
import { MatButtonModule } from '@angular/material/button';
import { MatCardModule } from '@angular/material/card';
import { MatCheckboxModule } from '@angular/material/checkbox';
import { MatChipsModule } from '@angular/material/chips';
import { MatNativeDateModule } from '@angular/material/core';
import { MatDatepickerModule } from '@angular/material/datepicker';
import { MatDialog, MatDialogModule, MatDialogRef } from '@angular/material/dialog';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatIconModule } from '@angular/material/icon';
import { MatInputModule } from '@angular/material/input';
import { MatMenuModule } from '@angular/material/menu';
import { MatPaginator, MatPaginatorModule } from '@angular/material/paginator';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatSelectModule } from '@angular/material/select';
import { MatSlideToggleModule } from '@angular/material/slide-toggle';
import { MatSnackBar, MatSnackBarModule } from '@angular/material/snack-bar';
import { MatSort, MatSortModule } from '@angular/material/sort';
import { MatTableDataSource, MatTableModule } from '@angular/material/table';
import { MatTooltipModule } from '@angular/material/tooltip';
import { catchError, finalize, forkJoin, map, Observable, of, switchMap, take } from 'rxjs';
import { AuthService } from '../../core/services/auth.service';
import { getHttpErrorMessage } from '../../core/utils/http-error-message.util';
import { OperationalConfirmDialogComponent, OperationalConfirmDialogData } from '../../shared/components/operational-confirm-dialog.component';
import { ClearingHouse } from '../../core/models/clearing-house.model';
import { ClearingHousesService } from '../../core/services/clearing-houses.service';
import { ClearingHouseCycleConfigsApiService } from '../../core/services/clearing-house-cycle-configs-api.service';
import { ClearingHouseCycleConfigItem } from '../../core/models/clearing-house-cycle-config.model';
import { AccountTypeEnum, TransactionTypeEnum } from '../../core/models/transaction-types.model';
import {
  CfaOperationalTransactionRestriction,
  CfaOperationalTransactionRestrictionFilter,
  SaveCfaOperationalTransactionRestrictionRequest,
  UpdateCfaOperationalTransactionRestrictionRequest
} from './operational-restrictions.models';
import { OperationalRestrictionsService } from './operational-restrictions.service';

type TriState = 'all' | 'yes' | 'no';
type RestrictionScope = 'global' | 'clearingHouse' | 'cycle';
type AccountTypesMode = 'inherit' | 'explicit';

type FilterForm = FormGroup<{
  clearingHouseCode: FormControl<string | null>;
  policyVersion: FormControl<string>;
  cycleConfigId: FormControl<number | null>;
  transactionType: FormControl<TransactionTypeEnum | null>;
  prenotification: FormControl<TriState>;
  active: FormControl<TriState>;
  effectiveAt: FormControl<Date | null>;
}>;

type EditorForm = FormGroup<{
  scope: FormControl<RestrictionScope>;
  clearingHouseCode: FormControl<string | null>;
  policyVersion: FormControl<string | null>;
  cycleConfigId: FormControl<number | null>;
  transactionType: FormControl<TransactionTypeEnum | null>;
  isPrenotification: FormControl<TriState>;
  maxAmountPerTransaction: FormControl<number | null>;
  maxAmountPerCycle: FormControl<number | null>;
  maxTransactionsPerCycle: FormControl<number | null>;
  accountTypesMode: FormControl<AccountTypesMode>;
  checking: FormControl<boolean>;
  savings: FormControl<boolean>;
  electronicDeposits: FormControl<boolean>;
  effectiveFrom: FormControl<Date | null>;
  effectiveTo: FormControl<Date | null>;
  isActive: FormControl<boolean>;
}>;

@Component({
  selector: 'app-operational-restrictions',
  standalone: true,
  imports: [
    CommonModule,
    ReactiveFormsModule,
    RouterLink,
    MatButtonModule,
    MatCardModule,
    MatCheckboxModule,
    MatChipsModule,
    MatDatepickerModule,
    MatNativeDateModule,
    MatDialogModule,
    MatFormFieldModule,
    MatIconModule,
    MatInputModule,
    MatMenuModule,
    MatPaginatorModule,
    MatProgressSpinnerModule,
    MatSelectModule,
    MatSlideToggleModule,
    MatSnackBarModule,
    MatSortModule,
    MatTableModule,
    MatTooltipModule
  ],
  templateUrl: './operational-restrictions.component.html',
  styleUrl: './operational-restrictions.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class OperationalRestrictionsComponent implements OnInit {
  @ViewChild('editorDialog') editorDialog!: TemplateRef<unknown>;
  @ViewChild('detailDialog') detailDialog!: TemplateRef<unknown>;
  @ViewChild(MatSort) set sort(sort: MatSort | undefined) {
    if (sort) this.dataSource.sort = sort;
  }
  @ViewChild(MatPaginator) set paginator(paginator: MatPaginator | undefined) {
    if (paginator) this.dataSource.paginator = paginator;
  }

  private readonly api = inject(OperationalRestrictionsService);
  private readonly housesApi = inject(ClearingHousesService);
  private readonly cyclesApi = inject(ClearingHouseCycleConfigsApiService);
  private readonly auth = inject(AuthService);
  private readonly dialog = inject(MatDialog);
  private readonly snackBar = inject(MatSnackBar);
  private readonly cdr = inject(ChangeDetectorRef);
  private readonly destroyRef = inject(DestroyRef);

  readonly canRead = this.auth.hasPermission(['OperationalRestrictions.Read', 'OperationalRestrictions.Manage']);
  readonly canManage = this.auth.hasPermission('OperationalRestrictions.Manage');
  readonly transactionTypes = TransactionTypeEnum;
  readonly accountTypes = AccountTypeEnum;
  readonly displayedColumns = ['scope', 'transaction', 'limits', 'accounts', 'validity', 'state', 'updated', 'actions'];
  readonly dataSource = new MatTableDataSource<CfaOperationalTransactionRestriction>([]);

  houses: ClearingHouse[] = [];
  filterCycles: ClearingHouseCycleConfigItem[] = [];
  editorCycles: ClearingHouseCycleConfigItem[] = [];
  loading = false;
  lookupsLoading = false;
  saving = false;
  error = '';
  lookupWarning = '';
  editing: CfaOperationalTransactionRestriction | null = null;
  detail: CfaOperationalTransactionRestriction | null = null;
  private editorRef?: MatDialogRef<unknown>;

  readonly filterForm: FilterForm = new FormGroup({
    clearingHouseCode: new FormControl<string | null>(null),
    policyVersion: new FormControl('', { nonNullable: true }),
    cycleConfigId: new FormControl<number | null>({ value: null, disabled: true }),
    transactionType: new FormControl<TransactionTypeEnum | null>(null),
    prenotification: new FormControl<TriState>('all', { nonNullable: true }),
    active: new FormControl<TriState>('all', { nonNullable: true }),
    effectiveAt: new FormControl<Date | null>(null)
  });

  readonly form: EditorForm = new FormGroup({
    scope: new FormControl<RestrictionScope>('global', { nonNullable: true }),
    clearingHouseCode: new FormControl<string | null>(null),
    policyVersion: new FormControl<string | null>(null),
    cycleConfigId: new FormControl<number | null>(null),
    transactionType: new FormControl<TransactionTypeEnum | null>(null),
    isPrenotification: new FormControl<TriState>('all', { nonNullable: true }),
    maxAmountPerTransaction: new FormControl<number | null>(null, Validators.min(0)),
    maxAmountPerCycle: new FormControl<number | null>(null, Validators.min(0)),
    maxTransactionsPerCycle: new FormControl<number | null>(null, [Validators.min(0), integerValidator]),
    accountTypesMode: new FormControl<AccountTypesMode>('inherit', { nonNullable: true }),
    checking: new FormControl(false, { nonNullable: true }),
    savings: new FormControl(false, { nonNullable: true }),
    electronicDeposits: new FormControl(false, { nonNullable: true }),
    effectiveFrom: new FormControl<Date | null>(today(), Validators.required),
    effectiveTo: new FormControl<Date | null>(null),
    isActive: new FormControl(true, { nonNullable: true })
  }, { validators: [dateRangeValidator, scopeValidator] });

  constructor() {
    this.filterForm.controls.clearingHouseCode.valueChanges
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe(code => this.onFilterHouseChanged(code));

    this.form.controls.scope.valueChanges
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe(scope => this.onScopeChanged(scope));

    this.form.controls.clearingHouseCode.valueChanges
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe(code => this.onEditorHouseChanged(code));

    this.form.controls.policyVersion.valueChanges
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe(version => this.ensureEditorCycleMatchesPolicy(version));
  }

  ngOnInit(): void {
    this.loadLookupsAndRows();
  }

  get editorPolicyVersions(): string[] {
    return [...new Set(this.editorCycles.map(cycle => cycle.policyVersion).filter(Boolean))].sort();
  }

  get editorCycleOptions(): ClearingHouseCycleConfigItem[] {
    const policyVersion = this.form.controls.policyVersion.value;
    return this.editorCycles
      .filter(cycle => !policyVersion || cycle.policyVersion === policyVersion)
      .sort((a, b) => a.cycleName.localeCompare(b.cycleName));
  }

  get accountsHelp(): string {
    if (this.form.controls.accountTypesMode.value === 'inherit') {
      return 'Hereda la allowlist del nivel menos específico.';
    }
    return this.selectedAccountTypes().length === 0
      ? 'Lista explícita vacía: DENY para todos los tipos de cuenta.'
      : 'Solo los tipos seleccionados quedan permitidos por esta restricción CFA.';
  }

  loadLookupsAndRows(): void {
    this.load();
    this.lookupsLoading = true;
    this.lookupWarning = '';
    this.loadAllHouses().pipe(
      finalize(() => {
        this.lookupsLoading = false;
        this.cdr.markForCheck();
      })
    ).subscribe({
      next: houses => {
        this.houses = houses.sort((a, b) => a.code.localeCompare(b.code));
        this.cdr.markForCheck();
      },
      error: error => {
        this.lookupWarning = getHttpErrorMessage(error, 'No fue posible cargar el catálogo de cámaras.');
        this.cdr.markForCheck();
      }
    });
  }

  load(): void {
    if (!this.canRead) return;
    this.loading = true;
    this.error = '';
    this.api.list(this.buildFilters()).pipe(
      finalize(() => {
        this.loading = false;
        this.cdr.markForCheck();
      })
    ).subscribe({
      next: rows => {
        this.dataSource.data = rows;
        this.mergeHouseOptionsFromRows(rows);
        if (this.dataSource.paginator) this.dataSource.paginator.firstPage();
      },
      error: error => {
        this.dataSource.data = [];
        this.error = getHttpErrorMessage(error, 'No fue posible consultar las restricciones operacionales CFA.');
      }
    });
  }

  applyFilters(): void {
    this.load();
  }

  clearFilters(): void {
    this.filterCycles = [];
    this.filterForm.controls.cycleConfigId.disable({ emitEvent: false });
    this.filterForm.reset({
      clearingHouseCode: null,
      policyVersion: '',
      cycleConfigId: null,
      transactionType: null,
      prenotification: 'all',
      active: 'all',
      effectiveAt: null
    }, { emitEvent: false });
    this.load();
  }

  create(): void {
    if (!this.canManage) return;
    this.editing = null;
    this.editorCycles = [];
    this.form.reset({
      scope: 'global',
      clearingHouseCode: null,
      policyVersion: null,
      cycleConfigId: null,
      transactionType: null,
      isPrenotification: 'all',
      maxAmountPerTransaction: null,
      maxAmountPerCycle: null,
      maxTransactionsPerCycle: null,
      accountTypesMode: 'inherit',
      checking: false,
      savings: false,
      electronicDeposits: false,
      effectiveFrom: today(),
      effectiveTo: null,
      isActive: true
    }, { emitEvent: false });
    this.form.markAsPristine();
    this.openEditor();
  }

  edit(row: CfaOperationalTransactionRestriction): void {
    if (!this.canManage) return;
    this.editing = row;
    const house = this.houseFor(row.clearingHouseCode);
    if (house) {
      this.loadEditorCycles(house.id).pipe(take(1)).subscribe({
        next: cycles => {
          this.editorCycles = cycles;
          this.populateEditor(row);
          this.openEditor();
        },
        error: error => this.notifyError(error)
      });
      return;
    }

    this.editorCycles = [];
    this.populateEditor(row);
    this.openEditor();
  }

  view(row: CfaOperationalTransactionRestriction): void {
    this.api.get(row.id).pipe(take(1)).subscribe({
      next: detail => {
        this.detail = detail;
        this.dialog.open(this.detailDialog, {
          width: 'min(780px, calc(100vw - 1.5rem))',
          autoFocus: 'dialog'
        }).afterClosed().subscribe(() => {
          this.detail = null;
          this.cdr.markForCheck();
        });
        this.cdr.markForCheck();
      },
      error: error => this.notifyError(error)
    });
  }

  save(): void {
    if (!this.canManage) return;
    this.form.markAllAsTouched();
    if (this.form.invalid) return;

    const payload = this.buildSaveRequest();
    this.saving = true;
    const operation = this.editing
      ? this.api.update(this.editing.id, { ...payload, expectedRowVersion: this.editing.rowVersion })
      : this.api.create(payload);

    operation.pipe(finalize(() => {
      this.saving = false;
      this.cdr.markForCheck();
    })).subscribe({
      next: saved => {
        this.editorRef?.close();
        this.snackBar.open(this.editing ? 'Restricción actualizada.' : 'Restricción creada.', 'Cerrar', { duration: 3500 });
        this.editing = null;
        this.load();
        void saved;
      },
      error: error => {
        if (this.isConcurrencyConflict(error) && this.editing) {
          this.handleEditorConcurrency(this.editing.id, error);
          return;
        }
        this.notifyError(error);
      }
    });
  }

  changeStatus(row: CfaOperationalTransactionRestriction): void {
    if (!this.canManage) return;
    const activating = !row.isActive;
    const data: OperationalConfirmDialogData = {
      title: activating ? 'Activar restricción' : 'Desactivar restricción',
      message: activating
        ? 'La restricción volverá a participar en la resolución operacional CFA dentro de su vigencia.'
        : 'La restricción dejará de participar en la resolución operacional CFA. No cambia ninguna política regulatoria.',
      confirmLabel: activating ? 'Activar' : 'Desactivar',
      icon: activating ? 'play_circle' : 'pause_circle',
      destructive: !activating
    };

    this.dialog.open(OperationalConfirmDialogComponent, { data, width: 'min(520px, calc(100vw - 1.5rem))' })
      .afterClosed().pipe(take(1)).subscribe(confirmed => {
        if (!confirmed) return;
        const operation = activating
          ? this.api.activate(row.id, { expectedRowVersion: row.rowVersion })
          : this.api.deactivate(row.id, { expectedRowVersion: row.rowVersion });
        operation.subscribe({
          next: () => {
            this.snackBar.open(activating ? 'Restricción activada.' : 'Restricción desactivada.', 'Cerrar', { duration: 3500 });
            this.load();
          },
          error: error => {
            if (this.isConcurrencyConflict(error)) {
              this.showReloadSnack('La restricción cambió mientras realizabas la operación.');
              return;
            }
            this.notifyError(error);
          }
        });
      });
  }

  scopeLabel(row: CfaOperationalTransactionRestriction): string {
    if (row.clearingHouseCycleConfigId != null) return 'Ciclo';
    if (row.clearingHouseCode) return 'Cámara';
    return 'Global';
  }

  scopeDetail(row: CfaOperationalTransactionRestriction): string {
    if (row.clearingHouseCycleConfigId != null) {
      return `${row.clearingHouseCode ?? '—'} · ${row.policyVersion ?? '—'} · ${row.cycleName ?? '—'}`;
    }
    return row.clearingHouseCode
      ? `${row.clearingHouseCode}${row.clearingHouseName ? ` · ${row.clearingHouseName}` : ''}`
      : 'Todas las cámaras';
  }

  transactionLabel(type?: TransactionTypeEnum | null): string {
    switch (type) {
      case TransactionTypeEnum.Credit: return 'Crédito';
      case TransactionTypeEnum.Debit: return 'Débito';
      case TransactionTypeEnum.Prenotification: return 'Prenotificación';
      case TransactionTypeEnum.Reversal: return 'Reverso';
      case TransactionTypeEnum.Return: return 'Devolución';
      default: return 'Cualquier tipo';
    }
  }

  prenotificationLabel(value?: boolean | null): string {
    return value == null ? 'Monetaria o prenotificación' : value ? 'Prenotificación' : 'Monetaria';
  }

  accountsLabel(value?: AccountTypeEnum[] | null): string {
    if (value == null) return 'Hereda';
    if (value.length === 0) return 'Ninguna · DENY';
    return value.map(item => this.accountTypeLabel(item)).join(', ');
  }

  accountTypeLabel(type: AccountTypeEnum): string {
    switch (type) {
      case AccountTypeEnum.Checking: return 'Corriente';
      case AccountTypeEnum.Savings: return 'Ahorros';
      case AccountTypeEnum.ElectronicDeposits: return 'Depósito electrónico';
      default: return String(type);
    }
  }

  validityLabel(row: CfaOperationalTransactionRestriction): string {
    const todayValue = formatDate(today())!;
    const from = row.effectiveFrom.slice(0, 10);
    const to = row.effectiveTo?.slice(0, 10) ?? null;
    if (from > todayValue) return 'Futura';
    if (to && to < todayValue) return 'Vencida';
    return 'Vigente';
  }

  dateLabel(value?: string | null): string {
    if (!value) return 'Sin fin';
    const [year, month, day] = value.slice(0, 10).split('-');
    return year && month && day ? `${day}/${month}/${year}` : value;
  }

  formatMoney(value?: number | null): string {
    return value == null ? 'Hereda' : new Intl.NumberFormat('es-CO', { style: 'currency', currency: 'COP', maximumFractionDigits: 0 }).format(value);
  }

  track(_: number, row: CfaOperationalTransactionRestriction): number {
    return row.id;
  }

  private buildFilters(): CfaOperationalTransactionRestrictionFilter {
    const value = this.filterForm.getRawValue();
    return {
      clearingHouseCode: value.clearingHouseCode,
      policyVersion: value.policyVersion.trim() || null,
      clearingHouseCycleConfigId: value.cycleConfigId,
      transactionType: value.transactionType,
      isPrenotification: toNullableBoolean(value.prenotification),
      isActive: toNullableBoolean(value.active),
      effectiveAt: formatDate(value.effectiveAt)
    };
  }

  private buildSaveRequest(): SaveCfaOperationalTransactionRestrictionRequest {
    const value = this.form.getRawValue();
    const clearingHouseCode = value.scope === 'global' ? null : value.clearingHouseCode;
    const cycleConfigId = value.scope === 'cycle' ? value.cycleConfigId : null;
    return {
      clearingHouseCode,
      clearingHouseCycleConfigId: cycleConfigId,
      transactionType: value.transactionType,
      isPrenotification: toNullableBoolean(value.isPrenotification),
      maxAmountPerTransaction: nullIfEmpty(value.maxAmountPerTransaction),
      maxAmountPerCycle: nullIfEmpty(value.maxAmountPerCycle),
      maxTransactionsPerCycle: nullIfEmpty(value.maxTransactionsPerCycle),
      allowedAccountTypes: value.accountTypesMode === 'inherit' ? null : this.selectedAccountTypes(),
      effectiveFrom: formatDate(value.effectiveFrom)!,
      effectiveTo: formatDate(value.effectiveTo),
      isActive: value.isActive
    };
  }

  selectedAccountTypes(): AccountTypeEnum[] {
    const result: AccountTypeEnum[] = [];
    if (this.form.controls.checking.value) result.push(AccountTypeEnum.Checking);
    if (this.form.controls.savings.value) result.push(AccountTypeEnum.Savings);
    if (this.form.controls.electronicDeposits.value) result.push(AccountTypeEnum.ElectronicDeposits);
    return result;
  }

  private populateEditor(row: CfaOperationalTransactionRestriction): void {
    const scope: RestrictionScope = row.clearingHouseCycleConfigId != null
      ? 'cycle'
      : row.clearingHouseCode
        ? 'clearingHouse'
        : 'global';
    const accounts = row.allowedAccountTypes;
    this.form.reset({
      scope,
      clearingHouseCode: row.clearingHouseCode ?? null,
      policyVersion: row.policyVersion ?? null,
      cycleConfigId: row.clearingHouseCycleConfigId ?? null,
      transactionType: row.transactionType ?? null,
      isPrenotification: fromNullableBoolean(row.isPrenotification),
      maxAmountPerTransaction: row.maxAmountPerTransaction ?? null,
      maxAmountPerCycle: row.maxAmountPerCycle ?? null,
      maxTransactionsPerCycle: row.maxTransactionsPerCycle ?? null,
      accountTypesMode: accounts == null ? 'inherit' : 'explicit',
      checking: accounts?.includes(AccountTypeEnum.Checking) ?? false,
      savings: accounts?.includes(AccountTypeEnum.Savings) ?? false,
      electronicDeposits: accounts?.includes(AccountTypeEnum.ElectronicDeposits) ?? false,
      effectiveFrom: parseDate(row.effectiveFrom),
      effectiveTo: parseDate(row.effectiveTo),
      isActive: row.isActive
    }, { emitEvent: false });
    this.form.markAsPristine();
  }

  private openEditor(): void {
    this.editorRef = this.dialog.open(this.editorDialog, {
      width: 'min(980px, calc(100vw - 1.5rem))',
      maxHeight: '92vh',
      autoFocus: 'dialog',
      disableClose: this.saving
    });
    this.editorRef.afterClosed().subscribe(() => {
      this.editorRef = undefined;
      this.editing = null;
      this.cdr.markForCheck();
    });
  }

  private onFilterHouseChanged(code: string | null): void {
    this.filterForm.controls.cycleConfigId.setValue(null, { emitEvent: false });
    this.filterCycles = [];
    const house = this.houseFor(code);
    if (!house) {
      this.filterForm.controls.cycleConfigId.disable({ emitEvent: false });
      this.cdr.markForCheck();
      return;
    }
    this.filterForm.controls.cycleConfigId.enable({ emitEvent: false });
    const fallback = this.filterCyclesFromRows(code);
    this.cyclesApi.getByClearingHouse({ clearingHouseId: house.id }).pipe(take(1)).subscribe({
      next: cycles => {
        this.filterCycles = cycles;
        this.cdr.markForCheck();
      },
      error: () => {
        this.filterCycles = fallback;
        this.cdr.markForCheck();
      }
    });
  }

  private onScopeChanged(scope: RestrictionScope): void {
    if (scope === 'global') {
      this.form.patchValue({ clearingHouseCode: null, policyVersion: null, cycleConfigId: null }, { emitEvent: false });
      this.editorCycles = [];
    } else if (scope === 'clearingHouse') {
      this.form.patchValue({ policyVersion: null, cycleConfigId: null }, { emitEvent: false });
    }
    this.form.updateValueAndValidity({ emitEvent: false });
    this.cdr.markForCheck();
  }

  private onEditorHouseChanged(code: string | null): void {
    this.form.patchValue({ policyVersion: null, cycleConfigId: null }, { emitEvent: false });
    this.editorCycles = [];
    const house = this.houseFor(code);
    if (!house) {
      this.form.updateValueAndValidity({ emitEvent: false });
      this.cdr.markForCheck();
      return;
    }
    this.loadEditorCycles(house.id).pipe(take(1)).subscribe({
      next: cycles => {
        this.editorCycles = cycles;
        this.form.updateValueAndValidity({ emitEvent: false });
        this.cdr.markForCheck();
      },
      error: error => this.notifyError(error)
    });
  }

  private ensureEditorCycleMatchesPolicy(policyVersion: string | null): void {
    const cycleId = this.form.controls.cycleConfigId.value;
    if (cycleId == null) return;
    const cycle = this.editorCycles.find(item => item.id === cycleId);
    if (!cycle || (policyVersion && cycle.policyVersion !== policyVersion)) {
      this.form.controls.cycleConfigId.setValue(null, { emitEvent: false });
      this.form.updateValueAndValidity({ emitEvent: false });
    }
  }

  private loadEditorCycles(clearingHouseId: number): Observable<ClearingHouseCycleConfigItem[]> {
    return this.cyclesApi.getByClearingHouse({ clearingHouseId });
  }

  private loadAllHouses(): Observable<ClearingHouse[]> {
    return this.housesApi.list('', null, 1).pipe(
      switchMap(first => {
        if (first.totalPages <= 1) return of(first.items);
        const remaining = Array.from({ length: first.totalPages - 1 }, (_, index) => this.housesApi.list('', null, index + 2));
        return forkJoin(remaining).pipe(map(pages => [first.items, ...pages.map(page => page.items)].flat()));
      })
    );
  }

  private mergeHouseOptionsFromRows(rows: CfaOperationalTransactionRestriction[]): void {
    const byCode = new Map(this.houses.map(house => [house.code.toUpperCase(), house]));
    for (const row of rows) {
      if (row.clearingHouseId == null || !row.clearingHouseCode) continue;
      const key = row.clearingHouseCode.toUpperCase();
      if (byCode.has(key)) continue;
      byCode.set(key, {
        id: row.clearingHouseId,
        code: row.clearingHouseCode,
        name: row.clearingHouseName ?? row.clearingHouseCode,
        originCode: '',
        isActive: true,
        timeZoneId: 'America/Bogota',
        holidayStrategy: '',
        requiresNachaProfile: false,
        activeCycleCount: 0,
        isReady: true,
        missingRequirements: [],
        createdAt: ''
      });
    }
    this.houses = [...byCode.values()].sort((a, b) => a.code.localeCompare(b.code));
  }

  private filterCyclesFromRows(code: string): ClearingHouseCycleConfigItem[] {
    const byId = new Map<number, ClearingHouseCycleConfigItem>();
    for (const row of this.dataSource.data) {
      if (row.clearingHouseCode?.toUpperCase() !== code.toUpperCase()
        || row.clearingHouseCycleConfigId == null
        || !row.policyVersion
        || !row.cycleName
        || row.clearingHouseId == null) continue;
      byId.set(row.clearingHouseCycleConfigId, {
        id: row.clearingHouseCycleConfigId,
        clearingHouseId: row.clearingHouseId,
        clearingHouseName: row.clearingHouseName,
        policyVersion: row.policyVersion,
        cycleName: row.cycleName,
        startTime: '', endTime: '', cutoffTime: '', outputReleaseTime: '',
        allowsMonetaryCredit: false, allowsMonetaryDebit: false,
        allowsCreditPrenotification: false, allowsDebitPrenotification: false,
        allowsReturn: false, allowsReturnOfReturn: false,
        isActive: row.isActive,
        effectiveFrom: row.effectiveFrom,
        effectiveTo: row.effectiveTo,
        isCurrent: true
      });
    }
    return [...byId.values()].sort((a, b) => a.policyVersion.localeCompare(b.policyVersion) || a.cycleName.localeCompare(b.cycleName));
  }

  private houseFor(code?: string | null): ClearingHouse | undefined {
    if (!code) return undefined;
    return this.houses.find(house => house.code.toUpperCase() === code.toUpperCase());
  }

  private handleEditorConcurrency(id: number, error: HttpErrorResponse): void {
    const ref = this.snackBar.open(
      getHttpErrorMessage(error, 'La restricción fue modificada por otro usuario.'),
      'Recargar',
      { duration: 9000 }
    );
    ref.onAction().pipe(take(1)).subscribe(() => {
      this.api.get(id).pipe(take(1)).subscribe({
        next: current => {
          this.editing = current;
          const house = this.houseFor(current.clearingHouseCode);
          const loadCycles = house ? this.loadEditorCycles(house.id) : of([] as ClearingHouseCycleConfigItem[]);
          loadCycles.pipe(take(1)).subscribe({
            next: cycles => {
              this.editorCycles = cycles;
              this.populateEditor(current);
              this.snackBar.open('Información actualizada. Revise los cambios antes de guardar.', 'Cerrar', { duration: 4500 });
              this.cdr.markForCheck();
            },
            error: loadError => this.notifyError(loadError)
          });
        },
        error: loadError => this.notifyError(loadError)
      });
    });
  }

  private showReloadSnack(message: string): void {
    const ref = this.snackBar.open(message, 'Recargar', { duration: 9000 });
    ref.onAction().pipe(take(1)).subscribe(() => this.load());
  }

  private isConcurrencyConflict(error: unknown): error is HttpErrorResponse {
    if (!(error instanceof HttpErrorResponse) || error.status !== 409) return false;
    const payload = error.error as { code?: string } | null;
    return payload?.code === 'CFA_OPERATIONAL_RESTRICTION_CONCURRENCY' || error.status === 409;
  }

  private notifyError(error: unknown): void {
    this.snackBar.open(getHttpErrorMessage(error), 'Cerrar', { duration: 6000 });
    this.cdr.markForCheck();
  }
}

function scopeValidator(control: AbstractControl): ValidationErrors | null {
  const scope = control.get('scope')?.value as RestrictionScope | undefined;
  const house = control.get('clearingHouseCode')?.value as string | null | undefined;
  const policy = control.get('policyVersion')?.value as string | null | undefined;
  const cycle = control.get('cycleConfigId')?.value as number | null | undefined;
  if ((scope === 'clearingHouse' || scope === 'cycle') && !house) return { houseRequired: true };
  if (scope === 'cycle' && !policy) return { policyRequired: true };
  if (scope === 'cycle' && cycle == null) return { cycleRequired: true };
  return null;
}

function dateRangeValidator(control: AbstractControl): ValidationErrors | null {
  const from = control.get('effectiveFrom')?.value as Date | null | undefined;
  const to = control.get('effectiveTo')?.value as Date | null | undefined;
  if (!from || !to) return null;
  return startOfDay(to).getTime() < startOfDay(from).getTime() ? { dateRange: true } : null;
}

function integerValidator(control: AbstractControl): ValidationErrors | null {
  const value = control.value;
  return value == null || value === '' || Number.isInteger(Number(value)) ? null : { integer: true };
}

function toNullableBoolean(value: TriState): boolean | null {
  return value === 'all' ? null : value === 'yes';
}

function fromNullableBoolean(value?: boolean | null): TriState {
  return value == null ? 'all' : value ? 'yes' : 'no';
}

function nullIfEmpty<T>(value: T | null | undefined): T | null {
  return value == null ? null : value;
}

function today(): Date {
  return startOfDay(new Date());
}


function startOfDay(value: Date): Date {
  return new Date(value.getFullYear(), value.getMonth(), value.getDate());
}

function parseDate(value?: string | null): Date | null {
  if (!value) return null;
  const datePart = value.slice(0, 10);
  const [year, month, day] = datePart.split('-').map(Number);
  if (!year || !month || !day) return null;
  return new Date(year, month - 1, day);
}

function formatDate(value?: Date | null): string | null {
  if (!value) return null;
  const year = value.getFullYear();
  const month = String(value.getMonth() + 1).padStart(2, '0');
  const day = String(value.getDate()).padStart(2, '0');
  return `${year}-${month}-${day}`;
}

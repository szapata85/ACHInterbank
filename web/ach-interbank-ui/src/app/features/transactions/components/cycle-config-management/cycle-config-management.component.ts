import { CommonModule } from '@angular/common';
import {
  ChangeDetectionStrategy,
  ChangeDetectorRef,
  Component,
  DestroyRef,
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
import { ActivatedRoute, RouterLink } from '@angular/router';
import { MatButtonModule } from '@angular/material/button';
import { MatCardModule } from '@angular/material/card';
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
import { MatSnackBar, MatSnackBarModule } from '@angular/material/snack-bar';
import { MatSort, MatSortModule } from '@angular/material/sort';
import { MatTableDataSource, MatTableModule } from '@angular/material/table';
import { MatTooltipModule } from '@angular/material/tooltip';
import { catchError, finalize, forkJoin, of, switchMap } from 'rxjs';
import { AuthService } from '../../../../core/services/auth.service';
import {
  ConfirmationDialogComponent,
  ConfirmationDialogData
} from '../../../clearing-houses/clearing-house-dialogs.component';
import { ClearingHouseContextNavigationComponent } from '../../../clearing-houses/clearing-house-context-navigation.component';
import { ClearingHouse } from '../../../clearing-houses/clearing-houses.models';
import { ClearingHousesService } from '../../../clearing-houses/clearing-houses.service';
import { ClearingHouseCycleConfigsApiService } from '../../services/clearing-house-cycle-configs-api.service';
import {
  ClearingHouseCycleConfigItem,
  CycleStatusFilter,
  CycleValidityFilter,
  UpsertCycleConfigRequest
} from '../../transactions.models';

type CycleFilterForm = FormGroup<{
  cycleName: FormControl<string>;
  status: FormControl<CycleStatusFilter>;
  validity: FormControl<CycleValidityFilter>;
  effectiveAt: FormControl<Date | null>;
}>;

type CycleEditorForm = FormGroup<{
  cycleName: FormControl<string>;
  startTime: FormControl<string>;
  endTime: FormControl<string>;
  cutoffTime: FormControl<string>;
  effectiveFrom: FormControl<Date | null>;
}>;

@Component({
  selector: 'app-cycle-config-management',
  standalone: true,
  imports: [
    CommonModule,
    ReactiveFormsModule,
    RouterLink,
    ClearingHouseContextNavigationComponent,
    MatButtonModule,
    MatCardModule,
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
    MatSnackBarModule,
    MatSortModule,
    MatTableModule,
    MatTooltipModule
  ],
  templateUrl: './cycle-config-management.component.html',
  styleUrl: './cycle-config-management.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class CycleConfigManagementComponent {
  @ViewChild('cycleEditorDialog') cycleEditorDialog!: TemplateRef<unknown>;
  @ViewChild('cycleHistoryDialog') cycleHistoryDialog!: TemplateRef<unknown>;
  @ViewChild(MatSort) set tableSort(sort: MatSort | undefined) {
    if (sort) this.dataSource.sort = sort;
  }
  @ViewChild(MatPaginator) set tablePaginator(paginator: MatPaginator | undefined) {
    if (paginator) this.dataSource.paginator = paginator;
  }

  private readonly route = inject(ActivatedRoute);
  private readonly cycleApi = inject(ClearingHouseCycleConfigsApiService);
  private readonly housesApi = inject(ClearingHousesService);
  private readonly auth = inject(AuthService);
  private readonly dialog = inject(MatDialog);
  private readonly snackBar = inject(MatSnackBar);
  private readonly cdr = inject(ChangeDetectorRef);
  private readonly destroyRef = inject(DestroyRef);
  editorRef?: MatDialogRef<unknown>;

  readonly displayedColumns = ['cycle', 'window', 'cutoff', 'validity', 'state', 'updated', 'actions'];
  readonly dataSource = new MatTableDataSource<ClearingHouseCycleConfigItem>([]);
  readonly canManage = this.auth.hasPermission('ClearingHouses.ManageCycles');
  readonly canReadPolicies = this.auth.hasPermission(['Config.Read', 'Config.Manage', 'CanReadAch', 'CanManageAch']);
  readonly canReadCycles = this.auth.hasPermission(['ClearingHouses.View', 'ClearingHouses.ManageCycles']);
  readonly canReadSpecialDates = this.auth.hasPermission(['ClearingHouses.View', 'ClearingHouses.ManageSpecialDates']);

  readonly filterForm: CycleFilterForm = new FormGroup({
    cycleName: new FormControl('', { nonNullable: true }),
    status: new FormControl<CycleStatusFilter>('all', { nonNullable: true }),
    validity: new FormControl<CycleValidityFilter>('all', { nonNullable: true }),
    effectiveAt: new FormControl<Date | null>(today())
  });

  readonly form: CycleEditorForm = new FormGroup({
    cycleName: new FormControl('', {
      nonNullable: true,
      validators: [Validators.required, Validators.maxLength(60)]
    }),
    startTime: new FormControl('', {
      nonNullable: true,
      validators: [Validators.required, timeValidator]
    }),
    endTime: new FormControl('', {
      nonNullable: true,
      validators: [Validators.required, timeValidator]
    }),
    cutoffTime: new FormControl('', {
      nonNullable: true,
      validators: [Validators.required, timeValidator]
    }),
    effectiveFrom: new FormControl<Date | null>(today(), Validators.required)
  }, { validators: cycleWindowValidator });

  clearingHouse: ClearingHouse | null = null;
  allItems: ClearingHouseCycleConfigItem[] = [];
  loading = true;
  saving = false;
  error = '';
  editingSource: ClearingHouseCycleConfigItem | null = null;
  historyCycleName = '';
  private clearingHouseId = 0;

  constructor() {
    this.filterForm.valueChanges
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe(() => this.applyLocalFilters());

    this.route.paramMap.pipe(
      switchMap(params => {
        this.closeEditor();
        this.clearingHouse = null;
        this.allItems = [];
        this.dataSource.data = [];
        this.error = '';
        const id = Number(params.get('id'));
        if (!Number.isInteger(id) || id <= 0) {
          this.loading = false;
          this.error = 'La cámara indicada no es válida.';
          this.cdr.markForCheck();
          return of(null);
        }

        this.clearingHouseId = id;
        this.loading = true;
        this.cdr.markForCheck();
        return forkJoin({
          house: this.housesApi.get(id),
          cycles: this.cycleApi.getByClearingHouse({
            clearingHouseId: id,
            effectiveAt: this.apiDate(this.filterForm.controls.effectiveAt.value)
          })
        }).pipe(
          catchError(error => {
            this.error = this.loadError(error);
            return of(null);
          }),
          finalize(() => {
            this.loading = false;
            this.cdr.markForCheck();
          })
        );
      }),
      takeUntilDestroyed(this.destroyRef)
    ).subscribe(result => {
      if (!result) return;
      this.clearingHouse = result.house;
      this.allItems = result.cycles;
      this.applyLocalFilters();
      this.cdr.markForCheck();
    });
  }

  get totalCount(): number {
    return this.allItems.length;
  }

  get currentCount(): number {
    return this.allItems.filter(item => item.isActive && this.resolveValidity(item) === 'current').length;
  }

  get futureCount(): number {
    return this.allItems.filter(item => item.isActive && this.resolveValidity(item) === 'future').length;
  }

  get inactiveCount(): number {
    return this.allItems.filter(item => !item.isActive).length;
  }

  get nextWindow(): string {
    const next = this.allItems
      .filter(item => item.isActive && this.resolveValidity(item) !== 'expired')
      .sort((a, b) => `${a.effectiveFrom}${a.startTime}`.localeCompare(`${b.effectiveFrom}${b.startTime}`))[0];
    return next ? `${next.cycleName} · ${this.time(next.startTime)}–${this.time(next.endTime)}` : 'Sin ventana programada';
  }

  get historyItems(): ClearingHouseCycleConfigItem[] {
    return this.allItems
      .filter(item => item.cycleName === this.historyCycleName)
      .sort((a, b) => b.effectiveFrom.localeCompare(a.effectiveFrom));
  }

  applyLocalFilters(): void {
    const name = this.filterForm.controls.cycleName.value.trim().toLocaleLowerCase('es');
    const status = this.filterForm.controls.status.value;
    const validity = this.filterForm.controls.validity.value;
    this.dataSource.data = this.allItems.filter(item => {
      const matchesName = !name || item.cycleName.toLocaleLowerCase('es').includes(name);
      const matchesStatus = status === 'all' || (status === 'active' ? item.isActive : !item.isActive);
      const matchesValidity = validity === 'all' || this.resolveValidity(item) === validity;
      return matchesName && matchesStatus && matchesValidity;
    });
    this.dataSource.paginator?.firstPage();
    this.cdr.markForCheck();
  }

  clearFilters(): void {
    this.filterForm.reset({
      cycleName: '',
      status: 'all',
      validity: 'all',
      effectiveAt: today()
    });
  }

  reload(): void {
    if (!this.clearingHouseId) return;
    this.loading = true;
    this.error = '';
    this.cycleApi.getByClearingHouse({
      clearingHouseId: this.clearingHouseId,
      effectiveAt: this.apiDate(this.filterForm.controls.effectiveAt.value)
    }).pipe(finalize(() => {
      this.loading = false;
      this.cdr.markForCheck();
    })).subscribe({
      next: items => {
        this.allItems = items;
        this.applyLocalFilters();
      },
      error: error => {
        this.error = this.errorMessage(error, 'No fue posible consultar las configuraciones de ciclos.');
        this.snack(this.error, true);
      }
    });
  }

  openCreateForm(): void {
    if (!this.canManage) return;
    this.editingSource = null;
    this.form.reset({
      cycleName: '',
      startTime: '',
      endTime: '',
      cutoffTime: '',
      effectiveFrom: today()
    });
    this.openEditor();
  }

  createVersion(item: ClearingHouseCycleConfigItem): void {
    if (!this.canManage) return;
    this.editingSource = item;
    this.form.reset({
      cycleName: item.cycleName,
      startTime: this.time(item.startTime),
      endTime: this.time(item.endTime),
      cutoffTime: this.time(item.cutoffTime),
      effectiveFrom: today()
    });
    this.openEditor();
  }

  viewHistory(item: ClearingHouseCycleConfigItem): void {
    this.historyCycleName = item.cycleName;
    this.dialog.open(this.cycleHistoryDialog, { width: 'min(720px, calc(100vw - 2rem))', autoFocus: 'dialog' });
  }

  save(): void {
    if (this.saving || !this.canManage) return;
    this.form.markAllAsTouched();
    this.validateVersionConflicts();
    if (this.form.invalid) return;

    const value = this.form.getRawValue();
    const payload: UpsertCycleConfigRequest = {
      clearingHouseId: this.clearingHouseId,
      cycleName: value.cycleName.trim(),
      startTime: this.apiTime(value.startTime),
      endTime: this.apiTime(value.endTime),
      cutoffTime: this.apiTime(value.cutoffTime),
      effectiveFrom: `${this.apiDate(value.effectiveFrom)}T00:00:00Z`
    };

    this.saving = true;
    this.cycleApi.createVersion(payload).pipe(finalize(() => {
      this.saving = false;
      this.cdr.markForCheck();
    })).subscribe({
      next: () => {
        this.snack(this.editingSource ? 'Nueva versión creada correctamente.' : 'Configuración creada correctamente.');
        this.closeEditor();
        this.reload();
      },
      error: error => {
        const message = this.errorMessage(error, 'No fue posible guardar la configuración de ciclo.');
        this.applyBackendError(message);
        this.snack(message, true);
      }
    });
  }

  askInactivate(item: ClearingHouseCycleConfigItem): void {
    if (!this.canManage) return;
    const data: ConfirmationDialogData = {
      title: 'Inactivar configuración',
      message: `Se cerrará la vigencia de ${item.cycleName} con la fecha de hoy. El historial se conservará.`,
      confirmText: 'Sí, inactivar',
      icon: 'event_busy',
      destructive: true
    };
    this.dialog.open(ConfirmationDialogComponent, { data, width: 'min(520px, calc(100vw - 2rem))' })
      .afterClosed().subscribe(confirmed => {
        if (confirmed) this.inactivate(item);
      });
  }

  status(item: ClearingHouseCycleConfigItem): string {
    if (!item.isActive) return 'Inactiva';
    const validity = this.resolveValidity(item);
    if (validity === 'future') return 'Futura';
    if (validity === 'expired') return 'Histórica';
    return 'Vigente';
  }

  validityText(item: ClearingHouseCycleConfigItem): string {
    return `${this.dateText(item.effectiveFrom)} – ${item.effectiveTo ? this.dateText(item.effectiveTo) : 'sin cierre'}`;
  }

  time(value: string): string {
    return value?.slice(0, 5) ?? '';
  }

  track(_: number, item: ClearingHouseCycleConfigItem): number {
    return item.id;
  }

  private openEditor(): void {
    this.editorRef = this.dialog.open(this.cycleEditorDialog, {
      width: 'min(760px, calc(100vw - 1.5rem))',
      maxHeight: '92vh',
      autoFocus: 'first-tabbable'
    });
    this.editorRef.afterClosed().subscribe(() => {
      this.editingSource = null;
      this.editorRef = undefined;
      this.cdr.markForCheck();
    });
  }

  private closeEditor(): void {
    this.editorRef?.close();
    this.editorRef = undefined;
    this.editingSource = null;
  }

  private inactivate(item: ClearingHouseCycleConfigItem): void {
    this.saving = true;
    this.cycleApi.inactivate(item.id, { effectiveTo: `${this.apiDate(today())}T00:00:00Z` })
      .pipe(finalize(() => {
        this.saving = false;
        this.cdr.markForCheck();
      }))
      .subscribe({
        next: () => {
          this.snack('Configuración inactivada correctamente.');
          this.reload();
        },
        error: error => this.snack(this.errorMessage(error, 'No fue posible inactivar la configuración.'), true)
      });
  }

  private validateVersionConflicts(): void {
    const current = { ...(this.form.errors ?? {}) };
    delete current['duplicate'];
    delete current['overlap'];
    const name = this.form.controls.cycleName.value.trim().toLocaleLowerCase('es');
    const effectiveFrom = this.apiDate(this.form.controls.effectiveFrom.value);
    if (name && effectiveFrom) {
      const duplicate = this.allItems.some(item =>
        item.cycleName.trim().toLocaleLowerCase('es') === name &&
        item.effectiveFrom.split('T')[0] === effectiveFrom
      );
      if (duplicate) current['duplicate'] = true;

      const overlap = this.allItems.some(item =>
        !item.isActive &&
        item.cycleName.trim().toLocaleLowerCase('es') === name &&
        effectiveFrom >= item.effectiveFrom.split('T')[0] &&
        !!item.effectiveTo &&
        effectiveFrom <= item.effectiveTo.split('T')[0]
      );
      if (overlap) current['overlap'] = true;
    }
    this.form.setErrors(Object.keys(current).length ? current : null);
  }

  private applyBackendError(message: string): void {
    const lower = message.toLocaleLowerCase('es');
    if (lower.includes('traslape') || lower.includes('solap')) {
      this.form.setErrors({ ...(this.form.errors ?? {}), overlap: true });
    } else if (lower.includes('existe') || lower.includes('duplic')) {
      this.form.setErrors({ ...(this.form.errors ?? {}), duplicate: true });
    } else {
      this.form.setErrors({ ...(this.form.errors ?? {}), backend: message });
    }
    this.cdr.markForCheck();
  }

  private resolveValidity(item: ClearingHouseCycleConfigItem): CycleValidityFilter {
    const reference = this.apiDate(this.filterForm.controls.effectiveAt.value ?? today());
    const from = item.effectiveFrom.split('T')[0];
    const to = item.effectiveTo ? item.effectiveTo.split('T')[0] : null;
    if (from > reference) return 'future';
    if (to && to < reference) return 'expired';
    return 'current';
  }

  private dateText(value: string): string {
    const normalized = value.split('T')[0];
    const [year, month, day] = normalized.split('-').map(Number);
    return new Intl.DateTimeFormat('es-CO', { dateStyle: 'medium', timeZone: 'UTC' })
      .format(new Date(Date.UTC(year, month - 1, day)));
  }

  private apiTime(value: string): string {
    return value.length === 5 ? `${value}:00` : value;
  }

  private apiDate(value: Date | null): string {
    if (!value || Number.isNaN(value.getTime())) return '';
    const year = value.getFullYear();
    const month = `${value.getMonth() + 1}`.padStart(2, '0');
    const day = `${value.getDate()}`.padStart(2, '0');
    return `${year}-${month}-${day}`;
  }

  private loadError(error: { status?: number }): string {
    if (error?.status === 403) return 'No tiene permisos para consultar los ciclos de esta cámara.';
    if (error?.status === 404) return 'La cámara solicitada no existe o ya no está disponible.';
    return 'No fue posible cargar la cámara y sus configuraciones de ciclos.';
  }

  private errorMessage(error: any, fallback: string): string {
    const message = error?.error?.message ?? error?.error?.title ?? error?.message;
    return typeof message === 'string' && message.trim() ? message : fallback;
  }

  private snack(message: string, error = false): void {
    this.snackBar.open(message, 'Cerrar', {
      duration: error ? 7000 : 4500,
      panelClass: error ? ['snackbar-error'] : undefined
    });
  }
}

function today(): Date {
  const now = new Date();
  return new Date(now.getFullYear(), now.getMonth(), now.getDate());
}

function timeValidator(control: AbstractControl<string>): ValidationErrors | null {
  return !control.value || /^([01]\d|2[0-3]):[0-5]\d$/.test(control.value) ? null : { timeFormat: true };
}

function cycleWindowValidator(control: AbstractControl): ValidationErrors | null {
  const start = control.get('startTime')?.value as string;
  const end = control.get('endTime')?.value as string;
  const cutoff = control.get('cutoffTime')?.value as string;
  if (!start || !end || !cutoff || !/^([01]\d|2[0-3]):[0-5]\d$/.test(start + '') ||
      !/^([01]\d|2[0-3]):[0-5]\d$/.test(end + '') || !/^([01]\d|2[0-3]):[0-5]\d$/.test(cutoff + '')) {
    return null;
  }
  if (start >= end) return { timeOrder: true };
  if (cutoff < start || cutoff > end) return { cutoffOutside: true };
  return null;
}

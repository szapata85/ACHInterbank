import { CommonModule } from '@angular/common';
import { ChangeDetectorRef, Component, HostListener, OnInit, inject } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { forkJoin, finalize } from 'rxjs';
import { MatButtonModule } from '@angular/material/button';
import { MatCardModule } from '@angular/material/card';
import { MatCheckboxModule } from '@angular/material/checkbox';
import { MatChipsModule } from '@angular/material/chips';
import { MatDialog, MatDialogModule } from '@angular/material/dialog';
import { MatDividerModule } from '@angular/material/divider';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatIconModule } from '@angular/material/icon';
import { MatInputModule } from '@angular/material/input';
import { MatMenuModule } from '@angular/material/menu';
import { PageEvent, MatPaginatorModule } from '@angular/material/paginator';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatSelectModule } from '@angular/material/select';
import { MatSnackBar, MatSnackBarModule } from '@angular/material/snack-bar';
import { Sort, MatSortModule } from '@angular/material/sort';
import { MatTableModule } from '@angular/material/table';
import { MatTooltipModule } from '@angular/material/tooltip';
import { AuthService } from '../../core/services/auth.service';
import { ConfirmationDialogComponent, ConfirmationDialogData } from './clearing-house-dialogs.component';
import { ClearingHouse, ClearingHouseInput, NachaProfileOption, PaymentRailOption } from './clearing-houses.models';
import { ClearingHousesService } from './clearing-houses.service';

type StateFilter = 'all' | 'active' | 'inactive';

@Component({
  selector: 'app-clearing-houses',
  standalone: true,
  imports: [
    CommonModule,
    ReactiveFormsModule,
    RouterLink,
    MatButtonModule,
    MatCardModule,
    MatCheckboxModule,
    MatChipsModule,
    MatDialogModule,
    MatDividerModule,
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
  templateUrl: './clearing-houses.component.html',
  styleUrl: './clearing-houses.component.scss'
})
export class ClearingHousesComponent implements OnInit {
  private readonly api = inject(ClearingHousesService);
  private readonly auth = inject(AuthService);
  private readonly fb = inject(FormBuilder);
  private readonly cdr = inject(ChangeDetectorRef);
  private readonly dialog = inject(MatDialog);
  private readonly snackBar = inject(MatSnackBar);

  readonly displayedColumns = ['code', 'name', 'state', 'availability', 'updated', 'configuration', 'actions'];
  rows: ClearingHouse[] = [];
  profiles: NachaProfileOption[] = [];
  paymentRailOptions: PaymentRailOption[] = [];
  selected?: ClearingHouse;
  loading = false;
  saving = false;
  error = '';
  totalCount = 0;
  activeCount = 0;
  page = 1;
  pageSize = 20;
  editing = false;
  sort: Sort = { active: 'code', direction: 'asc' };

  readonly canCreate = this.auth.hasPermission('ClearingHouses.Create');
  readonly canReadPolicies = this.auth.hasPermission(['Config.Read', 'Config.Manage', 'CanReadAch', 'CanManageAch']);
  readonly canUpdate = this.auth.hasPermission('ClearingHouses.Update');
  readonly canStatus = this.auth.hasPermission('ClearingHouses.ChangeStatus');
  readonly canCycles = this.auth.hasPermission('ClearingHouses.ManageCycles');
  readonly canSpecialDates = this.auth.hasPermission('ClearingHouses.ManageSpecialDates');

  readonly filterForm = this.fb.nonNullable.group({
    search: [''],
    state: ['all' as StateFilter]
  });

  readonly form = this.fb.nonNullable.group({
    code: ['', [Validators.required, Validators.pattern(/^[A-Z0-9][A-Z0-9_-]{1,19}$/)]],
    name: ['', [Validators.required, Validators.maxLength(120)]],
    originCode: ['', [Validators.required, Validators.maxLength(20)]],
    timeZoneId: ['America/Bogota', Validators.required],
    holidayStrategy: ['Colombian', Validators.required],
    paymentRailCode: [null as string | null],
    requiresNachaProfile: [false],
    nachaProfileId: [null as number | null]
  });

  get sortedRows(): ClearingHouse[] {
    const direction = this.sort.direction === 'desc' ? -1 : 1;
    const active = this.sort.active;
    return [...this.rows].sort((a, b) => {
      const left = this.sortValue(a, active);
      const right = this.sortValue(b, active);
      return left < right ? -direction : left > right ? direction : 0;
    });
  }

  ngOnInit(): void {
    this.loadPaymentRailOptions();
    this.load();
  }

  loadPaymentRailOptions(): void {
    this.api.paymentRailOptions().subscribe({
      next: options => {
        this.paymentRailOptions = options.filter(option => option.code.toUpperCase() !== 'UNKNOWN');
        this.cdr.markForCheck();
      },
      error: error => {
        this.error = this.errorText(error);
        this.cdr.markForCheck();
      }
    });
  }

  load(): void {
    this.loading = true;
    this.error = '';
    const filters = this.filterForm.getRawValue();
    const active = filters.state === 'all' ? null : filters.state === 'active';
    forkJoin({
      page: this.api.list(filters.search, active, this.page),
      active: this.api.list('', true, 1)
    }).pipe(finalize(() => {
      this.loading = false;
      this.cdr.markForCheck();
    })).subscribe({
      next: result => {
        this.rows = result.page.items;
        this.totalCount = result.page.totalCount;
        this.activeCount = result.active.totalCount;
        this.pageSize = result.page.pageSize;
        this.cdr.markForCheck();
      },
      error: error => {
        this.rows = [];
        this.error = this.errorText(error);
        this.cdr.markForCheck();
      }
    });
  }

  applyFilters(): void {
    this.page = 1;
    this.load();
  }

  clearFilters(): void {
    this.filterForm.reset({ search: '', state: 'all' });
    this.applyFilters();
  }

  pageChanged(event: PageEvent): void {
    this.page = event.pageIndex + 1;
    this.load();
  }

  sortChanged(sort: Sort): void {
    this.sort = sort.direction ? sort : { active: 'code', direction: 'asc' };
  }

  normalizeCode(): void {
    this.form.controls.code.setValue(this.form.controls.code.value.trim().toUpperCase());
  }

  create(): void {
    this.selected = undefined;
    this.editing = true;
    this.error = '';
    this.form.reset({
      code: '',
      name: '',
      originCode: '',
      timeZoneId: 'America/Bogota',
      holidayStrategy: 'Colombian',
      paymentRailCode: null,
      requiresNachaProfile: false,
      nachaProfileId: null
    });
    this.setPaymentRailControlState(false);
  }

  edit(row: ClearingHouse): void {
    this.selected = row;
    this.editing = true;
    this.error = '';
    this.form.reset({
      code: row.code,
      name: row.name,
      originCode: row.originCode,
      timeZoneId: row.timeZoneId,
      holidayStrategy: row.holidayStrategy,
      paymentRailCode: row.paymentRailCode ?? null,
      requiresNachaProfile: row.requiresNachaProfile,
      nachaProfileId: row.nachaProfileId ?? null
    });
    this.setPaymentRailControlState(row.isActive);
    this.loadProfiles();
  }

  view(row: ClearingHouse): void {
    this.selected = row;
    this.editing = false;
    this.error = '';
  }

  loadProfiles(): void {
    this.normalizeCode();
    if (!this.form.controls.code.valid) {
      this.profiles = [];
      return;
    }
    this.api.profiles(this.form.controls.code.value).subscribe({
      next: profiles => {
        this.profiles = profiles;
        this.cdr.markForCheck();
      }
    });
  }

  save(): void {
    this.normalizeCode();
    this.form.markAllAsTouched();
    if (this.form.invalid || this.saving) {
      return;
    }
    this.saving = true;
    this.error = '';
    const input: ClearingHouseInput = { ...this.form.getRawValue(), expectedUpdatedAt: this.selected?.updatedAt };
    const request = this.selected ? this.api.update(this.selected.id, input) : this.api.create(input);
    request.pipe(finalize(() => {
      this.saving = false;
      this.cdr.markForCheck();
    })).subscribe({
      next: row => {
        this.selected = row;
        this.editing = false;
        this.form.markAsPristine();
        this.snackBar.open('Cámara compensadora guardada correctamente.', 'Cerrar', { duration: 4500 });
        this.load();
      },
      error: error => {
        this.error = this.errorText(error);
        this.cdr.markForCheck();
      }
    });
  }

  cancel(): void {
    if (!this.form.dirty) {
      this.editing = false;
      return;
    }
    this.confirm({
      title: 'Descartar cambios',
      message: 'Hay cambios sin guardar. Si continúa, se perderán los datos del formulario.',
      confirmText: 'Descartar cambios',
      icon: 'edit_off',
      destructive: true
    }).subscribe(confirmed => {
      if (confirmed) {
        this.editing = false;
        this.form.markAsPristine();
        this.cdr.markForCheck();
      }
    });
  }

  changeStatus(row: ClearingHouse): void {
    const action = row.isActive ? 'desactivar' : 'activar';
    this.confirm({
      title: `${row.isActive ? 'Desactivar' : 'Activar'} cámara`,
      message: `¿Desea ${action} ${row.name} (${row.code})? Este cambio afecta su disponibilidad operativa.`,
      confirmText: row.isActive ? 'Desactivar' : 'Activar',
      icon: row.isActive ? 'pause_circle' : 'play_circle',
      destructive: row.isActive
    }).subscribe(confirmed => {
      if (!confirmed) {
        return;
      }
      this.api.changeStatus(row.id, !row.isActive).subscribe({
        next: updated => {
          this.selected = updated;
          this.snackBar.open(updated.isActive ? 'Cámara activada.' : 'Cámara desactivada.', 'Cerrar', { duration: 4500 });
          this.load();
        },
        error: error => {
          this.error = this.errorText(error);
          this.cdr.markForCheck();
        }
      });
    });
  }

  @HostListener('window:beforeunload', ['$event'])
  protectChanges(event: BeforeUnloadEvent): void {
    if (this.editing && this.form.dirty) {
      event.preventDefault();
    }
  }

  private setPaymentRailControlState(isActive: boolean): void {
    const control = this.form.controls.paymentRailCode;
    if (isActive) {
      control.disable({ emitEvent: false });
    } else {
      control.enable({ emitEvent: false });
    }
  }

  private confirm(data: ConfirmationDialogData) {
    return this.dialog.open(ConfirmationDialogComponent, {
      data,
      width: 'min(92vw, 520px)',
      autoFocus: 'dialog',
      restoreFocus: true
    }).afterClosed();
  }

  private sortValue(row: ClearingHouse, active: string): string | number {
    switch (active) {
      case 'name': return row.name.toLocaleLowerCase();
      case 'state': return row.isActive ? 1 : 0;
      case 'availability': return row.isReady ? 1 : 0;
      case 'updated': return new Date(row.updatedAt || row.createdAt).getTime();
      default: return row.code.toLocaleLowerCase();
    }
  }

  private errorText(error: unknown): string {
    const value = error as {
      error?: { detail?: string; title?: string; missingRequirements?: string[]; errors?: Record<string, string[]> };
      message?: string;
    };
    const missing = value?.error?.missingRequirements;
    const validations = value?.error?.errors ? Object.values(value.error.errors).flat() : [];
    return [...(missing ?? []), ...validations].join(' ')
      || value?.error?.detail
      || value?.error?.title
      || value?.message
      || 'No fue posible completar la operación.';
  }
}

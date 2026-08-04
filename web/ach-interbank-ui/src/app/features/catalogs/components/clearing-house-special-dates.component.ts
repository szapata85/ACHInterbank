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
import { FormControl, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
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
import { catchError, finalize, forkJoin, of, switchMap, take } from 'rxjs';
import { AuthService } from '../../../core/services/auth.service';
import {
  ConfirmationDialogComponent,
  ConfirmationDialogData
} from '../../clearing-houses/clearing-house-dialogs.component';
import { ClearingHouseContextNavigationComponent } from '../../clearing-houses/clearing-house-context-navigation.component';
import { ClearingHouse } from '../../clearing-houses/clearing-houses.models';
import { ClearingHousesService } from '../../clearing-houses/clearing-houses.service';
import { BankHoliday } from '../models/bank-holiday.model';
import { ClearingHouseSpecialDate } from '../models/clearing-house-special-date.model';
import { BankHolidaysAdminService } from '../services/bank-holidays-admin.service';
import { ClearingHouseSpecialDatesService } from '../services/clearing-house-special-dates.service';

type SpecialDateStatusFilter = 'all' | 'active' | 'inactive';

@Component({
  selector: 'app-clearing-house-special-dates',
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
  templateUrl: './clearing-house-special-dates.component.html',
  styleUrl: './clearing-house-special-dates.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class ClearingHouseSpecialDatesComponent {
  @ViewChild('specialDateEditorDialog') editorDialog!: TemplateRef<unknown>;
  @ViewChild(MatSort) set tableSort(sort: MatSort | undefined) {
    if (sort) this.dataSource.sort = sort;
  }
  @ViewChild(MatPaginator) set tablePaginator(paginator: MatPaginator | undefined) {
    if (paginator) this.dataSource.paginator = paginator;
  }

  private readonly route = inject(ActivatedRoute);
  private readonly service = inject(ClearingHouseSpecialDatesService);
  private readonly houses = inject(ClearingHousesService);
  private readonly bankHolidays = inject(BankHolidaysAdminService);
  private readonly auth = inject(AuthService);
  private readonly dialog = inject(MatDialog);
  private readonly snackBar = inject(MatSnackBar);
  private readonly cdr = inject(ChangeDetectorRef);
  private readonly destroyRef = inject(DestroyRef);
  private readonly holidayCache = new Map<number, BankHoliday[]>();
  private editorRef?: MatDialogRef<unknown>;
  private clearingHouseId = 0;

  readonly displayedColumns = ['date', 'weekday', 'description', 'effect', 'state', 'updated', 'actions'];
  readonly dataSource = new MatTableDataSource<ClearingHouseSpecialDate>([]);
  readonly canManage = this.auth.hasPermission('ClearingHouses.ManageSpecialDates');
  readonly canReadPolicies = this.auth.hasPermission(['Config.Read', 'Config.Manage', 'CanReadAch', 'CanManageAch']);
  readonly canReadCycles = this.auth.hasPermission(['ClearingHouses.View', 'ClearingHouses.ManageCycles']);
  readonly canReadSpecialDates = this.auth.hasPermission(['ClearingHouses.View', 'ClearingHouses.ManageSpecialDates']);
  readonly years = buildYears();

  readonly filterForm = new FormGroup({
    year: new FormControl(new Date().getFullYear(), { nonNullable: true }),
    status: new FormControl<SpecialDateStatusFilter>('all', { nonNullable: true }),
    description: new FormControl('', { nonNullable: true })
  });

  readonly form = new FormGroup({
    date: new FormControl<Date | null>(null, Validators.required),
    description: new FormControl('', {
      nonNullable: true,
      validators: [Validators.required, Validators.maxLength(200)]
    })
  });

  clearingHouse: ClearingHouse | null = null;
  allItems: ClearingHouseSpecialDate[] = [];
  loading = true;
  saving = false;
  error = '';
  editing: ClearingHouseSpecialDate | null = null;
  dateWarning = '';

  constructor() {
    this.filterForm.controls.status.valueChanges
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe(() => this.applyLocalFilters());
    this.filterForm.controls.description.valueChanges
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe(() => this.applyLocalFilters());
    this.form.controls.date.valueChanges
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe(value => {
        this.dateWarning = '';
        if (!value || Number.isNaN(value.getTime())) return;
        this.clearFunctionalDateErrors();
        this.loadHolidays(value.getFullYear())
          .pipe(takeUntilDestroyed(this.destroyRef))
          .subscribe({
            next: holidays => {
              this.validateDate(value, holidays);
              this.cdr.markForCheck();
            },
            error: () => {
              this.dateWarning = 'No fue posible comprobar si la fecha coincide con un festivo nacional.';
              this.cdr.markForCheck();
            }
          });
      });

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
          house: this.houses.get(id),
          dates: this.service.list(this.filterForm.controls.year.value, id)
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
      this.allItems = result.dates;
      this.applyLocalFilters();
      this.cdr.markForCheck();
    });
  }

  get totalCount(): number {
    return this.allItems.length;
  }

  get activeCount(): number {
    return this.allItems.filter(item => item.isActive).length;
  }

  get upcomingCount(): number {
    const reference = this.apiDate(today());
    return this.allItems.filter(item => item.isActive && normalizeDate(item.date) >= reference).length;
  }

  reload(): void {
    if (!this.clearingHouseId) return;
    this.loading = true;
    this.error = '';
    this.service.list(this.filterForm.controls.year.value, this.clearingHouseId)
      .pipe(finalize(() => {
        this.loading = false;
        this.cdr.markForCheck();
      }))
      .subscribe({
        next: items => {
          this.allItems = items;
          this.applyLocalFilters();
        },
        error: error => {
          this.error = this.errorMessage(error, 'No fue posible consultar las fechas especiales.');
          this.snack(this.error, true);
        }
      });
  }

  applyLocalFilters(): void {
    const state = this.filterForm.controls.status.value;
    const description = this.filterForm.controls.description.value.trim().toLocaleLowerCase('es');
    this.dataSource.data = this.allItems.filter(item => {
      const matchesState = state === 'all' || (state === 'active' ? item.isActive : !item.isActive);
      const matchesDescription = !description || item.description.toLocaleLowerCase('es').includes(description);
      return matchesState && matchesDescription;
    });
    this.dataSource.paginator?.firstPage();
    this.cdr.markForCheck();
  }

  clearFilters(): void {
    this.filterForm.reset({
      year: new Date().getFullYear(),
      status: 'all',
      description: ''
    });
    this.reload();
  }

  startCreate(): void {
    if (!this.canManage) return;
    this.editing = null;
    this.dateWarning = '';
    this.form.reset({ date: null, description: '' });
    this.openEditor();
  }

  startEdit(item: ClearingHouseSpecialDate): void {
    if (!this.canManage) return;
    this.editing = item;
    this.dateWarning = item.calendarWarning ?? '';
    this.form.reset({
      date: parseLocalDate(item.date),
      description: item.description
    });
    this.openEditor();
  }

  cancelEdit(): void {
    this.closeEditor();
  }

  save(): void {
    if (this.saving || !this.canManage) return;
    this.form.markAllAsTouched();
    this.clearFunctionalDateErrors();
    if (this.form.invalid) return;
    const selectedDate = this.form.controls.date.value!;
    const year = selectedDate.getFullYear();

    this.loadHolidays(year).subscribe({
      next: holidays => {
        if (!this.validateDate(selectedDate, holidays)) {
          this.cdr.markForCheck();
          return;
        }
        this.persist();
      },
      error: error => this.snack(this.errorMessage(error, 'No fue posible validar el calendario bancario.'), true)
    });
  }

  askStatusChange(item: ClearingHouseSpecialDate): void {
    if (!this.canManage) return;
    const activating = !item.isActive;
    const data: ConfirmationDialogData = {
      title: activating ? 'Activar fecha especial' : 'Desactivar fecha especial',
      message: `${activating ? 'Se activará' : 'Se desactivará'} la fecha especial del ${this.dateText(item.date)}. El registro se conservará.`,
      confirmText: activating ? 'Sí, activar' : 'Sí, desactivar',
      icon: activating ? 'play_circle' : 'pause_circle',
      destructive: !activating
    };
    this.dialog.open(ConfirmationDialogComponent, { data, width: 'min(520px, calc(100vw - 2rem))' })
      .afterClosed().subscribe(confirmed => {
        if (confirmed) this.changeStatus(item);
      });
  }

  weekday(value: string): string {
    const date = parseLocalDate(value);
    return new Intl.DateTimeFormat('es-CO', { weekday: 'long' }).format(date);
  }

  dateText(value: string): string {
    return new Intl.DateTimeFormat('es-CO', { dateStyle: 'medium' }).format(parseLocalDate(value));
  }

  track(_: number, item: ClearingHouseSpecialDate): number {
    return item.id;
  }

  private openEditor(): void {
    this.editorRef = this.dialog.open(this.editorDialog, {
      width: 'min(620px, calc(100vw - 1.5rem))',
      maxHeight: '92vh',
      autoFocus: 'first-tabbable'
    });
    this.editorRef.afterClosed().subscribe(() => {
      this.editing = null;
      this.editorRef = undefined;
      this.cdr.markForCheck();
    });
  }

  private closeEditor(): void {
    this.editorRef?.close();
    this.editorRef = undefined;
    this.editing = null;
    this.dateWarning = '';
  }

  private persist(): void {
    const payload: ClearingHouseSpecialDate = {
      id: this.editing?.id ?? 0,
      clearingHouseId: this.clearingHouseId,
      clearingHouseName: this.clearingHouse?.name,
      date: this.apiDate(this.form.controls.date.value),
      description: this.form.controls.description.value.trim(),
      isActive: this.editing?.isActive ?? true
    };
    this.saving = true;
    const request = this.editing ? this.service.update(payload) : this.service.create(payload);
    request.pipe(finalize(() => {
      this.saving = false;
      this.cdr.markForCheck();
    })).subscribe({
      next: () => {
        this.snack(this.editing ? 'Fecha especial actualizada correctamente.' : 'Fecha especial creada correctamente.');
        this.closeEditor();
        const year = this.form.controls.date.value?.getFullYear();
        if (year && year !== this.filterForm.controls.year.value) {
          this.filterForm.controls.year.setValue(year);
        }
        this.reload();
      },
      error: error => {
        const message = this.errorMessage(error, 'No fue posible guardar la fecha especial.');
        if (message.toLocaleLowerCase('es').includes('existe') || message.toLocaleLowerCase('es').includes('duplic')) {
          this.setDateError('duplicateDate');
        } else {
          this.form.setErrors({ backend: message });
        }
        this.snack(message, true);
      }
    });
  }

  private changeStatus(item: ClearingHouseSpecialDate): void {
    this.saving = true;
    this.service.changeStatus(item.id, !item.isActive).pipe(finalize(() => {
      this.saving = false;
      this.cdr.markForCheck();
    })).subscribe({
      next: () => {
        this.snack(item.isActive ? 'Fecha especial desactivada.' : 'Fecha especial activada.');
        this.reload();
      },
      error: error => this.snack(this.errorMessage(error, 'No fue posible cambiar el estado.'), true)
    });
  }

  private loadHolidays(year: number) {
    const cached = this.holidayCache.get(year);
    if (cached) return of(cached);
    return this.bankHolidays.list(year).pipe(switchMap(items => {
      const holidays = items ?? [];
      this.holidayCache.set(year, holidays);
      return of(holidays);
    }));
  }

  private validateDate(value: Date, holidays: BankHoliday[]): boolean {
    const day = value.getDay();
    const warnings: string[] = [];
    if (day === 0 || day === 6) {
      warnings.push('La fecha ya corresponde a un sábado o domingo. Puede guardarla como antecedente propio de la cámara.');
    }
    const normalized = this.apiDate(value);
    if (holidays.some(holiday => normalizeDate(holiday.date) === normalized)) {
      warnings.push('La fecha ya corresponde a un festivo nacional. La configuración seguirá siendo independiente para esta cámara.');
    }
    if (this.allItems.some(item => item.id !== (this.editing?.id ?? 0) && normalizeDate(item.date) === normalized)) {
      this.setDateError('duplicateDate');
      return false;
    }
    this.dateWarning = warnings.join(' ');
    return true;
  }

  private clearFunctionalDateErrors(): void {
    const errors = { ...(this.form.controls.date.errors ?? {}) };
    delete errors['duplicateDate'];
    this.form.controls.date.setErrors(Object.keys(errors).length ? errors : null);
  }

  private setDateError(key: string): void {
    this.form.controls.date.setErrors({ ...(this.form.controls.date.errors ?? {}), [key]: true });
    this.form.controls.date.markAsTouched();
  }

  private apiDate(value: Date | null): string {
    if (!value || Number.isNaN(value.getTime())) return '';
    return `${value.getFullYear()}-${`${value.getMonth() + 1}`.padStart(2, '0')}-${`${value.getDate()}`.padStart(2, '0')}`;
  }

  private loadError(error: { status?: number }): string {
    if (error?.status === 403) return 'No tiene permisos para consultar las fechas especiales de esta cámara.';
    if (error?.status === 404) return 'La cámara solicitada no existe o ya no está disponible.';
    return 'No fue posible cargar la cámara y sus fechas especiales.';
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

@Component({
  selector: 'app-clearing-house-special-dates-legacy-redirect',
  standalone: true,
  imports: [MatProgressSpinnerModule],
  template: '<div class="legacy-redirect" role="status"><mat-spinner diameter="32"></mat-spinner><span>Abriendo fechas especiales…</span></div>',
  styles: ['.legacy-redirect { display:flex; justify-content:center; align-items:center; gap:1rem; min-height:12rem; }']
})
export class ClearingHouseSpecialDatesLegacyRedirectComponent implements OnInit {
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);

  ngOnInit(): void {
    this.route.queryParamMap.pipe(take(1)).subscribe(params => {
      const id = Number(params.get('clearingHouseId'));
      const commands = Number.isInteger(id) && id > 0
        ? ['/clearing-houses', id, 'special-dates']
        : ['/clearing-houses'];
      void this.router.navigate(commands, { replaceUrl: true });
    });
  }
}

function today(): Date {
  const now = new Date();
  return new Date(now.getFullYear(), now.getMonth(), now.getDate());
}

function normalizeDate(value: string): string {
  return value?.split('T')[0] ?? '';
}

function parseLocalDate(value: string): Date {
  const [year, month, day] = normalizeDate(value).split('-').map(Number);
  return new Date(year, month - 1, day);
}

function buildYears(): number[] {
  const current = new Date().getFullYear();
  return [current - 1, current, current + 1, current + 2];
}

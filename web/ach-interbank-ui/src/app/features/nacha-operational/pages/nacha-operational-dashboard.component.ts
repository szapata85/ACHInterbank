import { CommonModule } from '@angular/common';
import { ChangeDetectionStrategy, ChangeDetectorRef, Component, DestroyRef, OnInit, inject } from '@angular/core';
import { AbstractControl, FormBuilder, ReactiveFormsModule, ValidationErrors } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MatCardModule } from '@angular/material/card';
import { MatCheckboxModule } from '@angular/material/checkbox';
import { MatDatepickerIntl, MatDatepickerModule } from '@angular/material/datepicker';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatIconModule } from '@angular/material/icon';
import { MatInputModule } from '@angular/material/input';
import { MatNativeDateModule } from '@angular/material/core';
import { MatPaginatorModule, PageEvent } from '@angular/material/paginator';
import { MatProgressBarModule } from '@angular/material/progress-bar';
import { MatSelectModule } from '@angular/material/select';
import { MatSortModule, Sort } from '@angular/material/sort';
import { MatTableModule } from '@angular/material/table';
import { MatTooltipModule } from '@angular/material/tooltip';
import { ActivatedRoute, Params, Router, RouterModule } from '@angular/router';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { catchError, finalize, map, of, switchMap } from 'rxjs';
import { ClearingHousesService } from '../../clearing-houses/clearing-houses.service';
import { SpanishDatepickerIntl } from '../../../core/services/material-spanish-intl';
import { SharedModule } from '../../../shared/shared.module';
import {
  ClearingHouseOption,
  IncomingNachaBusinessOutcome,
  IncomingNachaFileFilters,
  IncomingNachaFileListItem,
  IncomingNachaObservabilitySummary
} from '../models/incoming-nacha-command-center.models';
import { operationalTone } from '../presentation/incoming-nacha-presentation';
import { IncomingNachaCommandCenterService } from '../services/incoming-nacha-command-center.service';

function validDateRange(control: AbstractControl): ValidationErrors | null {
  const from = control.get('uploadedFrom')?.value as Date | null;
  const to = control.get('uploadedTo')?.value as Date | null;
  return from && to && from > to ? { invalidDateRange: true } : null;
}

@Component({
  selector: 'app-nacha-operational-dashboard',
  standalone: true,
  imports: [
    CommonModule, ReactiveFormsModule, RouterModule, SharedModule, MatButtonModule, MatCardModule,
    MatCheckboxModule, MatDatepickerModule, MatFormFieldModule, MatIconModule, MatInputModule,
    MatNativeDateModule, MatPaginatorModule, MatProgressBarModule, MatSelectModule, MatSortModule,
    MatTableModule, MatTooltipModule
  ],
  templateUrl: './nacha-operational-dashboard.component.html',
  styleUrls: ['./nacha-operational-dashboard.component.scss'],
  providers: [{ provide: MatDatepickerIntl, useClass: SpanishDatepickerIntl }],
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class NachaOperationalDashboardComponent implements OnInit {
  private readonly api = inject(IncomingNachaCommandCenterService);
  private readonly clearingHousesApi = inject(ClearingHousesService);
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly fb = inject(FormBuilder);
  private readonly cdr = inject(ChangeDetectorRef);
  private readonly destroyRef = inject(DestroyRef);

  readonly displayedColumns = [
    'fileName', 'clearingHouse', 'operationalDate', 'cycle', 'uploadedAt', 'validation',
    'processing', 'counts', 'amounts', 'result', 'scheduledAt', 'actions'
  ];
  readonly pageSizes = [10, 20, 50];
  readonly ingestionStatuses = [
    { value: 'Recibido', label: 'Recibido' },
    { value: 'EnValidacion', label: 'Validando información' },
    { value: 'Parseado', label: 'Contenido interpretado' },
    { value: 'Completado', label: 'Carga completada' },
    { value: 'Bloqueado', label: 'Bloqueado' },
    { value: 'Fallido', label: 'Error técnico' },
    { value: 'Duplicado', label: 'Duplicado' }
  ];
  readonly businessOutcomes: Array<{ value: IncomingNachaBusinessOutcome; label: string }> = [
    { value: 'Successful', label: 'Exitoso' },
    { value: 'Rejected', label: 'Rechazado' },
    { value: 'Returned', label: 'Devuelto' },
    { value: 'PendingResponse', label: 'Pendiente de respuesta' },
    { value: 'NotProcessed', label: 'No procesado' }
  ];

  readonly filtersForm = this.fb.group({
    fileName: [''],
    clearingHouseId: [null as number | null],
    operationalDate: [null as Date | null],
    uploadedFrom: [null as Date | null],
    uploadedTo: [null as Date | null],
    achCycleId: [''],
    ingestionStatus: [''],
    businessOutcome: [''],
    resultCode: [''],
    hasIssues: [false],
    hasTechnicalErrors: [false]
  }, { validators: validDateRange });

  rows: IncomingNachaFileListItem[] = [];
  clearingHouses: ClearingHouseOption[] = [];
  summary?: IncomingNachaObservabilitySummary;
  totalItems = 0;
  pageIndex = 0;
  pageSize = 20;
  sortBy = 'uploadedAtUtc';
  sortDescending = true;
  loading = false;
  error = '';
  lastUpdatedAt?: Date;
  filtersExpanded = true;

  ngOnInit(): void {
    this.readFormFromQuery(this.route.snapshot.queryParams);
    this.loadClearingHouses();
    this.route.queryParamMap.pipe(
      map((params) => this.filtersFromParams(Object.fromEntries(params.keys.map((key) => [key, params.get(key)])))),
      switchMap((filters) => {
        this.loading = true;
        this.error = '';
        return this.api.getFiles(filters).pipe(
          catchError(() => {
            this.error = 'No fue posible consultar los archivos. Revise su conexión e intente nuevamente.';
            return of(null);
          }),
          finalize(() => {
            this.loading = false;
            this.cdr.markForCheck();
          })
        );
      }),
      takeUntilDestroyed(this.destroyRef)
    ).subscribe((page) => {
      if (!page) return;
      this.rows = page.items;
      this.totalItems = page.totalItems;
      this.pageIndex = page.page - 1;
      this.pageSize = page.pageSize;
      this.lastUpdatedAt = new Date();
      this.cdr.markForCheck();
    });

    this.loadSummary();
  }

  applyFilters(): void {
    this.filtersForm.markAllAsTouched();
    if (this.filtersForm.invalid) return;
    void this.navigateWithFilters(0);
  }

  clearFilters(): void {
    this.filtersForm.reset({
      fileName: '', clearingHouseId: null, operationalDate: null, uploadedFrom: null, uploadedTo: null,
      achCycleId: '', ingestionStatus: '', businessOutcome: '', resultCode: '', hasIssues: false,
      hasTechnicalErrors: false
    });
    this.sortBy = 'uploadedAtUtc';
    this.sortDescending = true;
    void this.router.navigate([], { relativeTo: this.route, queryParams: { page: 1, pageSize: this.pageSize }, replaceUrl: true });
  }

  refresh(): void {
    this.loadSummary();
    void this.router.navigate([], {
      relativeTo: this.route,
      queryParams: { actualizacion: Date.now() },
      queryParamsHandling: 'merge',
      replaceUrl: true
    });
  }

  pageChanged(event: PageEvent): void {
    this.pageSize = event.pageSize;
    void this.navigateWithFilters(event.pageIndex);
  }

  sortChanged(sort: Sort): void {
    this.sortBy = sort.active || 'uploadedAtUtc';
    this.sortDescending = sort.direction !== 'asc';
    void this.navigateWithFilters(0);
  }

  openFile(row: IncomingNachaFileListItem): void {
    void this.router.navigate(['files', row.id], {
      relativeTo: this.route,
      queryParams: { seccion: 'resumen', retorno: this.router.url }
    });
  }

  tone(value: string): string {
    return `status status--${operationalTone(value)}`;
  }

  private navigateWithFilters(pageIndex: number): Promise<boolean> {
    const value = this.filtersForm.getRawValue();
    const queryParams: Params = {
      page: pageIndex + 1,
      pageSize: this.pageSize,
      fileName: this.trim(value.fileName),
      clearingHouseId: value.clearingHouseId,
      operationalDate: this.dateOnly(value.operationalDate),
      uploadedFromUtc: this.startOfDay(value.uploadedFrom),
      uploadedToUtc: this.endOfDay(value.uploadedTo),
      achCycleId: this.trim(value.achCycleId),
      ingestionStatus: this.trim(value.ingestionStatus),
      businessOutcome: this.trim(value.businessOutcome),
      resultCode: this.trim(value.resultCode)?.toUpperCase(),
      hasIssues: value.hasIssues || null,
      hasTechnicalErrors: value.hasTechnicalErrors || null,
      sortBy: this.sortBy,
      sortDescending: this.sortDescending || null
    };
    return this.router.navigate([], { relativeTo: this.route, queryParams, replaceUrl: true });
  }

  private filtersFromParams(params: Params): IncomingNachaFileFilters {
    this.pageIndex = Math.max(0, Number(params['page'] ?? 1) - 1);
    this.pageSize = this.pageSizes.includes(Number(params['pageSize'])) ? Number(params['pageSize']) : 20;
    this.sortBy = params['sortBy'] ?? 'uploadedAtUtc';
    this.sortDescending = String(params['sortDescending'] ?? 'true') !== 'false';
    return {
      page: this.pageIndex + 1,
      pageSize: this.pageSize,
      fileName: params['fileName'] || undefined,
      clearingHouseId: params['clearingHouseId'] ? Number(params['clearingHouseId']) : undefined,
      operationalDate: params['operationalDate'] || undefined,
      uploadedFromUtc: params['uploadedFromUtc'] || undefined,
      uploadedToUtc: params['uploadedToUtc'] || undefined,
      achCycleId: params['achCycleId'] || undefined,
      ingestionStatus: params['ingestionStatus'] || undefined,
      businessOutcome: params['businessOutcome'] || undefined,
      resultCode: params['resultCode'] || undefined,
      hasIssues: params['hasIssues'] === 'true' ? true : undefined,
      hasTechnicalErrors: params['hasTechnicalErrors'] === 'true' ? true : undefined,
      sortBy: this.sortBy,
      sortDescending: this.sortDescending
    } as IncomingNachaFileFilters;
  }

  private readFormFromQuery(params: Params): void {
    this.filtersForm.patchValue({
      fileName: params['fileName'] ?? '',
      clearingHouseId: params['clearingHouseId'] ? Number(params['clearingHouseId']) : null,
      operationalDate: this.parseDate(params['operationalDate']),
      uploadedFrom: this.parseDate(params['uploadedFromUtc']),
      uploadedTo: this.parseDate(params['uploadedToUtc']),
      achCycleId: params['achCycleId'] ?? '',
      ingestionStatus: params['ingestionStatus'] ?? '',
      businessOutcome: params['businessOutcome'] ?? '',
      resultCode: params['resultCode'] ?? '',
      hasIssues: params['hasIssues'] === 'true',
      hasTechnicalErrors: params['hasTechnicalErrors'] === 'true'
    }, { emitEvent: false });
  }

  private loadSummary(): void {
    this.api.getSummary().pipe(catchError(() => of(undefined)), takeUntilDestroyed(this.destroyRef))
      .subscribe((summary) => {
        this.summary = summary;
        this.cdr.markForCheck();
      });
  }

  private loadClearingHouses(): void {
    this.clearingHousesApi.list('', true, 1).pipe(catchError(() => of({ items: [] })), takeUntilDestroyed(this.destroyRef))
      .subscribe((page) => {
        this.clearingHouses = page.items.map((item) => ({ id: item.id, name: item.name }));
        this.cdr.markForCheck();
      });
  }

  private trim(value: string | null | undefined): string | undefined {
    return value?.trim() || undefined;
  }

  private dateOnly(value: Date | null | undefined): string | undefined {
    if (!value) return undefined;
    const year = value.getFullYear();
    const month = String(value.getMonth() + 1).padStart(2, '0');
    const day = String(value.getDate()).padStart(2, '0');
    return `${year}-${month}-${day}`;
  }

  private startOfDay(value: Date | null | undefined): string | undefined {
    if (!value) return undefined;
    const date = new Date(value);
    date.setHours(0, 0, 0, 0);
    return date.toISOString();
  }

  private endOfDay(value: Date | null | undefined): string | undefined {
    if (!value) return undefined;
    const date = new Date(value);
    date.setHours(23, 59, 59, 999);
    return date.toISOString();
  }

  private parseDate(value: string | null | undefined): Date | null {
    return value ? new Date(value) : null;
  }
}

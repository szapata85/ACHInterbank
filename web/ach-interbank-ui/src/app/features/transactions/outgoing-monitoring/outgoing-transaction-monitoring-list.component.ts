import { CommonModule } from '@angular/common';
import { ChangeDetectionStrategy, Component, DestroyRef, OnInit, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule } from '@angular/forms';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { Router } from '@angular/router';
import { MatButtonModule } from '@angular/material/button';
import { MatCardModule } from '@angular/material/card';
import { MatDatepickerModule } from '@angular/material/datepicker';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatIconModule } from '@angular/material/icon';
import { MatInputModule } from '@angular/material/input';
import { MatNativeDateModule } from '@angular/material/core';
import { MatPaginatorModule, PageEvent } from '@angular/material/paginator';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatSelectModule } from '@angular/material/select';
import { MatSortModule, Sort } from '@angular/material/sort';
import { MatTableDataSource, MatTableModule } from '@angular/material/table';
import { Subject, catchError, finalize, of, switchMap } from 'rxjs';
import { OutgoingTransactionMonitoringApiService } from './outgoing-transaction-monitoring-api.service';
import { OutgoingMonitoringListItem, OutgoingMonitoringOption, OutgoingMonitoringPage, OutgoingMonitoringQuery } from './outgoing-transaction-monitoring.models';

@Component({
  selector: 'app-outgoing-transaction-monitoring-list',
  standalone: true,
  imports: [
    CommonModule, ReactiveFormsModule, MatButtonModule, MatCardModule, MatDatepickerModule, MatFormFieldModule,
    MatIconModule, MatInputModule, MatNativeDateModule, MatPaginatorModule, MatProgressSpinnerModule,
    MatSelectModule, MatSortModule, MatTableModule
  ],
  templateUrl: './outgoing-transaction-monitoring-list.component.html',
  styleUrl: './outgoing-transaction-monitoring-list.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class OutgoingTransactionMonitoringListComponent implements OnInit {
  private readonly fb = inject(FormBuilder);
  private readonly api = inject(OutgoingTransactionMonitoringApiService);
  private readonly router = inject(Router);
  private readonly destroyRef = inject(DestroyRef);
  private readonly searches = new Subject<OutgoingMonitoringQuery>();

  readonly displayedColumns = ['createdAt', 'identifier', 'clearingHouse', 'cycle', 'destination', 'amount', 'process', 'result', 'subsequent', 'updated', 'action'];
  readonly dataSource = new MatTableDataSource<OutgoingMonitoringListItem>([]);
  readonly pageSizes = [10, 25, 50, 100];
  readonly loading = signal(false);
  readonly error = signal(false);
  readonly page = signal<OutgoingMonitoringPage<OutgoingMonitoringListItem>>(emptyPage());
  readonly institutions = signal<OutgoingMonitoringOption[]>([]);
  readonly clearingHouses = signal<OutgoingMonitoringOption[]>([]);
  private currentPage = 1;
  private currentPageSize: 10 | 25 | 50 | 100 = 25;
  private currentSort: OutgoingMonitoringQuery['sortBy'] = 'createdAt';
  private currentDirection: 'asc' | 'desc' = 'desc';

  readonly form = this.fb.group({
    fromDate: [daysAgo(7)],
    toDate: [new Date()],
    clearingHouseId: [null as number | null],
    cycleId: [''],
    destinationInstitutionId: [null as number | null],
    transactionExternalId: [''],
    traceNumber: [''],
    transactionType: [null as number | null],
    processStatus: [''],
    initialResult: [''],
    subsequentSituation: [''],
    hasReturn: [null as boolean | null],
    requiresAttention: [null as boolean | null],
    minimumAmount: [null as number | null],
    maximumAmount: [null as number | null]
  });

  ngOnInit(): void {
    this.api.getDestinationInstitutions().pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: items => this.institutions.set(items ?? []),
      error: () => this.institutions.set([])
    });
    this.api.getClearingHouses().pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: items => this.clearingHouses.set(items ?? []),
      error: () => this.clearingHouses.set([])
    });
    this.searches.pipe(
      switchMap(query => {
        this.loading.set(true);
        this.error.set(false);
        return this.api.search(query).pipe(
        catchError(() => { this.error.set(true); return of(emptyPage(query.pageNumber, query.pageSize)); }),
        finalize(() => this.loading.set(false))
      );}),
      takeUntilDestroyed(this.destroyRef)
    ).subscribe(page => {
      this.page.set(page);
      this.dataSource.data = page.items;
    });
    this.search();
  }

  search(resetPage = true): void {
    if (resetPage) this.currentPage = 1;
    this.searches.next(this.buildQuery());
  }

  clearFilters(): void {
    this.form.reset({ fromDate: daysAgo(7), toDate: new Date() });
    this.currentPage = 1;
    this.currentPageSize = 25;
    this.currentSort = 'createdAt';
    this.currentDirection = 'desc';
    this.search(false);
  }

  pageChanged(event: PageEvent): void {
    this.currentPage = event.pageIndex + 1;
    this.currentPageSize = event.pageSize as 10 | 25 | 50 | 100;
    this.search(false);
  }

  sortChanged(event: Sort): void {
    const mapping: Record<string, OutgoingMonitoringQuery['sortBy']> = {
      createdAt: 'createdAt', amount: 'amount', identifier: 'identifier', updated: 'lastUpdatedAt'
    };
    this.currentSort = mapping[event.active] ?? 'createdAt';
    this.currentDirection = event.direction === 'asc' ? 'asc' : 'desc';
    this.search();
  }

  viewDetail(item: OutgoingMonitoringListItem): void {
    sessionStorage.setItem('outgoing-monitoring-filters', JSON.stringify(this.form.getRawValue()));
    void this.router.navigate(['/transactions/outgoing-monitoring', item.id]);
  }

  statusIcon(code: string): string {
    if (/Certified|Accepted|Successful|Processed/.test(code)) return 'check_circle';
    if (/Rejected|TechnicalError/.test(code)) return 'error';
    if (/Returned|Attention/.test(code)) return 'warning';
    return 'schedule';
  }

  trackById(_: number, item: OutgoingMonitoringListItem): number { return item.id; }

  private buildQuery(): OutgoingMonitoringQuery {
    const value = this.form.getRawValue();
    return compact({
      fromUtc: startOfDay(value.fromDate),
      toUtc: endOfDay(value.toDate),
      clearingHouseId: value.clearingHouseId ?? undefined,
      cycleId: value.cycleId?.trim() || undefined,
      destinationInstitutionId: value.destinationInstitutionId ?? undefined,
      transactionExternalId: value.transactionExternalId?.trim() || undefined,
      traceNumber: value.traceNumber?.trim() || undefined,
      transactionType: value.transactionType ?? undefined,
      processStatus: value.processStatus || undefined,
      initialResult: value.initialResult || undefined,
      subsequentSituation: value.subsequentSituation || undefined,
      hasReturn: value.hasReturn ?? undefined,
      requiresAttention: value.requiresAttention ?? undefined,
      minimumAmount: value.minimumAmount ?? undefined,
      maximumAmount: value.maximumAmount ?? undefined,
      pageNumber: this.currentPage,
      pageSize: this.currentPageSize,
      sortBy: this.currentSort,
      sortDirection: this.currentDirection
    }) as OutgoingMonitoringQuery;
  }
}

function daysAgo(days: number): Date { const date = new Date(); date.setDate(date.getDate() - days); return date; }
function startOfDay(value: Date | null | undefined): string | undefined { if (!value) return undefined; const date = new Date(value); date.setHours(0, 0, 0, 0); return date.toISOString(); }
function endOfDay(value: Date | null | undefined): string | undefined { if (!value) return undefined; const date = new Date(value); date.setHours(23, 59, 59, 999); return date.toISOString(); }
function compact<T extends object>(value: T): T { return Object.fromEntries(Object.entries(value).filter(([, item]) => item !== undefined && item !== '')) as T; }
function emptyPage(pageNumber = 1, pageSize = 25): OutgoingMonitoringPage<OutgoingMonitoringListItem> {
  return { items: [], pageNumber, pageSize, totalItems: 0, totalPages: 0, hasPreviousPage: false, hasNextPage: false };
}

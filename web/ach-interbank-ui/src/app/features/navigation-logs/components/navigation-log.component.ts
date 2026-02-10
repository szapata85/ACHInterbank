import { ChangeDetectionStrategy, ChangeDetectorRef, Component, OnDestroy, inject } from '@angular/core';
import { FormBuilder } from '@angular/forms';
import { Subject } from 'rxjs';
import { finalize, takeUntil } from 'rxjs/operators';
import { SharedModule } from '../../../shared/shared.module';
import { NavigationLogEntry, NavigationLogFilters } from '../models/navigation-log.model';
import { NavigationLogListService } from '../services/navigation-log-list.service';

interface NavigationLogRow extends NavigationLogEntry {
  visitedDateDisplay: string;
  visitedTimeDisplay: string;
  durationDisplay: string;
  userDisplay: string;
}

@Component({
  selector: 'app-navigation-log',
  standalone: true,
  imports: [SharedModule],
  templateUrl: './navigation-log.component.html',
  styleUrls: ['./navigation-log.component.css'],
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class NavigationLogComponent implements OnDestroy {
  private readonly service = inject(NavigationLogListService);
  private readonly fb = inject(FormBuilder);
  private readonly cdr = inject(ChangeDetectorRef);
  private readonly destroy$ = new Subject<void>();

  rows: NavigationLogRow[] = [];
  loading = false;
  total = 0;
  page = 1;
  pageSize = 20;
  hasSearched = false;

  readonly columns = [
    { key: 'visitedDateDisplay', label: 'Fecha' },
    { key: 'visitedTimeDisplay', label: 'Hora' },
    { key: 'userDisplay', label: 'Usuario' },
    { key: 'route', label: 'Ruta' },
    { key: 'durationDisplay', label: 'Duración' },
    { key: 'sessionId', label: 'Sesión' },
    { key: 'ipAddress', label: 'IP' },
    { key: 'userAgent', label: 'User agent' }
  ];

  filterForm = this.fb.nonNullable.group({
    startDate: [''],
    endDate: [''],
    userId: [''],
    route: ['']
  });

  ngOnDestroy(): void {
    this.destroy$.next();
    this.destroy$.complete();
  }

  search(page = 1): void {
    const { startDate, endDate, userId, route } = this.filterForm.getRawValue();

    const filters: NavigationLogFilters = {
      startDate: startDate || null,
      endDate: endDate || null,
      userId: userId || null,
      route: route || null,
      page,
      pageSize: this.pageSize
    };

    this.loading = true;
    this.hasSearched = true;
    this.cdr.markForCheck();

    this.service
      .search(filters)
      .pipe(
        takeUntil(this.destroy$),
        finalize(() => {
          this.loading = false;
          this.cdr.markForCheck();
        })
      )
      .subscribe((response) => {
        this.rows = response.items.map((item) => ({
          ...item,
          visitedDateDisplay: this.formatDate(item.visitedAt),
          visitedTimeDisplay: this.formatTime(item.visitedAt),
          durationDisplay: this.formatDuration(item.durationMs),
          userDisplay: this.formatUser(item)
        }));
        this.total = response.total;
        this.page = response.page;
      });
  }

  clear(): void {
    this.filterForm.reset({
      startDate: '',
      endDate: '',
      userId: '',
      route: ''
    });
    this.rows = [];
    this.total = 0;
    this.page = 1;
    this.hasSearched = false;
    this.cdr.markForCheck();
  }

  onPageChange(page: number): void {
    this.search(page);
  }

  private formatDate(value: string): string {
    const date = new Date(value);
    return Number.isNaN(date.getTime()) ? value : date.toLocaleDateString();
  }

  private formatTime(value: string): string {
    const date = new Date(value);
    return Number.isNaN(date.getTime()) ? value : date.toLocaleTimeString();
  }

  private formatUser(item: NavigationLogEntry): string {
    const username = (item.username ?? '').trim();
    if (username) {
      return username;
    }

    const userId = (item.userId ?? '').trim();
    return userId || '-';
  }

  private formatDuration(durationMs?: number | null): string {
    if (!durationMs || durationMs <= 0) {
      return '-';
    }

    const seconds = Math.floor(durationMs / 1000);
    if (seconds < 60) {
      return `${seconds}s`;
    }

    const minutes = Math.floor(seconds / 60);
    const remaining = seconds % 60;
    return `${minutes}m ${remaining}s`;
  }
}

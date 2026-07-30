import {
  ChangeDetectionStrategy,
  ChangeDetectorRef,
  Component,
  OnDestroy,
  inject
} from '@angular/core';
import { FormBuilder } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MatCardModule } from '@angular/material/card';
import { MatChipsModule } from '@angular/material/chips';
import { MatNativeDateModule } from '@angular/material/core';
import { MatDatepickerModule } from '@angular/material/datepicker';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatIconModule } from '@angular/material/icon';
import { MatInputModule } from '@angular/material/input';
import { MatPaginatorModule, PageEvent } from '@angular/material/paginator';
import { MatProgressBarModule } from '@angular/material/progress-bar';
import { MatSelectModule } from '@angular/material/select';
import { MatSortModule, Sort } from '@angular/material/sort';
import { MatTableModule } from '@angular/material/table';
import { MatTooltipModule } from '@angular/material/tooltip';
import { Subject } from 'rxjs';
import { finalize, takeUntil } from 'rxjs/operators';
import { SharedModule } from '../../../shared/shared.module';
import {
  auditDateRangeValidator,
  sanitizeAuditText,
  toLocalDateParam
} from '../../audit-shared/audit-display.util';
import { AuthLogEntry, AuthLogFilters } from '../models/auth-log.model';
import { AuthLogService } from '../services/auth-log.service';

interface AuthLogRow extends AuthLogEntry {
  loggedDateDisplay: string;
  loggedTimeDisplay: string;
  usernameDisplay: string;
  resultDisplay: string;
  resultIcon: string;
  failureReasonDisplay: string;
  ipAddressDisplay: string;
  userAgentDisplay: string;
}

@Component({
  selector: 'app-auth-log',
  standalone: true,
  imports: [
    SharedModule,
    MatButtonModule,
    MatCardModule,
    MatChipsModule,
    MatDatepickerModule,
    MatFormFieldModule,
    MatIconModule,
    MatInputModule,
    MatNativeDateModule,
    MatPaginatorModule,
    MatProgressBarModule,
    MatSelectModule,
    MatSortModule,
    MatTableModule,
    MatTooltipModule
  ],
  templateUrl: './auth-log.component.html',
  styleUrls: ['./auth-log.component.css'],
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class AuthLogComponent implements OnDestroy {
  private readonly service = inject(AuthLogService);
  private readonly fb = inject(FormBuilder);
  private readonly cdr = inject(ChangeDetectorRef);
  private readonly destroy$ = new Subject<void>();

  rows: AuthLogRow[] = [];
  loading = false;
  total = 0;
  page = 1;
  pageSize = 20;
  hasSearched = false;
  filterAttempted = false;
  errorMessage: string | null = null;
  sort: Sort = { active: 'loggedAt', direction: 'desc' };

  readonly displayedColumns = [
    'loggedAt',
    'loggedTime',
    'username',
    'success',
    'failureReason',
    'ipAddress',
    'userAgent'
  ];
  readonly pageSizeOptions = [10, 20, 50];
  readonly statusOptions = [
    { value: '', label: 'Todos' },
    { value: 'true', label: 'Exitoso' },
    { value: 'false', label: 'Fallido' }
  ];

  readonly filterForm = this.fb.nonNullable.group(
    {
      startDate: this.fb.control<Date | null>(null),
      endDate: this.fb.control<Date | null>(null),
      username: this.fb.nonNullable.control(''),
      success: this.fb.nonNullable.control('')
    },
    { validators: auditDateRangeValidator }
  );

  get showDateRangeError(): boolean {
    const controls = this.filterForm.controls;
    return (
      this.filterForm.hasError('dateRange') &&
      (this.filterAttempted || controls.startDate.touched || controls.endDate.touched)
    );
  }

  ngOnDestroy(): void {
    this.destroy$.next();
    this.destroy$.complete();
  }

  search(page = 1): void {
    if (this.loading) {
      return;
    }

    this.filterAttempted = true;
    if (this.filterForm.invalid) {
      this.filterForm.markAllAsTouched();
      this.cdr.markForCheck();
      return;
    }

    const { startDate, endDate, username, success } = this.filterForm.getRawValue();
    const parsedSuccess = success === '' ? null : success === 'true';
    const filters: AuthLogFilters = {
      startDate: toLocalDateParam(startDate),
      endDate: toLocalDateParam(endDate),
      username: username.trim() || null,
      success: parsedSuccess,
      page,
      pageSize: this.pageSize
    };

    this.loading = true;
    this.hasSearched = true;
    this.errorMessage = null;
    this.rows = [];
    this.total = 0;
    this.page = page;
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
      .subscribe({
        next: (response) => {
          const mappedRows = response.items.map((item) => ({
            ...item,
            loggedDateDisplay: this.formatDate(item.loggedAt),
            loggedTimeDisplay: this.formatTime(item.loggedAt),
            usernameDisplay: sanitizeAuditText(
              item.username,
              'Usuario no informado',
              80
            ),
            resultDisplay: item.success ? 'Exitoso' : 'Fallido',
            resultIcon: item.success ? 'check_circle' : 'cancel',
            failureReasonDisplay: item.success
              ? 'No aplica'
              : sanitizeAuditText(
                  item.failureReason,
                  'Sin detalle informado',
                  220
                ),
            ipAddressDisplay: sanitizeAuditText(item.ipAddress, 'No informada', 64),
            userAgentDisplay: sanitizeAuditText(
              item.userAgent,
              'No informado',
              180
            )
          }));

          this.rows = this.sortRows(mappedRows, this.sort);
          this.total = response.total;
          this.page = response.page > 0 ? response.page : page;
          this.pageSize = response.pageSize > 0 ? response.pageSize : this.pageSize;
          this.cdr.markForCheck();
        },
        error: () => {
          this.rows = [];
          this.total = 0;
          this.errorMessage =
            'No fue posible cargar el registro de autenticaciones. Intenta nuevamente.';
          this.cdr.markForCheck();
        }
      });
  }

  clear(): void {
    if (this.loading) {
      return;
    }

    this.filterForm.reset({
      startDate: null,
      endDate: null,
      username: '',
      success: ''
    });
    this.rows = [];
    this.total = 0;
    this.page = 1;
    this.hasSearched = false;
    this.filterAttempted = false;
    this.errorMessage = null;
    this.cdr.markForCheck();
  }

  onPageChange(event: PageEvent): void {
    this.pageSize = event.pageSize;
    this.search(event.pageIndex + 1);
  }

  onSortChange(sort: Sort): void {
    this.sort = sort;
    this.rows = this.sortRows(this.rows, sort);
    this.cdr.markForCheck();
  }

  retry(): void {
    this.search(this.page);
  }

  private formatDate(value: string): string {
    const date = new Date(value);
    return Number.isNaN(date.getTime())
      ? 'Fecha no disponible'
      : new Intl.DateTimeFormat('es-CO', { dateStyle: 'medium' }).format(date);
  }

  private formatTime(value: string): string {
    const date = new Date(value);
    return Number.isNaN(date.getTime())
      ? 'Hora no disponible'
      : new Intl.DateTimeFormat('es-CO', { timeStyle: 'short' }).format(date);
  }

  private sortRows(rows: AuthLogRow[], sort: Sort): AuthLogRow[] {
    if (!sort.direction) {
      return [...rows];
    }

    const direction = sort.direction === 'asc' ? 1 : -1;
    return [...rows].sort((left, right) => {
      const comparison = this.compareValues(
        this.getSortValue(left, sort.active),
        this.getSortValue(right, sort.active)
      );
      return comparison * direction;
    });
  }

  private getSortValue(row: AuthLogRow, key: string): string | number {
    switch (key) {
      case 'loggedAt':
      case 'loggedTime':
        return new Date(row.loggedAt).getTime() || 0;
      case 'username':
        return row.usernameDisplay;
      case 'success':
        return row.resultDisplay;
      case 'failureReason':
        return row.failureReasonDisplay;
      case 'ipAddress':
        return row.ipAddressDisplay;
      case 'userAgent':
        return row.userAgentDisplay;
      default:
        return '';
    }
  }

  private compareValues(left: string | number, right: string | number): number {
    if (typeof left === 'number' && typeof right === 'number') {
      return left - right;
    }

    return String(left).localeCompare(String(right), 'es', {
      sensitivity: 'base',
      numeric: true
    });
  }
}

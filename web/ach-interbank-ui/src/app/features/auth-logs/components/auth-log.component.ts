import { ChangeDetectionStrategy, ChangeDetectorRef, Component, OnDestroy, inject } from '@angular/core';
import { FormBuilder } from '@angular/forms';
import { Subject } from 'rxjs';
import { finalize, takeUntil } from 'rxjs/operators';
import { SharedModule } from '../../../shared/shared.module';
import { AuthLogEntry, AuthLogFilters } from '../models/auth-log.model';
import { AuthLogService } from '../services/auth-log.service';

interface AuthLogRow extends AuthLogEntry {
  loggedAtDisplay: string;
  successDisplay: string;
  failureReasonDisplay: string;
}

@Component({
  selector: 'app-auth-log',
  standalone: true,
  imports: [SharedModule],
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

  readonly columns = [
    { key: 'loggedAtDisplay', label: 'Fecha' },
    { key: 'username', label: 'Usuario' },
    { key: 'successDisplay', label: 'Resultado' },
    { key: 'failureReasonDisplay', label: 'Detalle' },
    { key: 'ipAddress', label: 'IP' },
    { key: 'userAgent', label: 'User agent' }
  ];

  readonly statusOptions = [
    { value: '', label: 'Todos' },
    { value: 'true', label: 'Exitoso' },
    { value: 'false', label: 'Fallido' }
  ];

  filterForm = this.fb.nonNullable.group({
    startDate: [''],
    endDate: [''],
    username: [''],
    success: ['']
  });

  ngOnDestroy(): void {
    this.destroy$.next();
    this.destroy$.complete();
  }

  search(page = 1): void {
    const { startDate, endDate, username, success } = this.filterForm.getRawValue();
    const parsedSuccess = success === '' ? null : success === 'true';

    const filters: AuthLogFilters = {
      startDate: startDate || null,
      endDate: endDate || null,
      username: username || null,
      success: parsedSuccess,
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
          loggedAtDisplay: item.loggedAt,
          successDisplay: item.success ? 'Exitoso' : 'Fallido',
          failureReasonDisplay: item.success ? '-' : item.failureReason || 'Sin detalle'
        }));
        this.total = response.total;
        this.page = response.page;
      });
  }

  clear(): void {
    this.filterForm.reset({
      startDate: '',
      endDate: '',
      username: '',
      success: ''
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
}

import { ChangeDetectionStrategy, ChangeDetectorRef, Component, OnDestroy, inject } from '@angular/core';
import { FormBuilder } from '@angular/forms';
import { Subject } from 'rxjs';
import { finalize, takeUntil } from 'rxjs/operators';
import { SharedModule } from '../../../shared/shared.module';
import { AuditLogEntry, AuditLogFilters } from '../models/audit-log.model';
import { AuditLogService } from '../services/audit-log.service';

interface AuditLogRow extends AuditLogEntry {
  changedAtDisplay: string;
  changedFieldsDisplay: string;
}

@Component({
  selector: 'app-audit-log',
  standalone: true,
  imports: [SharedModule],
  templateUrl: './audit-log.component.html',
  styles: [
    `
      .audit-log {
        display: flex;
        flex-direction: column;
        gap: 1.5rem;
      }

      .form-grid {
        display: grid;
        grid-template-columns: repeat(auto-fit, minmax(220px, 1fr));
        gap: 1rem;
        align-items: end;
        background: #ffffff;
        border: 1px solid #e5e7eb;
        border-radius: 8px;
        padding: 1.25rem;
      }

      .form-grid label {
        display: flex;
        flex-direction: column;
        gap: 0.4rem;
        font-weight: 600;
        color: #374151;
      }

      .form-grid input,
      .form-grid select {
        padding: 0.5rem 0.65rem;
        border-radius: 6px;
        border: 1px solid #d1d5db;
        font-weight: 500;
      }

      .form-actions {
        display: flex;
        gap: 0.75rem;
        align-items: center;
      }

      button.primary {
        background: #2563eb;
        border: none;
        color: #fff;
        padding: 0.55rem 1.1rem;
        border-radius: 6px;
        cursor: pointer;
      }

      button.ghost {
        background: transparent;
        border: 1px solid #d1d5db;
        color: #111827;
        padding: 0.55rem 1.1rem;
        border-radius: 6px;
        cursor: pointer;
      }

      .empty-state {
        text-align: center;
        color: #6b7280;
      }
    `
  ],
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class AuditLogComponent implements OnDestroy {
  private readonly service = inject(AuditLogService);
  private readonly fb = inject(FormBuilder);
  private readonly cdr = inject(ChangeDetectorRef);
  private readonly destroy$ = new Subject<void>();

  rows: AuditLogRow[] = [];
  loading = false;
  total = 0;
  page = 1;
  pageSize = 20;
  hasSearched = false;

  readonly columns = [
    { key: 'changedAtDisplay', label: 'Fecha' },
    { key: 'changedBy', label: 'Usuario' },
    { key: 'action', label: 'Acción' },
    { key: 'entityName', label: 'Entidad' },
    { key: 'entityId', label: 'ID entidad' },
    { key: 'changedFieldsDisplay', label: 'Campos modificados' }
  ];

  readonly actions = [
    { value: '', label: 'Todas' },
    { value: 'Added', label: 'Added' },
    { value: 'Modified', label: 'Modified' },
    { value: 'Deleted', label: 'Deleted' }
  ];

  filterForm = this.fb.nonNullable.group({
    startDate: [''],
    endDate: [''],
    changedBy: [''],
    action: ['']
  });

  ngOnDestroy(): void {
    this.destroy$.next();
    this.destroy$.complete();
  }

  search(page = 1): void {
    const { startDate, endDate, changedBy, action } = this.filterForm.getRawValue();

    const filters: AuditLogFilters = {
      startDate: startDate || null,
      endDate: endDate || null,
      changedBy: changedBy || null,
      action: action || null,
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
          changedAtDisplay: new Date(item.changedAt).toLocaleString(),
          changedFieldsDisplay: this.formatChangedFields(item.changedFields)
        }));
        this.total = response.total;
        this.page = response.page;
      });
  }

  clear(): void {
    this.filterForm.reset({
      startDate: '',
      endDate: '',
      changedBy: '',
      action: ''
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

  private formatChangedFields(changedFields?: string | null): string {
    if (!changedFields) {
      return '-';
    }

    try {
      const parsed = JSON.parse(changedFields) as string[];
      if (Array.isArray(parsed) && parsed.length > 0) {
        return parsed.join(', ');
      }
    } catch {
      return changedFields;
    }

    return '-';
  }
}

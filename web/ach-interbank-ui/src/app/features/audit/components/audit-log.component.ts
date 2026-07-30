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
  summarizeChangedFields,
  toLocalDateParam
} from '../../audit-shared/audit-display.util';
import { AuditLogEntry, AuditLogFilters } from '../models/audit-log.model';
import { AuditLogService } from '../services/audit-log.service';

interface AuditLogRow extends AuditLogEntry {
  changedAtDisplay: string;
  changedByDisplay: string;
  actionDisplay: string;
  actionIcon: string;
  actionClass: string;
  entityNameDisplay: string;
  entityIdDisplay: string;
  changedFieldsDisplay: string;
}

interface ActionPresentation {
  label: string;
  icon: string;
  cssClass: string;
}

@Component({
  selector: 'app-audit-log',
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
  templateUrl: './audit-log.component.html',
  styleUrls: ['./audit-log.component.css'],
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
  filterAttempted = false;
  errorMessage: string | null = null;
  sort: Sort = { active: 'changedAt', direction: 'desc' };

  readonly displayedColumns = [
    'changedAt',
    'changedBy',
    'action',
    'entityName',
    'entityId',
    'changedFields'
  ];
  readonly pageSizeOptions = [10, 20, 50];
  readonly actions = [
    { value: '', label: 'Todas' },
    { value: 'Added', label: 'Creado' },
    { value: 'Modified', label: 'Modificado' },
    { value: 'Deleted', label: 'Eliminado' }
  ];

  readonly filterForm = this.fb.nonNullable.group(
    {
      startDate: this.fb.control<Date | null>(null),
      endDate: this.fb.control<Date | null>(null),
      changedBy: this.fb.nonNullable.control(''),
      action: this.fb.nonNullable.control('')
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

    const { startDate, endDate, changedBy, action } = this.filterForm.getRawValue();
    const filters: AuditLogFilters = {
      startDate: toLocalDateParam(startDate),
      endDate: toLocalDateParam(endDate),
      changedBy: changedBy.trim() || null,
      action: action || null,
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
          const mappedRows = response.items.map((item) => {
            const actionPresentation = this.getActionPresentation(item.action);

            return {
              ...item,
              changedAtDisplay: this.formatDateTime(item.changedAt),
              changedByDisplay: sanitizeAuditText(
                item.changedBy,
                'Usuario no informado',
                80
              ),
              actionDisplay: actionPresentation.label,
              actionIcon: actionPresentation.icon,
              actionClass: actionPresentation.cssClass,
              entityNameDisplay: sanitizeAuditText(
                item.entityName,
                'Entidad no informada',
                100
              ),
              entityIdDisplay: sanitizeAuditText(
                item.entityId,
                'Sin identificador',
                80
              ),
              changedFieldsDisplay: summarizeChangedFields(item.changedFields)
            };
          });

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
            'No fue posible cargar el registro de auditoría. Intenta nuevamente.';
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
      changedBy: '',
      action: ''
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

  private getActionPresentation(action: string): ActionPresentation {
    switch (action.toLowerCase()) {
      case 'added':
        return {
          label: 'Creado',
          icon: 'add_circle',
          cssClass: 'status-chip--created'
        };
      case 'modified':
        return {
          label: 'Modificado',
          icon: 'edit',
          cssClass: 'status-chip--modified'
        };
      case 'deleted':
        return {
          label: 'Eliminado',
          icon: 'delete',
          cssClass: 'status-chip--deleted'
        };
      default:
        return {
          label: sanitizeAuditText(action, 'Otra acción', 48),
          icon: 'info',
          cssClass: 'status-chip--neutral'
        };
    }
  }

  private formatDateTime(value: string): string {
    const date = new Date(value);
    if (Number.isNaN(date.getTime())) {
      return 'Fecha no disponible';
    }

    return new Intl.DateTimeFormat('es-CO', {
      dateStyle: 'medium',
      timeStyle: 'short'
    }).format(date);
  }

  private sortRows(rows: AuditLogRow[], sort: Sort): AuditLogRow[] {
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

  private getSortValue(row: AuditLogRow, key: string): string | number {
    switch (key) {
      case 'changedAt':
        return new Date(row.changedAt).getTime() || 0;
      case 'changedBy':
        return row.changedByDisplay;
      case 'action':
        return row.actionDisplay;
      case 'entityName':
        return row.entityNameDisplay;
      case 'entityId':
        return row.entityIdDisplay;
      case 'changedFields':
        return row.changedFieldsDisplay;
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

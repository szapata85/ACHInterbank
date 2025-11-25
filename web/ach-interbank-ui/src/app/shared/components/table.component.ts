import { ChangeDetectionStrategy, Component, EventEmitter, Input, Output, TemplateRef } from '@angular/core';

export interface TableColumn {
  key: string;
  label: string;
  width?: string;
  align?: 'start' | 'center' | 'end';
}

@Component({
  selector: 'app-table',
  template: `
    <div class="table-wrapper" [class.loading]="loading">
      <div class="table-scroll">
        <table>
          <thead>
            <tr>
              <th *ngFor="let column of columns" [style.width]="column.width" [class.align-end]="column.align === 'end'">
                {{ column.label }}
              </th>
              <th *ngIf="rowActions">Acciones</th>
            </tr>
          </thead>
          <tbody>
            <tr *ngIf="!loading && data?.length === 0">
              <td [attr.colspan]="columns.length + (rowActions ? 1 : 0)" class="empty">No hay registros para mostrar</td>
            </tr>
            <tr *ngFor="let item of data; trackBy: trackByIndex">
              <td *ngFor="let column of columns" [class.align-end]="column.align === 'end'">
                {{ item[column.key] ?? '-' }}
              </td>
              <td *ngIf="rowActions">
                <ng-container *ngTemplateOutlet="rowActions; context: { $implicit: item }"></ng-container>
              </td>
            </tr>
            <tr *ngIf="loading">
              <td [attr.colspan]="columns.length + (rowActions ? 1 : 0)" class="loading-row">Cargando...</td>
            </tr>
          </tbody>
        </table>
      </div>

      <div class="pagination" *ngIf="total > pageSize">
        <button type="button" (click)="changePage(page - 1)" [disabled]="page <= 1">Anterior</button>
        <span>Página {{ page }} de {{ totalPages }}</span>
        <button type="button" (click)="changePage(page + 1)" [disabled]="page >= totalPages">Siguiente</button>
      </div>
    </div>
  `,
  styles: [
    `
      .table-wrapper {
        border: 1px solid #e5e7eb;
        border-radius: 8px;
        background: #fff;
        box-shadow: 0 1px 3px rgba(0, 0, 0, 0.05);
      }
      .table-scroll {
        overflow: auto;
      }
      table {
        width: 100%;
        border-collapse: collapse;
      }
      th,
      td {
        padding: 0.75rem 1rem;
        border-bottom: 1px solid #f3f4f6;
        text-align: left;
      }
      th {
        background: #f9fafb;
        font-weight: 600;
        color: #374151;
        white-space: nowrap;
      }
      .align-end {
        text-align: right;
      }
      .empty,
      .loading-row {
        text-align: center;
        color: #6b7280;
      }
      .pagination {
        display: flex;
        justify-content: space-between;
        align-items: center;
        padding: 0.75rem 1rem;
      }
      button {
        border: 1px solid #d1d5db;
        background: #fff;
        padding: 0.35rem 0.75rem;
        border-radius: 4px;
        cursor: pointer;
      }
      button:disabled {
        opacity: 0.6;
        cursor: not-allowed;
      }
    `
  ],
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class TableComponent {
  @Input() columns: TableColumn[] = [];
  @Input() data: any[] = [];
  @Input() loading = false;
  @Input() page = 1;
  @Input() pageSize = 10;
  @Input() total = 0;
  @Input() rowActions?: TemplateRef<any> | null;

  @Output() pageChange = new EventEmitter<number>();

  get totalPages(): number {
    return Math.max(1, Math.ceil(this.total / this.pageSize));
  }

  changePage(page: number): void {
    if (page < 1 || page > this.totalPages) {
      return;
    }
    this.pageChange.emit(page);
  }

  trackByIndex(index: number): number {
    return index;
  }
}

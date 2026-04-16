import { CommonModule } from '@angular/common';
import { ChangeDetectionStrategy, Component, ContentChild, EventEmitter, Input, Output, TemplateRef } from '@angular/core';
import { ColDef } from 'ag-grid-community';
import { TableRowActionsCellRendererComponent } from './table-row-actions-cell-renderer.component';
import { UiGrillaEmpresarialComponent } from './ui/ui-grilla-empresarial.component';

export interface TableColumn {
  key: string;
  label: string;
  width?: string;
  align?: 'start' | 'center' | 'end';
}

@Component({
  selector: 'app-table',
  standalone: true,
  imports: [CommonModule, UiGrillaEmpresarialComponent],
  template: `
    <ui-grilla-empresarial
      [columnas]="columnDefs"
      [datos]="data"
      [cargando]="loading"
      [error]="false"
      [paginacion]="total > pageSize"
      [tamanoPagina]="pageSize"
      [mensajeVacio]="'No hay registros para mostrar'"
      [mensajeCargando]="'Cargando información...'"
    ></ui-grilla-empresarial>
  `,
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
  @ContentChild('rowActions') rowActionsContenido?: TemplateRef<any>;

  @Output() pageChange = new EventEmitter<number>();

  get columnDefs(): ColDef[] {
    const defs: ColDef[] = this.columns.map((column) => ({
      field: column.key,
      headerName: column.label,
      width: column.width ? Number(column.width.replace('px', '')) : undefined,
      sortable: true,
      filter: true,
      cellStyle: column.align === 'end' ? { textAlign: 'right' } : undefined
    }));

    const templateAcciones = this.rowActions ?? this.rowActionsContenido ?? null;
    if (templateAcciones) {
      defs.push({
        colId: 'acciones',
        headerName: 'Acciones',
        minWidth: 220,
        width: 260,
        maxWidth: 340,
        sortable: false,
        filter: false,
        floatingFilter: false,
        cellRenderer: TableRowActionsCellRendererComponent,
        cellRendererParams: {
          template: templateAcciones
        }
      });
    }

    return defs;
  }
}

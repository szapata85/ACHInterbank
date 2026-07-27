import { TemplateRef } from '@angular/core';
import { TableComponent } from './table.component';
import { TableRowActionsCellRendererComponent } from './table-row-actions-cell-renderer.component';

describe('TableComponent', () => {
  it('mantiene visibles las acciones de fila en una columna fijada a la derecha', () => {
    const component = new TableComponent();
    component.columns = [{ key: 'status', label: 'Estado' }];
    component.rowActions = {} as TemplateRef<unknown>;

    const actionColumn = component.columnDefs.find(column => column.colId === 'acciones');

    expect(actionColumn).toBeDefined();
    expect(actionColumn?.headerName).toBe('Acciones');
    expect(actionColumn?.pinned).toBe('right');
    expect(actionColumn?.lockPinned).toBeTrue();
    expect(actionColumn?.cellRenderer).toBe(TableRowActionsCellRendererComponent);
  });

  it('no agrega la columna de acciones cuando la pantalla no provee plantilla', () => {
    const component = new TableComponent();
    component.columns = [{ key: 'status', label: 'Estado' }];

    expect(component.columnDefs.some(column => column.colId === 'acciones')).toBeFalse();
  });
});

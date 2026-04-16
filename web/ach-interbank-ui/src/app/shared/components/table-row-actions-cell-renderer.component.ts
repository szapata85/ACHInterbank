import { CommonModule } from '@angular/common';
import { Component, TemplateRef } from '@angular/core';
import { ICellRendererAngularComp } from 'ag-grid-angular';

@Component({
  selector: 'app-table-row-actions-cell-renderer',
  standalone: true,
  imports: [CommonModule],
  template: `
    <ng-container *ngIf="template" [ngTemplateOutlet]="template" [ngTemplateOutletContext]="{ $implicit: rowData }"></ng-container>
  `
})
export class TableRowActionsCellRendererComponent implements ICellRendererAngularComp {
  template: TemplateRef<any> | null = null;
  rowData: any;

  agInit(params: { data: any; template?: TemplateRef<any> | null }): void {
    this.rowData = params.data;
    this.template = params.template ?? null;
  }

  refresh(params: { data: any; template?: TemplateRef<any> | null }): boolean {
    this.rowData = params.data;
    this.template = params.template ?? null;
    return true;
  }
}

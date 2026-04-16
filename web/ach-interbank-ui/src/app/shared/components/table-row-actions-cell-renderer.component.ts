import { CommonModule } from '@angular/common';
import { Component, TemplateRef } from '@angular/core';
import { ICellRendererAngularComp } from 'ag-grid-angular';

@Component({
  selector: 'app-table-row-actions-cell-renderer',
  standalone: true,
  imports: [CommonModule],
  template: `
    <div class="acciones-renderer" (click)="detenerPropagacion($event)" (mousedown)="detenerPropagacion($event)">
      <ng-container *ngIf="template" [ngTemplateOutlet]="template" [ngTemplateOutletContext]="{ $implicit: rowData }"></ng-container>
    </div>
  `,
  styles: [`
    :host { display: block; }
    .acciones-renderer { display: flex; align-items: center; gap: .4rem; min-height: 100%; }
  `]
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

  detenerPropagacion(event: Event): void {
    event.stopPropagation();
  }
}

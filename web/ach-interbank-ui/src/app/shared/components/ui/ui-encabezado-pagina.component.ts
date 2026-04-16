import { CommonModule } from '@angular/common';
import { Component, Input, TemplateRef } from '@angular/core';

@Component({
  selector: 'ui-encabezado-pagina',
  standalone: true,
  imports: [CommonModule],
  template: `
    <header class="encabezado">
      <div>
        <h2>{{ titulo }}</h2>
        <p *ngIf="descripcion">{{ descripcion }}</p>
      </div>
      <ng-container *ngIf="acciones" [ngTemplateOutlet]="acciones"></ng-container>
    </header>
  `,
  styles: [`.encabezado{display:flex;justify-content:space-between;gap:1rem;align-items:flex-start;margin-bottom:1rem}h2{margin:0;font-size:1.35rem}p{margin:.25rem 0 0;color:var(--color-text-soft)}`]
})
export class UiEncabezadoPaginaComponent {
  @Input({ required: true }) titulo = '';
  @Input() descripcion = '';
  @Input() acciones?: TemplateRef<unknown> | null;
}

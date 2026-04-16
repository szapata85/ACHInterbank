import { CommonModule } from '@angular/common';
import { Component, Input, TemplateRef } from '@angular/core';

@Component({
  selector: 'ui-barra-filtros',
  standalone: true,
  imports: [CommonModule],
  template: `
    <section class="barra-filtros" [class.compacta]="compacta">
      <ng-container [ngTemplateOutlet]="contenido"></ng-container>
    </section>
  `,
  styles: [`.barra-filtros{display:grid;gap:.75rem;padding:1rem;border:1px solid var(--color-border);background:#fff;border-radius:var(--radius-lg);margin-bottom:1rem}.compacta{padding:.65rem}`]
})
export class UiBarraFiltrosComponent {
  @Input({ required: true }) contenido!: TemplateRef<unknown>;
  @Input() compacta = false;
}

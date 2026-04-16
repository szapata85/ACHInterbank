import { CommonModule } from '@angular/common';
import { Component, Input, TemplateRef } from '@angular/core';

@Component({
  selector: 'ui-formulario-seccion',
  standalone: true,
  imports: [CommonModule],
  template: `
    <section class="seccion-formulario">
      <header>
        <h3>{{ titulo }}</h3>
        <p *ngIf="descripcion">{{ descripcion }}</p>
      </header>
      <div class="contenido" *ngIf="contenido" [ngTemplateOutlet]="contenido"></div>
    </section>
  `,
  styles: [`.seccion-formulario{border:1px solid var(--color-border);border-radius:var(--radius-lg);padding:1rem;background:#fff}.seccion-formulario header{margin-bottom:.75rem}h3{margin:0}p{margin:.2rem 0 0;color:var(--color-text-soft)}.contenido{display:grid;gap:.75rem}`]
})
export class UiFormularioSeccionComponent {
  @Input({ required: true }) titulo = '';
  @Input() descripcion = '';
  @Input() contenido?: TemplateRef<unknown> | null;
}

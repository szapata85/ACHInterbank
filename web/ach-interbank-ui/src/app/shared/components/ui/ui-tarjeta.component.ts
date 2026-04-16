import { CommonModule } from '@angular/common';
import { Component, Input, TemplateRef } from '@angular/core';

@Component({
  selector: 'ui-tarjeta',
  standalone: true,
  imports: [CommonModule],
  template: `
    <section class="tarjeta">
      <header *ngIf="titulo || subtitulo" class="tarjeta__header">
        <h3 *ngIf="titulo">{{ titulo }}</h3>
        <p *ngIf="subtitulo">{{ subtitulo }}</p>
      </header>
      <ng-container *ngIf="contenido" [ngTemplateOutlet]="contenido"></ng-container>
    </section>
  `,
  styles: [`.tarjeta{background:#fff;border:1px solid var(--color-border);border-radius:var(--radius-lg);padding:1rem}.tarjeta__header{margin-bottom:.75rem}h3{margin:0}p{margin:.25rem 0 0;color:var(--color-text-soft)}`]
})
export class UiTarjetaComponent {
  @Input() titulo = '';
  @Input() subtitulo = '';
  @Input() contenido?: TemplateRef<unknown> | null;
}

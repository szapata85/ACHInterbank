import { Component, Input } from '@angular/core';

@Component({
  selector: 'ui-estado-vacio',
  standalone: true,
  template: `<div class="estado"><p class="titulo">{{ titulo }}</p><small>{{ mensaje }}</small></div>`,
  styles: [`.estado{padding:1rem;border:1px dashed var(--color-border-strong);border-radius:var(--radius-md);text-align:center;color:var(--color-text-soft)}.titulo{margin:0 0 .25rem;font-weight:600;color:var(--color-text-muted)}`]
})
export class UiEstadoVacioComponent {
  @Input() titulo = 'Sin resultados';
  @Input() mensaje = 'No se encontraron datos con los filtros actuales.';
}

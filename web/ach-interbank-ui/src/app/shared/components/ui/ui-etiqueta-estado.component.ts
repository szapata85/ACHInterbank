import { Component, Input } from '@angular/core';

export type UiEtiquetaEstado = 'activo' | 'inactivo' | 'pendiente' | 'fallido' | 'exitoso';

@Component({
  selector: 'ui-etiqueta-estado',
  standalone: true,
  template: `<span class="etiqueta" [class]="'etiqueta estado-' + estado">{{ texto }}</span>`,
  styles: [`.etiqueta{display:inline-flex;padding:.2rem .6rem;border-radius:var(--radius-pill);font-size:var(--font-xs);font-weight:600}.estado-activo,.estado-exitoso{background:var(--color-success-surface);color:var(--color-success-text)}.estado-inactivo{background:var(--color-surface-muted);color:var(--color-text-muted)}.estado-pendiente{background:var(--color-warning-surface);color:var(--color-warning-text)}.estado-fallido{background:var(--color-error-surface);color:var(--color-error-text)}`]
})
export class UiEtiquetaEstadoComponent {
  @Input() estado: UiEtiquetaEstado = 'activo';
  @Input() texto = 'Activo';
}

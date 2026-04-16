import { Component, Input } from '@angular/core';

export type UiEtiquetaEstado = 'activo' | 'inactivo' | 'pendiente' | 'fallido' | 'exitoso';

@Component({
  selector: 'ui-etiqueta-estado',
  standalone: true,
  template: `<span class="etiqueta" [class]="'etiqueta estado-' + estado">{{ texto }}</span>`,
  styles: [`.etiqueta{display:inline-flex;padding:.2rem .6rem;border-radius:999px;font-size:var(--font-xs);font-weight:600}.estado-activo,.estado-exitoso{background:#dcfce7;color:#166534}.estado-inactivo{background:#e2e8f0;color:#334155}.estado-pendiente{background:#ffedd5;color:#9a3412}.estado-fallido{background:#fee2e2;color:#991b1b}`]
})
export class UiEtiquetaEstadoComponent {
  @Input() estado: UiEtiquetaEstado = 'activo';
  @Input() texto = 'Activo';
}

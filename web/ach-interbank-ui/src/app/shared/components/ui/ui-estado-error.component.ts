import { Component, EventEmitter, Input, Output } from '@angular/core';

@Component({
  selector: 'ui-estado-error',
  standalone: true,
  template: `
    <div class="estado">
      <p class="titulo">{{ titulo }}</p>
      <small>{{ mensaje }}</small>
      <button *ngIf="mostrarReintentar" type="button" (click)="reintentar.emit()">Reintentar</button>
    </div>
  `,
  styles: [`.estado{padding:1rem;border:1px solid #fecaca;background:#fef2f2;border-radius:var(--radius-md);display:flex;flex-direction:column;gap:.5rem}.titulo{margin:0;color:#991b1b;font-weight:700}small{color:#7f1d1d}button{align-self:flex-start}`]
})
export class UiEstadoErrorComponent {
  @Input() titulo = 'No fue posible completar la operación';
  @Input() mensaje = 'Intente nuevamente o contacte al administrador.';
  @Input() mostrarReintentar = true;
  @Output() reintentar = new EventEmitter<void>();
}

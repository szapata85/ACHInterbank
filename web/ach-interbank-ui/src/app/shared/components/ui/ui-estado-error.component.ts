import { CommonModule } from '@angular/common';
import { Component, EventEmitter, Input, Output } from '@angular/core';

@Component({
  selector: 'ui-estado-error',
  standalone: true,
  imports: [CommonModule],
  template: `
    <div class="estado">
      <p class="titulo">{{ titulo }}</p>
      <small>{{ mensaje }}</small>
      <button *ngIf="mostrarReintentar" type="button" (click)="reintentar.emit()">Reintentar</button>
    </div>
  `,
  styles: [`
    .estado{padding:1rem;border:1px solid #fecaca;background:#fef2f2;border-radius:var(--radius-md);display:flex;flex-direction:column;gap:.5rem;animation:aparecer .18s ease}
    .titulo{margin:0;color:#991b1b;font-weight:700}
    small{color:#7f1d1d}
    button{align-self:flex-start;border:1px solid #dc2626;background:#fff;color:#b91c1c;border-radius:var(--radius-sm);padding:.35rem .7rem;font-weight:600;cursor:pointer;transition:all .18s ease}
    button:hover{background:#fee2e2}
    button:focus-visible{outline:none;box-shadow:0 0 0 3px rgba(220,38,38,.24)}
    @keyframes aparecer{from{opacity:0;transform:translateY(2px)}to{opacity:1;transform:translateY(0)}}
  `]
})
export class UiEstadoErrorComponent {
  @Input() titulo = 'No fue posible completar la operación';
  @Input() mensaje = 'Ocurrió un error al procesar la solicitud. Intente nuevamente.';
  @Input() mostrarReintentar = true;
  @Output() reintentar = new EventEmitter<void>();
}

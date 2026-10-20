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
      <button *ngIf="mostrarReintentar" type="button" (click)="reintentar.emit()">{{ textoReintentar }}</button>
    </div>
  `,
  styles: [`
    .estado{padding:1rem;border:1px solid var(--color-error-border);background:var(--color-error-surface);border-radius:var(--radius-md);display:flex;flex-direction:column;gap:.5rem;animation:aparecer .18s ease}
    .titulo{margin:0;color:var(--color-error-text);font-weight:700}
    small{color:var(--color-error-text)}
    button{align-self:flex-start;border:1px solid var(--color-error);background:var(--color-surface);color:var(--color-error);border-radius:var(--radius-sm);padding:.35rem .7rem;font-weight:600;cursor:pointer;transition:all .18s ease}
    button:hover{background:var(--color-error-surface)}
    button:focus-visible{outline:none;box-shadow:0 0 0 3px color-mix(in srgb,var(--color-error) 24%,transparent)}
    @keyframes aparecer{from{opacity:0;transform:translateY(2px)}to{opacity:1;transform:translateY(0)}}
  `]
})
export class UiEstadoErrorComponent {
  @Input() titulo = 'No fue posible completar la operación';
  @Input() mensaje = 'Ocurrió un error al procesar la solicitud. Intente nuevamente.';
  @Input() mostrarReintentar = true;
  @Input() textoReintentar = 'Reintentar';
  @Output() reintentar = new EventEmitter<void>();
}

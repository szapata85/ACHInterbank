import { CommonModule } from '@angular/common';
import { Component, EventEmitter, Input, Output } from '@angular/core';
import { UiBotonComponent } from './ui-boton.component';

@Component({
  selector: 'ui-modal-confirmacion',
  standalone: true,
  imports: [CommonModule, UiBotonComponent],
  template: `
    <div class="overlay" *ngIf="abierto" role="dialog" aria-modal="true" aria-label="Confirmación">
      <section class="modal">
        <h3>{{ titulo }}</h3>
        <p>{{ mensaje }}</p>
        <div class="acciones">
          <ui-boton texto="Cancelar" variante="contorno" (accion)="cancelar.emit()"></ui-boton>
          <ui-boton [texto]="textoConfirmar" [variante]="varianteConfirmar" (accion)="confirmar.emit()"></ui-boton>
        </div>
      </section>
    </div>
  `,
  styles: [`
    .overlay{position:fixed;inset:0;background:rgba(15,23,42,.45);display:grid;place-items:center;z-index:70;padding:1rem}
    .modal{width:min(520px,100%);max-height:calc(100dvh - 2rem);overflow:auto;background:#fff;border-radius:var(--radius-lg);padding:1rem 1.1rem}
    .modal h3{margin:0;overflow-wrap:anywhere}
    .modal p{margin:.5rem 0 1rem;color:var(--color-text-soft);overflow-wrap:anywhere}
    .acciones{display:flex;justify-content:flex-end;gap:.65rem;flex-wrap:wrap}
    @media(max-width:480px){.overlay{padding:.5rem}.modal{max-height:calc(100dvh - 1rem);padding:.9rem}.acciones{align-items:stretch;flex-direction:column-reverse}.acciones ui-boton{display:block}}
  `]
})
export class UiModalConfirmacionComponent {
  @Input() abierto = false;
  @Input() titulo = 'Confirmar acción';
  @Input() mensaje = '¿Desea continuar con esta operación?';
  @Input() textoConfirmar = 'Confirmar';
  @Input() varianteConfirmar: 'primario' | 'peligro' = 'primario';
  @Output() confirmar = new EventEmitter<void>();
  @Output() cancelar = new EventEmitter<void>();
}

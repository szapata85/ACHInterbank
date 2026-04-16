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
  styles: [`.overlay{position:fixed;inset:0;background:rgba(15,23,42,.45);display:grid;place-items:center;z-index:70}.modal{width:min(520px,92vw);background:#fff;border-radius:var(--radius-lg);padding:1rem 1.1rem}.modal h3{margin:0}.modal p{margin:.5rem 0 1rem;color:var(--color-text-soft)}.acciones{display:flex;justify-content:flex-end;gap:.65rem}`]
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

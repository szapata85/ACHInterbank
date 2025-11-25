import { ChangeDetectionStrategy, Component, EventEmitter, Input, Output } from '@angular/core';

@Component({
  selector: 'app-confirm-dialog',
  template: `
    <div class="backdrop" *ngIf="open">
      <div class="dialog" role="alertdialog" aria-modal="true">
        <h3>{{ title }}</h3>
        <p>{{ message }}</p>
        <div class="actions">
          <button type="button" class="secondary" (click)="cancel.emit()">{{ cancelText }}</button>
          <button type="button" class="danger" (click)="confirm.emit()">{{ confirmText }}</button>
        </div>
      </div>
    </div>
  `,
  styles: [
    `
      .backdrop {
        position: fixed;
        inset: 0;
        background: rgba(0, 0, 0, 0.35);
        display: grid;
        place-items: center;
        z-index: 1000;
      }
      .dialog {
        background: #fff;
        padding: 1.5rem;
        border-radius: 8px;
        width: min(420px, 90vw);
        box-shadow: 0 10px 30px rgba(0, 0, 0, 0.08);
      }
      h3 {
        margin: 0 0 0.5rem;
      }
      p {
        margin: 0 0 1.25rem;
        color: #4b5563;
      }
      .actions {
        display: flex;
        justify-content: flex-end;
        gap: 0.5rem;
      }
      button {
        border: 1px solid #d1d5db;
        padding: 0.45rem 0.9rem;
        border-radius: 6px;
        cursor: pointer;
      }
      .secondary {
        background: #fff;
      }
      .danger {
        background: #dc2626;
        border-color: #dc2626;
        color: #fff;
      }
    `
  ],
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class ConfirmDialogComponent {
  @Input() open = false;
  @Input() title = 'Confirmar acción';
  @Input() message = '¿Deseas continuar?';
  @Input() confirmText = 'Confirmar';
  @Input() cancelText = 'Cancelar';

  @Output() confirm = new EventEmitter<void>();
  @Output() cancel = new EventEmitter<void>();
}

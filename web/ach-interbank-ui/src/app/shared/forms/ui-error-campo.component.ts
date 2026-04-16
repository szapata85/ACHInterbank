import { CommonModule } from '@angular/common';
import { Component, Input } from '@angular/core';
import { AbstractControl } from '@angular/forms';
import { resolverMensajeValidacion } from './mensajes-validacion';

@Component({
  selector: 'ui-error-campo',
  standalone: true,
  imports: [CommonModule],
  template: `<small class="ui-error" *ngIf="mensaje">{{ mensaje }}</small>`,
  styles: [
    `
      .ui-error {
        color: var(--color-danger);
        font-size: var(--font-xs);
        font-weight: 600;
      }
    `
  ]
})
export class UiErrorCampoComponent {
  @Input({ required: true }) control!: AbstractControl | null;
  @Input() mensajesPersonalizados?: Record<string, (error: any) => string>;

  get mensaje(): string | null {
    if (!this.control || (!this.control.touched && !this.control.dirty)) {
      return null;
    }

    return resolverMensajeValidacion(this.control.errors, this.mensajesPersonalizados);
  }
}

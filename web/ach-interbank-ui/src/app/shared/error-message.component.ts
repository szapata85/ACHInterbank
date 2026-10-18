import { CommonModule } from '@angular/common';
import { ChangeDetectionStrategy, Component, Input } from '@angular/core';
import { AbstractControl } from '@angular/forms';

@Component({
  selector: 'app-error-message',
  standalone: true,
  imports: [CommonModule],
  template: `
    <p *ngIf="messageToShow" class="error-message">{{ messageToShow }}</p>
  `,
  styles: [
    `
      .error-message {
        color: #b91c1c;
        margin: 0.25rem 0 0;
        font-size: 0.75rem;
      }
    `
  ],
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class ErrorMessageComponent {
  @Input() control: AbstractControl | null = null;
  @Input() text: string | null = null;

  get messageToShow(): string | null {
    if (this.text) {
      return this.text;
    }

    if (!this.control || !this.control.touched || !this.control.invalid) {
      return null;
    }

    if (this.control.hasError('required')) {
      return 'Completa este campo.';
    }
    if (this.control.hasError('min')) {
      return 'Ingresa un valor igual o mayor que el mínimo permitido.';
    }
    if (this.control.hasError('pattern')) {
      return 'Revisa el formato del valor ingresado.';
    }
    if (this.control.hasError('email')) {
      return 'Ingresa un correo electrónico válido.';
    }
    if (this.control.hasError('invalidEmailDomain')) {
      return 'Ingresa un correo con un dominio válido.';
    }
    if (this.control.hasError('maxlength')) {
      return 'Reduce el texto a la longitud máxima permitida.';
    }
    if (this.control.hasError('sameAccount')) {
      return 'Usa cuentas de origen y destino diferentes.';
    }

    return 'Revisa el valor ingresado.';
  }
}

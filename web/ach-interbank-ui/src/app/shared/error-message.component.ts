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
      return 'Campo obligatorio';
    }
    if (this.control.hasError('min')) {
      return 'El valor ingresado es demasiado bajo';
    }
    if (this.control.hasError('pattern')) {
      return 'Formato inválido';
    }
    if (this.control.hasError('maxlength')) {
      return 'Se superó la longitud máxima permitida';
    }
    if (this.control.hasError('sameAccount')) {
      return 'La cuenta de origen y destino no pueden ser iguales';
    }

    return 'Dato inválido';
  }
}

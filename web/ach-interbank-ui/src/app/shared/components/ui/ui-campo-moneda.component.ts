import { CommonModule } from '@angular/common';
import { Component, Input } from '@angular/core';
import { FormControl, ReactiveFormsModule } from '@angular/forms';
import { UiErrorCampoComponent } from '../../forms/ui-error-campo.component';

@Component({
  selector: 'ui-campo-moneda',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, UiErrorCampoComponent],
  template: `
    <label class="ui-campo">
      <span class="ui-campo__label">{{ etiqueta }}</span>
      <div class="envoltura">
        <span class="prefijo">{{ moneda }}</span>
        <input type="text" [placeholder]="placeholder" [value]="valorVisible" (input)="onInput($event)" [disabled]="control.disabled" />
      </div>
      <small class="ui-campo__ayuda" *ngIf="ayuda">{{ ayuda }}</small>
      <ui-error-campo [control]="control"></ui-error-campo>
    </label>
  `,
  styles: [`.ui-campo{display:flex;flex-direction:column;gap:.35rem}.envoltura{display:grid;grid-template-columns:auto 1fr;align-items:center;border:1px solid var(--color-border-strong);border-radius:var(--radius-md);padding:0 .5rem;background:#fff}.prefijo{color:var(--color-text-soft);font-size:var(--font-sm);padding-right:.4rem} input{border:none;min-height:var(--control-height);outline:none}`]
})
export class UiCampoMonedaComponent {
  @Input({ required: true }) etiqueta = '';
  @Input({ required: true }) control!: FormControl;
  @Input() placeholder = '0';
  @Input() ayuda = '';
  @Input() moneda = 'COP';

  get valorVisible(): string {
    const valor = this.control.value;
    if (valor === null || valor === undefined || valor === '') {
      return '';
    }

    const numero = Number(valor);
    if (!Number.isFinite(numero)) {
      return String(valor);
    }

    return new Intl.NumberFormat('es-CO', { maximumFractionDigits: 0 }).format(numero);
  }

  onInput(event: Event): void {
    const input = event.target as HTMLInputElement;
    const digitos = input.value.replace(/[^0-9]/g, '');
    this.control.setValue(digitos ? Number(digitos) : null);
    this.control.markAsDirty();
    this.control.markAsTouched();
  }
}

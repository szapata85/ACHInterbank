import { CommonModule } from '@angular/common';
import { Component, Input } from '@angular/core';
import { FormControl, ReactiveFormsModule } from '@angular/forms';
import { UiErrorCampoComponent } from '../../forms/ui-error-campo.component';

@Component({
  selector: 'ui-campo-texto',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, UiErrorCampoComponent],
  template: `
    <label class="ui-campo">
      <span class="ui-campo__label">{{ etiqueta }}</span>
      <input [type]="tipo" [placeholder]="placeholder" [readonly]="soloLectura" [formControl]="control" />
      <small class="ui-campo__ayuda" *ngIf="ayuda">{{ ayuda }}</small>
      <ui-error-campo [control]="control"></ui-error-campo>
    </label>
  `,
  styles: [
    `
      .ui-campo { display: flex; flex-direction: column; gap: 0.35rem; }
      .ui-campo__label { font-weight: 600; font-size: var(--font-sm); color: var(--color-text); }
      input { width: 100%; min-height: var(--control-height); border: 1px solid var(--color-border-strong); border-radius: var(--radius-md); padding: 0.55rem 0.75rem; }
      .ui-campo__ayuda { color: var(--color-text-soft); font-size: var(--font-xs); }
    `
  ]
})
export class UiCampoTextoComponent {
  @Input({ required: true }) etiqueta = '';
  @Input({ required: true }) control!: FormControl;
  @Input() placeholder = '';
  @Input() ayuda = '';
  @Input() soloLectura = false;
  @Input() tipo: 'text' | 'email' | 'password' = 'text';
}

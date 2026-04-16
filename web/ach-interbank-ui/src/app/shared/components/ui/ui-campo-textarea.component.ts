import { CommonModule } from '@angular/common';
import { Component, Input } from '@angular/core';
import { FormControl, ReactiveFormsModule } from '@angular/forms';
import { UiErrorCampoComponent } from '../../forms/ui-error-campo.component';

@Component({
  selector: 'ui-campo-textarea',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, UiErrorCampoComponent],
  template: `
    <label class="ui-campo">
      <span class="ui-campo__label">{{ etiqueta }}</span>
      <textarea [rows]="filas" [placeholder]="placeholder" [formControl]="control"></textarea>
      <small class="ui-campo__ayuda" *ngIf="ayuda">{{ ayuda }}</small>
      <ui-error-campo [control]="control"></ui-error-campo>
    </label>
  `,
  styles: [`.ui-campo{display:flex;flex-direction:column;gap:.35rem} textarea{border:1px solid var(--color-border-strong);border-radius:var(--radius-md);padding:.55rem .75rem}`]
})
export class UiCampoTextareaComponent {
  @Input({ required: true }) etiqueta = '';
  @Input({ required: true }) control!: FormControl;
  @Input() placeholder = '';
  @Input() ayuda = '';
  @Input() filas = 4;
}

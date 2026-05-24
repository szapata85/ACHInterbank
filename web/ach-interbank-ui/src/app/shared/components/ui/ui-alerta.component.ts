import { CommonModule } from '@angular/common';
import { Component, Input } from '@angular/core';

export type UiAlertaTipo = 'info' | 'exito' | 'advertencia' | 'error';

@Component({
  selector: 'ui-alerta',
  standalone: true,
  imports: [CommonModule],
  template: `<div class="alerta" [class]="'alerta tipo-' + tipo"><strong *ngIf="titulo">{{ titulo }}</strong><span>{{ mensaje }}</span></div>`,
  styles: [`.alerta{display:flex;gap:.45rem;padding:.7rem .85rem;border-radius:var(--radius-md);border:1px solid transparent}.tipo-info{background:#eff6ff;border-color:#bfdbfe;color:#1e3a8a}.tipo-exito{background:#ecfdf5;border-color:#86efac;color:#166534}.tipo-advertencia{background:#fffbeb;border-color:#fde68a;color:#92400e}.tipo-error{background:#fef2f2;border-color:#fecaca;color:#991b1b}`]
})
export class UiAlertaComponent {
  @Input() tipo: UiAlertaTipo = 'info';
  @Input() titulo = '';
  @Input({ required: true }) mensaje = '';
}

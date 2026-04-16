import { CommonModule } from '@angular/common';
import { Component, EventEmitter, Input, Output } from '@angular/core';
import { Observable } from 'rxjs';
import { AccionProtegidaDirective } from '../../directives/accion-protegida.directive';

export type UiBotonVariante = 'primario' | 'secundario' | 'contorno' | 'fantasma' | 'peligro' | 'icono';

@Component({
  selector: 'ui-boton',
  standalone: true,
  imports: [CommonModule, AccionProtegidaDirective],
  template: `
    <button
      type="button"
      class="ui-boton"
      [ngClass]="['var-' + variante, tamano, iconoSolo ? 'icono-solo' : '']"
      [uiAccionProtegida]="claveAccion"
      [deshabilitado]="deshabilitado || cargando"
      [ejecutarAccion]="ejecutarAccionInterna"
      [enError]="enError"
    >
      <span *ngIf="cargando" class="spinner" aria-hidden="true"></span>
      <span *ngIf="icono" class="material-symbols-outlined" aria-hidden="true">{{ icono }}</span>
      <span *ngIf="!iconoSolo">{{ cargando && textoCargando ? textoCargando : texto }}</span>
    </button>
  `,
  styles: [
    `
      .ui-boton {
        border: 1px solid transparent;
        border-radius: var(--radius-md);
        min-height: var(--control-height);
        padding: 0.5rem 1rem;
        font-weight: 600;
        display: inline-flex;
        align-items: center;
        justify-content: center;
        gap: 0.45rem;
        cursor: pointer;
        box-shadow: 0 1px 2px rgba(15, 23, 42, 0.06);
        transition: transform 0.18s ease, box-shadow 0.18s ease, background-color 0.18s ease, border-color 0.18s ease, color 0.18s ease, opacity 0.18s ease;
      }
      .ui-boton:focus-visible { outline: none; box-shadow: 0 0 0 3px rgba(37, 99, 235, 0.28), 0 4px 10px rgba(15, 23, 42, 0.08); }
      .ui-boton:hover:not(:disabled) { transform: translateY(-1px); box-shadow: 0 8px 14px rgba(15, 23, 42, 0.12); }
      .ui-boton:active:not(:disabled) { transform: translateY(0) scale(0.985); box-shadow: 0 2px 6px rgba(15, 23, 42, 0.16); }
      .ui-boton:disabled { opacity: 0.56; cursor: not-allowed; box-shadow: none; filter: saturate(0.7); }
      .var-primario { background: var(--color-primary); color: #fff; border-color: var(--color-primary); }
      .var-primario:hover:not(:disabled) { background: var(--color-primary-hover); border-color: var(--color-primary-hover); }
      .var-secundario { background: var(--color-secondary); color: #fff; border-color: var(--color-secondary); }
      .var-secundario:hover:not(:disabled) { background: var(--color-secondary-hover); border-color: var(--color-secondary-hover); }
      .var-contorno { background: #fff; color: var(--color-primary); border-color: var(--color-primary); }
      .var-contorno:hover:not(:disabled) { background: #eff6ff; }
      .var-fantasma { background: transparent; color: var(--color-primary); border-color: transparent; }
      .var-fantasma:hover:not(:disabled) { background: #eff6ff; }
      .var-peligro { background: var(--color-danger); color: #fff; border-color: var(--color-danger); }
      .var-peligro:hover:not(:disabled) { background: var(--color-danger-hover); border-color: var(--color-danger-hover); }
      .var-icono { background: #fff; border-color: var(--color-border-strong); color: var(--color-text); }
      .icono-solo { width: var(--control-height); min-width: var(--control-height); padding: 0; }
      .sm { min-height: 32px; padding: 0.35rem 0.7rem; font-size: var(--font-xs); }
      .lg { min-height: 46px; padding: 0.65rem 1.2rem; font-size: var(--font-md); }
      .spinner {
        width: 14px;
        height: 14px;
        border: 2px solid rgba(255, 255, 255, 0.45);
        border-top-color: currentColor;
        border-radius: 50%;
        animation: giro 0.8s linear infinite;
      }
      .var-contorno .spinner,
      .var-fantasma .spinner,
      .var-icono .spinner {
        border-color: rgba(15, 23, 42, 0.25);
        border-top-color: currentColor;
      }
      @keyframes giro { to { transform: rotate(360deg); } }
    `
  ]
})
export class UiBotonComponent {
  @Input() texto = 'Acción';
  @Input() textoCargando = 'Procesando…';
  @Input() variante: UiBotonVariante = 'primario';
  @Input() tamano: 'sm' | 'md' | 'lg' = 'md';
  @Input() icono?: string;
  @Input() iconoSolo = false;
  @Input() deshabilitado = false;
  @Input() cargando = false;
  @Input() claveAccion = '';
  @Input() ejecutar?: () => void | Promise<unknown> | Observable<unknown>;
  @Input() enError?: (error: unknown) => void;
  @Output() accion = new EventEmitter<void>();

  ejecutarAccionInterna = () => {
    const resultado = this.ejecutar?.();
    if (resultado) {
      return resultado;
    }
    this.accion.emit();
    return;
  };
}

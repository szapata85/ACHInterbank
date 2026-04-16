import { CommonModule } from '@angular/common';
import { Component, EventEmitter, Input, Output } from '@angular/core';
import { FormControl, ReactiveFormsModule } from '@angular/forms';

@Component({
  selector: 'ui-campo-busqueda',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule],
  template: `
    <label class="ui-campo-busqueda">
      <span class="material-symbols-outlined" aria-hidden="true">search</span>
      <input type="search" [placeholder]="placeholder" [formControl]="control" (keyup.enter)="buscar.emit()" />
      <button *ngIf="control.value" type="button" class="limpiar" (click)="limpiar()">Limpiar</button>
    </label>
  `,
  styles: [
    `
      .ui-campo-busqueda {
        display: grid;
        grid-template-columns: auto 1fr auto;
        gap: .35rem;
        align-items: center;
        border: 1px solid var(--color-border-strong);
        border-radius: var(--radius-md);
        padding: 0 .5rem;
        min-height: var(--control-height);
        background: #fff;
      }
      .ui-campo-busqueda input {
        border: none;
        outline: none;
        color: var(--color-text);
      }
      .limpiar {
        border: none;
        background: transparent;
        color: var(--color-text);
        font-weight: 500;
        cursor: pointer;
      }
      .limpiar:hover,
      .limpiar:focus-visible {
        color: var(--color-primary-hover);
        text-decoration: underline;
      }
    `
  ]
})
export class UiCampoBusquedaComponent {
  @Input({ required: true }) control!: FormControl<string | null>;
  @Input() placeholder = 'Buscar...';
  @Output() buscar = new EventEmitter<void>();

  limpiar(): void {
    this.control.setValue('');
    this.buscar.emit();
  }
}

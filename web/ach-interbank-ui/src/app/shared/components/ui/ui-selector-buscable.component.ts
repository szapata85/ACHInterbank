import { CommonModule } from '@angular/common';
import { ChangeDetectionStrategy, Component, EventEmitter, Input, Output, forwardRef } from '@angular/core';
import { ControlValueAccessor, FormControl, NG_VALUE_ACCESSOR, ReactiveFormsModule } from '@angular/forms';

export interface OpcionSelectorBuscable {
  valor: string | number;
  etiqueta: string;
  descripcion?: string;
}

@Component({
  selector: 'ui-selector-buscable',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule],
  providers: [
    {
      provide: NG_VALUE_ACCESSOR,
      useExisting: forwardRef(() => UiSelectorBuscableComponent),
      multi: true
    }
  ],
  template: `
    <div class="ui-selector" [class.deshabilitado]="deshabilitado">
      <label *ngIf="etiqueta" class="etiqueta">{{ etiqueta }}</label>
      <div class="control">
        <input
          type="text"
          [formControl]="terminoControl"
          [placeholder]="placeholderBusqueda"
          [disabled]="deshabilitado || cargando"
        />
        <button type="button" class="limpiar" (click)="limpiarSeleccion()" [disabled]="deshabilitado || valorInterno == null">
          Limpiar
        </button>
      </div>

      <div class="lista" *ngIf="!cargando; else estadoCarga">
        <button
          type="button"
          class="opcion"
          *ngFor="let opcion of opcionesFiltradas"
          [class.seleccionada]="opcion.valor === valorInterno"
          (click)="seleccionar(opcion.valor)"
          [disabled]="deshabilitado"
        >
          <span>{{ opcion.etiqueta }}</span>
          <small *ngIf="opcion.descripcion">{{ opcion.descripcion }}</small>
        </button>

        <p class="sin-resultados" *ngIf="opcionesFiltradas.length === 0">Sin resultados para la búsqueda.</p>
      </div>

      <ng-template #estadoCarga>
        <p class="cargando">Cargando opciones...</p>
      </ng-template>
    </div>
  `,
  styles: [
    `
      .ui-selector { display: flex; flex-direction: column; gap: 0.4rem; }
      .etiqueta { font-weight: 600; font-size: var(--font-sm); }
      .control { display: grid; grid-template-columns: 1fr auto; gap: .5rem; }
      .control input { min-height: var(--control-height); border: 1px solid var(--color-border-strong); border-radius: var(--radius-md); padding: .5rem .75rem; }
      .limpiar { border: 1px solid var(--color-border-strong); background: #fff; border-radius: var(--radius-md); padding: .4rem .75rem; }
      .lista { border: 1px solid var(--color-border); border-radius: var(--radius-md); max-height: 220px; overflow: auto; display: flex; flex-direction: column; }
      .opcion { text-align: left; border: none; border-bottom: 1px solid var(--color-border); background: #fff; padding: .55rem .75rem; cursor: pointer; display:flex;flex-direction:column; }
      .opcion:last-child { border-bottom: none; }
      .opcion:hover:not(:disabled), .opcion.seleccionada { background: #eff6ff; }
      .sin-resultados, .cargando { margin: 0; padding: .75rem; color: var(--color-text-soft); }
      .deshabilitado { opacity: .75; }
    `
  ],
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class UiSelectorBuscableComponent implements ControlValueAccessor {
  @Input() etiqueta = '';
  @Input() placeholderBusqueda = 'Buscar opción...';
  @Input() cargando = false;
  @Input() opciones: OpcionSelectorBuscable[] = [];
  @Output() busqueda = new EventEmitter<string>();

  readonly terminoControl = new FormControl<string>('', { nonNullable: true });
  valorInterno: string | number | null = null;
  deshabilitado = false;
  opcionesFiltradas: OpcionSelectorBuscable[] = [];

  private onChange: (value: string | number | null) => void = () => {};
  private onTouched: () => void = () => {};

  constructor() {
    this.terminoControl.valueChanges.subscribe(() => this.filtrar());
  }

  ngOnChanges(): void {
    this.filtrar();
  }

  writeValue(value: string | number | null): void {
    this.valorInterno = value;
  }

  registerOnChange(fn: (value: string | number | null) => void): void {
    this.onChange = fn;
  }

  registerOnTouched(fn: () => void): void {
    this.onTouched = fn;
  }

  setDisabledState(isDisabled: boolean): void {
    this.deshabilitado = isDisabled;
  }

  filtrar(): void {
    const termino = this.terminoControl.value.trim().toLocaleLowerCase();
    this.opcionesFiltradas = this.opciones.filter((o) => o.etiqueta.toLocaleLowerCase().includes(termino));
    this.busqueda.emit(this.terminoControl.value);
  }

  seleccionar(valor: string | number): void {
    this.valorInterno = valor;
    this.onChange(valor);
    this.onTouched();
  }

  limpiarSeleccion(): void {
    this.valorInterno = null;
    this.onChange(null);
    this.onTouched();
  }
}

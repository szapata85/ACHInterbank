import { CommonModule } from '@angular/common';
import { Component, EventEmitter, Input, Output } from '@angular/core';
import { UiBotonComponent } from './ui-boton.component';

@Component({
  selector: 'ui-formulario-acciones',
  standalone: true,
  imports: [CommonModule, UiBotonComponent],
  template: `
    <div class="acciones-formulario">
      <ui-boton
        [texto]="textoCancelar"
        variante="contorno"
        [deshabilitado]="deshabilitadoCancelar || procesando"
        (accion)="cancelar.emit()"
      ></ui-boton>
      <ui-boton
        [texto]="textoGuardar"
        [textoCargando]="textoProcesando"
        variante="primario"
        [deshabilitado]="deshabilitadoGuardar"
        [cargando]="procesando"
        [claveAccion]="claveAccion"
        [ejecutar]="ejecutarGuardar"
      ></ui-boton>
    </div>
  `,
  styles: [`.acciones-formulario{display:flex;justify-content:flex-end;gap:.65rem;flex-wrap:wrap}`]
})
export class UiFormularioAccionesComponent {
  @Input() textoGuardar = 'Guardar';
  @Input() textoCancelar = 'Cancelar';
  @Input() textoProcesando = 'Guardando...';
  @Input() deshabilitadoGuardar = false;
  @Input() deshabilitadoCancelar = false;
  @Input() procesando = false;
  @Input() claveAccion = '';
  @Input() ejecutarGuardar?: () => void | Promise<unknown> | import('rxjs').Observable<unknown>;

  @Output() cancelar = new EventEmitter<void>();
}

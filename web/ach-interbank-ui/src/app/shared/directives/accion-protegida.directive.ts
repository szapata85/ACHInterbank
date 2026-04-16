import { Directive, HostBinding, HostListener, Input } from '@angular/core';
import { EMPTY, from, isObservable } from 'rxjs';
import { catchError, finalize } from 'rxjs/operators';
import { AccionAsincronaService } from '../../core/services/patrones/accion-asincrona.service';

@Directive({
  selector: '[uiAccionProtegida]',
  standalone: true
})
export class AccionProtegidaDirective {
  constructor(private readonly patron: AccionAsincronaService) {}

  @Input('uiAccionProtegida') claveAccion = '';
  @Input() deshabilitado = false;
  @Input() ejecutarAccion?: () => void | Promise<unknown> | import('rxjs').Observable<unknown>;
  @Input() enError?: (error: unknown) => void;

  procesando = false;

  @HostBinding('disabled')
  get disabledHost(): boolean {
    return this.deshabilitado || this.procesando;
  }

  @HostBinding('attr.aria-busy')
  get ariaBusy(): string {
    return String(this.procesando);
  }

  @HostListener('click', ['$event'])
  manejarClick(event: Event): void {
    event.preventDefault();
    event.stopPropagation();

    if (this.disabledHost || !this.ejecutarAccion) {
      return;
    }

    const clave = this.claveAccion || this.generarClaveTemporal();
    const resultado = this.ejecutarAccion();
    const flujo = isObservable(resultado)
      ? resultado
      : resultado instanceof Promise
        ? from(resultado)
        : from(Promise.resolve(resultado));

    this.procesando = true;
    this.patron
      .ejecutar(clave, flujo)
      .pipe(
        catchError((error) => {
          this.enError?.(error);
          return EMPTY;
        }),
        finalize(() => {
          this.procesando = false;
        })
      )
      .subscribe();
  }

  private generarClaveTemporal(): string {
    return `accion:${Math.random().toString(36).slice(2)}`;
  }
}

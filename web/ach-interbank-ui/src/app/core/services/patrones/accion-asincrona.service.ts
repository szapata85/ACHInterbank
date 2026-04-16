import { Injectable } from '@angular/core';
import { Observable, from, isObservable } from 'rxjs';
import { finalize } from 'rxjs/operators';

@Injectable({ providedIn: 'root' })
export class AccionAsincronaService {
  private accionesEnCurso = new Set<string>();

  estaProcesando(clave: string): boolean {
    return this.accionesEnCurso.has(clave);
  }

  ejecutar<T>(clave: string, accion: Observable<T> | Promise<T> | (() => Observable<T> | Promise<T>)): Observable<T> {
    if (this.estaProcesando(clave)) {
      return from(Promise.reject(new Error('Acción en curso.')));
    }

    this.accionesEnCurso.add(clave);

    const resultado = typeof accion === 'function' ? accion() : accion;
    const stream = isObservable(resultado) ? resultado : from(resultado);

    return stream.pipe(
      finalize(() => {
        this.accionesEnCurso.delete(clave);
      })
    );
  }
}

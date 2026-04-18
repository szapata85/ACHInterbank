import { Injectable } from '@angular/core';
import { BehaviorSubject } from 'rxjs';
import { NachaConfigProfileDetail, NachaConfigValidationResult } from '../models/nacha-config-admin.models';

@Injectable({ providedIn: 'root' })
export class NachaConfigStateService {
  private readonly perfilSubject = new BehaviorSubject<NachaConfigProfileDetail | null>(null);
  readonly perfil$ = this.perfilSubject.asObservable();

  private readonly validacionSubject = new BehaviorSubject<NachaConfigValidationResult | null>(null);
  readonly validacion$ = this.validacionSubject.asObservable();

  setPerfil(perfil: NachaConfigProfileDetail | null): void {
    this.perfilSubject.next(perfil);
  }

  setValidacion(validacion: NachaConfigValidationResult | null): void {
    this.validacionSubject.next(validacion);
  }

  rowVersionActual(): string {
    return this.perfilSubject.value?.rowVersion ?? '';
  }
}

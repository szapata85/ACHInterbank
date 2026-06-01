import { Injectable, inject } from '@angular/core';
import { map } from 'rxjs';
import { NachaConfigApiService } from './nacha-config-api.service';

@Injectable({ providedIn: 'root' })
export class NachaConfigQueryService {
  private readonly api = inject(NachaConfigApiService);

  perfiles() {
    return this.api.listarPerfiles().pipe(map((rows) => rows ?? []));
  }

  dashboardReadOnly() {
    return this.api.dashboardReadOnly();
  }

  perfilesReadOnly() {
    return this.api.listarPerfilesReadOnly().pipe(map((rows) => rows ?? []));
  }

  detalleReadOnly(profileId: number) {
    return this.api.obtenerPerfilReadOnly(profileId);
  }

  variantesReadOnly(profileId: number) {
    return this.api.variantesReadOnly(profileId).pipe(map((rows) => rows ?? []));
  }

  fieldsReadOnly(profileId: number) {
    return this.api.fieldsReadOnly(profileId).pipe(map((rows) => rows ?? []));
  }

  catalogosFiltro() {
    return this.api.catalogosFiltro().pipe(map((catalogos) => ({
      estados: catalogos?.estados ?? [],
      camaras: catalogos?.camaras ?? [],
      flujos: catalogos?.flujos ?? [],
      direcciones: catalogos?.direcciones ?? [],
      servicios: catalogos?.servicios ?? []
    })));
  }

  detalle(profileId: number) {
    return this.api.obtenerPerfil(profileId);
  }

  historial(profileId: number) {
    return this.api.historial(profileId).pipe(map((rows) => rows ?? []));
  }

  snapshots(profileId: number) {
    return this.api.snapshots(profileId).pipe(map((rows) => rows ?? []));
  }
}

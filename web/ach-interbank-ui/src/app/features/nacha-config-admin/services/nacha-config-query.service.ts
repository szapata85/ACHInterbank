import { Injectable, inject } from '@angular/core';
import { map } from 'rxjs';
import { NachaConfigApiService } from './nacha-config-api.service';

@Injectable({ providedIn: 'root' })
export class NachaConfigQueryService {
  private readonly api = inject(NachaConfigApiService);

  perfiles() {
    return this.api.listarPerfiles().pipe(map((rows) => rows ?? []));
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

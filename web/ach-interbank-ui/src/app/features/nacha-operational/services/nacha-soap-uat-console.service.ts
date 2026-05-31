import { Injectable } from '@angular/core';
import { Observable, catchError, forkJoin, throwError } from 'rxjs';
import { ApiService } from '../../../core/services/api.service';
import {
  NachaSoapUatAudit,
  NachaSoapUatCandidate,
  NachaSoapUatConsoleDashboard
} from '../models/nacha-operational.models';

export interface NachaSoapUatConsoleData {
  dashboard: NachaSoapUatConsoleDashboard;
  candidates: NachaSoapUatCandidate[];
  audit: NachaSoapUatAudit[];
}

@Injectable({ providedIn: 'root' })
export class NachaSoapUatConsoleService {
  private readonly basePath = 'api/ach/nacha/soap-uat-console';

  constructor(private readonly api: ApiService) {}

  getDashboard(): Observable<NachaSoapUatConsoleDashboard> {
    return this.api.get<NachaSoapUatConsoleDashboard>(`${this.basePath}/dashboard`);
  }

  getCandidates(): Observable<NachaSoapUatCandidate[]> {
    return this.api.get<NachaSoapUatCandidate[]>(`${this.basePath}/candidates`);
  }

  getAudit(): Observable<NachaSoapUatAudit[]> {
    return this.api.get<NachaSoapUatAudit[]>(`${this.basePath}/audit`);
  }

  getCandidate(correlationId: string): Observable<NachaSoapUatCandidate> {
    return this.api.get<NachaSoapUatCandidate>(`${this.basePath}/candidates/${encodeURIComponent(correlationId)}`).pipe(
      catchError((error) => {
        const message = error?.status === 404
          ? 'Candidato SOAP/UAT no encontrado.'
          : 'No fue posible cargar el candidato SOAP/UAT.';
        return throwError(() => new Error(message));
      })
    );
  }

  getConsoleData(): Observable<NachaSoapUatConsoleData> {
    return forkJoin({
      dashboard: this.getDashboard(),
      candidates: this.getCandidates(),
      audit: this.getAudit()
    });
  }
}

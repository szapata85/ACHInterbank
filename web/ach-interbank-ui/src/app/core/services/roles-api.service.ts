import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { RoleSummary } from '../models/role.model';
import { ApiService } from './api.service';

@Injectable({ providedIn: 'root' })
export class RolesApiService {
  private readonly api = inject(ApiService);
  private readonly basePath = 'api/roles';

  getRoles(): Observable<RoleSummary[]> {
    return this.api.get<RoleSummary[]>(this.basePath);
  }
}

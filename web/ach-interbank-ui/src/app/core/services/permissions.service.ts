import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { Permission } from '../models/permission.model';
import { ApiService } from './api.service';

@Injectable({ providedIn: 'root' })
export class PermissionsService {
  private readonly api = inject(ApiService);
  private readonly basePath = 'api/permissions';

  getPermissions(): Observable<Permission[]> {
    return this.api.get<Permission[]>(this.basePath);
  }
}

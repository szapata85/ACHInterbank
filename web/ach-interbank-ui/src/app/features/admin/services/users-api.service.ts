import { Injectable, inject } from '@angular/core';
import { ApiService } from '../../../core/services/api.service';
import { map, Observable } from 'rxjs';
import { ApiResponse } from '../../../core/models/api-response.model';
import { PagedResponse, SaveUserRequest, UserFilter, UserSummary, RoleSummary } from '../models/user.model';

@Injectable({ providedIn: 'root' })
export class UsersApiService {
  private readonly api = inject(ApiService);
  private readonly basePath = 'api/users';

  getUsers(filter: UserFilter): Observable<PagedResponse<UserSummary>> {
    const params: Record<string, string | number | boolean | undefined> = {
      search: filter.search,
      roleId: filter.roleId,
      page: filter.page ?? 1,
      pageSize: filter.pageSize ?? 10
    };
    return this.api.get<PagedResponse<UserSummary>>(this.basePath, { params });
  }

  getUser(id: string): Observable<UserSummary> {
    return this.api.get<ApiResponse<UserSummary>>(`${this.basePath}/${id}`).pipe(
      map((response) => response.data)
    );
  }

  createUser(request: SaveUserRequest): Observable<UserSummary> {
    return this.api.post<UserSummary>(this.basePath, request);
  }

  updateUser(id: string, request: SaveUserRequest): Observable<UserSummary> {
    return this.api.put<UserSummary>(`${this.basePath}/${id}`, request);
  }

  assignRoles(id: string, roleIds: string[]): Observable<void> {
    return this.api.post<void>(`${this.basePath}/${id}/roles`, { roleIds });
  }

  validateEmailDomain(email: string): Observable<boolean> {
    return this.api.get<boolean>(`${this.basePath}/validate-email-domain`, { params: { email } });
  }

  deactivateUser(id: string): Observable<void> {
    return this.api.delete<void>(`${this.basePath}/${id}`);
  }
}

@Injectable({ providedIn: 'root' })
export class RolesApiService {
  private readonly api = inject(ApiService);
  private readonly basePath = 'api/roles';

  getRoles(): Observable<RoleSummary[]> {
    return this.api.get<RoleSummary[]>(this.basePath);
  }
}

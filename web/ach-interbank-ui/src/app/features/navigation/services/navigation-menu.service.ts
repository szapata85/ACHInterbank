import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { ApiService } from '../../../core/services/api.service';
import { NavigationMenuItem, SaveNavigationMenuItem } from '../models/navigation-menu.model';

@Injectable({ providedIn: 'root' })
export class NavigationMenuService {
  private readonly api = inject(ApiService);
  private readonly basePath = 'navigation/menu-items';

  getMenuItems(): Observable<NavigationMenuItem[]> {
    return this.api.get<NavigationMenuItem[]>(this.basePath);
  }

  createMenuItem(request: SaveNavigationMenuItem): Observable<NavigationMenuItem> {
    return this.api.post<NavigationMenuItem>(this.basePath, request);
  }

  updateMenuItem(id: number, request: SaveNavigationMenuItem): Observable<NavigationMenuItem> {
    return this.api.put<NavigationMenuItem>(`${this.basePath}/${id}`, request);
  }

  deleteMenuItem(id: number): Observable<void> {
    return this.api.delete<void>(`${this.basePath}/${id}`);
  }
}

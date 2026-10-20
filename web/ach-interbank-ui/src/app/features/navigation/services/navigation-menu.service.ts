import { Injectable, inject } from '@angular/core';
import { map, Observable } from 'rxjs';
import { ApiService } from '../../../core/services/api.service';
import { NavigationMenuItem, SaveNavigationMenuItem } from '../models/navigation-menu.model';

const RETIRED_FRONTEND_ROUTES = new Set(['/aliases']);

@Injectable({ providedIn: 'root' })
export class NavigationMenuService {
  private readonly api = inject(ApiService);
  private readonly basePath = 'api/navigation/menu-items';

  getMenuItems(): Observable<NavigationMenuItem[]> {
    return this.api.get<NavigationMenuItem[]>(this.basePath).pipe(
      map((items) => this.removeRetiredMenuItems(items ?? []))
    );
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

  private removeRetiredMenuItems(items: NavigationMenuItem[]): NavigationMenuItem[] {
    return items
      .filter((item) => !RETIRED_FRONTEND_ROUTES.has(this.normalizeRoute(item.route)))
      .map((item) => ({
        ...item,
        children: item.children?.length ? this.removeRetiredMenuItems(item.children) : []
      }));
  }

  private normalizeRoute(route?: string | null): string {
    return (route ?? '')
      .trim()
      .toLowerCase()
      .replace(/\/+$/, '');
  }
}

import { Injectable, inject } from '@angular/core';
import { map, Observable } from 'rxjs';
import { MenuItem } from '../models/menu.model';
import { ApiService } from './api.service';

const LEGACY_NACHA_ROUTES = new Set([
  '/ach-cycles/nacha/layouts',
  '/ach-cycles/nacha/definitions',
  '/nacha-layouts',
  '/nacha-record-definitions'
]);

@Injectable({ providedIn: 'root' })
export class NavigationService {
  private readonly api = inject(ApiService);

  getMenu(): Observable<MenuItem[]> {
    return this.api.get<MenuItem[]>('api/navigation/menu').pipe(
      map((items) => this.sortMenu(this.removeLegacyNachaMenuItems(items ?? [])))
    );
  }

  private sortMenu(items: MenuItem[]): MenuItem[] {
    return [...items]
      .sort((a, b) => (a.order ?? 0) - (b.order ?? 0))
      .map((item) => ({
        ...item,
        children: item.children?.length ? this.sortMenu(item.children) : []
      }));
  }

  private removeLegacyNachaMenuItems(items: MenuItem[]): MenuItem[] {
    return items
      .filter((item) => !LEGACY_NACHA_ROUTES.has(this.normalizeRoute(item.route)))
      .map((item) => ({
        ...item,
        children: item.children?.length ? this.removeLegacyNachaMenuItems(item.children) : []
      }));
  }

  private normalizeRoute(route?: string | null): string {
    return (route ?? '')
      .trim()
      .toLowerCase()
      .replace(/\/+$/, '');
  }
}

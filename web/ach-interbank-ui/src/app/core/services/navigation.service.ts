import { Injectable, inject } from '@angular/core';
import { Observable, map } from 'rxjs';
import { MenuItem } from '../models/menu.model';
import { ApiService } from './api.service';

@Injectable({ providedIn: 'root' })
export class NavigationService {
  private readonly api = inject(ApiService);

  getMenu(): Observable<MenuItem[]> {
    return this.api.get<MenuItem[]>('navigation/menu').pipe(
      map((items) => this.sortMenu(items ?? []))
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
}

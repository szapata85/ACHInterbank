import { Injectable, inject } from '@angular/core';
import { ActivatedRouteSnapshot } from '@angular/router';
import { Observable, shareReplay } from 'rxjs';
import { MenuItem } from '../models/menu.model';
import { NavigationService } from './navigation.service';
import {
  NavigationNaming,
  RouteNamingEntry,
  canonicalLabelForRoute,
  resolveNavigationNaming
} from './navigation-naming.util';

export type { NavigationBreadcrumb, NavigationNaming, RouteNamingEntry } from './navigation-naming.util';

@Injectable({ providedIn: 'root' })
export class NavigationNamingService {
  private readonly navigationService = inject(NavigationService);

  readonly menu$: Observable<MenuItem[]> = this.navigationService.getMenu().pipe(
    shareReplay({ bufferSize: 1, refCount: false })
  );

  resolve(
    menuItems: MenuItem[],
    currentUrl: string,
    routeRoot: ActivatedRouteSnapshot
  ): NavigationNaming {
    return resolveNavigationNaming(menuItems, currentUrl, this.collectRouteEntries(routeRoot));
  }

  resolveEntries(
    menuItems: MenuItem[],
    currentUrl: string,
    routeEntries: RouteNamingEntry[]
  ): NavigationNaming {
    return resolveNavigationNaming(menuItems, currentUrl, routeEntries);
  }

  canonicalLabelForRoute(menuItems: MenuItem[], route: string | undefined, fallback: string): string {
    return canonicalLabelForRoute(menuItems, route, fallback);
  }

  private collectRouteEntries(routeRoot: ActivatedRouteSnapshot): RouteNamingEntry[] {
    const entries: RouteNamingEntry[] = [];
    const pathSegments: string[] = [];
    let current: ActivatedRouteSnapshot | null = routeRoot;

    while (current) {
      for (const segment of current.url) {
        pathSegments.push(segment.path);
      }

      const breadcrumb = current.data['breadcrumb'];
      const title = current.data['title'];
      if (breadcrumb || title) {
        entries.push({
          url: pathSegments.length ? `/${pathSegments.join('/')}` : '/',
          breadcrumb: typeof breadcrumb === 'string' ? breadcrumb : undefined,
          title: typeof title === 'string' ? title : undefined
        });
      }

      current = current.firstChild;
    }

    return entries;
  }
}

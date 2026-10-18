import {
  EnvironmentInjector,
  Injectable,
  Type,
  createNgModule,
  inject,
  runInInjectionContext
} from '@angular/core';
import { PRIMARY_OUTLET, ROUTES, Route, Router, Routes } from '@angular/router';
import { firstValueFrom, isObservable } from 'rxjs';

/**
 * Determines whether an exact menu URL resolves to its own functional Angular surface.
 *
 * A route is functional when the exact URL activates a component/loadComponent directly,
 * or through empty-path child routes. Redirect-only destinations and missing routes are
 * structural. Lazy route configuration is inspected through the real Router loaders and
 * cached; no menu path list is maintained here.
 */
@Injectable({ providedIn: 'root' })
export class MenuRouteSurfaceService {
  private readonly router = inject(Router);
  private readonly environmentInjector = inject(EnvironmentInjector);
  private readonly loadedChildren = new WeakMap<Route, Promise<Routes>>();

  async hasFunctionalSurface(url: string | null | undefined): Promise<boolean> {
    if (!url) {
      return false;
    }

    let segments: string[];
    try {
      const urlTree = this.router.parseUrl(url);
      segments = urlTree.root.children[PRIMARY_OUTLET]?.segments.map((segment) => segment.path) ?? [];
    } catch {
      return false;
    }

    if (!segments.length) {
      return false;
    }

    return (await this.matchExactUrl(this.router.config, segments, 0)) ?? false;
  }

  private async matchExactUrl(
    routes: Routes,
    segments: string[],
    segmentIndex: number
  ): Promise<boolean | null> {
    for (const route of routes) {
      if (route.outlet && route.outlet !== PRIMARY_OUTLET) {
        continue;
      }

      const path = route.path ?? '';
      if (path === '**') {
        continue;
      }

      const routeSegments = path.split('/').filter(Boolean);

      if (routeSegments.length === 0) {
        if (route.redirectTo !== undefined) {
          if (segmentIndex === segments.length) {
            return false;
          }
          continue;
        }

        const children = await this.getChildRoutes(route);
        if (children.length) {
          const childMatch = await this.matchExactUrl(children, segments, segmentIndex);
          if (childMatch !== null) {
            return childMatch;
          }
        }
        continue;
      }

      if (!this.pathMatches(routeSegments, segments, segmentIndex)) {
        continue;
      }

      const nextIndex = segmentIndex + routeSegments.length;
      if (nextIndex === segments.length) {
        return this.hasSurfaceAtExactRoute(route);
      }

      const children = await this.getChildRoutes(route);
      if (!children.length) {
        continue;
      }

      const childMatch = await this.matchExactUrl(children, segments, nextIndex);
      if (childMatch !== null) {
        return childMatch;
      }
    }

    return null;
  }

  private async hasSurfaceAtExactRoute(route: Route): Promise<boolean> {
    if (route.redirectTo !== undefined) {
      return false;
    }

    if (route.component || route.loadComponent) {
      return true;
    }

    const children = await this.getChildRoutes(route);
    return this.hasDefaultFunctionalSurface(children);
  }

  private async hasDefaultFunctionalSurface(routes: Routes): Promise<boolean> {
    for (const route of routes) {
      if ((route.outlet && route.outlet !== PRIMARY_OUTLET) || (route.path ?? '') !== '') {
        continue;
      }

      if (route.redirectTo !== undefined) {
        return false;
      }

      if (route.component || route.loadComponent) {
        return true;
      }

      const children = await this.getChildRoutes(route);
      return this.hasDefaultFunctionalSurface(children);
    }

    return false;
  }

  private pathMatches(routeSegments: string[], urlSegments: string[], segmentIndex: number): boolean {
    if (segmentIndex + routeSegments.length > urlSegments.length) {
      return false;
    }

    return routeSegments.every((routeSegment, index) => {
      const urlSegment = urlSegments[segmentIndex + index];
      return routeSegment.startsWith(':') || routeSegment === urlSegment;
    });
  }

  private async getChildRoutes(route: Route): Promise<Routes> {
    if (route.children) {
      return route.children;
    }

    if (!route.loadChildren) {
      return [];
    }

    const cached = this.loadedChildren.get(route);
    if (cached) {
      return cached;
    }

    const loaded = this.loadLazyChildren(route).catch(() => [] as Routes);
    this.loadedChildren.set(route, loaded);
    return loaded;
  }

  private async loadLazyChildren(route: Route): Promise<Routes> {
    const loader = route.loadChildren;
    if (!loader) {
      return [];
    }

    const loaderResult = runInInjectionContext(this.environmentInjector, () => loader());
    const resolved = isObservable(loaderResult)
      ? await firstValueFrom(loaderResult)
      : await Promise.resolve(loaderResult);
    const unwrapped = this.unwrapDefaultExport(resolved as unknown);

    if (Array.isArray(unwrapped)) {
      return unwrapped as Routes;
    }

    if (typeof unwrapped !== 'function') {
      return [];
    }

    const moduleRef = createNgModule(unwrapped as Type<unknown>, this.environmentInjector);
    try {
      const routeGroups = moduleRef.injector.get(ROUTES, [] as Route[][], { self: true });
      return routeGroups.flat();
    } finally {
      moduleRef.destroy();
    }
  }

  private unwrapDefaultExport(value: unknown): unknown {
    if (value && typeof value === 'object' && 'default' in value) {
      return (value as { default: unknown }).default;
    }

    return value;
  }
}

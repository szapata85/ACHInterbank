import { Injectable, inject } from '@angular/core';
import { NavigationEnd, Router } from '@angular/router';
import { catchError, filter, of } from 'rxjs';
import { ApiService } from './api.service';
import { AuthService } from './auth.service';

interface NavigationLogCreate {
  route: string;
  visitedAt?: string;
  sessionId?: string;
  durationMs?: number;
}

@Injectable({ providedIn: 'root' })
export class NavigationLogService {
  private readonly api = inject(ApiService);
  private readonly router = inject(Router);
  private readonly auth = inject(AuthService);

  private previousRoute: string | null = null;
  private previousVisitedAt: number | null = null;
  private readonly sessionKey = 'ach_navigation_session_id';

  startTracking(): void {
    this.router.events
      .pipe(filter((event): event is NavigationEnd => event instanceof NavigationEnd))
      .subscribe((event) => {
        if (!this.auth.isAuthenticated()) {
          return;
        }

        const route = this.normalizeRoute(event.urlAfterRedirects);
        if (!route || this.shouldIgnoreRoute(route)) {
          return;
        }

        const now = Date.now();
        const previousDuration =
          this.previousVisitedAt && this.previousRoute
            ? Math.max(1, now - this.previousVisitedAt)
            : undefined;

        const payload: NavigationLogCreate = {
          route,
          visitedAt: new Date(now).toISOString(),
          sessionId: this.getSessionId(),
          durationMs: previousDuration
        };

        this.api
          .post('api/navigation-logs', payload)
          .pipe(catchError(() => of(null)))
          .subscribe();

        this.previousRoute = route;
        this.previousVisitedAt = now;
      });
  }

  private normalizeRoute(url: string): string {
    let route = (url ?? '').trim();
    if (!route) {
      return '';
    }

    const queryIndex = route.indexOf('?');
    if (queryIndex >= 0) {
      route = route.substring(0, queryIndex);
    }

    const hashIndex = route.indexOf('#');
    if (hashIndex >= 0) {
      route = route.substring(0, hashIndex);
    }

    if (!route.startsWith('/')) {
      route = `/${route}`;
    }

    return route;
  }

  private shouldIgnoreRoute(route: string): boolean {
    return route.startsWith('/auth');
  }

  private getSessionId(): string {
    if (typeof window === 'undefined') {
      return 'server-session';
    }

    const current = window.localStorage.getItem(this.sessionKey);
    if (current) {
      return current;
    }

    const generated = window.crypto?.randomUUID?.() ?? `${Date.now()}-${Math.random()}`;
    window.localStorage.setItem(this.sessionKey, generated);
    return generated;
  }
}

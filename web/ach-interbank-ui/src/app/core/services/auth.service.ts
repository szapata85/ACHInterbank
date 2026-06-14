import { Inject, Injectable, PLATFORM_ID, inject } from '@angular/core';
import { isPlatformBrowser } from '@angular/common';
import { NavigationEnd, Router } from '@angular/router';
import { BehaviorSubject, EMPTY, Observable, catchError, filter, map, switchMap, tap } from 'rxjs';
import { TokenStorageService } from '../../security/token-storage.service';
import { environment } from '../../../environments/environment';
import { ApiResponse } from '../models/api-response.model';
import { AuthPayload, LoginRequestModel, UserSession } from '../models/auth.models';
import { ApiService } from './api.service';

interface AuthResponse extends ApiResponse<AuthPayload> {}

@Injectable({ providedIn: 'root' })
export class AuthService {
  private readonly api = inject(ApiService);
  private readonly tokenStorage = inject(TokenStorageService);
  private readonly router = inject(Router);
  private readonly platformId = inject(PLATFORM_ID);

  private readonly authEndpoint = environment.authEndpoint ?? 'auth';
  private readonly userSubject = new BehaviorSubject<UserSession | null>(null);
  readonly user$: Observable<UserSession | null> = this.userSubject.asObservable();

  constructor() {
    const token = this.tokenStorage.getAccessToken();
    if (token) {
      this.hydrateFromToken(token);
    }

    this.router.events
      .pipe(
        filter((event): event is NavigationEnd => event instanceof NavigationEnd),
        filter(() => this.isAuthenticated()),
        switchMap(() =>
          this.refreshSession().pipe(
            catchError(() => {
              this.logout();
              return EMPTY;
            })
          )
        )
      )
      .subscribe();
  }

  login(credentials: LoginRequestModel): Observable<UserSession> {
    return this.api
      .post<AuthResponse>(`${this.authEndpoint}/login`, credentials)
      .pipe(
        map((response) => {
          if (!response.sucess || !response.data?.token) {
            throw new Error(response.message ?? 'No fue posible iniciar sesión.');
          }
          return response.data;
        }),
        map((payload: AuthPayload) => this.persistSession(payload)),
        tap((session: UserSession) => this.userSubject.next(session))
      );
  }

  forgotPassword(email: string): Observable<void> {
    return this.api
      .post<ApiResponse<unknown>>(`${this.authEndpoint}/forgot-password`, { email })
      .pipe(map(() => void 0));
  }

  resetPassword(token: string, newPassword: string, confirmPassword: string): Observable<void> {
    return this.api
      .post<ApiResponse<unknown>>(`${this.authEndpoint}/reset-password`, {
        token,
        newPassword,
        confirmPassword
      })
      .pipe(map(() => void 0));
  }

  refreshSession(): Observable<UserSession> {
    return this.api
      .post<AuthResponse>(`${this.authEndpoint}/refresh`, {})
      .pipe(
        map((response) => {
          if (!response.sucess || !response.data?.token) {
            throw new Error(response.message ?? 'No fue posible refrescar la sesión.');
          }
          return response.data;
        }),
        map((payload: AuthPayload) => this.persistSession(payload)),
        tap((session: UserSession) => this.userSubject.next(session))
      );
  }

  logout(): void {
    this.tokenStorage.clear();
    this.userSubject.next(null);
    this.clearClientCaches();
    this.router.navigate(['/login']);
  }

  getToken(): string | null {
    return this.tokenStorage.getAccessToken();
  }

  getCurrentUser(): UserSession | null {
    return this.userSubject.value;
  }

  isAuthenticated(): boolean {
    const session = this.userSubject.value;
    if (!session) {
      return false;
    }

    if (!session.expiresAt) {
      return true;
    }

    return session.expiresAt.getTime() > Date.now();
  }

  hasRole(expected: string | string[]): boolean {
    const session = this.userSubject.value;
    if (!session) return false;

    const roles = Array.isArray(expected) ? expected : [expected];
    return roles.some((role) => session.roles.includes(role));
  }

  hasPermission(expected: string | string[]): boolean {
    const session = this.userSubject.value;
    if (!session) return false;

    const permissions = Array.isArray(expected) ? expected : [expected];
    return permissions.some((permission) => session.permissions.includes(permission));
  }

  get currentUser(): UserSession | null {
    return this.userSubject.value;
  }

  private persistSession(payload: AuthPayload): UserSession {
    this.tokenStorage.setAccessToken(payload.token);
    return this.hydrateFromToken(payload.token, payload);
  }

  private hydrateFromToken(token: string, payload?: AuthPayload): UserSession {
    const parsed = this.parseJwt(token);
    const rawExp = parsed['exp'];
    const rawIat = parsed['iat'];
    const exp = typeof rawExp === 'number' ? rawExp : typeof rawExp === 'string' ? Number(rawExp) : undefined;
    const expiresAt = exp ? new Date(exp * 1000) : undefined;
    const iat = typeof rawIat === 'number' ? rawIat : typeof rawIat === 'string' ? Number(rawIat) : undefined;
    const issuedAt = iat ? new Date(iat * 1000) : undefined;
    const roles = this.toStringArray(
      parsed['role'] ?? parsed['http://schemas.microsoft.com/ws/2008/06/identity/claims/role'] ?? payload?.roles
    );
    const permissions = this.toStringArray(parsed['permission'] ?? payload?.permissions);

    const session: UserSession = {
      token,
      username: (parsed['unique_name'] as string) ?? payload?.username ?? 'usuario',
      fullName: (parsed['name'] as string) ?? payload?.fullName ?? payload?.username ?? 'Usuario',
      userId: (parsed['uid'] as string) ?? (parsed['sub'] as string),
      roles,
      permissions,
      issuedAt,
      expiresAt
    };

    this.userSubject.next(session);
    return session;
  }

  private parseJwt(token: string): Record<string, unknown> {
    try {
      const [, payload] = token.split('.');
      const decoded = atob(payload.replace(/-/g, '+').replace(/_/g, '/'));
      return JSON.parse(decoded) as Record<string, unknown>;
    } catch (error) {
      throw new Error('Token inválido');
    }
  }

  private toStringArray(value: unknown): string[] {
    if (!value) return [];
    if (Array.isArray(value)) return value.map((item) => String(item));
    return [String(value)];
  }

  private clearClientCaches(): void {
    if (!isPlatformBrowser(this.platformId)) {
      return;
    }

    try {
      window.sessionStorage.clear();
      window.localStorage.clear();
    } catch {
      // ignored
    }

    if ('caches' in window) {
      void window.caches.keys().then((keys) => Promise.all(keys.map((key) => window.caches.delete(key))));
    }
  }
}

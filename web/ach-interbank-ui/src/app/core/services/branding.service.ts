import { Injectable, inject } from '@angular/core';
import { BehaviorSubject, Observable, catchError, map, of, tap } from 'rxjs';
import { BrandingSettings } from '../models/branding.model';
import { ApiService } from './api.service';

@Injectable({ providedIn: 'root' })
export class BrandingService {
  private readonly api = inject(ApiService);
  private readonly brandingSubject = new BehaviorSubject<BrandingSettings>({});
  readonly branding$: Observable<BrandingSettings> = this.brandingSubject.asObservable();

  constructor() {
    this.refreshFromServer().subscribe();
  }

  updateBranding(settings: Partial<BrandingSettings>): Observable<BrandingSettings> {
    return this.api
      .put<BrandingSettings>('api/users/branding', settings)
      .pipe(
        map((response) => response ?? {}),
        tap((saved) => this.persistBranding(saved))
      );
  }

  getBrandingSnapshot(): BrandingSettings {
    return this.brandingSubject.value;
  }

  refreshFromServer(): Observable<BrandingSettings> {
    return this.api.get<BrandingSettings>('api/users/branding').pipe(
      map((settings) => settings ?? {}),
      tap((settings) => this.persistBranding(settings)),
      catchError(() => {
        const fallback = this.brandingSubject.value;
        this.brandingSubject.next(fallback);
        return of(fallback);
      })
    );
  }

  private persistBranding(settings: BrandingSettings): void {
    const next: BrandingSettings = { ...this.brandingSubject.value, ...settings };
    this.brandingSubject.next(next);
    this.applyThemeCssVariables(next);
  }

  private applyThemeCssVariables(settings: BrandingSettings): void {
    if (typeof document === 'undefined') {
      return;
    }

    const root = document.documentElement;
    const primary = this.normalizeHexColor(settings.buttonColor) ?? '#2563eb';
    const hover = this.shiftHexLightness(primary, -0.12);
    const contrast = this.getContrastText(primary);

    root.style.setProperty('--button-color', primary);
    root.style.setProperty('--color-primary', primary);
    root.style.setProperty('--color-primary-hover', hover);
    root.style.setProperty('--color-primary-contrast', contrast);
  }

  private normalizeHexColor(value: string | null | undefined): string | null {
    if (!value) {
      return null;
    }

    const raw = value.trim();
    const shortHex = /^#([0-9a-fA-F]{3})$/;
    const longHex = /^#([0-9a-fA-F]{6})$/;

    if (longHex.test(raw)) {
      return raw.toLowerCase();
    }

    const shortMatch = raw.match(shortHex);
    if (shortMatch) {
      const [r, g, b] = shortMatch[1].split('');
      return `#${r}${r}${g}${g}${b}${b}`.toLowerCase();
    }

    return null;
  }

  private shiftHexLightness(hex: string, delta: number): string {
    const r = parseInt(hex.slice(1, 3), 16);
    const g = parseInt(hex.slice(3, 5), 16);
    const b = parseInt(hex.slice(5, 7), 16);

    const shift = (channel: number): number => {
      if (delta < 0) {
        return Math.round(channel * (1 + delta));
      }

      return Math.round(channel + (255 - channel) * delta);
    };

    const nr = Math.max(0, Math.min(255, shift(r)));
    const ng = Math.max(0, Math.min(255, shift(g)));
    const nb = Math.max(0, Math.min(255, shift(b)));

    return `#${nr.toString(16).padStart(2, '0')}${ng.toString(16).padStart(2, '0')}${nb.toString(16).padStart(2, '0')}`;
  }

  private getContrastText(hex: string): string {
    const r = parseInt(hex.slice(1, 3), 16);
    const g = parseInt(hex.slice(3, 5), 16);
    const b = parseInt(hex.slice(5, 7), 16);
    const luminance = (0.299 * r + 0.587 * g + 0.114 * b) / 255;

    return luminance > 0.62 ? '#0f172a' : '#ffffff';
  }
}
